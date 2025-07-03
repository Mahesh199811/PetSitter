using PetSitterConnect.Models;
using System.Globalization;

namespace PetSitterConnect.Converters;

public class StatusToPendingConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is BookingStatus status)
        {
            return status == BookingStatus.Pending;
        }
        
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
