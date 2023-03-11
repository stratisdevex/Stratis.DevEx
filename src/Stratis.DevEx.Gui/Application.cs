using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Eto;
using Eto.Forms;

namespace Stratis.DevEx.Gui
{
    public class Application : Eto.Forms.Application
    {
        #region Constructors
        public Application()
            : this(Platform.Detect) {}

        public Application(Platform platform)
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
    }
}
