using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace UI_ChausseeNeuve.Views
{
    public class StatutCellBgConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string tag && tag == "StatutSystem")
                return new SolidColorBrush(Color.FromRgb(31, 31, 31)); // #FF1F1F1F
            return new SolidColorBrush(Color.FromRgb(45, 45, 48)); // #FF2D2D30
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
