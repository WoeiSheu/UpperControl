using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.Text.RegularExpressions;

namespace UpperControl
{
    public partial class hypocrisyForm : Form
    {
        private Dictionary<CheckBox, TextBox> hypocrisyGetData = new Dictionary<CheckBox, TextBox>();
        private Regex regNum = new Regex("^[0-9]+$");
        private SerialPort hypocrisySerialPort = new SerialPort();
        private StringBuilder builder = new StringBuilder();    //避免在事件处理方法中反复的创建,定义到外面
        private long receivedCount = 0;                         //接收计数
        private List<byte> buffer = new List<byte>(4096);       //默认分配1页内存，并始终限制不允许超过
        private byte[] catchedBinaryData = new byte[10];         //缓存的10字节数据
        private short[] receivedData = new short[4];
        private const int timerInterval = 5;
        private System.Timers.Timer hypocrisyTimer = new System.Timers.Timer(timerInterval);
        private UInt64 timeCount = 0;

        public hypocrisyForm()
        {
            InitializeComponent();
            //Control.CheckForIllegalCrossThreadCalls = false;
        }

        private void hypocrisyForm_Load(object sender, EventArgs e)
        {
            //初始化下拉串口名称列表框
            /*
             * 测试无效
             * 猜想原因:用的Framework4.0
             * 2015-04-18
             * 测试发现有效,前提是在发现某个串口的情况下才会显示该串口
             */
            string[] ports = new string[]{"COM1","COM2","COM3","COM4","COM5","COM6"};
            if (SerialPort.GetPortNames().Length > 0)
            {
                ports = SerialPort.GetPortNames();
            }
            Array.Sort(ports);
            hypocrisyComboBox1.Items.AddRange(ports);
            //初始化为CRC校验
            cRCToolStripMenuItem.Checked = true;
            //初始化SerialPort对象  
            hypocrisySerialPort.NewLine = "/r/n";
            hypocrisySerialPort.RtsEnable = true;   //在串行通信中启用请求发送(RTS)信号  
            //添加事件注册
            hypocrisySerialPort.DataReceived += hypocrisyDataReceived;
            //添加接收数据部分界面的字典
            hypocrisyGetData.Add(hypocrisyCheckBox1, hypocrisyTextBox1);
            hypocrisyGetData.Add(hypocrisyCheckBox2, hypocrisyTextBox2);
            hypocrisyGetData.Add(hypocrisyCheckBox3, hypocrisyTextBox3);
            hypocrisyGetData.Add(hypocrisyCheckBox4, hypocrisyTextBox4);
            hypocrisyGetData.Add(hypocrisyCheckBox5, hypocrisyTextBox5);
            hypocrisyGetData.Add(hypocrisyCheckBox6, hypocrisyTextBox6);
            hypocrisyGetData.Add(hypocrisyCheckBox7, hypocrisyTextBox7);
            hypocrisyGetData.Add(hypocrisyCheckBox8, hypocrisyTextBox8);
            hypocrisyGetData.Add(hypocrisyCheckBox9, hypocrisyTextBox9);

            //添加定时器处理
            hypocrisyTimer.Elapsed += new System.Timers.ElapsedEventHandler(timerDispose);
            hypocrisyTimer.AutoReset = true;
            hypocrisyTimer.Enabled = false;
            //添加Chart画图初始化
            //hypocrisyScope.ChartAreas[0].BackColor = Color.Black;
            hypocrisyScope.ChartAreas[0].CursorX.IsUserEnabled = true;
            hypocrisyScope.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;
            hypocrisyScope.ChartAreas[0].CursorX.AutoScroll = true;
            //hypocrisyScope.ChartAreas[0].CursorX.IntervalType = System.Windows.Forms.DataVisualization.Charting.DateTimeIntervalType.Seconds;
            hypocrisyScope.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
            hypocrisyScope.ChartAreas[0].AxisX.ScaleView.Size = 100;
            //hypocrisyScope.ChartAreas[0].AxisX.ScaleView.SmallScrollSize = double.NaN;
            //hypocrisyScope.ChartAreas[0].AxisX.ScaleView.SmallScrollMinSize = 20;
            hypocrisyScope.ChartAreas[0].AxisX.ScrollBar.IsPositionedInside = true;
            hypocrisyScope.ChartAreas[0].AxisX.ScrollBar.ButtonColor = Color.Green;
            hypocrisyScope.ChartAreas[0].AxisX.ScrollBar.BackColor = Color.Cyan;
            hypocrisyScope.ChartAreas[0].AxisX.ScrollBar.ButtonStyle = System.Windows.Forms.DataVisualization.Charting.ScrollBarButtonStyles.SmallScroll;
            hypocrisyScope.ChartAreas[0].AxisX.ScrollBar.Size = 6;
            hypocrisyScope.ChartAreas[0].AxisX.ScrollBar.Enabled = true;            
            hypocrisyScope.ChartAreas[0].AxisX.Interval = 10;
            //hypocrisyScope.ChartAreas[0].AxisX.IntervalType = System.Windows.Forms.DataVisualization.Charting.DateTimeIntervalType.Seconds;
            //hypocrisyScope.ChartAreas[0].AxisX.IsLabelAutoFit = false;
            this.hypocrisyScope.Series[0].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
        }

