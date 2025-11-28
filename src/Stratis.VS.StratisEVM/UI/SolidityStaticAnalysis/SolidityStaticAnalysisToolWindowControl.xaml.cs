using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows;
using System.Windows.Controls;

using Microsoft.VisualStudio.Shell;
using EnvDTE;
using Stratis.DevEx;

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

        private void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {

        }

        public void AnalyzeProjectFileItem(ProjectItem item)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var project = item.ContainingProject;
            var projectdir = Path.GetDirectoryName(project.FileName);
            var filepath = item.FileNames[1];
            var outputDir = Path.Combine(Path.GetDirectoryName(project.FileName), project.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString());
            var a = SolidityCompiler.Analyze(Runtime.GetWindowsRelativePath(filepath, projectdir), projectdir, outputDir, "0.8.27");
        }
    }
}