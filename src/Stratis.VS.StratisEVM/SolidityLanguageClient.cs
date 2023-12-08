using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel.Composition;

using Task = System.Threading.Tasks.Task;

using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using EnvDTE;

using Newtonsoft.Json.Linq;
using StreamJsonRpc;


using Stratis.DevEx;
using Microsoft.VisualStudio.Shell.Interop;

namespace Stratis.VS
{
    [ContentType("solidity")]
    [Export(typeof(ILanguageClient))]
    [RunOnContext(RunningContext.RunOnHost)]
    public class SolidityLanguageClient : Runtime, ILanguageClient, ILanguageClientCustomMessage2
    {
        #region Constructors
        static SolidityLanguageClient()
        {
            Initialize("Stratis.VS.StratisEVM", "VS");
        }
        public SolidityLanguageClient()
        {
            Instance = this;
            this.MiddleLayer = new SolidityLanguageClientMiddleLayer();
        }
        #endregion

        #region Properties
        public static string SolutionOpenFolder {get; set;}
        
        public string Name => "Solidity Language Extension";

        public IEnumerable<string> ConfigurationSections
        {
            get
            {
                yield return "solidity";
            }
        }

        public object InitializationOptions { get; set; } = null;

        public IEnumerable<string> FilesToWatch => null;

        public object MiddleLayer
        {
            get;
            set;
        }

        public object CustomMessageTarget => null;

        public bool ShowNotificationOnInitializeFailed => true;

        internal static SolidityLanguageClient Instance
        {
            get;
            set;
        }

        internal System.Diagnostics.Process ServerProcess { get; set; }

        internal JsonRpc Rpc
        {
            get;
            set;
        }
        #endregion

        public event AsyncEventHandler<EventArgs> StartAsync;
        public event AsyncEventHandler<EventArgs> StopAsync;

        #region Methods
        protected bool StartLanguageServerProcess()
        {
            ProcessStartInfo info = new ProcessStartInfo();
            var programPath = "cmd.exe";
            info.FileName = programPath;
            info.Arguments = "/c " + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "npm", "nomicfoundation-solidity-language-server.cmd") +  " --stdio";
            info.RedirectStandardInput = true;
            info.RedirectStandardOutput = true;
            info.UseShellExecute = false;
            info.CreateNoWindow = false;
            
            var process = new System.Diagnostics.Process();
            process.StartInfo = info;
            process.EnableRaisingEvents = true;
            process.Exited += (e, args) =>
            {
                Info("Language server proceess exited. Restarting.");
                process.Start();
            };
            ServerProcess = process;
            return process.Start();
        }
        public async Task<Connection> ActivateAsync(CancellationToken token)
        {
            await Task.Yield();
            var solution = (IVsSolution) await ServiceProvider.GetGlobalServiceAsync(typeof(SVsSolution));
            solution.GetSolutionInfo(out var dir, out var f, out var d);
            Info("Solution dir is {d}", dir);
            this.InitializationOptions = JObject.FromObject(new
            {
                workspaceFolders = new string[] {dir},
                rootUri = dir
            });

            if (StartLanguageServerProcess())
            {
                Info("Started language server process {proc} {p}.", ServerProcess.StartInfo.FileName, ServerProcess.StartInfo.Arguments);
                return new Connection(ServerProcess.StandardOutput.BaseStream, ServerProcess.StandardInput.BaseStream);
            }
            else
            {
                Info("Could not start language server process {proc}", ServerProcess.StartInfo.FileName);
                return null;
            }
        }

        public async Task OnLoadedAsync()
        {
            if (StartAsync != null)
            {
                await StartAsync.InvokeAsync(this, EventArgs.Empty);
            }
        }

        public async Task StopServerAsync()
        {
            if (StopAsync != null)
            {
                await StopAsync.InvokeAsync(this, EventArgs.Empty);
            }
        }

        public Task OnServerInitializedAsync()
        {
            return Task.CompletedTask;
        }

        public Task AttachForCustomMessageAsync(JsonRpc rpc)
        {
            this.Rpc = rpc;

            return Task.CompletedTask;
        }

        public Task<InitializationFailureContext> OnServerInitializeFailedAsync(ILanguageClientInitializationInfo initializationState)
        {
            string message = "Solidity language server failed to initialize.";
            Error(message);
            string exception = initializationState.InitializationException?.ToString() ?? string.Empty;
            message = $"{message}\n {exception}";

            var failureContext = new InitializationFailureContext()
            {
                FailureMessage = message,
            };

            return Task.FromResult(failureContext);
        }

        #endregion
        internal class SolidityLanguageClientMiddleLayer : ILanguageClientMiddleLayer
        {
            public bool CanHandle(string methodName)
            {
                return true;
            }

            public async Task HandleNotificationAsync(string methodName, JToken methodParam, Func<JToken, Task> sendNotification)
            {
                Info("Notification {req} {param}.", methodName, methodParam.ToString());
                await sendNotification(methodParam);
            }

            public async Task<JToken> HandleRequestAsync(string methodName, JToken methodParam, Func<JToken, Task<JToken>> sendRequest)
            {
                var resp = await sendRequest(methodParam); 
                Info("Request {req} {param}: {resp}", methodName, methodParam.ToString(), resp?.ToString() ?? "(null)");
                return resp;
            }
        }
    }
}
