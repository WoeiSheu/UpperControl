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
    }
}
