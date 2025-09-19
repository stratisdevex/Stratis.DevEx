using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

using Microsoft.VisualStudio.Shell;
using Nethereum.Hex.HexTypes;

using Wpc = Wpf.Ui.Controls;

using static Stratis.DevEx.Result;
using Stratis.DevEx.Ethereum;
using Stratis.VS.StratisEVM.UI.ViewModel;


namespace Stratis.VS.StratisEVM.UI
{
    public partial class DeploySolidityProjectToolWindowControl : UserControl
    {
        public DeploySolidityProjectToolWindowControl()
        {
            this.InitializeComponent();
            var b = BlockchainInfo.Load("BlockchainExplorerTree", out var e);
            if (b == null)
            {
                BlockchainViewModel.CreateInitialTreeData();
            }
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
            string evmversion = VSUtil.GetProjectProperty(project, "EVMVersion");
#else
            var contracts = new[] { "Contract1.sol", "Contract2.sol", "Contract3.sol" };    
            var profiles = new[] { "Deploy profie 1", "Deploy 2", "Deploy 3" }; 
            string evmversion = "london"; 
#endif

            this.ProjectEVMVersionStackPanel.Visibility = Visibility.Visible;
            this.ProjectEVMVersionLabel.Content = evmversion;
            this.DeployContractComboBox.ItemsSource = contracts;
            this.DeployContractComboBox.SelectedIndex = 0;  
            this.DeployProfileComboBox.ItemsSource = profiles;
            this.DeployProfileComboBox.SelectedIndex = 0;
            this.DeploySolidityProjectDialogStackPanel.Visibility = Visibility.Visible;
            this.DeployStatusStackPanel.Visibility = Visibility.Hidden;
            this.DeploySuccessStackPanel.Visibility = Visibility.Hidden; 
            this.DeployErrorsStackPanel.Visibility = Visibility.Hidden; 
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

        private void DeployContractComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CreateDeployContractParamsInputElements();
        }

        
        private void DeployButton_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (DeployContractComboBox.SelectedItem == null || DeployProfileComboBox.SelectedItem == null)
            {
                ShowDeployError("Select a Solidity smart contract to deploy from the project and a deploy profile to use.");
                return;
            }
            var project = VSUtil.GetSelectedProject();
            var contract = DeployContractComboBox.SelectedItem.ToString().Split(new string[] { " - " }, StringSplitOptions.None);
            var contractFileName = contract[0] + "." + contract[1];
            var deployProfileName = DeployProfileComboBox.SelectedItem.ToString();
            object[] deployValues = null;
            if (SolidityFileParser.GetConstructorParameters(VSUtil.GetProjectItemFilePath(project, contract[1])).TryGetValue(contract[0], out var deployParamTypes))
            {
                deployValues = GetDeployContractParams(deployParamTypes);  
            }
            ShowDeployProgress($"Building {project.Name} project...");
            if (!VSUtil.BuildProject(project))
            {
                ShowDeployError("Build failed. Please check the build output for errors.");
                return;
            }
            
            var bo = VSUtil.GetSmartContractProjectOutput(project, contractFileName);
            if (!bo.ContainsKey("bin"))
            {
                ShowDeployError($"No bin file found for {contractFileName}. Please check the build output.");
                return;
            }
            if (!bo.ContainsKey("abi"))
            {
                ShowDeployError($"No bin file found for {contractFileName}. Please check the build output.");
                return;
            }

