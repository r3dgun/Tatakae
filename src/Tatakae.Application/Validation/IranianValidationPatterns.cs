namespace Tatakae.Application.Validation;

public static class IranianValidationPatterns
{
    // 0912..., 912..., +98912... plus Persian and Arabic-Indic digits.
    public const string Mobile = @"^(?:(?:\+?98)|(?:\+?۹۸)|(?:\+?٩٨)|0|۰|٠)?[9۹٩][0-9۰-۹٠-٩]{9}$";

    public const string PostalCode = @"^[0-9۰-۹٠-٩]{10}$";
}
