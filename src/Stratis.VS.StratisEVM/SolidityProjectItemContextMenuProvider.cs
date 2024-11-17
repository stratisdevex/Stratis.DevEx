using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Stratis.VS.StratisEVM
{
    [Export(typeof(IProjectItemContextMenuProvider))]
    [AppliesTo(SolidityUnconfiguredProject.UniqueCapability)]
    [Order(99)]
    internal class SolidityProjectItemContextMenuProvider : IProjectItemContextMenuProvider
    {
        public bool TryGetContextMenu(IProjectTree projectItem, out Guid menuCommandGuid, out int menuCommandId)
        {
            menuCommandGuid = StratisEVMPackageIds.Guid2;
            menuCommandId = 4128;
            if (projectItem.FilePath.EndsWith(".sol"))
            {
                return false;
            }
            else
            {
                return false;
            }
        }

        public bool TryGetMixedItemsContextMenu(IEnumerable<IProjectTree> projectItems, out Guid menuCommandGuid, out int menuCommandId)
        {
            menuCommandGuid = StratisEVMPackageIds.GuidStratisEVMPackageCmdSet;
            menuCommandId = StratisEVMPackageIds.Cmd1Id;
            return false; // we let others display a mixed item menu
        }
    }
}
