using System;
using System.Globalization;
using System.Windows.Data;

namespace GradingTool.Converters;

public class GenerationModeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string currentMode && parameter is string targetMode)
        {
            return currentMode == targetMode;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isChecked && isChecked && parameter is string mode)
        {
            return mode;
        }
        return Binding.DoNothing;
    }
}
