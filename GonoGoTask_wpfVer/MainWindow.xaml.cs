using System;
using System.Collections.Generic;
using System.Windows.Shapes;
using System.Windows;
using System.IO.Ports;
using System.Windows.Controls;
using System.Threading;
using System.Windows.Media;
using System.IO;
using swf = System.Windows.Forms;
using sd = System.Drawing;
using Newtonsoft.Json;

namespace GonoGoTask_wpfVer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string serialPortIO8_name;

        private string saved_folder = @"F:\yang7003@umn\NMRC_umn\Projects\GoNogoTaskDev\GononGoTask_wpf\savefolder\";
        public string file_saved;


        public MainWindow()
        {
            InitializeComponent();


            // Get the first notTouch Screen */
            swf.Screen showMainScreen = Utility.Detect_notTouchScreen();


            /* Show the  MainWindow on the Touch Screen*/
            sd.Rectangle Rect_touchScreen = showMainScreen.WorkingArea;
            this.Top = Rect_touchScreen.Top;
            this.Left = Rect_touchScreen.Left;


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

            //WindowState = WindowState.Minimized;
            btn_start.IsEnabled = false;

            // Get the touch Screen
            swf.Screen touchScreen = Utility.Detect_TouchScreen();

            // Show the taskpresent Window on the Touch Screen
            presentation taskpresent = new presentation(this);
            sd.Rectangle Rect_touchScreen = touchScreen.WorkingArea;
            taskpresent.Top = Rect_touchScreen.Top;
            taskpresent.Left = Rect_touchScreen.Left;

            taskpresent.Show();
            taskpresent.StartExp();
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

                file.WriteLine(String.Format("{0, -40}:  {1}", "Close Margin (%)", textBox_closeMargin.Text));

                file.WriteLine(String.Format("{0, -40}:  {1}", "Total Number of Go Trials", textBox_goTrialNum.Text));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Total Number of Nogo Trials", textBox_nogoTrialNum.Text));

                file.WriteLine(String.Format("{0, -40}:  {1}", "Go Target Color", cbo_goColor.Text));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Nogo Target Color", cbo_nogoColor.Text));

                file.WriteLine(String.Format("{0, -40}:  {1}", "Target Diameter (inch)", textBox_objdiameter.Text));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Target Distance from the Center (inch)", textBox_disfromcenter.Text));


                file.WriteLine(String.Format("{0, -40}:  [{1} {2}]", "Ready Interface Show Time Range (s)", textBox_tReady_min.Text, textBox_tReady_max.Text));
                file.WriteLine(String.Format("{0, -40}:  [{1} {2}]", "Cue Interface Show Time Range (s)", textBox_tCue_min.Text, textBox_tCue_max.Text));
                file.WriteLine(String.Format("{0, -40}:  [{1} {2}]", "Nogo Interface Show Range Time (s)", textBox_tNogoShow_min.Text, textBox_tNogoShow_max.Text));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Reward Interface Show Time (s)", textBox_tRewardShow.Text));

                file.WriteLine(String.Format("{0, -40}:  {1}", "Max Reach Time (s)", textBox_MaxReachTime.Text));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Max Reaction Time (s)", textBox_MaxReactionTime.Text));

            }
        }



        private void btnShowAllTargets_Click(object sender, RoutedEventArgs e)
        {
            // Get the touch Screen
            swf.Screen touchScreen = Utility.Detect_TouchScreen();

            /* Show the Win_allTargets on the Touch Screen*/
            Window Win_allTargets = new Window();
            sd.Rectangle Rect_touchScreen = touchScreen.WorkingArea;
            Win_allTargets.Top = Rect_touchScreen.Top;
            Win_allTargets.Left = Rect_touchScreen.Left;

            Win_allTargets.Background = new SolidColorBrush(Colors.Black);
            Win_allTargets.Show();
            Win_allTargets.WindowState = WindowState.Maximized;

            // Add a Grid
            Grid wholeGrid = new Grid();
            wholeGrid.Height = Win_allTargets.ActualHeight;
            wholeGrid.Width = Win_allTargets.ActualWidth;
            Win_allTargets.Content = wholeGrid;
            wholeGrid.UpdateLayout();


            int Diameter = Convert2Pixal.in2pixal(float.Parse(textBox_objdiameter.Text));
            List<int[]> optPostions_List = new List<int[]>();

            int screenCenter_X = (int)wholeGrid.ActualWidth / 2;
            int screenCenter_Y = (int)wholeGrid.ActualHeight / 2;
            int disFromCenter = Convert2Pixal.in2pixal(float.Parse(textBox_disfromcenter.Text));
            int disXFromCenter = disFromCenter;
            int disYFromCenter = disFromCenter;

            optPostions_List.Add(new int[] { screenCenter_X - disXFromCenter, screenCenter_Y }); // left position
            optPostions_List.Add(new int[] { screenCenter_X, screenCenter_Y - disYFromCenter }); // top position
            optPostions_List.Add(new int[] { screenCenter_X + disXFromCenter, screenCenter_Y }); // right position


            foreach (int[] centerPoint_Pos in optPostions_List)
            {
                Ellipse circleGo = Create_GoCircle((double) Diameter, centerPoint_Pos);
                wholeGrid.Children.Add(circleGo);
            }
            wholeGrid.UpdateLayout();

            
        }

        private Ellipse Create_GoCircle(double Diameter, int[] centerPoint_Pos)
        {/*
            Create the go circle

            */

            // Create an Ellipse  
            Ellipse circleGo = new System.Windows.Shapes.Ellipse();

            // set the size, position of circleGo
            circleGo.Height = Diameter;
            circleGo.Width = Diameter;
            circleGo.VerticalAlignment = VerticalAlignment.Top;
            circleGo.HorizontalAlignment = HorizontalAlignment.Left;
            circleGo.Fill = new SolidColorBrush(Colors.Blue);

            double left = centerPoint_Pos[0] - circleGo.Width / 2;
            double top = centerPoint_Pos[1] - circleGo.Height / 2;
            circleGo.Margin = new Thickness(left, top, 0, 0);

            return circleGo;
        }

        private void btnLoadConf_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            openFileDlg.DefaultExt = ".json";
            openFileDlg.Filter = "Json Files|*.json";

            Nullable<bool> result = openFileDlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = openFileDlg.FileName;


                JsonTextReader reader = new JsonTextReader(new StringReader(json));
                while (reader.Read())
                {
                    if (reader.Value != null)
                    {
                        Console.WriteLine("Token: {0}, Value: {1}", reader.TokenType, reader.Value);
                    }
                    else
                    {
                        Console.WriteLine("Token: {0}", reader.TokenType);
                    }
                }
            }

           
        }
        

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }



    }


   
}
