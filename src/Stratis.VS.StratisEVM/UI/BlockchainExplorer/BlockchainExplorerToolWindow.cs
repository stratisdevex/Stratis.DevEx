using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Windows.Input;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Wpf.Ui.Controls;

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
    [Guid("4505b590-ce1c-436b-8fa4-0543c98b90ef")]
    public class BlockchainExplorerToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BlockchainExplorerToolWindow"/> class.
        /// </summary>
        public BlockchainExplorerToolWindow() : base(StratisEVMPackage.Instance)
        {
            this.Caption = "Blockchain Explorer";
            this.BitmapImageMoniker = KnownMonikers.CloudServer;
            this.ToolBar = new CommandID(StratisEVMPackageIds.BlockchainExplorerGuid, StratisEVMPackageIds.BlockchainExplorerTWindowId);
            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            var _ = new Card(); // Bug workaround, see https://github.com/microsoft/XamlBehaviorsWpf/issues/86
            var control = new BlockchainExplorerToolWindowControl();
            control.window = this;
            this.Content = control;
        }

        protected override void OnClose()
        {
            base.OnClose();
        }

        protected override void OnInfoBarClosed(IVsInfoBarUIElement infoBarUI, IVsInfoBar infoBar)
        {
            base.OnInfoBarClosed(infoBarUI, infoBar);
        }
    }
}
