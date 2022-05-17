using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace SelfpacedTask
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
            cbo_BKWaitTrialColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_BKGoInterfaceColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_BDCorrFeedbackInterfaceColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_BDErrorFeedbackInterfaceColor.ItemsSource = typeof(Colors).GetProperties();


            // Set Default Selected Item
            cbo_BKWaitTrialColor.SelectedItem = typeof(Colors).GetProperty(parent.BKWaitTrialColorStr);
            cbo_BKGoInterfaceColor.SelectedItem = typeof(Colors).GetProperty(parent.BKGoInterfaceColorStr);
            cbo_BDCorrFeedbackInterfaceColor.SelectedItem = typeof(Colors).GetProperty(parent.CorrFeedbackBDInterfaceColorStr);
            cbo_BDErrorFeedbackInterfaceColor.SelectedItem = typeof(Colors).GetProperty(parent.ErrorFeedbackBDInterfaceColorStr);
        }

        private void SaveColorsData()
        { /* ---- Save all the Select Colors Information back to MainWindow Color Strings ----- */


            parent.BKWaitTrialColorStr = (cbo_BKWaitTrialColor.SelectedItem as PropertyInfo).Name;
            parent.BKGoInterfaceColorStr = (cbo_BKGoInterfaceColor.SelectedItem as PropertyInfo).Name;

            parent.CorrFeedbackBDInterfaceColorStr = (cbo_BDCorrFeedbackInterfaceColor.SelectedItem as PropertyInfo).Name;
            parent.ErrorFeedbackBDInterfaceColorStr = (cbo_BDErrorFeedbackInterfaceColor.SelectedItem as PropertyInfo).Name;
        }

        private void Btn_Save_Click(object sender, RoutedEventArgs e)
        {
            SaveColorsData();
            ResumeBtnStartStop();
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ResumeBtnStartStop();
        }

        private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            ResumeBtnStartStop();
            this.Close();
        }
    }
}
