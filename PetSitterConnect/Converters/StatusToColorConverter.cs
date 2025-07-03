using PetSitterConnect.Models;
using System.Globalization;

namespace PetSitterConnect.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is BookingStatus status)
        {
            return status switch
            {
                BookingStatus.Pending => Color.FromArgb("#FF9500"), // Orange
                BookingStatus.Confirmed => Color.FromArgb("#007AFF"), // Blue
                BookingStatus.InProgress => Color.FromArgb("#34C759"), // Green
                BookingStatus.Completed => Color.FromArgb("#30D158"), // Dark Green
                BookingStatus.Cancelled => Color.FromArgb("#FF3B30"), // Red
                BookingStatus.Rejected => Color.FromArgb("#D70015"), // Dark Red
                _ => Color.FromArgb("#8E8E93") // Gray
            };
        }
        
        return Color.FromArgb("#8E8E93"); // Default gray
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