        void hypocrisyDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int n = hypocrisySerialPort.BytesToRead;//先记录下来,避免某种原因,人为的原因,操作几次之间时间长,缓存不一致
            byte[] buf = new byte[n];//声明一个临时数组存储当前来的串口数据
            bool dataCatched = false;
            receivedCount += n;//增加接收计数
            hypocrisySerialPort.Read(buf, 0, n);//读取缓冲数据
            buffer.AddRange(buf);
            
            if (cRCToolStripMenuItem.Checked)
            {
                ushort CRCResult = 0xffff;
                while (buffer.Count >= 10)
                {
                    for (int iCount = 0; iCount < 8; iCount++)
                    {
                        CRCResult ^= buffer[iCount];
                        for (int jCount = 0; jCount < 8; jCount++)
                        {
                            if ((0x01 & CRCResult) != 0)
                                CRCResult = Convert.ToUInt16((Convert.ToInt32(CRCResult) >> 1) ^ 0xa001);
                            else
                                CRCResult = Convert.ToUInt16(Convert.ToInt32(CRCResult) >> 1);
                        }
                    }
                    buffer.CopyTo(0, catchedBinaryData, 0, 10);
                    if (CRCResult != BitConverter.ToUInt16(catchedBinaryData, 8))
                    {
                        buffer.RemoveRange(0, 10);
                        continue;
                    }
                    dataCatched = true;
                    buffer.RemoveRange(0, 10);
                }
            }
            else if (checkSumToolStripMenuItem.Checked)
            {
                byte checkSum = 0;
                while (buffer.Count >= 9)
                {
                    for (int iCount = 0; iCount < 8; iCount++)
                    {
                        checkSum ^= buffer[iCount];
                    }
                    if (checkSum != buffer[8])
                    {
                        buffer.RemoveRange(0, 9);
                        continue;
                    }
                    buffer.CopyTo(0, catchedBinaryData, 0, 9);
                    dataCatched = true;
                    buffer.RemoveRange(0, 9);
                }
            }
            else
            {
                while (buffer.Count >= 8)
                {
                    buffer.CopyTo(0, catchedBinaryData, 0, 8);
                    dataCatched = true;
                    buffer.RemoveRange(0, 8);
                }
            }

            if (dataCatched)
            {
                for (int kCount = 0; kCount < 4; kCount++)
                {
                    receivedData[kCount] = BitConverter.ToInt16(catchedBinaryData, kCount*2);
                }
            }

            builder.Clear();//清除字符串构造器的内容

            //因为要访问ui资源,所以需要使用invoke方式同步ui
            this.Invoke((EventHandler)(delegate
            {
                int iCount = 0;
                //在文本框显示接收的各个数据
                foreach (KeyValuePair<CheckBox, TextBox> getDataCollection in hypocrisyGetData)
                {
                    if (getDataCollection.Key.Checked)
                    {
                        if (iCount < 4)
                        {
                            getDataCollection.Value.Text = receivedData[iCount].ToString();
                        }
                        iCount++;
                    }
                    if (iCount > 4)
                    {
                        MessageBox.Show("Now, you can only check 4 variables.");
                        break;
                    }
                }
            }));
        }

        private void hypocrisyButton1_Click(object sender, EventArgs e)
        {
            if (!hypocrisySerialPort.IsOpen)
            {
                if (hypocrisyComboBox1.Text.IndexOf("COM") == -1)
                {
                    MessageBox.Show("Please select a serial port.");
                    return;
                }
                if (!regNum.IsMatch(hypocrisyComboBox2.Text))
                {
                    MessageBox.Show("Please select a baudRate.");
                    return;
                }
                hypocrisySerialPort.PortName = hypocrisyComboBox1.Text;
                hypocrisySerialPort.BaudRate = int.Parse(hypocrisyComboBox2.Text);
                try
                {
                    hypocrisySerialPort.Open();
                }
                catch (Exception ex)
                {
                    hypocrisySerialPort = new SerialPort();
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                hypocrisySerialPort.Close();
            }
            hypocrisyButton1.Text = hypocrisySerialPort.IsOpen ? "Close" : "Open";
        }

        private void hypocrisyButton2_Click(object sender, EventArgs e)
        {
            if (!regNum.IsMatch(hypocrisyComboBox2.Text))
            {
                MessageBox.Show("Please select a baudRate.");
                return;
            }
            hypocrisySerialPort.BaudRate = int.Parse(hypocrisyComboBox2.Text);
        }

        private void hypocrisyLinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://weibo.com/u/2613432527/home");
        }

