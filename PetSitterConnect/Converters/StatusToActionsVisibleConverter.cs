using PetSitterConnect.Models;
using System.Globalization;

namespace PetSitterConnect.Converters;

public class StatusToActionsVisibleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is BookingStatus status)
        {
            return status == BookingStatus.Pending || 
                   status == BookingStatus.Confirmed || 
                   status == BookingStatus.InProgress;
        }
        
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
