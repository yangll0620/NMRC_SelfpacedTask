using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Interaction logic for SetupTargetsWin.xaml
    /// </summary>
    public partial class SetupTargetsWin : Window
    {
        public MainWindow parent;

        private bool BtnStartState, BtnStopState;
        public SetupTargetsWin(MainWindow parentWindow)
        {
            InitializeComponent();
            parent = parentWindow;

            DisableBtnStartStop();

            LoadInitTargetData();

        }

        private void LoadInitTargetData()
        {
            textBox_closeMargin.Text = parent.closeMarginPercentage.ToString();
            textBox_targetDiameter.Text = parent.targetDiameterInch.ToString();
            textBox_targetDisfromCenter.Text = parent.targetDisFromCenterInch.ToString();
        }

        private void SaveTargetData()
        {/* ---- Save all the Set Target Information back to MainWindow Variables ----- */

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

        private void Btn_OK_Click(object sender, RoutedEventArgs e)
        {
            SaveTargetData();

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
