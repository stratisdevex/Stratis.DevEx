using System;
using System.Collections.Generic;
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
using Nethereum.Util;

using static Stratis.DevEx.Result;
using Stratis.DevEx.Ethereum;
using Stratis.VS.StratisEVM.UI.ViewModel;
using Stratis.DevEx;
using System.Windows.Documents;

namespace Stratis.VS.StratisEVM.UI
{
    /// <summary>
    /// Interaction logic for BlockchainExplorerToolWindowControl.
    /// </summary>
    public partial class BlockchainExplorerToolWindowControl : UserControl
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BlockchainExplorerToolWindowControl"/> class.
        /// </summary>
        public BlockchainExplorerToolWindowControl() : base()
        {
            instance = this;
            // Bug workaround, see https://github.com/microsoft/XamlBehaviorsWpf/issues/86
            var _ = new Card();
            var __ = new BlockchainExplorerTree();
            
            var _x = __.Style;
            var _y = __.Items;
            var _z = __.TreeStyle;
            var __xz = __.TreeNodeStyle;
            var ____ = new Wpf.Ui.ThemeService();
            /*
            System.Uri resourceLocater = new System.Uri("/Stratis.VS.StratisEVM;component/ui/blockchainexplorer/blockchainexplorertoolwind" +
        "owcontrol.xaml", System.UriKind.Relative);

#line 1 "..\..\..\..\UI\BlockchainExplorer\BlockchainExplorerToolWindowControl.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            */
            InitializeComponent();       
#if IS_VSIX
            VSTheme.WatchThemeChanges();
            instance = this;
#endif
        }
        #endregion

