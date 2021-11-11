using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using ZedGraph;

namespace ArastirmaProjesi
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();            
        }

        string[] serialPorts = SerialPort.GetPortNames();

        GraphPane myPane = new GraphPane();

        double samples = 0;

        PointPairList list0 = new PointPairList();
        PointPairList list1 = new PointPairList();
        PointPairList list2 = new PointPairList();      

        LineItem ch0, ch1, ch2;

        //Serial Comunication definitions
        byte[] serialRxBuff = new byte[100];
        int serialRxBuffCount = 0;
        byte serialRxBuff_previous = 0;

        float accel_x, accel_y, accel_z;

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            if (backgroundWorker1.CancellationPending)
            {
                e.Cancel = true;
            }

            this.Invoke(new Action(
            delegate ()
            {
                if (serialRxBuffCount == 11 && (char)serialRxBuff[0] != 'T' && (char)serialRxBuff[1] != 'T' && (char)serialRxBuff[0] != 'S')
                {
                    try
                    {
                        accel_x = (float)(((int)serialRxBuff[0]) | (((int)serialRxBuff[1]) << 8) | (((int)serialRxBuff[2] ) << 16) | (((int)serialRxBuff[3] ) << 24));
                        accel_y = (float)(((int)serialRxBuff[4]) | (((int)serialRxBuff[5]) << 8) | (((int)serialRxBuff[6] ) << 16) | (((int)serialRxBuff[7] ) << 24));
                        accel_z = (float)(((int)serialRxBuff[8]) | (((int)serialRxBuff[9]) << 8) | (((int)serialRxBuff[10]) << 16) | (((int)serialRxBuff[11]) << 24));

                        string[] values = new string[3];
                        values[0] = Convert.ToString(accel_x);
                        values[1] = Convert.ToString(accel_y);
                        values[2] = Convert.ToString(accel_z);

                        label_X.Text = values[0];
                        label_Y.Text = values[1];
                        label_Z.Text = values[2];

                        list0.Add(new PointPair(samples, Convert.ToDouble(accel_x)));
                        list1.Add(new PointPair(samples, Convert.ToDouble(accel_y)));
                        list2.Add(new PointPair(samples, Convert.ToDouble(accel_z)));

                        samples += (double)0.2;

                        zedGraphControl1.AxisChange();
                        zedGraphControl1.Invalidate();
                        zedGraphControl1.Refresh();
                    }
                    catch (Exception err)
                    {
                        MessageBox.Show(err.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                
            }));            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            cBoxComPort.Items.AddRange(serialPorts);

            myPane = zedGraphControl1.GraphPane;
            myPane.Title.Text = "İvme-Zaman Grafiği";            
            myPane.XAxis.Title.Text = "Zaman (sn)";
            myPane.YAxis.Title.Text = "İvme (m/sn^2)";                      

            ch0 = myPane.AddCurve("X Ekseni", list0, Color.Red, SymbolType.None);

            ch1 = myPane.AddCurve("Y Ekseni", list1, Color.Green, SymbolType.None);

            ch2 = myPane.AddCurve("Z Ekseni", list2, Color.Blue, SymbolType.None);         
        }

        private void btn_OpenClose_Port_Click(object sender, EventArgs e)
        {
            try
            {
                if (serialPort1.IsOpen==false)
                {
                    serialPort1.PortName = cBoxComPort.Text;
                    serialPort1.BaudRate = Convert.ToInt32(cBoxBaud.Text);
                    serialPort1.DataBits = 8;
                    serialPort1.StopBits = StopBits.One;
                    serialPort1.Parity = Parity.None;
                    serialPort1.Open();
                    labelPortStatus.Text = "AÇIK";
                    btn_OpenClose_Port.Text = "KAPAT";
                    cBoxBaud.Enabled = false;
                    cBoxComPort.Enabled = false;

                    list0.Clear();
                    list1.Clear();
                    list2.Clear();                    
                }
                else
                {
                    serialPort1.Close();
                    btn_OpenClose_Port.Text = "BAĞLAN";
                    labelPortStatus.Text = "KAPALI";
                    cBoxBaud.Enabled = true;
                    cBoxComPort.Enabled = true;

                    backgroundWorker1.CancelAsync();
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int nextByte = serialPort1.ReadByte();
            //if (nextByte >= 0) serialRxBuff[serialRxBuffCount] = (byte)nextByte;
            serialRxBuff[serialRxBuffCount] = (byte)nextByte;
            if ((char)serialRxBuff_previous == 'S' && (char)serialRxBuff[serialRxBuffCount] == 'T')
            {
                serialRxBuffCount = 0;
                serialPort1.Read(serialRxBuff, 0, 12);
                serialRxBuffCount = 11;
                if (!backgroundWorker1.IsBusy)
                {
                    backgroundWorker1.RunWorkerAsync();
                }
            }
            else
            {
                serialRxBuff_previous = serialRxBuff[serialRxBuffCount];
                serialRxBuffCount += 1;
                if (serialRxBuffCount > 11) serialRxBuffCount = 0;
            }
        }

        private void cBoxComPort_Click(object sender, EventArgs e)
        {
            cBoxComPort.Items.Clear();
            serialPorts = SerialPort.GetPortNames();
            cBoxComPort.Items.AddRange(serialPorts);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            backgroundWorker1.CancelAsync();

            if (serialPort1.IsOpen==true) serialPort1.Close();
        }
    }
}
