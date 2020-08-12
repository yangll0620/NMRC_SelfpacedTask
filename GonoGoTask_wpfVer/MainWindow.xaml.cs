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

        // Strings stoing the Colors
        public string goColorStr, nogoColorStr, cueColorStr;
        public string BKWaitTrialColorStr, BKTrialColorStr;
        public string CorrFillColorStr, CorrOutlineColorStr, ErrorFillColorStr, ErrorOutlineColorStr;

        // Time Related Variables
        public float[] tRange_ReadyTime, tRange_CueTime, tRange_NogoShowTime;
        public float tMax_ReactionTimeS, tMax_ReachTimeS, t_VisfeedbackShow;
        public float t_JuicerFullGivenS, t_JuicerCloseGivenS;

        // Target Related Variables
        public float targetDiameterInch, targetDisFromCenterInch, closeMarginPercentage;


        // Touch Screen Rectangle
        sd.Rectangle Rect_touchScreen;

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
            serialPortIO8_name = SerialPortIO8.Locate_serialPortIO8();
            if (String.Equals(serialPortIO8_name,""))
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


            // Get the touch Screen Rectangle
            swf.Screen PrimaryScreen = swf.Screen.PrimaryScreen;
            Rect_touchScreen = PrimaryScreen.WorkingArea;
        }

        private void Btn_comReconnect_Click(object sender, RoutedEventArgs e)
        {
            serialPortIO8_name = SerialPortIO8.Locate_serialPortIO8();
            if (String.Equals(serialPortIO8_name, ""))
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

        private void btnTestTouchpadJuicer_Click(object sender, RoutedEventArgs e)
        {
            TestStartpadJuicerWin Win_TestStartpadJuicer = new TestStartpadJuicerWin(this);

            // Set Owner
            Win_TestStartpadJuicer.Owner = this;

            // Get the first not Primary Screen 
            swf.Screen showMainScreen = Utility.Detect_oneNonPrimaryScreen();
            // Show the  MainWindow on the Touch Screen
            sd.Rectangle Rect_showMainScreen = showMainScreen.WorkingArea;
            Win_TestStartpadJuicer.Top = Rect_showMainScreen.Top;
            Win_TestStartpadJuicer.Left = Rect_showMainScreen.Left;

            Win_TestStartpadJuicer.Show();
        }

        private void saveInputParameters()
        {
            DateTime time_now = DateTime.Now;

            // if saved_folder not exist, created!
            if (Directory.Exists(saved_folder) == false)
            {
                System.IO.Directory.CreateDirectory(saved_folder);
            }

            string filename_saved = textBox_NHPName.Text + time_now.ToString("-yyyyMMdd-HHmmss") + ".txt";
            file_saved = System.IO.Path.Combine(saved_folder, filename_saved);

            using (StreamWriter file = new StreamWriter(file_saved))
            {
                file.WriteLine("Date: " + time_now.ToString("MM/dd/yyyy HH:mm:ss") + "\t\tNHP Name: " + textBox_NHPName.Text);
                file.WriteLine("\n");


                file.WriteLine("Input Parameters:");

                file.WriteLine(String.Format("{0, -40}:  {1}", "Close Margin (%)", closeMarginPercentage.ToString()));

                file.WriteLine(String.Format("{0, -40}:  {1}", "Total Number of Go Trials", textBox_goTrialNum.Text));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Total Number of Nogo Trials", textBox_nogoTrialNum.Text));

                file.WriteLine(String.Format("{0, -40}:  {1}", "Go Target Color", goColorStr));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Nogo Target Color", nogoColorStr));

                file.WriteLine(String.Format("{0, -40}:  {1}", "Target Diameter (inch)", targetDiameterInch.ToString()));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Target Distance from the Center (inch)", targetDisFromCenterInch.ToString()));


                file.WriteLine(String.Format("{0, -40}:  [{1} {2}]", "Ready Interface Show Time Range (s)", tRange_ReadyTime[0].ToString(), tRange_ReadyTime[1].ToString()));
                file.WriteLine(String.Format("{0, -40}:  [{1} {2}]", "Cue Interface Show Time Range (s)", tRange_CueTime[0].ToString(), tRange_CueTime[1].ToString()));
                file.WriteLine(String.Format("{0, -40}:  [{1} {2}]", "Nogo Interface Show Range Time (s)", tRange_NogoShowTime[0].ToString(), tRange_NogoShowTime[1].ToString()));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Visual Feedback Time (s)", t_VisfeedbackShow.ToString()));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Juicer Feedback Time (s)", t_JuicerCloseGivenS.ToString()));

                file.WriteLine(String.Format("{0, -40}:  {1}", "Max Reach Time (s)", tMax_ReachTimeS.ToString()));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Max Reaction Time (s)", tMax_ReactionTimeS.ToString()));


                file.WriteLine(String.Format("{0, -40}:  {1}", "Screen Resolution(pixal)", Rect_touchScreen.Width.ToString() + "x" + Rect_touchScreen.Height.ToString()));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Inch to Pixal Ratio", Utility.ratioIn2Pixal.ToString()));

            }
        }


        private void MenuItem_SetupTime(object sender, RoutedEventArgs e)
        {
            SetupTimeWin Win_SetupTime = new SetupTimeWin(this);

            // Get the first not Primary Screen 
            swf.Screen showMainScreen = Utility.Detect_oneNonPrimaryScreen();
            // Show the  MainWindow on the Touch Screen
            sd.Rectangle Rect_showMainScreen = showMainScreen.WorkingArea;
            Win_SetupTime.Top = Rect_showMainScreen.Top;
            Win_SetupTime.Left = Rect_showMainScreen.Left;

            // Set Owner
            Win_SetupTime.Owner = this;

            Win_SetupTime.Show();
        }

        private void MenuItem_SetupColors(object sender, RoutedEventArgs e)
        {
            SetupColorsWin Win_SetupColors = new SetupColorsWin(this);


            // Get the first not Primary Screen 
            swf.Screen showMainScreen = Utility.Detect_oneNonPrimaryScreen();
            // Show the  MainWindow on the Touch Screen
            sd.Rectangle Rect_showMainScreen = showMainScreen.WorkingArea;
            Win_SetupColors.Top = Rect_showMainScreen.Top;
            Win_SetupColors.Left = Rect_showMainScreen.Left;

            // Set Owner
            Win_SetupColors.Owner = this;

            Win_SetupColors.Show();


        }

        private void MenuItem_SetupTarget(object sender, RoutedEventArgs e)
        {
            SetupTargetsWin Win_SetupTarget = new SetupTargetsWin(this);


            // Get the first not Primary Screen 
            swf.Screen showMainScreen = Utility.Detect_oneNonPrimaryScreen();
            // Show the  MainWindow on the Touch Screen
            sd.Rectangle Rect_showMainScreen = showMainScreen.WorkingArea;
            Win_SetupTarget.Top = Rect_showMainScreen.Top;
            Win_SetupTarget.Left = Rect_showMainScreen.Left;

            // Set Owner
            Win_SetupTarget.Owner = this;

            Win_SetupTarget.Show();
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

            Color selectedColor = (Color)(typeof(Colors).GetProperty(BKTrialColorStr) as PropertyInfo).GetValue(null, null);
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


            int Diameter = Utility.in2pixal(targetDiameterInch);
            List<int[]> optPostions_List = new List<int[]>();

            int screenCenter_X = (int)wholeGrid.ActualWidth / 2;
            int screenCenter_Y = (int)wholeGrid.ActualHeight / 2;
            int disFromCenter = Utility.in2pixal(targetDisFromCenterInch);
            int disXFromCenter = disFromCenter;
            int disYFromCenter = disFromCenter;

            optPostions_List.Add(new int[] { screenCenter_X - disXFromCenter, screenCenter_Y }); // left position
            optPostions_List.Add(new int[] { screenCenter_X, screenCenter_Y - disYFromCenter }); // top position
            optPostions_List.Add(new int[] { screenCenter_X + disXFromCenter, screenCenter_Y }); // right position

            Color goCircleColor = (Color)(typeof(Colors).GetProperty(goColorStr) as PropertyInfo).GetValue(null, null); ;
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
            
            /* ---- Config into the Interface ---- */
            var config = array[0];
            textBox_NHPName.Text = config["NHP Name"];
            textBox_goTrialNum.Text = config["Go Trials Num"];
            textBox_nogoTrialNum.Text = config["noGo Trials Num"];


            // Juicer Given Time
            var configJuicer = config["JuicerGivenTime"];
            t_JuicerFullGivenS = float.Parse((string)configJuicer["Correct"]);
            t_JuicerCloseGivenS = float.Parse((string)configJuicer["Close"]);


            // Times Sections
            var configTime = config["Times"];
            tRange_ReadyTime = new float[] {float.Parse((string)configTime["Ready Show Time Range"][0]), float.Parse((string)configTime["Ready Show Time Range"][1])};
            tRange_CueTime = new float[] {float.Parse((string)configTime["Cue Show Time Range"][0]), float.Parse((string)configTime["Cue Show Time Range"][1])};
            tRange_NogoShowTime = new float[] { float.Parse((string)configTime["Nogo Show Range Time"][0]), float.Parse((string)configTime["Nogo Show Range Time"][1]) };
            tMax_ReactionTimeS = float.Parse((string)configTime["Max Reach Time"]);
            tMax_ReachTimeS = float.Parse((string)configTime["Max Reaction Time"]);
            t_VisfeedbackShow = float.Parse((string)configTime["Visual Feedback Show Time"]);

            // Color Sections
            var configColors = config["Colors"];
            goColorStr = configColors["Go Fill Color"];
            nogoColorStr = configColors["noGo Fill Color"];
            cueColorStr = configColors["Cue Crossing Color"];
            BKWaitTrialColorStr = configColors["Wait Trial Start Background"];
            BKTrialColorStr = configColors["Trial Background"];
            CorrFillColorStr = configColors["Correct Fill"];
            CorrOutlineColorStr = configColors["Correct Outline"];
            ErrorFillColorStr = configColors["Error Fill"];
            ErrorOutlineColorStr = configColors["Error Outline"];


            // Target Sections
            var configTarget = config["Target"];      
            targetDiameterInch = float.Parse((string)configTarget["Target Diameter"]);
            targetDisFromCenterInch = float.Parse((string)configTarget["Target Distance from Center"]);
            closeMarginPercentage = float.Parse((string)configTarget["Close Margin Percentage"]);


            audioFile_Correct = config["audioFile_Correct"];
            audioFile_Error = config["audioFile_Error"];
            saved_folder = config["saved_folder"];
            if (String.Compare(saved_folder, "default", true) == 0)
            {
                saved_folder = @"C:\\GonoGoSave";
            }
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
            // save all the Input parameters
            saveInputParameters();

            // btn_Start and btn_stop
            btn_start.IsEnabled = false;
            btn_stop.IsEnabled = true;


            // Show the taskpresent Window on the Touch Screen
            taskPresentWin = new presentation(this);
            taskPresentWin.Top = Rect_touchScreen.Top;
            taskPresentWin.Left = Rect_touchScreen.Left;

            taskPresentWin.Name = "childWin_Task";
            taskPresentWin.Owner = this;


            

            // Start the Task
            taskPresentWin.Show();
            taskPresentWin.Present_Start();
        }

        private void Btn_stop_Click(object sender, RoutedEventArgs e)
        {
            if (taskPresentWin != null)
            {
                taskPresentWin.Present_Stop();
                taskPresentWin.Close();
                taskPresentWin = null;
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
            saveInputParameters();
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
