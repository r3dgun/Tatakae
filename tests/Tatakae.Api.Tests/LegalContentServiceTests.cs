using Microsoft.EntityFrameworkCore;
using Tatakae.Infrastructure.Gateways;
using Tatakae.Application.Contracts.Legal;
using Tatakae.Infrastructure.Persistence;
using Tatakae.Infrastructure.Persistence.Models;

namespace Tatakae.Api.Tests;

public sealed class LegalContentServiceTests
{
    [Fact]
    public async Task GetPublishedPagesAsync_ReturnsOnlyPublishedPages_InConfiguredOrder()
    {
        await using var db = CreateDbContext();
        db.StorePolicyPages.AddRange(
            Policy("privacy", "حریم خصوصی", isPublished: false, sortOrder: 0),
            Policy("contact", "تماس با ما", isPublished: true, sortOrder: 20),
            Policy("about", "درباره ما", isPublished: true, sortOrder: 10));
        await db.SaveChangesAsync();
        var service = new EfLegalContentGateway(db);

        var pages = await service.GetPublishedPagesAsync();

        Assert.Collection(
            pages,
            page => Assert.Equal("about", page.Slug),
            page => Assert.Equal("contact", page.Slug));
        Assert.DoesNotContain(pages, page => page.Slug == "privacy");
    }

    [Theory]
    [InlineData("rules", "terms")]
    [InlineData("shipping-policy", "shipping")]
    [InlineData("  حريم‌خصوصي  ", "حریم-خصوصی")]
    public async Task GetPublishedPageAsync_NormalizesAliasesAndPersianSlug(string requestedSlug, string storedSlug)
    {
        await using var db = CreateDbContext();
        db.StorePolicyPages.Add(Policy(storedSlug, "صفحه تست"));
        await db.SaveChangesAsync();
        var service = new EfLegalContentGateway(db);

        var page = await service.GetPublishedPageAsync(requestedSlug);

        Assert.NotNull(page);
        Assert.Equal(storedSlug, page.Slug);
    }

    [Fact]
    public async Task GetPublishedPageAsync_DoesNotReturnDraftPage()
    {
        await using var db = CreateDbContext();
        db.StorePolicyPages.Add(Policy("privacy", "حریم خصوصی", isPublished: false));
        await db.SaveChangesAsync();
        var service = new EfLegalContentGateway(db);

        var page = await service.GetPublishedPageAsync("privacy");

        Assert.Null(page);
    }

    [Fact]
    public async Task UpsertPageAsync_RenamesExistingPageWithoutCreatingDuplicate()
    {
        await using var db = CreateDbContext();
        var original = Policy("old-page", "عنوان قبلی");
        db.StorePolicyPages.Add(original);
        await db.SaveChangesAsync();
        var service = new EfLegalContentGateway(db);
        var request = ValidPageRequest("حریم خصوصی");

        var result = await service.UpsertPageAsync("old-page", request);

        Assert.Equal(original.Id, result.Id);
        Assert.Equal("حریم-خصوصی", result.Slug);
        var stored = Assert.Single(await db.StorePolicyPages.AsNoTracking().ToListAsync());
        Assert.Equal(original.Id, stored.Id);
        Assert.Equal("حریم-خصوصی", stored.Slug);
    }

    [Fact]
    public async Task UpsertPageAsync_WhenTargetSlugExists_ThrowsDuplicateSlugError()
    {
        await using var db = CreateDbContext();
        db.StorePolicyPages.AddRange(
            Policy("first-page", "صفحه اول"),
            Policy("second-page", "صفحه دوم"));
        await db.SaveChangesAsync();
        var service = new EfLegalContentGateway(db);
        var request = ValidPageRequest("second-page");

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.UpsertPageAsync("first-page", request));

