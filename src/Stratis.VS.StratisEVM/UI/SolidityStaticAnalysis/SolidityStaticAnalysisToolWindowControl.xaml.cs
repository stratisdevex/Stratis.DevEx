using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows;
using System.Windows.Controls;

using Microsoft.VisualStudio.Shell;
using EnvDTE;
using Stratis.DevEx;
using Stratis.VS.StratisEVM.UI.ViewModel;

namespace Stratis.VS.StratisEVM.UI
{
    /// <summary>
    /// Interaction logic for SolidityAnalysisToolWindowControl.
    /// </summary>
    public partial class SolidityStaticAnalysisToolWindowControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SolidityStaticAnalysisToolWindowControl"/> class.
        /// </summary>
        public SolidityStaticAnalysisToolWindowControl()
        {
            this.InitializeComponent();
        }

        
        internal ToolWindowPane window;

        internal static EnvDTE.Project selectedProject;
        internal static string selectedFilePath;

       
        
        public void AnalyzeProjectFileItem(ProjectItem item, SlitherAnalysis analysis)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
        }

        private SolidityStaticAnalysisInfo GetSelectedItem(object sender)
        {
            var window = (SolidityStaticAnalysisToolWindowControl)sender;
            var tree = window.SolidityStaticAnalysisTree;
            return tree.SelectedItem;
        }

    }
}