using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;


using swf = System.Windows.Forms;
using sd = System.Drawing;
using System.Windows.Media;
using System.Reflection;
using System.Collections.Generic;
using System.Windows.Shapes;
using System.Windows.Input;


namespace COTTask_wpf
{
    /// <summary>
    /// Interaction logic for SetupTargetsWin.xaml
    /// </summary>
    public partial class SetupTargetsWin : Window
    {
        public MainWindow parent;

        private bool BtnStartState, BtnStopState;

        // Optional positions 
        ArrayList optPosString_List;
        private TextBox editBox_Pos;
        int indexSelected = 0;

        public SetupTargetsWin(MainWindow parentWindow)
        {
            InitializeComponent();


            parent = parentWindow;

            DisableBtnStartStop();

            LoadInitTargetData();

        }

        private void LoadInitTargetData()
        {
            textBox_targetSize.Text = parent.targetSizeCM.ToString();

            // generate sizesList and bind optPosString_list to listBox_Sizes
            optPosString_List = new ArrayList();
            GenPositions();

            // Editable TextBox for changing position
            editBox_Pos = new TextBox();
            editBox_Pos.Name = "editBox_Pos";
            editBox_Pos.Width = 0;
            editBox_Pos.Height = 0;
            editBox_Pos.Visibility = Visibility.Hidden;
            editBox_Pos.Text = "";
            editBox_Pos.Background = new SolidColorBrush(Colors.Beige);
            editBox_Pos.Foreground = new SolidColorBrush(Colors.Blue);
            Grid_setupTarget.Children.Add(editBox_Pos);
            Grid_setupTarget.RegisterName(editBox_Pos.Name, editBox_Pos);
            Grid_setupTarget.UpdateLayout();


            textBox_targetNoOfPositions.Text = parent.targetNoOfPositions.ToString();

            textBox_closeMargin.Text = parent.closeMarginPercentage.ToString();
            textBox_targetDiameter.Text = parent.targetDiameterInch.ToString();
            textBox_targetDisfromCenter.Text = parent.targetDisFromCenterInch.ToString();
        }

        private void GenPositions()
        {/*
                Generate the optional X, Y Positions (origin in center)

                Store into class member parent.optPostions_OCenter_List
                and Show on the control listBox_Positions
            */
            int targetSizeCM = int.Parse(textBox_targetSize.Text);
            int targetSizePixal = Utility.cm2pixal(targetSizeCM);

            sd.Rectangle workArea = Utility.Detect_PrimaryScreen_WorkArea();
            int epilson = targetSizePixal;
            int xMin = -workArea.Width / 2 + epilson, xMax = workArea.Width / 2 - epilson;
            int yMin = -workArea.Height / 2 + epilson, yMax = workArea.Height /2 - epilson;

            // generate randomly x, y positions
            Random rnd = new Random();
            parent.optPostions_OCenter_List.Clear();
            for (int i = 0; i < parent.targetNoOfPositions; i++)
            {
                int x = rnd.Next(0, xMax - xMin) + xMin;
                int y = rnd.Next(0, yMax - yMin) + yMin;
                parent.optPostions_OCenter_List.Add(new int[] { x, y });
            }

            // Binding with listBox_Position
            optPosString_List.Clear();
            foreach (int[] xyPos in parent.optPostions_OCenter_List)
            {
                optPosString_List.Add(xyPos[0].ToString() + ", " + xyPos[1].ToString());
            }
            listBox_Positions.ItemsSource = null;
            listBox_Positions.ItemsSource = optPosString_List;
        }

        private void SaveTargetData()
        {/* ---- Save all the Set Target Information back to MainWindow Variables ----- */

            parent.targetSizeCM = int.Parse(textBox_targetSize.Text);
            parent.targetNoOfPositions = int.Parse(textBox_targetNoOfPositions.Text);

            parent.closeMarginPercentage = float.Parse(textBox_closeMargin.Text);
            parent.targetDiameterInch = float.Parse(textBox_targetDiameter.Text);
            parent.targetDisFromCenterInch = float.Parse(textBox_targetDisfromCenter.Text);
        }

        private void DisableBtnStartStop()
        {
            BtnStartState = parent.btn_start.IsEnabled;
            BtnStopState = parent.btn_stop.IsEnabled;
            parent.btn_start.IsEnabled = false;
            parent.btn_stop.IsEnabled = false;
        }

        private void ResumeBtnStartStop()
        {
            parent.btn_start.IsEnabled = BtnStartState;
            parent.btn_stop.IsEnabled = BtnStopState;
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ResumeBtnStartStop();
        }

        private void Btn_GenSizePos_Click(object sender, RoutedEventArgs e)
        {
            GenPositions();
        }

        private void Btn_CheckPositions_Click(object sender, RoutedEventArgs e)
        {
            int targetSizeCM = int.Parse(textBox_targetSize.Text);
            int targetSizePixal = Utility.cm2pixal(targetSizeCM);

            Color BKColor = (Color)(typeof(Colors).GetProperty(parent.BKTrialColorStr) as PropertyInfo).GetValue(null, null);
            Color targetColor = (Color)(typeof(Colors).GetProperty(parent.goColorStr) as PropertyInfo).GetValue(null, null); ;

            ShowAllTargets(targetSizePixal, parent.optPostions_OCenter_List, targetColor, BKColor);
        }


