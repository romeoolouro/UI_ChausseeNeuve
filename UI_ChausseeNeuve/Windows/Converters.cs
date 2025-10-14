using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace UI_ChausseeNeuve.Windows
{
    public class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isInterface = value is bool b && b;
            var param = parameter as string;
            if (isInterface)
            {
                return new SolidColorBrush(Color.FromRgb(0xF0, 0xF7, 0xFF)); // light blue for interface rows
            }
            return new SolidColorBrush(Color.FromRgb(0xF7, 0xF7, 0xF7)); // light gray for layers
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}