        private void hypocrisyLinkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.xiami.com/u/31608105");
        }

        private void hypocrisyLinkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("www.douban.com/people/66128646");
        }

        private void hypocrisyLinkLabel4_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://twitter.com/hecate_xw");
        }

        private void hypocrisySend_Click(object sender, EventArgs e)
        {
            //定义一个变量,记录发送了几个字节
            int sendCount = 0;
            string originalSting = hypocrisySendText.Text;
            char[] hypocrisySeperator = new char[] { ',', ';', '\n' };
            string[] hypocrisySendString = originalSting.Split(hypocrisySeperator);
            //检测选中项并发送
            for (sendCount = 0; sendCount < hypocrisyCheckedListBox1.CheckedItems.Count; sendCount++)
            {
                //16进制发送
                if (hypocrisyCheckHex.Checked)
                {
                    //我们不管规则了,如果写错了一些,我们允许的,只用正则得到有效的十六进制数
                    string patternString = "[0-9a-fA-F]";
                    MatchCollection mc = Regex.Matches(hypocrisySendString[sendCount], patternString);
                    List<byte> buf = new List<byte>();//填充到这个临时列表
                    //依次添加到列表中
                    foreach (Match m in mc)
                    {
                        //System.Globalization.NumberStyles.HexNumber这个一定要加,这个Exception我找了2小时
                        buf.Add(byte.Parse(m.Value, System.Globalization.NumberStyles.HexNumber));
                    }
                    //转换列表为数组后发送
                    if (hypocrisySerialPort.IsOpen)
                    {
                        hypocrisySerialPort.Write(buf.ToArray(), 0, buf.Count);
                    }
                }
                else//ascii编码直接发送
                {
                    try
                    {
                        byte[] tempData = BitConverter.GetBytes(Int16.Parse(hypocrisySendString[sendCount]));
                        if (hypocrisySerialPort.IsOpen)
                        {
                            hypocrisySerialPort.Write(tempData, 0, 2);
                        }
                    }
                    catch
                    {
                        if (hypocrisySerialPort.IsOpen)
                        {
                            hypocrisySerialPort.Write(hypocrisySendString[sendCount]);
                        }
                    }
                }
            }
        }

        private void hypocrisyReset_Click(object sender, EventArgs e)
        {
            //清空接收数据和CheckBox选项
            foreach (KeyValuePair<CheckBox, TextBox> getDataCollection in hypocrisyGetData)
            {
                getDataCollection.Key.Checked = false;
                getDataCollection.Value.Text = null;
            }
            //清空发送数据
            for (int j = 0; j < hypocrisyCheckedListBox1.Items.Count; j++)
            {
                hypocrisyCheckedListBox1.SetItemChecked(j, false);
            }
            hypocrisySendText.Text = null;
            //清空串口选项
            hypocrisyComboBox1.Text = null;
            hypocrisyComboBox2.Text = null;
            //恢复Hex初始状态
            hypocrisyCheckHex.Checked = false;

        }

        private void setupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            hypocrisySetup setupForm = new hypocrisySetup();
            setupForm.Show();
        }

        private void checkSumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cRCToolStripMenuItem.Checked = false;
            checkSumToolStripMenuItem.Checked = !checkSumToolStripMenuItem.Checked;
        }

        private void cRCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            checkSumToolStripMenuItem.Checked = false;
            cRCToolStripMenuItem.Checked = !cRCToolStripMenuItem.Checked;
        }

        private void hypocrisyClear_Click(object sender, EventArgs e)
        {
            foreach (TextBox textBox in hypocrisyGetData.Values)
            {
                textBox.Text = null;
            }
        }

        private void timerDispose(Object source, System.Timers.ElapsedEventArgs e)
        {
            double xVlaue = Convert.ToDouble(timeCount);
            //double yValue = Math.Exp(Math.Sin(xVlaue));
            double yValue = receivedData[0];

            this.Invoke((EventHandler)(delegate
            {
                this.hypocrisyScope.Series[0].Points.AddXY(xVlaue, yValue);
                hypocrisyScope.ChartAreas[0].AxisX.ScaleView.Scroll(DateTime.Now);  //实时滚动
            }));

            timeCount += timerInterval;
        }

        private void hypocrisyButtonScope_Click(object sender, EventArgs e)
        {
            if(hypocrisyButtonScope.Text == "View Scope")
            {
                hypocrisyButtonScope.Text = "Stop View";
                hypocrisyTimer.Enabled = true;
            }
            else
            {
                hypocrisyButtonScope.Text = "View Scope";
                hypocrisyTimer.Enabled = false;
            }
        }

        private void hypocrisyScopeClear_Click(object sender, EventArgs e)
        {
            double[] xClear = {}, yClear = {};
            for (int iCount = 0; iCount < hypocrisyScope.Series.Count; iCount++)
            {
                hypocrisyScope.Series[iCount].Points.DataBindXY(xClear, yClear);
            }
        }
    }
}
