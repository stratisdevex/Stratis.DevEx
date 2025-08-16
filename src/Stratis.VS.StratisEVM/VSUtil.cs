using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;  

using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;

using Stratis.DevEx;

namespace Stratis.VS.StratisEVM
{
    public class VSUtil : Runtime
    {
        protected static Dictionary<string, Guid> logWindowPanes = new Dictionary<string, Guid>();

        protected static IVsUIShell uiShell;

        protected static IVsOutputWindow outputWindow;

        protected static string[] LogWindowNames = new string[] { "StratisEVM", "Solidity Compiler" };
        
        public static void LogToBuildWindow(string text)
        {
            ThreadHelper.ThrowIfNotOnUIThread();    
            var guid = VSConstants.GUID_BuildOutputWindowPane;
            IVsOutputWindowPane buildOutputWindowPane;
            outputWindow.GetPane(ref guid, out buildOutputWindowPane);
            buildOutputWindowPane.OutputStringThreadSafe(text);
            buildOutputWindowPane.Activate(); 
        }

        protected static IVsOutputWindowPane GetCustomLogOutputPane(string name)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            IVsOutputWindowPane outputPane = null;
            if (!logWindowPanes.ContainsKey(name) || ErrorHandler.Failed(outputWindow.GetPane(logWindowPanes[name], out outputPane)))
            {
                var guid = Guid.NewGuid();
                outputWindow.CreatePane(ref guid, name, 1, 1);
                outputWindow.GetPane(ref guid, out outputPane);
                if (outputPane != null)
                {
                    logWindowPanes[name] = guid;
                }
            }
            return outputPane;
        }
        public static void LogInfo(string logname, string text)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Info("(" + logname + ") " + text);
            IVsOutputWindowPane outputPane = GetCustomLogOutputPane(logname);
            if (outputPane is null)
            {
                Error("Could not get output window pane {l}.", logname);
                return;
            }
            else
            {
                if (ErrorHandler.Failed(outputPane.OutputStringThreadSafe(DateTime.Now.ToString() + ": " + text + Environment.NewLine)))
                {
                    Error("Could not log to output window pane {l}.", logname);
                }
            }
        }

        public static void LogError(string logname, string text)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Error("(" + logname + ") " + text);
            IVsOutputWindowPane outputPane = GetCustomLogOutputPane(logname);
            if (outputPane is null)
            {
                Error("Could not get output window pane {l}.", logname);
                return;
            }
            else
            {
                if (ErrorHandler.Failed(outputPane.OutputStringThreadSafe(DateTime.Now.ToString() + ": " + text + Environment.NewLine)))
                {
                    Error("Could not log to output window pane {l}.", logname);
                }
            }
        }

        public static void LogError(string logname, Exception ex)
        {
            LogError(logname, "(" + logname + ") (exception) " + ex.Message);
        }
        public static bool InitializeVSServices(IServiceProvider provider)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (uiShell is null)
            {
                uiShell = provider.GetService(typeof(SVsUIShell)) as IVsUIShell;
                if (uiShell is null)
                {
                    Error("Could not initialize Visual Studio UI shell.");
                    return false;
                }
            }
            if (outputWindow is null)
            {
                var ow = provider.GetService(typeof(SVsOutputWindow)) as IVsOutputWindow;
                if (ow != null)
                {
                    outputWindow = ow;
                }
                else
                {
                    Error("Could not initialize Visual Studio Output Window.");
                    return false;
                }

            }
            IVsOutputWindowPane outputPane = null;
            foreach (var name in LogWindowNames)
            {
                if (!logWindowPanes.ContainsKey(name) || ErrorHandler.Failed(outputWindow.GetPane(logWindowPanes[name], out outputPane)))
                {
                    var guid = Guid.NewGuid();
                    outputWindow.CreatePane(ref guid, name, 1, 1);
                    outputWindow.GetPane(ref guid, out outputPane);
                    if (outputPane != null)
                    {
                        logWindowPanes[name] = guid;
                    }
                    else
                    {
                        Error("Could not create Visual Studio Ouput Window pane {name}.", name);
                    }
                }
            }
            VSServicesInitialized = true;
            return VSServicesInitialized;
        }

        public static void ShowLogOutputWindowPane(IServiceProvider provider, string pane)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            IVsWindowFrame windowFrame;
            if (uiShell != null)
            {
                uint flags = (uint)__VSFINDTOOLWIN.FTW_fForceCreate;
                uiShell.FindToolWindow(flags, VSConstants.StandardToolWindows.Output, out windowFrame);
                windowFrame.Show();
                var p = GetCustomLogOutputPane(pane);
                if (p != null)
                {
                    p.Activate();
                }
                else
                {
                    Error("Could not get a reference to the VsUIShell.");
                }
            }
            else
            {
                Error("Could not get a reference to the VsUIShell.");
            }
        }

        public static bool CheckRunCmdOutput(Dictionary<string, object> output, string logname, string checktext)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (output.ContainsKey("error") || output.ContainsKey("exception"))
            {
                if (output.ContainsKey("error"))
                {
                    LogError(logname, (string)output["error"]);
                }
                if (output.ContainsKey("exception"))
                {
                    LogError(logname, (Exception)output["exception"]);
                }
                return false;
            }
            else
            {
                if (output.ContainsKey("stderr"))
                {
                    var stderr = (string)output["stderr"];
                    LogInfo(logname, stderr);
                }
                if (output.ContainsKey("stdout"))
                {
                    var stdout = (string)output["stdout"];
                    LogInfo(logname, stdout);
                    if (stdout.Contains(checktext))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        public static EnvDTE.Project GetDTEProject(IVsHierarchy hierarchy)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out var objProj);
            return (EnvDTE.Project)objProj;
        }


        public static UnconfiguredProject GetUnconfiguredProject(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            IVsBrowseObjectContext context = project as IVsBrowseObjectContext;
            if (context == null && project != null)
            { // VC implements this on their DTE.Project.Object
                context = project.Object as IVsBrowseObjectContext;
            }

            return context != null ? context.UnconfiguredProject : null;
        }

        public static UnconfiguredProject GetUnconfiguredProject(IVsHierarchy hierarchy) => GetUnconfiguredProject(GetDTEProject(hierarchy));

        public static string LoadUserSettings(IServiceProvider serviceProvider, string propertyName, string defaultValue = null)
        {
            SettingsManager settingsManager = new ShellSettingsManager(serviceProvider);
            SettingsStore settingsStore = settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);
            return settingsStore.GetString("StratisEVM", propertyName, defaultValue);
        }

        public static void SaveUserSettings(IServiceProvider serviceProvider, string propertyName, string value)
        {
            SettingsManager settingsManager = new ShellSettingsManager(serviceProvider);
            WritableSettingsStore settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
            settingsStore.CreateCollection("StratisEVM");
            settingsStore.SetString("StratisEVM", propertyName, value);
        }

        public static void ShowModalErrorDialogBox(string text, string title = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            VsShellUtilities.ShowMessageBox(StratisEVMPackage.Instance, text, title ?? "StratisEVM error",
            OLEMSGICON.OLEMSGICON_CRITICAL,
            OLEMSGBUTTON.OLEMSGBUTTON_OK,
            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        public static bool IsProjectLoaded()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = StratisEVMPackage.Instance.GetService<EnvDTE.DTE, EnvDTE80.DTE2>();
            if (dte == null)
            {
                ShowModalErrorDialogBox("Could not get DTE service.");
                return false;
            }
            if (dte.Solution == null || dte.Solution.Projects == null || dte.Solution.Projects.Count == 0)
            {
                return false;
            }
            else
            {
                Array a = (Array)dte.ActiveSolutionProjects;
                return a != null && a.Length > 0;
            }
        }

        public static Project GetSelectedProject()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = StratisEVMPackage.Instance.GetService<DTE, EnvDTE80.DTE2>();
            if (dte == null)
            {
                ShowModalErrorDialogBox("Could not get DTE service.");
                return null;
            }          
            Array a = (Array)dte.ActiveSolutionProjects;
            var si = dte.SelectedItems;
            if (si == null || si.Count == 0 || si.Item(1).Project == null)
            {
                ShowModalErrorDialogBox("No project is selected or could not get selected project.");
                return null;
            }
            else
            {
                return si.Item(1).Project;
            }
        }

        public static List<string> GetSolidityProjectContracts(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();    
            List<string> contracts = new List<string>();
            foreach (EnvDTE.ProjectItem item in project.ProjectItems)
            {
                if (item.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFile && item.Name.EndsWith(".sol", StringComparison.OrdinalIgnoreCase))
                {
                    contracts.Add(item.Name);
                }
            }
            return contracts;
        }

        public IProjectLockService GetProjectLockService()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // Get access to global MEF services.
            IComponentModel componentModel = StratisEVMPackage.Instance.GetService<SComponentModel, IComponentModel>();
            // Get access to project services scope services.
            IProjectServiceAccessor projectServiceAccessor = componentModel.GetService<IProjectServiceAccessor>();
            var projectService = projectServiceAccessor.GetProjectService();
            return projectService.Services.ProjectLockService;
        }

        public static IVsHierarchy GetProjectVsHierarchy(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var solutionService = StratisEVMPackage.Instance.GetService<SVsSolution, IVsSolution>();
            ErrorHandler.ThrowOnFailure(solutionService.GetProjectOfUniqueName(project.UniqueName, out var projectHierarchy));
            return projectHierarchy;    
        }

        public static string GetProjectProperty(Project project, string name)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var hier = GetProjectVsHierarchy(project) as IVsBuildPropertyStorage;
            ErrorHandler.ThrowOnFailure(hier.GetPropertyValue(name, project.ConfigurationManager.ActiveConfiguration.ConfigurationName, (uint)_PersistStorageType.PST_PROJECT_FILE, out var value));
            return value;
        }

        public static bool SetProjectProperty(Project project, string name, string value)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var hier = GetProjectVsHierarchy(project) as IVsBuildPropertyStorage;
            return ErrorHandler.Succeeded(hier.SetPropertyValue(name, project.ConfigurationManager.ActiveConfiguration.ConfigurationName, (uint)_PersistStorageType.PST_PROJECT_FILE, value));
        }

        public static Dictionary<string, object> GetSolidityProjectProperties(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var hier = GetProjectVsHierarchy(project) as IVsBuildPropertyStorage;
            Dictionary<string, object> props = new Dictionary<string, object>();
            ErrorHandler.ThrowOnFailure(hier.GetPropertyValue("DeployURL", project.ConfigurationManager.ActiveConfiguration.ConfigurationName, (int) _PersistStorageType.PST_PROJECT_FILE, out string deployUrl));
            if (!string.IsNullOrEmpty(deployUrl))
            {
                props["DeployURL"] = deployUrl;
            }
            return props;
        }

      

        public static bool BuildProject(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = StratisEVMPackage.Instance.GetService<DTE, EnvDTE80.DTE2>();
            var solution = dte.Solution;
            solution.SolutionBuild.BuildProject(solution.SolutionBuild.ActiveConfiguration.Name, project.UniqueName, true);
            return solution.SolutionBuild.LastBuildInfo == 0;   
        }

        public static Dictionary<string, FileInfo> GetSmartContractProjectOutput(Project project, string contractFileName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var outputDir = Path.Combine(Path.GetDirectoryName(project.FileName), project.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString());
            Dictionary<string, FileInfo> outputFiles = new Dictionary<string, FileInfo>();
            var abi = Path.Combine(outputDir, Path.ChangeExtension(contractFileName, "abi"));
            if (File.Exists(abi))
            {
                outputFiles.Add("abi", new FileInfo(abi));
            }
            var bin = Path.Combine(outputDir, Path.ChangeExtension(contractFileName, "bin"));
            if (File.Exists(bin))
            {
                outputFiles.Add("bin", new FileInfo(bin));
            }
            var gj = Path.Combine(outputDir, Path.ChangeExtension(contractFileName, "gas.json"));
            if (File.Exists(gj))
            {
                outputFiles.Add("gas", new FileInfo(gj));
            }
            var oct = Path.Combine(outputDir, Path.ChangeExtension(contractFileName, "opcodes.txt"));
            if (File.Exists(oct))
            {
                outputFiles.Add("opcodes", new FileInfo(oct));
            }
            return outputFiles;   
        }

        public static bool VSServicesInitialized = false;
    }
}
