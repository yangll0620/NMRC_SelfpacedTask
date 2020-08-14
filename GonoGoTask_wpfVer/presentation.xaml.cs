using System;
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

namespace GonoGoTask_wpfVer
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
            Nogo,
            Go,
        }

        public enum InterfaceState
        {
            beforeStart,
            Ready,
            TargetCue,
            GoNogo,
            Reward,
        }

        public enum TrialExeResult
        {
            readyWaitTooShort,
            cueWaitTooShort,
            goReactionTimeToolong,
            goReachTimeToolong,
            goHit,
            goClose,
            goMiss,
            nogoMoved,
            nogoSuccess
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
        int crossingLineThickness = 3;
        int disFromCenter, disXFromCenter, disYFromCenter;

        TargetType targetType;

        // randomized Go noGo tag list, tag_gonogo ==1: go, ==0: nogo
        List<TargetType> targetType_List = new List<TargetType>();
        // randomized t_Ready list
        List<float> t_Ready_List = new List<float>();
        // randomized t_Cue list
        List<float> t_Cue_List = new List<float>();
        // randomized t_Cue list
        List<float> t_noGoShow_List = new List<float>();


        // objects of Go cirle, nogo Rectangle, lines of the crossing, and two white points
        Ellipse circleGo, circleGoClose;
        Rectangle rectNogo;
        Line vertLine, horiLine;
        Ellipse point1, point2;

        // ColorBrushes 
        private SolidColorBrush brush_goCircle, brush_nogoRect;
        private SolidColorBrush brush_BKWaitTrialStart, brush_BKTrial;
        private SolidColorBrush brush_CorrectFill, brush_CorrOutline, brush_ErrorFill, brush_ErrorOutline;
        private SolidColorBrush brush_CloseFill;
        private SolidColorBrush brush_BDWaitTrialStart;
        private SolidColorBrush brush_CueCrossing;

        Point circleGo_centerPoint; // the center of circleGo 
        double circleGo_radius; // the radius of circleGO
        double circleGoClose_radius; // the radius of circleGO

        // audio feedback
        private string audioFile_Correct, audioFile_Error;
        System.Media.SoundPlayer player_Correct, player_Error;


        // name of all the objects
        string name_circleGo = "circleGo";
        string name_circleGoClose = "circleGoClose";
        string name_rectNogo = "rectNogo";
        string name_vLine = "vLine", name_hLine = "hLine";
        string name_point1 = "wpoint1", name_point2 = "wpoint2";

        // all the optional positions, in which one is for target, the remaining for white points
        List<int[]> optPostions_List = new List<int[]>();

        // randomized target Position index list for each trial
        List<int> targetPosInd_List = new List<int>();
        // the remaining Position indices (for two while points) list for each trial
        List<int[]> otherPosInds_List = new List<int[]>();



        // Wait Time Range for Each Event, and Max Reaction and Reach Time
        float[] tRange_ReadyTime, tRange_CueTime, tRange_NogoShowTime;
        float tMax_ReactionTimeMS, tMax_ReachTimeMS; 
        Int32 t_VisfeedbackShow; // Visual Feedback Show Time (ms)

        bool PresentTrial;

        // time stamp
        long timestamp_0;


        // set storing the touch point id (no replicates)
        HashSet<int> touchPoints_Id = new HashSet<int>();

        // list storing the position/Timepoint of the touch points when touched down
        List<double[]> downPoints_Pos = new List<double[]>();
        // list storing the position/Timepoint of the touch points when touched down
        List<double[]> downPoints_PosTime = new List<double[]>();
        // Stop Watch for recording the time interval between the first touchpoint and the last touchpoint within One Touch
        Stopwatch tpoints1TouchWatch;
        // the Max Duration for One Touch (ms)
        long tMax_1Touch = 40;
        GoTargetTouchState gotargetTouchstate;

        String calcTouchStateString;

        // Executed Trial Information
        public int totalGoTrialNum, successGoTrialNum;
        public int missGoTrialNum, noreactionGoTrialNum, noreachGoTrialNum;
        public int totalNogoTrialNum, successNogoTrialNum;

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
        long timePoint_Interface_ReadyOnset, timePoint_Interface_CueOnset, timePoint_Interface_TargetOnset;



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

            // Set the Optional Positions in Global Variable optPositions_List
            SetOptionalPostions();


            //shuffle go and nogo trials
            Shuffle_GonogoTrials(Int32.Parse(parent.textBox_goTrialNum.Text), Int32.Parse(parent.textBox_nogoTrialNum.Text));



            // Create necessary elements: go circle, nogo rect, two white points and one crossing
            Create_GoCircle();
            Create_NogoRect();
            Create_TwoWhitePoints();
            Create_OneCrossing();

            // Set audio Feedback related members 
            SetAudioFeedback();

            PrepBef_Present();
        }

        private void PrepBef_Present()
        {
            // Write Head Inf 
            WriteHeaderInf();

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


        public async void Present_Start()
        {                 
            float t_Cue, t_Ready, t_noGoShow;
            int[] pos_Taget;
            int targetPosInd;

            Random r = new Random();
            int randomIndex;

            // Present Each Trial
            globalWatch.Restart();
            thread_ReadWrite_IO8.Start();
            timestamp_0 = DateTime.Now.Ticks;
            int totalTriali = 0;
            PresentTrial = true;
            while (targetType_List.Count > 0 && PresentTrial)
            {
                // choose a random index in list targetType_List
                randomIndex = r.Next(0, targetType_List.Count);

                // Extract trial parameters, always using the trial at index 0
                targetType = targetType_List[randomIndex];
                targetPosInd = targetPosInd_List[randomIndex];
                pos_Taget = optPostions_List[targetPosInd];
                t_Cue = t_Cue_List[randomIndex];
                t_Ready = t_Ready_List[randomIndex];
                t_noGoShow = t_noGoShow_List[randomIndex];


                totalTriali++;


                /*----- WaitStartTrial Interface ------*/
                pressedStartpad = PressedStartpad.No;
                await Interface_WaitStartTrial();


                /*-------- Trial Interfaces -------*/
                try
                {
                    // Ready Interface
                    await Interface_Ready(t_Ready);

                    // Cue Interface
                    await Interface_Cue(t_Cue, pos_Taget);

                    // Go or noGo Target Interface
                    if (targetType == TargetType.Go)
                    {
                        await Interface_Go(pos_Taget);
                    }
                    else
                    {
                        await Interface_noGo(t_noGoShow, pos_Taget);
                    }

                    Update_FeedbackTrialsInformation();
                    Remove_All();
                }
                catch (TaskCanceledException)
                {
                    Update_FeedbackTrialsInformation();
                    Remove_All();
                }


                if(trialExeResult == TrialExeResult.goClose | trialExeResult == TrialExeResult.goHit | trialExeResult == TrialExeResult.nogoSuccess)
                {
                    // Remove the trial setting only when success
                    targetType_List.RemoveAt(randomIndex);
                    targetPosInd_List.RemoveAt(randomIndex);
                    t_Cue_List.RemoveAt(randomIndex);
                    t_Ready_List.RemoveAt(randomIndex);
                    t_noGoShow_List.RemoveAt(randomIndex);
                }

                /*-------- Write Trial Information ------*/
                List<String> strExeSubResult = new List<String>();
                strExeSubResult.Add("readyWaitTooShort");
                strExeSubResult.Add("cueWaitTooShort");
                strExeSubResult.Add("goReactionTimeToolong");
                strExeSubResult.Add("goReachTimeToolong");
                strExeSubResult.Add("goMiss");
                strExeSubResult.Add("goSuccess");
                strExeSubResult.Add("nogoMoved");
                strExeSubResult.Add("nogoSuccess");
                String strExeFail = "Failed";
                String strExeSuccess = "Success";
                using (StreamWriter file = File.AppendText(file_saved))
                {

                    if (totalTriali > 1)
                    { // Startpad touched in trial i+1 treated as the return point as in trial i        

                        file.WriteLine(String.Format("{0, -40}: {1}", "Returned to Startpad TimePoint(ms)", timePoint_StartpadTouched.ToString()));
                    }


                    /* Current Trial Written Inf*/

                    file.WriteLine("\n");

                    // Trial Num
                    file.WriteLine(String.Format("{0, -40}: {1}", "TrialNum", totalTriali.ToString()));
                    
                    // the timepoint when touching the startpad to initial a new trial
                    file.WriteLine(String.Format("{0, -40}: {1}", "Startpad Touched TimePoint(ms)", timePoint_StartpadTouched.ToString()));
                    
                    // Start Interface showed TimePoint
                    file.WriteLine(String.Format("{0, -40}: {1}", "Ready Start TimePoint (ms)", timePoint_Interface_ReadyOnset.ToString()));
                    
                    // Ready Time
                    file.WriteLine(String.Format("{0, -40}: {1}", "Ready Interface Time (s)", t_Ready.ToString()));

                    
                    // Various Cases
                    if (trialExeResult == TrialExeResult.readyWaitTooShort)
                    {// case: ready WaitTooShort
                        
                        // Left startpad early during ready
                        file.WriteLine(String.Format("{0, -40}: {1}", "Startpad Left TimePoint(ms)", timePoint_StartpadLeft.ToString()));

                        // trial exe result : success or fail
                        file.WriteLine(String.Format("{0, -40}: {1}, {2}", "Trial Result", strExeFail, strExeSubResult[0]));
                    }
                    else if (trialExeResult == TrialExeResult.cueWaitTooShort)
                    {// case: Cue WaitTooShort

                        // Cue Interface Timepoint, Cue Time and Left startpad early during Cue
                        file.WriteLine(String.Format("{0, -40}: {1}", "Cue Start TimePoint (ms)", timePoint_Interface_CueOnset.ToString()));
                        file.WriteLine(String.Format("{0, -40}: {1}", "Cue Interface Time (s)", t_Cue.ToString()));
                        file.WriteLine(String.Format("{0, -40}: {1}", "Startpad Left TimePoint(ms)", timePoint_StartpadLeft.ToString()));

                        // trial exe result : success or fail
                        file.WriteLine(String.Format("{0, -40}: {1}, {2}", "Trial Result", strExeFail, strExeSubResult[1]));
                    }
                    else if (trialExeResult == TrialExeResult.goReactionTimeToolong)
                    {// case : goReactionTimeToolong 

                        // Cue Interface Timepoint and Cue Time
                        file.WriteLine(String.Format("{0, -40}: {1}", "Cue Start TimePoint (ms)", timePoint_Interface_CueOnset.ToString()));
                        file.WriteLine(String.Format("{0, -40}: {1}", "Cue Interface Time (s)", t_Cue.ToString()));

                        // Cue Interface Timepoint, Target type: Go, and Target position index: 0 (1, 2)
                        file.WriteLine(String.Format("{0, -40}: {1}", "Target Start TimePoint (ms)", timePoint_Interface_TargetOnset.ToString()));
                        file.WriteLine(String.Format("{0, -40}: {1}", "TargetType", targetType.ToString()));
                        file.WriteLine(String.Format("{0, -40}: {1}", "TargetPositionIndex", targetPosInd.ToString()));


                        // trial exe result : success or fail
                        file.WriteLine(String.Format("{0, -40}: {1}, {2}", "Trial Result", strExeFail, strExeSubResult[2]));
                    }
                    else if (trialExeResult == TrialExeResult.goReachTimeToolong)
                    {// case : goReachTimeToolong

                        // Cue Interface Timepoint and Cue Time
                        file.WriteLine(String.Format("{0, -40}: {1}", "Cue Start TimePoint (ms)", timePoint_Interface_CueOnset.ToString()));
                        file.WriteLine(String.Format("{0, -40}: {1}", "Cue Interface Time (s)", t_Cue.ToString()));

                        // Cue Interface Timepoint, Target type: Go, and Target position index: 0 (1, 2)
                        file.WriteLine(String.Format("{0, -40}: {1}", "Target Start TimePoint (ms)", timePoint_Interface_TargetOnset.ToString()));
                        file.WriteLine(String.Format("{0, -40}: {1}", "TargetType", targetType.ToString()));
                        file.WriteLine(String.Format("{0, -40}: {1}", "TargetPositionIndex", targetPosInd.ToString()));
                        // Target interface:  Left Startpad Time Point
                        file.WriteLine(String.Format("{0, -40}: {1}", "Startpad Left TimePoint(ms)", timePoint_StartpadLeft.ToString()));


                        // trial exe result : success or fail
                        file.WriteLine(String.Format("{0, -40}: {1}, {2}", "Trial Result", strExeFail, strExeSubResult[3]));
                    }
                    else if (trialExeResult == TrialExeResult.goClose | trialExeResult == TrialExeResult.goHit | trialExeResult == TrialExeResult.goMiss)
                    {// case: Go success (goClose or goHit) or goMiss

                        // Cue Interface Timepoint and Cue Time
                        file.WriteLine(String.Format("{0, -40}: {1}", "Cue Start TimePoint (ms)", timePoint_Interface_CueOnset.ToString()));
                        file.WriteLine(String.Format("{0, -40}: {1}", "Cue Interface Time (s)", t_Cue.ToString()));

                        // Cue Interface Timepoint, Target type: Go, and Target position index: 0 (1, 2)
                        file.WriteLine(String.Format("{0, -40}: {1}", "Target Start TimePoint (ms)", timePoint_Interface_TargetOnset.ToString()));
                        file.WriteLine(String.Format("{0, -40}: {1}", "TargetType", targetType.ToString()));
                        file.WriteLine(String.Format("{0, -40}: {1}", "TargetPositionIndex", targetPosInd.ToString()));

                        // Target interface:  Left Startpad Time Point
                        file.WriteLine(String.Format("{0, -40}: {1}", "Startpad Left TimePoint(ms)", timePoint_StartpadLeft.ToString()));
                        //  Target interface:  touch timepoint and (x, y position) of all touch points
                        for (int pointi = 0; pointi < downPoints_PosTime.Count; pointi++)
                        {
                            double[] downPoint = downPoints_PosTime[pointi];
                            String downPointstr = downPoint[0].ToString() + " (" + downPoint[1].ToString() + ", " + downPoint[2].ToString() + ")";
                            file.WriteLine(String.Format("{0}{1, -40}: {2}", "Touch Point", pointi.ToString() + " TimePoint (X, Y Position)", downPointstr));
                        }


                        // trial exe result : success or fail
                        if (trialExeResult == TrialExeResult.goMiss)
                            file.WriteLine(String.Format("{0, -40}: {1}, {2}", "Trial Result", strExeFail, strExeSubResult[4]));
                        else
                            file.WriteLine(String.Format("{0, -40}: {1}, {2}", "Trial Result", strExeSuccess, strExeSubResult[5]));

                    }
                    else if (trialExeResult == TrialExeResult.nogoMoved)
                    { // case: noGo moved 

                        // Cue Interface Timepoint and Cue Time
                        file.WriteLine(String.Format("{0, -40}: {1}", "Cue Start TimePoint (ms)", timePoint_Interface_CueOnset.ToString()));
                        file.WriteLine(String.Format("{0, -40}: {1}", "Cue Interface Time (s)", t_Cue.ToString()));

                        // Cue Interface Timepoint, Target type: Go, and Target position index: 0 (1, 2)
                        file.WriteLine(String.Format("{0, -40}: {1}", "Target Start TimePoint (ms)", timePoint_Interface_TargetOnset.ToString()));
                        file.WriteLine(String.Format("{0, -40}: {1}", "TargetType", targetType.ToString()));
                        file.WriteLine(String.Format("{0, -40}: {1}", "TargetPositionIndex", targetPosInd.ToString()));

                        // Target nogo interface show time
                        file.WriteLine(String.Format("{0, -40}: {1}", "Nogo Interface Show Time(s)", t_noGoShow.ToString()));

                        // Target interface:  Left Startpad Time Point
                        file.WriteLine(String.Format("{0, -40}: {1}", "Startpad Left TimePoint(ms)", timePoint_StartpadLeft.ToString()));



                        // trial exe result : success or fail
                        file.WriteLine(String.Format("{0, -40}: {1}, {2}", "Trial Result", strExeFail, strExeSubResult[6]));

                    }
                    else if (trialExeResult == TrialExeResult.nogoSuccess)
                    { // case: noGo success 

                        // Cue Interface Timepoint and Cue Time
                        file.WriteLine(String.Format("{0, -40}: {1}", "Cue Start TimePoint (ms)", timePoint_Interface_CueOnset.ToString()));
                        file.WriteLine(String.Format("{0, -40}: {1}", "Cue Interface Time (s)", t_Cue.ToString()));

                        // Cue Interface Timepoint, Target type: Go, and Target position index: 0 (1, 2)
                        file.WriteLine(String.Format("{0, -40}: {1}", "Target Start TimePoint (ms)", timePoint_Interface_TargetOnset.ToString()));
                        file.WriteLine(String.Format("{0, -40}: {1}", "TargetType", targetType.ToString()));
                        file.WriteLine(String.Format("{0, -40}: {1}", "TargetPositionIndex", targetPosInd.ToString()));
                        // Target nogo interface show time
                        file.WriteLine(String.Format("{0, -40}: {1}", "Nogo Interface Show Time(s)", t_noGoShow.ToString()));


                        // trial exe result : success or fail
                        file.WriteLine(String.Format("{0, -40}: {1}, {2}", "Trial Result", strExeSuccess, strExeSubResult[7]));
                    }

                }
            }

            if (targetType_List.Count == 0)
            {// Finished all Trials

                // show different Border Color demonstrating the endof exp
                myGridBorder.BorderBrush = brush_CorrectFill;

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
                        file.WriteLine(String.Format("{0, -40}: {1}", "Returned to Startpad TimePoint(ms)", timePoint_StartpadTouched.ToString()));
                    }
                }
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


            // Nogo Trials
            parent.textBox_successNogoTrialNum.Text = successNogoTrialNum.ToString();
            parent.textBox_totalNogoTrialNum.Text = totalNogoTrialNum.ToString();
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

        private void SetOptionalPostions()
        {/*Set the Optional Target Postions 
            the Global Variable: 
                optPositions_List
         */

            // Get the Position of the Screen Center Point
            int screenCenter_X = (int)wholeGrid.ActualWidth / 2;
            int screenCenter_Y = (int)wholeGrid.ActualHeight / 2;


            disXFromCenter = disFromCenter;
            disYFromCenter = disFromCenter;
            optPostions_List.Add(new int[] { screenCenter_X - disXFromCenter, screenCenter_Y }); // left position
            optPostions_List.Add(new int[] { screenCenter_X, screenCenter_Y - disYFromCenter }); // top position
            optPostions_List.Add(new int[] { screenCenter_X + disXFromCenter, screenCenter_Y }); // right position
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


        private void Shuffle_GonogoTrials(int gotrialnum, int nogotrialnum)
        {/* ---- 
            1. shuffle go and nogo trials, present in member variable taglist_gonogo 
            2. Generate the random t_Ready, and t_Cue for each trial, stored in t_Ready_List and t_Cue_List;
            3. Select the target position index of the optional positions, stored in targetPosInd_List and otherPosInds_List
             */

            // create orderred gonogo list
            List<TargetType> tmporder_go = new List<TargetType>(Enumerable.Repeat(TargetType.Go, gotrialnum));
            List<TargetType> tmporder_nogo = new List<TargetType>(Enumerable.Repeat(TargetType.Nogo, nogotrialnum));
            List<TargetType> tmporder_gonogo = tmporder_go.Concat(tmporder_nogo).ToList();


            // shuffle 
            Random r = new Random();
            int randomIndex;
            

            while (tmporder_gonogo.Count > 0)
            {
                // choose a random index in list tmporder_gonogo
                randomIndex = r.Next(0, tmporder_gonogo.Count);

                // add the selected value (go/nogo type) into tagarray_gonogo
                targetType_List.Add(tmporder_gonogo[randomIndex]);


                // add the corresponding go/noGo object and the two white points position
                int targetPosInd = r.Next(0, 3);
                int[] otherPosInds = new int[] { 0, 1, 2};
                otherPosInds = otherPosInds.Where(w => w != targetPosInd).ToArray();
                targetPosInd_List.Add(targetPosInd);
                otherPosInds_List.Add(otherPosInds);

                // generate a random t_Ready and t_Cue, and and them into t_Ready_List and t_Cue_List individually
                t_Cue_List.Add(TransferTo((float)r.NextDouble(), tRange_CueTime[0], tRange_CueTime[1]));
                t_Ready_List.Add(TransferTo((float)r.NextDouble(), tRange_ReadyTime[0], tRange_ReadyTime[1]));
                t_noGoShow_List.Add(TransferTo((float)r.NextDouble(), tRange_NogoShowTime[0], tRange_NogoShowTime[1]));

                //remove this value
                tmporder_gonogo.RemoveAt(randomIndex);
            }
        }


        private void GetSetupParameters()
        {/* get the setup from the parent interface */

            // object size and distance parameters
            objdiameter = Utility.in2pixal(parent.targetDiameterInch);
            disFromCenter = Utility.in2pixal(parent.targetDisFromCenterInch);
            closediameter = (int)(objdiameter * (1 + parent.closeMarginPercentage / 100));

            // interfaces time related parameters
            tRange_ReadyTime = parent.tRange_ReadyTime;
            tRange_CueTime = parent.tRange_CueTime;
            tRange_NogoShowTime = parent.tRange_NogoShowTime;
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
            brush_goCircle = new SolidColorBrush(selectedColor);

            // nogoRect Color
            selectedColor = (Color)(typeof(Colors).GetProperty(parent.nogoColorStr) as PropertyInfo).GetValue(null, null);
            brush_nogoRect = new SolidColorBrush(selectedColor);

            // Cue Crossing Color
            selectedColor = (Color)(typeof(Colors).GetProperty(parent.cueColorStr) as PropertyInfo).GetValue(null, null);
            brush_CueCrossing = new SolidColorBrush(selectedColor);


            // Wait Background 
            selectedColor = (Color)(typeof(Colors).GetProperty(parent.BKWaitTrialColorStr) as PropertyInfo).GetValue(null, null);
            brush_BKWaitTrialStart = new SolidColorBrush(selectedColor);
            // Wait Boarder
            brush_BDWaitTrialStart = brush_BKWaitTrialStart;

            // Trial Background
            selectedColor = (Color)(typeof(Colors).GetProperty(parent.BKTrialColorStr) as PropertyInfo).GetValue(null, null);
            brush_BKTrial = new SolidColorBrush(selectedColor);

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


        private void WriteHeaderInf()
        {
            // save
            using (StreamWriter file = File.AppendText(file_saved))
            {
                
                // Store all the optional positions
                file.WriteLine("\n");
                for (int i=0; i< optPostions_List.Count; i++)
                {
                    int[] position = optPostions_List[i];
                    file.WriteLine(String.Format("{0}{1, -10}:{2}, {3}", "Optional Postion ", i, position[0], position[1]));
                }


                file.WriteLine("\n\n\n");

                file.WriteLine("Trial Information:");
            }
        }

        private void Create_GoCircle()
        {/*
            Create the go circle: circleGo

            */

            // Create an Ellipse  
            circleGo = new Ellipse();
                   
            circleGo.Fill = brush_goCircle;

            // set the size, position of circleGo
            circleGo.Height = objdiameter;
            circleGo.Width = objdiameter;
            circleGo.VerticalAlignment = VerticalAlignment.Top;
            circleGo.HorizontalAlignment = HorizontalAlignment.Left;

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

        private void Add_GoCircle(int[] centerPoint_Pos)
        {/*show the Go Circle with Circle Center at (centerPoint_Pos[0], centerPoint_Pos[1]) */

            int centerPoint_X = centerPoint_Pos[0], centerPoint_Y = centerPoint_Pos[1];

            double left = centerPoint_X - circleGo.Width / 2;
            double top = centerPoint_Y - circleGo.Height / 2;
            circleGo.Margin = new Thickness(left, top, 0, 0);

            circleGo.Fill = brush_goCircle;
            circleGo.Visibility = Visibility.Visible;
            circleGo.IsEnabled = true;
            myGrid.UpdateLayout();


            // get the center point and the radius of circleGo
            circleGo_centerPoint = circleGo.TransformToAncestor(this).Transform(new Point(circleGo.Width / 2, circleGo.Height / 2));
            circleGo_radius = ((circleGo.Height + circleGo.Width) / 2) / 2;


            // add the go close circle
            Add_GoCircleClose(centerPoint_Pos);
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

            circleGoClose.Stroke = brush_goCircle;

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
            circleGoClose_radius = ((circleGoClose.Height + circleGoClose.Width) / 2) / 2;
        }
        private void Remove_GoCircleClose()
        {
            circleGoClose.Visibility = Visibility.Hidden;
            circleGo.IsEnabled = false;
            myGrid.UpdateLayout();
        }


        private void Create_NogoRect()
        {/*Create the red nogo rectangle: rectNogo*/

            // Create an Ellipse  
            rectNogo = new Rectangle();

            rectNogo.Fill = brush_nogoRect;

            // set the size, position of circleGo
            int square_width = objdiameter;
            int square_height = objdiameter;
            rectNogo.Height = square_height;
            rectNogo.Width = square_width;
            rectNogo.VerticalAlignment = VerticalAlignment.Top;
            rectNogo.HorizontalAlignment = HorizontalAlignment.Left;

            // name
            rectNogo.Name = name_rectNogo;

            // hidden and not enabled at first
            rectNogo.Visibility = Visibility.Hidden;
            rectNogo.IsEnabled = false;

            // add to myGrid   
            myGrid.Children.Add(rectNogo);
            myGrid.RegisterName(rectNogo.Name, rectNogo);
            myGrid.UpdateLayout();
        }

        private void Add_NogoRect(int[] centerPoint_Pos)
        {/*show the Nogo Rectangle with Rectangle Center at (centerPoint_Pos[0], centerPoint_Pos[1]) */

            int centerPoint_X = centerPoint_Pos[0], centerPoint_Y = centerPoint_Pos[1];

            double left = centerPoint_X - circleGo.Width / 2;
            double top = centerPoint_Y - circleGo.Height / 2;
            rectNogo.Margin = new Thickness(left, top, 0, 0);

            rectNogo.Fill = brush_nogoRect;
            rectNogo.Visibility = Visibility.Visible;
            rectNogo.IsEnabled = true;
            myGrid.UpdateLayout();
        }

        private void Remove_NogoRect()
        {
            rectNogo.Visibility = Visibility.Hidden;
            rectNogo.IsEnabled = false;
            myGrid.UpdateLayout();
        }


        private void Create_TwoWhitePoints()
        {/* Create draw the two write points: point1, point2 */

            // Create a while Brush    
            SolidColorBrush whiteBrush = new SolidColorBrush();
            whiteBrush.Color = Colors.White;

            // the left white point
            point1 = new Ellipse();
            point1.Fill = whiteBrush;
            point1.Height = wpoints_radius;
            point1.Width = wpoints_radius;
            point1.HorizontalAlignment = HorizontalAlignment.Center;
            point1.VerticalAlignment = VerticalAlignment.Center;
            

            point1.Name = name_point1;

            point1.Visibility = Visibility.Hidden;
            point1.IsEnabled = false;
            myGrid.Children.Add(point1);
            myGrid.RegisterName(point1.Name, point1);


            // the top white point
            point2 = new Ellipse();
            point2.Fill = whiteBrush;
            point2.Height = wpoints_radius;
            point2.Width = wpoints_radius;
            point2.HorizontalAlignment = HorizontalAlignment.Center;
            point2.VerticalAlignment = VerticalAlignment.Center;
            

            point2.Name = name_point2;
            point2.Visibility = Visibility.Hidden;
            point2.IsEnabled = false;
            myGrid.Children.Add(point2);
            myGrid.RegisterName(point2.Name, point2);
            myGrid.UpdateLayout();



        }


        private void Remove_TwoWhitePoints()
        {// add nogo rectangle to myGrid

            point1.Visibility = Visibility.Hidden;
            point2.Visibility = Visibility.Hidden;

            myGrid.UpdateLayout();
        }

        private void Create_OneCrossing()
        {/*create the crossing cue*/

            // the line length of the crossing
            int len = objdiameter;

            // Create a while Brush    


            // Create the horizontal line
            horiLine = new Line();
            horiLine.X1 = 0;
            horiLine.Y1 = 0;
            horiLine.X2 = len;
            horiLine.Y2 = horiLine.Y1;        
            
            // horizontal line position
            horiLine.HorizontalAlignment = HorizontalAlignment.Left;
            horiLine.VerticalAlignment = VerticalAlignment.Top;
            
            // horizontal line color
            horiLine.Stroke = brush_CueCrossing;
            // horizontal line stroke thickness
            horiLine.StrokeThickness = crossingLineThickness;
            // name
            horiLine.Name = name_hLine;
            horiLine.Visibility = Visibility.Hidden;
            horiLine.IsEnabled = false;
            myGrid.Children.Add(horiLine);
            myGrid.RegisterName(horiLine.Name, horiLine);


            // Create the vertical line
            vertLine = new Line();
            vertLine.X1 = 0;
            vertLine.Y1 = 0;
            vertLine.X2 = vertLine.X1;
            vertLine.Y2 = len;
            // vertical line position
            vertLine.HorizontalAlignment = HorizontalAlignment.Left;
            vertLine.VerticalAlignment = VerticalAlignment.Top;
            
            // vertical line color
            vertLine.Stroke = brush_CueCrossing;
            // vertical line stroke thickness
            vertLine.StrokeThickness = crossingLineThickness;
            //name
            vertLine.Name = name_vLine;

            vertLine.Visibility = Visibility.Hidden;
            vertLine.IsEnabled = false;
            myGrid.Children.Add(vertLine);
            myGrid.RegisterName(vertLine.Name, vertLine);
            myGrid.UpdateLayout();
        }

        private void Add_OneCrossing(int[] centerPoint_Pos)
        {/*     Show One Crossing Containing One Horizontal Line and One Vertical Line
            *   The Center Points of the Two Lines Intersect at centerPoint_Pos
            * 
             */

            int centerPoint_X = centerPoint_Pos[0], centerPoint_Y = centerPoint_Pos[1];

            horiLine.Margin = new Thickness(centerPoint_X - objdiameter/2, centerPoint_Y, 0, 0);
            vertLine.Margin = new Thickness(centerPoint_X, centerPoint_Y - objdiameter / 2, 0, 0);

            horiLine.Visibility = Visibility.Visible;
            vertLine.Visibility = Visibility.Visible;
            myGrid.UpdateLayout();
        }

        private void Remove_OneCrossing()
        {
            horiLine.Visibility = Visibility.Hidden;
            vertLine.Visibility = Visibility.Hidden;
            myGrid.UpdateLayout();
        }

        private void Remove_All()
        {
            Remove_OneCrossing();
            Remove_TwoWhitePoints();
            Remove_GoCircle();
            Remove_NogoRect();

        }


        public float TransferTo(float value, float lower, float upper)
        {// transform value (0=<value<1) into a valueT (lower=<valueT<upper)

            float rndTime;
            rndTime = value * (upper - lower) + lower;

            return rndTime;
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

                            // the time point for startpad touched
                            timePoint_StartpadTouched = globalWatch.ElapsedMilliseconds;
                            
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
             * Wait for Startpad touch to trigger a new Trial
             */

            Remove_All();
            myGrid.Background = brush_BKWaitTrialStart;
            //myGridBorder.BorderBrush = brush_BDWaitTrialStart;

            Task task_WaitStart = Task.Run(() =>
            {
                while (PresentTrial && pressedStartpad == PressedStartpad.No) ;

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
                myGrid.Background = brush_BKTrial;
                timePoint_Interface_ReadyOnset = globalWatch.ElapsedMilliseconds;

                // Wait Startpad Hold Enough Time
                startpadHoldstate = StartPadHoldState.HoldTooShort;
                await Wait_EnoughTouch(t_Ready);

            }
            catch (TaskCanceledException)
            {
                // trial execute result: waitReadyTooShort 
                trialExeResult = TrialExeResult.readyWaitTooShort;
                Task task = null;
                throw new TaskCanceledException(task);
            }
        }

 
        public async Task Interface_Cue(float t_Cue, int[] onecrossingPos)
        {/* task for Cue Interface 
            Show the Cue Interface while Listen to the state of the startpad. 
            
            Args:
                t_Cue: Cue interface showes duration(s)
                onecrossingPos: the center position of the one crossing
                wpoint1pos, wpoint2pos: the positions of the two white points

            * Output:
            *   startPadHoldstate_Cue = 
            *       StartPadHoldState.HoldEnough (if startpad is touched lasting t_Cue)
            *       StartPadHoldState.HoldTooShort (if startpad is released before t_Cue) 
            */

            try
            {
                //myGrid.Children.Clear();
                Remove_All();

                // add one crossing on the right middle
                Add_OneCrossing(onecrossingPos);

                timePoint_Interface_CueOnset = globalWatch.ElapsedMilliseconds;

                // wait target cue for several seconds
                startpadHoldstate = StartPadHoldState.HoldTooShort;
                await Wait_EnoughTouch(t_Cue);

            }
            catch (TaskCanceledException)
            {
             
                // Audio Feedback
                player_Error.Play();

                // trial execute result: waitReadyTooShort 
                trialExeResult = TrialExeResult.cueWaitTooShort;

                Task task = null;
                throw new TaskCanceledException(task);
            }
            
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
                        trialExeResult = TrialExeResult.goReachTimeToolong;
                        throw new TaskCanceledException("No Reach within the Max Reach Time");
                    }
                }
                downPoints_Pos.Clear();
                downPoints_PosTime.Clear();
                waitWatch.Restart();
                while (waitWatch.ElapsedMilliseconds <= tMax_1Touch) ;
                waitWatch.Stop();
                calc_GoTargetTouchState();
            }); 
        }

        private void calc_GoTargetTouchState()
        {/* Calculate GoTargetTouchState  
            1. based on the Touch Down Positions in  List downPoints_Pos and circleGo_centerPoint
            2. Assign the calculated target touch state to the GoTargetTouchState variable gotargetTouchstate
            */

            double distance; 
            gotargetTouchstate = GoTargetTouchState.goMissed;
            while (downPoints_Pos.Count > 0)
            {
                // always deal with the point at 0
                Point touchp = new Point(downPoints_Pos[0][0], downPoints_Pos[0][1]);

                // distance between the touchpoint and the center of the circleGo
                distance = Point.Subtract(circleGo_centerPoint, touchp).Length;


                if (distance <= circleGo_radius)
                {// Hit 
                    gotargetTouchstate = GoTargetTouchState.goHit;
                    downPoints_Pos.Clear();
                    break;
                }
                else if (gotargetTouchstate == GoTargetTouchState.goMissed && distance <= circleGoClose_radius)
                {
                    gotargetTouchstate = GoTargetTouchState.goClose;
                }

                // remove the downPoint at 0
                downPoints_Pos.RemoveAt(0);
            }
            downPoints_Pos.Clear();
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
                // Remove the Crossing and Add the Go Circle
                Remove_OneCrossing();
                Add_GoCircle(pos_Target);

                // go target Onset Time Point
                timePoint_Interface_TargetOnset = globalWatch.ElapsedMilliseconds;

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


        private async Task Interface_noGo(float t_noGoShow, int[] pos_Target)
        {/* task for noGo Interface: Show the noGo Interface while Listen to the state of the startpad.
            * If StartpadTouched off within t_nogoshow, go to noGo Interface; Otherwise, noGo Correct Interface
            
            * Args:
            *    t_noGoShow: noGo interface shows duration(s)
            *    pos_Target: the center position of the Go Target

            * Output:
            *   startPadHoldstate_Cue = 
            *       StartPadHoldState.HoldEnough (if startpad is touched lasting t_Cue)
            *       StartPadHoldState.HoldTooShort (if startpad is released before t_Cue) 
            */

            try
            {
                // Remove the Crossing and Add the noGo Rect
                Remove_OneCrossing();
                Add_NogoRect(pos_Target);

                // noGo target Onset Time Point
                timePoint_Interface_TargetOnset = globalWatch.ElapsedMilliseconds;

                totalNogoTrialNum++;

                // Wait Startpad TouchedOn  for t_noGoShow
                startpadHoldstate = StartPadHoldState.HoldTooShort;
                await Wait_EnoughTouch(t_noGoShow);

                // noGo trial success when running here
                Feedback_noGoCorrect();

                successNogoTrialNum++;
                trialExeResult = TrialExeResult.nogoSuccess;

                await Task.Delay(t_VisfeedbackShow);
            }
            catch (TaskCanceledException)
            {
                Feedback_noGoError();

                trialExeResult = TrialExeResult.nogoMoved;

                await Task.Delay(t_VisfeedbackShow);
                throw new TaskCanceledException("Startpad Touched off within t_nogoshow");
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

        private void Feedback_noGoError()
        {
            // Visual Feedback
            //myGridBorder.BorderBrush = brush_ErrorFill;
            rectNogo.Fill = brush_ErrorFill;
            rectNogo.Stroke = brush_ErrorOutline;
            myGrid.UpdateLayout();


            //Juicer Feedback
            giveJuicerState = GiveJuicerState.No;

            // Audio Feedback
            player_Error.Play();
        }

        private void Feedback_noGoCorrect()
        {
            // Visual Feedback
            //myGridBorder.BorderBrush = brush_CorrectFill;
            rectNogo.Fill = brush_CorrectFill;
            myGrid.UpdateLayout();


            //Juicer Feedback
            giveJuicerState = GiveJuicerState.FullGiven;

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
                        lock (downPoints_PosTime)
                        {
                            downPoints_PosTime.Add(new double[3] { timestamp_now, _touchPoint.Position.X, _touchPoint.Position.Y });
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
                }
            }

        }
    }
}
