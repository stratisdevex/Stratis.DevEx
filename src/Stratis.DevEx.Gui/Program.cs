using System;
using System.Linq;
using System.Threading.Tasks;

using CommandLine;
using CommandLine.Text;

using Stratis.DevEx.GuiMessages;
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
                    Info("Debug mode enabled.");
                    GlobalConfig["General"]["Debug"].SetValue(true);
                    Logger.SetLogLevelDebug();
                }
            }

            PipeServer = new PipeServer<Message>("stratis-devexgui");
            PipeServer.ClientConnected += (sender, e) => Info("Client connected...");

            if (!args.Contains("--no-gui"))
            {
                Info("Starting GUI...");
                Application = new Application(Eto.Platform.Detect);
                Application.Initialized += (sender, e) => Application.AsyncInvoke(async () => await PipeServer.StartAsync());
                Application.Terminating += (sender, e) => PipeServer.Dispose();
                Application.Run(new MainForm());
            }
            else
            {
                Info("Not starting GUI. Press Ctrl-C to exit...");
                Console.CancelKeyPress += (sender, e) =>
                {
                    Info("Shutting down...");
                    PipeServer.Dispose();
                    Environment.Exit(0);
                };
                while (true);
            }

            
            
        }

        private static void PipeServer_ClientConnected(object sender, Pipes.Args.ConnectionEventArgs<Message> e)
        {
            throw new NotImplementedException();
        }
        #endregion

        #endregion

        #region Properties
        public static Application? Application { get; private set; }
        public static PipeServer<Message>? PipeServer { get; private set; }
        #endregion
    }
}
