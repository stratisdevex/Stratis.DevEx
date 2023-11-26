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

namespace MockLanguageExtension
{  
    [ContentType("foo")]
    [Export(typeof(ILanguageClient))]
    [RunOnContext(RunningContext.RunOnHost)]
    public class FooLanguageClient : Runtime, ILanguageClient, ILanguageClientCustomMessage2
    {
        
        static FooLanguageClient()
        {
            Initialize("Stratis.Editor.Solidity.LanguageClient", "CL");
        }
        public FooLanguageClient()
        {
            Instance = this;
            this.MiddleLayer = new FooMiddleLayer();
        }

        internal static FooLanguageClient Instance
        {
            get;
            set;
        }

        internal JsonRpc Rpc
        {
            get;
            set;
        }

        public event AsyncEventHandler<EventArgs> StartAsync;
        public event AsyncEventHandler<EventArgs> StopAsync;

        public string Name => "Foo Language Extension";

        public IEnumerable<string> ConfigurationSections
        {
            get
            {
                yield return "foo";
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

        public async Task<Connection> ActivateAsync(CancellationToken token)
        {
            //Debugger.Launch();
        
            ProcessStartInfo info = new ProcessStartInfo();
            //var programPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Server", @"LanguageServerWithUI.exe");
            var programPath = "nomicfoundation-solidity-language-server";
            info.FileName = programPath;
            info.WorkingDirectory = Path.GetDirectoryName(programPath);
            info.Arguments = "--pipe=sol";
            info.RedirectStandardInput = false;
            info.RedirectStandardOutput = false;
            info.UseShellExecute = true;
            //info.CreateNoWindow = true;
            //info.
            var pipeName = @"sol";
            //var stdOutPipeName = @"sol";

            var pipeAccessRule = new PipeAccessRule("Everyone", PipeAccessRights.ReadWrite, System.Security.AccessControl.AccessControlType.Allow);
            var pipeSecurity = new PipeSecurity();
            pipeSecurity.AddAccessRule(pipeAccessRule);

            var bufferSize = 256;
            //var readerPipe = new NamedPipeServerStream(stdInPipeName, PipeDirection.InOut, 4, PipeTransmissionMode.Message, PipeOptions.Asynchronous, bufferSize, bufferSize, pipeSecurity);
            //var writerPipe = new NamedPipeServerStream(stdOutPipeName, PipeDirection.InOut, 4, PipeTransmissionMode.Message, PipeOptions.Asynchronous, bufferSize, bufferSize, pipeSecurity);
            var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 4, PipeTransmissionMode.Message, PipeOptions.Asynchronous, 256, 256, pipeSecurity);
            var process = new System.Diagnostics.Process();
            process.StartInfo = info;

            if (process.Start())
            {
                Info("Started language server process {proc}", process.StartInfo.FileName);
                await pipe.WaitForConnectionAsync(token);
                //await writerPipe.WaitForConnectionAsync(token);

                return new Connection(pipe, pipe);
            }
            else
            {
                Info("Could not start language server process {proc}", process.StartInfo.FileName);
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
            Error("Language server failed to initialize.");
            string message = "Oh no! Foo Language Client failed to activate, now we can't test LSP! :(";
            string exception = initializationState.InitializationException?.ToString() ?? string.Empty;
            message = $"{message}\n {exception}";

            var failureContext = new InitializationFailureContext()
            {
                FailureMessage = message,
            };

            return Task.FromResult(failureContext);
        }

        internal class FooMiddleLayer : ILanguageClientMiddleLayer
        {
            public bool CanHandle(string methodName)
            {
                Info("Calling method {m}", methodName);
                //return methodName == Methods.TextDocumentCompletionName;
                return true;
            }

            public Task HandleNotificationAsync(string methodName, JToken methodParam, Func<JToken, Task> sendNotification)
            {
                Info("Notification {req} {param}.", methodName, methodParam);
                return Task.CompletedTask;
                //throw new NotImplementedException();
            }

            public async Task<JToken> HandleRequestAsync(string methodName, JToken methodParam, Func<JToken, Task<JToken>> sendRequest)
            {
                Info("Request {req} {param}.", methodName, methodParam);
                var result = await sendRequest(methodParam);
                return result;
            }
        }
    }
}
