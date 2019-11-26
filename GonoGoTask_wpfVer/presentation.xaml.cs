using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
        double disThreshold_close = 100; // the threshold distance defining close

        // diameter for crossing, circle, square and white points
        int objdiameter = 200;
        int rightgap = 30;
        int leftgap = 30;
        int topgap = 30;
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

        TargetType targetType;
        // randomized Go noGo tag list, tag_gonogo ==1: go, ==0: nogo
        List<TargetType> targetType_List = new List<TargetType>();

        // objects of Go cirle, nogo Rectangle, lines of the crossing, and two white points
        Ellipse circleGo;
        Rectangle rectNogo;
        Line vertLine, horiLine;
        Ellipse point1, point2;

        Point circleGo_centerPoint; // the center of circleGo 
        double circleGo_radius; // the radius of circleGO
        


        InterfaceState interfaceState;

        // name of all the objects
        string name_circleGo = "circleGo";
        string name_rectNogo = "rectNogo";
        string name_vLine = "vLine", name_hLine = "hLine";
        string name_point1 = "wpoint1", name_point2 = "wpoint2";

        // wait range for each event
        int[] wrange_ready = new int[] {1, 3};
        int[] wrange_targetcue = new int[] { 1, 3 };
        int[] wrange_gonogo = new int[] { 5};
        int[] wrange_reward = new int[] {1};


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


        // serial port for DLP-IO8-G
        SerialPort serialPort_IO8;
        int baudRate = 115200;

        /* startpad parameters */
        StartPad4TrialState startPad4TrialState;
        // tmin_touchpad: the minimal touch time on start pad
        int tMin_Startpad = 3; 
        public delegate void UpdateTextCallback(string message);


        
        /*****Methods*******/
        public presentation(MainWindow mainWindow)
        {
            InitializeComponent();
            WindowState = WindowState.Maximized;

            Touch.FrameReported += new TouchFrameEventHandler(Touch_FrameReported);

            parent = mainWindow;


        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //shuffle go and nogo trials
            Shuffle_GonogoTrials(parent.gotrialnum, parent.nogotrialnum);

            // create goCircle, NogoRect, TwoWhitePoints and One Crossing
            Create_GoCircle();
            Create_NogoRect();
            Create_TwoWhitePoints();
            Create_OneCrossing();


            /* serial Port IO8 */
            serialPort_IO8 = new SerialPort();
            //setup and open IO8 serial port 
            serialPort_SetOpen(parent.serialPortIO8_name, baudRate);

            // present task trial by trail
            Present_Task();


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
                MessageBox.Show(ex.Message, "Error Message", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void Shuffle_GonogoTrials(int gotrialnum, int nogotrialnum)
        {/* ---- shuffle go and nogo trials, present in member variable taglist_gonogo -----*/

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

                // add the selected value into tagarray_gonogo
                targetType_List.Add(tmporder_gonogo[randomIndex]);

                //remove this value
                tmporder_gonogo.RemoveAt(randomIndex);
            }
        }


        private void Create_GoCircle()
        {/*Create the blue go circle: circleGo*/

            // Create an Ellipse  
            circleGo = new Ellipse();

            // Create a blue Brush    
            SolidColorBrush blueBrush = new SolidColorBrush();
            blueBrush.Color = Colors.Blue;
            circleGo.Fill = blueBrush;

            // set the size, position of circleGo
            circleGo.Height = objdiameter;
            circleGo.Width = objdiameter;
            circleGo.VerticalAlignment = VerticalAlignment.Center;
            circleGo.HorizontalAlignment = HorizontalAlignment.Right;
            circleGo.Margin = new Thickness(0, 0, rightgap, 0);

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

        private void Add_GoCircle()
        {
            circleGo.Fill = Brushes.Blue;
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

            // Create a red Brush    
            SolidColorBrush redBrush = new SolidColorBrush();
            redBrush.Color = Colors.Red;
            rectNogo.Fill = redBrush;

            // set the size, position of circleGo
            int square_width = objdiameter;
            int square_height = objdiameter;
            rectNogo.Height = square_height;
            rectNogo.Width = square_width;
            rectNogo.VerticalAlignment = VerticalAlignment.Center;
            rectNogo.HorizontalAlignment = HorizontalAlignment.Right;
            rectNogo.Margin = new Thickness(0, 0, rightgap, 0);

            // name
            rectNogo.Name = name_rectNogo;

            rectNogo.Visibility = Visibility.Hidden;
            rectNogo.IsEnabled = false;

            // add to myGrid   
            myGrid.Children.Add(rectNogo);
            myGrid.RegisterName(rectNogo.Name, rectNogo);
            myGrid.UpdateLayout();
        }

        private void Add_NogoRect()
        {
            rectNogo.Fill = Brushes.Red;
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
            point1.HorizontalAlignment = HorizontalAlignment.Left;
            point1.VerticalAlignment = VerticalAlignment.Center;
            point1.Margin = new Thickness(leftgap, 0, 0, 0);

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
            point2.VerticalAlignment = VerticalAlignment.Top;
            point2.Margin = new Thickness(0, topgap, 0, 0);

            point2.Name = name_point2;
            point2.Visibility = Visibility.Hidden;
            point2.IsEnabled = false;
            myGrid.Children.Add(point2);
            myGrid.RegisterName(point2.Name, point2);
            myGrid.UpdateLayout();



        }

        private void Add_TwoWhitePoints()
        {// add nogo rectangle to myGrid

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
            horiLine.HorizontalAlignment = HorizontalAlignment.Right;
            horiLine.VerticalAlignment = VerticalAlignment.Center;
            horiLine.Margin = new Thickness(0, 0, rightgap, 0);
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
            vertLine.HorizontalAlignment = HorizontalAlignment.Right;
            vertLine.VerticalAlignment = VerticalAlignment.Center;
            vertLine.Margin = new Thickness(0, 0, rightgap + len/2, 0);
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

        private void Add_OneCrossing()
        {
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



        public async void Present_Task()
        {
            int triali = 0;
            while (triali < targetType_List.Count)
            {
               // add a new interface. beforeTrial interface


                targetType = targetType_List[triali];
                textbox_main.Text = "triali = " + (triali + 1).ToString();

                // rest to ready interface
                Interface_Ready();

                // Ready interface: wait for touching the startpad to start a new trial
                startPad4TrialState = StartPad4TrialState.noTouch;
                await Wait_Startpad();
                // start a new trial if touched too short
                textbox_thread.Text = startPad4TrialState.ToString();
                if (startPad4TrialState == StartPad4TrialState.TouchedTooShort)
                    continue;



                try
                {
                    touched_InterfaceGoNogo = false;
                    interupt_InterfaceOthers = false;


                    textBox_State.Text = "";

                    // target cue interface
                    await Interface_Targetcue(300);

                    triali++;
                }
                catch (TaskCanceledException)
                {
                    Remove_All();
                    textbox_main.Text = "main Targetcue cancelled";
                    continue;
                }
                await Interface_GoNogo(300);
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

        private static Task Wait_Interface(int loop, CancellationToken cancellationToken)
        {
            /* 
             * wait for several seconds  for one kind of interface
             * 
             * Input: 
             *      int loop: seconds = (loop * 10) / 1000;  
             */

            Task task = null;

            // start a task and return it
            return Task.Run(() =>
            {

                for (int i = 0; i < loop; i++)
                {
                    // Check if a cancellation is required
                    if (cancellationToken.IsCancellationRequested)
                        throw new TaskCanceledException(task);

                    //Do something
                    Thread.Sleep(10);
                }
            });
        }


        private Task Wait_Startpad()
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
                while (readStartpad == ReadStartpad.Yes)
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
            myGrid.Background = Brushes.Black;
        }

        private void Interface_Ready()
        {
            Remove_All();
            myGrid.Background = Brushes.Gray;
            myGridBorder.BorderBrush = Brushes.Gray;
        }



        public async Task Interface_Targetcue(int loop)
        {
            using (var cancellationTokenSourece = new CancellationTokenSource())
            {
                var buttonTask = Task.Run(() =>
                {
                    DateTime startTime = DateTime.Now;
                    while (interupt_InterfaceOthers == false && (DateTime.Now - startTime).TotalSeconds < 5) ;

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
                    Add_OneCrossing();
                    // two white points on left middle and top middle
                    Add_TwoWhitePoints();

                    textbox_thread.Text = "Targetcue running......";
                    interfaceState = InterfaceState.TargetCue;
                    // wait target cue for several seconds
                    await Wait_Interface(loop, cancellationTokenSourece.Token);
                    textbox_thread.Text = "Targetcue run completely";
                    
                }
                catch (TaskCanceledException)
                {
                    textbox_thread.Text = "Targetcue run cancelled";

                    Task task = null;
                    throw new TaskCanceledException(task);
                }

                await buttonTask;
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


        public async Task Interface_GoNogo(int loop)
        {
            using (var cancellationTokenSourece = new CancellationTokenSource())
            {
                var buttonTask = Task.Run(() =>
                {
                    DateTime startTime = DateTime.Now;
                    while (touched_InterfaceGoNogo == false && (DateTime.Now - startTime).TotalSeconds < 5) ;

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
                        Add_GoCircle();
                    }

                    else
                    {
                        Add_NogoRect();
                    }
          
                    interfaceState = InterfaceState.GoNogo;
                    touchState = TouchState.start;

                    textbox_thread.Text = "Gonogo running.....";
                    // wait target cue for several seconds
                    await Wait_Interface(loop, cancellationTokenSourece.Token);

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
                
                await buttonTask;
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
