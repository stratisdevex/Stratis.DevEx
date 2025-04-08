using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using System.Windows.Controls;
using System.Windows.Input;

using Hardcodet.Wpf.GenericTreeView;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

using Stratis.VS.StratisEVM.UI.ViewModel;
using Wpf.Ui;
using System.IO.Packaging;

namespace Stratis.VS.StratisEVM.UI
{
    /// <summary>
    /// Interaction logic for BlockchainExplorerToolWindowControl.
    /// </summary>
    public partial class BlockchainExplorerToolWindowControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BlockchainExplorerToolWindowControl"/> class.
        /// </summary>
        public BlockchainExplorerToolWindowControl()
        {
            var _ = new Wpf.Ui.Controls.Card(); // Bug workaround, see https://github.com/microsoft/XamlBehaviorsWpf/issues/86
            InitializeComponent();
            this.BlockchainExplorerTree.MouseDoubleClick += BlockchainExplorerTree_MouseDoubleClick;
        }

        #region Event handlers
        private void BlockchainExplorerTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            
        }

        private void OnSelectedItemChanged(object sender, RoutedTreeItemEventArgs<BlockchainInfo> e)
        {

            if (sender is BlockchainExplorerTree tree && tree.SelectedItem != null)
            {
                if (tree.SelectedItem.Name == "Stratis Mainnet" && tree.SelectedItem.Kind == BlockchainInfoKind.Network)
                {

                    //e.Handled = true;
                }
            }
        }

        private async void NewNetworkCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {

            /*
            var dw = new BlockchainExplorerDialogWindow();
            
           
            dw.Content = (StackPanel)TryFindResource("AddNetworkDialogContent");
            
            dw.ShowModal();
            var sp = (StackPanel)dw.Content;
            var to = (StackPanel)sp.Children[0]; 
            var t = (Wpf.Ui.Controls.TextBox) to.Children[1];   
            var te = t.Text;
            */

            try
            {
                var dw = new BlockchainExplorerDialog(RootContentDialog)
                {
                    Title = "Add EVM Network",
                    PrimaryButtonIcon = new SymbolIcon(SymbolRegular.Save20),
                    Content = (StackPanel)TryFindResource("AddNetworkDialogContent"),
                    PrimaryButtonText = "Save",
                    CloseButtonText = "Cancel",
                    Height = 400
                };

                var r = await dw.ShowAsync();
                if (r != ContentDialogResult.Primary)
                {
                    return;
                }
                var sp = (StackPanel)dw.Content;
                var to = (StackPanel)sp.Children[0];
                var t = (Wpf.Ui.Controls.TextBox)to.Children[1];
                var te = t.Text;
            }
            catch
            {

            }


        }
        #endregion

        #region Fields
        internal BlockchainExplorerToolWindow window;
        #endregion

    }
}