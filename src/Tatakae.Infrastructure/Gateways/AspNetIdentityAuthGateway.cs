using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Tatakae.Application.Contracts.Account;
using Tatakae.Application.Interfaces.Gateways;
using Tatakae.Application.Security;
using Tatakae.Infrastructure.Persistence;
using Tatakae.Infrastructure.Persistence.Models;

namespace Tatakae.Infrastructure.Gateways;

public sealed class AspNetIdentityAuthGateway(
    UserManager<ApplicationUserIdentity> users,
    RoleManager<ApplicationRoleIdentity> roles,
    TatakaeDbContext db,
    IConfiguration configuration) : IIdentityAuthGateway
{
    public Task<AccountSessionDto> RegisterAsync(RegisterCustomerRequest request, ClientRequestMetadata metadata, CancellationToken cancellationToken = default)
        => RegisterCoreAsync(request, metadata ?? ClientRequestMetadata.Empty, cancellationToken);

    private async Task<AccountSessionDto> RegisterCoreAsync(RegisterCustomerRequest request, ClientRequestMetadata metadata, CancellationToken cancellationToken)
    {
        var mobile = NormalizeMobile(request.Mobile);
        var existing = await users.FindByNameAsync(mobile);
        if (existing is not null) throw new InvalidOperationException("کاربری با این شماره موبایل قبلاً ثبت شده است.");

        var user = new ApplicationUserIdentity
        {
            Id = Guid.NewGuid(),
            UserName = mobile,
            PhoneNumber = mobile,
            PhoneNumberConfirmed = true,
            MobileConfirmed = true,
            Email = request.Email,
            EmailConfirmed = !string.IsNullOrWhiteSpace(request.Email),
            FullName = request.FullName.Trim(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };

        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(" | ", result.Errors.Select(x => x.Description)));

        if (await roles.RoleExistsAsync("Customer")) await users.AddToRoleAsync(user, "Customer");

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await users.UpdateAsync(user);
        var session = await CreateSessionAsync(user, requestRememberMe: true, cancellationToken);
        await WriteLoginAuditAsync(user, mobile, session.SessionKey, succeeded: true, failureReason: null, session.ExpiresAt, metadata, cancellationToken);
        return session;
    }

    public Task<AccountSessionDto?> LoginAsync(LoginRequest request, ClientRequestMetadata metadata, CancellationToken cancellationToken = default)
        => LoginCoreAsync(request, metadata ?? ClientRequestMetadata.Empty, cancellationToken);

    private async Task<AccountSessionDto?> LoginCoreAsync(LoginRequest request, ClientRequestMetadata metadata, CancellationToken cancellationToken)
    {
        var mobile = NormalizeMobile(request.Mobile);
        var user = await users.FindByNameAsync(mobile);
        if (user is null || !user.IsActive)
        {
            await WriteLoginAuditAsync(null, mobile, Guid.NewGuid().ToString("N"), succeeded: false, "کاربر پیدا نشد یا غیرفعال است.", null, metadata, cancellationToken);
            return null;
        }

        if (!await users.CheckPasswordAsync(user, request.Password))
        {
            await WriteLoginAuditAsync(user, mobile, Guid.NewGuid().ToString("N"), succeeded: false, "رمز عبور اشتباه است.", null, metadata, cancellationToken);
            return null;
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await users.UpdateAsync(user);
        var session = await CreateSessionAsync(user, request.RememberMe, cancellationToken);
        await WriteLoginAuditAsync(user, mobile, session.SessionKey, succeeded: true, failureReason: null, session.ExpiresAt, metadata, cancellationToken);
        return session;
    }

    public Task<AccountSessionDto?> CurrentAsync(AuthenticatedSessionContext session, CancellationToken cancellationToken = default)
        => CurrentCoreAsync(session.UserId, session.SessionKey, cancellationToken);

    private async Task<AccountSessionDto?> CurrentCoreAsync(Guid userId, string? sessionKey, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty) return null;
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive) return null;

        if (!string.IsNullOrWhiteSpace(sessionKey))
        {
            var loginAudit = await db.LoginAudits
                .Where(x => x.UserId == user.Id && x.SessionKey == sessionKey && x.Succeeded && x.LogoutAt == null)
                .OrderByDescending(x => x.LoggedInAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (loginAudit is not null)
            {
                loginAudit.LastSeenAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        return await CreateSessionAsync(user, requestRememberMe: true, cancellationToken, existingSessionKey: sessionKey);
    }

    public Task LogoutAsync(AuthenticatedSessionContext session, CancellationToken cancellationToken = default)
        => LogoutCoreAsync(session.UserId, session.SessionKey, cancellationToken);

    private async Task LogoutCoreAsync(Guid userId, string? sessionKey, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(sessionKey)) return;

        var loginAudit = await db.LoginAudits
            .Where(x => x.UserId == userId && x.SessionKey == sessionKey && x.Succeeded && x.LogoutAt == null)
            .OrderByDescending(x => x.LoggedInAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (loginAudit is not null)
        {
            loginAudit.LastSeenAt = DateTimeOffset.UtcNow;
            loginAudit.LogoutAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<AccountSessionDto> CreateSessionAsync(ApplicationUserIdentity user, bool requestRememberMe, CancellationToken cancellationToken, string? existingSessionKey = null)
    {
        var customer = await EnsureCustomerLinkAsync(user, cancellationToken);
        var roleNames = (await users.GetRolesAsync(user)).OrderBy(x => x).ToArray();
        var permissions = await GetPermissionsAsync(roleNames, cancellationToken);
        var configuredMinutes = requestRememberMe
            ? configuration["Jwt:RememberMeAccessTokenMinutes"]
            : configuration["Jwt:AccessTokenMinutes"];
        var defaultMinutes = requestRememberMe ? 10080 : 240;
        var minutes = int.TryParse(configuredMinutes, out var parsedMinutes) && parsedMinutes > 0
            ? parsedMinutes
            : defaultMinutes;
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(minutes);
        var sessionKey = string.IsNullOrWhiteSpace(existingSessionKey) ? Guid.NewGuid().ToString("N") : existingSessionKey;
        var token = CreateJwt(user, roleNames, permissions, expiresAt, sessionKey);

        return new AccountSessionDto(
            customer.Id,
            user.FullName,
            user.PhoneNumber ?? user.UserName ?? string.Empty,
            user.Email,
            token,
            expiresAt,
            roleNames,
            permissions,
            sessionKey,
            requestRememberMe);
    }

    private async Task<CustomerDbRecord> EnsureCustomerLinkAsync(ApplicationUserIdentity user, CancellationToken cancellationToken)
    {
        var mobile = user.PhoneNumber ?? user.UserName ?? string.Empty;
        CustomerDbRecord? customer = null;

        if (user.CustomerId.HasValue)
        {
            customer = await db.Customers.FirstOrDefaultAsync(x => x.Id == user.CustomerId.Value, cancellationToken);
        }

        customer ??= await db.Customers.FirstOrDefaultAsync(x => x.Mobile == mobile, cancellationToken);

        if (customer is null)
        {
            customer = new CustomerDbRecord
            {
                Id = Guid.NewGuid(),
                FullName = user.FullName,
                Mobile = mobile,
                Email = user.Email,
                CreatedAt = user.CreatedAt == default ? DateTimeOffset.UtcNow : user.CreatedAt
            };
            db.Customers.Add(customer);
        }
        else
        {
            customer.FullName = string.IsNullOrWhiteSpace(user.FullName) ? customer.FullName : user.FullName;
            customer.Mobile = mobile;
            customer.Email = user.Email;
            if (customer.CreatedAt == default) customer.CreatedAt = DateTimeOffset.UtcNow;
        }

        if (user.CustomerId != customer.Id)
        {
            user.CustomerId = customer.Id;
            await users.UpdateAsync(user);
        }

        await db.SaveChangesAsync(cancellationToken);
        return customer;
    }

    private async Task<IReadOnlyCollection<string>> GetPermissionsAsync(IReadOnlyCollection<string> roleNames, CancellationToken cancellationToken)
    {
        var roleIds = await roles.Roles
            .Where(x => x.Name != null && roleNames.Contains(x.Name))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        return await db.RolePermissions
            .Include(x => x.Permission)
            .Where(x => roleIds.Contains(x.RoleId) && x.Permission != null && x.Permission.IsActive)
            .Select(x => x.Permission!.Key)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }

    private string CreateJwt(ApplicationUserIdentity user, IReadOnlyCollection<string> roleNames, IReadOnlyCollection<string> permissions, DateTimeOffset expiresAt, string sessionKey)
    {
        var key = configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey باید حداقل ۳۲ کاراکتر باشد.");
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.PhoneNumber ?? user.UserName ?? string.Empty),
            new(ClaimTypes.GivenName, user.FullName),
            new("mobile", user.PhoneNumber ?? user.UserName ?? string.Empty),
            new("sid", sessionKey),
            new("session_id", sessionKey),
            new(JwtRegisteredClaimNames.Jti, sessionKey)
        };

        if (!string.IsNullOrWhiteSpace(user.Email)) claims.Add(new Claim(ClaimTypes.Email, user.Email));
        claims.AddRange(roleNames.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Select(permission => new Claim(PermissionClaimTypes.Permission, permission)));

        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private async Task WriteLoginAuditAsync(ApplicationUserIdentity? user, string mobile, string sessionKey, bool succeeded, string? failureReason, DateTimeOffset? tokenExpiresAt, ClientRequestMetadata metadata, CancellationToken cancellationToken)
    {
        db.LoginAudits.Add(new LoginAuditDbRecord
        {
            Id = Guid.NewGuid(),
            UserId = user?.Id,
            Mobile = mobile,
            FullName = user?.FullName,
            SessionKey = sessionKey,
            Succeeded = succeeded,
            FailureReason = failureReason,
            IpAddress = metadata.IpAddress,
            UserAgent = string.IsNullOrWhiteSpace(metadata.UserAgent) ? null : metadata.UserAgent,
            LoggedInAt = DateTimeOffset.UtcNow,
            LastSeenAt = succeeded ? DateTimeOffset.UtcNow : null,
            TokenExpiresAt = tokenExpiresAt
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeMobile(string mobile)
    {
        var value = mobile.Trim().Replace(" ", "").Replace("-", "");
        if (value.StartsWith("+98", StringComparison.Ordinal)) value = "0" + value[3..];
        if (value.StartsWith("98", StringComparison.Ordinal) && value.Length == 12) value = "0" + value[2..];
        return value;
    }
}
