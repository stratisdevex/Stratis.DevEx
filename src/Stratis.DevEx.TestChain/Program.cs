using System;
using System.Collections.Concurrent;
using System.Linq;

using _MessagePack = Stratis.DevEx.Pipes.MessagePack;
using Stratis.SmartContracts.TestChain;
using Stratis.DevEx.Pipes;

namespace Stratis.DevEx.TestChain
{
    public class Program : Runtime
    {
        #region Methods

        #region Entry-point
        [STAThread]
        public static void Main(string[] args)
        {
            Initialize("Stratis.DevEx.TestChain", "APP", true);
            if ((args.Contains("--debug") || args.Contains("-d")) && !GlobalSetting("General", "Debug", false))
            {
                GlobalConfig["General"]["Debug"].SetValue(true);
                DebugEnabled = true;
                Logger.SetLogLevelDebug();
                Info("Debug mode enabled.");
            }
            PipeServer = new PipeServer<_MessagePack>("stratis_testchain") { WaitFreePipe = true };
            PipeServer.ClientConnected += (sender, e) => Info("Client connected...");
            PipeServer.ExceptionOccurred += (sender, e) => Error(e.Exception, "Exception occurred in pipe server.");
            PipeServer.StartAsync().Wait();
            PipeServer.MessageReceived += (sender, e) => ReadMessage(e.Message);
            TestChain = new SmartContracts.TestChain.TestChain(DebugEnabled);
            TestChain.Initialize();
            
            var b = TestChain.builder.ConfigParameters;
            Info("here");
        }

        public static void ReadMessage(Stratis.DevEx.Pipes.MessagePack m)
        {
            //var m = 
        }
        #endregion

        #endregion

        #region Properties
       
        public static PipeServer<Stratis.DevEx.Pipes.MessagePack>? PipeServer { get; private set; }
        
        public static ConcurrentQueue<Stratis.DevEx.Pipes.MessagePack> Messages { get; private set; } = new ConcurrentQueue<Stratis.DevEx.Pipes.MessagePack>();

        public static Stratis.SmartContracts.TestChain.TestChain? TestChain { get; private set; } 
        #endregion
    }
}
