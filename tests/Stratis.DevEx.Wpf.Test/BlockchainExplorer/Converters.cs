using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Stratis.VS.StratisEVM.UI
{
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
