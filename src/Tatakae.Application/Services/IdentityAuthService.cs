using Microsoft.Extensions.Logging;
using Tatakae.Application.Contracts.Account;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Interfaces.Gateways;
using Tatakae.Application.Interfaces.Services;

namespace Tatakae.Application.Services;

/// <summary>Authentication use cases. ASP.NET Identity and JWT details are behind IIdentityAuthGateway.</summary>
public sealed class IdentityAuthService(
    IIdentityAuthGateway gateway,
    ILogger<IdentityAuthService> logger) : IIdentityAuthService
{
    public Task<ResultDto<AccountSessionDto>> RegisterAsync(
        RegisterCustomerRequest request,
        ClientRequestMetadata metadata,
        CancellationToken cancellationToken = default)
        => request is null
            ? Task.FromResult(new ResultDto<AccountSessionDto>().ValidationFailed("اطلاعات ثبت‌نام ارسال نشده است.", "auth_register_request_required"))
            : ApplicationServiceResult.ExecuteAsync(
                () => gateway.RegisterAsync(request, metadata ?? ClientRequestMetadata.Empty, cancellationToken),
                "ثبت‌نام با موفقیت انجام شد.",
                "خطایی در ثبت‌نام رخ داده است.",
                "auth_register_failed",
                logger);

    public Task<ResultDto<AccountSessionDto>> LoginAsync(
        LoginRequest request,
        ClientRequestMetadata metadata,
        CancellationToken cancellationToken = default)
        => request is null
            ? Task.FromResult(new ResultDto<AccountSessionDto>().ValidationFailed("اطلاعات ورود ارسال نشده است.", "auth_login_request_required"))
            : ApplicationServiceResult.ExecuteNullableAsync(
                () => gateway.LoginAsync(request, metadata ?? ClientRequestMetadata.Empty, cancellationToken),
                "ورود با موفقیت انجام شد.",
                "خطایی در ورود رخ داده است.",
                "auth_login_failed",
                logger,
                ResultStatus.Unauthorized,
                "شماره موبایل یا رمز عبور اشتباه است.",
                "auth_invalid_credentials");

    public Task<ResultDto<AccountSessionDto>> CurrentAsync(
        AuthenticatedSessionContext session,
        CancellationToken cancellationToken = default)
        => session is null || session.UserId == Guid.Empty
            ? Task.FromResult(new ResultDto<AccountSessionDto>().Unauthorized("نشست کاربر معتبر نیست.", "auth_session_invalid"))
            : ApplicationServiceResult.ExecuteNullableAsync(
                () => gateway.CurrentAsync(session, cancellationToken),
                "نشست کاربر با موفقیت دریافت شد.",
                "خطایی در دریافت نشست کاربر رخ داده است.",
                "auth_current_failed",
                logger,
                ResultStatus.Unauthorized,
                "نشست کاربر معتبر نیست یا منقضی شده است.",
                "auth_session_expired");

    public Task<ResultDto> LogoutAsync(
        AuthenticatedSessionContext session,
        CancellationToken cancellationToken = default)
        => session is null || session.UserId == Guid.Empty || string.IsNullOrWhiteSpace(session.SessionKey)
            ? Task.FromResult(new ResultDto().Unauthorized("نشست کاربر معتبر نیست.", "auth_session_invalid"))
            : ApplicationServiceResult.ExecuteAsync(
                () => gateway.LogoutAsync(session, cancellationToken),
                "خروج از حساب با موفقیت انجام شد.",
                "خطایی در خروج از حساب رخ داده است.",
                "auth_logout_failed",
                logger);
}
