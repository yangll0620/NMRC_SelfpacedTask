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

        public enum TouchState
        {
            start,

            // Go Cue
            goHit, // at least one finger inside the circleGo
            goClose, // the distance between the closest touch point and the center of the circleGo is less than a threshold
            goMissed, // touched with longer distance 
            goNoaction, // no touch

            //noGo Cue
            nogoTouched,
            nogoNoaction,
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

        private enum StartPad4TrialState
        {
            noTouch,
            TouchedEnough,
            TouchedTooShort
        }



        /*****************parameters ************************/
        MainWindow parent;


        // diameter for crossing, circle, square and white points
        int objdiameter;
        int disXFromCenter, disYFromCenter;
        // the threshold distance defining close
        int disThreshold_close; 

        TargetType targetType;
        
        // randomized Go noGo tag list, tag_gonogo ==1: go, ==0: nogo
        List<TargetType> targetType_List = new List<TargetType>();


        // objects of Go cirle, nogo Rectangle, lines of the crossing, and two white points
        Ellipse circleGo;
        SolidColorBrush brush_goCircle;
        Rectangle rectNogo;
        SolidColorBrush brush_nogoRect;
        Line vertLine, horiLine;
        Ellipse point1, point2;

        Point circleGo_centerPoint; // the center of circleGo 
        double circleGo_radius; // the radius of circleGO



        // background of ready and trial
        SolidColorBrush brush_bkready, brush_bktrial;


        InterfaceState interfaceState;

        // name of all the objects
        string name_circleGo = "circleGo";
        string name_rectNogo = "rectNogo";
        string name_vLine = "vLine", name_hLine = "hLine";
        string name_point1 = "wpoint1", name_point2 = "wpoint2";
        // randomized positions of the Go/noGo target  and the two white points
        List<int[]> goNogoPos_List = new List<int[]>();
        List<int[]> wpoint1Pos_List = new List<int[]>();
        List<int[]> wpoint2Pos_List = new List<int[]>();

        // wait range for each event
        float waitt_goNogo, waitt_reward;
        float[] waittrange_ready, waittrange_cue;
        


        // set storing the touch point id (no replicates)
        HashSet<int> touchPoints_Id = new HashSet<int>();
        // list storing the position of the touch points when touched up
        List<double[]> upPoints_pos = new List<double[]>();

        // tag for finishing one set of touched down and up 
        bool touched_Downup;

        TouchState touchState;

        // tag of starting a trial
        bool startTrial = false;
        // tag of touching during go interface 
        bool touched_InterfaceGoNogo = false;
        //tag of interupting during interfaces except go interface
        bool interupt_InterfaceOthers = false;


        CancellationTokenSource cancellationTokenSourece = new CancellationTokenSource();


        // serial port for DLP-IO8-G
        SerialPort serialPort_IO8;
        int baudRate = 115200;

        /* startpad parameters */
        StartPad4TrialState startPad4TrialState;
        // tmin_touchpad: the minimal touch time on start pad
        //int tMin_Startpad = 3; 
        public delegate void UpdateTextCallback(string message);


        
        /*****Methods*******/
        public presentation(MainWindow mainWindow)
        {
            InitializeComponent();
            WindowState = WindowState.Maximized;

            Touch.FrameReported += new TouchFrameEventHandler(Touch_FrameReported);

            parent = mainWindow;


        }

        public int cm2pixal(float cmlen)
        {/* convert length with unit cm to unit pixal, 96 pixals = 1 inch = 2.54 cm

            args:   
                cmlen: to be converted length (unit: cm)

            return:
                pixalen: converted length with unit pixal
         */

            float ratio = (float)96/(float)2.54;

            int pixalen = (int)(cmlen * ratio);

            return pixalen;
        }


        public int in2pixal(float inlen)
        {/* convert length with unit inch to unit pixal, 96 pixals = 1 inch = 2.54 cm

            args:   
                cmlen: to be converted length (unit: inch)

            return:
                pixalen: converted length with unit pixal
         */

            int ratio = 96;

            int pixalen = (int) (inlen * ratio);

            return pixalen;
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            /* get the setup from the parent interface */
            // object size and distance parameters
            objdiameter = in2pixal(float.Parse(parent.textBox_objdiameter.Text));
            disXFromCenter = in2pixal(float.Parse(parent.textBox_Xdisfromcenter.Text));
            disYFromCenter = in2pixal(float.Parse(parent.textBox_Ydisfromcenter.Text));
            disThreshold_close = (int) (objdiameter * float.Parse(parent.textBox_errorMargin.Text) /100);


            // interfaces time related parameters
            waitt_goNogo = float.Parse(parent.textBox_tmaxGoNogoShow.Text);
            waitt_reward = float.Parse(parent.textBox_tRewardShow.Text);
            waittrange_ready = new float[] { float.Parse(parent.textBox_tReady_min.Text), float.Parse(parent.textBox_tReady_max.Text) };
            waittrange_cue = new float[] { float.Parse(parent.textBox_tCue_min.Text), float.Parse(parent.textBox_tCue_max.Text) };


            // Brush for background of ready and trial
            brush_bkready = new SolidColorBrush();
            brush_bkready.Color = Colors.Gray;
            brush_bktrial = new SolidColorBrush();
            brush_bktrial.Color = Colors.Black;


            //shuffle go and nogo trials
            Shuffle_GonogoTrials(parent.gotrialnum, parent.nogotrialnum);

            // Create necessary elements: go circle, nogo rect, two white points and one crossing
            Create_GoCircle();
            Create_NogoRect();
            Create_TwoWhitePoints();
            Create_OneCrossing();



            // create a serial Port IO8 instance, and open it
            serialPort_IO8 = new SerialPort();

            try
            {
                serialPort_SetOpen(parent.serialPortIO8_name, baudRate);

                // present task trial by trail
                Present_Task();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Message", MessageBoxButton.OK, MessageBoxImage.Error);
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


        private void Shuffle_GonogoTrials(int gotrialnum, int nogotrialnum)
        {/* ---- 
            shuffle go and nogo trials, present in member variable taglist_gonogo 
         
             */

            // create orderred gonogo list
            List<TargetType> tmporder_go = new List<TargetType>(Enumerable.Repeat(TargetType.Go, gotrialnum));
            List<TargetType> tmporder_nogo = new List<TargetType>(Enumerable.Repeat(TargetType.Nogo, nogotrialnum));
            List<TargetType> tmporder_gonogo = tmporder_go.Concat(tmporder_nogo).ToList();


            // shuffle 
            Random r = new Random();
            int randomIndex, randomPosInd;

            while (tmporder_gonogo.Count > 0)
            {
                // choose a random index in list tmporder_gonogo
                randomIndex = r.Next(0, tmporder_gonogo.Count);

                // add the selected value (go/nogo type) into tagarray_gonogo
                targetType_List.Add(tmporder_gonogo[randomIndex]);

                // add the corresponding go/noGo object and the two white points position
                randomPosInd = r.Next(1, 4);
                if (randomPosInd == 1)
                {// goNogo object on the left
                    goNogoPos_List.Add(new int[] { -disXFromCenter, 0 });

                    wpoint1Pos_List.Add(new int[] { 0, -disYFromCenter });
                    wpoint2Pos_List.Add(new int[] { disXFromCenter, 0 });
                }
                else if (randomPosInd == 2)
                {// goNogo object on the top
                    goNogoPos_List.Add(new int[] { 0, -disYFromCenter });

                    wpoint1Pos_List.Add(new int[] { -disXFromCenter, 0 });
                    wpoint2Pos_List.Add(new int[] { disXFromCenter, 0 });
                }

                else if (randomPosInd == 3) 
                {// goNogo object on the right
                    goNogoPos_List.Add(new int[] { disXFromCenter, 0 });
                    
                    wpoint1Pos_List.Add(new int[] { -disYFromCenter, 0 });
                    wpoint2Pos_List.Add(new int[] { 0, -disYFromCenter });
                }
                    

                //remove this value
                tmporder_gonogo.RemoveAt(randomIndex);
            }
        }


        private void Create_GoCircle()
        {/*
            Create the go circle: circleGo

            Arg:
                pos: the position ([left, top]) the go circle, vertical and horizontal aligned to center 


            */

            // Create an Ellipse  
            circleGo = new Ellipse();

            // Create a Brush    
            brush_goCircle = new SolidColorBrush();

            if(parent.cbo_goColor.Text == "Blue")
                brush_goCircle.Color = Colors.Blue;
            else if(parent.cbo_goColor.Text == "Red")
                brush_goCircle.Color = Colors.Red;
            else if (parent.cbo_goColor.Text == "Green")
                brush_goCircle.Color = Colors.Green;
            else if (parent.cbo_goColor.Text == "Yellow")
                brush_goCircle.Color = Colors.Yellow;


            circleGo.Fill = brush_goCircle;

            // set the size, position of circleGo
            circleGo.Height = objdiameter;
            circleGo.Width = objdiameter;
            circleGo.VerticalAlignment = VerticalAlignment.Center;
            circleGo.HorizontalAlignment = HorizontalAlignment.Center;

            circleGo.Name = name_circleGo;
            circleGo.Visibility = Visibility.Hidden;
            circleGo.IsEnabled = false;

            // add to myGrid
            myGrid.Children.Add(circleGo);
            myGrid.RegisterName(circleGo.Name, circleGo);
            myGrid.UpdateLayout();

            // get the center point and the radius of circleGo
            Point leftTopPoint_circleGo = circleGo.TransformToAncestor(this).Transform(new Point(0, 0));
            circleGo_centerPoint = Point.Add(leftTopPoint_circleGo, new Vector(circleGo.Width / 2, circleGo.Height / 2));
            circleGo_radius = ((circleGo.Height + circleGo.Width) / 2) / 2;
        }

        private void Add_GoCircle(int[] pos)
        {/*show the Go Circle at pos*/

            circleGo.Margin = new Thickness(pos[0], pos[1], 0, 0);
            circleGo.Fill = brush_goCircle;
            circleGo.Visibility = Visibility.Visible;
            circleGo.IsEnabled = true;
            myGrid.UpdateLayout();
        }
        private void Remove_GoCircle()
        {
            circleGo.Visibility = Visibility.Hidden;
            circleGo.IsEnabled = false;
            myGrid.UpdateLayout();
        }


        private void Create_NogoRect()
        {/*Create the red nogo rectangle: rectNogo*/

            // Create an Ellipse  
            rectNogo = new Rectangle();

            // Create a Brush for nogo Rectangle    
            brush_nogoRect = new SolidColorBrush();
            if (parent.cbo_nogoColor.Text == "Blue")
                brush_nogoRect.Color = Colors.Blue;
            else if (parent.cbo_nogoColor.Text == "Red")
                brush_nogoRect.Color = Colors.Red;
            else if (parent.cbo_nogoColor.Text == "Green")
                brush_nogoRect.Color = Colors.Green;
            else if (parent.cbo_nogoColor.Text == "Yellow")
                brush_nogoRect.Color = Colors.Yellow;

            rectNogo.Fill = brush_nogoRect;

            // set the size, position of circleGo
            int square_width = objdiameter;
            int square_height = objdiameter;
            rectNogo.Height = square_height;
            rectNogo.Width = square_width;
            rectNogo.VerticalAlignment = VerticalAlignment.Center;
            rectNogo.HorizontalAlignment = HorizontalAlignment.Center;

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

        private void Add_NogoRect(int[] pos)
        {/*show the noGo Rect at pos*/

            rectNogo.Margin = new Thickness(pos[0], pos[1], 0, 0);
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

        private void Add_TwoWhitePoints(int[] pos1, int[] pos2)
        {// show Two White Points at pos1 and pos2 separately rectangle to myGrid

            point1.Margin = new Thickness(pos1[0], pos1[1], 0, 0);
            point2.Margin = new Thickness(pos2[0], pos2[1], 0, 0);

            point1.Visibility = Visibility.Visible;
            point2.Visibility = Visibility.Visible;

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
            int linethickness = 2;

            // Create a while Brush    
            SolidColorBrush whiteBrush = new SolidColorBrush();
            whiteBrush.Color = Colors.White;

            // Create the horizontal line
            horiLine = new Line();
            horiLine.X1 = 0;
            horiLine.Y1 = 0;
            horiLine.X2 = len;
            horiLine.Y2 = horiLine.Y1;        
            
            // horizontal line position
            horiLine.HorizontalAlignment = HorizontalAlignment.Center;
            horiLine.VerticalAlignment = VerticalAlignment.Center;
            
            // horizontal line color
            horiLine.Stroke = whiteBrush;
            // horizontal line stroke thickness
            horiLine.StrokeThickness = linethickness;
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
            vertLine.HorizontalAlignment = HorizontalAlignment.Center;
            vertLine.VerticalAlignment = VerticalAlignment.Center;
            
            // vertical line color
            vertLine.Stroke = whiteBrush;
            // vertical line stroke thickness
            vertLine.StrokeThickness = linethickness;
            //name
            vertLine.Name = name_vLine;

            vertLine.Visibility = Visibility.Hidden;
            vertLine.IsEnabled = false;
            myGrid.Children.Add(vertLine);
            myGrid.RegisterName(vertLine.Name, vertLine);
            myGrid.UpdateLayout();
        }

        private void Add_OneCrossing(int[] pos)
        {//show One Crossing at the same position of object go/nogo

            horiLine.Margin = new Thickness(pos[0], pos[1], 0, 0);
            vertLine.Margin = new Thickness(pos[0], pos[1], 0, 0);

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


        public float randomT(float lower, float upper)
        {// randomly generate a time in interval [lower, upper]

            Random rnd = new Random();
            float rndTime;
            rndTime = (float)rnd.NextDouble() * (upper - lower) + lower;

            return rndTime;
        }

        public async void Present_Task()
        {
            int triali = 0;
            float waitt_ready, waitt_cue;
            int[] wpoint1pos, wpoint2pos;
            int[] goNogopos;

            while (triali < targetType_List.Count)
            {
               
                // parameters for this trial
                targetType = targetType_List[triali];
                wpoint1pos = wpoint1Pos_List[triali];
                wpoint2pos = wpoint2Pos_List[triali];
                goNogopos = goNogoPos_List[triali];


                textbox_main.Text = "triali = " + (triali + 1).ToString();

                // rest to ready interface
                Interface_Ready();

                // Ready interface: wait for touching the startpad to start a new trial
                startPad4TrialState = StartPad4TrialState.noTouch;
                await Wait_Startpad(3);
                
                // restart a new trial if touched too short
                textbox_thread.Text = startPad4TrialState.ToString();
                if (startPad4TrialState == StartPad4TrialState.TouchedTooShort)
                    continue;

                try
                {
                    touched_InterfaceGoNogo = false;
                    interupt_InterfaceOthers = false;


                    textBox_State.Text = "";

                    // target cue interface
                    waitt_cue = randomT(waittrange_cue[0], waittrange_cue[1]);
                    await Interface_Targetcue(waitt_cue, goNogopos, wpoint1pos, wpoint2pos);

                    triali++;
                }
                catch (TaskCanceledException)
                {
                    Remove_All();
                    textbox_main.Text = "main Targetcue cancelled";
                    continue;
                }
                await Interface_GoNogo(waitt_goNogo, goNogopos);
            }

            Remove_All();
        }


        private static Task Wait_Interface(CancellationToken cancellationToken)
        {
            /* 
             * wait until cancellation
             * 
             * Input: 
             *       cancellationToken
             */

            Task task = null;

            // start a task and return it
            return Task.Run(() =>
            {

                while(true)
                {
                    // Check if a cancellation is required
                    if (cancellationToken.IsCancellationRequested)
                        throw new TaskCanceledException(task);

                    //Do something
                    Thread.Sleep(10);
                }
            });
        }

      private static Task Wait_Interface(float t_wait, CancellationToken cancellationToken)
        {
            /* 
             * wait for several seconds for one kind of interface
             * 
             * Input: 
             *    t_wait: the waited time (s)  
             */

            Task task = null;

            // start a task and return it
            return Task.Run(() =>
            {
                Stopwatch waitWatch = new Stopwatch();
                waitWatch.Restart();
                while (waitWatch.ElapsedMilliseconds < t_wait * 1000)
                {
                    // Check if a cancellation is required
                    if (cancellationToken.IsCancellationRequested)
                        throw new TaskCanceledException(task);
                }
            });
        }


        private Task Wait_Startpad(float tMin_Startpad)
        {
            /* 
                task wait for touching startpad

                update startPad4TrialState (StartPad4TrialState.TouchedEnough or StartPad4TrialState.TouchedTooShort)
            */
            Task task_Startpad = Task.Run(() =>
            {
                Stopwatch touchedWatch = new Stopwatch();
                PressedStartpad pressedStartpad = PressedStartpad.No;
                ReadStartpad readStartpad = ReadStartpad.Yes;
                while (serialPort_IO8.IsOpen && readStartpad == ReadStartpad.Yes)
                {
                    serialPort_IO8.WriteLine("Z");

                    // extract and parse the start pad voltage 
                    string str_Read = serialPort_IO8.ReadExisting();
                    string[] str_vol = str_Read.Split(new Char[] { 'V' });

                    if (!String.IsNullOrEmpty(str_vol[0]))
                    {
                        float voltage = float.Parse(str_vol[0]);

                        if (voltage < 1)
                        {
                            if (pressedStartpad == PressedStartpad.No)
                            {/* first time to touched state */

                                // restart measuring touch time
                                touchedWatch.Restart();

                                textbox_thread2.Dispatcher.Invoke(new UpdateTextCallback(this.UpdateBackground),
                                    new object[] { "First Touch" });
                            }
                            else if (touchedWatch.ElapsedMilliseconds >= tMin_Startpad * 1000)
                            { /* touched with enough time */

                                startPad4TrialState = StartPad4TrialState.TouchedEnough;

                                // stop read start pad
                                readStartpad = ReadStartpad.No;
                            }
                            pressedStartpad = PressedStartpad.Yes;

                        }
                        else
                        {
                            if (pressedStartpad == PressedStartpad.Yes)
                            {/* first time from touched to idle */

                                if (touchedWatch.IsRunning && touchedWatch.ElapsedMilliseconds < tMin_Startpad * 1000)
                                {/* touched with no enough time */

                                    startPad4TrialState = StartPad4TrialState.TouchedTooShort;

                                    // stop read start pad
                                    readStartpad = ReadStartpad.No;
                                }
                            }
                            pressedStartpad = PressedStartpad.No;
                        }


                    }

                    Thread.Sleep(30);
                }
            });

            return task_Startpad;
        }

        private void UpdateBackground(string message)
        {
            myGrid.Background = brush_bktrial;
        }

        private void Interface_Ready()
        {
            Remove_All();
            myGrid.Background = brush_bkready;
            myGridBorder.BorderBrush = brush_bktrial;
        }



        public async Task Interface_Targetcue(float waitt_cue, int[] onecrossingPos, int[] wpointpos1, int[] wpointpos2)
        {/* async task for targetcue interface 

            Args:
                waitt_cue: wait time for cue interface (ms)
                onecrossingPos: the center position of the one crossing
                wpoint1pos, wpoint2pos: the positions of the two white points

            */


            using (var cancellationTokenSourece = new CancellationTokenSource())
            {
                var buttonTask = Task.Run(() =>
                {
                    DateTime startTime = DateTime.Now;
                    while (interupt_InterfaceOthers == false) ;

                    //
                    if (interupt_InterfaceOthers == true)
                    {
                        cancellationTokenSourece.Cancel();
                        interupt_InterfaceOthers = false;
                    }

                });

                try
                {
                    //myGrid.Children.Clear();
                    Remove_All();

                    // add one crossing on the right middle
                    Add_OneCrossing(onecrossingPos);
                    // two white points on left middle and top middle
                    Add_TwoWhitePoints(wpointpos1, wpointpos2);

                    textbox_thread.Text = "Targetcue running......";
                    interfaceState = InterfaceState.TargetCue;
                    // wait target cue for several seconds
                    await Wait_Interface(waitt_cue, cancellationTokenSourece.Token);
                    textbox_thread.Text = "Targetcue run completely";
                    
                }
                catch (TaskCanceledException)
                {
                    textbox_thread.Text = "Targetcue run cancelled";

                    Task task = null;
                    throw new TaskCanceledException(task);
                }

            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            if (serialPort_IO8.IsOpen)
                serialPort_IO8.Close();
        }

        public void Interface_Reward()
        {
            myGridBorder.BorderBrush = Brushes.Yellow;

            if (targetType == TargetType.Go)
            {
                circleGo.Fill = Brushes.Yellow;
            }

            else
            {
                rectNogo.Fill = Brushes.Yellow;
            }
            
            myGrid.UpdateLayout();
        }


        public async Task Interface_GoNogo(float waitt_gonogo, int[] goNogPos)
        {/* async task for go/nogo interface 

            Args:
                waitt_gonogo: wait time for go interface (ms)
                objpos: the center position of the object (go cicle or nogo rect)

            */
            using (var cancellationTokenSourece = new CancellationTokenSource())
            {
                var buttonTask = Task.Run(() =>
                {
                    DateTime startTime = DateTime.Now;
                    while (touched_InterfaceGoNogo == false) ;

                    // touched detected
                    if (touched_InterfaceGoNogo == true)
                    {
                        cancellationTokenSourece.Cancel();
                        touched_InterfaceGoNogo = false;
                    }

                });

                try
                {
                    //myGrid.Children.Clear();

                    Remove_OneCrossing();

                    // add go circle button or nogo square button on the right middle visible
                    if (targetType == TargetType.Go)
                    {
                        Add_GoCircle(goNogPos);
                    }

                    else
                    {
                        Add_NogoRect(goNogPos);
                    }
          
                    interfaceState = InterfaceState.GoNogo;
                    touchState = TouchState.start;

                    textbox_thread.Text = "Gonogo running.....";
                    // wait target cue for several seconds
                    await Wait_Interface(waitt_gonogo, cancellationTokenSourece.Token);

                    if (targetType == TargetType.Go)
                    {
                        touchState = TouchState.goNoaction;
                    }
                    else
                        touchState = TouchState.nogoNoaction;

                    textbox_thread.Text = "Gonogo Complete.";

                    await Task.Delay(1000);
                }
                catch (TaskCanceledException)
                {
                    Interface_Reward();
                    textBox_State.Text = touchState.ToString();
                    await Task.Delay(1000);

                }
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            
        }


        void Touch_FrameReported(object sender, TouchFrameEventArgs e)
        {
     
            if (interfaceState == InterfaceState.Ready)
            {// ready for starting a new trial

                TouchPointCollection touchPoints = e.GetTouchPoints(myGrid);
                bool addedNew, removedOld;
                for (int i = 0; i < touchPoints.Count; i++)
                {
                    TouchPoint _touchPoint = touchPoints[i];

                    if (_touchPoint.Action == TouchAction.Down)
                    { // TouchAction.Down
                      // check if new Point
                        lock (touchPoints_Id)
                        {
                            addedNew = touchPoints_Id.Add(_touchPoint.TouchDevice.Id);
                        }

                        // deal the new touch point
                        if (addedNew)
                        {

                        }

                    }
                    else if (_touchPoint.Action == TouchAction.Up)
                    {// TouchAction.Up

                        // remove the id of the point with up action
                        lock (touchPoints_Id)
                        {
                            removedOld = touchPoints_Id.Remove(_touchPoint.TouchDevice.Id);
                        }

                        // deal with the point with up action
                        if (removedOld)
                        {
                            double[] pos = new double[2] { _touchPoint.Position.X, _touchPoint.Position.Y };

                            // store the pos of the point with up action
                            lock (upPoints_pos)
                            {
                                upPoints_pos.Add(pos);
                            }
                        }
                        else
                        {
                            throw new System.ArgumentException("Touch point ID can't be removed!");
                        }

                        // all points are already touched up
                        if (touchPoints_Id.Count == 0)
                        {
                            while (upPoints_pos.Count > 0)
                            {

                                upPoints_pos.RemoveAt(0);
                            }

                            startTrial = true;
                        }
                    }

                }
            }
            else if (interfaceState == InterfaceState.GoNogo)
            {// gonogo interface
                if (targetType == TargetType.Nogo)
                { // No Go 
                    touched_InterfaceGoNogo = true;
                }
                else if (targetType == TargetType.Go)
                { // Go 
                    bool addedNew, removedOld;

                    TouchPointCollection touchPoints = e.GetTouchPoints(myGrid);

                    for (int i = 0; i < touchPoints.Count; i++)
                    {
                        TouchPoint _touchPoint = touchPoints[i];

                        if (_touchPoint.Action == TouchAction.Down)
                        { // TouchAction.Down
                            // check if new Point
                            lock (touchPoints_Id)
                            {
                                addedNew = touchPoints_Id.Add(_touchPoint.TouchDevice.Id);
                            }

                            // deal the new touch point
                            if (addedNew)
                            {

                            }

                        }
                        else if (_touchPoint.Action == TouchAction.Up)
                        {// TouchAction.Up

                            // remove the id of the point with up action
                            lock (touchPoints_Id)
                            {
                                removedOld = touchPoints_Id.Remove(_touchPoint.TouchDevice.Id);
                            }

                            // deal with the point with up action
                            if (removedOld)
                            {
                                double[] pos = new double[2] { _touchPoint.Position.X, _touchPoint.Position.Y };

                                // store the pos of the point with up action
                                lock (upPoints_pos)
                                {
                                    upPoints_pos.Add(pos);
                                }
                            }
                            else
                            {
                                throw new System.ArgumentException("Touch point ID can't be removed!");
                            }

                            // all points are already touched up
                            if (touchPoints_Id.Count == 0)
                            {
                                //touched_Downup = true;
                                
                                /* calculate TouchState*/
                                double distance;
                                touchState = TouchState.goMissed;
                                while (upPoints_pos.Count > 0)
                                {
                                    Point touchp = new Point(upPoints_pos[0][0], upPoints_pos[0][1]);
                                    // distance between the touchpoint and the center of the circleGo
                                    distance = Point.Subtract(circleGo_centerPoint, touchp).Length;

                                    // identify TouchState
                                    if (distance <= circleGo_radius)
                                    {
                                        touchState = TouchState.goHit;
                                    }
                                    else if (distance <= circleGo_radius + disThreshold_close && touchState != TouchState.goHit)
                                    {
                                        touchState = TouchState.goClose;
                                    }
                                    /*else if (distance > circleGo_radius + disThreshold_close && (touchState != TouchState.goHit && touchState != TouchState.goClose))
                                        touchState = TouchState.goMissed;*/

                                    upPoints_pos.RemoveAt(0);
                                }

                                touched_InterfaceGoNogo = true;

                            }
                        }

                    }
                }
            }
            else if(interfaceState == InterfaceState.TargetCue)
            {
               
                TouchPointCollection touchPoints = e.GetTouchPoints(myGrid);
                bool addedNew, removedOld;
                for (int i = 0; i < touchPoints.Count; i++)
                {
                    TouchPoint _touchPoint = touchPoints[i];

                    if (_touchPoint.Action == TouchAction.Down)
                    { // TouchAction.Down
                      // check if new Point
                        lock (touchPoints_Id)
                        {
                            addedNew = touchPoints_Id.Add(_touchPoint.TouchDevice.Id);
                        }

                    }
                    else if (_touchPoint.Action == TouchAction.Up)
                    {// TouchAction.Up

                        // remove the id of the point with up action
                        lock (touchPoints_Id)
                        {
                            removedOld = touchPoints_Id.Remove(_touchPoint.TouchDevice.Id);
                        }

                        // deal with the point with up action
                        if (removedOld)
                        {
                            double[] pos = new double[2] { _touchPoint.Position.X, _touchPoint.Position.Y };

                            // store the pos of the point with up action
                            lock (upPoints_pos)
                            {
                                upPoints_pos.Add(pos);
                            }
                        }
                        else
                        {
                            throw new System.ArgumentException("Touch point ID can't be removed!");
                        }

                        // all points are already touched up
                        if (touchPoints_Id.Count == 0)
                        {
                            while (upPoints_pos.Count > 0)
                            {

                                upPoints_pos.RemoveAt(0);
                            }

                            interupt_InterfaceOthers = true;
                        }
                    }

                }
            }
        }
    }
}
