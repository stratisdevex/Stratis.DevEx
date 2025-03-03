using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Input;
using System.Windows.Data;

using Hardcodet.Wpf.GenericTreeView;

using Stratis.VS.StratisEVM.UI.ViewModel;
using System.Windows.Controls;
using Stratis.DevEx;
using System.Windows.Media.Imaging;

namespace Stratis.VS.StratisEVM.UI
{
    public class BlockchainExplorerTree : TreeViewBase<BlockchainInfo>
    {
        public override string GetItemKey(BlockchainInfo item) => (item.Parent?.Name) ?? "Root" + "_" + item.Kind + "_" + item.Name;

        public override ICollection<BlockchainInfo> GetChildItems(BlockchainInfo parent) => parent.Children;

        public override BlockchainInfo GetParentItem(BlockchainInfo item) => item.Parent;

        public static RoutedCommand NewNetworkCmd = new RoutedCommand();

        public static BitmapImage FolderClosedIcon { get; } = new BitmapImage(new Uri(Runtime.AssemblyLocation.CombinePath("Images", "FolderClosed.png")));

        public static BitmapImage FolderOpenIcon { get; } = new BitmapImage(new Uri(Runtime.AssemblyLocation.CombinePath("Images", "FolderOpen.png")));

        public static BitmapImage FolderSelectedIcon { get; } = new BitmapImage(new Uri(Runtime.AssemblyLocation.CombinePath("Images", "FolderOpen.png")));

        public static BitmapImage NetworkIcon { get; } = new BitmapImage(new Uri(Runtime.AssemblyLocation.CombinePath("Images", "BlockChainNetwork.png")));
    }

    [ValueConversion(typeof(TreeViewItem), typeof(String))]
    public class BlockchainInfoKindConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null) return "";
            var t = (TreeViewItem)value;
            if (t.HasHeader)
            {
                var bi = t.Header as BlockchainInfo;
                return bi.Kind.ToString();
            }
            else
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => DependencyProperty.UnsetValue;
        
    }
}
