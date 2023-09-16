using System;
using System.Collections.Concurrent;
using System.Linq;

//using SharpConfig;
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
                Logger.SetLogLevelDebug();
                Info("Debug mode enabled.");
            }
            TestChain.Initialize();
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
        
        public static ConcurrentQueue<Message> Messages { get; private set; } = new ConcurrentQueue<Message>();

        public static Stratis.SmartContracts.TestChain.TestChain TestChain { get; private set; } = new SmartContracts.TestChain.TestChain();
        #endregion
    }
}
