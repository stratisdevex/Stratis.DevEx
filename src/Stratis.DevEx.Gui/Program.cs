using System;
using System.Linq;
using System.Threading.Tasks;

using CommandLine;
using CommandLine.Text;

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

            PipeServer = new PipeServer<Message>("stratis_devexgui");
            PipeServer.ClientConnected += (sender, e) => Info("Client connected...");

            if (!args.Contains("--no-gui"))
            {
                Info("Starting GUI...");
                GuiApp = new GuiApp(Eto.Platform.Detect);
                GuiApp.Initialized += (sender, e) => GuiApp.AsyncInvoke(async () => await PipeServer.StartAsync());
                GuiApp.Terminating += (sender, e) => PipeServer.Dispose();
                GuiApp.Run(new MainForm());
            }
            else
            {
                PipeServer.StartAsync().Wait();
                Console.CancelKeyPress += (sender, e) =>
                {
                    Info("Shutting down...");
                    PipeServer.Dispose();
                    Environment.Exit(0);
                };
                Info("Not starting GUI. Press Ctrl-C to exit...");
                while (true);
            }

            
            
        }
        #endregion

        #endregion

        #region Properties
        public static GuiApp? GuiApp { get; private set; }
        public static PipeServer<Message>? PipeServer { get; private set; }
        #endregion
    }
}
