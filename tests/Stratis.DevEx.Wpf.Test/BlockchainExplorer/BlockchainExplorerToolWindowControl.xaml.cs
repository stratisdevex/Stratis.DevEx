﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using System.Threading.Tasks;

using Hardcodet.Wpf.GenericTreeView;

using Stratis.VS.StratisEVM.UI.ViewModel;

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
            this.InitializeComponent();
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