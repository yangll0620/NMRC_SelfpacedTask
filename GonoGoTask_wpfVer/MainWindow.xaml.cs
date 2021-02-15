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

namespace COTTask_wpf
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
        public string BKWaitTrialColorStr, BKReadyColorStr, BKTargetShownColorStr;
        public string CorrFillColorStr, CorrOutlineColorStr, ErrorFillColorStr, ErrorOutlineColorStr;

        // Time Related Variables
        public float[] tRange_ReadyTime, tRange_CueTime, tRange_NogoShowTime;
        public float tMax_ReactionTimeS, tMax_ReachTimeS, t_VisfeedbackShow;
        public float t_JuicerFullGivenS, t_JuicerCloseGivenS;

        
        // Target Related Variables
        public float closeMarginPercentage;
        public int targetNoOfPositions;
        public float targetDiaCM;
        public int targetDiaPixal;
        public List<int[]> optPostions_OCenter_List;



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

            // Get the touch Screen Rectangle
            Rect_touchScreen = Utility.Detect_PrimaryScreen_Rect(); 


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
            targetDiaPixal = Utility.cm2pixal(targetDiaCM);

            // Generate optional positions
            optPostions_OCenter_List = Utility.GenPositions(targetNoOfPositions, targetDiaPixal, Rect_touchScreen); 


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
                Directory.CreateDirectory(saved_folder);
            }

            string filename_saved = textBox_NHPName.Text + time_now.ToString("-yyyyMMdd-HHmmss") + ".txt";
            file_saved = System.IO.Path.Combine(saved_folder, filename_saved);

            using (StreamWriter file = new StreamWriter(file_saved))
            {
                file.WriteLine("Date: " + time_now.ToString("MM/dd/yyyy hh:mm:ss tt"));
                file.WriteLine("NHP Name: " + textBox_NHPName.Text);
                file.WriteLine("\n");


                file.WriteLine("Input Parameters:");

                file.WriteLine(String.Format("{0, -40}:  {1}", "Close Margin (%)", closeMarginPercentage.ToString()));

                file.WriteLine(String.Format("{0, -40}:  {1}", "Required Number of Blocks", textBox_requiredBlocks.Text));

                file.WriteLine(String.Format("{0, -40}:  {1}", "Go Target Color", goColorStr));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Nogo Target Color", nogoColorStr));

                file.WriteLine(String.Format("{0, -40}:  {1}", "Target Diameter (CM)", targetDiaCM.ToString()));


                file.WriteLine(String.Format("{0, -40}:  [{1} {2}]", "Ready Interface Show Time Range (s)", tRange_ReadyTime[0].ToString(), tRange_ReadyTime[1].ToString()));
                file.WriteLine(String.Format("{0, -40}:  [{1} {2}]", "Cue Interface Show Time Range (s)", tRange_CueTime[0].ToString(), tRange_CueTime[1].ToString()));
                file.WriteLine(String.Format("{0, -40}:  [{1} {2}]", "Nogo Interface Show Range Time (s)", tRange_NogoShowTime[0].ToString(), tRange_NogoShowTime[1].ToString()));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Visual Feedback Time (s)", t_VisfeedbackShow.ToString()));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Juicer Feedback Time (s)", t_JuicerCloseGivenS.ToString()));

                file.WriteLine(String.Format("{0, -40}:  {1}", "Max Reach Time (s)", tMax_ReachTimeS.ToString()));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Max Reaction Time (s)", tMax_ReactionTimeS.ToString()));

                file.WriteLine("\n");
                file.WriteLine(String.Format("{0, -40}:  {1}", "Unit of X Y Position", "CM"));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Unit of TimePoint/Time", "s"));


                file.WriteLine(String.Format("{0, -40}:  {1}", "Screen Resolution(pixal)", Rect_touchScreen.Width.ToString() + "x" + Rect_touchScreen.Height.ToString()));
                file.WriteLine(String.Format("{0, -40}:  {1}", "CM to Pixal Ratio", Utility.ratioCM2Pixal.ToString()));


                // Store all the optional positions
                file.WriteLine("\n");
                for (int i = 0; i < optPostions_OCenter_List.Count; i++)
                {
                    int[] position = optPostions_OCenter_List[i];
                    file.WriteLine(String.Format("{0, -40}:{1}, {2}", "Optional Postion " + i.ToString(), position[0], position[1]));
                }

            }
        }


        private void MenuItem_NewWindow(object sender, RoutedEventArgs e)
        {
            Window1 testWind = new Window1(this);
            testWind.Show();

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
                var defaultConfigFile = "COTTask_wpf.Resources.ConfigFiles.defaultConfig.json";

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
            textBox_requiredBlocks.Text = config["Required Blocks"];


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
            BKWaitTrialColorStr = configColors["Wait Start Background"];
            BKReadyColorStr = configColors["Ready Background"];
            BKTargetShownColorStr = configColors["Target Shown Background"];
            CorrFillColorStr = configColors["Correct Fill"];
            CorrOutlineColorStr = configColors["Correct Outline"];
            ErrorFillColorStr = configColors["Error Fill"];
            ErrorOutlineColorStr = configColors["Error Outline"];


            // Target Sections
            var configTarget = config["Target"];
            targetDiaCM = int.Parse((string)configTarget["Target Size"]);
            targetNoOfPositions = int.Parse((string)configTarget["Target No of Positions"]);
            closeMarginPercentage = float.Parse((string)configTarget["Close Margin Percentage"]);


            audioFile_Correct = config["audioFile_Correct"];
            audioFile_Error = config["audioFile_Error"];
            saved_folder = config["saved_folder"];
            if (String.Compare(saved_folder, "default", true) == 0)
            {
                saved_folder = @"C:\\COTTaskSave";
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
