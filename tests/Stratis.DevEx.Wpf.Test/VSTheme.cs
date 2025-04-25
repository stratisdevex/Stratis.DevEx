using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wpf.Ui;

namespace Stratis.VS.StratisEVM.UI
{
    public class VSTheme
    {
        public static object ToolWindowTextKey => Wpf.Ui.Markup.ThemeResource.TextFillColorPrimaryBrush.ToString();
        public static object ToolWindowBackgroundKey => Wpf.Ui.Markup.ThemeResource.ApplicationBackgroundBrush;

        public static void WatchThemeChanges()
        {
            
        }
    }
}
