using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
//using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;
using Hardcodet.Wpf.GenericTreeView;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

using Stratis.VS.StratisEVM.UI.ViewModel;
using Wpf.Ui;

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
            cds.SetDialogHost(RootContentDialog);
            this.BlockchainExplorerTree.MouseDoubleClick += BlockchainExplorerTree_MouseDoubleClick;
        }

        #region Event handlers
        private void BlockchainExplorerTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is BlockchainExplorerTree tree && tree.SelectedItem != null)
            {
                if (tree.SelectedItem.Name == "Stratis Mainnet" && tree.SelectedItem.Kind == BlockchainInfoKind.Network)
                {
                    IVsUIShell vsUIShell = (IVsUIShell) Package.GetGlobalService(typeof(SVsUIShell));
                    Guid guid = typeof(StratisEVMBlockchainDashboardToolWindow).GUID;
                    IVsWindowFrame windowFrame;
                    int result = vsUIShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fFindFirst, ref guid, out windowFrame);   // Find MyToolWindow

                    if (result != VSConstants.S_OK)
                        result = vsUIShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref guid, out windowFrame); // Crate MyToolWindow if not found

                    if (result == VSConstants.S_OK)                                                                           // Show MyToolWindow
                        ErrorHandler.ThrowOnFailure(windowFrame.Show());
                }
                e.Handled = true;
            }
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
            var dw = new ContentDialog(RootContentDialog)
            {
                Title = "Add EVM Network",
                PrimaryButtonIcon = new SymbolIcon(SymbolRegular.Save20),
                Content = (StackPanel)TryFindResource("AddNetworkDialogContent"),
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                Background = (System.Windows.Media.Brush) TryFindResource(EnvironmentColors.ToolWindowBackgroundBrushKey)
            }; 
            try
            {
                var r = await dw.ShowAsync(); 
                if (r!= ContentDialogResult.Primary) 
                {
                    return;
                }
            }
            catch {
                return;
            }
            var sp = (StackPanel)dw.Content;
            var to = (StackPanel)sp.Children[0];
            var t = (Wpf.Ui.Controls.TextBox)to.Children[1];
            var te = t.Text;

        }
        #endregion

        #region Fields
        internal BlockchainExplorerToolWindow window;
        internal ContentDialogService cds = new ContentDialogService();

        public string AddNetworkName;
        #endregion

    }
}