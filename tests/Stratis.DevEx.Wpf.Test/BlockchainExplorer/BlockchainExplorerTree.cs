using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Media.Imaging;

using Hardcodet.Wpf.GenericTreeView;

using Stratis.DevEx;
using Stratis.VS.StratisEVM.UI.ViewModel;

namespace Stratis.VS.StratisEVM.UI
{
    public class BlockchainExplorerTree : TreeViewBase<BlockchainInfo>
    {
        #region Properties
        public static RoutedCommand NewNetworkCmd { get; } = new RoutedCommand();

        public static RoutedCommand DeleteNetworkCmd { get; } = new RoutedCommand();

        public static RoutedCommand PropertiesCmd { get; } = new RoutedCommand();

        public static RoutedCommand NewEndpointCmd { get; } = new RoutedCommand();

        public static RoutedCommand NewFolderCmd { get; } = new RoutedCommand();

        public static RoutedCommand DeleteFolderCmd { get; } = new RoutedCommand();

        public static RoutedCommand DeleteEndpointCmd { get; } = new RoutedCommand();

        public static RoutedCommand EditAccountCmd { get; } = new RoutedCommand();

        public BlockchainInfo RootItem => Items?.First();
        #endregion

        #region Overriden Members
        public override string GetItemKey(BlockchainInfo item) => item.Key;

        public override ICollection<BlockchainInfo> GetChildItems(BlockchainInfo parent) => parent.Children;

        public override BlockchainInfo GetParentItem(BlockchainInfo item) => item.Parent;

        protected override TreeViewItem CreateTreeViewItem(BlockchainInfo data)
        {
            var item = base.CreateTreeViewItem(data);
            if (data.Kind == BlockchainInfoKind.Folder && data.Name == "EVM Networks")
            {
                item.ContextMenu = (ContextMenu)TryFindResource("RootContextMenu");
            }
            else if (data.Kind == BlockchainInfoKind.Network)
            {
                item.ContextMenu = (ContextMenu)TryFindResource("NetworkContextMenu");
            }
            else if (data.Kind == BlockchainInfoKind.Endpoint)
            {
                item.ContextMenu = (ContextMenu)TryFindResource("EndpointContextMenu");
            }
            else if (data.Kind == BlockchainInfoKind.UserFolder)
            {
                item.ContextMenu = (ContextMenu)TryFindResource("UserFolderContextMenu");
            }
            else if (data.Kind == BlockchainInfoKind.Folder && data.Name == "Endpoints")
            {
                item.ContextMenu = (ContextMenu)TryFindResource("EndpointsFolderContextMenu"); ;
            }
            return item;
        }
        #endregion
    }
}
