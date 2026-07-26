using System.Reflection;
using Tatakae.Domain.Entities;

namespace Tatakae.Domain.Tests;

public sealed class DomainArchitectureTests
{
    [Fact]
    public void DomainAssembly_DoesNotReferenceOuterLayers()
    {
        var references = typeof(Order).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();

        Assert.DoesNotContain("Tatakae.Application", references);
        Assert.DoesNotContain("Tatakae.Infrastructure", references);
        Assert.DoesNotContain("Tatakae.Api", references);
        Assert.DoesNotContain("Tatakae.Web", references);
    }

    [Fact]
    public void OrderCreate_RequiresIdentityNumberAndTimestampFromApplication()
    {
        var create = typeof(Order).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(x => x.Name == nameof(Order.Create));
        var names = create.GetParameters().Select(x => x.Name).ToArray();

        Assert.Equal("id", names[0]);
        Assert.Equal("orderNumber", names[1]);
        Assert.Equal("createdAt", names[^1]);
    }

    [Fact]
    public void ProductAndCustomer_ExposeExplicitFactories()
    {
        Assert.NotNull(typeof(Product).GetMethod(nameof(Product.Create), BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(typeof(Product).GetMethod(nameof(Product.Rehydrate), BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(typeof(Customer).GetMethod(nameof(Customer.Create), BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(typeof(Customer).GetMethod(nameof(Customer.Rehydrate), BindingFlags.Public | BindingFlags.Static));
    }
}
