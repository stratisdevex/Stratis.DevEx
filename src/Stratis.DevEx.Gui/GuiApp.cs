using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Eto;
using Eto.Forms;

using Stratis.DevEx.Pipes;

namespace Stratis.DevEx.Gui
{
    public class GuiApp : Application
    {
        #region Constructors
        public GuiApp()
            : this(Platform.Detect) {}

        public GuiApp(Platform platform)
            : base(platform)
        {
        }
        #endregion

        #region Overriden members
        protected override void OnInitialized(EventArgs e)
        {
            this.MainForm = new MainForm();
            base.OnInitialized(e);
        }
        #endregion

        #region Methods
        public void ReadMessage(MessagePack m)
        {
            switch (m.Type)
            {
                case MessageType.COMPILATION_MESSAGE:
                    var cm = MessageUtils.Deserialize<CompilationMessage>(m.MessageBytes);
                    Info("Message received: {msg}", MessageUtils.PrettyPrint(cm));
                    ReadMessage(cm);
                    break;

                case MessageType.CONTROL_FLOW_GRAPH_MESSAGE:
                    var cfgm = MessageUtils.Deserialize<ControlFlowGraphMessage>(m.MessageBytes);
                    Info("Message received: {msg}", MessageUtils.PrettyPrint(cfgm));
                    ReadMessage(cfgm);
                    break;
            }
        }
        
        public static void ReadMessage(CompilationMessage m)
        {

        }

        public static void ReadMessage(ControlFlowGraphMessage m)
        {

        }

        #region Logging
        [DebuggerStepThrough]
        public static void Info(string messageTemplate, params object[] args) => Runtime.Info(messageTemplate, args);

        [DebuggerStepThrough]
        public static void Debug(string messageTemplate, params object[] args) => Runtime.Debug(messageTemplate, args);

        [DebuggerStepThrough]
        public static void Error(string messageTemplate, params object[] args) => Runtime.Error(messageTemplate, args);

        [DebuggerStepThrough]
        public static void Error(Exception ex, string messageTemplate, params object[] args) => Runtime.Error(ex, messageTemplate, args);

        [DebuggerStepThrough]
        public static void Warn(string messageTemplate, params object[] args) => Runtime.Warn(messageTemplate, args);

        [DebuggerStepThrough]
        public static void Fatal(string messageTemplate, params object[] args) => Runtime.Fatal(messageTemplate, args);

        [DebuggerStepThrough]
        public static Logger.Op Begin(string messageTemplate, params object[] args) => Runtime.Begin(messageTemplate, args);
        #endregion

        #endregion
    }
}
