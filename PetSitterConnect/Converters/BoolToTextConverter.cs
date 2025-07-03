using System.Globalization;

namespace PetSitterConnect.Converters;

public class BoolToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            // Check parameter to determine context
            var context = parameter?.ToString();

            if (context == "BookingMode")
            {
                return boolValue ? "As Owner" : "As Sitter";
            }

            // Default behavior for request filtering
            return boolValue ? "My Requests" : "All Requests";
        }

        return "All Requests";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
