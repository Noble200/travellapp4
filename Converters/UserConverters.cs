using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Allva.Desktop.Converters;

/// <summary>
/// Convierte un booleano a un color (para mensajes de éxito/error en usuarios)
/// </summary>
public class BoolToSuccessErrorColorConverter : IValueConverter
{
    public static readonly BoolToSuccessErrorColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool esExito)
        {
            return esExito 
                ? new SolidColorBrush(Color.Parse("#28a745")) // Verde para éxito
                : new SolidColorBrush(Color.Parse("#dc3545")); // Rojo para error
        }

        return new SolidColorBrush(Colors.Gray);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convierte un booleano de estado a texto de botón (Activar/Desactivar)
/// </summary>
public class BoolToUserStatusButtonTextConverter : IValueConverter
{
    public static readonly BoolToUserStatusButtonTextConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool activo)
        {
            return activo ? "⏸️ Desactivar" : "✅ Activar";
        }

        return "Estado";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convierte un booleano de flotante a texto informativo
/// </summary>
public class BoolToUserTypeInfoConverter : IValueConverter
{
    public static readonly BoolToUserTypeInfoConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool esFlotante)
        {
            return esFlotante 
                ? "ℹ️ Usuario flotante: puede trabajar en múltiples locales" 
                : "ℹ️ Usuario fijo: solo puede trabajar en un local";
        }

        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convierte un booleano de modo edición a texto del botón guardar
/// </summary>
public class BoolToSaveUserButtonTextConverter : IValueConverter
{
    public static readonly BoolToSaveUserButtonTextConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool modoEdicion)
        {
            return modoEdicion ? "💾 Actualizar Usuario" : "✅ Crear Usuario";
        }

        return "Guardar";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}