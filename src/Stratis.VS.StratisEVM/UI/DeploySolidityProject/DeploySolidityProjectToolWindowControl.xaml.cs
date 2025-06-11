using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;

using Microsoft.VisualStudio.Shell;

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
            
        }

        public void InitSelectedProject()
        {
            if (!VSUtil.IsProjectLoaded())
            {
                return;
            }
            var project = VSUtil.GetSelectedProject();
            if (project == null)
            {
                MessageBox.Show("No project selected. Please select a Solidity project to deploy.", "Deploy Solidity Project", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var contracts = VSUtil.GetSolidityProjectContracts(project);
            this.DeployContractComboBox.ItemsSource = contracts;
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
            //var contracs = project.ProjectItems.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder
            //    ? project.ProjectItems.Cast<EnvDTE.ProjectItem>().Where(pi => pi.Name.EndsWith(".sol")).ToList()
            //    : project.ProjectItems.Cast<EnvDTE.ProjectItem>().Where(pi => pi.Name.EndsWith(".sol")).ToList();
            return Array.Empty<string>();
#else
            return new[] { "Deploy 1", "Deploy 2", "Deploy 3" };
#endif
        }
    }
}