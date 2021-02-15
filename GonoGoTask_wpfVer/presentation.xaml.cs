﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Diagnostics;
using System.IO.Ports;
using System.IO;
using System.Reflection;
using sd = System.Drawing;

namespace COTTask_wpf
{
    /// <summary>
    /// Interaction logic for presentation.xaml
    /// </summary>
    /// 
    public partial class presentation : Window
    {

        /***** predefined parameters*******/
        int wpoints_radius = 15;



        /***********enumerate *****************/
        public enum TargetType
        {
            Go,
        }

        public enum TrialExeResult
        {
            readyWaitTooShort,
            cueWaitTooShort,
            goReactionTimeToolong,
            goReachTimeToolong,
            goHit,
            goClose,
            goMiss
        }

        public enum ScreenTouchState
        {
            Idle,
            Touched
        }

        private enum GoTargetTouchState
        {
            goHit, // at least one finger inside the circleGo
            goClose, // the distance between the closest touch point and the center of the circleGo is less than a threshold
            goMissed, // touched with longer distance 
        }

        /*startpad related enumerate*/
        private enum ReadStartpad
        {
            No,
            Yes
        }

        private enum PressedStartpad
        {
            No,
            Yes
        }

        private enum GiveJuicerState
        {
            No,
            CloseGiven,
            FullGiven
        }

        private enum StartPadHoldState
        {
            HoldEnough,
            HoldTooShort
        }



        /*****************parameters ************************/
        MainWindow parent;


        string file_saved;

        // diameter for crossing, circle, square and white points
        int objdiameter, closediameter;


        TargetType targetType;



        // objects of Go cirle, nogo Rectangle, lines of the crossing, and two white points
        Ellipse circleGo, circleGoClose;

        // ColorBrushes 
        private SolidColorBrush brush_goCircleFill;
        private SolidColorBrush brush_BKWaitTrialStart, brush_BKReady, brush_BKTargetShown;
        private SolidColorBrush brush_CorrectFill, brush_CorrOutline, brush_ErrorFill, brush_ErrorOutline;
        private SolidColorBrush brush_CloseFill;
        private SolidColorBrush brush_BDWaitTrialStart;


        // Center Point and Radius of CircleGo (in Pixal)
        Point circleGo_cPoint_Pixal; 
        double circleGo_Radius_Pixal;
        // the Radius of CircleGoClose (in Pixal)
        double circleGoClose_Radius_Pixal; 

        // audio feedback
        private string audioFile_Correct, audioFile_Error;
        System.Media.SoundPlayer player_Correct, player_Error;


        // name of all the objects
        string name_circleGo = "circleGo";
        string name_circleGoClose = "circleGoClose";


        // rng for shuffling 
        private static Random rng = new Random();


        // Wait Time Range for Each Event, and Max Reaction and Reach Time
        float tMax_ReactionTimeMS, tMax_ReachTimeMS; 
        Int32 t_VisfeedbackShow; // Visual Feedback Show Time (ms)

        bool PresentTrial;

        // time stamp
        long timestamp_0;


        // set storing the touch point id (no replicates)
        HashSet<int> touchPoints_Id = new HashSet<int>();

        // list storing the position/Timepoint of the touch points when touched down
        List<double[]> downPoints_Pos = new List<double[]>();

        // list storing the position, touched and left Timepoints of the touch points
        // one element: [point_id, touched_timepoint, touched_x, touched_y, left_timepoint, left_x, left_y]
        List<double[]> touchPoints_PosTime = new List<double[]>(); 

        // Stop Watch for recording the time interval between the first touchpoint and the last touchpoint within One Touch
        Stopwatch tpoints1TouchWatch;
        // the Max Duration for One Touch (ms)
        long tMax_1Touch = 40;
        GoTargetTouchState gotargetTouchstate;


        // Executed Trial Information
        public int totalGoTrialNum, successGoTrialNum;
        public int missGoTrialNum, noreactionGoTrialNum, noreachGoTrialNum;


        // executeresult of each trial
        TrialExeResult trialExeResult;

        ScreenTouchState screenTouchstate;

        // hold states for Ready, Cue Interfaces
        StartPadHoldState startpadHoldstate;



        // serial port for DLP-IO8-G
        SerialPort serialPort_IO8;
        int baudRate = 115200;
        Thread thread_ReadWrite_IO8;
        int volTouch = 4;

        // commands for setting dig out high/low for channels
        static string cmdHigh5 = "5";
        static string cmdLow5 = "T";
        static string cmdHigh6 = "6";
        static string cmdLow6 = "Y";
        static string cmdHigh7 = "7";
        static string cmdLow7 = "U";
        static string cmdHigh8 = "8";
        static string cmdLow8 = "I";

        static string TDTCmd_TouchTriggerTrial = cmdHigh5 + cmdHigh6 + cmdHigh7 + cmdLow8;
        static string TDTCmd_ReadyShown = cmdLow5 + cmdHigh6 + cmdHigh7 + cmdLow8;
        static string TDTCmd_ReadyWaitTooShort = cmdLow5 + cmdLow6 + cmdHigh7 + cmdHigh8;
        static string TDTCmd_GoTargetShown = cmdHigh5 + cmdLow6 + cmdHigh7 + cmdLow8;
        static string TDTCmd_GoReactionTooLong = cmdHigh5 + cmdHigh6 + cmdLow7 + cmdLow8;
        static string TDTCmd_GoReachTooLong = cmdHigh5 + cmdLow6 + cmdHigh7 + cmdHigh8;
        static string TDTCmd_GoTouched = cmdHigh5 + cmdHigh6 + cmdLow7 + cmdHigh8;
        static string TDTCmd_GoTouchedHit = cmdLow5 + cmdHigh6 + cmdLow7 + cmdLow8;
        static string TDTCmd_GoTouchedClose = cmdLow5 + cmdHigh6 + cmdLow7 + cmdHigh8;
        static string TDTCmd_GoTouchedMiss = cmdHigh5 + cmdLow6 + cmdLow7 + cmdLow8;



