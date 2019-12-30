using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO.Ports;
using System.Windows.Controls;
using System.Threading;
using System.Windows.Media;

namespace GonoGoTask_wpfVer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public int gotrialnum, nogotrialnum;
        public string serialPortIO8_name;

        public int objdiameter;
        public int rightMargin, leftMargin, topMargin;

        public MainWindow()
        {
            InitializeComponent();

            // locate serial Port Name
            locate_serialPortIO8();
            if (serialPortIO8_name == null)
            {
                btn_start.IsEnabled = false;
                btn_comReconnect.Visibility = Visibility.Visible;
                btn_comReconnect.IsEnabled = true;
                textblock_comState.Visibility = Visibility.Visible;

                run_comState.Text = "Can't Find the COM Port for DLP-IO8!";
                run_comState.Background = new SolidColorBrush(Colors.Orange);
                run_comState.Foreground = new SolidColorBrush(Colors.Red);
                run_instruction.Text = "Please connect it correctly and reCheck!";
                run_instruction.Background = new SolidColorBrush(Colors.Orange);
                run_instruction.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
                btn_start.IsEnabled = true;
                btn_comReconnect.Visibility = Visibility.Hidden;
                btn_comReconnect.IsEnabled = false;
                textblock_comState.Visibility = Visibility.Hidden;
            }
        }

        private bool locate_serialPortIO8()
        { /* 
            Locate the correct COM port used for communicating with the DLP-IO8 

            Outputs:
                update serialPortIO8_name if located

            Returns:
                true, if located; otherwise, false
            
             */

            string[] portNames = SerialPort.GetPortNames();
            foreach (string portName in portNames)
            {
                SerialPort serialPort = new SerialPort();
                try
                {
                    serialPort.PortName = portName;
                    serialPort.BaudRate = 115200;
                    serialPort.Open();

                    for (int i = 0; i < 5; i++)
                    {
                        // channel 1 Ping command of the DLP-IO8
                        serialPort.WriteLine("Z");
                        // Read exist Analog in from serialPort
                        string str_Read = serialPort.ReadExisting();

                        if (str_Read.Contains("V"))
                        {// e.g str_Read = "5.00V"
                            serialPortIO8_name = portName;
                            serialPort.Close();
                            return true;
                        }
                        Thread.Sleep(30);
                    }

                    serialPort.Close();
                }
                catch (Exception ex)
                {
                    if (serialPort.IsOpen)
                        serialPort.Close();
                    MessageBox.Show(ex.Message, "Error Message", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                
            }
            return false;
        }
           

        private void Btn_comReconnect_Click(object sender, RoutedEventArgs e)
        {

            locate_serialPortIO8();
            if (serialPortIO8_name == null)
            {
                run_comState.Text = "Can't Find the COM Port for DLP-IO8!";
                run_comState.Background = new SolidColorBrush(Colors.Orange);
                run_comState.Foreground = new SolidColorBrush(Colors.Red);
                run_instruction.Text = "Please connect it correctly and reCheck!";
                run_instruction.Background = new SolidColorBrush(Colors.Orange);
                run_instruction.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
                btn_start.IsEnabled = true;
                btn_comReconnect.Visibility = Visibility.Hidden;
                btn_comReconnect.IsEnabled = false;
                run_comState.Text = "Found the COM Port for DLP-IO8!";
                run_comState.Background = new SolidColorBrush(Colors.White);
                run_comState.Foreground = new SolidColorBrush(Colors.Green);
                run_instruction.Text = "Can start trials now";
                run_instruction.Background = new SolidColorBrush(Colors.White);
                run_instruction.Foreground = new SolidColorBrush(Colors.Green);
            }
        }

        private void TextBox_goTrialNum_TextChanged(object sender, TextChangedEventArgs e)
        {
            gotrialnum = Int32.Parse(textBox_goTrialNum.Text);
        }

        private void TextBox_nogoTrialNum_TextChanged(object sender, TextChangedEventArgs e)
        {
            nogotrialnum = Int32.Parse(textBox_nogoTrialNum.Text);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
                      
            presentation taskpresent = new presentation(this);
            taskpresent.Show();
        }

    }
}
