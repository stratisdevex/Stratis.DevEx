using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Wpf.Ui.Controls;
using Wpc = Wpf.Ui.Controls;
using Stratis.DevEx.Ethereum;

using Stratis.VS.StratisEVM.UI.ViewModel;

namespace Stratis.VS.StratisEVM.UI
{
    /// <summary>
    /// Interaction logic for RunSmartContractToolWindowControl.
    /// </summary>
    public partial class RunSmartContractToolWindowControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RunSmartContractToolWindowControl"/> class.
        /// </summary>
        public RunSmartContractToolWindowControl()
        {
            this.InitializeComponent();
            instance = this;
        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show(
            //    string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Invoked '{0}'", this.ToString()),
            //    "RunSmartContractToolWindow");
        }

        #region Properties
        public static RoutedCommand RunContractCmd { get; } = new RoutedCommand();
        #endregion

        #region Fields
        public RunSmartContractToolWindow window;
        public static RunSmartContractToolWindowControl instance;
        #endregion

        private void CommandBinding_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            //MessageBox.Show(
            //    string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Invoked '{0}'", this.ToString()),
            //    "RunSmartContractToolWindow");
            if (e.Source != null && e.Source is BlockchainExplorerTree tree)
            {

            }
        }

        internal async void RunContract(BlockchainInfo contract)
        {
            
           
            //var window = (RunSmartContractToolWindowControl)sender;
            //var tree = window.BlockchainExplorerTree;
            //var item = GetSelectedItem(sender);
            var endpoint = contract.Data["Endpoint"];
            var abi = Contract.DeserializeABI((string)contract.Data["Abi"]);
            
           

           
        }

        private void ShowValidationErrors(Wpc.TextBlock textBlock, string message)
        {
            textBlock.Visibility = Visibility.Visible;
            textBlock.Text = message;
        }

    }
}