        /* startpad parameters */
        PressedStartpad pressedStartpad;
        public delegate void UpdateTextCallback(string message);

        /*Juicer Parameters*/
        GiveJuicerState giveJuicerState;
        // juiver given duration(ms)
        int t_JuicerFullGiven, t_JuicerCloseGiven;


        // Global stopwatch
        Stopwatch globalWatch;


        // Variables for Various Time Points during trials
        long timePoint_StartpadTouched, timePoint_StartpadLeft;
        long timePoint_Interface_ReadyOnset, timePoint_Interface_TargetOnset;



        /*****Methods*******/
        public presentation(MainWindow mainWindow)
        {
            InitializeComponent();
            
            Touch.FrameReported += new TouchFrameEventHandler(Touch_FrameReported);

            // parent
            parent = mainWindow;
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Maximized;

            // get the setup from the parent interface
            GetSetupParameters();



            // Create necessary elements: go circle,  two white points and one crossing
            Create_GoCircle();

            // Set audio Feedback related members 
            SetAudioFeedback();

            PrepBef_Present();
        }

        private void PrepBef_Present()
        {
            // create a serial Port IO8 instance, and open it
            serialPort_IO8 = new SerialPort();
            try
            {
                serialPort_SetOpen(parent.serialPortIO8_name, baudRate);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Message", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            // Thread for Read and Write IO8
            thread_ReadWrite_IO8 = new Thread(new ThreadStart(Thread_ReadWrite_IO8));


            // init a global stopwatch
            globalWatch = new Stopwatch();
            tpoints1TouchWatch = new Stopwatch();

            // Init Trial Information
            Update_FeedbackTrialsInformation();
        }

        public static void Shuffle<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }


