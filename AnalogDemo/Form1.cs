using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.Net;
using System.Windows.Forms.DataVisualization.Charting;

namespace AnalogDemo
{
    public partial class Form1 : Form
    {

        private const int CHART_SECONDS = 300;
        
        private SerialPort serialPort;
        private Boolean connected;
        private WebClient webClient;
        private Series mySerie;

        public delegate void AddDataDelegate();
        public AddDataDelegate addDataDel;

        public Form1()
        {
            InitializeComponent();
            
            connected = false;
            webClient = new WebClient();
            addDataDel += new AddDataDelegate(AddData);

            DateTime minValue = DateTime.Now;
            DateTime maxValue = minValue.AddSeconds(CHART_SECONDS);
            chart1.ChartAreas[0].AxisX.Minimum = minValue.ToOADate();
            chart1.ChartAreas[0].AxisX.Maximum = maxValue.ToOADate();
            chart1.ChartAreas[0].AxisX.LabelStyle.Format = "T";
            chart1.ChartAreas[0].AxisX.IntervalType = DateTimeIntervalType.Minutes;
            chart1.ChartAreas[0].AxisX.Interval = 1;
            chart1.ChartAreas[0].AxisX.MajorGrid.LineColor = Color.DarkGray;
            chart1.ChartAreas[0].AxisX.MinorGrid.IntervalType = DateTimeIntervalType.Seconds;
            chart1.ChartAreas[0].AxisX.MinorGrid.Interval = 10;
            chart1.ChartAreas[0].AxisX.MinorGrid.LineColor = Color.LightGray;
            chart1.ChartAreas[0].AxisX.MinorGrid.Enabled = true;

            chart1.ChartAreas[0].AxisY.Minimum = 0;
            chart1.ChartAreas[0].AxisY.Maximum = 1023;
            chart1.ChartAreas[0].AxisY.IntervalType = DateTimeIntervalType.Number;
            chart1.ChartAreas[0].AxisY.Interval = 200;
            chart1.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.DarkGray;
            chart1.ChartAreas[0].AxisY.MinorGrid.IntervalType = DateTimeIntervalType.Number;
            chart1.ChartAreas[0].AxisY.MinorGrid.Interval = 50;
            chart1.ChartAreas[0].AxisY.MinorGrid.LineColor = Color.LightGray;
            chart1.ChartAreas[0].AxisY.MinorGrid.Enabled = true;

            chart1.Series.Clear();
            mySerie = new Series("AnalogInput");
            mySerie.ChartType = SeriesChartType.FastLine;
            mySerie.BorderWidth = 1;
            mySerie.Color = Color.Red;
            mySerie.XValueType = ChartValueType.DateTime;
            chart1.Series.Add(mySerie);

            DateTime timeStamp = DateTime.Now;
            chart1.Series[0].Points.AddXY(timeStamp, 0);
        }

        private void Form1_Load(object sender, EventArgs e)
        {         
            string[] serialPortNames = SerialPort.GetPortNames();
            foreach(string serialPortName in serialPortNames)
                cbSerialPort.Items.Add(serialPortName);
            if (serialPortNames.Length > 0)
                cbSerialPort.SelectedIndex = 0;
        }

        private void btConnect_Click(object sender, EventArgs e)
        {
            if (!connected)
            {
                string serialPortName = cbSerialPort.SelectedItem.ToString();
                serialPort = new SerialPort(serialPortName, 9600);
                serialPort.Open();
                serialPort.DataReceived += new SerialDataReceivedEventHandler(serialPort_DataReceived);
                btConnect.Text = "Disconnect";
                cbSerialPort.Enabled = false;
                connected = true;
            }
            else
            {
                serialPort.Close();
                btConnect.Text = "Connect";
                cbSerialPort.Enabled = true;
                connected = false;
            }
        }

        void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            chart1.Invoke(addDataDel);
        }

        public void AddData()
        {
            DateTime timeStamp = DateTime.Now;
            string stringValue = serialPort.ReadLine();
            chart1.Series[0].Points.AddXY(timeStamp, Double.Parse(stringValue));

            double removeBefore = timeStamp.AddSeconds(-CHART_SECONDS).ToOADate();
            while (mySerie.Points[0].XValue < removeBefore)
            {
                mySerie.Points.RemoveAt(0);
            }
            chart1.ChartAreas[0].AxisX.Minimum = mySerie.Points[0].XValue;
            chart1.ChartAreas[0].AxisX.Maximum = DateTime.FromOADate(mySerie.Points[0].XValue).AddSeconds(CHART_SECONDS).ToOADate();
            chart1.Invalidate();

            if (cbSendData.Checked)
            {
                if (tbAPIKey.Text.Trim().Equals("") || tbFeedId.Text.Trim().Equals("") ||
                    tbDatastreamId.Text.Trim().Equals(""))
                {
                    MessageBox.Show("Missing Pachube informations", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    cbSendData.Checked = false;
                }

                string updatePath = "http://api.pachube.com/v2/feeds/" + tbFeedId.Text + "/datastreams/" + tbDatastreamId.Text + ".csv";
                webClient.Headers.Set("X-PachubeApiKey", tbAPIKey.Text);
                try
                {
                    webClient.UploadString(updatePath, "PUT", stringValue);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to send data to Pachube:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    cbSendData.Checked = false;

                }
            }
        }
    }
}
