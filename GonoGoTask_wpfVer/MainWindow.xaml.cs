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
using System.Reflection;

namespace GonoGoTask_wpfVer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string serialPortIO8_name;

        private string saved_folder;
        public string file_saved;
        public string audioFile_Correct, audioFile_Error;

        public bool showCloseCircle;

        private List<Window> openedWins = new List<Window>();
        presentation taskPresentWin;

        public MainWindow()
        {
            InitializeComponent();


            // Get the first not Primary Screen 
            swf.Screen showMainScreen = Utility.Detect_oneNonPrimaryScreen();
            // Show the  MainWindow on the Touch Screen
            sd.Rectangle Rect_showMainScreen = showMainScreen.WorkingArea;
            this.Top = Rect_showMainScreen.Top;
            this.Left = Rect_showMainScreen.Left;


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

            //Data binding the Color ComboBoxes
            cbo_goColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_nogoColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_BKWaitTrialColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_BKTrialColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_CorrFillColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_CorrOutlineColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_ErrorFillColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_ErrorOutlineColor.ItemsSource = typeof(Colors).GetProperties();

            // Load Default Config File
            LoadConfigFile("");

            if (textBox_NHPName.Text != "" && serialPortIO8_name != null)
            {
                btn_start.IsEnabled = true;
                btn_stop.IsEnabled = false;
            }
            else
            {
                btn_start.IsEnabled = false;
                btn_stop.IsEnabled = false;
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

            // if saved_folder not exist, created!
            if (Directory.Exists(saved_folder) == false)
            {
                System.IO.Directory.CreateDirectory(saved_folder);
            }

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
                file.WriteLine(String.Format("{0, -40}:  {1}", "Reward Interface Show Time (s)", textBox_tVisFeedback.Text));

                file.WriteLine(String.Format("{0, -40}:  {1}", "Max Reach Time (s)", textBox_MaxReachTime.Text));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Max Reaction Time (s)", textBox_MaxReactionTime.Text));

            }
        }



        private void btnShowAllTargets_Click(object sender, RoutedEventArgs e)
        {

            // Get the touch Screen, Should Set Touch Screen as the PrimaryScreen
            swf.Screen primaryScreen = swf.Screen.PrimaryScreen;

            //Show the Win_allTargets on the Touch Screen
            Window Win_allTargets = new Window();
            sd.Rectangle Rect_primaryScreen = primaryScreen.WorkingArea;
            Win_allTargets.Top = Rect_primaryScreen.Top;
            Win_allTargets.Left = Rect_primaryScreen.Left;

            Color selectedColor = (Color)(cbo_BKTrialColor.SelectedItem as PropertyInfo).GetValue(null, null);
            Win_allTargets.Background = new SolidColorBrush(selectedColor);
            Win_allTargets.Show();
            Win_allTargets.WindowState = WindowState.Maximized;
            Win_allTargets.Name = "childWin_ShowAllTargets";

            // Add a Grid
            Grid wholeGrid = new Grid();
            wholeGrid.Height = Win_allTargets.ActualHeight;
            wholeGrid.Width = Win_allTargets.ActualWidth;
            Win_allTargets.Content = wholeGrid;
            wholeGrid.UpdateLayout();


            int Diameter = Utility.in2pixal(float.Parse(textBox_objdiameter.Text));
            List<int[]> optPostions_List = new List<int[]>();

            int screenCenter_X = (int)wholeGrid.ActualWidth / 2;
            int screenCenter_Y = (int)wholeGrid.ActualHeight / 2;
            int disFromCenter = Utility.in2pixal(float.Parse(textBox_disfromcenter.Text));
            int disXFromCenter = disFromCenter;
            int disYFromCenter = disFromCenter;

            optPostions_List.Add(new int[] { screenCenter_X - disXFromCenter, screenCenter_Y }); // left position
            optPostions_List.Add(new int[] { screenCenter_X, screenCenter_Y - disYFromCenter }); // top position
            optPostions_List.Add(new int[] { screenCenter_X + disXFromCenter, screenCenter_Y }); // right position

            Color goCircleColor = (Color)(cbo_goColor.SelectedItem as PropertyInfo).GetValue(null, null);
            foreach (int[] centerPoint_Pos in optPostions_List)
            {
                Ellipse circleGo = Create_GoCircle((double)Diameter, centerPoint_Pos);
                circleGo.Fill = new SolidColorBrush(goCircleColor);
                wholeGrid.Children.Add(circleGo);
            }
            wholeGrid.UpdateLayout();

            Win_allTargets.Owner = this;
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


        private void LoadConfigFile(string configFile)
        {/*Load Config File .json 
            configFile == '': load the default Config File
            */

            // Read the Config. File and convert to JsonObject
            string jsonStr;
            if (String.IsNullOrEmpty(configFile))
            {
                var assembly = Assembly.GetExecutingAssembly();
                var defaultConfigFile = "GonoGoTask_wpfVer.Resources.ConfigFiles.defaultConfig.json";

                using (Stream stream = assembly.GetManifestResourceStream(defaultConfigFile))
                {
                    using (StreamReader r = new StreamReader(stream))
                    {
                        jsonStr = r.ReadToEnd();
                    }
                }

            }
            else
            {
                using (StreamReader r = new StreamReader(configFile))
                {
                    jsonStr = r.ReadToEnd();
                }
            }
                      
            dynamic array = JsonConvert.DeserializeObject(jsonStr);
            // Config into the Interface
            var config = array[0];
            textBox_NHPName.Text = config["NHP Name"];
            textBox_goTrialNum.Text = config["Go Trials Num"];
            textBox_nogoTrialNum.Text = config["noGo Trials Num"];
            textBox_closeMargin.Text = config["Close Margin Percentage"];
            textBox_objdiameter.Text = config["Target Diameter"];
            textBox_disfromcenter.Text = config["Target Distance from the Center"];
            textBox_tVisFeedback.Text = config["Visual Feedback Show Time"];
            textBox_MaxReachTime.Text = config["Max Reach Time"];
            textBox_MaxReactionTime.Text = config["Max Reaction Time"];

            textBox_tReady_min.Text = config["Ready Show Time Range"][0];
            textBox_tReady_max.Text = config["Ready Show Time Range"][1];
            textBox_tCue_min.Text = config["Cue Show Time Range"][0];
            textBox_tCue_max.Text = config["Cue Show Time Range"][1];
            textBox_tNogoShow_min.Text = config["Nogo Show Range Time"][0];
            textBox_tNogoShow_max.Text = config["Nogo Show Range Time"][1];

            cbo_goColor.SelectedItem = typeof(Colors).GetProperty((string)config["Colors"]["Go Fill Color"]);
            cbo_nogoColor.SelectedItem = typeof(Colors).GetProperty((string)config["Colors"]["noGo Fill Color"]);
            cbo_BKWaitTrialColor.SelectedItem = typeof(Colors).GetProperty((string)config["Colors"]["Wait Trial Start Background"]);
            cbo_BKTrialColor.SelectedItem = typeof(Colors).GetProperty((string)config["Colors"]["Trial Background"]);
            cbo_CorrFillColor.SelectedItem = typeof(Colors).GetProperty((string)config["Colors"]["Correct Fill"]);
            cbo_CorrOutlineColor.SelectedItem = typeof(Colors).GetProperty((string)config["Colors"]["Correct Outline"]);
            cbo_ErrorFillColor.SelectedItem = typeof(Colors).GetProperty((string)config["Colors"]["Error Fill"]);
            cbo_ErrorOutlineColor.SelectedItem = typeof(Colors).GetProperty((string)config["Colors"]["Error Outline"]);

            audioFile_Correct = config["audioFile_Correct"];
            audioFile_Error = config["audioFile_Error"];
            saved_folder = config["saved_folder"];
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
                string configFile = openFileDlg.FileName;
                LoadConfigFile(configFile);
            }
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            saveInputParameters();

            // btn_Start and btn_stop
            btn_start.IsEnabled = false;
            btn_stop.IsEnabled = true;

            // Get the touch Screen
            swf.Screen PrimaryScreen = swf.Screen.PrimaryScreen;

            // Show the taskpresent Window on the Touch Screen
            taskPresentWin = new presentation(this);
            sd.Rectangle Rect_touchScreen = PrimaryScreen.WorkingArea;
            taskPresentWin.Top = Rect_touchScreen.Top;
            taskPresentWin.Left = Rect_touchScreen.Left;
            taskPresentWin.Name = "childWin_Task";
            taskPresentWin.Owner = this;

            taskPresentWin.Show();
            taskPresentWin.Present_Start();
        }

        private void Btn_stop_Click(object sender, RoutedEventArgs e)
        {
            if (taskPresentWin != null)
            {
                taskPresentWin.Present_Stop();
                taskPresentWin.Close();
            }

            // btn_Start and btn_stop
            btn_start.IsEnabled = true;
            btn_stop.IsEnabled = false;
        }

        private void MenuItem_showCloseCircle(object sender, RoutedEventArgs e)
        {
            showCloseCircle = true;
        }

        private void MenuItem_noShowCloseCircle(object sender, RoutedEventArgs e)
        {
            showCloseCircle = false;
        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
         
        }

        private void MenuItem_SetupColors(object sender, RoutedEventArgs e)
        {
            SetupColorsWin Win_SetupColors = new SetupColorsWin();
           

            // Get the first not Primary Screen 
            swf.Screen showMainScreen = Utility.Detect_oneNonPrimaryScreen();
            // Show the  MainWindow on the Touch Screen
            sd.Rectangle Rect_showMainScreen = showMainScreen.WorkingArea;
            Win_SetupColors.Top = Rect_showMainScreen.Top;
            Win_SetupColors.Left = Rect_showMainScreen.Left;

            Win_SetupColors.Show();


        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (taskPresentWin != null)
            {
                taskPresentWin.Present_Stop();
            }
        }
    }
}
