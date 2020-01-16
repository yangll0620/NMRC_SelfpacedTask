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
using System.IO;

namespace GonoGoTask_wpfVer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string serialPortIO8_name;

        public int objdiameter;
        public int rightMargin, leftMargin, topMargin;


        private string saved_folder = @"F:\yang7003@umn\NMRC_umn\Projects\GoNogoTaskDev\GononGoTask_wpf\";
        public string file_saved;

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
                btn_comReconnect.Visibility = Visibility.Hidden;
                btn_comReconnect.IsEnabled = false;
                textblock_comState.Visibility = Visibility.Hidden;
            }

            if (textBox_NHPName.Text != "" && serialPortIO8_name != null)
            {
                btn_start.IsEnabled = true;
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
                btn_comReconnect.Visibility = Visibility.Hidden;
                btn_comReconnect.IsEnabled = false;
                run_comState.Text = "Found the COM Port for DLP-IO8!";
                run_comState.Background = new SolidColorBrush(Colors.White);
                run_comState.Foreground = new SolidColorBrush(Colors.Green);
                run_instruction.Text = "Can start trials now";
                run_instruction.Background = new SolidColorBrush(Colors.White);
                run_instruction.Foreground = new SolidColorBrush(Colors.Green);
            }


            if (textBox_NHPName.Text != "" && serialPortIO8_name != null)
            {
                btn_start.IsEnabled = true;
            }

        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            saveInputParameters();
            presentation taskpresent = new presentation(this);
            taskpresent.Show();
        }

        private void TextBox_NHPName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(textBox_NHPName.Text != "" && serialPortIO8_name != null)
            {
                btn_start.IsEnabled = true;
            }
        }

        private void saveInputParameters()
        {
            DateTime time_now = DateTime.Now;


            file_saved = saved_folder + textBox_NHPName.Text + time_now.ToString("-yyyyMMdd-HHmmss") + ".txt";
            using (StreamWriter file = new StreamWriter(file_saved))
            {
                file.WriteLine("Date: " + time_now.ToString("MM/dd/yyyy HH:mm:ss") + "\t\tNHP Name: " + textBox_NHPName.Text);
                file.WriteLine("\n");


                file.WriteLine("Input Parameters:");

                file.WriteLine(String.Format("{0, -40}:  {1}", "Total Number of Go Trials", textBox_goTrialNum.Text));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Total Number of Nogo Trials", textBox_nogoTrialNum.Text));

                file.WriteLine(String.Format("{0, -40}:  {1}", "Go Target Color", cbo_goColor.Text));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Nogo Target Color", cbo_nogoColor.Text));

                file.WriteLine(String.Format("{0, -40}:  {1}", "Target Diameter (inch)", textBox_objdiameter.Text));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Target Distance from the Center (inch)", textBox_disfromcenter.Text));

                file.WriteLine(String.Format("{0, -40}:  [{1} {2}]", "Ready Interface Show Time Range (s)", textBox_tReady_min.Text, textBox_tReady_max.Text));
                file.WriteLine(String.Format("{0, -40}:  [{1} {2}]", "Cue Interface Show Time Range (s)", textBox_tCue_min.Text, textBox_tCue_max.Text));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Go/Nogo Interface Max Show Time (s)", textBox_tmaxGoNogoShow.Text));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Reward Interface Show Time (s)", textBox_tRewardShow.Text));

                file.WriteLine(String.Format("{0, -40}:  {1}", "Max Reach Time (s)", textBox_MaxReachTime.Text));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Max Reaction Time (s)", textBox_MaxReactionTime.Text));

            }
        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            saveInputParameters();
        }



    }


   
}
