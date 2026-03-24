using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ConverterSplitter.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible;
}

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Collapsed;
}

public class TimeSpanToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TimeSpan ts)
            return ts.Hours > 0 ? ts.ToString(@"hh\:mm\:ss\.f") : ts.ToString(@"mm\:ss\.f");
        return "00:00.0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => TimeSpan.Zero;
}

public class FileSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long size)
        {
            return size switch
            {
                < 1024 => $"{size} B",
                < 1024 * 1024 => $"{size / 1024.0:F1} KB",
                < 1024 * 1024 * 1024 => $"{size / (1024.0 * 1024):F1} MB",
                _ => $"{size / (1024.0 * 1024 * 1024):F2} GB"
            };
        }
        return "0 B";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => 0L;
}

public class PercentageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
            return $"{d:F0}%";
        return "0%";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => 0.0;
}
