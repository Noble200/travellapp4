using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Allva.Desktop.Converters;

/// <summary>
/// Conversores estáticos para valores enteros (int)
/// Proporciona conversiones comunes de int a bool para uso en bindings XAML
/// </summary>
public static class IntConverters
{
    /// <summary>
    /// Convierte un entero a booleano: true si el valor es cero
    /// Útil para mostrar elementos cuando una colección está vacía
    /// </summary>
    /// <example>
    /// IsVisible="{Binding Items.Count, Converter={x:Static IntConverters.IsZero}}"
    /// </example>
    public static readonly IValueConverter IsZero = 
        new FuncValueConverter<int, bool>(value => value == 0);
    
    /// <summary>
    /// Convierte un entero a booleano: true si el valor es mayor que cero
    /// Útil para mostrar elementos cuando hay items en una colección
    /// </summary>
    /// <example>
    /// IsVisible="{Binding Items.Count, Converter={x:Static IntConverters.IsGreaterThanZero}}"
    /// </example>
    public static readonly IValueConverter IsGreaterThanZero = 
        new FuncValueConverter<int, bool>(value => value > 0);
    
    /// <summary>
    /// Convierte un entero a booleano: true si el valor es menor que cero
    /// </summary>
    public static readonly IValueConverter IsLessThanZero = 
        new FuncValueConverter<int, bool>(value => value < 0);
    
    /// <summary>
    /// Convierte un entero a booleano: true si el valor es mayor o igual que cero
    /// </summary>
    public static readonly IValueConverter IsGreaterThanOrEqualToZero = 
        new FuncValueConverter<int, bool>(value => value >= 0);
    
    /// <summary>
    /// Convierte un entero a booleano: true si el valor es menor o igual que cero
    /// </summary>
    public static readonly IValueConverter IsLessThanOrEqualToZero = 
        new FuncValueConverter<int, bool>(value => value <= 0);
    
    /// <summary>
    /// Convierte un entero a booleano invertido: false si es cero, true si no es cero
    /// </summary>
    public static readonly IValueConverter IsNotZero = 
        new FuncValueConverter<int, bool>(value => value != 0);
}

/// <summary>
/// Conversor de función genérico para crear conversores simples inline
/// Usado internamente por IntConverters
/// </summary>
/// <typeparam name="TIn">Tipo de entrada</typeparam>
/// <typeparam name="TOut">Tipo de salida</typeparam>
public class FuncValueConverter<TIn, TOut> : IValueConverter
{
    private readonly Func<TIn, TOut> _convert;
    private readonly Func<TOut, TIn>? _convertBack;

    /// <summary>
    /// Constructor con solo función de conversión (sin convertBack)
    /// </summary>
    public FuncValueConverter(Func<TIn, TOut> convert)
    {
        _convert = convert ?? throw new ArgumentNullException(nameof(convert));
    }

    /// <summary>
    /// Constructor con funciones de conversión bidireccional
    /// </summary>
    public FuncValueConverter(Func<TIn, TOut> convert, Func<TOut, TIn> convertBack)
    {
        _convert = convert ?? throw new ArgumentNullException(nameof(convert));
        _convertBack = convertBack ?? throw new ArgumentNullException(nameof(convertBack));
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TIn typedValue)
        {
            return _convert(typedValue);
        }
        return default(TOut);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (_convertBack == null)
        {
            throw new NotSupportedException("ConvertBack is not supported by this converter");
        }

        if (value is TOut typedValue)
        {
            return _convertBack(typedValue);
        }
        return default(TIn);
    }
}