using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ChausseeNeuve.Domain.Models;

namespace UI_ChausseeNeuve.Converters
{
    public class EqualityConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, CultureInfo c) => Equals(v, p);
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => (v is bool b && b) ? p! : System.Windows.Data.Binding.DoNothing!;
    }

    public class ThicknessToHeightMulti : IMultiValueConverter
    {
        public object Convert(object[] values, Type t, object p, CultureInfo c)
        {
            // values[0] = Thickness_m (double)
            // values[1] = DepthScale (double)
            // values[2] = Role (LayerRole) - optional
            double thick = values.Length > 0 && values[0] is double d ? d : 0;
            double scale = values.Length > 1 && values[1] is double s ? s : 650;
            // If role present and is Plateforme, force a small constant height
            if (values.Length > 2 && values[2] is LayerRole role && role == LayerRole.Plateforme) return 22.0;
            return Math.Max(2, thick * scale);
        }
        public object[] ConvertBack(object v, Type[] ts, object p, CultureInfo c) => throw new NotImplementedException();
    }

    public class RoleToBrushConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, CultureInfo c)
        {
            if (v is LayerRole r)
            {
                return r switch
                {
                    LayerRole.Roulement => (SolidColorBrush)new BrushConverter().ConvertFromString("#0B5C8E")!,
                    LayerRole.Base => (SolidColorBrush)new BrushConverter().ConvertFromString("#D95A4E")!,
                    LayerRole.Fondation => (SolidColorBrush)new BrushConverter().ConvertFromString("#E7B567")!,
                    LayerRole.Plateforme => (SolidColorBrush)new BrushConverter().ConvertFromString("#7A4B2E")!,
                    _ => System.Windows.Media.Brushes.LightGray
                };
            }
            return System.Windows.Media.Brushes.LightGray;
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => System.Windows.Data.Binding.DoNothing!;
    }

    // New: map MaterialFamily to brushes
    public class MaterialToBrushConverter : IValueConverter
    {
        private static SolidColorBrush FromHex(string hex)
        {
            var conv = new BrushConverter();
            var b = conv.ConvertFromString(hex) as SolidColorBrush;
            return b ?? System.Windows.Media.Brushes.LightGray;
        }

        public object Convert(object v, Type t, object p, CultureInfo c)
        {
            if (v is MaterialFamily mf)
            {
                switch (mf)
                {
                    case MaterialFamily.BetonBitumineux:
                        // #000000 - Black (unchanged)
                        return System.Windows.Media.Brushes.Black;
                    case MaterialFamily.GNT:
                        // #E4B99F - Beige / sandy (updated per client)
                        return FromHex("#E4B99F");
                    case MaterialFamily.MTLH:
                        // #D3D3D3 - LightGray (modified per client)
                        return FromHex("#D3D3D3");
                    case MaterialFamily.BetonCiment:
                        // #A9A9A9 - DarkGray / DimGray (modified per client)
                        return FromHex("#A9A9A9");
                    case MaterialFamily.Bibliotheque:
                        // #BEBEBE - Neutral Gray (unchanged)
                        return FromHex("#BEBEBE");
                    default:
                        return System.Windows.Media.Brushes.LightGray;
                }
            }
            return System.Windows.Media.Brushes.LightGray;
        }

        public object ConvertBack(object v, Type t, object p, CultureInfo c) => System.Windows.Data.Binding.DoNothing!;
    }

    // Converter to return the DescriptionAttribute of an enum value if present, otherwise ToString()
    public class EnumDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            var type = value.GetType();
            if (!type.IsEnum) return value.ToString() ?? string.Empty;
            var name = System.Enum.GetName(type, value);
            if (name == null) return value.ToString() ?? string.Empty;
            var field = type.GetField(name);
            if (field == null) return name;
            var attr = Attribute.GetCustomAttribute(field, typeof(System.ComponentModel.DescriptionAttribute)) as System.ComponentModel.DescriptionAttribute;
            return attr?.Description ?? name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Not used in one-way display bindings; attempt to parse if needed
            if (targetType.IsEnum && value is string s)
            {
                foreach (var name in System.Enum.GetNames(targetType))
                {
                    var field = targetType.GetField(name);
                    var attr = Attribute.GetCustomAttribute(field!, typeof(System.ComponentModel.DescriptionAttribute)) as System.ComponentModel.DescriptionAttribute;
                    if ((attr != null && attr.Description == s) || name == s)
                        return System.Enum.Parse(targetType, name);
                }
            }
            return System.Windows.Data.Binding.DoNothing!;
        }
    }

    // Converter for boolean to Visibility
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            }
            return System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Windows.Visibility visibility)
            {
                return visibility == System.Windows.Visibility.Visible;
            }
            return false;
        }
    }
}
