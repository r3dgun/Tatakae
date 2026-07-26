namespace Tatakae.Domain.Common;

internal static class DomainGuard
{
    public static Guid NotEmpty(Guid value, string parameterName, string message)
    {
        if (value == Guid.Empty)
            throw new ArgumentException(message, parameterName);
        return value;
    }

    public static string Required(string? value, string parameterName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(message, parameterName);
        return value.Trim();
    }

    public static string? Optional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static decimal NonNegative(decimal value, string parameterName, string message)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(parameterName, value, message);
        return value;
    }

    public static int NonNegative(int value, string parameterName, string message)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(parameterName, value, message);
        return value;
    }

    public static decimal Positive(decimal value, string parameterName, string message)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(parameterName, value, message);
        return value;
    }

    public static int Positive(int value, string parameterName, string message)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(parameterName, value, message);
        return value;
    }

    public static IReadOnlyCollection<T> NotEmpty<T>(IEnumerable<T>? values, string parameterName, string message)
    {
        var items = values?.ToArray() ?? [];
        if (items.Length == 0)
            throw new ArgumentException(message, parameterName);
        return items;
    }

    public static void InRange(int value, int minimum, int maximum, string parameterName, string message)
    {
        if (value < minimum || value > maximum)
            throw new ArgumentOutOfRangeException(parameterName, value, message);
    }
}
