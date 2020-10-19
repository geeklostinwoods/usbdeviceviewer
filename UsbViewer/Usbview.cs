using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UsbViewer
{
    public partial class Usbview : Form
    {
        public Usbview()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.dataGridView1.Size = this.Size;
            this.dataGridView1.DataSource = CurrentlyPluggedUsbDevices.CreateUSBDataTable();
        }

        private void Usbview_Resize(object sender, EventArgs e)
        {
            this.dataGridView1.Size = this.Size;
        }
    }
}
