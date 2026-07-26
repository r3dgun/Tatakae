using System.Globalization;

namespace Tatakae.Web.Formatting;

public static class Currency
{
    public static string Format(decimal amount) => $"{amount.ToString("N0", CultureInfo.GetCultureInfo("fa-IR"))} تومان";
}
