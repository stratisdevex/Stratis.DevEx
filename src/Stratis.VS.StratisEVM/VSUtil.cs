using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

using Stratis.DevEx;
using static System.Net.Mime.MediaTypeNames;

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

        public static bool VSServicesInitialized = false;
    }
}
