using Microsoft.Extensions.Logging;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Legal;
using Tatakae.Application.Interfaces.Gateways;
using Tatakae.Application.Interfaces.Services;

namespace Tatakae.Application.Services;

public sealed class LegalContentService(
    ILegalContentGateway gateway,
    ILogger<LegalContentService> logger) : ILegalContentService
{
    public Task<ResultDto<IReadOnlyCollection<StorePolicyPageDto>>> GetPublishedPagesAsync(CancellationToken cancellationToken = default)
        => ApplicationServiceResult.ExecuteAsync(
            () => gateway.GetPublishedPagesAsync(cancellationToken),
            "صفحات منتشرشده با موفقیت دریافت شدند.",
            "خطایی در دریافت صفحات منتشرشده رخ داده است.",
            "legal_published_pages_failed",
            logger);

    public Task<ResultDto<StorePolicyPageDto>> GetPublishedPageAsync(string slug, CancellationToken cancellationToken = default)
        => string.IsNullOrWhiteSpace(slug)
            ? Task.FromResult(new ResultDto<StorePolicyPageDto>().ValidationFailed("Slug صفحه الزامی است.", "legal_slug_required"))
            : ApplicationServiceResult.ExecuteNullableAsync(
                () => gateway.GetPublishedPageAsync(slug, cancellationToken),
                "صفحه قانونی با موفقیت دریافت شد.",
                "خطایی در دریافت صفحه قانونی رخ داده است.",
                "legal_page_get_failed",
                logger,
                ResultStatus.NotFound,
                "صفحه قانونی پیدا نشد.",
                "legal_page_not_found");

    public Task<ResultDto<IReadOnlyCollection<StorePolicyPageDto>>> GetAllPagesAsync(CancellationToken cancellationToken = default)
        => ApplicationServiceResult.ExecuteAsync(
            () => gateway.GetAllPagesAsync(cancellationToken),
            "صفحات قانونی با موفقیت دریافت شدند.",
            "خطایی در دریافت صفحات قانونی رخ داده است.",
            "legal_pages_get_failed",
            logger);

    public Task<ResultDto<StorePolicyPageDto>> UpsertPageAsync(string? currentSlug, UpsertStorePolicyPageRequest request, CancellationToken cancellationToken = default)
        => request is null
            ? Task.FromResult(new ResultDto<StorePolicyPageDto>().ValidationFailed("اطلاعات صفحه قانونی ارسال نشده است.", "legal_request_required"))
            : ApplicationServiceResult.ExecuteAsync(
                () => gateway.UpsertPageAsync(currentSlug, request, cancellationToken),
                "صفحه قانونی با موفقیت ذخیره شد.",
                "خطایی در ذخیره صفحه قانونی رخ داده است.",
                "legal_page_save_failed",
                logger);

    public Task<ResultDto<ContactMessageDto>> SubmitContactAsync(SubmitContactMessageRequest request, string? ipAddress, CancellationToken cancellationToken = default)
        => request is null
            ? Task.FromResult(new ResultDto<ContactMessageDto>().ValidationFailed("اطلاعات پیام تماس ارسال نشده است.", "contact_request_required"))
            : ApplicationServiceResult.ExecuteAsync(
                () => gateway.SubmitContactAsync(request, ipAddress, cancellationToken),
                "پیام شما با موفقیت ثبت شد.",
                "خطایی در ثبت پیام تماس رخ داده است.",
                "contact_submit_failed",
                logger);

    public Task<ResultDto<IReadOnlyCollection<ContactMessageDto>>> GetContactMessagesAsync(CancellationToken cancellationToken = default)
        => ApplicationServiceResult.ExecuteAsync(
            () => gateway.GetContactMessagesAsync(cancellationToken),
            "پیام‌های تماس با موفقیت دریافت شدند.",
            "خطایی در دریافت پیام‌های تماس رخ داده است.",
            "contact_messages_get_failed",
            logger);

    public Task<ResultDto<ContactMessageDto>> UpdateContactMessageAsync(Guid id, UpdateContactMessageStatusRequest request, CancellationToken cancellationToken = default)
        => id == Guid.Empty
            ? Task.FromResult(new ResultDto<ContactMessageDto>().ValidationFailed("شناسه پیام تماس معتبر نیست.", "contact_message_id_invalid"))
            : request is null
                ? Task.FromResult(new ResultDto<ContactMessageDto>().ValidationFailed("اطلاعات وضعیت پیام تماس ارسال نشده است.", "contact_status_request_required"))
                : ApplicationServiceResult.ExecuteAsync(
                    () => gateway.UpdateContactMessageAsync(id, request, cancellationToken),
                    "وضعیت پیام تماس با موفقیت به‌روزرسانی شد.",
                    "خطایی در به‌روزرسانی پیام تماس رخ داده است.",
                    "contact_message_update_failed",
                    logger);
}
