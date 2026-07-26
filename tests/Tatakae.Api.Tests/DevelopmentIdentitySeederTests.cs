using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tatakae.Infrastructure.Persistence;
using Tatakae.Infrastructure.Persistence.Models;
using Tatakae.Infrastructure.Seeding;

namespace Tatakae.Api.Tests;

public sealed class DevelopmentIdentitySeederTests
{
    [Fact]
    public async Task EnsureUsersAsync_CreatesAdminAndCustomerAccountsIdempotently()
    {
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TatakaeDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUserIdentity>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRoleIdentity>>();
        await db.Database.EnsureCreatedAsync();
        await EnsureRequiredRolesAsync(roles);

        await DevelopmentIdentitySeeder.EnsureUsersAsync(users, resetPasswords: true);
        await DevelopmentIdentitySeeder.EnsureUsersAsync(users, resetPasswords: true);

        Assert.Equal(DevelopmentSeedCatalog.Credentials.Count, await users.Users.CountAsync());
        foreach (var credential in DevelopmentSeedCatalog.Credentials)
        {
            var user = await users.FindByNameAsync(credential.Mobile);
            Assert.NotNull(user);
            Assert.Equal(credential.UserId, user.Id);
            Assert.True(await users.CheckPasswordAsync(user, credential.Password));
            Assert.True(await users.IsInRoleAsync(user, credential.Role));
        }

        var customer = await users.FindByNameAsync(DevelopmentSeedCatalog.Customer.Mobile);
        Assert.NotNull(customer);
        Assert.Equal(DevelopmentSeedCatalog.CustomerId, customer.CustomerId);
    }

    [Fact]
    public async Task EnsureUsersAsync_WithPasswordReset_RestoresDocumentedDevelopmentPassword()
    {
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TatakaeDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUserIdentity>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRoleIdentity>>();
        await db.Database.EnsureCreatedAsync();
        await EnsureRequiredRolesAsync(roles);
        await DevelopmentIdentitySeeder.EnsureUsersAsync(users, resetPasswords: true);

        var admin = await users.FindByNameAsync(DevelopmentSeedCatalog.SuperAdmin.Mobile);
        Assert.NotNull(admin);
        admin.PasswordHash = users.PasswordHasher.HashPassword(admin, "Changed@987654");
        Assert.True((await users.UpdateAsync(admin)).Succeeded);
        Assert.False(await users.CheckPasswordAsync(admin, DevelopmentSeedCatalog.SuperAdmin.Password));

        await DevelopmentIdentitySeeder.EnsureUsersAsync(users, resetPasswords: true);

        admin = await users.FindByNameAsync(DevelopmentSeedCatalog.SuperAdmin.Mobile);
        Assert.NotNull(admin);
        Assert.True(await users.CheckPasswordAsync(admin, DevelopmentSeedCatalog.SuperAdmin.Password));
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<TatakaeDbContext>(options =>
            options.UseInMemoryDatabase($"tatakae-phase14-identity-{Guid.NewGuid():N}"));
        services
            .AddIdentityCore<ApplicationUserIdentity>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<ApplicationRoleIdentity>()
            .AddEntityFrameworkStores<TatakaeDbContext>();
        return services.BuildServiceProvider();
    }

    private static async Task EnsureRequiredRolesAsync(RoleManager<ApplicationRoleIdentity> roles)
    {
        foreach (var roleName in DevelopmentSeedCatalog.Credentials.Select(x => x.Role).Distinct(StringComparer.Ordinal))
        {
            if (await roles.RoleExistsAsync(roleName)) continue;
            var result = await roles.CreateAsync(new ApplicationRoleIdentity
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant(),
                DisplayName = roleName,
                IsSystem = true,
                CreatedAt = DevelopmentSeedCatalog.FixedTimestamp
            });
            Assert.True(result.Succeeded, string.Join(" | ", result.Errors.Select(x => x.Description)));
        }
    }
}
