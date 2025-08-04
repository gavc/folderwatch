using System.Globalization;
using System.Windows.Data;

namespace FolderWatch.WPF;

/// <summary>
/// Converts boolean HasMultipleSteps to rule type display text
/// </summary>
public class RuleTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool hasMultipleSteps)
        {
            return hasMultipleSteps ? "Multi-step" : "Single";
        }
        
        return "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}