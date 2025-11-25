using Microsoft.VisualStudio.Shell;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;

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

        private void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}