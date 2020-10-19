using System;
using System.Collections.Generic;
using System.Data;
using System.Management;
using System.Text;

namespace UsbViewer
{
    /// <summary>
    /// CurrentlyPluggedUsbDevices provide two methods to get the currently plugged usb devices
    ///  as Datatable using CreateUSBDataTable()
    ///  as List using GetUSBDevices()
    /// </summary>
    static class CurrentlyPluggedUsbDevices
    {
        /// <summary>
        /// USBDeviceInfo object consists of 
        /// Device Name - USB Device Name
        /// Manufacturer Name - USB Device Manufacturer Name
        /// Description - USB Device Description
        /// Vendor Id and Product ID - VID and PID parsed from the DeviceID
        /// </summary>
        public class USBDeviceInfo
        {
            public USBDeviceInfo(string deviceName, string manufacturerName, string description, string vendorId, string productId)
            {
                this.DeviceName = deviceName;
                this.ManufacturerName = manufacturerName;
                this.Description = description;
                this.VendorId = vendorId;
                this.ProductId = productId;
            }
            /// <summary>
            /// DeviceName comes from the Caption Property of the Win32_PnPEntity (Short description of the object.)
            /// https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-pnpentity
            /// </summary>
            public string DeviceName { get; private set; }

            /// <summary>
            /// Manufacturer Name comes from the Manufacturer Property of the Win32_PNPEntity 
            /// (Name of the manufacturer of the Plug and Play device.)
            /// https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-pnpentity
            /// </summary>
            public string ManufacturerName { get; private set; }

            /// <summary>
            /// Description come from the Description Property of the Win32_PNPEntity
            /// Description of the object
            /// </summary>
            public string Description { get; private set; }
            /// <summary>
            /// Vendor ID comes from the DeviceID Property of the Win32_PNPEntity but this is a PARSED value
            /// based on Identifier of the Plug and Play device.
            /// https://docs.microsoft.com/en-us/windows-hardware/drivers/install/hardware-ids 
            /// </summary>
            public string VendorId { get; private set; }
            public string ProductId { get; private set; }
        }
        /// <summary>
        /// CreateUSBDataTable creates a datatable with the USBDeviceInfo objects.
        /// </summary>
        /// <returns>DataTable of USBDeviceInfo objects</returns>
        public static DataTable CreateUSBDataTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("DeviceName");
            dt.Columns.Add("Description");
            dt.Columns.Add("Manufacturer");
            dt.Columns.Add("VID");
            dt.Columns.Add("PID");

            var usbDevices = GetUSBDevices();

            foreach (var usbDevice in usbDevices)
            {
                if (usbDevice.DeviceName != null)
                {
                    DataRow row = dt.NewRow();
                    dt.Rows.Add(usbDevice.DeviceName, usbDevice.ManufacturerName,
                        usbDevice.Description, usbDevice.VendorId, usbDevice.ProductId);
                }
            }

            return dt;
        }
        /// <summary>
        /// GetUSBDevices() will return all the usb devices that are currently plugged in. This will be list of usdeviceinfo objects
        /// Parsing is based on the https://docs.microsoft.com/en-us/windows-hardware/drivers/install/hardware-ids
        /// </summary>
        /// <returns> List<USBDeviceInfo> </returns>
        public static List<USBDeviceInfo> GetUSBDevices()
        {
            List<USBDeviceInfo> devices = new List<USBDeviceInfo>();
            ManagementObjectCollection collection;

            // query wmi's win32 usb controller. This will give all the usb devices, but we need win32_pnpentity for more details
            using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_USBControllerDevice"))
                collection = searcher.Get();
            List<string> Win32_UsbControllerDevices = new List<string>();

            foreach (var device in collection)
            {
                string deviceName = device.GetPropertyValue("Dependent").ToString().Split('=')[1].Replace('"', ' ');
                deviceName = deviceName.Replace("\\\\", "\\").Trim();
                Win32_UsbControllerDevices.Add(deviceName);
            }

            // query wmi for the win32_pnpentity for friendly name, manufacturer, description, device id etc
            using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_PnPEntity"))
                collection = searcher.Get();

            foreach (var device in collection)
            {
                try
                {
                    // retrieve each property from the wmi and map it to the class, so we can display in UI later
                    string pnpDevice = device.GetPropertyValue("PNPDeviceID")?.ToString()?.Replace("\\\\", "\\");
                    string pnpdeviceName = (string)device.GetPropertyValue("Caption");
                    string pnpManufacturer = (string)device.GetPropertyValue("Manufacturer");
                    string pnpDescription = (string)device.GetPropertyValue("Description");
                    string pnpDeviceId = device.GetPropertyValue("DeviceID")?.ToString()?.Replace("\\\\", "\\");

                    // unable to make the split work with double slashes, need to fix this code later
                    string[] splitPnpDeviceId = pnpDeviceId?.Replace("\\", "▄")?.Split('▄');
                    string pid = null, vid = null, pidpart = null, vidpart = null;

                    // ugly parts of wmi, we have to manually parse VID and PID based on its location
                    if (splitPnpDeviceId.Length >= 1 && splitPnpDeviceId[1].Contains("VID_") && splitPnpDeviceId[1].Contains("PID_"))
                    {
                        vidpart = splitPnpDeviceId[1].ToString().Split('&')?[0];
                        pidpart = splitPnpDeviceId[1].ToString().Split('&')?[1];

                        if (vidpart != null && vidpart.Contains("VID_"))
                            vid = vidpart.Split('_')[1];

                        if (pidpart != null && pidpart.Contains("PID_"))
                            pid = pidpart.Split('_')[1];

                    }

                    // Make sure that the device exists in both the Win32_PnPEntity and Win32_USBControllerDevice
                    if (Win32_UsbControllerDevices.Contains(pnpDevice))
                    {
                        devices.Add(new USBDeviceInfo(pnpdeviceName,
                                       pnpManufacturer,
                                       pnpDescription,
                                       vid,
                                       pid
                                    ));
                    }
                }
                catch (Exception)
                {
                    // ignore the parsers errors for now.
                }
            }

            collection.Dispose();
            return devices;
        }
    }
}
