using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tatakae.Infrastructure.Persistence.Models;

[Table("Customers")]
[Index(nameof(Mobile), IsUnique = true)]
[Index(nameof(Email))]
public sealed class CustomerDbRecord : BaseEntity<Guid>
{
    [Required, MaxLength(180)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(20), Phone]
    public string Mobile { get; set; } = string.Empty;

    [MaxLength(260), EmailAddress]
    public string? Email { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    [InverseProperty(nameof(CustomerAddressDbRecord.Customer))]
    public List<CustomerAddressDbRecord> Addresses { get; set; } = [];
}

[Table("CustomerAddresses")]
[Index(nameof(CustomerId))]
public sealed class CustomerAddressDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid CustomerId { get; set; }

    [Required, MaxLength(180)]
    public string RecipientName { get; set; } = string.Empty;

    [Required, MaxLength(20), Phone]
    public string Mobile { get; set; } = string.Empty;

    [Required, MaxLength(90)]
    public string Province { get; set; } = string.Empty;

    [Required, MaxLength(90)]
    public string City { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string PostalCode { get; set; } = string.Empty;

    [Required, MaxLength(900)]
    public string AddressLine { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Plaque { get; set; }

    [MaxLength(30)]
    public string? Unit { get; set; }

    public bool IsDefault { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public CustomerDbRecord? Customer { get; set; }
}