        Assert.Contains("تکراری", exception.Message);
        Assert.Equal(2, await db.StorePolicyPages.CountAsync());
    }

    [Fact]
    public async Task UpsertPageAsync_UsesSeoFallbacksAndEnforcesSearchSnippetLengths()
    {
        await using var db = CreateDbContext();
        var service = new EfLegalContentGateway(db);
        var request = ValidPageRequest("long-seo-page");
        request.Title = new string('ع', 90);
        request.Summary = new string('ت', 220);
        request.SeoTitle = null;
        request.SeoDescription = null;

        var result = await service.UpsertPageAsync(null, request);

        Assert.NotNull(result.SeoTitle);
        Assert.NotNull(result.SeoDescription);
        Assert.Equal(65, result.SeoTitle!.Length);
        Assert.Equal(160, result.SeoDescription!.Length);
        Assert.Equal(request.Title[..65], result.SeoTitle);
        Assert.Equal(request.Summary[..160], result.SeoDescription);
    }

    [Fact]
    public async Task SubmitContactAsync_NormalizesIranianMobileAndPersistsRequestMetadata()
    {
        await using var db = CreateDbContext();
        var service = new EfLegalContentGateway(db);
        var request = new SubmitContactMessageRequest
        {
            FullName = "  کاربر تست  ",
            Mobile = "+989121234567",
            Email = "  customer@example.com  ",
            Subject = "  پیگیری سفارش  ",
            Message = "  این پیام برای تست ثبت فرم تماس فروشگاه است.  "
        };

        var result = await service.SubmitContactAsync(request, "127.0.0.1");

        Assert.Equal("کاربر تست", result.FullName);
        Assert.Equal("09121234567", result.Mobile);
        Assert.Equal("customer@example.com", result.Email);
        Assert.Equal("پیگیری سفارش", result.Subject);
        Assert.Equal("new", result.Status);

        var stored = Assert.Single(await db.ContactMessages.AsNoTracking().ToListAsync());
        Assert.Equal("127.0.0.1", stored.IpAddress);
        Assert.Equal("09121234567", stored.Mobile);
    }

    [Fact]
    public async Task UpdateContactMessageAsync_NormalizesStatusAndSetsAnsweredAt()
    {
        await using var db = CreateDbContext();
        var record = new ContactMessageDbRecord
        {
            Id = Guid.NewGuid(),
            FullName = "کاربر تست",
            Mobile = "09121234567",
            Subject = "پیگیری",
            Message = "متن پیام تستی برای پیگیری سفارش",
            Status = "new",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
        };
        db.ContactMessages.Add(record);
        await db.SaveChangesAsync();
        var service = new EfLegalContentGateway(db);

        var result = await service.UpdateContactMessageAsync(record.Id, new UpdateContactMessageStatusRequest
        {
            Status = "answered",
            AdminNote = "پاسخ از طریق تماس تلفنی ارسال شد."
        });

        Assert.Equal("answered", result.Status);
        Assert.NotNull(result.AnsweredAt);
        Assert.Equal("پاسخ از طریق تماس تلفنی ارسال شد.", result.AdminNote);
    }

    [Fact]
    public async Task UpdateContactMessageAsync_WhenMessageDoesNotExist_ThrowsKeyNotFound()
    {
        await using var db = CreateDbContext();
        var service = new EfLegalContentGateway(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateContactMessageAsync(
            Guid.NewGuid(),
            new UpdateContactMessageStatusRequest { Status = "seen" }));
    }

    private static TatakaeDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TatakaeDbContext>()
            .UseInMemoryDatabase($"tatakae-phase12-legal-tests-{Guid.NewGuid():N}")
            .EnableSensitiveDataLogging()
            .Options;

        return new TatakaeDbContext(options);
    }

    private static StorePolicyPageDbRecord Policy(
        string slug,
        string title,
        bool isPublished = true,
        int sortOrder = 0)
        => new()
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Title = title,
            Summary = "خلاصه معتبر برای صفحه قانونی فروشگاه Tatakae و توضیح هدف این صفحه.",
            Body = "<p>این متن کامل و معتبر برای تست محتوای صفحه قانونی فروشگاه Tatakae نوشته شده است.</p>",
            SeoTitle = $"{title} | Tatakae",
            SeoDescription = "توضیح سئوی معتبر برای صفحه قانونی فروشگاه Tatakae و اطلاعات مورد نیاز مشتری.",
            IsPublished = isPublished,
            SortOrder = sortOrder,
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

    private static UpsertStorePolicyPageRequest ValidPageRequest(string slug)
        => new()
        {
            Slug = slug,
            Title = "عنوان صفحه قانونی فروشگاه",
            Summary = "خلاصه معتبر و کامل برای صفحه قانونی فروشگاه Tatakae نوشته شده است.",
            Body = "<p>این متن کامل برای صفحه قانونی فروشگاه نوشته شده و حداقل طول لازم برای ذخیره‌سازی و تست را دارد.</p>",
            SeoTitle = "عنوان سئوی صفحه قانونی | Tatakae",
            SeoDescription = "توضیح سئوی صفحه قانونی فروشگاه Tatakae برای نمایش صحیح در نتایج موتورهای جست‌وجو.",
            IsPublished = true,
            SortOrder = 10
        };
}
