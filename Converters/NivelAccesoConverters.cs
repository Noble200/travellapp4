using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Allva.Desktop.Converters;

/// <summary>
/// Convierte nivel de acceso (1-4) a índice de ComboBox (0-3)
/// </summary>
public class NivelToIndexConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int nivel)
        {
            return nivel - 1; // Nivel 1 = Index 0, Nivel 2 = Index 1, etc.
        }
        return 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index)
        {
            return index + 1; // Index 0 = Nivel 1, Index 1 = Nivel 2, etc.
        }
        return 1;
    }
}

/// <summary>
/// Determina si el nivel es menor a 3 (para habilitar checkboxes de módulos)
/// </summary>
public class NivelMenorA3Converter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int nivel)
        {
            return nivel < 3;
        }
        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Determina si el nivel es mayor o igual a 3
/// </summary>
public class NivelMayorIgual3Converter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int nivel)
        {
            return nivel >= 3;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Determina si el nivel es igual a 4 (Super Admin)
/// </summary>
public class NivelIgual4Converter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int nivel)
        {
            return nivel == 4;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}