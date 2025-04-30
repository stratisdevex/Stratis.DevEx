using System;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

#if IS_VSIX
using Microsoft.VisualStudio.Shell;
#endif

using Hardcodet.Wpf.GenericTreeView;
using Wpf.Ui.Controls;
using Wpc = Wpf.Ui.Controls;

using Stratis.VS.StratisEVM.UI.ViewModel;
using Stratis.DevEx.Ethereum;
using static Stratis.DevEx.Result;
using System.IO;
using Stratis.DevEx;
using System.Diagnostics.Eventing.Reader;
using System.Text;

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
        public BlockchainExplorerToolWindowControl() : base()
        {
            var _ = new Wpf.Ui.Controls.Card(); // Bug workaround, see https://github.com/microsoft/XamlBehaviorsWpf/issues/86
            InitializeComponent();
            this.BlockchainExplorerTree.MouseDoubleClick += BlockchainExplorerTree_MouseDoubleClick;
#if IS_VSIX
            VSTheme.WatchThemeChanges();
#endif
        }

        #region Event handlers
        private void BlockchainExplorerTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is BlockchainExplorerTree tree && tree.SelectedItem != null)
            {
                if (tree.SelectedItem.Kind == BlockchainInfoKind.Account)
                {
                    BlockchainExplorerTree.EditAccountCmd.Execute(tree, null);
                }
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

        private async void NewNetworkCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                var window = (BlockchainExplorerToolWindowControl)sender;
                var tree = window.BlockchainExplorerTree;
                var item = GetSelectedItem(sender);
                var dw = new BlockchainExplorerDialog(RootContentDialog)
                {
                    Title = "Add EVM Network",
                    PrimaryButtonIcon = new SymbolIcon(SymbolRegular.Save20),
                    Content = (StackPanel)TryFindResource("AddNetworkDialog"),
                    PrimaryButtonText = "Save",
                    CloseButtonText = "Cancel"
                };
                var sp = (StackPanel)dw.Content;
                var name = (Wpc.TextBox)((StackPanel)sp.Children[0]).Children[1];
                var rpcurl = (Wpc.TextBox)((StackPanel)sp.Children[1]).Children[1];
                var chainid = (Wpc.NumberBox)((StackPanel)sp.Children[2]).Children[1];
                var nid = "";
                string[] accts = Array.Empty<string>();
                var errors = (Wpc.TextBlock)((Grid)((StackPanel)sp.Children[3]).Children[0]).Children[0];
                var progressring = (Wpc.ProgressRing)((Grid)((StackPanel)sp.Children[3]).Children[0]).Children[1];
                var validForClose = false;
                dw.Closing += (d, args) =>
                {
                    args.Cancel = !validForClose;
                };
                dw.ButtonClicked += (cd, args) =>
                {
                    validForClose = false;
                    errors.Visibility = Visibility.Hidden;
                    if (args.Button == ContentDialogButton.Primary)
                    {
                        if (!string.IsNullOrEmpty(name.Text) && !string.IsNullOrEmpty(rpcurl.Text) && Uri.TryCreate(rpcurl.Text, UriKind.Absolute, out var _))
                        {
                            if (tree.RootItem.HasChild(name.Text, BlockchainInfoKind.Network))
                            {
                                ShowValidationErrors(errors, "Enter a unique name for the network name.");
                                return;
                            }
                            ShowProgressRing(progressring);
                            var text = rpcurl.Text;
#if IS_VSIX
                            var result = ThreadHelper.JoinableTaskFactory.Run(() => ExecuteAsync(Network.GetChainandNetworkIdAsync(text)));
#else
                            var result = Task.Run(() => ExecuteAsync(Network.GetNetworkDetailsAsync(text))).Result;
#endif
                            HideProgressRing(progressring);
                            if (Succedeed(result, out var cnid))
                            {
                                if (!string.IsNullOrEmpty(chainid.Text) && cnid.Value.Item1 == BigInteger.Parse(chainid.Text))
                                {
                                    nid = cnid.Value.Item2;
                                    accts = cnid.Value.Item3;
                                    validForClose = true;
                                }
                                else if (string.IsNullOrEmpty(chainid.Text))
                                {
                                    chainid.Text = cnid.Value.Item1.ToString();
                                    nid = cnid.Value.Item2;
                                    accts = cnid.Value.Item3;
                                    validForClose = true;
                                }
                                else
                                {
                                    ShowValidationErrors(errors, string.Format("The specified chain id {0} does not match the chain id returned by the network endpoint: {1}.", chainid.Text, cnid.Value.Item1));
                                    return;
                                }
                            }
                            else
                            {
                                ShowValidationErrors(errors, "Error connecting to JSON-RPC URL: " + cnid.Exception.Message + " " + cnid.Exception.InnerException?.Message);
                                return;
                            }
                        }
                        else
                        {
                            ShowValidationErrors(errors, "Enter a network name and a valid JSON-RPC endpoint URL.");
                            return;
                        }
                    }
                    else
                    {
                        validForClose = true;
                    }
                };

                var r = await dw.ShowAsync();
                if (r != ContentDialogResult.Primary)
                {
                    name.Text = "";
                    rpcurl.Text = "";
                    chainid.Text = "";
                    nid = "";
                    accts = Array.Empty<string>();  
                    return;
                }
                var t = item.AddNetwork(name.Text, rpcurl.Text, BigInteger.Parse(chainid.Text), nid);
                var endpoints = t.AddChild(BlockchainInfoKind.Folder, "Endpoints");
                endpoints.AddChild(BlockchainInfoKind.Endpoint, rpcurl.Text);
                var accounts = t.AddChild(BlockchainInfoKind.Folder, "Accounts");
                foreach (var acct in accts)
                {
                    accounts.AddAccount(acct);   
                }
                t.AddChild(BlockchainInfoKind.Folder, "Deploy Profiles");
                if (!tree.RootItem.Save("BlockchainExplorerTree", out var ex))
                {
#if IS_VSIX
                    VSUtil.ShowModalErrorDialogBox("Error saving tree data: " + ex?.Message);
#else
                    System.Windows.MessageBox.Show("Error saving tree data: " +  ex?.Message);
#endif
                }
            }
            catch (Exception ex)
            {
#if IS_VSIX
                VSUtil.ShowModalErrorDialogBox(ex?.Message);
#else
                System.Windows.MessageBox.Show(ex?.Message);
#endif
            }
        }

        private async void NewEndpointCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                var window = (BlockchainExplorerToolWindowControl)sender;
                var tree = window.BlockchainExplorerTree;
                var dw = new BlockchainExplorerDialog(RootContentDialog)
                {
                    Title = "Add EVM network endpoint",
                    PrimaryButtonIcon = new SymbolIcon(SymbolRegular.Save20),
                    Content = (StackPanel)TryFindResource("AddEndpointDialog"),
                    PrimaryButtonText = "Save",
                    CloseButtonText = "Cancel",
                };
                var sp = (StackPanel)dw.Content;
                var rpcurl = (Wpc.TextBox)((StackPanel)sp.Children[0]).Children[1];
                var errors = (Wpc.TextBlock)((StackPanel)sp.Children[1]).Children[0];
                var validForClose = false;
                dw.ButtonClicked += (cd, args) =>
                {
                    validForClose = false;
                    errors.Visibility = Visibility.Hidden;
                    if (args.Button == ContentDialogButton.Primary)
                    {
                        if (!string.IsNullOrEmpty(rpcurl.Text) && Uri.TryCreate(rpcurl.Text, UriKind.Absolute, out var _))
                        {
                            if (tree.SelectedItem.HasChild(rpcurl.Text, BlockchainInfoKind.Endpoint))
                            {
                                ShowValidationErrors(errors, "Enter a unique network endpoint URL.");
                                return;
                            }
                            else
                            {
                                validForClose = true;
                            }
                        }
                        else
                        {
                            validForClose = false;
                            ShowValidationErrors(errors, "Enter a valid URL for the network JSON-RPC endpoint.");
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
                    rpcurl.Text = "";
                    return;
                }

                var uri = new Uri(rpcurl.Text);

                var endpoints = tree.SelectedItem.Kind == BlockchainInfoKind.Network ? tree.SelectedItem.GetChild("Endpoints", BlockchainInfoKind.Folder) : tree.SelectedItem;
                endpoints.AddChild(BlockchainInfoKind.Endpoint, uri.ToString());
                if (!tree.RootItem.Save("BlockchainExplorerTree", out var ex))
                {
                    System.Windows.MessageBox.Show("Error saving tree data: " + ex?.Message);
                }
                
            }
            catch (Exception ex)
            {
#if IS_VSIX
                VSUtil.ShowModalErrorDialogBox(ex?.Message);
#else
                System.Windows.MessageBox.Show(ex?.Message);
#endif
            }
        }

        private void DeleteEndpointCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var item = GetSelectedItem(sender);
            item.Parent.DeleteChild(item);
        }

        private void DeleteEndpointCmd_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var item = GetSelectedItem(sender);
            var endpoints = item.GetNetworkEndPoints();
            if (endpoints.Count() == 1)
            {
                e.CanExecute = false;
            }
            e.CanExecute = (item.Parent.Name == "Stratis MainNet");
        }

        private void PropertiesCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var item = GetSelectedItem(sender);
            var dw = new BlockchainExplorerDialog(RootContentDialog)
            {
                Title = item.Name + " properties",
                PrimaryButtonIcon = new SymbolIcon(SymbolRegular.Save20),
                Content = (StackPanel)TryFindResource("AddEndpointDialog"),
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
            };
        }

        private async void NewFolderCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                var item = GetSelectedItem(sender);
                var dw = new BlockchainExplorerDialog(RootContentDialog)
                {
                    Title = "Add Folder",
                    PrimaryButtonIcon = new SymbolIcon(SymbolRegular.Save20),
                    Content = (StackPanel)TryFindResource("AddFolderDialog"),
                    PrimaryButtonText = "Save",
                    CloseButtonText = "Cancel",
                };
                var sp = (StackPanel)dw.Content;
                var foldername = (Wpc.TextBox)((StackPanel)sp.Children[0]).Children[1];
                var errors = (Wpc.TextBlock)((StackPanel)sp.Children[1]).Children[0];
                var validForClose = false;
                dw.ButtonClicked += (cd, args) =>
                {
                    validForClose = false;
                    errors.Visibility = Visibility.Hidden;
                    if (args.Button == ContentDialogButton.Primary)
                    {
                        if (!string.IsNullOrEmpty(foldername.Text))
                        {
                            if (item.HasChild(foldername.Text, BlockchainInfoKind.Endpoint))
                            {
                                ShowValidationErrors(errors, "Enter a unique folder name.");
                                return;
                            }
                            else
                            {
                                validForClose = true;
                            }
                        }
                        else
                        {
                            validForClose = false;
                            ShowValidationErrors(errors, "Enter a valid folder name.");
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
                    foldername.Text = "";
                    return;
                }
                var f = item.AddChild(BlockchainInfoKind.UserFolder, foldername.Text);
                if (!item.Save("BlockchainExplorerTree", out var ex))
                {
#if IS_VSIX
                    VSUtil.ShowModalErrorDialogBox("Error saving tree data: " + ex?.Message);
#else
                    System.Windows.MessageBox.Show("Error saving tree data: " +  ex?.Message);
#endif
                }
            }
            catch (Exception ex)
            {
#if IS_VSIX
                VSUtil.ShowModalErrorDialogBox(ex?.Message);
#else
                System.Windows.MessageBox.Show(ex?.Message);
#endif
            }
        }

        private void DeleteFolderCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var window = (BlockchainExplorerToolWindowControl)sender;
            var tree = window.BlockchainExplorerTree;
            var item = GetSelectedItem(sender);
            item.Parent.DeleteChild(item);
            if (!tree.RootItem.Save("BlockchainExplorerTree", out var ex))
            {
#if IS_VSIX
                VSUtil.ShowModalErrorDialogBox("Error saving tree data: " + ex?.Message);
#else
                System.Windows.MessageBox.Show("Error saving tree data: " +  ex?.Message);
#endif
            }
        }

        private void DeleteNetworkCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var window = (BlockchainExplorerToolWindowControl)sender;
            var tree = window.BlockchainExplorerTree;
            var item = GetSelectedItem(sender);
            item.Parent.DeleteChild(item);
            if (!tree.RootItem.Save("BlockchainExplorerTree", out var ex))
            {
#if IS_VSIX
                VSUtil.ShowModalErrorDialogBox("Error saving tree data: " + ex?.Message);
#else
                System.Windows.MessageBox.Show("Error saving tree data: " +  ex?.Message);
#endif
            }
        }

        private void DeleteNetworkCmd_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var item = GetSelectedItem(sender);
            if (item.Name == "Stratis Mainnet")
            {
                e.CanExecute = false;
            }
            else
            {
                e.CanExecute = true;
            }
        }


        private async void EditAccountCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                var window = (BlockchainExplorerToolWindowControl)sender;
                var tree = window.BlockchainExplorerTree;
                var item = GetSelectedItem(sender);
                var dw = new BlockchainExplorerDialog(RootContentDialog)
                {
                    Title = "Edit Account",
                    PrimaryButtonIcon = new SymbolIcon(SymbolRegular.Save20),
                    Content = (StackPanel)TryFindResource("EditAccountDialog"),
                    PrimaryButtonText = "Save",
                    CloseButtonText = "Cancel",
                };
                var sp = (StackPanel)dw.Content;
                var acctpubkey = (Wpc.TextBlock)((StackPanel)sp.Children[0]).Children[1];
                acctpubkey.Text = item.Name;
                var acctlabel = (Wpc.TextBox)((StackPanel)sp.Children[0]).Children[3];
                acctlabel.Text = (string)item.Data["Label"];
                var validForClose = false;
                dw.ButtonClicked += (cd, args) =>
                {
                    validForClose = true;
                };
                dw.Closing += (d, args) =>
                {
                    args.Cancel = !validForClose;
                };
                var r = await dw.ShowAsync();
                if (r != ContentDialogResult.Primary)
                {
                    acctlabel.Text = (string)item.Data["Label"];
                    return;
                }
                item.Data["Label"] = acctlabel.Text;
                if (!tree.RootItem.Save("BlockchainExplorerTree", out var ex))
                {
#if IS_VSIX
                    VSUtil.ShowModalErrorDialogBox("Error saving tree data: " + ex?.Message);
#else
                    System.Windows.MessageBox.Show("Error saving tree data: " + ex?.Message);
#endif
                }
                else
                {
                    tree.Refresh();
                }
            }
            catch (Exception ex)
            {
#if IS_VSIX
                VSUtil.ShowModalErrorDialogBox(ex?.Message);
#else
                System.Windows.MessageBox.Show(ex?.Message);
#endif
            }
        }

        private async void NewDeployProfileCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            int roundUp(int numToRound, int multiple)
            {
                if (multiple == 0)
                    return numToRound;

                int remainder = numToRound % multiple;
                if (remainder == 0)
                    return numToRound;

                return numToRound + multiple - remainder;
            }

            try
            {
                var window = (BlockchainExplorerToolWindowControl)sender;
                var tree = window.BlockchainExplorerTree;
                var item = GetSelectedItem(sender);
                var dw = new BlockchainExplorerDialog(RootContentDialog)
                {
                    Title = "Add Deploy Profile",
                    PrimaryButtonIcon = new SymbolIcon(SymbolRegular.Save20),
                    Content = (StackPanel)TryFindResource("AddDeployProfileDialog"),
                    PrimaryButtonText = "Save",
                    CloseButtonText = "Cancel",
                };
                var sp = (StackPanel)dw.Content;
                var name = (Wpc.TextBox)((StackPanel)sp.Children[0]).Children[1];
                var endpoint = (ComboBox)((StackPanel)sp.Children[1]).Children[1];
                var accounts = (ComboBox)((StackPanel)sp.Children[2]).Children[1];
                var pkey = (Wpc.TextBox)((StackPanel)sp.Children[3]).Children[1];
                var errors = (Wpc.TextBlock)((StackPanel)sp.Children[4]).Children[0];
                endpoint.ItemsSource = item.GetNetworkEndPoints();
                endpoint.SelectedIndex = 0;
                accounts.ItemsSource = item.GetNetworkAccounts();
                var validForClose = false;

                dw.ButtonClicked += (cd, args) =>
                {
                    validForClose = false;
                    errors.Visibility = Visibility.Hidden;

                    if (args.Button == ContentDialogButton.Primary)
                    {
                        if (!string.IsNullOrEmpty(name.Text) && accounts.SelectedValue != null && endpoint.SelectedValue != null)
                        {
                            var dp = item.GetNetworkDeployProfiles();
                            if (dp.Contains(name.Text))
                            {
                                ShowValidationErrors(errors, "The " + name.Text + " deploy profile already exists.");
                            }
                            else
                            {
                                validForClose = true;
                            }
                        }
                        else
                        {
                            ShowValidationErrors(errors, "Enter a deploy profile name and select a valid endpoint and account");
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
                    name.Text = "";
                    endpoint.ItemsSource = null;
                    accounts.ItemsSource = null;
                    return;
                }
                else if (!string.IsNullOrEmpty(pkey.Text))
                {
                    byte[] pkeydata = UnicodeEncoding.ASCII.GetBytes(pkey.Text);
                    var len = pkeydata.Length; 
                    var lb = BitConverter.GetBytes(len);
                    var round = roundUp(pkeydata.Length, 16);
                    Array.Resize(ref pkeydata, round);  
                    ProtectedMemory.Protect(pkeydata, MemoryProtectionScope.SameLogon);
                    var encodeddata = lb.Concat(pkeydata).ToArray();  
                    item.AddDeployProfile(name.Text, (string)endpoint.SelectedValue, (string)accounts.SelectedValue, encodeddata);
                }
                else
                {
                    item.AddDeployProfile(name.Text, (string)endpoint.SelectedValue, (string)accounts.SelectedValue);
                }
                if (!tree.RootItem.Save("BlockchainExplorerTree", out var ex))
                {
#if IS_VSIX
                VSUtil.ShowModalErrorDialogBox("Error saving tree data: " + ex?.Message);
#else
                    System.Windows.MessageBox.Show("Error saving tree data: " + ex?.Message);
#endif
                }
                else
                {
                    tree.Refresh();
                }
            }
            catch (Exception ex)
            {
#if IS_VSIX
                VSUtil.ShowModalErrorDialogBox(ex?.Message);
#else
                System.Windows.MessageBox.Show(ex?.Message);
#endif
            }
        }
        #endregion

        #region Methods
        private BlockchainInfo GetSelectedItem(object sender)
        {
            var window = (BlockchainExplorerToolWindowControl)sender;
            var tree = window.BlockchainExplorerTree;
            return tree.SelectedItem;
        }
        private void ShowValidationErrors(Wpc.TextBlock textBlock, string message)
        {
            textBlock.Visibility = Visibility.Visible;
            textBlock.Text = message;
        }

        private void ShowProgressRing(ProgressRing progressRing)
        {
            progressRing.IsEnabled = true;
            progressRing.Visibility = Visibility.Visible;
        }

        private void HideProgressRing(ProgressRing progressRing)
        {
            progressRing.IsEnabled = false;
            progressRing.Visibility = Visibility.Hidden;
        }

        #endregion

        #region Fields
        internal BlockchainExplorerToolWindow window;

        #endregion

    }
}