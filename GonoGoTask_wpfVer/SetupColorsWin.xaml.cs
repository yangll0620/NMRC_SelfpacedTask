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
    /// Interaction logic for SetupColorsWin.xaml
    /// </summary>
    public partial class SetupColorsWin : Window
    {
        public SetupColorsWin()
        {
            InitializeComponent();
            BindingComboData();
        }

        private void BindingComboData()
        {
            //Data binding the Color ComboBoxes
            cbo_goColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_nogoColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_BKWaitTrialColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_BKTrialColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_CorrFillColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_CorrOutlineColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_ErrorFillColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_ErrorOutlineColor.ItemsSource = typeof(Colors).GetProperties();
        }
    }
}
