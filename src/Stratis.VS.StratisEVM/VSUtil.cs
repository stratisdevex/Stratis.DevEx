using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

using Stratis.DevEx;

namespace Stratis.VS.StratisEVM
{
    public class VSUtil : Runtime
    {
        protected static Dictionary<string, Guid> logWindowPanes = new Dictionary<string, Guid> ();

        protected static IVsUIShell uiShell;
        
        protected static IVsOutputWindow outputWindow;

        protected static string[] LogWindowNames = new string[] { "Stratis EVM", "Solidity Compiler" };
        protected static IVsOutputWindowPane GetLogOutputPane(string name)
        {
            ThreadHelper.ThrowIfNotOnUIThread ();
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
            IVsOutputWindowPane outputPane = GetLogOutputPane(logname);
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
            IVsOutputWindowPane outputPane = GetLogOutputPane(logname);
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
                    Error("Could not initialize Visual Studio Ouput Window.");
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
                var p = GetLogOutputPane(pane);
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
                    LogError(logname, (Exception) output["exception"]);
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
        public static bool VSServicesInitialized = false;
    }
}
