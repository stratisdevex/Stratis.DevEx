using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using _MessagePack = Stratis.DevEx.Pipes.MessagePack;
using Stratis.SmartContracts.TestChain;
using Stratis.DevEx.Pipes;
using Stratis.DevEx.Gui.IO;

namespace Stratis.DevEx.TestChain
{
    public class Program : Runtime
    {
        #region Methods
        static Program()
        {
            AppDomain.CurrentDomain.UnhandledException += Program_UnhandledException;
        }

        #region Entry-point
        [STAThread]
        public static void Main(string[] args)
        {
            Initialize("Stratis.DevEx.TestChain", "APP", true);
            Console.CancelKeyPress += (sender, e) => Shutdown();
            if ((args.Contains("--debug") || args.Contains("-d")) && !GlobalSetting("General", "Debug", false))
            {
                GlobalConfig["General"]["Debug"].SetValue(true);
                DebugEnabled = true;
                Logger.SetLogLevelDebug();
                Info("Debug mode enabled.");
            }
            var pc = Stratis.DevEx.Gui.IO.Gui.CreatePipeClient("stratis_devex");
            if (pc is null)
            {
                Error("Could not create pipe client for pipe stratis_devex");
                Shutdown(1);
            }
            else
            {
                PipeClient = pc;
            }
            PipeServer = new PipeServer<_MessagePack>("stratis_testchain") { WaitFreePipe = true };
            PipeServer.ClientConnected += (sender, e) => Info("Client connected...");
            PipeServer.ExceptionOccurred += (sender, e) => Error(e.Exception, "Exception occurred in pipe server.");
            PipeServer.StartAsync().Wait();
            PipeServer.MessageReceived += (sender, e) => ReadMessage(e.Message);
            
            TestChain = new SmartContracts.TestChain.TestChain(DebugEnabled);
            var i = Task.Run(TestChain.Initialize);
            //Info("here");
            while (true)
            {
                System.Threading.Thread.Sleep(100);
                if (TestChain.NodeState.All(s => s == Bitcoin.IntegrationTests.Common.EnvironmentMockUpHelpers.CoreNodeState.Running) && TestChain.Initialized)
                {
                    Shutdown();
                }
            }

        }
        #endregion

        public static void ReadMessage(Stratis.DevEx.Pipes.MessagePack m)
        {
            var pc = PipeClient ?? throw new Exception("The pipe client is not initialized.");
            switch (m.Type)
            {
                case MessageType.TESTCHAIN_PING:
                    if (TestChain is not null && TestChain.Initialized)
                    {
                        Gui.IO.Gui.ReconnectIfDisconnected(pc);
                    }
                    pc.WriteAsync(new _MessagePack() { Type = MessageType.TESTCHAIN_PONG }).Wait();
                    break;
                default:
                    Error("Can't read message type {mt}.", m.Type);
                    break;
            }
        }
        public static void Shutdown(int exitCode = 0)
        {
            Info("Shutting down...");
            if (PipeServer is not null)
            {
                PipeServer.Dispose();
            }
            if (TestChain is not null)
            {
                TestChain.Dispose();
            }
            Info("Shutdown complete.");
            Environment.Exit(exitCode);
        }
        #endregion

        #region Properties
        public static PipeServer<Stratis.DevEx.Pipes.MessagePack>? PipeServer { get; private set; }

        public static PipeClient<Stratis.DevEx.Pipes.MessagePack> PipeClient { get; private set; } = new PipeClient<_MessagePack>("stratis_devex"); 
        
        public static ConcurrentQueue<Stratis.DevEx.Pipes.MessagePack> Messages { get; private set; } = new ConcurrentQueue<Stratis.DevEx.Pipes.MessagePack>();

        public static Stratis.SmartContracts.TestChain.TestChain? TestChain { get; private set; }
        #endregion

        #region Event handlers
        private static void Program_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Error((Exception) e.ExceptionObject, "Unhandled exception occurred. Shutting down.");
            if (PipeServer is not null)
            {
                PipeServer.Dispose();
            }
            if (TestChain is not null)
            {
                TestChain.Dispose();
            }
            Environment.Exit(-1);
        }
        #endregion
    }
}
