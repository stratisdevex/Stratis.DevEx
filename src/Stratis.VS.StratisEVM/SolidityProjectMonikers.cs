using Microsoft.VisualStudio.Imaging.Interop;
using System;

namespace Stratis.VS.StratisEVM
{
    public static class SolidityProjectMonikers
    {
        private static readonly Guid ProjectManifestGuid = new Guid("293347bb-f054-408c-8ad9-cbabe93176fc");

        private static readonly Guid SoliditySourceManifestGuid = new Guid("c0a684d8-7f68-4bc3-9b34-60a864cfdf98");

        private const int ProjectIcon = 0;

        private const int SoliditySourceIcon = 1;
        
        public static ImageMoniker ProjectIconImageMoniker
        {
            get
            {
                return new ImageMoniker { Guid = ProjectManifestGuid, Id = ProjectIcon };
            }
        }

        public static ImageMoniker SolidityIconImageMoniker
        {
            get
            {
                return new ImageMoniker { Guid = SoliditySourceManifestGuid, Id = SoliditySourceIcon };
            }
        }
    }
}
