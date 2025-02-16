using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Microsoft.IO;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio;
using static Microsoft.VisualStudio.VSConstants.UICONTEXT;

using Stratis.DevEx;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.ProjectSystem;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Stratis.VS.StratisEVM
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "0.1", IconResourceID = 400)]
    [Guid(PackageGuidString)]
    //[ProvideAutoLoad("4646B819-1AE0-4E79-97F4-8A8176FDD664", PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideUIContextRule(SolidityFileUIContextRule,
        name: "Solidity Project Files",
        expression: "(SingleProject | MultipleProjects) & Solidity",
        termNames: new[] { "SingleProject", "MultipleProjects", "Solidity" },
        termValues: new[] { SolutionHasSingleProject_string, SolutionHasMultipleProjects_string, "HierSingleSelectionName:.sol$" })]
    [ProvideUIContextRule(NPMFileUIContextRule,
        name: "NPM Configuration Files",
        expression: "(SingleProject | MultipleProjects) & Solidity",
        termNames: new[] { "SingleProject", "MultipleProjects", "Solidity" },
        termValues: new[] { SolutionHasSingleProject_string, SolutionHasMultipleProjects_string, "HierSingleSelectionName:package.json$" })]
    [ProvideToolWindow(typeof(BlockchainExplorerToolWindow), Style = VsDockStyle.Tabbed, Window = "{3AE79031-E1BC-11D0-8F78-00A0C9110057}")]
    [ProvideToolWindowVisibility(typeof(BlockchainExplorerToolWindow), SolutionHasSingleProject_string)]
    [ProvideToolWindowVisibility(typeof(BlockchainExplorerToolWindow), SolutionHasMultipleProjects_string)]
    [ProvideToolWindowVisibility(typeof(BlockchainExplorerToolWindow), NoSolution_string)]
    [ProvideToolWindowVisibility(typeof(BlockchainExplorerToolWindow), EmptySolution_string)]
    public sealed class StratisEVMPackage : AsyncPackage, IVsSolutionEvents7, IVsSolutionEvents
    {
        #region Constructors
        static StratisEVMPackage()
        {
            Runtime.Initialize("Stratis.VS.StratisEVM", "VS");
        }
        #endregion

        #region Methods

        #region IVsSolutionEvents7 members

        public void OnBeforeCloseFolder(string folderPath)
        {

        }

        public void OnAfterCloseFolder(string folderPath)
        {

        }

        public void OnQueryCloseFolder(string folderPath, ref int s)
        {

        }
        public void OnAfterOpenFolder(string folderPath)
        {
            Runtime.Info("Opened solution folder {f}.", folderPath);
        }

        public void OnAfterLoadAllDeferredProjects()
        {

        }
        #endregion

        #region IVsSolutionEvents members

        int IVsSolutionEvents.OnAfterCloseSolution(object pUnkReserved) => VSConstants.S_OK;
        
        int IVsSolutionEvents.OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) => VSConstants.S_OK;

        int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            if (pHierarchy.IsCapabilityMatch("CPS") && pHierarchy.IsCapabilityMatch(SolidityUnconfiguredProject.UniqueCapability))
            {
                var unconfiguredProject = VSUtil.GetUnconfiguredProject(pHierarchy);
                InstallSolidityProjectDataFlowSinks(unconfiguredProject);   
            }
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenSolution(object pUnkReserved, int fNewSolution) => VSConstants.S_OK;

        int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) => VSConstants.S_OK;

        int IVsSolutionEvents.OnBeforeCloseSolution(object pUnkReserved) => VSConstants.S_OK;

        int IVsSolutionEvents.OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) => VSConstants.S_OK;

        int IVsSolutionEvents.OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) => VSConstants.S_OK;

        int IVsSolutionEvents.OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) => VSConstants.S_OK;

        int IVsSolutionEvents.OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) => VSConstants.S_OK;

        #endregion

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await SolidityProjectMenuCommands.InitializeAsync(this);
            if (!VSUtil.VSServicesInitialized)
            {
                if (VSUtil.InitializeVSServices(ServiceProvider.GlobalProvider))
                {
                    VSUtil.LogInfo("Stratis EVM", $"Extension assembly directory is {Runtime.AssemblyLocation}. StratisEVM package services initialized.");
                }
                else
                {
                    Runtime.Error("Could not initialize StratisEVM package services.");
                    return;
                }
            }

            IVsSolution solution = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            solution.AdviseSolutionEvents(this, out var c);
            // Here we initialize our internal IPackageService implementations, both in global and project services scope.

            // Get access to global MEF services.
            IComponentModel componentModel = await this.GetServiceAsync<SComponentModel, IComponentModel>();

            // Get access to project services scope services.
            IProjectServiceAccessor projectServiceAccessor = componentModel.GetService<IProjectServiceAccessor>();

            //projectServiceAccessor.GetProjectService().
            // Find package services in global scope.
            //IEnumerable<ISolidityProjectServices> globalPackageServices = componentModel.GetExtensions<ISolidityProjectServices>();

            // Find package services in project service scope.
            
        
            await TaskScheduler.Default;

            await InstallBuildSystemAsync();

            //await BlockchainNetworks.TestAsync();

            await JoinableTaskFactory.SwitchToMainThreadAsync();

            await BlockchainExplorerToolWindowCommand.InitializeAsync(this);
            
        }
        #endregion

        #region Static Methods
        public static async Task InstallBuildSystemAsync()
        {
#if DEBUG
            await Runtime.CopyDirectoryAsync(Runtime.AssemblyLocation.CombinePath("BuildSystem"), Runtime.LocalAppDataDir.CombinePath("CustomProjectSystems", "Solidity"), true);

            if (!Directory.Exists(Runtime.LocalAppDataDir.CombinePath("CustomProjectSystems", "Solidity", "Tools")))
            {
                Directory.CreateDirectory(Runtime.LocalAppDataDir.CombinePath("CustomProjectSystems", "Solidity", "Tools"));
            }
            await Runtime.CopyFileAsync(Runtime.AssemblyLocation.CombinePath("Stratis.VS.SolidityProjectBuildTasks.dll"), Runtime.LocalAppDataDir.CombinePath("CustomProjectSystems", "Solidity", "Tools", "Stratis.VS.SolidityProjectBuildTasks.dll"));
            if (!File.Exists(Runtime.LocalAppDataDir.CombinePath("CustomProjectSystems", "Solidity", "Tools", "CompactJson.dll")))
            {
                await Runtime.CopyFileAsync(Runtime.AssemblyLocation.CombinePath("CompactJson.dll"), Runtime.LocalAppDataDir.CombinePath("CustomProjectSystems", "Solidity", "Tools", "CompactJson.dll"));
            }
#else
            if (!Directory.Exists(Runtime.LocalAppDataDir.CombinePath("CustomProjectSystems", "Solidity")))
            {
                await Runtime.CopyDirectoryAsync(Runtime.AssemblyLocation.CombinePath("BuildSystem"), Runtime.LocalAppDataDir.CombinePath("CustomProjectSystems", "Solidity"), true);

                if (!Directory.Exists(Runtime.LocalAppDataDir.CombinePath("CustomProjectSystems", "Solidity", "Tools")))
                {
                    Directory.CreateDirectory(Runtime.LocalAppDataDir.CombinePath("CustomProjectSystems", "Solidity", "Tools"));
                }
                await Runtime.CopyFileAsync(Runtime.AssemblyLocation.CombinePath("Stratis.VS.SolidityProjectBuildTasks.dll"), Runtime.LocalAppDataDir.CombinePath("CustomProjectSystems", "Solidity", "Tools", "Stratis.VS.SolidityProjectBuildTasks.dll"));
                if (!File.Exists(Runtime.LocalAppDataDir.CombinePath("CustomProjectSystems", "Solidity", "Tools", "CompactJson.dll")))
                {
                    await Runtime.CopyFileAsync(Runtime.AssemblyLocation.CombinePath("CompactJson.dll"), Runtime.LocalAppDataDir.CombinePath("CustomProjectSystems", "Solidity", "Tools", "CompactJson.dll"));
                }
            }
#endif
            await File.WriteAllTextAsync(Runtime.LocalAppDataDir.CombinePath("CustomProjectSystems", "Solidity", "extdir.txt"), Runtime.AssemblyLocation);
        }

        private void InstallSolidityProjectDataFlowSinks(UnconfiguredProject unconfiguredProject)
        {
            var subscriptionService = unconfiguredProject.Services.ActiveConfiguredProjectSubscription;
            
            var receivingBlock = new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(async u =>
            {
                await this.JoinableTaskFactory.SwitchToMainThreadAsync();
                VSUtil.LogInfo("Stratis EVM", "updatw");
            });
            subscriptionService.JointRuleSource.SourceBlock.LinkTo(receivingBlock, new JointRuleDataflowLinkOptions() { PropagateCompletion = true}); 
        }
        #endregion

        #endregion

        #region Constants
        /// <summary>
        /// Stratis.VS.StratisEVMPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "711b90a1-97e6-4b9a-91c4-3d62ccd32d4e";

        public const string SolidityFileUIContextRule = "82268519-FB9D-4B7E-8B01-2A311F4181E2";

        public const string NPMFileUIContextRule = "9A7CA75A-FA6E-45B2-B6E9-4BFF0AB7BB88";
        #endregion

        #region Fields
        //public static Dispatcher _dispatcher;
        #endregion
    }
}
