using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Stratis.DevEx;
using Stratis.VS.StratisEVM.UI.ViewModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

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
            var _ = new Wpf.Ui.Markdown.Controls.MarkdownViewer();  
            this.InitializeComponent();
        }

        
        internal ToolWindowPane window;

        internal static EnvDTE.Project selectedProject;
        internal static string selectedFilePath;

       
        
        public void AnalyzeProjectFileItem(string filePath, string projectDir, SlitherAnalysis analysis)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var root =  SolidityStaticAnalysisTree.RootItem;
            var vm = (SolidityStaticAnalysisViewModel)TryFindResource("StaticAnalysis");
            vm.ClearAnalysis();                      
            foreach (var d in analysis.results.detectors)
            {
                vm.AddDetectorResult(d);
            }            
        }

        private SolidityStaticAnalysisInfo GetSelectedItem(object sender)
        {
            var window = (SolidityStaticAnalysisToolWindowControl)sender;
            var tree = window.SolidityStaticAnalysisTree;
            return tree.SelectedItem;
        }

    }
}