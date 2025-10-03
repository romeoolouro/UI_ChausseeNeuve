using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace UI_ChausseeNeuve.Converters
{
    public class BooleanToBackgroundConverter : IValueConverter
    {
        public Brush TrueValue { get; set; } = Brushes.White;
        public Brush FalseValue { get; set; } = new SolidColorBrush(Color.FromRgb(245, 245, 245));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueValue : FalseValue;
            }
            return FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}