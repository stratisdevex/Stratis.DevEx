using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace Stratis.VS.StratisEVM.UI
{ 
    /// <summary>
    /// Interaction logic for BlockchainExplorerAddNetwork.xaml
    /// </summary>
    public partial class BlockchainExplorerDialog : ContentDialog
    {
        public BlockchainExplorerDialog(ContentPresenter host) : base(host)
        {
            InitializeComponent();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (availableSize.Width > 0 && availableSize.Height > 0)
            {
                return base.MeasureOverride(availableSize);
            }
            else
            {
                return new Size(0, 0);  
            }

        }
    }
}
