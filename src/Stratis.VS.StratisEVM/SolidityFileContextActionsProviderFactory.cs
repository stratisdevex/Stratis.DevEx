using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Workspace;
using Microsoft.VisualStudio.Workspace.Extensions.VS;
using System.Linq;

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
                //new CommandID(StratisEVMPackageIds.GuidStratisEVMPackageCmdSet, StratisEVMPackageIds.Cmd2Id),
            };

        public IFileContextActionProvider CreateProvider(IWorkspace workspaceContext)
        {
            return new SolidityFileContextActionProvider(workspaceContext);
        }

        public IReadOnlyCollection<CommandID> GetSupportedVsCommands()
        {
            return SupportedCommands;
        }

        internal class SolidityFileContextActionProvider : IFileContextActionProvider
        {
            private static readonly Guid ActionOutputWindowPane = new Guid("b9319ea3-b873-442f-abaa-3c1c1c00fd08");
            private IWorkspace workspaceContext;

            internal SolidityFileContextActionProvider(IWorkspace workspaceContext)
            {
                this.workspaceContext = workspaceContext;
            }

            public Task<IReadOnlyList<IFileContextAction>> GetActionsAsync(string filePath, FileContext fileContext, CancellationToken cancellationToken)
            {
                return Task.FromResult<IReadOnlyList<IFileContextAction>>(new IFileContextAction[]
                {
                    // Word count command:
                    new SolidityFileContextAction(
                        fileContext,
                        new Tuple<Guid, uint>(ProviderCommandGroup, StratisEVMPackageIds.Cmd1Id),
                        "Compile Solidity File" + fileContext.DisplayName,
                        async (fCtxt, progress, ct) =>
                        {
                            await SolidityCompiler.CompileFileAsync(filePath, fileContext.InputFiles.First());
                        }),
                });
            }

            internal static async Task OutputWindowPaneAsync(string message)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                IVsOutputWindowPane outputPane = null;
                var outputWindow = ServiceProvider.GlobalProvider.GetService(typeof(SVsOutputWindow)) as IVsOutputWindow;
                if (outputWindow != null && ErrorHandler.Failed(outputWindow.GetPane(ActionOutputWindowPane, out outputPane)))
                {
                    IVsWindowFrame windowFrame;
                    var vsUiShell = ServiceProvider.GlobalProvider.GetService(typeof(SVsUIShell)) as IVsUIShell;
                    if (vsUiShell != null)
                    {
                        uint flags = (uint)__VSFINDTOOLWIN.FTW_fForceCreate;
                        vsUiShell.FindToolWindow(flags, VSConstants.StandardToolWindows.Output, out windowFrame);
                        windowFrame.Show();
                    }

                    outputWindow.CreatePane(ActionOutputWindowPane, "Solidity Compiler", 1, 1);
                    outputWindow.GetPane(ActionOutputWindowPane, out outputPane);
                    outputPane.Activate();
                }

                outputPane?.OutputStringThreadSafe(message);
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
