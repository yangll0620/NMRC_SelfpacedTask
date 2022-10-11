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

namespace SelfpacedTask_wpfVer
{
    /// <summary>
    /// Interaction logic for presentation.xaml
    /// </summary>
    /// 
    public partial class presentation : Window
    {

        /***********enumerate *****************/

        public enum TrialExeResult
        {
            Idle,
            HoldTooShort,
            ReachTimeToolong,
            Touched
        }

        public enum ScreenTouchState
        {
            Idle,
            Touched
        }


        /*startpad related enumerate*/

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



        /*****************parameters ************************/
        MainWindow parent;


        string file_saved;


        // ColorBrushes 
        private SolidColorBrush brush_BKWaitTrialStart, brush_BDWaitTrialStart, brush_BKHold;
        private SolidColorBrush brush_BKCorrect, brush_BKError;


        // audio feedback
        private string audioFile_Correct, audioFile_Error;
        System.Media.SoundPlayer player_Correct, player_Error;


        // rng for shuffling 
        private static Random rng = new Random();


        // Wait Time Range for Each Event, and Max Reaction and Reach Time (ms)
        float  tMax_ReachTimeMS; 
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


        // Executed Trial Information
        public int totalTrialNum, successTrialNum, noreachGoTrialNum, shortHoldTrialNum;



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
        static string Code_HoldInterfaceShown = "0110";
        static string Code_LeaveStartpad_Early = "1001";
        static string Code_HoldTooShort = "0011";
        static string Code_LeaveStartpad_InitMove = "1010";
        static string Code_GoReachTooLong = "1011";
        static string Code_GoTouched = "1101";

        

        string TDTCmd_InitState, TDTCmd_TouchTriggerTrial, TDTCmd_LeaveStartpad_Early, TDTCmd_LeaveStartpad_InitMove, TDTCmd_HoldTooShort;
        string TDTCmd_GoReachTooLong, TDTCmd_GoTouched;
        string TDTCmd_HoldInterfaceShown;


        /* startpad parameters */
        PressedStartpad pressedStartpad;
        public delegate void UpdateTextCallback(string message);

        /*Juicer Parameters*/
        GiveJuicerState giveJuicerState;
        // juicer given duration(ms)
        int t_JuicerCorrectGiven;


        // Global stopwatch
        Stopwatch globalWatch;
        int t_trialHoldMS;


        // Variables for Various Time Points during trials
        long timePoint_Startpad_Touch2StartTrial, timePoint_Startpad_LeftEarly, timePoint_Startpad_Left2InitMove;
        long timePoint_Interface_HoldOnset;


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
            TDTCmd_HoldInterfaceShown = Convert2_IO8EventCmd_Bit5to8(Code_HoldInterfaceShown);
            TDTCmd_LeaveStartpad_Early = Convert2_IO8EventCmd_Bit5to8(Code_LeaveStartpad_Early);
            TDTCmd_HoldTooShort = Convert2_IO8EventCmd_Bit5to8(Code_HoldTooShort);
            TDTCmd_LeaveStartpad_InitMove = Convert2_IO8EventCmd_Bit5to8(Code_LeaveStartpad_InitMove);
            TDTCmd_GoReachTooLong = Convert2_IO8EventCmd_Bit5to8(Code_GoReachTooLong);
            TDTCmd_GoTouched = Convert2_IO8EventCmd_Bit5to8(Code_GoTouched);
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
                file.WriteLine(String.Format("{0, -40}:  {1}", "Unit of Event TimePoint/Time", "microSecond"));
                file.WriteLine("\n");


