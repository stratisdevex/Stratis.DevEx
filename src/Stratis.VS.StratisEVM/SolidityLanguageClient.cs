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

        public string Name => "Solidity Language Client";

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
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            //await Task.Yield();
            if (!VSUtil.VSServicesInitialized)
            {
                if (VSUtil.InitializeVSServices(ServiceProvider.GlobalProvider))
                {
                    VSUtil.LogInfo("Stratis EVM", "StratisEVM package services initialized.");
                }
                else
                {
                    Error("Could not initialize StratisEVM package services.");
                    return null;
                }
            }
            if (Directory.Exists(Path.Combine(Runtime.AssemblyLocation, "node_modules")) && File.Exists(Path.Combine(Runtime.AssemblyLocation, "node_modules", "solidity", "dist", "cli", "server.js")))
            {
               VSUtil.LogInfo("Stratis EVM", "vscode-solidity language server present.");
            }
            else
            {
                VSUtil.ShowLogOutputWindowPane(ServiceProvider.GlobalProvider, "Stratis EVM");
                VSUtil.LogInfo("Stratis EVM", "Installing vscode-solidity language server...");
                var output = await ThreadHelper.JoinableTaskFactory.RunAsync(InstallVSCodeSolidityLanguageServerAsync, JoinableTaskCreationOptions.LongRunning);
                if (CheckRunCmdOutput(output, "Run `npm audit` for details."))
                {
                    VSUtil.LogInfo("Stratis EVM", "vscode-solidity language server installed.");
                }
                else
                {
                    VSUtil.LogError("Stratis EVM", "Could not install vscode-solidity language server.");
                    return null;
                }
            }
            var solution = (IVsSolution)await ServiceProvider.GetGlobalServiceAsync(typeof(SVsSolution));
            solution.GetSolutionInfo(out var dir, out var f, out var d);
            Info("Solution dir is {d}", dir);
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
            string message = "vscode-solidity language server failed to initialize.";
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
                try
                {
                    var resp = await sendRequest(methodParam);
                    Info("Request {req} {param}: {resp}", methodName, methodParam?.ToString() ?? "", resp?.ToString() ?? "(null)");
                    if (resp != null)
                    {
                        if (methodName == "textDocument/hover")
                        {
                            if (resp.Root != null && resp.Root["contents"] != null && resp.Root["contents"]["kind"] != null && resp.Root["contents"]["kind"].Value<string>() == "markdown")
                            {
                                Info("Replace hover markup contents with plaintext.");
                                resp.Root["contents"]["kind"] = JValue.CreateString("plaintext");
                                if (resp.Root["contents"]["value"] != null)
                                {
                                    resp.Root["contents"]["value"] = JValue.CreateString(resp.Root["contents"]["value"].Value<string>().Replace("### ", "").Replace("#", ""));
                                }
                                else
                                {
                                    resp.Root["contents"]["value"] = JValue.CreateString("");
                                }
                            }
                        }
                        else if (methodName == "textDocument/completion")
                        {
                            if (resp.Root.Type == JTokenType.Array && resp.Root.HasValues)
                            {
                                foreach (var f in resp.Root)
                                {
                                    if (f != null && f["documentation"] != null && f["documentation"]["kind"] != null && f["documentation"]["value"] != null && f["documentation"]["kind"].Value<string>() == "markdown")
                                    {
                                        Info("Replace completion markup contents with plaintext.");
                                        f["documentation"]["kind"] = JValue.CreateString("plaintext");
                                        f["documentation"]["value"] = JValue.CreateString(f["documentation"]["value"].Value<string>().Replace("### ", "").Replace("#", ""));
                                    }
                                }
                            }

                        }
                        return resp;
                    }
                    else
                    {
                        Info("resp is null");
                        return JObject.FromObject(new { contents = new { kind = "plaintext", value = "" } });
                    }
                }
                catch (Exception ex)
                {
                    Error(ex, "Exception thrown handling request {m}.", methodName);
                    return JObject.FromObject(new { contents = new { kind = "plaintext", value = "" } });
                }
            }
        }
        #endregion

        #region Fields
        internal System.Diagnostics.Process serverProcess;
        #endregion
    }




}
