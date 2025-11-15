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
using System.Windows.Navigation;
using System.Windows.Shapes;


using Microsoft.VisualStudio.PlatformUI;
namespace Stratis.VS.StratisEVM
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class Window1 : DialogWindow
    {
        public Window1()
        {
            InitializeComponent();
            //InitializeComponent();
        }

        public string SelectedEVMVersion { get; set; } = "london";

        public string SelectedCompilerVersion { get; set; } =  "0.8.27";

        private void okButton_Click(object sender, RoutedEventArgs e) =>
    DialogResult = true;

        private void cancelButton_Click(object sender, RoutedEventArgs e) =>
            DialogResult = false;

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Windows.Controls.ComboBoxItem item = (System.Windows.Controls.ComboBoxItem)e.AddedItems[0];
            if (item != null && item.Content != null)
            {
                SelectedEVMVersion = (string)item.Content;
            }
        }

        private void ComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            System.Windows.Controls.ComboBoxItem item = (System.Windows.Controls.ComboBoxItem) e.AddedItems[0];
            if (item != null && item.Content != null)
            {
                SelectedCompilerVersion = (string) item.Content;
            }
        }
    }
}
