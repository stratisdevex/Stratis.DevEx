using System;
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
        public void ReadMessage(Message m)
        {

        }
        #endregion
    }
}
