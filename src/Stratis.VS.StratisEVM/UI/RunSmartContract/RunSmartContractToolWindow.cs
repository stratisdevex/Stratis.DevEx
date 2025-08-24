using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

namespace Stratis.VS.StratisEVM.UI
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("a95ee9d3-7813-4138-b179-80fe62b95a67")]
    public class RunSmartContractToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RunSmartContractToolWindow"/> class.
        /// </summary>
        public RunSmartContractToolWindow() : base(null)
        {
            this.Caption = "Run Smart Contract";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            var c = new RunSmartContractToolWindowControl();
            c.window = this;
            this.Content = c;
        }
    }
}
