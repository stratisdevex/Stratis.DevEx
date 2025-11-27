using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

using Microsoft.VisualStudio.Shell;

namespace Stratis.VS.StratisEVM
{
    internal sealed class SolidityProjectMenuCommands
    {
        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("1370374f-6ad4-4975-80fb-6d3487cd4ba9");

        public const int CompileCommandId = 0x0100;

        public const int InstallPackagesCommandId = 0x0101;

        public const int ShowDeployToolWindowCommandId = 0x0102;

        public const int AnalyzeCommandId = 0x0104;
        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="SolidityProjectMenuCommands"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private SolidityProjectMenuCommands(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CompileCommandId);
            var menuItem = new OleMenuCommand(CompileFile, menuCommandID)
            {
                Supported = false
            };
            menuItem.ParametersDescription = "$";
            commandService.AddCommand(menuItem);
            menuCommandID = new CommandID(CommandSet, InstallPackagesCommandId);
            menuItem = new OleMenuCommand(InstallNPMPackages, menuCommandID)
            {
                Supported = false
            };
            commandService.AddCommand(menuItem);
            menuCommandID = new CommandID(CommandSet, ShowDeployToolWindowCommandId);
            menuItem = new OleMenuCommand(InstallNPMPackages, menuCommandID)
            {
                Supported = false
            };
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static SolidityProjectMenuCommands Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IAsyncServiceProvider ServiceProvider => this.package;
        
        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in SolidityProjectMenuCommands's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new SolidityProjectMenuCommands(package, commandService);
        }

        private async Task CompileFileAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            EnvDTE80.DTE2 dte = (EnvDTE80.DTE2) await ServiceProvider.GetServiceAsync(typeof(EnvDTE.DTE));
            var file = dte.SelectedItems.Item(1).ProjectItem.Properties.Item("FullPath").Value.ToString();
            var dir = Path.GetDirectoryName(dte.SelectedItems.Item(1).ProjectItem.ContainingProject.FileName);
            await SolidityCompiler.CompileFileAsync(file, dir);
        }

        private async Task InstallNPMPackagesAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            EnvDTE80.DTE2 dte = (EnvDTE80.DTE2)await ServiceProvider.GetServiceAsync(typeof(EnvDTE.DTE));
            var dir = Path.GetDirectoryName(dte.SelectedItems.Item(1).ProjectItem.ContainingProject.FileName);
            await SolidityCompiler.InstallNPMPackagesAsync(dir);
        }

        #pragma warning disable VSTHRD110, CS4014
        private void CompileFile(object sender, EventArgs e) => CompileFileAsync();
        private void InstallNPMPackages(object sender, EventArgs e) => InstallNPMPackagesAsync();
#pragma warning restore VSTHRD110, CS4014
    }
}
