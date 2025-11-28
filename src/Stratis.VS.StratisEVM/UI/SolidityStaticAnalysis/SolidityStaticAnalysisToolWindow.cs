using EnvDTE;
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
    [Guid("4f52fe4e-7335-406c-ab70-135b601790b1")]
    public class SolidityStaticAnalysisToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SolidityStaticAnalysisToolWindow"/> class.
        /// </summary>
        public SolidityStaticAnalysisToolWindow() : base(null)
        {
            this.Caption = "Solidity Static Analysis";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            var c  = new SolidityStaticAnalysisToolWindowControl();
            c.window = this;
            this.Content = c;   
            this.control = c;
        }

        public SolidityStaticAnalysisToolWindowControl control;
    }
}
