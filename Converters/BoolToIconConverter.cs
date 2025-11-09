using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Allva.Desktop.Converters;

/// <summary>
/// Convierte valores booleanos a iconos emoji
/// true = 🔓 (activo/desbloqueado)
/// false = 🔒 (inactivo/bloqueado)
/// </summary>
public class BoolToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? "🔓" : "🔒";
        }
        return "❓";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException("BoolToIconConverter no soporta ConvertBack");
    }
}