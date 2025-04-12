using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Controls;
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

        public static RoutedCommand NewEndpointCmd { get; } = new RoutedCommand();

        #endregion

        #region Methods
        public override string GetItemKey(BlockchainInfo item) => ((item.Parent?.Name) ?? "Root") + "_" + item.Kind + "_" + item.Name;

        public override ICollection<BlockchainInfo> GetChildItems(BlockchainInfo parent) => parent.Children;

        public override BlockchainInfo GetParentItem(BlockchainInfo item) => item.Parent;

        protected override TreeViewItem CreateTreeViewItem(BlockchainInfo data)
        {
            var item = base.CreateTreeViewItem(data);
            if (data.Kind == BlockchainInfoKind.Network)
            {
                item.ContextMenu = (ContextMenu)TryFindResource("NetworkContextMenu");
            }
            return item;
        }
        #endregion

    }    
}
