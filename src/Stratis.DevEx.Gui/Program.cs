using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using CommandLine;
using CommandLine.Text;

using SharpConfig;

using Stratis.DevEx.Pipes;

namespace Stratis.DevEx.Gui
{
    class Program : Runtime
    {
        #region Methods

        #region Entry-point
        [STAThread]
        static void Main(string[] args)
        {
            Initialize("Stratis.DevEx.Gui", "APP", true);
            if ((args.Contains("--debug") || args.Contains("-d")) && !GlobalSetting("General", "Debug", false))
            {
                GlobalConfig["General"]["Debug"].SetValue(true);
                Logger.SetLogLevelDebug();
                Info("Debug mode enabled.");                
            }

            PipeServer = new PipeServer<MessagePack>("stratis_devexgui") { WaitFreePipe = true };
            PipeServer.ClientConnected += (sender, e) => Info("Client connected...");
            PipeServer.ExceptionOccurred += (sender, e) => Error(e.Exception, "Exception occurred in pipe server.");

            //ParserResult<Options> result = new Parser().ParseArguments<Options>(args);
            if (!args.Contains("--no-gui"))
            {
                Info("Starting GUI...");
                GuiApp = new GuiApp(Eto.Platform.Detect);
                GuiApp.Initialized += (sender, e) => GuiApp.InvokeAsync(async () => await PipeServer.StartAsync());
                GuiApp.Terminating += (sender, e) => Shutdown();
                PipeServer.MessageReceived += (sender, e) => GuiApp.AsyncInvoke(() => ReadMessage(e.Message, GuiApp.ReadMessage, GuiApp.ReadMessage));
                WriteRunFile();
                GuiApp.Run(new MainForm());
            }
            else
            {
                Info("Not starting GUI...");
                PipeServer.StartAsync().Wait();
                PipeServer.MessageReceived += (sender, e) => ReadMessage(e.Message);
                WriteRunFile();
                Console.CancelKeyPress += (sender, e) => Shutdown();
                Info("Press Ctrl-C to exit...");
                while (true)
                {
                    System.Threading.Thread.Sleep(100);
                }
            }
        }
        #endregion

        public static void WriteRunFile()
        {
            var cfg = new Configuration();
            cfg.Add(new Section("Process"));
            cfg["Process"].Add(new Setting("ProcessId", System.Diagnostics.Process.GetCurrentProcess().Id));
            cfg.SaveToFile(RunFile);
            Info("Wrote run file {runfile}.", RunFile);
        }

        public static void ReadMessage(MessagePack m, Action<CompilationMessage>? cma = null, Action<ControlFlowGraphMessage>? cfgma = null)
        {
            switch (m.Type)
            {
                case MessageType.COMPILATION:
                    var cm = MessageUtils.Deserialize<CompilationMessage>(m.MessageBytes);
                    Info("Message received: \n{msg}", MessageUtils.PrettyPrint(cm));
                    cma?.Invoke(cm);
                    break;

                case MessageType.CONTROL_FLOW_GRAPH:
                    var cfgm = MessageUtils.Deserialize<ControlFlowGraphMessage>(m.MessageBytes);
                    Info("Message received: \n{msg}", MessageUtils.PrettyPrint(cfgm));
                    cfgma?.Invoke(cfgm);
                    break;
            }
        }
        public static void Shutdown()
        {
            Info("Shutting down...");
            if (PipeServer is not null)
            {
                PipeServer.Dispose();
            }
            if (File.Exists(RunFile))
            {
                File.Delete(RunFile);
            }
            Environment.Exit(0);
        }
        #endregion

        #region Properties
        public static GuiApp? GuiApp { get; private set; }
        public static PipeServer<MessagePack>? PipeServer { get; private set; }
        #endregion
    }
}
