using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;

namespace Stratis.VS.StratisEVM.UI
{
    public partial class DeploySolidityProjectToolWindowControl : UserControl
    {
        public DeploySolidityProjectToolWindowControl()
        {
            this.InitializeComponent();
#if !IS_VSIX
            InitSelectedProject();
#endif
            VSTheme.WatchThemeChanges(); 
        }

        public void InitSelectedProject()
        {
#if IS_VSIX
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
            var profiles = GetDeployProfiles();
#else
            var contracts = new[] { "Contract1.sol", "Contract2.sol", "Contract3.sol" };    
            var profiles = new[] { "Deploy profie 1", "Deploy 2", "Deploy 3" };    
#endif
            this.DeployContractComboBox.ItemsSource = contracts;
            this.DeployContractComboBox.SelectedIndex = 0;  
            this.DeployProfileComboBox.ItemsSource = profiles;
            this.DeployProfileComboBox.SelectedIndex = 0;
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

        #region Event Handlers

        private void EstimatedGasFeeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (CustomGasFeeNumberBox != null)
            {
                CustomGasFeeNumberBox.IsEnabled = false;
            }
        }

        private void CustomGasFeeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (CustomGasFeeNumberBox != null)
            {
                CustomGasFeeNumberBox.IsEnabled = true;
            }   
            
        }
        #endregion
    }
}