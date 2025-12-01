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
    public class SolidityStaticAnalysisTree : TreeViewBase<SolidityStaticAnalysisInfo>
    {
        #region Properties
        

        public SolidityStaticAnalysisInfo RootItem => Items?.First();
        #endregion

        #region Overriden Members
        public override string GetItemKey(SolidityStaticAnalysisInfo item) => item.Key;

        public override ICollection<SolidityStaticAnalysisInfo> GetChildItems(SolidityStaticAnalysisInfo parent) => parent.Children;

        public override SolidityStaticAnalysisInfo GetParentItem(SolidityStaticAnalysisInfo item) => item.Parent;

        protected override TreeViewItem CreateTreeViewItem(SolidityStaticAnalysisInfo data)
        {
            var item = base.CreateTreeViewItem(data);
            
            return item;
        }
        #endregion

        #region Methods
       
        #endregion
    }
}
