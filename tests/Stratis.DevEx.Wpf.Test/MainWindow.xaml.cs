using Stratis.VS.StratisEVM.UI;
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

namespace Stratis.VS.StratisEVM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.toolWindowControl.BlockchainExplorerTree.MouseDoubleClick += BlockchainExplorerTree_MouseDoubleClick;
        }

        private void BlockchainExplorerTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is BlockchainExplorerTree tree && tree.SelectedItem != null)
            {
                if (tree.SelectedItem.Name == "Stratis Mainnet" && tree.SelectedItem.Kind == UI.ViewModel.BlockchainInfoKind.Network)
                {
                    contentControl.Content = dashboardToolWindowControl;
                }
                e.Handled = true;   
            }
        }

        public StratisDashboardToolWindowControl dashboardToolWindowControl = new StratisDashboardToolWindowControl();  
    }
}