        public async void Present_Start()
        {                 
            int[] pos_OCenter_Taget;
            float t_Ready;
            Random rnd = new Random();

            // Present Each Trial
            globalWatch.Restart();
            thread_ReadWrite_IO8.Start();
            timestamp_0 = DateTime.Now.Ticks;
            int totalTriali = 0;
            int blockN = 0;
            PresentTrial = true;
            while (PresentTrial)
            {
                blockN++;
                Shuffle(parent.optPostions_OCenter_List);

                // Write blockN
                using (StreamWriter file = File.AppendText(file_saved))
                {
                    file.WriteLine("\n\n");
                    
                    if(totalTriali == 0)
                    {
                        file.WriteLine("Trial Information:");
                        file.WriteLine("\n\n");
                    }

                    file.WriteLine(String.Format("{0, -40}: {1}", "BlockN", blockN.ToString()));
                }

                for (int opi = 0; opi < parent.optPostions_OCenter_List.Count; opi++)
                {
                    // Extract trial parameters
                    targetType = TargetType.Go;
                    pos_OCenter_Taget = parent.optPostions_OCenter_List[opi];

                    t_Ready = Utility.TransferTo((float)rnd.NextDouble(), parent.tRange_ReadyTime[0], parent.tRange_ReadyTime[1]);

                    totalTriali++;

                    /*----- WaitStartTrial Interface ------*/
                    pressedStartpad = PressedStartpad.No;
                    await Interface_WaitStartTrial();

                    /*-------- Trial Interfaces -------*/
                    try
                    {
                        // Ready Interface
                        await Interface_Ready(t_Ready);


                        // Go Target Interface
                        await Interface_Go(pos_OCenter_Taget);


                        Update_FeedbackTrialsInformation();
                        Remove_All();
                    }
                    catch (TaskCanceledException)
                    {
                        Update_FeedbackTrialsInformation();
                        Remove_All();
                    }


                    /*-------- Write Trial Information ------*/
                    List<String> strExeSubResult = new List<String>();
                    strExeSubResult.Add("readyWaitTooShort");
                    strExeSubResult.Add("cueWaitTooShort");
                    strExeSubResult.Add("goReactionTimeToolong");
                    strExeSubResult.Add("goReachTimeToolong");
                    strExeSubResult.Add("goMiss");
                    strExeSubResult.Add("goSuccess");
                    String strExeFail = "Failed";
                    String strExeSuccess = "Success";

                    using (StreamWriter file = File.AppendText(file_saved))
                    {
                        decimal ms2sRatio = 1000;

                        if (totalTriali > 1)
                        { // Startpad touched in trial i+1 treated as the return point as in trial i        
                            file.WriteLine(String.Format("{0, -40}: {1}", "Returned to Startpad TimePoint", (timePoint_StartpadTouched / ms2sRatio).ToString()));
                        }

                        /* Current Trial Written Inf*/
                        file.WriteLine("\n");

                        // Trial Num
                        file.WriteLine(String.Format("{0, -40}: {1}", "TrialNum", totalTriali.ToString()));
                        // the timepoint when touching the startpad to initial a new trial
                        file.WriteLine(String.Format("{0, -40}: {1}", "Startpad Touched TimePoint", (timePoint_StartpadTouched / ms2sRatio).ToString()));

                        // Start Interface showed TimePoint
                        file.WriteLine(String.Format("{0, -40}: {1}", "Ready Start TimePoint", (timePoint_Interface_ReadyOnset / ms2sRatio).ToString()));

                        // Ready Time
                        file.WriteLine(String.Format("{0, -40}: {1}", "Ready Interface Time", t_Ready.ToString()));

                        // trialExeResult
                        if (trialExeResult == TrialExeResult.readyWaitTooShort)
                        {// case: ready WaitTooShort

                            // Left startpad early during ready
                            file.WriteLine(String.Format("{0, -40}: {1}", "Startpad Left TimePoint", (timePoint_StartpadLeft / ms2sRatio).ToString()));

                            // trial exe result : success or fail
                            file.WriteLine(String.Format("{0, -40}: {1}, {2}", "Trial Result", strExeFail, strExeSubResult[0]));
                        }
                        else if (trialExeResult == TrialExeResult.goReactionTimeToolong)
                        {// case : goReactionTimeToolong 

                            // Target Interface Timepoint, and Target position 
                            file.WriteLine(String.Format("{0, -40}: {1}", "Target Start TimePoint", (timePoint_Interface_TargetOnset / ms2sRatio).ToString()));
                            file.WriteLine(String.Format("{0, -40}: {1}, {2}", "TargetPosition", pos_OCenter_Taget[0].ToString(), pos_OCenter_Taget[1].ToString()));


                            // trial exe result
                            file.WriteLine(String.Format("{0, -40}: {1}, {2}", "Trial Result", strExeFail, strExeSubResult[2]));
                        }
                        else if (trialExeResult == TrialExeResult.goReachTimeToolong)
                        {// case : goReachTimeToolong

                            // Target Interface Timepoint, and Target position 
                            file.WriteLine(String.Format("{0, -40}: {1}", "Target Start TimePoint", (timePoint_Interface_TargetOnset / ms2sRatio).ToString()));
                            file.WriteLine(String.Format("{0, -40}: {1}, {2}", "TargetPosition", pos_OCenter_Taget[0].ToString(), pos_OCenter_Taget[1].ToString()));


                            // Target interface:  Left Startpad Time Point
                            file.WriteLine(String.Format("{0, -40}: {1}", "Startpad Left TimePoint", (timePoint_StartpadLeft / ms2sRatio).ToString()));

                            // trial exe result
                            file.WriteLine(String.Format("{0, -40}: {1}, {2}", "Trial Result", strExeFail, strExeSubResult[3]));
                        }
                        else if (trialExeResult == TrialExeResult.goClose | trialExeResult == TrialExeResult.goHit | trialExeResult == TrialExeResult.goMiss)
                        {// case: Go success (goClose or goHit) or goMiss

                            // Target Interface Timepoint, and Target position 
                            file.WriteLine(String.Format("{0, -40}: {1}", "Target Start TimePoint", (timePoint_Interface_TargetOnset / ms2sRatio).ToString()));
                            file.WriteLine(String.Format("{0, -40}: {1}, {2}", "TargetPosition", pos_OCenter_Taget[0].ToString(), pos_OCenter_Taget[1].ToString()));


                            // Target interface:  Left Startpad Time Point
                            file.WriteLine(String.Format("{0, -40}: {1}", "Startpad Left TimePoint", (timePoint_StartpadLeft / ms2sRatio).ToString()));

                            //  Target interface:  touched  timepoint and (x, y position) of all touch points
                            for (int pointi = 0; pointi < touchPoints_PosTime.Count; pointi++)
                            {
                                double[] downPoint = touchPoints_PosTime[pointi];

                                // touched pointi touchpoint
                                file.WriteLine(String.Format("{0, -40}: {1, -40}", "Touch Point " + pointi.ToString() + " TimePoint", ((decimal)downPoint[1] / ms2sRatio).ToString()));

                                // touched pointi position
                                file.WriteLine(String.Format("{0, -40}: {1}", "Touch Point " + pointi.ToString() + " XY Position", downPoint[2].ToString() + ", " + downPoint[3].ToString()));

                            }

                            //  Target interface:  left timepoint and (x, y position) of all touch points
                            for (int pointi = 0; pointi < touchPoints_PosTime.Count; pointi++)
                            {
                                double[] downPoint = touchPoints_PosTime[pointi];

                                // left pointi touchpoint
                                file.WriteLine(String.Format("{0, -40}: {1, -40}", "Left Point " + pointi.ToString() + " TimePoint", ((decimal)downPoint[4] / ms2sRatio).ToString()));

                                // left pointi position
                                file.WriteLine(String.Format("{0, -40}: {1}", "Left Point " + pointi.ToString() + " XY Position", downPoint[5].ToString() + ", " + downPoint[6].ToString()));
                            }


                            // trial exe result : success or fail
                            if (trialExeResult == TrialExeResult.goMiss)
                                file.WriteLine(String.Format("{0, -40}: {1}, {2}", "Trial Result", strExeFail, strExeSubResult[4]));
                            else
                                file.WriteLine(String.Format("{0, -40}: {1}, {2}", "Trial Result", strExeSuccess, strExeSubResult[5]));

                        }
                    }

                    if(PresentTrial==false)
                    {
                        break;
                    }
                }
            }

            // Detect the return to startpad timepoint for the last trial
            pressedStartpad = PressedStartpad.No;
            try
            {
                await Wait_Return2StartPad(1);
            }
            catch (TaskCanceledException)
            {
                using (StreamWriter file = File.AppendText(file_saved))
                {
                    file.WriteLine(String.Format("{0, -40}: {1}", "Returned to Startpad TimePoint", timePoint_StartpadTouched.ToString()));
                }
            }

            // save the summary of exp
            SaveSummaryofExp();
        }


