using Microsoft.VisualStudio.Imaging.Interop;
using System;

namespace Stratis.VS.StratisEVM
{
    public static class SolidityProjectMonikers
    {
        private static readonly Guid ManifestGuid = new Guid("293347bb-f054-408c-8ad9-cbabe93176fc");

        private const int ProjectIcon = 0;

        public static ImageMoniker ProjectIconImageMoniker
        {
            get
            {
                return new ImageMoniker { Guid = ManifestGuid, Id = ProjectIcon };
            }
        }
    }
}
