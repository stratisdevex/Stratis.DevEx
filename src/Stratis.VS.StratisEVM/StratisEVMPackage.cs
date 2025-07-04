﻿using System;
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
    [ProvideUIContextRule(SolidityProjectFileUIContextRule,
        name: "Solidity Project Configuration File",
        expression: "(SingleProject | MultipleProjects) & Solidity",
        termNames: new[] { "SingleProject", "MultipleProjects", "Solidity" },
        termValues: new[] { SolutionHasSingleProject_string, SolutionHasMultipleProjects_string, "HierSingleSelectionName:.solproj$" })]
    [ProvideUIContextRule(NPMFileUIContextRule,
        name: "NPM Configuration Files",
        expression: "(SingleProject | MultipleProjects) & Solidity",
        termNames: new[] { "SingleProject", "MultipleProjects", "Solidity" },
        termValues: new[] { SolutionHasSingleProject_string, SolutionHasMultipleProjects_string, "HierSingleSelectionName:package.json$" })]
    [ProvideToolWindow(typeof(UI.BlockchainExplorerToolWindow), Style = VsDockStyle.Tabbed, Window = EnvDTE.Constants.vsWindowKindSolutionExplorer)]
    [ProvideToolWindowVisibility(typeof(UI.BlockchainExplorerToolWindow), SolutionHasSingleProject_string)]
    [ProvideToolWindowVisibility(typeof(UI.BlockchainExplorerToolWindow), SolutionHasMultipleProjects_string)]
    [ProvideToolWindowVisibility(typeof(UI.BlockchainExplorerToolWindow), NoSolution_string)]
    [ProvideToolWindowVisibility(typeof(UI.BlockchainExplorerToolWindow), EmptySolution_string)]
    [ProvideToolWindow(typeof(UI.StratisEVMBlockchainDashboardToolWindow), Style = VsDockStyle.MDI)]
    [ProvideToolWindow(typeof(UI.DeploySolidityProjectToolWindow), Style = VsDockStyle.Tabbed, Window = EnvDTE.Constants.vsWindowKindSolutionExplorer)]
    [ProvideToolWindowVisibility(typeof(UI.DeploySolidityProjectToolWindow), SolutionHasSingleProject_string)]
    [ProvideToolWindowVisibility(typeof(UI.DeploySolidityProjectToolWindow), SolutionHasMultipleProjects_string)]
    public sealed partial class StratisEVMPackage : AsyncPackage, IVsSolutionEvents7, IVsSolutionEvents
    {
        #region Constructors
        static StratisEVMPackage()
        {
            Runtime.Initialize("Stratis.VS.StratisEVM", "VS");
        }
        #endregion

        #region Methods

        #region IVsSolutionEvents7 members

        public void OnBeforeCloseFolder(string folderPath) {}

        public void OnAfterCloseFolder(string folderPath) {}

        public void OnQueryCloseFolder(string folderPath, ref int s) {}
        
        public void OnAfterOpenFolder(string folderPath)
        {
            Runtime.Info("Opened solution folder {f}.", folderPath);
        }

        public void OnAfterLoadAllDeferredProjects() {}
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
            Instance = this;
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

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
            
            await TaskScheduler.Default;
            await InstallBuildSystemAsync();

            await JoinableTaskFactory.SwitchToMainThreadAsync();
            await SolidityProjectMenuCommands.InitializeAsync(this);
            await UI.BlockchainExplorerToolWindowCommand.InitializeAsync(this);
            await UI.StratisEVMBlockchainDashboardToolWindowCommand.InitializeAsync(this);
            await UI.DeploySolidityProjectToolWindowCommand.InitializeAsync(this); 
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
            
            var receivingBlock = new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(u =>
            {
                
                //await this.JoinableTaskFactory.SwitchToMainThreadAsync();
                //VSUtil.LogInfo("Stratis EVM", "update");
            });
            subscriptionService.JointRuleSource.SourceBlock.LinkTo(receivingBlock, new JointRuleDataflowLinkOptions() { PropagateCompletion = true}); 
        }
        #endregion

        #endregion

        #region Fields
        public static StratisEVMPackage Instance { get; private set; }
        #endregion

        #region Constants
        public const string PackageGuidString = "711b90a1-97e6-4b9a-91c4-3d62ccd32d4e";

        public const string SolidityFileUIContextRule = "82268519-FB9D-4B7E-8B01-2A311F4181E2";

        public const string SolidityProjectFileUIContextRule = "9d4c64d4-52eb-4ebe-aa01-1d975eb3a9d7";

        public const string NPMFileUIContextRule = "9A7CA75A-FA6E-45B2-B6E9-4BFF0AB7BB88";
        #endregion

    }
}
