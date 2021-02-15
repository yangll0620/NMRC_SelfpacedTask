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


        // Window and Grid showing the targets
        Window Win_allTargets;
        Grid wholeGrid;

        public SetupTargetsWin(MainWindow parentWindow)
        {
            InitializeComponent();


            parent = parentWindow;

            DisableBtnStartStop();

            LoadInitTargetData();

        }

        private void LoadInitTargetData()
        {
            // Fill in textBox_targetDiaCM
            textBox_targetDiaCM.Text = parent.targetDiaCM.ToString();
            

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
        }

        private void GenPositions()
        {/*
                Generate the optional X, Y Positions (origin in center)

                Store into class member parent.optPostions_OCenter_List
                and Show on the control listBox_Positions
            */

            sd.Rectangle Rect_touchScreen = Utility.Detect_PrimaryScreen_Rect();
            parent.optPostions_OCenter_List = Utility.GenPositions(parent.targetNoOfPositions, parent.targetDiaPixal, Rect_touchScreen);

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

            parent.targetDiaCM = float.Parse(textBox_targetDiaCM.Text);
            parent.targetDiaPixal = Utility.cm2pixal(parent.targetDiaCM);
            parent.targetNoOfPositions = int.Parse(textBox_targetNoOfPositions.Text);

            parent.closeMarginPercentage = float.Parse(textBox_closeMargin.Text);
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

            Color BKColor = (Color)(typeof(Colors).GetProperty(parent.BKTargetShownColorStr) as PropertyInfo).GetValue(null, null);
            Color targetColor = (Color)(typeof(Colors).GetProperty(parent.goColorStr) as PropertyInfo).GetValue(null, null); ;

            ShowAllTargets(parent.targetDiaPixal, parent.optPostions_OCenter_List, targetColor, BKColor);
        }


        private void ShowAllTargets(int targetDiaPixal, List<int[]> postions_OriginCenter_List, Color targetColor, Color BKColor)
        {/* 
            Show all the targets 

            Args:
                targetDiaPixal: Target Diameter in Pixal

                postions_OriginCenter_List: x,y Position for Each Target (Origin in Screen Center)

                targetColor: the target Color

                BKColor: the Background Color
            */


            //Show the Win_allTargets on the Touch Screen
            Window Win_allTargets = new Window();
            sd.Rectangle Rect_primaryScreen = Utility.Detect_PrimaryScreen_Rect();
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
                // Change the cPoint  into Top Left Coordinate System
                sd.Rectangle Rect_touchScreen = Utility.Detect_PrimaryScreen_Rect();
                int[] cPoint_Pos_OTopLeft = new int[] { cPoint_Pos_OCenter[0] + Rect_touchScreen.Width / 2, cPoint_Pos_OCenter[1] + Rect_touchScreen.Height / 2 };

                Ellipse circle = Utility.Create_Circle((double)targetDiaPixal, new SolidColorBrush(targetColor));
                Utility.Move_Circle_OTopLeft(circle, cPoint_Pos_OTopLeft);

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


        private void Create_ShowGrid(Color BKColor)
        {
            //Show the Win_allTargets on the Touch Screen
            Win_allTargets = new Window();
            sd.Rectangle Rect_primaryScreen = Utility.Detect_PrimaryScreen_Rect();
            Win_allTargets.Top = Rect_primaryScreen.Top;
            Win_allTargets.Left = Rect_primaryScreen.Left;


            // Show Background
            Win_allTargets.Background = new SolidColorBrush(BKColor);
            Win_allTargets.Show();
            Win_allTargets.WindowState = WindowState.Maximized;
            Win_allTargets.Name = "childWin_ShowAllTargets";
            Win_allTargets.Title = "All Optional Targets";
            Win_allTargets.Owner = this;


            // Add a Grid
            wholeGrid = new Grid();
            wholeGrid.Height = Win_allTargets.ActualHeight;
            wholeGrid.Width = Win_allTargets.ActualWidth;
            Win_allTargets.Content = wholeGrid;
            wholeGrid.UpdateLayout();

        }

        private void AddTarget(int targetDiaPixal, int[] cPoint_Pos_OCenter, Color targetColor)
        {/* 
            Add a new target into the Grid 

            Args:
                targetDiaPixal: Target Diameter in Pixal

                cPoint_Pos_OCenter: x,y Position for the Target (Origin in Screen Center)

                targetColor: the target Color

                BKColor: the Background Color
            */


            // Change the cPoint  into Top Left Coordinate System
            sd.Rectangle Rect_touchScreen = Utility.Detect_PrimaryScreen_Rect();
            int[] cPoint_Pos_OTopLeft = new int[] { cPoint_Pos_OCenter[0] + Rect_touchScreen.Width / 2, cPoint_Pos_OCenter[1] + Rect_touchScreen.Height / 2 };


            // Add A new Target
            Ellipse circle = Utility.Create_Circle((double)targetDiaPixal, new SolidColorBrush(targetColor));
            Utility.Move_Circle_OTopLeft(circle, cPoint_Pos_OTopLeft);

            wholeGrid.Children.Add(circle);
            wholeGrid.UpdateLayout();  
        }


        private void Btn_test_Click(object sender, RoutedEventArgs e)
        {
            try {
                string[] strxy = textBox_Pos.Text.Split(',');
                int[] pos_OCenter_Pixal = new int[] { int.Parse(strxy[0]), int.Parse(strxy[1]) };

                Color BKColor = (Color)(typeof(Colors).GetProperty(parent.BKTargetShownColorStr) as PropertyInfo).GetValue(null, null);
                Color targetColor = (Color)(typeof(Colors).GetProperty(parent.goColorStr) as PropertyInfo).GetValue(null, null); ;


                if (wholeGrid == null)
                {
                    Create_ShowGrid(BKColor);
                }

                AddTarget(parent.targetDiaPixal, pos_OCenter_Pixal, targetColor);

            }
            catch
            {

            }
        }

        private void Btn_test2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                wholeGrid.Children.RemoveAt(wholeGrid.Children.Count - 1);
            }
            catch
            {

            }
        }

        private void TextBox_targetDiaCM_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void TextBox_targetDiaCM_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                parent.targetDiaCM = float.Parse(textBox_targetDiaCM.Text);
                parent.targetDiaPixal = Utility.cm2pixal(parent.targetDiaCM);
                textBox_DiaPixal.Text = parent.targetDiaPixal.ToString();
            }
        }

        private void Btn_Cancle_Click(object sender, RoutedEventArgs e)
        {
            ResumeBtnStartStop();
            this.Close();
        }
    }
}
