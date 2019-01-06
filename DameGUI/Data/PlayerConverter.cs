using System;
using System.Windows.Data;

namespace DameGUI.Data
{
    public class PlayerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int idx = (int)value;

            if (idx % 2 == 0) { return "Bílý"; }
            return "Černý";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return "";
        }
    }
}