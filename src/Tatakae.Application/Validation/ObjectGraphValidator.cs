using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Tatakae.Application.Validation;

public sealed record ObjectGraphValidationError(object Instance, string MemberName, string ErrorMessage);

public static class ObjectGraphValidator
{
    public static IReadOnlyCollection<ObjectGraphValidationError> Validate(object model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var errors = new List<ObjectGraphValidationError>();
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        ValidateRecursive(model, errors, visited);
        return errors;
    }

    public static IReadOnlyCollection<ObjectGraphValidationError> ValidateProperty(object model, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        var property = model.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (property is null || property.GetIndexParameters().Length != 0)
        {
            return [];
        }

        var results = new List<ValidationResult>();
        var context = new ValidationContext(model) { MemberName = propertyName };
        Validator.TryValidateProperty(property.GetValue(model), context, results);
        return results.SelectMany(result => ToErrors(model, propertyName, result)).ToArray();
    }

    private static void ValidateRecursive(object instance, ICollection<ObjectGraphValidationError> errors, ISet<object> visited)
    {
        if (!visited.Add(instance)) return;

        var validationContext = new ValidationContext(instance);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, validationContext, results, validateAllProperties: true);
        AddDistinctErrors(instance, results, errors);

        // Validator.TryValidateObject only invokes IValidatableObject when all
        // property-level attributes succeed. Form validation should still report
        // cross-field rules (for example duplicate SKU) beside field errors, so
        // invoke it explicitly and de-duplicate results when .NET already ran it.
        if (instance is IValidatableObject validatable)
        {
            AddDistinctErrors(instance, validatable.Validate(validationContext), errors);
        }

        foreach (var property in instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead || property.GetIndexParameters().Length != 0) continue;
            var value = property.GetValue(instance);
            if (value is null || IsSimple(value.GetType())) continue;

            if (value is IEnumerable enumerable && value is not string)
            {
                foreach (var item in enumerable)
                {
                    if (item is not null && !IsSimple(item.GetType())) ValidateRecursive(item, errors, visited);
                }
            }
            else
            {
                ValidateRecursive(value, errors, visited);
            }
        }
    }

    private static void AddDistinctErrors(
        object instance,
        IEnumerable<ValidationResult>? results,
        ICollection<ObjectGraphValidationError> errors)
    {
        if (results is null) return;

        foreach (var result in results)
        {
            foreach (var error in ToErrors(instance, string.Empty, result))
            {
                var exists = errors.Any(existing =>
                    ReferenceEquals(existing.Instance, error.Instance)
                    && string.Equals(existing.MemberName, error.MemberName, StringComparison.Ordinal)
                    && string.Equals(existing.ErrorMessage, error.ErrorMessage, StringComparison.Ordinal));

                if (!exists) errors.Add(error);
            }
        }
    }

    private static IEnumerable<ObjectGraphValidationError> ToErrors(object instance, string fallbackMember, ValidationResult result)
    {
        var message = result.ErrorMessage ?? "مقدار واردشده معتبر نیست.";
        var members = result.MemberNames.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        if (members.Length == 0)
        {
            yield return new ObjectGraphValidationError(instance, fallbackMember, message);
            yield break;
        }

        foreach (var member in members)
        {
            yield return new ObjectGraphValidationError(instance, member, message);
        }
    }

    private static bool IsSimple(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan)
            || type == typeof(Guid)
            || type == typeof(Uri);
    }
}
