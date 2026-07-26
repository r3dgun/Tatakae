namespace Tatakae.Infrastructure.Seeding;

/// <summary>
/// Controls database bootstrap data. Core catalog/policies can be enabled independently
/// from development-only users, orders, questions and addresses.
/// </summary>
public sealed class SeedDataOptions
{
    public const string SectionName = "SeedData";

    public bool Enabled { get; set; } = true;
    public bool IncludeDevelopmentFixtures { get; set; }
    public bool ResetDevelopmentPasswords { get; set; }
}
