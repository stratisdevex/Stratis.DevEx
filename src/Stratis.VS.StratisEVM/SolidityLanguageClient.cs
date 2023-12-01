using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json.Linq;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using System.ComponentModel.Composition;


using Stratis.DevEx;
using EnvDTE;
using System.Configuration;
using System.IO.Pipelines;

namespace Stratis.VS
{  
    [ContentType("solidity")]
    [Export(typeof(ILanguageClient))]
    [RunOnContext(RunningContext.RunOnHost)]
    public class SolidityLanguageClient : Runtime, ILanguageClient, ILanguageClientCustomMessage2
    {
        
        static SolidityLanguageClient()
        {
            Initialize("Stratis.VS.Solidity", "LanguageClient");
        }
        public SolidityLanguageClient()
        {
            Instance = this;
            this.MiddleLayer = new SolidityLanguageClientMiddleLayer();
        }

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

        public event AsyncEventHandler<EventArgs> StartAsync;
        public event AsyncEventHandler<EventArgs> StopAsync;

        public string Name => "Solidity Language Extension";

        public IEnumerable<string> ConfigurationSections
        {
            get
            {
                yield return "solidity";
            }
        }

        public object InitializationOptions => null;

        public IEnumerable<string> FilesToWatch => null;

        public object MiddleLayer
        {
            get;
            set;
        }

        public object CustomMessageTarget => null;

        public bool ShowNotificationOnInitializeFailed => true;

        protected bool StartLanguageServerProcess()
        {
            ProcessStartInfo info = new ProcessStartInfo();
            //var programPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Server", @"LanguageServerWithUI.exe");
            //var programPath = "nomicfoundation-solidity-language-server";
            var programPath = "cmd.exe";
            info.FileName = programPath;
            //info.WorkingDirectory = Path.GetDirectoryName(programPath);
            info.Arguments = "/c C:\\Users\\Allister\\AppData\\Roaming\\npm\\nomicfoundation-solidity-language-server.cmd --stdio";
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
            //Debugger.Launch();

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

        internal class SolidityLanguageClientMiddleLayer : ILanguageClientMiddleLayer
        {
            public bool CanHandle(string methodName)
            {
                //Info("Calling method {m}", methodName);
                //return methodName == Methods.TextDocumentCompletionName;
                return false;
            }

            public async Task HandleNotificationAsync(string methodName, JToken methodParam, Func<JToken, Task> sendNotification)
            {
                Info("Notification {req} {param}.", methodName, methodParam.ToString());
                await sendNotification(methodParam);
                //return Task.CompletedTask;
                //throw new NotImplementedException();
            }

            public async Task<JToken> HandleRequestAsync(string methodName, JToken methodParam, Func<JToken, Task<JToken>> sendRequest)
            {
                Info("Request {req} {param}.", methodName, methodParam.ToString());
                var result = await sendRequest(methodParam);
                return result;
            }
        }
    }
}
