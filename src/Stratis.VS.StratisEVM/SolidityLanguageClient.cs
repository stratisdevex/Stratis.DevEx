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
using System.Windows.Ink;
using Stratis.VS.StratisEVM;
using System.Text;

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
        public static string SolutionOpenFolder { get; set; }

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

        internal JsonRpc Rpc
        {
            get;
            set;
        }
        #endregion

        #region Events
        public event AsyncEventHandler<EventArgs> StartAsync;
        public event AsyncEventHandler<EventArgs> StopAsync;
        #endregion

        #region Methods
        protected bool StartLanguageServerProcess()
        {
            ProcessStartInfo info = new ProcessStartInfo();
            var programPath = "cmd.exe";
            info.FileName = programPath;
            //info.Arguments = "/c " + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "npm", "nomicfoundation-solidity-language-server.cmd") +  " --stdio";
            info.Arguments = "/c " + "node \"" + Path.Combine(Runtime.AssemblyLocation, "node_modules", "solidity", "dist", "cli", "server.js") + "\" --stdio";
            info.WorkingDirectory = AssemblyLocation;
            info.RedirectStandardInput = true;
            info.RedirectStandardOutput = true;
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            var process = new System.Diagnostics.Process();
            process.StartInfo = info;
            process.EnableRaisingEvents = true;
            process.Exited += (e, args) =>
            {
                Info("Language server proceess exited.");
            };
            serverProcess = process;
            try
            {
                return process.Start();
            }
            catch (Exception ex) 
            {
                Error(ex, "Exception thrown starting language server process.");
                return false;
            }

        }

        public static Dictionary<string, object> InstallVSCodeSolidityLanguageServer()
        {
            return RunCmd("cmd.exe", "/c npm install solidity-0.0.165.tgz --force --quiet --no-progress", AssemblyLocation);
        }

        public static async Task<Dictionary<string, object>> InstallVSCodeSolidityLanguageServerAsync()
        {
            return await RunCmdAsync("cmd.exe", "/c npm install solidity-0.0.165.tgz --force --quiet --no-progress", AssemblyLocation);
        }

        #region ILanguageClient, ILanguageClientCustomMessage2 implementation
        public async Task<Connection> ActivateAsync(CancellationToken token)
        {
            await Task.Yield();
            if (Directory.Exists(Path.Combine(Runtime.AssemblyLocation, "node_modules")) && File.Exists(Path.Combine(Runtime.AssemblyLocation, "node_modules", "solidity", "dist", "cli", "server.js")))
            {
                Info("VSCode Solidity language server present.");
            }
            else
            {
                var output = await InstallVSCodeSolidityLanguageServerAsync();
                if (CheckRunCmdOutput(output, "Run `npm audit` for details."))
                {
                    Info("VSCode Solidity language server installed.");
                }
                else
                {
                    Error("Could not install VSCode Solidity language server.");
                    return null;
                }
            }
            var solution = (IVsSolution)await ServiceProvider.GetGlobalServiceAsync(typeof(SVsSolution));
            solution.GetSolutionInfo(out var dir, out var f, out var d);
            Info("Solution dir is {d}", dir);
            this.InitializationOptions = JObject.FromObject(new
            {
                enabledAsYouTypeCompilationErrorCheck = true,
                maxNumberOfProblems = true
                //workspaceFolders = new string[] {dir},
                //rootUri = dir
                //compileUsingLocalVersion = "C:\\Users\\Allister\\Downloads\\soljson.js",
                //enabledAsYouTypeCompilationErrorCheck = true,
                //defaultCompiler = "local",
                //compileUsingLocalVersion = "C:\\Users\\Allister\\Downloads\\soljson.js"
                //compileUsingRemoteVersion = "latest",
            });

            if (StartLanguageServerProcess())
            {
                Info("Started language server process {proc} {p}.", serverProcess.StartInfo.FileName, serverProcess.StartInfo.Arguments);
                return new Connection(serverProcess.StandardOutput.BaseStream, serverProcess.StandardInput.BaseStream);
            }
            else
            {
                Error("Could not start language server process: {proc} {args}. Exit code: {code}.", serverProcess.StartInfo.FileName, serverProcess.StartInfo.Arguments, serverProcess.ExitCode);
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

        #endregion

        #region Types
        internal class SolidityLanguageClientMiddleLayer : ILanguageClientMiddleLayer
        {
            public bool CanHandle(string methodName)
            {
                return true;
            }

            public async Task HandleNotificationAsync(string methodName, JToken methodParam, Func<JToken, Task> sendNotification)
            {
                //Info("Notification {req} {param}.", methodName, methodParam.ToString());
                //await sendNotification(methodParam);

                if (methodName == "textDocument/didChange")
                {
                    methodParam.Root["contentChanges"] = JArray.FromObject(new[] {new
                    {
                        text = methodParam.Root["contentChanges"][0]["text"].Value<string>(),
                    } });
                    Info("didchange Notification {req} {param}.", methodName, methodParam.ToString());
                    await sendNotification(methodParam);

                }
                else
                {
                    Info("Notification {req} {param}.", methodName, methodParam.ToString());
                    await sendNotification(methodParam);
                }

            }

            public async Task<JToken> HandleRequestAsync(string methodName, JToken methodParam, Func<JToken, Task<JToken>> sendRequest)
            {
                var resp = await sendRequest(methodParam);
                Info("Request {req} {param}: {resp}", methodName, methodParam.ToString(), resp?.ToString() ?? "(null)");
                return resp;
            }
        }
        #endregion

        #region Fields
        internal System.Diagnostics.Process serverProcess;
        #endregion
    }




}