        private void SaveSummaryofExp()
        {
            /*Save the summary information of the exp*/

            using (StreamWriter file = File.AppendText(file_saved))
            {
                file.WriteLine("\n\n");

                file.WriteLine(String.Format("{0}", "Summary of the Go Trials"));
                file.WriteLine(String.Format("{0, -40}: {1}", "Total Go Trials", totalGoTrialNum.ToString()));
                file.WriteLine(String.Format("{0, -40}: {1}", "Success Go Trials", successGoTrialNum.ToString()));
                file.WriteLine(String.Format("{0, -40}: {1}", "Miss Go Trials", missGoTrialNum.ToString()));
                file.WriteLine(String.Format("{0, -40}: {1}", "No Reaction Go Trials", noreactionGoTrialNum.ToString()));
                file.WriteLine(String.Format("{0, -40}: {1}", "No Reach Go Trials", noreachGoTrialNum.ToString()));
            }

        }


        public void Update_FeedbackTrialsInformation()
        {/* Update the Feedback Trial Information in the Mainwindow */

            // Go trials
            parent.textBox_totalGoTrialNum.Text = totalGoTrialNum.ToString();
            parent.textBox_successGoTrialNum.Text = successGoTrialNum.ToString();
            parent.textBox_missGoTrialNum.Text = missGoTrialNum.ToString();
            parent.textBox_noreactionGoTrialNum.Text = noreactionGoTrialNum.ToString();
            parent.textBox_noreachGoTrialNum.Text = noreachGoTrialNum.ToString();

        }

        public void Present_Stop()
        {
            PresentTrial = false;
            thread_ReadWrite_IO8.Abort();
            globalWatch.Stop();

            // After Trials Presentation
            if (serialPort_IO8.IsOpen)
                serialPort_IO8.Close();
            tpoints1TouchWatch.Stop();
        }


        private void SetAudioFeedback()
        {/*set the player_Correct and player_Error members
            */

            player_Correct = new System.Media.SoundPlayer(Properties.Resources.Correct);
            player_Error = new System.Media.SoundPlayer(Properties.Resources.Error);


            // Assign new audios
            if (String.Compare(audioFile_Correct, "default", true) != 0)
            {
                player_Correct.SoundLocation = audioFile_Correct;
            }
            if (String.Compare(audioFile_Error, "default", true) != 0)
            {
                player_Error.SoundLocation = audioFile_Error;
            }

            

        }


