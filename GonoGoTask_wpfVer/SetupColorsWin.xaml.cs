using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GonoGoTask_wpfVer
{
    /// <summary>
    /// Interaction logic for SetupColorsWin.xaml
    /// </summary>
    public partial class SetupColorsWin : Window
    {
        MainWindow parent;
        private bool BtnStartState, BtnStopState;

        public SetupColorsWin(MainWindow parentWindow)
        {
            InitializeComponent();

            parent = parentWindow;
            DisableBtnStartStop();

            BindingComboData();  
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

        private void BindingComboData()
        {
            //Data binding the Color ComboBoxes
            cbo_goColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_nogoColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_cueColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_BKWaitTrialColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_BKTrialColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_CorrFillColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_CorrOutlineColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_ErrorFillColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_ErrorOutlineColor.ItemsSource = typeof(Colors).GetProperties();


            // Set Default Selected Item
            cbo_goColor.SelectedItem = typeof(Colors).GetProperty(parent.goColorStr);
            cbo_nogoColor.SelectedItem = typeof(Colors).GetProperty(parent.nogoColorStr);
            cbo_cueColor.SelectedItem = typeof(Colors).GetProperty(parent.cueColorStr);
            cbo_BKWaitTrialColor.SelectedItem = typeof(Colors).GetProperty(parent.BKWaitTrialColorStr);
            cbo_BKTrialColor.SelectedItem = typeof(Colors).GetProperty(parent.BKTrialColorStr);
            cbo_CorrFillColor.SelectedItem = typeof(Colors).GetProperty(parent.CorrFillColorStr);
            cbo_CorrOutlineColor.SelectedItem = typeof(Colors).GetProperty(parent.CorrOutlineColorStr);
            cbo_ErrorFillColor.SelectedItem = typeof(Colors).GetProperty(parent.ErrorFillColorStr);
            cbo_ErrorOutlineColor.SelectedItem = typeof(Colors).GetProperty(parent.ErrorOutlineColorStr);
        }

        private void SaveColorsData()
        { /* ---- Save all the Select Colors Information back to MainWindow Color Strings ----- */

            parent.goColorStr = (cbo_goColor.SelectedItem as PropertyInfo).Name;
            parent.nogoColorStr = (cbo_nogoColor.SelectedItem as PropertyInfo).Name;
            parent.BKWaitTrialColorStr = (cbo_BKWaitTrialColor.SelectedItem as PropertyInfo).Name;
            parent.BKTrialColorStr = (cbo_BKTrialColor.SelectedItem as PropertyInfo).Name;
            parent.CorrFillColorStr = (cbo_CorrFillColor.SelectedItem as PropertyInfo).Name;
            parent.CorrOutlineColorStr = (cbo_CorrOutlineColor.SelectedItem as PropertyInfo).Name;
            parent.ErrorFillColorStr = (cbo_ErrorFillColor.SelectedItem as PropertyInfo).Name;
            parent.ErrorOutlineColorStr = (cbo_ErrorOutlineColor.SelectedItem as PropertyInfo).Name;
        }

        private void Btn_OK_Click(object sender, RoutedEventArgs e)
        {
            SaveColorsData();
            ResumeBtnStartStop();
            this.Close();
        }

        private void Btn_Cancle_Click(object sender, RoutedEventArgs e)
        {
            ResumeBtnStartStop();
            this.Close();
        }
    }
}
