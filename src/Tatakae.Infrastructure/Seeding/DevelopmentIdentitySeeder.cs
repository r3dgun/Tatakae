using Microsoft.AspNetCore.Identity;
using Tatakae.Infrastructure.Persistence.Models;

namespace Tatakae.Infrastructure.Seeding;

public static class DevelopmentIdentitySeeder
{
    public static async Task EnsureUsersAsync(
        UserManager<ApplicationUserIdentity> userManager,
        bool resetPasswords,
        CancellationToken cancellationToken = default)
    {
        foreach (var credential in DevelopmentSeedCatalog.Credentials)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var user = await EnsureUserAsync(userManager, credential, resetPasswords);
            await EnsureRoleAsync(userManager, user, credential.Role);
        }
    }

    private static async Task<ApplicationUserIdentity> EnsureUserAsync(
        UserManager<ApplicationUserIdentity> userManager,
        DevelopmentSeedCredential credential,
        bool resetPassword)
    {
        var user = await userManager.FindByNameAsync(credential.Mobile);
        if (user is null)
        {
            user = new ApplicationUserIdentity
            {
                Id = credential.UserId,
                UserName = credential.Mobile,
                NormalizedUserName = credential.Mobile,
                PhoneNumber = credential.Mobile,
                PhoneNumberConfirmed = true,
                MobileConfirmed = true,
                Email = credential.Email,
                NormalizedEmail = credential.Email.ToUpperInvariant(),
                EmailConfirmed = true,
                FullName = credential.FullName,
                CustomerId = credential.Role == "Customer" ? DevelopmentSeedCatalog.CustomerId : null,
                IsActive = true,
                CreatedAt = DevelopmentSeedCatalog.FixedTimestamp,
                SecurityStamp = SeedIds.From($"security-stamp:{credential.Mobile}").ToString("N")
            };

            var createResult = await userManager.CreateAsync(user, credential.Password);
            ThrowIfFailed(createResult, $"create development user {credential.Mobile}");
            return user;
        }

        user.FullName = credential.FullName;
        user.PhoneNumber = credential.Mobile;
        user.PhoneNumberConfirmed = true;
        user.MobileConfirmed = true;
        user.Email = credential.Email;
        user.NormalizedEmail = credential.Email.ToUpperInvariant();
        user.EmailConfirmed = true;
        user.CustomerId = credential.Role == "Customer" ? DevelopmentSeedCatalog.CustomerId : null;
        user.IsActive = true;

        if (resetPassword)
        {
            user.PasswordHash = userManager.PasswordHasher.HashPassword(user, credential.Password);
            user.SecurityStamp = SeedIds.From($"security-stamp:{credential.Mobile}").ToString("N");
        }

        var updateResult = await userManager.UpdateAsync(user);
        ThrowIfFailed(updateResult, $"update development user {credential.Mobile}");
        return user;
    }

    private static async Task EnsureRoleAsync(
        UserManager<ApplicationUserIdentity> userManager,
        ApplicationUserIdentity user,
        string roleName)
    {
        if (await userManager.IsInRoleAsync(user, roleName)) return;
        var result = await userManager.AddToRoleAsync(user, roleName);
        ThrowIfFailed(result, $"assign role {roleName} to {user.UserName}");
    }

    private static void ThrowIfFailed(IdentityResult result, string operation)
    {
        if (result.Succeeded) return;
        throw new InvalidOperationException($"Could not {operation}: {string.Join(" | ", result.Errors.Select(x => x.Description))}");
    }
}
