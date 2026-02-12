using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GradingTool.Converters;

public class StatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string status)
        {
            if (status.Contains("Erreur"))
            {
                return Brushes.Red;
            }
            else if (status.Contains("réussie") || status.Contains("Sauvegarde réussie"))
            {
                return Brushes.Green;
            }
            else if (status.Contains("en cours"))
            {
                return Brushes.Orange;
            }
        }
        return Brushes.Black;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}