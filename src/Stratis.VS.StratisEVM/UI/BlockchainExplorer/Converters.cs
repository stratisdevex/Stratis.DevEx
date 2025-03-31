using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Data;

namespace Stratis.VS.StratisEVM.UI
{
    [ValueConversion(typeof(TreeViewItem), typeof(String))]
    public class TreeViewItemKindConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null) return "";
            var t = (TreeViewItem)value;
            if (t.HasHeader)
            {
                var bi = t.Header as ViewModel.BlockchainInfo;
                if (bi is null)
                {
                    return "";
                }
                else
                {
                    return bi.Kind.ToString();
                }
            }
            else
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => DependencyProperty.UnsetValue;

    }

    [ValueConversion(typeof(TreeViewItem), typeof(String))]
    public class TreeViewItemNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null) return "";
            var t = (TreeViewItem)value;
            if (t.HasHeader)
            {
                var bi = t.Header as ViewModel.BlockchainInfo;
                if (bi is null)
                {
                    return "";
                }
                else
                {
                    return bi.Kind.ToString() + "_" + bi.Name;
                }
            }
            else
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => DependencyProperty.UnsetValue;

    }

    [ValueConversion(typeof(ViewModel.BlockchainInfo), typeof(String))]
    public class BlockchainInfoKindConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null) return "";
            var t =  (ViewModel.BlockchainInfo) value;
            if (t != null)
            {
                return t.Kind.ToString();
            }
            else
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => DependencyProperty.UnsetValue;

    }

    [ValueConversion(typeof(bool), typeof(bool))]
    public class NegationConvertor : IValueConverter

    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                Boolean condition = (Boolean)value;
                return !condition;
            }


            else


                return value;


        }



        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)


        {


            if (value != null)


            {


                Boolean condition = (Boolean)value;



                return !condition;


            }


            else


                return value;


        }


    }

}
