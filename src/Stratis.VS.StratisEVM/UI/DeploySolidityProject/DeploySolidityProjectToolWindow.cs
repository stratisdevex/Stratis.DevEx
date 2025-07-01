using System;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.Shell;

namespace Stratis.VS.StratisEVM.UI
{
    [Guid("5813549f-de3e-46d9-aaf9-0d06109baa6f")]
    public class DeploySolidityProjectToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeploySolidityProjectToolWindow"/> class.
        /// </summary>
        public DeploySolidityProjectToolWindow() : base(StratisEVMPackage.Instance)
        {
            this.Caption = "Deploy Solidity Project";
            
            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.control = new DeploySolidityProjectToolWindowControl();
            this.Content = control;
        }

        public DeploySolidityProjectToolWindowControl control;        
    }

    
}
