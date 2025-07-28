using System.Globalization;

namespace PetSitterConnect.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string colorPair)
        {
            var colors = colorPair.Split(',');
            if (colors.Length == 2)
            {
                return boolValue ? Color.FromArgb(colors[0]) : Color.FromArgb(colors[1]);
            }
        }
        return Colors.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ServiceTypeToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string serviceType)
        {
            return serviceType switch
            {
                "PetSitting" => "🏠",
                "DogWalking" => "🚶‍♂️",
                "PetBoarding" => "🏨",
                "PetGrooming" => "✂️",
                "VeterinaryVisit" => "🏥",
                "PetTraining" => "🎓",
                "PetTaxi" => "🚗",
                "Overnight" => "🌙",
                _ => "🐾"
            };
        }
        return "🐾";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class PetTypeToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string petType)
        {
            return petType switch
            {
                "Dog" => "🐕",
                "Cat" => "🐱",
                "Bird" => "🐦",
                "Fish" => "🐠",
                "Rabbit" => "🐰",
                "Hamster" => "🐹",
                "GuineaPig" => "🐹",
                "Reptile" => "🦎",
                _ => "🐾"
            };
        }
        return "🐾";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class DistanceToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double distance)
        {
            if (distance < 1)
            {
                return $"{(distance * 1000):F0}m away";
            }
            return $"{distance:F1}km away";
        }
        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class PriceRangeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal price)
        {
            return price switch
            {
                < 25 => "$",
                < 50 => "$$",
                < 100 => "$$$",
                _ => "$$$$"
            };
        }
        return "$";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class AvailabilityToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isAvailable)
        {
            return isAvailable ? Color.FromArgb("#34C759") : Color.FromArgb("#FF3B30");
        }
        return Color.FromArgb("#8E8E93");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class AvailabilityToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isAvailable)
        {
            return isAvailable ? "Available" : "Busy";
        }
        return "Unknown";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class LastActiveToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime lastActive)
        {
            var timeSpan = DateTime.UtcNow - lastActive;
            
            if (timeSpan.TotalMinutes < 5)
                return "Active now";
            if (timeSpan.TotalMinutes < 60)
                return $"Active {timeSpan.Minutes}m ago";
            if (timeSpan.TotalHours < 24)
                return $"Active {timeSpan.Hours}h ago";
            if (timeSpan.TotalDays < 7)
                return $"Active {timeSpan.Days}d ago";
            
            return $"Active {lastActive:MMM dd}";
        }
        return "Unknown";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class UrgentToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool isUrgent && isUrgent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class EmergencyToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isEmergency && isEmergency)
        {
            return "🚨";
        }
        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}