using Stratis.VS.StratisEVM.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
            var b = BlockchainInfo.Load("BlockchainExplorerTree", out var e);
            if (e != null)
            {
                MessageBox.Show($"Error loading blockchain info: {e?.Message ?? "(null)"}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return Array.Empty<string>();
            }
            else if (b == null)
            {
                return Array.Empty<string>();
            }
            else
            {
                deployProfiles = b.GetAllDeployProfiles();
                return deployProfiles.Keys.ToArray();
            }
#else
            return new[] { "Deploy Profile 1", "Deploy Profile 2", "Deploy Profile 3" };
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

        private void DeployButton_Click(object sender, RoutedEventArgs e)
        {
            /*
            if (DeployContractComboBox.SelectedItem == null || DeployProfileComboBox.SelectedItem == null)
            {
                ShowDeployError("Select a Solidity smart contract to deploy from the project and a deploy profile to use.");
                return;
            }
            */
            var project = VSUtil.GetSelectedProject();
            var contractFileName = DeployContractComboBox.SelectedItem.ToString();
            //var deployProfileName = DeployProfileComboBox.SelectedItem.ToString();
            if (!VSUtil.BuildProject(project))
            {
                ShowDeployError("Build failed. Please check the build output for errors.");
                return;
            }

            var bo = VSUtil.GetSmartContractProjectOutput(project, contractFileName);

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ShowDeployError("Cancelled.");
        }
        #endregion

        #region Methods
        public void ShowDeployInfoStatus(string text)
        {
            DeployErrorsTextBlock.Visibility = Visibility.Hidden;
            DeployStatusStackPanel.Visibility = Visibility.Visible; 
            DeployProgressRing.IsEnabled = true;
            DeploySolidityProjectStatusTextBlock.Text = text;
        }

        public void ShowDeployError(string text)
        {
            DeployStatusStackPanel.Visibility = Visibility.Hidden;
            DeployProgressRing.IsEnabled = false;
            DeployErrorsTextBlock.Visibility = Visibility.Visible;
            DeployErrorsTextBlock.Text = text;  
        }
        #endregion

        #region Fields
        protected bool projectInitialized = false;
        protected Dictionary<string, BlockchainInfo> deployProfiles = new Dictionary<string, BlockchainInfo>();
        #endregion

        
    }
}