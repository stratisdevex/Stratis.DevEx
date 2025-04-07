using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Data;
using Wpf.Ui.Controls;

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

    // This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.


    public class EnumToBoolConverter<TEnum> : IValueConverter
        where TEnum : Enum
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is TEnum valueEnum))
            {
                throw new ArgumentException($"{nameof(value)} is not type: {typeof(TEnum)}");
            }

            if (!(parameter is TEnum parameterEnum))
            {
                throw new ArgumentException($"{nameof(parameter)} is not type: {typeof(TEnum)}");
            }

            return EqualityComparer<TEnum>.Default.Equals(valueEnum, parameterEnum);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ContentDialogButtonEnumToBoolConverter : EnumToBoolConverter<ContentDialogButton> { }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is true ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
