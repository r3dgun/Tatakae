using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tatakae.Infrastructure.Persistence.Models;

[Table("Categories")]
[Index(nameof(Slug), IsUnique = true)]
[Index(nameof(ParentId))]
public sealed class CategoryDbRecord : BaseEntity<Guid>
{
    [Required, MaxLength(180)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(220)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(1400)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(1000), Url]
    public string? CoverImageUrl { get; set; }

    [Required, MaxLength(260)]
    public string SeoMetaTitle { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string SeoMetaDescription { get; set; } = string.Empty;

    [MaxLength(600)]
    public string? SeoCanonicalPath { get; set; }

    [MaxLength(1000), Url]
    public string? SeoOpenGraphImageUrl { get; set; }

    public bool SeoAllowIndex { get; set; } = true;
    public bool SeoAllowFollow { get; set; } = true;

    public Guid? ParentId { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(ParentId))]
    public CategoryDbRecord? Parent { get; set; }

    [InverseProperty(nameof(Parent))]
    public List<CategoryDbRecord> Children { get; set; } = [];

    [InverseProperty(nameof(ProductDbRecord.Category))]
    public List<ProductDbRecord> Products { get; set; } = [];
}
