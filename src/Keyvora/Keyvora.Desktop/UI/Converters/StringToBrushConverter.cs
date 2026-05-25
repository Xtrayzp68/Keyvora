namespace Keyvora.Desktop.UI.Converters;

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

public sealed class StringToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrEmpty(hex))
        {
            try
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            }
            catch
            {
                return new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x2D));
            }
        }
        return new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x2D));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
