using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Allva.Desktop.Converters;

public class BoolToFormTitleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isNew && parameter is string param)
        {
            var titles = param.Split('|');
            if (titles.Length == 2)
            {
                return isNew ? titles[0] : titles[1];
            }
        }
        return "Formulario";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}