        #region Event handlers
        private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is BlockchainExplorerTree tree && tree.SelectedItem != null)
            {
                if (tree.SelectedItem.Kind == BlockchainInfoKind.Account)
                {
                    BlockchainExplorerTree.EditAccountCmd.Execute(null, tree);
                }
                else if (tree.SelectedItem.Kind == BlockchainInfoKind.Contract)
                {
                    BlockchainExplorerTree.EditContractCmd.Execute(null, tree);
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
                var dw = new ToolWindowDialog(RootContentDialog)
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
                            var result = ThreadHelper.JoinableTaskFactory.Run(() => ExecuteAsync(Network.GetNetworkDetailsAsync(text)));
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
                var endpoints = t.GetChild("Endpoints", BlockchainInfoKind.Folder);
                endpoints.AddChild(rpcurl.Text, BlockchainInfoKind.Endpoint);
                var accounts = t.GetChild("Accounts", BlockchainInfoKind.Folder);
                foreach (var acct in accts)
                {
                    accounts.AddAccount(acct);   
                };
                if (!tree.RootItem.Save("BlockchainExplorerTree", out var ex))
                {
#if IS_VSIX
                    VSUtil.ShowModalErrorDialogBox("Error saving tree data: " + ex?.Message);
#else
                    System.Windows.MessageBox.Show("Error saving tree data: " +  ex?.Message);
#endif
                }
                tree.Refresh(); 
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
                var dw = new ToolWindowDialog(RootContentDialog)
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
                endpoints.AddChild(rpcurl.Text, BlockchainInfoKind.Endpoint);
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
            var endpoints = item.Parent.Parent.GetNetworkEndPoints();
            if (endpoints.Count() == 1)
            {
                e.CanExecute = false;
            }
            e.CanExecute = (item.Parent.Name == "Stratis MainNet");
        }

        private void PropertiesCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var item = GetSelectedItem(sender);
            var dw = new ToolWindowDialog(RootContentDialog)
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
                var dw = new ToolWindowDialog(RootContentDialog)
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
                var f = item.AddChild(foldername.Text, BlockchainInfoKind.UserFolder);
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
                var dw = new ToolWindowDialog(RootContentDialog)
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
            try
            {
                var window = (BlockchainExplorerToolWindowControl)sender;
                var tree = window.BlockchainExplorerTree;
                var item = GetSelectedItem(sender);
                if (item.Kind == BlockchainInfoKind.Folder && item.Name == "Deploy Profiles")
                {
                    item = item.Parent;
                }
                var dw = new ToolWindowDialog(RootContentDialog)
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
                else 
                {
                    item.GetChild("Deploy Profiles", BlockchainInfoKind.Folder).AddDeployProfile(name.Text, (string)endpoint.SelectedValue, (string)accounts.SelectedValue, pkey.Text);
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

        private async void EditDeployProfileCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                var window = (BlockchainExplorerToolWindowControl)sender;
                var tree = window.BlockchainExplorerTree;
                var item = GetSelectedItem(sender);
                var dw = new ToolWindowDialog(RootContentDialog)
                {
                    Title = "Edit Deploy Profile",
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
                name.Text = item.Name;
                endpoint.ItemsSource = item.Parent.Parent.GetNetworkEndPoints();
                endpoint.SelectedValue = item.Data["Endpoint"];
                accounts.ItemsSource = item.Parent.Parent.GetNetworkAccounts();
                accounts.SelectedValue = item.Data["Account"];
                if (item.Data.ContainsKey("PrivateKey"))
                {
                    pkey.Text = item.GetDeployProfilePrivateKey();  
                }
                var validForClose = false;

                dw.ButtonClicked += (cd, args) =>
                {
                    validForClose = false;
                    errors.Visibility = Visibility.Hidden;

                    if (args.Button == ContentDialogButton.Primary)
                    {
                        if (!string.IsNullOrEmpty(name.Text) && accounts.SelectedValue != null && endpoint.SelectedValue != null)
                        {
                            var dp = item.Parent.Parent.GetNetworkDeployProfiles();
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
                               
                item.Name = name.Text;
                item.Data["Account"] = accounts.SelectedValue;
                item.Data["Endpoint"] = endpoint.SelectedValue;
                if (!string.IsNullOrEmpty(pkey.Text))
                {
                    item.Data["PrivateKey"] = item.SetDeployProfilePrivateKey(pkey.Text);
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

        private void DeleteDeployProfileCmd_Executed(object sender, ExecutedRoutedEventArgs e)
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
                System.Windows.MessageBox.Show("Error saving tree data: " + ex?.Message);
#endif
            }
            tree.Refresh();
        }

        private async void NewAccountCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                var window = (BlockchainExplorerToolWindowControl)sender;
                var tree = window.BlockchainExplorerTree;
                var item = GetSelectedItem(sender).GetChild("Accounts", BlockchainInfoKind.Folder);
                var dw = new ToolWindowDialog(RootContentDialog)
                {
                    Title = "Add Account",
                    PrimaryButtonIcon = new SymbolIcon(SymbolRegular.Save20),
                    Content = (StackPanel)TryFindResource("AddAccountDialog"),
                    PrimaryButtonText = "Save",
                    CloseButtonText = "Cancel",
                };
                var sp = (StackPanel)dw.Content;
                var acctpubkey = (Wpc.TextBox)((StackPanel)sp.Children[0]).Children[1];
                var acctlabel = (Wpc.TextBox)((StackPanel)sp.Children[0]).Children[3];
                var errors = (Wpc.TextBlock)((StackPanel)sp.Children[1]).Children[0];
                var validForClose = false;
                dw.ButtonClicked += (cd, args) =>
                {
                    if (!string.IsNullOrEmpty(acctpubkey.Text))
                    {
                        validForClose = true;
                    }
                    else
                    {
                        ShowValidationErrors(errors, "Enter a valid account public key.");
                    }
                };
                dw.Closing += (d, args) =>
                {
                    args.Cancel = !validForClose;
                };
                var r = await dw.ShowAsync();
                if (r != ContentDialogResult.Primary)
                {
                    return;
                }
                item.AddAccount(acctpubkey.Text, acctlabel.Text);   
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

        private void DeleteAccountCmd_Executed(object sender, ExecutedRoutedEventArgs e)
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
                System.Windows.MessageBox.Show("Error saving tree data: " + ex?.Message);
#endif
            }
            tree.Refresh();
        }

        private void CopyAccountAddressCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var item = GetSelectedItem(sender);
            var address = item.Name;
            Clipboard.SetText(address);
        }

        private async void EditContractCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                var window = (BlockchainExplorerToolWindowControl)sender;
                var tree = window.BlockchainExplorerTree;
                var item = GetSelectedItem(sender);
                var _sp = (StackPanel)TryFindResource("EditContractDialog");
                var dw = new ToolWindowDialog(RootContentDialog)
                {
                    Title = "Edit contract " + item.DisplayName,
                    Content = _sp,
                    PrimaryButtonText = "Save",
                    PrimaryButtonIcon = new SymbolIcon(SymbolRegular.Save16),
                    SecondaryButtonText = "Run",
                    SecondaryButtonIcon = new SymbolIcon(SymbolRegular.Run16),
                    CloseButtonText = "Cancel",
                };
                var sp = (StackPanel)(_sp).Children[0];
                var address = (Wpc.TextBox)(sp.Children[1]);
                var label = (Wpc.TextBox)(sp.Children[3]);
                var creator = (Wpc.TextBox)(sp.Children[5]);
                var transactionHash = (Wpc.TextBox)(sp.Children[7]);
                var deployedOn = (Wpc.TextBox)(sp.Children[9]);
                var abi = (Wpc.TextBox)(sp.Children[11]);
                //var sp1 = (StackPanel)((StackPanel)dw.Content).Children[1];
                //var errors = (Wpc.TextBlock) sp1.Children[0];
                address.Text = item.Name;
                label.Text = item.Data.ContainsKey("Label") ? (string)item.Data["Label"] : "";
                creator.Text = (string)item.Data["Creator"];
                transactionHash.Text = (string)item.Data["TransactionHash"];
                deployedOn.Text = (string)item.Data["DeployedOn"];
                abi.Text = (string)item.Data["Abi"];               
                var r = await dw.ShowAsync();
                if (r == ContentDialogResult.None)
                {
                    return;
                }
                else if (r == ContentDialogResult.Secondary)
                {
                    BlockchainExplorerTree.RunContractCmd.Execute(null, tree);
                    return;
                }
                //item.Data["Label"] = label.Text;
                //item.Data["Abi"] = abi.Text;
                //tree.Save();                
            }
            catch (Exception ex)
            {
#if IS_VSIX
                VSUtil.ShowModalErrorDialogBox(ex?.Message, "Edit Contract error");
#else
                System.Windows.MessageBox.Show(ex?.Message);
#endif
            }
        }

        private void DeleteContractCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var window = (BlockchainExplorerToolWindowControl)sender;
            var tree = window.BlockchainExplorerTree;
            var item = GetSelectedItem(sender);
            item.Parent.DeleteChild(item);
            SaveBlockchainTree(tree);
        }

        private async void RunContractCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                var window = (BlockchainExplorerToolWindowControl)sender;
                var tree = window.BlockchainExplorerTree;
                var item = GetSelectedItem(sender);
                var _sp = (StackPanel)TryFindResource("RunContractDialog");
                var dw = new ToolWindowDialog(RootContentDialog)
                {
                    Title = "Run contract " + item.DisplayName,
                    Content = _sp,
                    CloseButtonText = "Close",
                };
           
                var transactCheckBox = (CheckBox)((_sp).Children[0]);
                var transactPanel = (StackPanel)((_sp).Children[1]);    
                var fromAddressTextBox = (Wpc.TextBox)((StackPanel)(transactPanel).Children[0]).Children[1];
                fromAddressTextBox.Text = (string)item.Data["Address"];
                transactCheckBox.Checked += (s, ev) =>
                {
                    transactPanel.IsEnabled = true;
                };
                transactCheckBox.Unchecked += (s, ev) =>
                {
                    transactPanel.IsEnabled = false;
                };
                var formPanel = (StackPanel)(_sp).Children[2];
                var statusPanel = ((StackPanel)(_sp).Children[3]);
                await CreateRunContractFormAsync(formPanel, statusPanel, item.Data, transactCheckBox, fromAddressTextBox.Text);
                dw.ButtonClicked += (cd, args) => { };
                dw.Closing += (d, args) => { };
                await dw.ShowAsync();                             
            }
            catch (Exception ex)
            {
#if IS_VSIX
                VSUtil.ShowModalErrorDialogBox(ex?.Message, "Run Contract Error");
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

        private void SaveBlockchainTree(BlockchainExplorerTree tree)
        {
            if (!tree.RootItem.Save("BlockchainExplorerTree", out var ex))
            {
#if IS_VSIX
                VSUtil.ShowModalErrorDialogBox("Error saving tree data: " + ex?.Message);
#else
                System.Windows.MessageBox.Show("Error saving tree data: " +  ex?.Message);
#endif
            }
        }

        private void ShowValidationErrors(Wpc.TextBlock textBlock, string message)
        {
            textBlock.Visibility = Visibility.Visible;
            textBlock.Text = message;
        }

        private void HideValidationErrors(Wpc.TextBlock textBlock) => textBlock.Visibility = Visibility.Hidden; 

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

        private void ShowValidationSuccess(StackPanel successPanel, Wpc.TextBlock successTextBlock, string message)
        {
            successPanel.Visibility = Visibility.Visible;
            successTextBlock.Text = message;
        }   

        private void HideValidationSuccess(StackPanel successPanel) => successPanel.Visibility = Visibility.Hidden;

        private async Task CreateRunContractFormAsync(StackPanel form, StackPanel statusPanel, Dictionary<string, object> contractData, CheckBox transactCheckBox, string fromAddress)
        {
            form.Children.Clear();  
            var errors = (Wpc.TextBlock)((Grid)statusPanel.Children[0]).Children[0];
            var progressring = (ProgressRing)((Grid)statusPanel.Children[0]).Children[1];
            var successPanel = ((StackPanel)((Grid)statusPanel.Children[0]).Children[2]);
            var successTextBlock = (Wpc.TextBlock) successPanel.Children[1];
            var address = (string)contractData["Address"];
            var rpcurl = (string)contractData["Endpoint"];
            var abi = (string)contractData["Abi"];
            var _abi = Contract.DeserializeABI(abi);
            
            ShowProgressRing(progressring);
            var balr = await ThreadHelper.JoinableTaskFactory.RunAsync(() => ExecuteAsync(Network.GetBalance(rpcurl, address)));
            HideProgressRing(progressring);
            if (balr.IsSuccess)
            {
                HideValidationErrors(errors);
                var hsp = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                };
                hsp.Children.Add(new Label()
                {
                    Content = "Contract Balance: ",
                    VerticalAlignment = VerticalAlignment.Center,
                });
                hsp.Children.Add(new Wpc.TextBlock()
                {
                    Text = $"{UnitConversion.Convert.FromWei(balr.Value, UnitConversion.EthUnit.Ether)} ETH",
                    FontWeight= FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center,
                });
                form.Children.Add(hsp); 
            }
            else
            {
                ShowValidationErrors(errors, $"Could not retrieve balance for contract. {balr.FailureMessage}");
                return;
            }

           
            
            foreach (var function in _abi.Functions)
            {
                var vsp = new StackPanel()
                {
                    Orientation = Orientation.Vertical,
                };
                vsp.Children.Add(new Separator()
                {
                    Margin = new Thickness(0, 8, 0, 8),
                    Height = 1
                });

                var button = new Wpc.Button()
                {
                    Name = function.Name + "_Button",
                    Content = function.Name,
                    Width=125.0,
                    Foreground = System.Windows.Media.Brushes.White,
                    Background = System.Windows.Media.Brushes.DodgerBlue,
                    Icon = new SymbolIcon(SymbolRegular.Play12),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    FontSize =11.0,
                    Margin = new Thickness(0,4,0,0)
                };

                if (function.InputParameters != null && function.InputParameters.Count() > 0)
                {
                    foreach (var p in function.InputParameters)
                    {
                        var sp = new StackPanel()
                        {
                            Orientation = Orientation.Horizontal,
                            VerticalAlignment = VerticalAlignment.Top,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            Margin = new Thickness(4, 4, 0, 2)
                        };
                        var lbl = new Wpc.TextBlock { Width = 100, VerticalAlignment = VerticalAlignment.Bottom};
                        lbl.Inlines.Add(new Run() { Text = p.Name });
                        lbl.Inlines.Add(new Run() { Text = $" ({p.Type}): ", FontStyle = FontStyles.Italic, FontSize=9.0});
                        var tb = new Wpc.TextBox() { Name = $"Param{p.Name}TextBox", Width = 150, VerticalAlignment = VerticalAlignment.Bottom};
                        sp.Children.Add(lbl);
                        sp.Children.Add(tb);
                        vsp.Children.Add(sp);
                    }

                    button.Click += (s, e) =>
                    {
                        var paramVals = GetContractFunctionParams(vsp, function.InputParameters.ToDictionary(ip => ip.Name, ip => ip.Type));
                        if (paramVals.Length != function.InputParameters.Count())
                        {
                            ShowValidationErrors(errors, $"The {function.Name} function requires {function.InputParameters.Count()} parameters.");
                            VSUtil.LogToStratisEVMWindow($"\n========== Call contract {address} at {rpcurl} failed.==========\nThe {function.Name} function requires {function.InputParameters.Count()} parameters.");
                            return;
                        }   

                        ShowProgressRing(progressring);                        
                        Result<string> r;
                      
                        if (transactCheckBox.IsChecked ?? false)
                        {
                            
                            if (string.IsNullOrEmpty(fromAddress))
                            {
                                HideProgressRing(progressring);
                                ShowValidationErrors(errors, "Enter a valid from address to send the transaction from.");
                                return;
                            }
                            r = ThreadHelper.JoinableTaskFactory.Run(() => ExecuteAsync(Network.SendContractTransactionAsync(rpcurl, address, abi, function.Name, fromAddress, functionInput: paramVals)));
                        }
                        else
                        {
                            r = ThreadHelper.JoinableTaskFactory.Run(() => ExecuteAsync(Network.CallContractAsync(rpcurl, address, abi, function.Name, functionInput: paramVals)));
                        }                        
                        HideProgressRing(progressring);
                        string desc = (transactCheckBox.IsChecked == null || transactCheckBox.IsChecked == false) ? "call" : "transaction";
                        if (r.IsSuccess)
                        {
                            HideValidationErrors(errors);
                            ShowValidationSuccess(successPanel, successTextBlock, $"Function {function.Name}({paramVals.Select(v => v.ToString()).JoinWith(",")}) returned: {r.Value}");
                            VSUtil.LogToStratisEVMWindow($"\n========== Call contract {address} at {rpcurl} succeeded.==========\n[{desc}] {address +":" + function.Name}({function.InputParameters.Select((p,i) =>p.Type + " " + p.Name + ":" +paramVals.ElementAt(i)).JoinWith(", ")}): {r.Value}");
                        }
                        else
                        {
                            HideValidationSuccess(successPanel);
                            ShowValidationErrors(errors, $"Error calling function {function.Name}({paramVals.Select(v => v.ToString()).JoinWith(",")}): {r.FailureMessage}");
                            VSUtil.LogToStratisEVMWindow($"\n========== Call contract {address} at {rpcurl} failed.==========\n[{desc}] {address + ":" + function.Name}({function.InputParameters.Select((p, i) => p.Type + " " + p.Name + ":" + paramVals.ElementAt(i)).JoinWith(", ")}): {r.FailureMessage}");
                        }
                    };
                }                
                else
                {
                    button.Click += (s, e) =>
                    {                                                
                        ShowProgressRing(progressring);
                        Result<string> r;
                        if (transactCheckBox.IsChecked ?? false)
                        {
                            if (string.IsNullOrEmpty(fromAddress))
                            {
                                HideProgressRing(progressring);
                                ShowValidationErrors(errors, "Enter a valid from address to send the transaction from.");
                                return;
                            }
                            r = ThreadHelper.JoinableTaskFactory.Run(() => ExecuteAsync(Network.SendContractTransactionAsync(rpcurl, address, abi, function.Name, fromAddress)));
                        }
                        else
                        {
                            r = ThreadHelper.JoinableTaskFactory.Run(() => ExecuteAsync(Network.CallContractAsync(rpcurl, address, abi, function.Name)));
                        }                        
                        HideProgressRing(progressring);
                        string desc = (transactCheckBox.IsChecked == null || transactCheckBox.IsChecked == false) ? "call" : "transaction";
                        if (r.IsSuccess)
                        {
                            HideValidationErrors(errors);                            
                            ShowValidationSuccess(successPanel, successTextBlock, $"Function {function.Name} returned: {r.Value}");
                            VSUtil.LogToStratisEVMWindow($"\n========== Call contract {address} at {rpcurl} succeeded.==========\n[{desc}] {address + ":" + function.Name}: {r.Value}");
                        }
                        else
                        {
                            HideValidationSuccess(successPanel);
                            ShowValidationErrors(errors, $"Error calling function: {r.FailureMessage}");
                            VSUtil.LogToStratisEVMWindow($"\n========== Call contract {address} at {rpcurl} failed.==========\n[{desc}] {address + ":" + function.Name}: {r.FailureMessage}");
                        }
                        
                    };
                }
                vsp.Children.Add(button);
                form.Children.Add(vsp);               
            }
        }

        private object[] GetContractFunctionParams(StackPanel form, Dictionary<string, string> paramTypes)
        {
            Dictionary<string, (string, string)> paramValues = new Dictionary<string, (string, string)>();
            foreach (var child in form.Children)
            {
                if (child is StackPanel sp && sp.Children.Count == 2 && sp.Children[0] is Wpc.TextBlock lbl && sp.Children[1] is Wpc.TextBox tb)
                {
                    var paramName = (Run)lbl.Inlines.FirstInline;
                    if (paramTypes.ContainsKey(paramName.Text) && !string.IsNullOrEmpty(tb.Text))
                    {
                        paramValues[paramName.Text] = (paramTypes[paramName.Text], tb.Text);
                    }
                }
            }
            return Contract.ParseFunctionParameterValues(paramValues);
        }
        #endregion

        #region Properties
        public static bool ControlIsLoaded => instance != null;
        #endregion

        #region Fields
        internal BlockchainExplorerToolWindow window;
        internal static BlockchainExplorerToolWindowControl instance;
        #endregion

        
    }
}