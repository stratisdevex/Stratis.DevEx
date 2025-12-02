using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Workspace;
using Microsoft.VisualStudio.Workspace.Build;
using Microsoft.VisualStudio.Workspace.Extensions.VS;
using Microsoft.VisualStudio.Workspace.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace Stratis.VS.StratisEVM
{
    [ExportFileContextActionProvider((FileContextActionProviderOptions)VsCommandActionProviderOptions.SupportVsCommands, ProviderType, ProviderPriority.Normal, StratisEVMPackageIds.SolidityFileContextType)]
    public class SolidityFileContextActionProviderFactory : IWorkspaceProviderFactory<IFileContextActionProvider>, IVsCommandActionProvider
    {
        // Unique Guid for WordCountActionProvider.
        private const string ProviderType = "e4c4063b-0682-43b6-aff5-f81dec7029a5";

        private static readonly Guid ProviderCommandGroup = StratisEVMPackageIds.GuidStratisEVMPackageCmdSet;
        private static readonly IReadOnlyList<CommandID> SupportedCommands = new List<CommandID>
        {
            new CommandID(StratisEVMPackageIds.GuidStratisEVMPackageCmdSet, StratisEVMPackageIds.Cmd1Id),
            new CommandID(StratisEVMPackageIds.GuidStratisEVMPackageCmdSet, StratisEVMPackageIds.Cmd4Id),
        };

        public IFileContextActionProvider CreateProvider(IWorkspace workspaceContext)
        {
            return new SolidityFileContextActionProvider(workspaceContext);
        }

        public IReadOnlyCollection<CommandID> GetSupportedVsCommands() => SupportedCommands;
        
        internal class SolidityFileContextActionProvider : IFileContextActionProvider
        {
            private IWorkspace workspaceContext;
            private string workingFolder;

            internal SolidityFileContextActionProvider(IWorkspace workspaceContext)
            {
                this.workspaceContext = workspaceContext;
                this.workingFolder = workspaceContext.Location;
            }

            public Task<IReadOnlyList<IFileContextAction>> GetActionsAsync(string filePath, FileContext fileContext, CancellationToken cancellationToken)
            {
                return Task.FromResult<IReadOnlyList<IFileContextAction>>(new IFileContextAction[]
                {
                    new SolidityFileContextAction(
                        fileContext,
                        new Tuple<Guid, uint>(ProviderCommandGroup, StratisEVMPackageIds.Cmd1Id),
                        "Compile Solidity File" + fileContext.DisplayName,
                        async (fCtxt, progress, ct) =>
                        {
                            await SolidityCompiler.CompileFileAsync(filePath, fileContext.InputFiles.First());
                        }
                    ),
                    new SolidityFileContextAction(
                        fileContext,
                        new Tuple<Guid, uint>(StratisEVMPackageIds.GuidStratisEVMPackageCmdSet, StratisEVMPackageIds.Cmd4Id),
                        "Analyze Solidity File" + fileContext.DisplayName,
                        async (fCtxt, progress, ct) =>
                        {
                            await SolidityCompiler.AnalyzeAsync(filePath, workingFolder, workingFolder, "0.8.27");
                        }
                    ),
                });
            }
            
            internal class SolidityFileContextAction : IFileContextAction, IVsCommandItem
            {
                private Func<FileContext, IProgress<IFileContextActionProgressUpdate>, CancellationToken, Task> executeAction;

                internal SolidityFileContextAction(
                    FileContext fileContext,
                    Tuple<Guid, uint> command,
                    string displayName,
                    Func<FileContext, IProgress<IFileContextActionProgressUpdate>, CancellationToken, Task> executeAction)
                {
                    this.executeAction = executeAction;
                    this.Source = fileContext;
                    this.CommandGroup = command.Item1;
                    this.CommandId = command.Item2;
                    this.DisplayName = displayName;
                }

                public Guid CommandGroup { get; }
                public uint CommandId { get; }
                public string DisplayName { get; }
                public FileContext Source { get; }

                public async Task<IFileContextActionResult> ExecuteAsync(IProgress<IFileContextActionProgressUpdate> progress, CancellationToken cancellationToken)
                {
                    await this.executeAction(this.Source, progress, cancellationToken);
                    return new FileContextActionResult(true);
                }
            }
        }
    }
}
