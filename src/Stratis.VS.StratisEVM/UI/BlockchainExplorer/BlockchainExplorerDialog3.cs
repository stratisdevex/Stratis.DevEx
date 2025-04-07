using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Wpf.Ui.Controls;

using System.Windows;

using Microsoft.VisualStudio.PlatformUI;



namespace Stratis.VS.StratisEVM.UI
{
    public class BlockchainExplorerDialog : ContentDialog
    {
        public BlockchainExplorerDialog(ContentPresenter host) : base(host) 
        {
           
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
