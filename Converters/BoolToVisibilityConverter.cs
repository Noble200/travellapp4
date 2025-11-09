using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Allva.Desktop.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // Convierte bool a Visibility (ajusta según tus necesidades)
                // Si necesitas Visibility, puedes usar:
                // return boolValue ? Avalonia.Visibility.Visible : Avalonia.Visibility.Collapsed;
                // O si necesitas retornar directamente el bool:
                return boolValue;
            }
            return false; // o return Avalonia.Visibility.Collapsed;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Si necesitas convertir de vuelta (two-way binding)
            if (value is bool boolValue)
            {
                return boolValue;
            }
            
            // Si estás usando Visibility:
            // if (value is Avalonia.Visibility visibility)
            // {
            //     return visibility == Avalonia.Visibility.Visible;
            // }
            
            return false;
        }
    }
}