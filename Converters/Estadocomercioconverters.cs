using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Allva.Desktop.Converters;

/// <summary>
/// Convierte bool a color específico para estados de locales
/// Verde para activo, Rojo para inactivo
/// </summary>
public class EstadoActivoColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool activo)
        {
            return new SolidColorBrush(activo ? Color.Parse("#28a745") : Color.Parse("#dc3545"));
        }
        return new SolidColorBrush(Color.Parse("#6c757d"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convierte bool a texto de estado en mayúsculas
/// True = "ACTIVO", False = "INACTIVO"
/// </summary>
public class EstadoTextoMayusculaConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool activo)
        {
            return activo ? "ACTIVO" : "INACTIVO";
        }
        return "DESCONOCIDO";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}