                file.WriteLine(String.Format("{0, -40}", "Event Codes in TDT System:"));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(Code_InitState), Code_InitState));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(Code_TouchTriggerTrial), Code_TouchTriggerTrial));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(Code_LeaveStartpad_Early), Code_LeaveStartpad_Early));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(Code_LeaveStartpad_InitMove), Code_LeaveStartpad_InitMove));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(Code_GoReachTooLong), Code_GoReachTooLong));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(Code_GoTouched), Code_GoTouched));
                file.WriteLine("\n");


                file.WriteLine(String.Format("{0, -40}", "IO8 Commands:"));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(TDTCmd_InitState), TDTCmd_InitState));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(TDTCmd_TouchTriggerTrial), TDTCmd_TouchTriggerTrial));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(TDTCmd_LeaveStartpad_Early), TDTCmd_LeaveStartpad_Early));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(TDTCmd_LeaveStartpad_InitMove), TDTCmd_LeaveStartpad_InitMove));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(TDTCmd_GoReachTooLong), TDTCmd_GoReachTooLong));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(TDTCmd_GoTouched), TDTCmd_GoTouched));
                file.WriteLine("\n");
            }
        }


        public async void Present_Start()
        {
            Random rnd = new Random();

            // restart globalWatch and thread for IO8
            globalWatch.Restart();
            thread_ReadWrite_IO8.Start();


            // Present Each Trial
            PresentTrial = true;
            timestamp_0 = DateTime.Now.Ticks;
            while (PresentTrial)
            {
                reset_TrialParas();

                t_trialHoldMS = (int)(Utility.TransferTo((float)rnd.NextDouble(), parent.tRange_HoldTimeS[0], parent.tRange_HoldTimeS[1]) * 1000);
                // Write InitState
                if (serialPort_IO8.IsOpen)
                    serialPort_IO8.WriteLine(TDTCmd_InitState);


                // Wait StartTrial 
                pressedStartpad = PressedStartpad.No;
                await Interface_WaitStartTrial();

                totalTriali++;

                if (PresentTrial == false)
                {
                    break;
                }


                if (PresentTrial == false)
                    break;

                try
                {
                    await Interface_Hold(t_trialHoldMS);

                    await Wait_SelfInitMove();

                    await Interface_Wait4Touch(tMax_ReachTimeMS);

                    Update_FeedbackTrialsInformation();
                }
                catch(TaskCanceledException)
                {
                    Update_FeedbackTrialsInformation();
                }
                    

                if (serialPort_IO8.IsOpen)
                    serialPort_IO8.WriteLine(TDTCmd_InitState);


                // save Trial Information to Txt
                saveTrialInf2Txt();


                if (PresentTrial == false)
                    break;


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


            if (PresentTrial) // Detect the return to startpad timepoint for the last trial
            {
                pressedStartpad = PressedStartpad.No;
                try
                {
                    await Wait_Return2StartPad(1);
                }
                catch (TaskCanceledException)
                {
                    using (StreamWriter file = File.AppendText(file_saved))
                    {
                        file.WriteLine(String.Format("{0, -40}: {1}", "Returned to Startpad TimePoint", timePoint_Startpad_Touch2StartTrial.ToString()));
                    }
                }
            }
        }


        
        private Task Interface_WaitStartTrial()
        {
            /* task for WaitStart interface
             * 
             * Wait for Touching Startpad to trigger a new Trial
             */

            myGrid.Background = brush_BKWaitTrialStart;
            myGrid.UpdateLayout();

            Task task_WaitStart = Task.Run(() =>
            {
                while (PresentTrial && pressedStartpad == PressedStartpad.No) ;


                if (PresentTrial && pressedStartpad == PressedStartpad.Yes)
                {
                    // the time point for startpad touched
                    if (serialPort_IO8.IsOpen)
                        serialPort_IO8.WriteLine(TDTCmd_TouchTriggerTrial);
                    timePoint_Startpad_Touch2StartTrial = globalWatch.ElapsedMilliseconds;
                }

            });

            return task_WaitStart;
        }


        private Task Wait_SelfInitMove()
        {
            /* task for Wait Self-Initiate
             * 
             * Wait for Leaving startpad to Initiate a move
             * 
             * Output:
             *       timePoint_Startpad_Left2InitMove = globalWatch.ElapsedMilliseconds;
             */

            Task task_WaitSelfInit = Task.Run(() =>
            {
                while (PresentTrial && pressedStartpad == PressedStartpad.Yes) ;


                if (PresentTrial && pressedStartpad == PressedStartpad.No)
                {
                    // the time point for leave Startpad
                    if (serialPort_IO8.IsOpen)
                        serialPort_IO8.WriteLine(TDTCmd_LeaveStartpad_InitMove);
                    timePoint_Startpad_Left2InitMove = globalWatch.ElapsedMilliseconds;
                }
            });

            return task_WaitSelfInit;
        }


        private async Task Interface_Wait4Touch(float t_MaxReachTimeMS)
        {
            try
            {
                screenTouchstate = ScreenTouchState.Idle;
                await Wait_Reach(t_MaxReachTimeMS);
                trialExeResult = TrialExeResult.Touched;

                Feedback_Correct();
                await Task.Delay(1000);
            }
            catch (TaskCanceledException)
            {
                Feedback_ERROR();
                await Task.Delay(1000);

                throw new TaskCanceledException("NoTouchWithinTime");
            }
        }


        private async Task Interface_Hold(int t_HoldMS)
        {/* Show the Hold Interface and Call Wait_EnoughHold
            * 
            * Output:
            *   throw new TaskCanceledException(ex.Message, ex) from Wait_EnoughTouch(t_HoldMS)
            */

            try
            {
                // Set background, write to TDTcmd and record timepoint
                myGrid.Background = brush_BKHold;
                if (PresentTrial && serialPort_IO8.IsOpen)
                    serialPort_IO8.WriteLine(TDTCmd_HoldInterfaceShown);
                timePoint_Interface_HoldOnset = globalWatch.ElapsedMilliseconds;

                if (!PresentTrial)
                    return;

                // Wait Startpad Hold Enough Time
                await Wait_EnoughHold(t_HoldMS);
                if (!PresentTrial)
                    return;

            }
            catch (TaskCanceledException ex)
            {
                if (!PresentTrial)
                    return;

                Feedback_ERROR();
                if (!PresentTrial)
                    return;

                await Task.Delay(t_VisfeedbackShowMS);

                throw new TaskCanceledException(ex.Message, ex);
            }
        }

        private Task Wait_EnoughHold(int t_EnoughTouchMS)
        {
            /* 
             * Wait for Enough Touch Time (ms)
             * 
             * Input: 
             *    t_EnoughTouch: the required Touch time (ms)  
             *    
             * Output:
             *      startpadHoldstate = StartPadHoldState.HoldEnough;
             *    or
             *      throw new TaskCanceledException("HoldTooShort");
             */

            // start a task and return it
            return Task.Run(() =>
            {
                Stopwatch touchedWatch = new Stopwatch();
                touchedWatch.Restart();

                startpadHoldstate = StartPadHoldState.HoldTooShort;
                while (PresentTrial && pressedStartpad == PressedStartpad.Yes && startpadHoldstate != StartPadHoldState.HoldEnough)
                {
                    if (touchedWatch.ElapsedMilliseconds >= t_EnoughTouchMS)
                    {/* touched with enough time */
                        startpadHoldstate = StartPadHoldState.HoldEnough;
                    }
                }
                touchedWatch.Stop();

                if (PresentTrial && startpadHoldstate == StartPadHoldState.HoldTooShort)
                {
                    if (serialPort_IO8.IsOpen)
                        serialPort_IO8.WriteLine(TDTCmd_HoldTooShort);
                    trialExeResult = TrialExeResult.HoldTooShort;
                    timePoint_Startpad_LeftEarly = globalWatch.ElapsedMilliseconds;

                    shortHoldTrialNum++;

                    throw new TaskCanceledException("HoldTooShort");
                }

            });
        }



        private  Task Wait_Reach(float tMax_WaitMS)
        {/* Wait for Reach within tMax_WaitMS
          * 
          *  Output:
          *        positions and touch time of all touch points 
          *     or
          *         throw new TaskCanceledException("goReachTimeToolong");
          */

            return Task.Run(() =>
            {
                Stopwatch waitWatch = new Stopwatch();
                waitWatch.Start();
                screenTouchstate = ScreenTouchState.Idle;
                while (PresentTrial && screenTouchstate == ScreenTouchState.Idle)
                {
                    if (PresentTrial && waitWatch.ElapsedMilliseconds >= tMax_WaitMS) // No Screen Touched within tMax_WaitMS
                    {
                        waitWatch.Stop();
                        noreachGoTrialNum++;
                        trialExeResult = TrialExeResult.ReachTimeToolong;

                        throw new TaskCanceledException("goReachTimeToolong");
                    }
                }
            });
        }


        private void Feedback_Correct()
        {
            // Visual Feedback
            myGrid.Background = brush_BKCorrect;
            myGrid.UpdateLayout();


            //Juicer Feedback
            giveJuicerState = GiveJuicerState.CorrectGiven;

            // Audio Feedback
            player_Correct.Play()
;
        }

        private void Feedback_ERROR()
        {
            // Visual Feedback
            myGrid.Background = brush_BKError;
            myGrid.UpdateLayout();


            //Juicer Feedback
            giveJuicerState = GiveJuicerState.No;

            // Audio Feedback
            player_Error.Play()
;
        }



        private void reset_TrialParas()
        {
            timePoint_Startpad_Touch2StartTrial = 0;
            timePoint_Startpad_LeftEarly = 0;
            timePoint_Startpad_Left2InitMove = 0;
            touchPoints_PosTime.Clear();
            trialExeResult = TrialExeResult.Idle;
            t_trialHoldMS = 0;
        }

        void saveTrialInf2Txt()
        {
            /*-------- Write Trial Information ------*/
            List<String> strExeSubResult = new List<String>();
            strExeSubResult.Add("HoldTooShort");
            strExeSubResult.Add("ReachTimeToolong");
            strExeSubResult.Add("Touched");
            String strExeFail = "Failed";
            String strExeSuccess = "Success";

            using (StreamWriter file = File.AppendText(file_saved))
            {
                decimal ms2sRatio = 1000;


                /* Current Trial Written Inf*/
                file.WriteLine("\n");

                file.WriteLine(String.Format("{0, -40}: {1}", "TrialNum", totalTriali.ToString()));
                file.WriteLine(String.Format("{0, -40}: {1}", "Startpad Touched to Start TimePoint", timePoint_Startpad_Touch2StartTrial.ToString()));
                file.WriteLine(String.Format("{0, -40}: {1}", "Current trial Required Holding Time", t_trialHoldMS.ToString()));

                
                // trialExeResult
                if (trialExeResult == TrialExeResult.HoldTooShort)
                {
                    file.WriteLine(String.Format("{0, -40}: {1}", "Startpad Left Early TimePoint", timePoint_Startpad_LeftEarly.ToString()));
                    file.WriteLine(String.Format("{0, -40}: {1}, {2}", "Trial Result", strExeFail, strExeSubResult[0]));
                }
                else if (trialExeResult == TrialExeResult.ReachTimeToolong)
                {
                    file.WriteLine(String.Format("{0, -40}: {1}", "Startpad Left to Self-Initiate a Reach", timePoint_Startpad_Left2InitMove.ToString()));
                    file.WriteLine(String.Format("{0, -40}: {1}, {2}", "Trial Result", strExeFail, strExeSubResult[1]));
                }

                else if (trialExeResult == TrialExeResult.Touched)
                {
                    file.WriteLine(String.Format("{0, -40}: {1}", "Startpad Left to Self-Initiate a Reach", timePoint_Startpad_Left2InitMove.ToString()));


                    //  touched  timepoint and (x, y position) of all touch points
                    for (int pointi = 0; pointi < touchPoints_PosTime.Count; pointi++)
                    {
                        double[] downPoint = touchPoints_PosTime[pointi];

                        // touched pointi touchpoint
                        file.WriteLine(String.Format("{0, -40}: {1, -40}", "Touch Point " + pointi.ToString() + " TimePoint", downPoint[1].ToString()));

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
                   
                    file.WriteLine(String.Format("{0, -40}: {1}, {2}", "Trial Result", strExeSuccess, strExeSubResult[2]));

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
                file.WriteLine(String.Format("{0, -40}: {1}", "No Reach Within Max Reach Time Trials", noreachGoTrialNum.ToString()));
                file.WriteLine(String.Format("{0, -40}: {1}", "Short Hold Trials", shortHoldTrialNum.ToString()));
            }

        }


        public void Update_FeedbackTrialsInformation()
        {/* Update the Feedback Trial Information in the Mainwindow */

            // Go trials
            parent.textBox_totalGoTrialNum.Text = totalTrialNum.ToString();
            parent.textBox_successGoTrialNum.Text = successTrialNum.ToString();
            parent.textBox_noreachGoTrialNum.Text = noreachGoTrialNum.ToString();
            parent.textBox_shortHoldTrialNum.Text = shortHoldTrialNum.ToString();
        }

        private void Init_FeedbackTrialsInformation()
        {/* Update the Feedback Trial Information in the Mainwindow */


            totalTrialNum = 0;
            successTrialNum = 0;
            noreachGoTrialNum = 0;
            shortHoldTrialNum = 0;

            // Update Main Window Feedback 
            parent.textBox_totalGoTrialNum.Text = totalTrialNum.ToString();
            parent.textBox_successGoTrialNum.Text = successTrialNum.ToString();
            parent.textBox_noreachGoTrialNum.Text = noreachGoTrialNum.ToString();
            parent.textBox_shortHoldTrialNum.Text = shortHoldTrialNum.ToString();

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
            tMax_ReachTimeMS = parent.tMax_ReachTimeS * 1000;
            t_VisfeedbackShowMS = (Int32)(parent.t_VisfeedbackShowS * 1000);
            t_InterTrialMS = (Int32)(parent.t_InterTrialS * 1000);

            // Juicer Time
            t_JuicerCorrectGiven = (Int32)(parent.t_JuicerCorrectGivenS * 1000);



            /* ---- Get all the Set Colors ----- */
            Color selectedColor;

            // Wait for Starting a trial 
            selectedColor = (Color)(typeof(Colors).GetProperty(parent.BKWaitTrialColorStr) as PropertyInfo).GetValue(null, null);
            brush_BKWaitTrialStart = new SolidColorBrush(selectedColor);
            brush_BDWaitTrialStart = brush_BKWaitTrialStart;


            // Hold Background
            selectedColor = (Color)(typeof(Colors).GetProperty(parent.BKHoldColorStr) as PropertyInfo).GetValue(null, null);
            brush_BKHold = new SolidColorBrush(selectedColor);


            // Correct Background
            selectedColor = (Color)(typeof(Colors).GetProperty(parent.BKCorrectColorStr) as PropertyInfo).GetValue(null, null);
            brush_BKCorrect = new SolidColorBrush(selectedColor);


            // Error Background
            selectedColor = (Color)(typeof(Colors).GetProperty(parent.BKErrorColorStr) as PropertyInfo).GetValue(null, null);
            brush_BKError = new SolidColorBrush(selectedColor);



            // get the file for saving 
            file_saved = parent.file_saved;
            audioFile_Correct = parent.textBox_audioFile_Correct.Text;
            audioFile_Error = parent.textBox_audioFile_Error.Text;
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
                                timePoint_Startpad_LeftEarly = globalWatch.ElapsedMilliseconds;
                                serialPort_IO8.WriteLine(TDTCmd_LeaveStartpad_Early);
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
