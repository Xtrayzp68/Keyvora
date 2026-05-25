namespace Keyvora.Desktop.UI.Converters;

using System;
using System.Globalization;
using System.Windows.Data;

public sealed class ActionTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            "builtin.keyboard" => "⌨",
            "builtin.launch" => "▶",
            "builtin.openfile" => "📂",
            "builtin.spotify" => "🎵",
            "builtin.macro" => "⚡",
            "builtin.text" => "Aa",
            _ => "⬜"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
