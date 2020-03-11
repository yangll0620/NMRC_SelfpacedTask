using System.Windows;

namespace GonoGoTask_wpfVer
{
    /// <summary>
    /// Interaction logic for SetupTimeWin.xaml
    /// </summary>
    public partial class SetupTimeWin : Window
    {
        public MainWindow parent;

        private bool BtnStartState, BtnStopState;

        public SetupTimeWin(MainWindow mainWindow)
        {
            InitializeComponent();

            parent = mainWindow;
            DisableBtnStartStop();

            LoadInitTimeData();
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

        private void LoadInitTimeData()
        {// Load Initial Time Data from MainWindow

            textBox_tReady_min.Text = parent.tRange_ReadyTime[0].ToString();
            textBox_tReady_max.Text = parent.tRange_ReadyTime[1].ToString();

            textBox_tCue_min.Text = parent.tRange_CueTime[0].ToString();
            textBox_tCue_max.Text = parent.tRange_CueTime[1].ToString();

            textBox_tNogoShow_min.Text = parent.tRange_NogoShowTime[0].ToString();
            textBox_tNogoShow_max.Text = parent.tRange_NogoShowTime[1].ToString();


            textBox_MaxReachTime.Text = parent.tMax_ReachTimeS.ToString();
            textBox_MaxReactionTime.Text = parent.tMax_ReactionTimeS.ToString();

            textBox_tVisFeedback.Text = parent.t_VisfeedbackShow.ToString(); 
        }

        private void SaveTimeData()
        {/* ---- Save all the Set Time Information back to MainWindow Variables ----- */
            parent.tRange_ReadyTime[0] = float.Parse(textBox_tReady_min.Text);
            parent.tRange_ReadyTime[1] = float.Parse(textBox_tReady_max.Text);

            parent.tRange_CueTime[0] = float.Parse(textBox_tCue_min.Text);
            parent.tRange_CueTime[1] = float.Parse(textBox_tCue_max.Text);

            parent.tRange_NogoShowTime[0] = float.Parse(textBox_tNogoShow_min.Text);
            parent.tRange_NogoShowTime[1] = float.Parse(textBox_tNogoShow_max.Text);


            parent.tMax_ReachTimeS = float.Parse(textBox_MaxReachTime.Text);
            parent.tMax_ReactionTimeS = float.Parse(textBox_MaxReactionTime.Text);

            parent.t_VisfeedbackShow = float.Parse(textBox_tVisFeedback.Text);
        }

        private void Btn_Cancle_Click(object sender, RoutedEventArgs e)
        {
            ResumeBtnStartStop();
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ResumeBtnStartStop();
        }

        private void Btn_OK_Click(object sender, RoutedEventArgs e)
        {
            SaveTimeData();
            ResumeBtnStartStop();
            this.Close();
        }
    }
}
