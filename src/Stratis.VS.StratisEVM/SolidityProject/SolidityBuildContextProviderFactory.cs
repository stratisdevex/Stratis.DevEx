using Microsoft.VisualStudio.Workspace;
using Microsoft.VisualStudio.Workspace.Build;
using Microsoft.VisualStudio.Workspace.Extensions.VS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stratis.VS.StratisEVM.SolidityProject
{
    [ExportFileContextProvider(ProviderType, BuildContextTypes.BuildContextType)]
    public class SolidityBuildContextProviderFactory : IWorkspaceProviderFactory<IFileContextProvider>
    {
        // Unique Guid for TxtFileContextProvider.
        private const string ProviderType = "401645c3-6573-4208-a59c-aa815cd7b73c";

        public IFileContextProvider CreateProvider(IWorkspace workspaceContext)
        {
            return new SolidityBuildContextProvider(workspaceContext);
        }

        private class SolidityBuildContextProvider : IFileContextProvider
        {
            private IWorkspace workspaceContext;

            internal SolidityBuildContextProvider(IWorkspace workspaceContext)
            {
                this.workspaceContext = workspaceContext;
            }

            /// <inheritdoc />
            public async Task<IReadOnlyCollection<FileContext>> GetContextsForFileAsync(string filePath, CancellationToken cancellationToken)
            {
                var fileContexts = new List<FileContext>();

                if (filePath.EndsWith(".sol"))
                {
                    //Runtime.Debug("Adding file context for file {file}.", filePath);
                    fileContexts.Add(new FileContext(
                        new Guid(ProviderType),
                        new Guid(StratisEVMPackageIds.SolidityFileContextType),
                        filePath + "\n",
                        new string[] { this.workspaceContext.Location }));
                }

                return await Task.FromResult(fileContexts.ToArray());
            }
        }
    }
}