        private void serialPort_SetOpen (string portName, int baudRate)
        {
            try
            {
                serialPort_IO8.PortName = portName;
                serialPort_IO8.BaudRate = baudRate;
                serialPort_IO8.Open();  
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }



        private void GetSetupParameters()
        {/* get the setup from the parent interface */

            // close  diameter
            closediameter = (int)(parent.targetDiaPixal * (1 + parent.closeMarginPercentage / 100));


            // interfaces time related parameters
            tMax_ReactionTimeMS = parent.tMax_ReactionTimeS * 1000;
            tMax_ReachTimeMS = parent.tMax_ReachTimeS * 1000;
            t_VisfeedbackShow = (Int32)(parent.t_VisfeedbackShow * 1000);

            // Juicer Time
            t_JuicerFullGiven = (Int32)(parent.t_JuicerFullGivenS * 1000);
            t_JuicerCloseGiven = (Int32)(parent.t_JuicerCloseGivenS * 1000);

            /* ---- Get all the Set Colors ----- */
            Color selectedColor;
            
            // goCircle Color
            selectedColor = (Color)(typeof(Colors).GetProperty(parent.goColorStr) as PropertyInfo).GetValue(null, null);
            brush_goCircleFill = new SolidColorBrush(selectedColor);


            // Wait Background 
            selectedColor = (Color)(typeof(Colors).GetProperty(parent.BKWaitTrialColorStr) as PropertyInfo).GetValue(null, null);
            brush_BKWaitTrialStart = new SolidColorBrush(selectedColor);
            // Wait Boarder
            brush_BDWaitTrialStart = brush_BKWaitTrialStart;

            // Ready Background
            selectedColor = (Color)(typeof(Colors).GetProperty(parent.BKReadyColorStr) as PropertyInfo).GetValue(null, null);
            brush_BKReady = new SolidColorBrush(selectedColor);

            // Target Shown Background
            selectedColor = (Color)(typeof(Colors).GetProperty(parent.BKTargetShownColorStr) as PropertyInfo).GetValue(null, null);
            brush_BKTargetShown = new SolidColorBrush(selectedColor);

            // Correct Fill Color
            selectedColor = (Color)(typeof(Colors).GetProperty(parent.CorrFillColorStr) as PropertyInfo).GetValue(null, null);
            brush_CorrectFill = new SolidColorBrush(selectedColor);

            // Correct Outline Color
            selectedColor = (Color)(typeof(Colors).GetProperty(parent.CorrOutlineColorStr) as PropertyInfo).GetValue(null, null);
            brush_CorrOutline = new SolidColorBrush(selectedColor);

            // Error Fill Color
            selectedColor = (Color)(typeof(Colors).GetProperty(parent.ErrorFillColorStr) as PropertyInfo).GetValue(null, null);
            brush_ErrorFill = new SolidColorBrush(selectedColor);

            // Error Outline Color
            selectedColor = (Color)(typeof(Colors).GetProperty(parent.ErrorOutlineColorStr) as PropertyInfo).GetValue(null, null);
            brush_ErrorOutline = new SolidColorBrush(selectedColor);

            // Close Fill Color
            brush_CloseFill = brush_CorrectFill;

            
            
            // get the file for saving 
            file_saved = parent.file_saved;
            audioFile_Correct = parent.audioFile_Correct;
            audioFile_Error = parent.audioFile_Error;
        }


        private void Create_GoCircle()
        {/*
            Create the go circle: circleGo

            */

            // Create an Ellipse  
            circleGo = Utility.Create_Circle(parent.targetDiaPixal,  brush_goCircleFill);
            
            circleGo.Name = name_circleGo;
            circleGo.Visibility = Visibility.Hidden;
            circleGo.IsEnabled = false;

            // add to myGrid
            myGrid.Children.Add(circleGo);
            myGrid.RegisterName(circleGo.Name, circleGo);
            myGrid.UpdateLayout();


            // create the go close circle
            Create_GoCircleClose();
        }

        private void Show_GoCircle(int[] cPoint_Pos_OCenter)
        {/*
            Show the GoCircle into cPoint_Pos_OCenter (Origin in the center of the Screen)

            Arg:
                cPoint_Pos_OCenter: the x, y Positions of the Circle center in Pixal (Origin in the center of the Screen)

             */

            // Change the cPoint  into Top Left Coordinate System
            sd.Rectangle Rect_touchScreen = Utility.Detect_PrimaryScreen_Rect();
            int[] cPoint_Pos_OTopLeft = new int[] { cPoint_Pos_OCenter[0] + Rect_touchScreen.Width/2, cPoint_Pos_OCenter[1] + Rect_touchScreen.Height/2 };


            circleGo  = Utility.Move_Circle_OTopLeft(circleGo, cPoint_Pos_OTopLeft);
            circleGo.Fill = brush_goCircleFill;
            circleGo.Stroke = brush_goCircleFill;
            circleGo.Visibility = Visibility.Visible;
            circleGo.IsEnabled = true;
            myGrid.UpdateLayout();


            // Extract circleGo_cPoint_Pixal 
            circleGo_cPoint_Pixal = new Point(cPoint_Pos_OTopLeft[0], cPoint_Pos_OTopLeft[1]);

            
            Point circleGo_cPoint_Pixal2 = new Point(cPoint_Pos_OCenter[0] + Rect_touchScreen.Width/ 2, cPoint_Pos_OCenter[1] + Rect_touchScreen .Height/ 2);

            circleGo_Radius_Pixal = ((circleGo.Height + circleGo.Width) / 2) / 2;

        }
        private void Remove_GoCircle()
        {
            circleGo.Visibility = Visibility.Hidden;
            circleGo.IsEnabled = false;
            myGrid.UpdateLayout();

            // Remove the go close circle
            Remove_GoCircleClose();
        }


        private void Create_GoCircleClose()
        {/*
            Create the go Close circle: circleGoClose

            */

            // Create an Ellipse  
            circleGoClose = new Ellipse();

            circleGoClose.Stroke = brush_goCircleFill;

            // set the size, position of circleGo
            circleGoClose.Height = closediameter;
            circleGoClose.Width = closediameter;
            circleGoClose.VerticalAlignment = VerticalAlignment.Top;
            circleGoClose.HorizontalAlignment = HorizontalAlignment.Left;

            circleGoClose.Name = name_circleGoClose;
            circleGoClose.Visibility = Visibility.Hidden;
            circleGoClose.IsEnabled = false;

            // add to myGrid
            myGrid.Children.Add(circleGoClose);
            myGrid.RegisterName(circleGoClose.Name, circleGoClose);
            myGrid.UpdateLayout();
        }

        private void Add_GoCircleClose(int[] centerPoint_Pos)
        {/*show the Go Close Circle with Circle Center at (centerPoint_Pos[0], centerPoint_Pos[1]) */

            int centerPoint_X = centerPoint_Pos[0], centerPoint_Y = centerPoint_Pos[1];

            double left = centerPoint_X - circleGoClose.Width / 2;
            double top = centerPoint_Y - circleGoClose.Height / 2;
            circleGoClose.Margin = new Thickness(left, top, 0, 0);

            if(parent.showCloseCircle==true)
                circleGoClose.Visibility = Visibility.Visible;
            else
                circleGoClose.Visibility = Visibility.Hidden;
            circleGoClose.IsEnabled = true;
            myGrid.UpdateLayout();


            // get the center point and the radius of circleGo
            circleGoClose_Radius_Pixal = ((circleGoClose.Height + circleGoClose.Width) / 2) / 2;
        }
        private void Remove_GoCircleClose()
        {
            circleGoClose.Visibility = Visibility.Hidden;
            circleGo.IsEnabled = false;
            myGrid.UpdateLayout();
        }



        private void Remove_All()
        {
            Remove_GoCircle();
        }


        private void Thread_ReadWrite_IO8()
        {/* Thread for reading/writing serial port IO8*/

            string codeHigh_JuicerPin = "3", codeLow_JuicerPin = "E";
            Stopwatch startpadReadWatch = new Stopwatch();
            long startpadReadInterval = 30;

            serialPort_IO8.WriteLine(codeLow_JuicerPin);
            startpadReadWatch.Start();
            while (serialPort_IO8.IsOpen)
            {
                // ----- Juicer Control
                if (giveJuicerState == GiveJuicerState.FullGiven)
                {
                    serialPort_IO8.WriteLine(codeHigh_JuicerPin);
                    Thread.Sleep(t_JuicerFullGiven);
                    serialPort_IO8.WriteLine(codeLow_JuicerPin);
                    giveJuicerState = GiveJuicerState.No;
                }
                if (giveJuicerState == GiveJuicerState.CloseGiven)
                {
                    serialPort_IO8.WriteLine(codeHigh_JuicerPin);
                    Thread.Sleep(t_JuicerCloseGiven);
                    serialPort_IO8.WriteLine(codeLow_JuicerPin);
                    giveJuicerState = GiveJuicerState.No;
                }
                //--- End of Juicer Control



                //--- Startpad Read
                if(startpadReadWatch.ElapsedMilliseconds >= startpadReadInterval)
                {
                    serialPort_IO8.WriteLine("Z");

                    // Read the Startpad Voltage
                    string str_Read = serialPort_IO8.ReadExisting();

                    // Restart the startpadReadWatch
                    startpadReadWatch.Restart();

                    // parse the start pad voltage 
                    string[] str_vol = str_Read.Split(new Char[] { 'V' });

                    if (!String.IsNullOrEmpty(str_vol[0]))
                    {
                        float voltage = float.Parse(str_vol[0]);

                        if (voltage < volTouch && pressedStartpad == PressedStartpad.No)
                        {/* time point from notouched state to touched state */
                            
                            pressedStartpad = PressedStartpad.Yes;
                        }
                        else if (voltage > volTouch && pressedStartpad == PressedStartpad.Yes)
                        {/* time point from touched state to notouched state */

                            // the time point for leaving startpad
                            timePoint_StartpadLeft = globalWatch.ElapsedMilliseconds;
                            pressedStartpad = PressedStartpad.No;
                        }
                    }
                }

            }

            startpadReadWatch.Stop();
        }



        private Task Interface_WaitStartTrial()
        {
            /* task for WaitStart interface
             * 
             * Wait for Touching Startpad to trigger a new Trial
             */

            Remove_All();
            myGrid.Background = brush_BKWaitTrialStart;
            //myGridBorder.BorderBrush = brush_BDWaitTrialStart;

            Task task_WaitStart = Task.Run(() =>
            {
                while (PresentTrial && pressedStartpad == PressedStartpad.No) ;


                if (pressedStartpad == PressedStartpad.Yes)
                {
                    // the time point for startpad touched
                    serialPort_IO8.WriteLine(TDTCmd_TouchTriggerTrial);
                    timePoint_StartpadTouched = globalWatch.ElapsedMilliseconds;
                }

            });

            return task_WaitStart;
        }


        private Task Wait_Return2StartPad(float t_maxWait)
        {
            /* 
             * Wait for Returning Back to Startpad 
             * 
             * Input: 
             *    t_maxWait: the maximum wait time for returning back (s)  
             */


            return Task.Run(() =>
            {
                Stopwatch waitWatch = new Stopwatch();
                waitWatch.Restart();
                bool waitEnoughTag = false;
                while (pressedStartpad == PressedStartpad.No && !waitEnoughTag)
                {
                    if (waitWatch.ElapsedMilliseconds >= t_maxWait * 1000)
                    {// Wait for t_maxWait
                        waitEnoughTag  = true;
                    }
                }

                waitWatch.Stop();


                if (pressedStartpad == PressedStartpad.Yes)
                {
                    throw new TaskCanceledException("A return touched occurred");
                }

            });
        }


        private Task Wait_Reaction()
        {/* Wait for Reaction within tMax_ReactionTime */

            // start a task and return it
            return Task.Run(() =>
            {
                Stopwatch waitWatch = new Stopwatch();
                waitWatch.Start();
                while (PresentTrial && pressedStartpad == PressedStartpad.Yes)
                {
                    if (waitWatch.ElapsedMilliseconds >= tMax_ReactionTimeMS)
                    {/* No release Startpad within tMax_ReactionTime */
                        waitWatch.Stop();

                        noreactionGoTrialNum++;

                        serialPort_IO8.WriteLine(TDTCmd_GoReactionTooLong);
                        trialExeResult = TrialExeResult.goReactionTimeToolong;
                        

                        throw new TaskCanceledException("No Reaction within the Max Reaction Time");
                    }
                }
                waitWatch.Stop();
            });
        }

        private Task Wait_Reach()
        {/* Wait for Reach within tMax_ReachTime*/

            return Task.Run(() =>
            {
                Stopwatch waitWatch = new Stopwatch();
                waitWatch.Start();
                while (PresentTrial && screenTouchstate == ScreenTouchState.Idle)
                {
                    if (waitWatch.ElapsedMilliseconds >= tMax_ReachTimeMS)
                    {/*No Screen Touched within tMax_ReachTime*/
                        waitWatch.Stop();

                        noreachGoTrialNum++;

                        serialPort_IO8.WriteLine(TDTCmd_GoReachTooLong);
                        trialExeResult = TrialExeResult.goReachTimeToolong;
                        

                        throw new TaskCanceledException("No Reach within the Max Reach Time");
                    }
                }
                downPoints_Pos.Clear();
                touchPoints_PosTime.Clear();
                waitWatch.Restart();
                while (waitWatch.ElapsedMilliseconds <= tMax_1Touch) ;
                waitWatch.Stop();
                calc_GoTargetTouchState();
            }); 
        }

        private void calc_GoTargetTouchState()
        {/* Calculate GoTargetTouchState  
            1. based on the Touch Down Positions in  List downPoints_Pos and circleGo_cPoint_Pixal
            2. Assign the calculated target touch state to the GoTargetTouchState variable gotargetTouchstate
            */

            double distance; 
            gotargetTouchstate = GoTargetTouchState.goMissed;
            while (downPoints_Pos.Count > 0)
            {
                // always deal with the point at 0
                Point touchp = new Point(downPoints_Pos[0][0], downPoints_Pos[0][1]);

                // distance between the touchpoint and the center of the circleGo
                distance = Point.Subtract(circleGo_cPoint_Pixal, touchp).Length;


                if (distance <= circleGo_Radius_Pixal)
                {// Hit 

                    serialPort_IO8.WriteLine(TDTCmd_GoTouchedHit);
                    gotargetTouchstate = GoTargetTouchState.goHit;
                    
                    downPoints_Pos.Clear();
                    break;
                }
                else if (gotargetTouchstate == GoTargetTouchState.goMissed && distance <= circleGoClose_Radius_Pixal)
                {
                    gotargetTouchstate = GoTargetTouchState.goClose;
                }
                // remove the downPoint at 0
                downPoints_Pos.RemoveAt(0);
            }
            if(gotargetTouchstate == GoTargetTouchState.goClose)
            {
                serialPort_IO8.WriteLine(TDTCmd_GoTouchedClose);
            }
            else if (gotargetTouchstate == GoTargetTouchState.goMissed)
            {
                serialPort_IO8.WriteLine(TDTCmd_GoTouchedMiss);
            }
            downPoints_Pos.Clear();
        }


        private Task Wait_EnoughTouch(float t_EnoughTouch)
        {
            /* 
             * Wait for Enough Touch Time
             * 
             * Input: 
             *    t_EnoughTouch: the required Touch time (s)  
             */

            Task task = null;

            // start a task and return it
            return Task.Run(() =>
            {
                Stopwatch touchedWatch = new Stopwatch();
                touchedWatch.Restart();

                while (PresentTrial && pressedStartpad == PressedStartpad.Yes && startpadHoldstate != StartPadHoldState.HoldEnough)
                {
                    if (touchedWatch.ElapsedMilliseconds >= t_EnoughTouch * 1000)
                    {/* touched with enough time */
                        startpadHoldstate = StartPadHoldState.HoldEnough;
                    }
                }
                touchedWatch.Stop();
                if (startpadHoldstate != StartPadHoldState.HoldEnough)
                {
                    throw new TaskCanceledException(task);
                }

            });
        }

        private async Task Interface_Ready(float t_Ready)
        {/* task for Ready interface:
            Show the Ready Interface while Listen to the state of the startpad. 
            * 
            * Output:
            *   startPadHoldstate_Ready = 
            *       StartPadHoldState.HoldEnough (if startpad is touched lasting t_Ready)
            *       StartPadHoldState.HoldTooShort (if startpad is released before t_Ready) 
            */

            try
            {
                myGrid.Background = brush_BKReady;
                serialPort_IO8.WriteLine(TDTCmd_ReadyShown);
                timePoint_Interface_ReadyOnset = globalWatch.ElapsedMilliseconds;

                // Wait Startpad Hold Enough Time
                startpadHoldstate = StartPadHoldState.HoldTooShort;
                await Wait_EnoughTouch(t_Ready);

            }
            catch (TaskCanceledException)
            {
                // trial execute result: waitReadyTooShort 
                serialPort_IO8.WriteLine(TDTCmd_ReadyWaitTooShort);
                trialExeResult = TrialExeResult.readyWaitTooShort;

                Task task = null;
                throw new TaskCanceledException(task);
            }
        }

        private async Task Interface_Go(int[] pos_Target)
        {/* task for Go Interface: Show the Go Interface while Listen to the state of the startpad.
            * 1. If Reaction time < Max Reaction Time or Reach Time < Max Reach Time, end up with long reaction or reach time ERROR Interface
            * 2. Within proper reaction time && reach time, detect the touch point and end up with hit, near and miss.
            
            * Args:
            *    pos_Target: the center position of the Go Target

            * Output:
            *   startPadHoldstate_Cue = 
            *       StartPadHoldState.HoldEnough (if startpad is touched lasting t_Cue)
            *       StartPadHoldState.HoldTooShort (if startpad is released before t_Cue) 
            */

            try
            {
                myGrid.Background = brush_BKTargetShown;

                // Add the Go Circle
                Show_GoCircle(pos_Target);

                // go target Onset Time Point
                timePoint_Interface_TargetOnset = globalWatch.ElapsedMilliseconds;
                serialPort_IO8.WriteLine(TDTCmd_GoTargetShown);

                totalGoTrialNum++;

                // Wait for Reaction within tMax_ReactionTime
                pressedStartpad = PressedStartpad.Yes;
                await Wait_Reaction();

                // Wait for Touch within tMax_ReachTime and Calcuate the gotargetTouchstate
                screenTouchstate = ScreenTouchState.Idle;
                await Wait_Reach();


                /*---- Go Target Touch States ----*/
                if (gotargetTouchstate == GoTargetTouchState.goHit)
                {/*Hit */

                    Feedback_GoCorrect_Hit();

                    successGoTrialNum++;

                    // trial execute result: goHit 
                    trialExeResult = TrialExeResult.goHit;
                    
                }
                else if (gotargetTouchstate == GoTargetTouchState.goClose)
                {/* touch close to the target*/
                    
                    Feedback_GoCorrect_Close();

                    successGoTrialNum++;

                    // trial execute result: goClose 
                    trialExeResult = TrialExeResult.goClose;
                }
                else if (gotargetTouchstate == GoTargetTouchState.goMissed)
                {/* touch missed*/
                    
                    Feedback_GoERROR_Miss();

                    missGoTrialNum++;

                    // trial execute result: goMiss 
                    trialExeResult = TrialExeResult.goMiss;
                }
                
                await Task.Delay(t_VisfeedbackShow);
            }
            catch(TaskCanceledException)
            {
                Interface_GoERROR_LongReactionReach();
                await Task.Delay(t_VisfeedbackShow);
                throw new TaskCanceledException("Not Reaction Within the Max Reaction Time.");
            }
            
        }

        private void Feedback_GoERROR()
        {
            // Visual Feedback
            //myGridBorder.BorderBrush = brush_ErrorFill;
            circleGo.Fill = brush_ErrorFill;
            circleGo.Stroke = brush_ErrorOutline;
            circleGoClose.Stroke = brush_ErrorOutline;
            myGrid.UpdateLayout();


            //Juicer Feedback
            giveJuicerState = GiveJuicerState.No;

            // Audio Feedback
            player_Error.Play()
;            
        }
        private void Interface_GoERROR_LongReactionReach()
        {
            Feedback_GoERROR();
        }

        private void Feedback_GoERROR_Miss()
        {
            Feedback_GoERROR();
        }

        private void Feedback_GoCorrect_Hit()
        {
            // Visual Feedback
            //myGridBorder.BorderBrush = brush_CorrectFill;
            circleGo.Fill = brush_CorrectFill;
            circleGoClose.Stroke = brush_CorrectFill;
            myGrid.UpdateLayout();


            //Juicer Feedback
            giveJuicerState = GiveJuicerState.FullGiven;

            // Audio Feedback
            player_Correct.Play();
        }

        private void Feedback_GoCorrect_Close()
        {
            // Visual Feedback
            //myGridBorder.BorderBrush = brush_CloseFill;
            circleGo.Fill = brush_CloseFill;
            circleGoClose.Stroke = brush_CloseFill;
            myGrid.UpdateLayout();

            //Juicer Feedback
            giveJuicerState = GiveJuicerState.CloseGiven;

            // Audio Feedback
            player_Correct.Play();
        }

        public void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Present_Stop();
            parent.btn_start.IsEnabled = true;
            parent.btn_stop.IsEnabled = false;
        }


