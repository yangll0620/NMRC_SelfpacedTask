using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO.Ports;


namespace GonoGoTask_wpfVer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public int gotrialnum, nogotrialnum;
        public string serialPortIO8_name;

        public MainWindow()
        {
            InitializeComponent();

            string[] ports = SerialPort.GetPortNames();

            foreach (string port in ports)
            {
                cboPort.Items.Add(port);
                cboPort.SelectedIndex = 0;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // get the number of go trials and nogo trials
            gotrialnum = Int32.Parse(textBox_goTrialNum.Text);
            nogotrialnum = Int32.Parse(textBox_nogoTrialNum.Text);

            // get the serial port name of DLP-IO8-G
            serialPortIO8_name = cboPort.Text;

            presentation taskpresent = new presentation(this);
            taskpresent.Show();


        }

    }
}
