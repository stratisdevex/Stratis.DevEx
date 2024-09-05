using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

using Microsoft.VisualStudio.Workspace;

using Stratis.DevEx;
namespace Stratis.VS.StratisEVM
{
    /// <summary>
    /// File context provider for Solidity files.
    /// </summary>
    [ExportFileContextProvider(ProviderType, StratisEVMPackageIds.SolidityFileContextType)]
    class SolidityFileContextProviderFactory : IWorkspaceProviderFactory<IFileContextProvider>
    {
        // Unique Guid for TxtFileContextProvider.
        private const string ProviderType = "403ac2da-90af-47d9-998a-b9cd1008af90";

        /// <inheritdoc/>
        public IFileContextProvider CreateProvider(IWorkspace workspaceContext)
        {
            return new SolidityFileContextProvider(workspaceContext);
        }

        private class SolidityFileContextProvider : IFileContextProvider
        {
            private IWorkspace workspaceContext;

            internal SolidityFileContextProvider(IWorkspace workspaceContext)
            {
                this.workspaceContext = workspaceContext;
            }

            /// <inheritdoc />
            public async Task<IReadOnlyCollection<FileContext>> GetContextsForFileAsync(string filePath, CancellationToken cancellationToken)
            {
                var fileContexts = new List<FileContext>();

                if (filePath.EndsWith(".sol"))
                {
                    Runtime.Debug("Adding file context for file {file}.", filePath);
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
