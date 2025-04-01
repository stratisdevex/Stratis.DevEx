using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using System.Threading.Tasks;

using Hardcodet.Wpf.GenericTreeView;

using Stratis.VS.StratisEVM.UI.ViewModel;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;

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
            this.InitializeComponent();
            this.BlockchainExplorerTree.MouseDoubleClick += BlockchainExplorerTree_MouseDoubleClick;
        }

        private void BlockchainExplorerTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is BlockchainExplorerTree tree && tree.SelectedItem != null)
            {
                if (tree.SelectedItem.Name == "Stratis Mainnet" && tree.SelectedItem.Kind == UI.ViewModel.BlockchainInfoKind.Network)
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

        //protected override void OnInitialized(System.EventArgs e)
        //{
        //    base.OnInitialized(e);
        //    var x = 1;
        //}
        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Invoked '{0}'", this.ToString()),
                "BlockchainExplorerToolWindow");
        }

        internal BlockchainExplorerToolWindow window;
        /// <summary>
        /// Handles the tree's <see cref="TreeViewBase{T}.SelectedItemChanged"/>
        /// event and updates the status bar.
        /// </summary>
        private async Task OnSelectedItemChangedAsync(object sender, RoutedTreeItemEventArgs<BlockchainInfo> e) 
        {
            await Task.CompletedTask;

        }

        private void OnSelectedItemChanged(object sender, RoutedTreeItemEventArgs<BlockchainInfo> e)
        {
            
            e.Handled = true;
        }

        private void NewNetworkCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var add = new BlockchainExplorerAddNetworkDialog();
            add.ShowDialog();

        }

        
    }
}