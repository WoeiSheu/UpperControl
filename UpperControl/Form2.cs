using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace UpperControl
{
    public partial class hypocrisySetup : Form
    {
        private string verifyBits = "None", dataBits = "8", stopBits = "1";

        public hypocrisySetup()
        {
            InitializeComponent();
        }

        private void hypocrisySetup_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = comboBox1.Items.Count > 0 ? 0 : -1;
            comboBox2.SelectedIndex = comboBox2.Items.Count > 0 ? 0 : -1;
            comboBox3.SelectedIndex = comboBox3.Items.Count > 0 ? 0 : -1;
        }

        public string[] getSetupData()
        {
            string[] setupData = {verifyBits, dataBits, stopBits};
            return setupData;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            verifyBits = comboBox1.Text;
            dataBits   = comboBox1.Text;
            stopBits   = comboBox1.Text;
            this.Close();
        }
    }
}
