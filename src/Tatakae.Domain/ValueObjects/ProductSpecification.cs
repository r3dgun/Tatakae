using Tatakae.Domain.Common;

namespace Tatakae.Domain.Entities;

public sealed record ProductSpecification
{
    public ProductSpecification(string name, string value, int sortOrder = 0)
    {
        Name = DomainGuard.Required(name, nameof(name), "نام ویژگی محصول الزامی است.");
        Value = DomainGuard.Required(value, nameof(value), "مقدار ویژگی محصول الزامی است.");
        SortOrder = DomainGuard.NonNegative(sortOrder, nameof(sortOrder), "ترتیب ویژگی نمی‌تواند منفی باشد.");
    }

    public string Name { get; }
    public string Value { get; }
    public int SortOrder { get; }
}
