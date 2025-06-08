using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;

namespace Stratis.VS.StratisEVM.UI
{
    /// <summary>
    /// Interaction logic for DeploySolidityProjectToolWindowControl.
    /// </summary>
    public partial class DeploySolidityProjectToolWindowControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeploySolidityProjectToolWindowControl"/> class.
        /// </summary>
        public DeploySolidityProjectToolWindowControl()
        {
            this.InitializeComponent();
            
            VSTheme.WatchThemeChanges();
            this.DeployProfileComboBox.ItemsSource = GetDeployProfiles();
            this.DeployProfileComboBox.SelectedIndex = 0;
        }

        //public Dep
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
                "DeploySolidityProjectToolWindow");
        }

        private string[] GetDeployProfiles()
        {
#if IS_VSIX
            return new string[] {};
#else
            return new[] { "Deploy 1", "Deploy 2", "Deploy 3" };
#endif
        }
    }
}