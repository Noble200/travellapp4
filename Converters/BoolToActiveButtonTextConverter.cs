using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Allva.Desktop.Converters;

public class BoolToActiveButtonTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive)
        {
            return isActive ? "🔴 Desactivar" : "✅ Activar";
        }
        return "Toggle";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}