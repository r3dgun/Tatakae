using System.ComponentModel.DataAnnotations;
using Tatakae.Application.Contracts.Categories;
using Tatakae.Application.Contracts.Legal;
using Tatakae.Application.Seo;

namespace Tatakae.Application.Tests;

public sealed class SeoSlugTests
{
    [Theory]
    [InlineData("تی شرت گلدوزی", "تی-شرت-گلدوزی")]
    [InlineData("Hoodie Oversize", "hoodie-oversize")]
    [InlineData("مدل_۱۴۰۵", "مدل-1405")]
    [InlineData("  حريم‌خصوصي  ", "حریم-خصوصی")]
    public void Normalize_SupportsPersianAndEnglish(string input, string expected)
        => Assert.Equal(expected, SeoSlug.Normalize(input));

    [Fact]
    public void AdminCategoryRequest_AcceptsPersianSlug()
    {
        var model = new AdminCategoryRequest
        {
            Name = "تی‌شرت",
            Slug = "تی-شرت-گلدوزی",
            Seo = new() { MetaTitle = "تی‌شرت گلدوزی | Tatakae", MetaDescription = "خرید آنلاین تی‌شرت گلدوزی با انتخاب سایز و رنگ در فروشگاه Tatakae." }
        };

        var results = new List<ValidationResult>();
        Assert.True(Validator.TryValidateObject(model, new ValidationContext(model), results, true));
    }


    [Theory]
    [InlineData(null, "")]
    [InlineData("---", "")]
    [InlineData("T-Shirt!!! مدل ١٢٣", "t-shirt-مدل-123")]
    [InlineData("كالاى ويژه", "کالای-ویژه")]
    [InlineData("a___b---c", "a-b-c")]
    [InlineData("Ｆｕｌｌ　Ｗｉｄｔｈ ۱۲۳", "full-width-123")]
    public void Normalize_HandlesEmptyInvalidMixedAndArabicInputs(string? input, string expected)
        => Assert.Equal(expected, SeoSlug.Normalize(input));

    [Fact]
    public void Normalize_IsIdempotent()
    {
        const string input = "  تی شرت_گلدوزی ۱۴۰۵  ";
        var normalized = SeoSlug.Normalize(input);

        Assert.Equal(normalized, SeoSlug.Normalize(normalized));
    }

    [Theory]
    [InlineData("product\\test\\", "/product/test")]
    [InlineData("/product/test#reviews", "/product/test")]
    [InlineData("   ", "/fallback")]
    [InlineData("/", "/")]
    public void NormalizeCanonicalPath_HandlesBackslashesFragmentsEmptyAndRoot(string input, string expected)
        => Assert.Equal(expected, SeoSlug.NormalizeCanonicalPath(input, "/fallback"));

    [Fact]
    public void StorePolicyRequest_AcceptsPersianSlugAndValidSeoFields()
    {
        var model = new UpsertStorePolicyPageRequest
        {
            Slug = "قوانین-مرجوعی",
            Title = "قوانین مرجوعی فروشگاه",
            Summary = "خلاصه کامل قوانین مرجوعی برای سفارش‌های آماده و شخصی‌سازی‌شده فروشگاه.",
            Body = "<p>این متن کامل قوانین مرجوعی فروشگاه است و شرایط سفارش‌های شخصی‌سازی‌شده را توضیح می‌دهد.</p>",
            SeoTitle = "قوانین مرجوعی فروشگاه | Tatakae",
            SeoDescription = "شرایط مرجوعی سفارش‌های آماده و محصولات گلدوزی شخصی‌سازی‌شده در فروشگاه Tatakae."
        };

        var results = new List<ValidationResult>();

        Assert.True(Validator.TryValidateObject(model, new ValidationContext(model), results, true));
        Assert.Empty(results);
    }

    [Theory]
    [InlineData("product/test")]
    [InlineData("slug@invalid")]
    [InlineData("-شروع-نامعتبر")]
    public void StorePolicyRequest_RejectsInvalidSlug(string slug)
    {
        var model = new UpsertStorePolicyPageRequest
        {
            Slug = slug,
            Title = "عنوان معتبر صفحه",
            Summary = "خلاصه معتبر و کامل برای صفحه قانونی فروشگاه Tatakae نوشته شده است.",
            Body = "<p>این متن کامل برای صفحه قانونی فروشگاه نوشته شده و حداقل طول لازم را دارد.</p>"
        };
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, new ValidationContext(model), results, true);

        Assert.False(isValid);
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(UpsertStorePolicyPageRequest.Slug)));
    }

    [Theory]
    [InlineData("product/test", "/product/test")]
    [InlineData("https://example.com/product/test?utm=1", "/product/test")]
    [InlineData("http://example.com/product/test#details", "/product/test")]
    [InlineData("//product//test/", "/product/test")]
    public void NormalizeCanonicalPath_ReturnsCleanRelativePath(string input, string expected)
        => Assert.Equal(expected, SeoSlug.NormalizeCanonicalPath(input, "/fallback"));
}
