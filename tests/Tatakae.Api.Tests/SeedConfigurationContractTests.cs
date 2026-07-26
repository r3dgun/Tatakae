using System.Text.Json;
using Tatakae.Infrastructure.Seeding;

namespace Tatakae.Api.Tests;

public sealed class SeedConfigurationContractTests
{
    [Fact]
    public void SeedOptions_DefaultToProductionSafeDevelopmentFixtureSettings()
    {
        var options = new SeedDataOptions();

        Assert.True(options.Enabled);
        Assert.False(options.IncludeDevelopmentFixtures);
        Assert.False(options.ResetDevelopmentPasswords);
    }

    [Fact]
    public void AppSettings_EnableDemoFixturesOnlyForDevelopment()
    {
        var root = FindSolutionRoot();
        using var production = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "src", "Tatakae.Api", "appsettings.json")));
        using var development = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "src", "Tatakae.Api", "appsettings.Development.json")));

        var productionSeed = production.RootElement.GetProperty(SeedDataOptions.SectionName);
        var developmentSeed = development.RootElement.GetProperty(SeedDataOptions.SectionName);

        Assert.False(productionSeed.GetProperty(nameof(SeedDataOptions.IncludeDevelopmentFixtures)).GetBoolean());
        Assert.False(productionSeed.GetProperty(nameof(SeedDataOptions.ResetDevelopmentPasswords)).GetBoolean());
        Assert.True(developmentSeed.GetProperty(nameof(SeedDataOptions.IncludeDevelopmentFixtures)).GetBoolean());
        Assert.True(developmentSeed.GetProperty(nameof(SeedDataOptions.ResetDevelopmentPasswords)).GetBoolean());
    }

    private static string FindSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Tatakae.sln"))) return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate Tatakae.sln from the test output directory.");
    }
}
