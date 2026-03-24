namespace GradingTool.Converters;

using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

/// <summary>
/// Convertit un pourcentage de points (0–100) en une couleur dégradée vert→rouge subtile.
/// </summary>
public class ScalePointsColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int points)
            return new SolidColorBrush(Colors.Transparent);

        double t = Math.Clamp(points / 100.0, 0.0, 1.0);

        // Vert subtil (#D4EDDA) → Jaune (#FFF3CD) → Rouge subtil (#F8D7DA)
        Color color = t switch
        {
            >= 0.75 => Lerp(Color.FromRgb(0xFF, 0xF3, 0xCD), Color.FromRgb(0xD4, 0xED, 0xDA), (t - 0.75) / 0.25),
            >= 0.40 => Lerp(Color.FromRgb(0xF8, 0xD7, 0xDA), Color.FromRgb(0xFF, 0xF3, 0xCD), (t - 0.40) / 0.35),
            _       => Lerp(Color.FromRgb(0xF8, 0xC0, 0xC4), Color.FromRgb(0xF8, 0xD7, 0xDA), t / 0.40)
        };

        return new SolidColorBrush(color);
    }

    private static Color Lerp(Color from, Color to, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);
        return Color.FromRgb(
            (byte)(from.R + (to.R - from.R) * t),
            (byte)(from.G + (to.G - from.G) * t),
            (byte)(from.B + (to.B - from.B) * t));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
