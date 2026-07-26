using Microsoft.EntityFrameworkCore;
using Tatakae.Application.Interfaces.Gateways;
using Tatakae.Application.Contracts.Legal;
using Tatakae.Application.Seo;
using Tatakae.Infrastructure.Persistence;
using Tatakae.Infrastructure.Persistence.Models;

namespace Tatakae.Infrastructure.Gateways;

public sealed class EfLegalContentGateway(TatakaeDbContext db) : ILegalContentGateway
{
    public async Task<IReadOnlyCollection<StorePolicyPageDto>> GetPublishedPagesAsync(CancellationToken cancellationToken = default)
    {
        var pages = await db.StorePolicyPages.AsNoTracking()
            .Where(x => x.IsPublished)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Title)
            .ToListAsync(cancellationToken);
        return pages.Select(ToDto).ToArray();
    }

    public async Task<StorePolicyPageDto?> GetPublishedPageAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeSlug(slug);
        var page = await db.StorePolicyPages.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == normalized && x.IsPublished, cancellationToken);
        return page is null ? null : ToDto(page);
    }

    public async Task<IReadOnlyCollection<StorePolicyPageDto>> GetAllPagesAsync(CancellationToken cancellationToken = default)
    {
        var pages = await db.StorePolicyPages.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Title)
            .ToListAsync(cancellationToken);
        return pages.Select(ToDto).ToArray();
    }

    public async Task<StorePolicyPageDto> UpsertPageAsync(string? currentSlug, UpsertStorePolicyPageRequest request, CancellationToken cancellationToken = default)
    {
        var slug = NormalizeSlug(request.Slug);
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Slug صفحه معتبر نیست.");

        var normalizedCurrentSlug = string.IsNullOrWhiteSpace(currentSlug) ? slug : NormalizeSlug(currentSlug);
        var page = await db.StorePolicyPages.FirstOrDefaultAsync(x => x.Slug == normalizedCurrentSlug, cancellationToken);
        page ??= await db.StorePolicyPages.FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);
        if (page is null)
        {
            page = new StorePolicyPageDbRecord { Id = Guid.NewGuid(), Slug = slug };
            db.StorePolicyPages.Add(page);
        }
        else if (!page.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase)
                 && await db.StorePolicyPages.AnyAsync(x => x.Id != page.Id && x.Slug == slug, cancellationToken))
        {
            throw new ArgumentException("Slug صفحه تکراری است.");
        }

        page.Slug = slug;
        page.Title = request.Title.Trim();
        page.Summary = request.Summary.Trim();
        page.Body = request.Body.Trim();
        page.SeoTitle = Limit(string.IsNullOrWhiteSpace(request.SeoTitle) ? request.Title : request.SeoTitle, 65);
        page.SeoDescription = Limit(string.IsNullOrWhiteSpace(request.SeoDescription) ? request.Summary : request.SeoDescription, 160);
        page.IsPublished = request.IsPublished;
        page.SortOrder = request.SortOrder;
        page.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return ToDto(page);
    }

    public async Task<ContactMessageDto> SubmitContactAsync(SubmitContactMessageRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var record = new ContactMessageDbRecord
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Mobile = NormalizeMobile(request.Mobile),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Subject = request.Subject.Trim(),
            Message = request.Message.Trim(),
            Status = "new",
            IpAddress = ipAddress,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.ContactMessages.Add(record);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(record);
    }

    public async Task<IReadOnlyCollection<ContactMessageDto>> GetContactMessagesAsync(CancellationToken cancellationToken = default)
    {
        var messages = await db.ContactMessages.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);
        return messages.Select(ToDto).ToArray();
    }

    public async Task<ContactMessageDto> UpdateContactMessageAsync(Guid id, UpdateContactMessageStatusRequest request, CancellationToken cancellationToken = default)
    {
        var record = await db.ContactMessages.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("پیام تماس پیدا نشد.");

        record.Status = request.Status.Trim().ToLowerInvariant();
        record.AdminNote = request.AdminNote;
        if (record.Status is "seen" && record.SeenAt is null) record.SeenAt = DateTimeOffset.UtcNow;
        if (record.Status is "answered" or "closed") record.AnsweredAt ??= DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(record);
    }

    private static string NormalizeSlug(string slug) => SeoSlug.Normalize(slug) switch
    {
        "shipping-policy" => "shipping",
        "rules" => "terms",
        var value => value
    };


    private static string Limit(string value, int maxLength)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength].TrimEnd();
    }

    private static string NormalizeMobile(string mobile)
    {
        var value = mobile.Trim().Replace(" ", "").Replace("-", "");
        if (value.StartsWith("+98", StringComparison.Ordinal)) value = "0" + value[3..];
        if (value.StartsWith("98", StringComparison.Ordinal) && value.Length == 12) value = "0" + value[2..];
        return value;
    }

    private static StorePolicyPageDto ToDto(StorePolicyPageDbRecord page) => new(page.Id, page.Slug, page.Title, page.Summary, page.Body, page.SeoTitle, page.SeoDescription, page.IsPublished, page.SortOrder, page.UpdatedAt);
    private static ContactMessageDto ToDto(ContactMessageDbRecord message) => new(message.Id, message.FullName, message.Mobile, message.Email, message.Subject, message.Message, message.Status, message.CreatedAt, message.AnsweredAt, message.AdminNote);
}
