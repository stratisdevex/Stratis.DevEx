using System.Windows.Controls;

using Microsoft.VisualStudio.Shell;

using Wpf.Ui.Controls;
using Hardcodet.Wpf.GenericTreeView;

using Stratis.VS.StratisEVM.UI.ViewModel;
using Stratis.DevEx;

namespace Stratis.VS.StratisEVM.UI
{
    public partial class SolidityStaticAnalysisToolWindowControl : UserControl
    {
        #region Constructor
        public SolidityStaticAnalysisToolWindowControl()
        {
            var _ = new Wpf.Ui.Markdown.Controls.MarkdownViewer();  
            this.InitializeComponent();
#if IS_VSIX
            VSTheme.WatchThemeChanges();
            instance = this;
#endif
        }
        #endregion

        #region Methods
        public void AnalyzeProjectFileItem(string filePath, string projectDir, SoliditySlitherAnalysis analysis)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var root = SolidityStaticAnalysisTree.RootItem;
            root.Data["Label"] = Runtime.GetWindowsRelativePath(filePath, projectDir); ;
            var vm = (SolidityStaticAnalysisViewModel)TryFindResource("StaticAnalysis");
            vm.ClearAnalysis();
            foreach (var d in analysis.results.detectors)
            {
                vm.AddDetectorResult(d);
            }
            SolidityStaticAnalysisTree.Refresh();
        }

        private SolidityStaticAnalysisInfo GetSelectedItem(object sender)
        {
            var window = (SolidityStaticAnalysisToolWindowControl)sender;
            var tree = window.SolidityStaticAnalysisTree;
            return tree.SelectedItem;
        }

        #region Event handlers
        private void OnSelectedItemChanged(object sender, RoutedTreeItemEventArgs<SolidityStaticAnalysisInfo> e)
        {
            if (sender is SolidityStaticAnalysisTree tree && tree.SelectedItem != null)
                if (tree.SelectedItem.Kind == SolidityStaticAnalysisInfoKind.Detector)
                {
                    StaticAnalysisMarkdownViewer.Markdown = (string)e.NewItem.Data["Markdown"];
                }
        }
        #endregion
        
        #endregion

        #region Fields
        internal ToolWindowPane window;
        internal SolidityStaticAnalysisToolWindowControl instance;
        #endregion

        
    }
}