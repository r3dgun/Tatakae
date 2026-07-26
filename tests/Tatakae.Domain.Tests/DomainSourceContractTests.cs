namespace Tatakae.Domain.Tests;

public sealed class DomainSourceContractTests
{
    [Fact]
    public void DomainSource_DoesNotGenerateTimeIdentityOrRandomValues()
    {
        var root = FindSolutionRoot();
        var domain = Path.Combine(root, "src", "Tatakae.Domain");
        var source = string.Join('\n', Directory.GetFiles(domain, "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText));

        Assert.DoesNotContain("DateTimeOffset.UtcNow", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DateTime.UtcNow", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Guid.NewGuid", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Random.Shared", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DomainSource_DoesNotDependOnOuterLayerConcepts()
    {
        var root = FindSolutionRoot();
        var domain = Path.Combine(root, "src", "Tatakae.Domain");
        var source = string.Join('\n', Directory.GetFiles(domain, "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText));

        Assert.DoesNotContain("ResultDto", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Tatakae.Application", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Tatakae.Infrastructure", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
    }

    private static string FindSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Tatakae.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate Tatakae.sln from the test output directory.");
    }
}
