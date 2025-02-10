using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hardcodet.Wpf.GenericTreeView;

using Stratis.VS.StratisEVM.ViewModel;

namespace Stratis.VS.StratisEVM
{
    public class BlockchainExplorerTree : TreeViewBase<BlockchainInfo>
    {
        public override string GetItemKey(BlockchainInfo item) => (item.Parent?.Name) ?? "Root" + "_" + item.Kind + "_" + item.Name;

        public override ICollection<BlockchainInfo> GetChildItems(BlockchainInfo parent) => parent.Children;

        public override BlockchainInfo GetParentItem(BlockchainInfo item) => item.Parent;
    }
}
