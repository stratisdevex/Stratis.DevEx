using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Msagl.Drawing;
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
            : base(platform) {}
        #endregion

        #region Overriden members
        
        #endregion

        #region Methods
        public void ReadMessage(CompilationMessage m)
        {
            var mainform = (MainForm)this.MainForm;
            mainform.ReadMessage(m);
        }

        public void ReadMessage(ControlFlowGraphMessage m)
        {
            var mainform = (MainForm)this.MainForm;
            mainform.ReadMessage(m);
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
