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


namespace Stratis.VS.StratisEVM
{
    [ExportFileContextActionProvider((FileContextActionProviderOptions)VsCommandActionProviderOptions.SupportVsCommands, ProviderType, ProviderPriority.Normal, StratisEVMPackageIds.SolidityFileContextType)]
    public class WordCountActionProviderFactory : IWorkspaceProviderFactory<IFileContextActionProvider>, IVsCommandActionProvider
    {
        // Unique Guid for WordCountActionProvider.
        private const string ProviderType = "e4c4063b-0682-43b6-aff5-f81dec7029a5";

        private static readonly Guid ProviderCommandGroup = StratisEVMPackageIds.GuidVsPackageCmdSet;
        private static readonly IReadOnlyList<CommandID> SupportedCommands = new List<CommandID>
            {
                new CommandID(StratisEVMPackageIds.GuidVsPackageCmdSet, StratisEVMPackageIds.Cmd1Id),
                new CommandID(StratisEVMPackageIds.GuidVsPackageCmdSet, StratisEVMPackageIds.Cmd2Id),
            };

        public IFileContextActionProvider CreateProvider(IWorkspace workspaceContext)
        {
            return new WordCountActionProvider(workspaceContext);
        }

        public IReadOnlyCollection<CommandID> GetSupportedVsCommands()
        {
            return SupportedCommands;
        }

        internal class WordCountActionProvider : IFileContextActionProvider
        {
            private static readonly Guid ActionOutputWindowPane = new Guid("b9319ea3-b873-442f-abaa-3c1c1c00fd08");
            private IWorkspace workspaceContext;

            internal WordCountActionProvider(IWorkspace workspaceContext)
            {
                this.workspaceContext = workspaceContext;
            }

            public Task<IReadOnlyList<IFileContextAction>> GetActionsAsync(string filePath, FileContext fileContext, CancellationToken cancellationToken)
            {
                return Task.FromResult<IReadOnlyList<IFileContextAction>>(new IFileContextAction[]
                {
                    // Word count command:
                    new MyContextAction(
                        fileContext,
                        new Tuple<Guid, uint>(ProviderCommandGroup, StratisEVMPackageIds.Cmd1Id),
                        "My Action" + fileContext.DisplayName,
                        async (fCtxt, progress, ct) =>
                        {
                            
                            await OutputWindowPaneAsync("command 1");
                        }),

                    // Toggle word count type command:
                    new MyContextAction(
                        fileContext,
                        new Tuple<Guid, uint>(ProviderCommandGroup, StratisEVMPackageIds.Cmd2Id),
                        "My Action" + fileContext.DisplayName,
                        async (fCtxt, progress, ct) =>
                        {
                            await OutputWindowPaneAsync("command 2");
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

                    outputWindow.CreatePane(ActionOutputWindowPane, "Actions", 1, 1);
                    outputWindow.GetPane(ActionOutputWindowPane, out outputPane);
                    outputPane.Activate();
                }

                outputPane?.OutputStringThreadSafe(message);
            }

            internal class MyContextAction : IFileContextAction, IVsCommandItem
            {
                private Func<FileContext, IProgress<IFileContextActionProgressUpdate>, CancellationToken, Task> executeAction;

                internal MyContextAction(
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