        private Ellipse Draw_Circle_OCenter(double Diameter, int[] cPoint_Pos_OCenter)
        {/*
            Draw the circle

            Args:
                Diameter: the Diameter of the Circle in Pixal

                cPoint_Pos_OCenter: the x, y Positions of the Circle center in Pixal (Origin in the center of the Screen)

            */

            // Create an Ellipse  
            Ellipse circle = new Ellipse();

            // set the size, position of circleGo
            circle.Height = Diameter;
            circle.Width = Diameter;
            circle.VerticalAlignment = VerticalAlignment.Center;
            circle.HorizontalAlignment = HorizontalAlignment.Center;

            circle.Margin = new Thickness(cPoint_Pos_OCenter[0], cPoint_Pos_OCenter[1], 0, 0);

            return circle;
        }

        private void ShowAllTargets(int targetSizePixal, List<int[]> postions_OriginCenter_List, Color targetColor, Color BKColor)
        {/* 
            Show all the targets 

            Args:
                targetSizePixal: Target Diameter in Pixal

                postions_OriginCenter_List: x,y Position for Each Target (Origin in Screen Center)

                targetColor: the target Color

                BKColor: the Background Color
            */


            // Get the touch Screen, Should Set Touch Screen as the PrimaryScreen
            swf.Screen primaryScreen = swf.Screen.PrimaryScreen;


            //Show the Win_allTargets on the Touch Screen
            Window Win_allTargets = new Window();
            sd.Rectangle Rect_primaryScreen = primaryScreen.WorkingArea;
            Win_allTargets.Top = Rect_primaryScreen.Top;
            Win_allTargets.Left = Rect_primaryScreen.Left;


            // Show Background
            Win_allTargets.Background = new SolidColorBrush(BKColor);
            Win_allTargets.Show();
            Win_allTargets.WindowState = WindowState.Maximized;
            Win_allTargets.Name = "childWin_ShowAllTargets";
            Win_allTargets.Title = "All Optional Targets";


            // Add a Grid
            Grid wholeGrid = new Grid();
            wholeGrid.Height = Win_allTargets.ActualHeight;
            wholeGrid.Width = Win_allTargets.ActualWidth;
            Win_allTargets.Content = wholeGrid;
            wholeGrid.UpdateLayout();


            // Show All Targets
            foreach (int[] cPoint_Pos_OCenter in postions_OriginCenter_List)
            {
                Ellipse circle = Draw_Circle_OCenter((double)targetSizePixal, cPoint_Pos_OCenter);

                circle.Fill = new SolidColorBrush(targetColor);
                wholeGrid.Children.Add(circle);
            }
            wholeGrid.UpdateLayout();

            Win_allTargets.Owner = this;
        }


        private void CreateEditBox(object sender)
        {
            // Get the position and width/height of selected Item
            ListBoxItem lbi = (ListBoxItem)listBox_Positions.ItemContainerGenerator.ContainerFromItem(listBox_Positions.SelectedItem);
            Point pt = lbi.TransformToAncestor(this).Transform(new Point(0, 0));

            double delta = 3;
            editBox_Pos.HorizontalAlignment = HorizontalAlignment.Left;
            editBox_Pos.VerticalAlignment = VerticalAlignment.Top;
            editBox_Pos.Margin = new Thickness(pt.X + delta, pt.Y + delta, 0, 0);
            editBox_Pos.Width = lbi.ActualWidth;
            editBox_Pos.Height = lbi.ActualHeight;

            editBox_Pos.Visibility = Visibility.Visible;
            editBox_Pos.Focus();

            editBox_Pos.Text = (String)lbi.Content;
            Grid_setupTarget.UpdateLayout();

            editBox_Pos.KeyDown += new KeyEventHandler(this.EditOver);

        }

        private void EditOver(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                try
                {
                    string[] strxy = editBox_Pos.Text.Split(',');
                    parent.optPostions_OCenter_List[indexSelected] = new int[] { int.Parse(strxy[0]), int.Parse(strxy[1]) };

                    optPosString_List[indexSelected] = editBox_Pos.Text;
                    listBox_Positions.ItemsSource = null;
                    listBox_Positions.ItemsSource = optPosString_List;
                    editBox_Pos.Visibility = Visibility.Hidden;
                }
                catch
                {
                    editBox_Pos.Text = "";
                }


            }
        }

        private void TextBox_targetNoOfPositions_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                parent.targetNoOfPositions = int.Parse(textBox_targetNoOfPositions.Text);
                GenPositions();
            }
            catch(Exception)
            {
            }

        }


        private void Btn_OK_Click(object sender, RoutedEventArgs e)
        {
            SaveTargetData();
            ResumeBtnStartStop();
            this.Close();
        }

        private void ListBox_Positions_KeyDown(object sender, KeyEventArgs e)
        {
            indexSelected = listBox_Positions.SelectedIndex;
            if (e.Key == Key.F2)
                CreateEditBox(sender);
        }

        private void ListBox_Positions_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            indexSelected = listBox_Positions.SelectedIndex;
            CreateEditBox(sender);
        }

        private void Btn_Cancle_Click(object sender, RoutedEventArgs e)
        {
            ResumeBtnStartStop();
            this.Close();
        }
    }
}
