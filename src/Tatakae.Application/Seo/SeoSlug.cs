using System.Text;
using System.Text.RegularExpressions;

namespace Tatakae.Application.Seo;

/// <summary>
/// Normalizes public URL slugs for Persian and English content.
/// The persisted value is lowercase and uses a single hyphen as separator.
/// </summary>
public static class SeoSlug
{
    public const string ValidationPattern = "^[A-Za-z0-9\\u0621-\\u063A\\u0641-\\u064A\\u067E\\u0686\\u0698\\u06A9\\u06AF\\u06CC\\u0660-\\u0669\\u06F0-\\u06F9]+(?:[\\s_\\-\\u200C]+[A-Za-z0-9\\u0621-\\u063A\\u0641-\\u064A\\u067E\\u0686\\u0698\\u06A9\\u06AF\\u06CC\\u0660-\\u0669\\u06F0-\\u06F9]+)*$";

    private static readonly Regex SeparatorRegex = new("[\\s_\\-\\u200C]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex InvalidCharacterRegex = new("[^a-z0-9\\u0621-\\u063A\\u0641-\\u064A\\u067E\\u0686\\u0698\\u06A9\\u06AF\\u06CC-]", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex DuplicateSlashRegex = new("/{2,}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var normalized = NormalizePersianCharacters(NormalizeUnicode(value).Trim().ToLowerInvariant());
        normalized = ConvertDigitsToLatin(normalized);
        normalized = SeparatorRegex.Replace(normalized, "-");
        normalized = InvalidCharacterRegex.Replace(normalized, string.Empty);
        return normalized.Trim('-');
    }

    public static string NormalizeCanonicalPath(string? candidate, string fallbackPath)
    {
        var value = string.IsNullOrWhiteSpace(candidate) ? fallbackPath : candidate.Trim();
        if (Uri.TryCreate(value, UriKind.Absolute, out var absolute)
            && (absolute.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || absolute.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            && !string.IsNullOrWhiteSpace(absolute.Host))
        {
            value = absolute.AbsolutePath;
        }

        var queryIndex = value.IndexOfAny('?', '#');
        if (queryIndex >= 0) value = value[..queryIndex];

        value = value.Replace('\\', '/');
        if (!value.StartsWith('/')) value = "/" + value;
        value = DuplicateSlashRegex.Replace(value, "/");
        value = value == "/" ? value : value.TrimEnd('/');
        return string.IsNullOrWhiteSpace(value) ? fallbackPath : value;
    }


    public static string LegalPagePath(string? slug) => Normalize(slug) switch
    {
        "about" => "/about",
        "terms" or "rules" => "/rules",
        "privacy" => "/privacy",
        "returns" => "/returns",
        "shipping" or "shipping-policy" => "/shipping-policy",
        "contact" => "/contact",
        var value => string.IsNullOrWhiteSpace(value) ? "/rules" : $"/pages/{value}"
    };


    private static string NormalizeUnicode(string value)
    {
        // Blazor WebAssembly does not support compatibility normalization forms
        // such as FormKC. Map the compatibility characters relevant to slugs
        // ourselves, then use canonical composition, which is browser-safe.
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(character switch
            {
                '\u3000' => ' ',
                >= '\uFF01' and <= '\uFF5E' => (char)(character - 0xFEE0),
                _ => character
            });
        }

        var compatible = builder.ToString();
        try
        {
            return compatible.Normalize(NormalizationForm.FormC);
        }
        catch (ArgumentException)
        {
            // Keep slug generation available even in a browser/runtime configured
            // without Unicode normalization data. Later filters still remove
            // unsupported characters deterministically.
            return compatible;
        }
        catch (PlatformNotSupportedException)
        {
            return compatible;
        }
    }

    private static string NormalizePersianCharacters(string value) => value
        .Replace('ي', 'ی')
        .Replace('ى', 'ی')
        .Replace('ك', 'ک')
        .Replace('ۀ', 'ه')
        .Replace('ة', 'ه');

    private static string ConvertDigitsToLatin(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(character switch
            {
                >= '\u06F0' and <= '\u06F9' => (char)('0' + character - '\u06F0'),
                >= '\u0660' and <= '\u0669' => (char)('0' + character - '\u0660'),
                _ => character
            });
        }
        return builder.ToString();
    }
}