        void Touch_FrameReported(object sender, TouchFrameEventArgs e)
        {/* Add the Id of New Touch Points into Hashset touchPoints_Id 
            and the Corresponding Touch Down Positions into List downPoints_Pos (no replicates)*/
            screenTouchstate = ScreenTouchState.Touched;
            TouchPointCollection touchPoints = e.GetTouchPoints(myGrid);
            bool addedNew;
            long time = tpoints1TouchWatch.ElapsedMilliseconds;
            long timestamp_now = (DateTime.Now.Ticks - timestamp_0) / TimeSpan.TicksPerMillisecond;
            for (int i = 0; i < touchPoints.Count; i++)
            {
                TouchPoint _touchPoint = touchPoints[i];
                if (_touchPoint.Action == TouchAction.Down)
                { /* TouchAction.Down */

                    if (touchPoints_Id.Count == 0)
                    {// the first touch point for one touch
                        tpoints1TouchWatch.Restart();
                        serialPort_IO8.WriteLine(TDTCmd_GoTouched);
                    }
                    lock (touchPoints_Id)
                    {
                        // Add the touchPoint to the Hashset touchPoints_Id, Return true if added, otherwise false.
                        addedNew = touchPoints_Id.Add(_touchPoint.TouchDevice.Id);
                    }
                    if (addedNew)
                    {/* deal with the New Added TouchPoint*/

                        // store the pos of the point with down action
                        lock (downPoints_Pos)
                        {
                            downPoints_Pos.Add(new double[2] { _touchPoint.Position.X, _touchPoint.Position.Y });
                        }

                        // store the pos and time of the point with down action, used for file writing
                        lock (touchPoints_PosTime)
                        {
                            touchPoints_PosTime.Add(new double[7] { _touchPoint.TouchDevice.Id, timestamp_now, _touchPoint.Position.X, _touchPoint.Position.Y, 0, 0, 0 });
                        }
                    }
                }
                else if (_touchPoint.Action == TouchAction.Up)
                {
                    // remove the id of the point with up action
                    lock (touchPoints_Id)
                    {
                        touchPoints_Id.Remove(_touchPoint.TouchDevice.Id);
                    }

                    // add the left points timepoint, and x,y positions of the current _touchPoint.TouchDevice.Id
                    lock (touchPoints_PosTime)
                    {
                        for (int pointi = 0; pointi < touchPoints_PosTime.Count; pointi++)
                        {
                            if (touchPoints_PosTime[pointi][0] == _touchPoint.TouchDevice.Id)
                            {
                                touchPoints_PosTime[pointi][4] = timestamp_now;
                                touchPoints_PosTime[pointi][5] = _touchPoint.Position.X;
                                touchPoints_PosTime[pointi][6] = _touchPoint.Position.Y;
                            }
                        }
                    }
                }
            }

        }
    }
}
