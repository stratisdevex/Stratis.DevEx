﻿using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

using Microsoft.VisualStudio.Shell;

using Microsoft.VisualStudio.Shell.Interop;
using Stratis.DevEx;
using Microsoft.VisualStudio.TaskRunnerExplorer;

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
    [Guid(StratisEVMPackage.PackageGuidString)]
    [ProvideAutoLoad("4646B819-1AE0-4E79-97F4-8A8176FDD664", PackageAutoLoadFlags.BackgroundLoad)]
    //[ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class StratisEVMPackage : AsyncPackage, IVsSolutionEvents7
    {
         /// <summary>
        /// Stratis.VS.StratisEVMPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "711b90a1-97e6-4b9a-91c4-3d62ccd32d4e";

        #region Constructors
        static StratisEVMPackage()
        {
            Runtime.Initialize("Stratis.VS.StratisEVM", "VS");
        }
        #endregion

        #region Methods

        #region IVsSolutionEvents7
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
            Runtime.Info("StratisEVM package initialized.");
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            VSUtil.InitializeVSServices(this);
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            VSUtil.LogInfo("Stratis EVM", "log init");
           

        }
        #endregion

        #endregion
    }
}
