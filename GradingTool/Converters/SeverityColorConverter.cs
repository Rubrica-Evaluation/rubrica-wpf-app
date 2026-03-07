namespace GradingTool.Converters;

using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using GradingTool.Models;

public class SeverityColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is CommentSeverity severity ? severity switch
        {
            CommentSeverity.Mineur   => new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)),
            CommentSeverity.Majeur   => new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)),
            CommentSeverity.Critique => new SolidColorBrush(Color.FromRgb(0x99, 0x1B, 0x1B)),
            _                        => new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E))
        } : Binding.DoNothing;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
