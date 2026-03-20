namespace GradingTool.Converters;

using System.Globalization;
using System.Windows.Data;
using GradingTool.Models;
using GradingTool.ViewModels;

public class CommentUsageTooltipConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is CommentEntry entry && values[1] is GridEditorViewModel vm)
            return vm.GetCommentUsagesTooltip(entry);
        return string.Empty;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
