using System.Globalization;
using System.Text;

namespace Tatakae.Application.Validation;

public static class IranianInputNormalizer
{
    public static string NormalizeDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value.Trim())
        {
            builder.Append(ch switch
            {
                '۰' or '٠' => '0',
                '۱' or '١' => '1',
                '۲' or '٢' => '2',
                '۳' or '٣' => '3',
                '۴' or '٤' => '4',
                '۵' or '٥' => '5',
                '۶' or '٦' => '6',
                '۷' or '٧' => '7',
                '۸' or '٨' => '8',
                '۹' or '٩' => '9',
                _ => ch
            });
        }
        return builder.ToString();
    }

    public static bool TryParseNonNegativeDecimal(string? value, out decimal result)
    {
        var normalized = NormalizeDigits(value)
            .Replace(",", string.Empty, StringComparison.Ordinal)
            .Replace("٬", string.Empty, StringComparison.Ordinal)
            .Replace("،", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        return decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out result)
            && result >= 0;
    }

    public static string NormalizeIranianMobile(string? value)
    {
        var normalized = NormalizeDigits(value).Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
        if (normalized.StartsWith("+98", StringComparison.Ordinal)) normalized = "0" + normalized[3..];
        else if (normalized.StartsWith("98", StringComparison.Ordinal) && normalized.Length == 12) normalized = "0" + normalized[2..];
        else if (normalized.Length == 10 && normalized.StartsWith('9')) normalized = "0" + normalized;
        return normalized;
    }
}
