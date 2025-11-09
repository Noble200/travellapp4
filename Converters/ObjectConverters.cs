using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Allva.Desktop.Converters;

/// <summary>
/// Conversores estáticos para objetos genéricos
/// Proporciona conversiones comunes para uso en bindings XAML
/// </summary>
public static class ObjectConverters
{
    /// <summary>
    /// Convierte un bool a texto basado en un parámetro con formato "TextoTrue|TextoFalse"
    /// Ejemplo: ConverterParameter='Actualizar|Crear'
    /// </summary>
    public static readonly IValueConverter IsTrue = new BoolToConditionalTextConverter();

    /// <summary>
    /// Compara un valor con un parámetro y devuelve true si son iguales
    /// Útil para activar estilos CSS basados en valores
    /// </summary>
    public static readonly IValueConverter Equal = new ObjectEqualityConverter();
}

/// <summary>
/// Conversor que compara un booleano y devuelve texto condicional
/// </summary>
public class BoolToConditionalTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string param)
        {
            var parts = param.Split('|');
            if (parts.Length == 2)
            {
                return boolValue ? parts[0] : parts[1];
            }
        }
        return value?.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("BoolToConditionalTextConverter does not support ConvertBack");
    }
}

/// <summary>
/// Conversor que compara dos objetos y devuelve true si son iguales
/// </summary>
public class ObjectEqualityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null && parameter == null) return true;
        if (value == null || parameter == null) return false;
        
        return value.ToString() == parameter.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("ObjectEqualityConverter does not support ConvertBack");
    }
}