            var b = BlockchainInfo.Load("BlockchainExplorerTree", out var bex);
            if (bex != null || b == null)
            {
                MessageBox.Show($"Error loading blockchain info: {bex?.Message ?? "(null)"}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var deployProfile = b.GetDeployProfile(deployProfileName);  
            if (deployProfile == null)
            {
                ShowDeployError($"Could not retrieve deploy profile {deployProfileName}. Please check the deploy profile name.");
                InitSelectedProject();
                return;
            }
            var network = deployProfile.Parent.Parent;
            ShowDeployProgress($"Deploying {contract[0]} contract to {network}...");
            var bin = "0x" + File.ReadAllText(bo["bin"].FullName);
            var abi = File.ReadAllText(bo["abi"].FullName);
            HexBigInteger gasDeploy = EstimatedGasFeeRadioButton.IsChecked == true ? default : new HexBigInteger((long)CustomGasFeeNumberBox.Value);            
            var result = ThreadHelper.JoinableTaskFactory.Run(() => ExecuteAsync(Network.DeployContract(deployProfile.DeployProfileEndpoint, bin, deployProfile.DeployProfileAccount, null, abi, gasDeploy, deployValues)));
            if (result.IsSuccess)
            {
                var deployedOn = DateTime.Now;
                if (BlockchainExplorerToolWindowControl.ControlIsLoaded)
                {
                    var tree = BlockchainExplorerToolWindowControl.instance.BlockchainExplorerTree.RootItem;
                    var n = tree.GetDeployProfile(deployProfileName).Parent.Parent;
                    n.GetChild("Contracts", BlockchainInfoKind.Folder).AddContract(result.Value.ContractAddress, deployProfile, project.Name, contractFileName, abi, result.Value.TransactionHash, deployedOn);
                    BlockchainExplorerToolWindowControl.instance.BlockchainExplorerTree.Refresh();
                    tree.Save("BlockchainExplorerTree", out Exception se);
                    if (se != null)
                    {
                        ShowDeployError($"Error saving contract to blockchain tree: {se.Message}");
                    }
                    else
                    {
                        ShowDeploySuccess();
                    }
                }
                else
                {
                    var n = b.GetDeployProfile(deployProfileName).Parent.Parent;
                    n.GetChild("Contracts", BlockchainInfoKind.Folder).AddContract(result.Value.ContractAddress, deployProfile, project.Name, contractFileName, abi, result.Value.TransactionHash, deployedOn);
                    b.Save("BlockchainExplorerTree", out Exception se);
                    if (se != null)
                    {
                        ShowDeployError($"Error saving contract to blockchain tree: {se.Message}");
                    }
                    else
                    {
                        ShowDeploySuccess();
                    }
                }
                VSUtil.LogToStratisEVMWindow($"\n========== {contract[0]} contract deployed successfully to {network.Name}. ==========\nTransaction Hash: {result.Value.TransactionHash}\nContract Address: {result.Value.ContractAddress}");  
            }
            else
            {
                ShowDeployError($"Error deploying contract: {result.FailureMessage}");
                VSUtil.LogToStratisEVMWindow($"\n========== Error deploying {contract[0]} contract to {network.Name}. ==========\n{result.FailureMessage}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ShowDeployError("Cancelled.");
        }
        #endregion

        #region Methods
        public void ShowDeployProgress(string text)
        {
            this.DeployErrorsStackPanel.Visibility = Visibility.Hidden;
            this.DeploySuccessStackPanel.Visibility = Visibility.Hidden;
            DeployStatusStackPanel.Visibility = Visibility.Visible; 
            DeployProgressRing.IsEnabled = true;
            DeploySolidityProjectStatusTextBlock.Text = text;
        }

        public void ShowDeploySuccess()
        {
            this.DeployErrorsStackPanel.Visibility = Visibility.Hidden;
            this.DeployStatusStackPanel.Visibility = Visibility.Hidden;
            this.DeploySuccessStackPanel.Visibility = Visibility.Visible;
            this.DeploySuccessTextBlock.Text = $"Contract deployed successfully.";
        }   

        public void ShowDeployError(string text)
        {
            this.DeploySuccessStackPanel.Visibility = Visibility.Hidden;
            this.DeployStatusStackPanel.Visibility = Visibility.Hidden;
            this.DeployErrorsStackPanel.Visibility = Visibility.Visible;
            this.DeployErrorsTextBlock.Text = text; 
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

        private void CreateDeployContractParamsInputElements()
        {
            // Clear existing parameters
            ContractDeployParamsStackPanel.Children.Clear();
            if (DeployContractComboBox.SelectedItem == null) return;
            var project = VSUtil.GetSelectedProject();
            var contract = DeployContractComboBox.SelectedItem.ToString().Split(new string[] { " - " }, StringSplitOptions.None);
            var path = VSUtil.GetProjectItemFilePath(project, contract[1]);
            //var deployParams = SolidityFileParser.GetConstructorParameters(path).tr[contract[0]];
            if (!SolidityFileParser.GetConstructorParameters(path).TryGetValue(contract[0], out var deployParams) || deployParams.Count == 0)
            {
                ContractDeployParamsStackPanel.Visibility = Visibility.Collapsed;
                return;
            }
            ContractDeployParamsStackPanel.Children.Add(new TextBlock() { Text = "Enter the constructor parameters:", Margin = new Thickness(4, 4, 0, 4) });
            foreach (var p in deployParams)
            {
                var sp = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(4, 0, 0, 2)
                };
                var lbl = new TextBlock { Width = 150, VerticalAlignment = VerticalAlignment.Center, FontSize = 11.0 };
                lbl.Inlines.Add(new Run() { Text = p.Key });
                lbl.Inlines.Add(new Run() { Text = $" ({p.Value})", FontStyle = FontStyles.Italic, FontSize = 10.0 });
                var tb = new TextBox() { Name = $"Param{p.Key}TextBox", Width = 150, VerticalAlignment = VerticalAlignment.Center, FontSize = 11.0 };
                sp.Children.Add(lbl);
                sp.Children.Add(tb);
                ContractDeployParamsStackPanel.Children.Add(sp);
            }
            ContractDeployParamsStackPanel.Visibility = Visibility.Visible;
        }

        private object[] GetDeployContractParams(Dictionary<string, string> paramTypes)
        {
            Dictionary<string, (string, string)> paramValues = new Dictionary<string, (string, string)>();
            //List<object> paramValues = new List<object>();
            foreach (var child in ContractDeployParamsStackPanel.Children)
            {
                if (child is StackPanel sp && sp.Children.Count == 2 && sp.Children[0] is TextBlock lbl && sp.Children[1] is TextBox tb)
                {
                    var paramName = (Run) lbl.Inlines.FirstInline;
                    if (paramTypes.ContainsKey(paramName.Text))
                    {
                        paramValues[paramName.Text] = (paramTypes[paramName.Text], tb.Text);
                        //paramValues.Add(tb.Text);   
                    }
                }
            }
            return Contract.ParseFunctionParameterValues(paramValues);
            //return paramValues.ToArray();
        }

        private void SetDeployProfilesComboBoxValues()
        {
            var profiles = GetDeployProfiles();
            if (profiles == null || profiles.Count() == 0)
            {
                DeployProfileComboBox.ItemsSource = profiles;
                return;
            }
            else if (DeployProfileComboBox.SelectedIndex == -1 || !profiles.Contains(DeployProfileComboBox.SelectedItem.ToString()))
            {
                DeployProfileComboBox.ItemsSource = profiles;
                DeployProfileComboBox.SelectedIndex = -1;
            }
            else if (profiles.Contains(DeployProfileComboBox.SelectedItem.ToString()))
            {
                var selected = DeployProfileComboBox.SelectedItem.ToString();   
                DeployProfileComboBox.ItemsSource = profiles;
                DeployProfileComboBox.SelectedIndex = Array.IndexOf(profiles, selected);
            }
            else
            {
                DeployProfileComboBox.ItemsSource = profiles;
                DeployProfileComboBox.SelectedIndex = -1;
            }
                
        }
        #endregion

        #region Fields
        protected bool projectInitialized = false;
        protected Dictionary<string, BlockchainInfo> deployProfiles = new Dictionary<string, BlockchainInfo>();

        #endregion

        private void DeployProfileComboBox_GotFocus(object sender, RoutedEventArgs e)
        {
            //SetDeployProfilesComboBoxValues();
        }
    }
}