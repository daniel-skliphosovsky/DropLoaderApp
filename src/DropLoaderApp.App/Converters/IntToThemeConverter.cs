using System.Globalization;

namespace DropLoaderApp.Converters;

public sealed class IntToThemeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index)
            return index == 0 ? "Light" : "Dark";
        return "Auto";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
