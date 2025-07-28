using PetSitterConnect.Models;
using System.Globalization;

namespace PetSitterConnect.Converters;

public class RatingToStarsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int rating)
        {
            return new string('⭐', rating) + new string('☆', 5 - rating);
        }
        return "☆☆☆☆☆";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class RatingToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int selectedRating && parameter is string starPositionStr && int.TryParse(starPositionStr, out int starPosition))
        {
            return selectedRating >= starPosition ? Color.FromArgb("#FFD700") : Color.FromArgb("#D3D3D3");
        }
        return Color.FromArgb("#D3D3D3");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ReviewTypeToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ReviewType reviewType)
        {
            return reviewType switch
            {
                ReviewType.OwnerToSitter => "Pet Owner Review",
                ReviewType.SitterToOwner => "Pet Sitter Review",
                _ => "Review"
            };
        }
        return "Review";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ReviewTypeToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ReviewType reviewType)
        {
            return reviewType switch
            {
                ReviewType.OwnerToSitter => "🏠", // House icon for pet owner
                ReviewType.SitterToOwner => "🐕‍🦺", // Service dog icon for pet sitter
                _ => "📝"
            };
        }
        return "📝";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ReviewTypeToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ReviewType reviewType)
        {
            return reviewType switch
            {
                ReviewType.OwnerToSitter => Color.FromArgb("#34C759"), // Green for pet owner
                ReviewType.SitterToOwner => Color.FromArgb("#007AFF"), // Blue for pet sitter
                _ => Color.FromArgb("#8E8E93")
            };
        }
        return Color.FromArgb("#8E8E93");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class IsNotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}