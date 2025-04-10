using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using System.Windows.Controls;
using System.Windows.Input;

using Hardcodet.Wpf.GenericTreeView;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

using Stratis.VS.StratisEVM.UI.ViewModel;
using Wpc = Wpf.Ui.Controls;
using System.IO.Packaging;
using System.Windows;

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
            var w = (BlockchainExplorerToolWindowControl)sender;
            var tree = w.BlockchainExplorerTree;
            
          
            try
            {
                var dw = new BlockchainExplorerDialog(RootContentDialog)
                {
                    Title = "Add EVM Network",
                    PrimaryButtonIcon = new SymbolIcon(SymbolRegular.Save20),
                    Content = (StackPanel)TryFindResource("AddNetworkDialogContent"),
                    PrimaryButtonText = "Save",
                    CloseButtonText = "Cancel"
                };
                var sp = (StackPanel)dw.Content;
                var name = (Wpc.TextBox)((StackPanel)sp.Children[0]).Children[1];
                var rpcurl = (Wpc.TextBox)((StackPanel)sp.Children[1]).Children[1];
                var chainid = (Wpc.NumberBox)((StackPanel)sp.Children[2]).Children[1];
                var validForClose = false;
                dw.ButtonClicked += (cd, args) =>
                {
                    if (args.Button == ContentDialogButton.Primary)
                    {
                        
                        if (!string.IsNullOrEmpty(name.Text) && !string.IsNullOrEmpty(rpcurl.Text) && !string.IsNullOrEmpty(chainid.Text))
                        {
                            validForClose = true;   
                            return;
                        }
                        else
                        {
                           
                        }
                    }
                    else
                    {
                        validForClose = true;    
                    }
                };
                dw.Closing += (d, args) =>
                {
                    args.Cancel = !validForClose;
                   
                };
                var r = await dw.ShowAsync();
                if (r != ContentDialogResult.Primary)
                {
                    return;
                }
                tree.SelectedItem.AddChild(BlockchainInfoKind.Network, name.Text);
                //var t = (Wpf.Ui.Controls.TextBox)to.Children[1];
                //if (name == null)
            }
            catch
            {

            }


        }

        private void Dw_ButtonClicked(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            throw new NotImplementedException();
        }

        private void Dw_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Fields
        internal BlockchainExplorerToolWindow window;
        #endregion

    }
}