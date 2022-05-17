using System;
using System.Collections.Generic;
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
using System.Windows.Interop;

namespace SelfpacedTask
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

        public enum TrialExeResult
        {
            readyWaitTooShort,
            cueWaitTooShort,
            goReactionTimeToolong,
            goReachTimeToolong,
            goTouched
        }

        public enum ScreenTouchState
        {
            Idle,
            Touched
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
            CorrectGiven
        }

        private enum StartPadHoldState
        {
            HoldEnough,
            HoldTooShort
        }


        String strTrialExeFail = "Failed";
        String strTrialExeSuccess = "Success";


        /*****************parameters ************************/
        MainWindow parent;


        string file_saved;


        // ColorBrushes 
        private SolidColorBrush brush_BKWaitTrialStart, brush_BDWaitTrialStart, brush_BKGoInterface;
        private SolidColorBrush brush_DBCorr, brush_BDError;


        // audio feedback
        private string audioFile_Correct, audioFile_Error;
        System.Media.SoundPlayer player_Correct, player_Error;



        // rng for shuffling 
        private static Random rng = new Random();


        // Wait Time Range for Each Event, and Max Reaction and Reach Time (ms)
        float tMax_ReactionTimeMS, tMax_ReachTimeMS; 
        Int32 t_VisfeedbackShowMS, t_InterTrialMS; // Visual Feedback Show Time (ms)

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



        // Executed Trial Summary Information
        public int totalTrialNum, successTrialNum;
        public int noreactionTrialNum, noreachTrialNum;


        // executeresult of each trial
        TrialExeResult trialExeResult;

        ScreenTouchState screenTouchstate;

        // hold states for Ready, Cue Interfaces
        StartPadHoldState startpadHoldstate;



        // serial port for DLP-IO8-G
        SerialPort serialPort_IO8;
        int baudRate = 115200;
        Thread thread_ReadWrite_IO8;

        // commands for setting dig out high/low for channels
        static string cmdDigIn1 = "A";

        static string cmdHigh3 = "3";
        static string cmdLow3 = "E";


        static string startpad_DigIn= cmdDigIn1;
        static int startpad_DigIn_Pressed = 0;
        static int startpad_DigIn_Unpressed = 1;


        static string Code_InitState = "0000";
        static string Code_TouchTriggerTrial = "1110";
        static string Code_LeaveStartpad = "1001";
        static string Code_GoTargetShown = "1010";
        static string Code_GoReactionTooLong = "1100";
        static string Code_GoReachTooLong = "1011";
        static string Code_GoTouched = "1101";
        static string Code_EndState = Code_InitState;


        string TDTCmd_InitState, TDTCmd_TouchTriggerTrial, TDTCmd_LeaveStartpad;
        string TDTCmd_GoTargetShown, TDTCmd_GoReactionTooLong, TDTCmd_GoReachTooLong, TDTCmd_GoTouched, TDTCmd_EndState;



        /* startpad parameters */
        PressedStartpad pressedStartpad;
        public delegate void UpdateTextCallback(string message);

        /*Juicer Parameters*/
        GiveJuicerState giveJuicerState;
        // juicer given duration(ms)
        int t_JuicerCorrectGiven;


        // Global stopwatch
        Stopwatch globalWatch;


        // Variables for Various Time Points during trials
        long timePoint_StartpadTouched, timePoint_StartpadLeft;
        long timePoint_Interface_GoOnset;


        // variables for counting total trials and blockN
        int totalTriali;


        /*****Methods*******/
        public presentation(MainWindow mainWindow)
        {
            InitializeComponent();
            
            Touch.FrameReported += new TouchFrameEventHandler(Touch_FrameReported);

            // parent
            parent = mainWindow;

        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {//  Called by Window.show() at the first time
            WindowState = WindowState.Maximized;


            // Create Serial port 
            Create_SerialPort();

            // Create stopwatches
            Create_StopWatches();

            // get the setup from the parent interface
            GetSetupParameters();


            // Set audio Feedback related members 
            SetAudioFeedback();

            // IO8EventTDT Cmd
            Generate_IO8EventTDTCmd();
        }


        private void Create_SerialPort()
        {
            serialPort_IO8 = new SerialPort();

            serialPort_IO8.PortName = parent.serialPortIO8_name;
            serialPort_IO8.BaudRate = baudRate;
        }


        private void Create_StopWatches()
        {
            globalWatch = new Stopwatch();
            tpoints1TouchWatch = new Stopwatch();
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

        private string Convert2_IO8EventCmd_Bit5to8(string EventCode)
        {/*
            Generate IO8 Event Command based on EventCode using bit 5-8
            E.g. "0000" -> "TYUI", "1111" -> "5678", "1010" -> "5Y7I"
            */

            string cmdHigh5 = "5";
            string cmdLow5 = "T";
            string cmdHigh6 = "6";
            string cmdLow6 = "Y";
            string cmdHigh7 = "7";
            string cmdLow7 = "U";
            string cmdHigh8 = "8";
            string cmdLow8 = "I";

            string IO8EventCmd = cmdLow5 + cmdLow6 + cmdLow7 + cmdLow8;
            if (EventCode[0] == '1')
                IO8EventCmd = IO8EventCmd.Remove(0, 1).Insert(0, cmdHigh5);
            if (EventCode[1] == '1')
                IO8EventCmd = IO8EventCmd.Remove(1, 1).Insert(1, cmdHigh6);
            if (EventCode[2] == '1')
                IO8EventCmd = IO8EventCmd.Remove(2, 1).Insert(2, cmdHigh7);
            if (EventCode[3] == '1')
                IO8EventCmd = IO8EventCmd.Remove(3, 1).Insert(3, cmdHigh8);

            return IO8EventCmd;
        }

        private void Generate_IO8EventTDTCmd()
        {
            TDTCmd_InitState = Convert2_IO8EventCmd_Bit5to8(Code_InitState);
            TDTCmd_TouchTriggerTrial = Convert2_IO8EventCmd_Bit5to8(Code_TouchTriggerTrial);
            TDTCmd_LeaveStartpad = Convert2_IO8EventCmd_Bit5to8(Code_LeaveStartpad);
            TDTCmd_GoTargetShown = Convert2_IO8EventCmd_Bit5to8(Code_GoTargetShown);
            TDTCmd_GoReactionTooLong = Convert2_IO8EventCmd_Bit5to8(Code_GoReactionTooLong);
            TDTCmd_GoReachTooLong = Convert2_IO8EventCmd_Bit5to8(Code_GoReachTooLong);
            TDTCmd_GoTouched = Convert2_IO8EventCmd_Bit5to8(Code_GoTouched);
            TDTCmd_EndState = Convert2_IO8EventCmd_Bit5to8(Code_EndState);
        }

        public void Prepare_bef_Present()
        {
            // create a serial Port IO8 instance, and open it
            serialPort_IO8 = new SerialPort();
            try
            {
                serialPort_SetOpen(parent.serialPortIO8_name, baudRate);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Error Message", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            // Thread for Read and Write IO8
            thread_ReadWrite_IO8 = new Thread(new ThreadStart(Thread_ReadWrite_IO8));


            // init a global stopwatch
            globalWatch = new Stopwatch();
            tpoints1TouchWatch = new Stopwatch();

            // Init Trial Information
            Init_FeedbackTrialsInformation();


            //Write Trial Setup Information
            Save_TrialSetupInformation();

            totalTriali = 0;

        }

        private void serialPort_SetOpen(string portName, int baudRate)
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

        private void Save_TrialSetupInformation()
        {
            using (StreamWriter file = File.AppendText(file_saved))
            {
                file.WriteLine("\n\n");

                file.WriteLine(String.Format("{0, -40}", "Trial Information"));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Unit of Touch Point X Y Position", "Pixal"));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Touch Point X Y Coordinate System", "(0,0) in Top Left Corner, Right and Down Direction is Positive"));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Unit of Event TimePoint/Time", "Second"));
                file.WriteLine("\n");


                file.WriteLine(String.Format("{0, -40}", "Event Codes in TDT System:"));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(Code_InitState), Code_InitState));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(Code_TouchTriggerTrial), Code_TouchTriggerTrial));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(Code_LeaveStartpad), Code_LeaveStartpad));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(Code_GoTargetShown), Code_GoTargetShown));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(Code_GoReactionTooLong), Code_GoReactionTooLong));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(Code_GoReachTooLong), Code_GoReachTooLong));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(Code_GoTouched), Code_GoTouched));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(Code_EndState), Code_EndState));
                file.WriteLine("\n");


                file.WriteLine(String.Format("{0, -40}", "IO8 Commands:"));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(TDTCmd_InitState), TDTCmd_InitState));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(TDTCmd_TouchTriggerTrial), TDTCmd_TouchTriggerTrial));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(TDTCmd_LeaveStartpad), TDTCmd_LeaveStartpad));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(TDTCmd_GoTargetShown), TDTCmd_GoTargetShown));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(TDTCmd_GoReactionTooLong), TDTCmd_GoReactionTooLong));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(TDTCmd_GoReachTooLong), TDTCmd_GoReachTooLong));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(TDTCmd_GoTouched), TDTCmd_GoTouched));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(TDTCmd_EndState), TDTCmd_EndState));
                file.WriteLine("\n");
            }
        }

        public async void Present_Start()
        {                 
            // restart globalWatch and thread for IO8
            globalWatch.Restart();
            thread_ReadWrite_IO8.Start();

            

            // Present Each Trial
            PresentTrial = true;
            timestamp_0 = DateTime.Now.Ticks;
            while (PresentTrial)
            {

                // Write blockN
                using (StreamWriter file = File.AppendText(file_saved))
                {
                    file.WriteLine("\n\n");
                    
                    if(totalTriali == 0)
                    {
                        file.WriteLine("Trial Information:");
                        file.WriteLine("Touched XY Position Unit is Pixal, (0,0) in Screen Top Left Corner");
                        file.WriteLine("Unit of Event TimePoint/Time is second");
                        file.WriteLine("\n\n");
                    }
                }


                /*----- A New Trial ------*/
                totalTriali++;

                if (serialPort_IO8.IsOpen)
                    serialPort_IO8.WriteLine(TDTCmd_InitState);


                // ----- WaitStartTrial Interface
                pressedStartpad = PressedStartpad.No;
                await Interface_WaitStartTrial();
                if (PresentTrial == false)
                {
                    break;
                }


                try
                {

                    // Interface Waiting for Go 
                    await Interface_Go();
                    if (PresentTrial == false)
                    {
                        break;
                    }


                    Update_FeedbackTrialsInformation();
                    Remove_All();
                }
                catch (TaskCanceledException)
                {
                    Update_FeedbackTrialsInformation();
                    Remove_All();
                }

                // Write EndState after Each trial
                if (serialPort_IO8.IsOpen)
                    serialPort_IO8.WriteLine(TDTCmd_EndState);



                /*-------- Write Trial Execution Information: left startpad timepoint, touched timepoint/position et.al  ------*/
                using (StreamWriter file = File.AppendText(file_saved))
                {
                    decimal ms2sRatio = 1000;

                    if (totalTriali > 1)
                    { // Startpad touched in trial i+1 treated as the return point as in trial i        
                        file.WriteLine(String.Format("{0, -40}: {1}", "Returned to Startpad TimePoint", timePoint_StartpadTouched.ToString()));
                    }



                    /* --------- Current Trial Written Inf ------------*/
                    file.WriteLine("\n");

                    // Trial Num
                    file.WriteLine(String.Format("{0, -40}: {1}", "TrialNum", totalTriali.ToString()));
                    
                    // the timepoint when touching the startpad to initial a new trial
                    file.WriteLine(String.Format("{0, -40}: {1}", "Startpad Touched TimePoint", timePoint_StartpadTouched.ToString()));

                    // Go Interface showed TimePoint
                    file.WriteLine(String.Format("{0, -40}: {1}", "Go Interface Showed TimePoint", timePoint_Interface_GoOnset.ToString()));


                    // Go Movement Info: left startpad timepoint, touched/left positions and time points.
                    switch (trialExeResult)
                    {
                        case TrialExeResult.goReachTimeToolong:

                            // Go interface:  Left Startpad Time Point
                            file.WriteLine(String.Format("{0, -40}: {1}", "Startpad Left TimePoint", timePoint_StartpadLeft.ToString()));
                            break;

                        case TrialExeResult.goTouched:
                            // Go interface:  Left Startpad Time Point
                            file.WriteLine(String.Format("{0, -40}: {1}", "Startpad Left TimePoint", timePoint_StartpadLeft.ToString()));

                            //  Go interface:  touched  timepoint and (x, y position) of all touch points
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

                            break;


                        default:
                            break;
                    }

                    // trialExeResult
                    file.WriteLine(String.Format("{0, -40}: {1}, {2}", "Trial Result", strTrialExeFail, Enum.GetName(typeof(TrialExeResult), trialExeResult)));
                }


                // Wait t_InterTrialMS between the end of the trial and before the next
                Stopwatch waitWatch = new Stopwatch();
                waitWatch.Start();
                while (PresentTrial)
                {
                    if (waitWatch.ElapsedMilliseconds >= t_InterTrialMS)
                    {
                        waitWatch.Stop();
                        break;
                    }
                }
            }

            if(PresentTrial)
            {
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
            }
        }

        public void Present_Stop()
        {
            PresentTrial = false;

            // After Trials Presentation
            if (serialPort_IO8.IsOpen)
                serialPort_IO8.Close();

            thread_ReadWrite_IO8.Abort();

            globalWatch.Stop();
            
            tpoints1TouchWatch.Stop();

            // save the summary of exp
            SaveSummaryofExp();
        }

        public void Present_Pause()
        {
            PresentTrial = false;

            // After Trials Presentation
            if (serialPort_IO8.IsOpen)
                serialPort_IO8.Close();

            globalWatch.Stop();
            tpoints1TouchWatch.Stop();
            
            myGrid.Background = brush_BKWaitTrialStart;
            Remove_All();
        }



        private void SaveSummaryofExp()
        {
            /*Save the summary information of the exp*/

            using (StreamWriter file = File.AppendText(file_saved))
            {
                file.WriteLine("\n\n");

                file.WriteLine(String.Format("{0}", "Summary of the Trials"));
                file.WriteLine(String.Format("{0, -40}: {1}", "Total Trials", totalTrialNum.ToString()));
                file.WriteLine(String.Format("{0, -40}: {1}", "Success Trials", successTrialNum.ToString()));
                file.WriteLine(String.Format("{0, -40}: {1}", "No Reaction Trials", noreactionTrialNum.ToString()));
                file.WriteLine(String.Format("{0, -40}: {1}", "No Reach Trials", noreachTrialNum.ToString()));
            }

        }


        public void Update_FeedbackTrialsInformation()
        {/* Update the Feedback Trial Information in the Mainwindow */

            // Go trials
            parent.textBox_totalGoTrialNum.Text = totalTrialNum.ToString();
            parent.textBox_successGoTrialNum.Text = successTrialNum.ToString();
            parent.textBox_noreactionGoTrialNum.Text = noreactionTrialNum.ToString();
            parent.textBox_noreachGoTrialNum.Text = noreachTrialNum.ToString();

        }

        private void Init_FeedbackTrialsInformation()
        {/* Update the Feedback Trial Information in the Mainwindow */


            totalTrialNum = 0;
            successTrialNum = 0;
            noreactionTrialNum = 0;
            noreachTrialNum = 0;

            // Update Main Window Feedback 
            parent.textBox_totalGoTrialNum.Text = totalTrialNum.ToString();
            parent.textBox_successGoTrialNum.Text = successTrialNum.ToString();
            parent.textBox_noreactionGoTrialNum.Text = noreactionTrialNum.ToString();
            parent.textBox_noreachGoTrialNum.Text = noreachTrialNum.ToString();

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


        

        private void GetSetupParameters()
        {/* get the setup from the parent interface */


            // interfaces time related parameters
            tMax_ReactionTimeMS = parent.tMax_ReactionTimeS * 1000;
            tMax_ReachTimeMS = parent.tMax_ReachTimeS * 1000;
            t_VisfeedbackShowMS = (Int32)(parent.t_VisfeedbackShowS * 1000);
            t_InterTrialMS = (Int32)(parent.t_InterTrialS * 1000);

            // Juicer Time
            t_JuicerCorrectGiven = (Int32)(parent.t_JuicerCorrectGivenS * 1000);



            /* ---- Get all the Set Colors ----- */
            Color selectedColor;
            

            // Wait Background 
            selectedColor = (Color)(typeof(Colors).GetProperty(parent.BKWaitTrialColorStr) as PropertyInfo).GetValue(null, null);
            brush_BKWaitTrialStart = new SolidColorBrush(selectedColor);
            
            // Wait Boarder
            brush_BDWaitTrialStart = brush_BKWaitTrialStart;


            // Go Interface Background
            selectedColor = (Color)(typeof(Colors).GetProperty(parent.BKGoInterfaceColorStr) as PropertyInfo).GetValue(null, null);
            brush_BKGoInterface = new SolidColorBrush(selectedColor);


            // Correct Outline Color
            selectedColor = (Color)(typeof(Colors).GetProperty(parent.CorrFeedbackBDInterfaceColorStr) as PropertyInfo).GetValue(null, null);
            brush_DBCorr = new SolidColorBrush(selectedColor);

            // Error Outline Color
            selectedColor = (Color)(typeof(Colors).GetProperty(parent.ErrorFeedbackBDInterfaceColorStr) as PropertyInfo).GetValue(null, null);
            brush_BDError = new SolidColorBrush(selectedColor);


            
            
            // get the file for saving 
            file_saved = parent.file_saved;
            audioFile_Correct = parent.textBox_audioFile_Correct.Text;
            audioFile_Error = parent.textBox_audioFile_Error.Text;
        }


        private void Remove_All()
        {
        }


        private void Thread_ReadWrite_IO8()
        {/* Thread for reading/writing serial port IO8*/

            string codeHigh_JuicerPin = cmdHigh3, codeLow_JuicerPin = cmdLow3;
            Stopwatch startpadReadWatch = new Stopwatch();
            long startpadReadInterval = 30;

            if (serialPort_IO8.IsOpen)
            {
                serialPort_IO8.WriteLine(codeLow_JuicerPin);
            }

            startpadReadWatch.Start();
            while (serialPort_IO8.IsOpen)
            {
                try
                {
                    // ----- Juicer Control
                    if (giveJuicerState == GiveJuicerState.CorrectGiven)
                    {
                        serialPort_IO8.WriteLine(codeHigh_JuicerPin);
                        Thread.Sleep(t_JuicerCorrectGiven);
                        if (serialPort_IO8.IsOpen)
                            serialPort_IO8.WriteLine(codeLow_JuicerPin);
                        giveJuicerState = GiveJuicerState.No;
                    }
                    //--- End of Juicer Control



                    //--- Startpad Read
                    if (startpadReadWatch.ElapsedMilliseconds >= startpadReadInterval)
                    {
                        if (serialPort_IO8.IsOpen)
                            serialPort_IO8.WriteLine(startpad_DigIn);

                        // Read the Startpad Voltage
                        string str_Read = "";
                        if (serialPort_IO8.IsOpen)
                            str_Read = serialPort_IO8.ReadExisting();

                        // Restart the startpadReadWatch
                        startpadReadWatch.Restart();


                        // parse the start pad status
                        string[] str_DigIn = str_Read.Split();

                        if (!String.IsNullOrEmpty(str_DigIn[0]))
                        {
                            int digIn = Int32.Parse(str_DigIn[0]);

                            if (digIn == startpad_DigIn_Pressed && pressedStartpad == PressedStartpad.No)
                            {/* time point from notouched state to touched state */

                                pressedStartpad = PressedStartpad.Yes;
                            }
                            else if (digIn == startpad_DigIn_Unpressed && pressedStartpad == PressedStartpad.Yes)
                            {/* time point from touched state to notouched state */

                                // the time point for leaving startpad
                                timePoint_StartpadLeft = globalWatch.ElapsedMilliseconds;
                                serialPort_IO8.WriteLine(TDTCmd_LeaveStartpad);
                                pressedStartpad = PressedStartpad.No;
                            }
                        }
                    }
                }
                catch (InvalidCastException e)
                { }
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


                if (PresentTrial && pressedStartpad == PressedStartpad.Yes)
                {
                    // the time point for startpad touched
                    if (serialPort_IO8.IsOpen)
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
                while (PresentTrial && pressedStartpad == PressedStartpad.No && !waitEnoughTag)
                {
                    if (waitWatch.ElapsedMilliseconds >= t_maxWait * 1000)
                    {// Wait for t_maxWait
                        waitEnoughTag  = true;
                    }
                }

                waitWatch.Stop();


                if (PresentTrial && pressedStartpad == PressedStartpad.Yes)
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
                    if (PresentTrial && waitWatch.ElapsedMilliseconds >= tMax_ReactionTimeMS)
                    {/* No release Startpad within tMax_ReactionTime */
                        if (serialPort_IO8.IsOpen)
                            serialPort_IO8.WriteLine(TDTCmd_GoReactionTooLong);
                        waitWatch.Stop();

                        noreactionTrialNum++;

                        
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
                    if (PresentTrial && waitWatch.ElapsedMilliseconds >= tMax_ReachTimeMS)
                    {/*No Screen Touched within tMax_ReachTime*/
                        if (serialPort_IO8.IsOpen)
                            serialPort_IO8.WriteLine(TDTCmd_GoReachTooLong);
                        waitWatch.Stop();

                        noreachTrialNum++;

                        trialExeResult = TrialExeResult.goReachTimeToolong;
                        

                        throw new TaskCanceledException("No Reach within the Max Reach Time");
                    }
                }
                downPoints_Pos.Clear();
                touchPoints_PosTime.Clear();
                waitWatch.Restart();
                while (PresentTrial && waitWatch.ElapsedMilliseconds <= tMax_1Touch) ;
                waitWatch.Stop();
            }); 
        }


        private Task Wait_EnoughTouch(int t_EnoughTouchMS)
        {
            /* 
             * Wait for Enough Touch Time (ms)
             * 
             * Input: 
             *    t_EnoughTouch: the required Touch time (ms)  
             */

            Task task = null;

            // start a task and return it
            return Task.Run(() =>
            {
                Stopwatch touchedWatch = new Stopwatch();
                touchedWatch.Restart();

                while (PresentTrial && pressedStartpad == PressedStartpad.Yes && startpadHoldstate != StartPadHoldState.HoldEnough)
                {
                    if (touchedWatch.ElapsedMilliseconds >= t_EnoughTouchMS)
                    {/* touched with enough time */
                        startpadHoldstate = StartPadHoldState.HoldEnough;
                    }
                }
                touchedWatch.Stop();
                if (PresentTrial && startpadHoldstate != StartPadHoldState.HoldEnough)
                {
                    throw new TaskCanceledException(task);
                }

            });
        }


        private async Task Interface_Go()
        {/* task for Go Interface: Show the Go Interface while Listen to the state of the startpad.
            * 1. If Reaction time < Max Reaction Time or Reach Time < Max Reach Time, end up with long reaction or reach time ERROR Interface
            * 2. Within proper reaction time && reach time, detect the touch point 
            
            * Args:
            *    pos_Target: the center position of the Go Target

            * Output:
            *   startPadHoldstate_Cue = 
            *       StartPadHoldState.HoldEnough (if startpad is touched lasting t_Cue)
            *       StartPadHoldState.HoldTooShort (if startpad is released before t_Cue) 
            */

            try
            {
                myGrid.Background = brush_BKGoInterface;


                // go target Onset Time Point
                timePoint_Interface_GoOnset = globalWatch.ElapsedMilliseconds;
                if(serialPort_IO8.IsOpen)
                    serialPort_IO8.WriteLine(TDTCmd_GoTargetShown);

                totalTrialNum++;

                // Wait for Reaction within tMax_ReactionTime
                pressedStartpad = PressedStartpad.Yes;
                await Wait_Reaction();
                if (!PresentTrial)
                    return;

                // Wait for Touch within tMax_ReachTime and Calcuate the gotargetTouchstate
                screenTouchstate = ScreenTouchState.Idle;
                await Wait_Reach();
                if (!PresentTrial)
                    return;

                // goTouched
                trialExeResult = TrialExeResult.goTouched;
                
                if (!PresentTrial)
                    return;
                
                await Task.Delay(t_VisfeedbackShowMS);
            }
            catch(TaskCanceledException)
            {
                if(PresentTrial)
                    Interface_GoERROR_LongReactionReach();
                if (PresentTrial)
                    await Task.Delay(t_VisfeedbackShowMS);
                throw new TaskCanceledException("Not Reaction Within the Max Reaction Time.");
                
            }
            
        }


        private void Feedback_GoERROR()
        {
            // Visual Feedback
            //myGridBorder.BorderBrush = brush_ErrorFill;

           
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
            myGrid.UpdateLayout();


            //Juicer Feedback
            giveJuicerState = GiveJuicerState.CorrectGiven;

            // Audio Feedback
            player_Correct.Play();
        }



        public void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            parent.presentation_Stop();
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
