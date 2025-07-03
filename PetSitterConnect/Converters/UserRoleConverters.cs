using PetSitterConnect.Models;
using System.Globalization;

namespace PetSitterConnect.Converters;

public class UserTypeToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is UserType userType)
        {
            return userType switch
            {
                UserType.PetOwner => "🏠",
                UserType.PetSitter => "🐕‍🦺",
                UserType.Both => "🏠🐕‍🦺",
                _ => "👤"
            };
        }
        return "👤";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class UserTypeToLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is UserType userType)
        {
            return userType switch
            {
                UserType.PetOwner => "PET OWNER",
                UserType.PetSitter => "PET SITTER",
                UserType.Both => "OWNER & SITTER",
                _ => "USER"
            };
        }
        return "USER";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class UserTypeToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is UserType userType)
        {
            return userType switch
            {
                UserType.PetOwner => Color.FromArgb("#2E7D32"), // Green
                UserType.PetSitter => Color.FromArgb("#1976D2"), // Blue
                UserType.Both => Color.FromArgb("#7B1FA2"), // Purple
                _ => Color.FromArgb("#616161") // Gray
            };
        }
        return Color.FromArgb("#616161");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class UserTypeToDescriptionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is UserType userType)
        {
            return userType switch
            {
                UserType.PetOwner => "Find trusted sitters for your pets",
                UserType.PetSitter => "Discover pet care opportunities",
                UserType.Both => "Manage your pets and find sitting jobs",
                _ => "Welcome to PetSitter Connect"
            };
        }
        return "Welcome to PetSitter Connect";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToRoleContextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool showMyRequestsOnly)
        {
            return showMyRequestsOnly 
                ? "Viewing your own pet care requests" 
                : "Browsing available requests from pet owners";
        }
        return "Browse pet care requests";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class UserTypeToActionTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is UserType userType)
        {
            return userType switch
            {
                UserType.PetOwner => "Create Pet Care Request",
                UserType.PetSitter => "Find Pet Care Jobs",
                UserType.Both => "Create Request or Find Jobs",
                _ => "Get Started"
            };
        }
        return "Get Started";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToBookingModeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool showAsSitter)
        {
            return showAsSitter
                ? "Viewing bookings as Pet Sitter"
                : "Viewing bookings as Pet Owner";
        }
        return "Manage your bookings";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class OwnerCanAcceptConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 2) return false;

        if (values[0] is BookingStatus status && values[1] is bool showAsSitter)
        {
            // Only show Accept/Reject buttons for pet owners (showAsSitter = false)
            // when viewing pending applications
            return !showAsSitter && status == BookingStatus.Pending;
        }

        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class SitterCanCompleteConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 2) return false;

        if (values[0] is BookingStatus status && values[1] is bool showAsSitter)
        {
            // Only show Complete button for pet sitters (showAsSitter = true)
            // when viewing confirmed/in-progress bookings
            return showAsSitter && (status == BookingStatus.Confirmed || status == BookingStatus.InProgress);
        }

        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
