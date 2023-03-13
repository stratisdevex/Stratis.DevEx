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
            //ParserResult<Options> result = new Parser().ParseArguments<Options>(args);
            Runtime.Initialize("Stratis.DevEx.Gui", "APP", true);
            if (args.Contains("--debug") || args.Contains("-d"))
            {
                if (!GlobalSetting("General", "Debug", false))
                {
                    GlobalConfig["General"]["Debug"].SetValue(true);
                    Logger.SetLogLevelDebug();
                    Info("Debug mode enabled.");
                }
            }

            PipeServer = new PipeServer<Message>("stratis_devexgui") { WaitFreePipe = true };
            PipeServer.ClientConnected += (sender, e) => Info("Client connected...");
            PipeServer.ExceptionOccurred += (sender, e) => Error(e.Exception, "Exception occurred in pipe server.");
            WriteRunFile();

            if (!args.Contains("--no-gui"))
            {
                Info("Starting GUI...");
                GuiApp = new GuiApp(Eto.Platform.Detect);
                GuiApp.Initialized += (sender, e) => GuiApp.AsyncInvoke(async () => await PipeServer.StartAsync());
                GuiApp.Terminating += (sender, e) => Shutdown();
                PipeServer.MessageReceived += (sender, e) => GuiApp.Invoke(() =>
                {
                    Info("Message received...");
                    GuiApp.ReadMessage(e.Message);
                });
                GuiApp.Run(new MainForm());
            }
            else
            {
                PipeServer.MessageReceived += (sender, e) =>
                {
                    Info("Message received: {msg}", MessageUtils.PrettyPrint(e.Message));
                };
                PipeServer.StartAsync().Wait();
                Console.CancelKeyPress += (sender, e) => Shutdown();
                Info("Not starting GUI. Press Ctrl-C to exit...");
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
        public static PipeServer<Message>? PipeServer { get; private set; }
        #endregion
    }
}
