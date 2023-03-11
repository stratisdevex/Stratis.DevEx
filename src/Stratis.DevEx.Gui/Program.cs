using System;
using System.Threading.Tasks;

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
            Application = new Application(Eto.Platform.Detect);
            PipeServer = new PipeServer<Message>("stratis-devexgui");
            Application.Initialized += (sender, e) => Application.AsyncInvoke(async () => await PipeServer.StartAsync());
            Application.Terminating += (sender, e) => PipeServer.Dispose();
            Application.Run(new MainForm());
        }
        #endregion

        #endregion

        #region Properties
        public static Application? Application { get; private set; }
        public static PipeServer<Message>? PipeServer { get; private set; }
        #endregion
    }
}
