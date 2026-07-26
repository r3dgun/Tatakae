using Tatakae.Application.Contracts.Coupons;
using Tatakae.Application.Contracts.Embroidery;
using Tatakae.Application.Contracts.Notifications;
using Tatakae.Application.Contracts.Orders;
using Tatakae.Application.Contracts.Products;
using Tatakae.Application.Contracts.Reviews;
using Tatakae.Application.Contracts.Shipping;
using Tatakae.Application.Validation;

namespace Tatakae.Application.Tests;

public sealed class FormValidationTests
{
    [Fact]
    public void ObjectGraphValidator_ValidatesNestedCheckoutAddressAndItems()
    {
        var request = new CheckoutRequest
        {
            CustomerName = "علی رضایی",
            Mobile = "09121234567",
            ShippingMethodCode = "post-standard",
            ShippingAddress = new CheckoutAddressRequest
            {
                RecipientName = "علی رضایی",
                Mobile = "09121234567",
                Province = "تهران",
                City = "تهران",
                PostalCode = "123",
                AddressLine = "کوتاه"
            },
            Items =
            [
                new CheckoutItemRequest
                {
                    ProductId = Guid.NewGuid(),
                    VariantId = Guid.NewGuid(),
                    Quantity = 1,
                    Embroidery = ValidEmbroidery()
                }
            ]
        };

        var errors = ObjectGraphValidator.Validate(request);

        Assert.Contains(errors, x => ReferenceEquals(x.Instance, request.ShippingAddress) && x.MemberName == nameof(CheckoutAddressRequest.PostalCode));
        Assert.Contains(errors, x => ReferenceEquals(x.Instance, request.ShippingAddress) && x.MemberName == nameof(CheckoutAddressRequest.AddressLine));
    }

    [Fact]
    public void ObjectGraphValidator_ValidatesNestedProductVariantsAndDuplicateSku()
    {
        var variant = new AdminProductVariantRequest
        {
            Sku = "TEST-001",
            Size = "L",
            ColorName = "مشکی",
            ColorHex = "#111111",
            RegularPrice = 100_000,
            SalePrice = 120_000,
            StockQuantity = 2,
            ReservedQuantity = 3
        };
        var request = new AdminProductRequest
        {
            Name = "محصول تست معتبر",
            Slug = "محصول-تست-معتبر",
            CategoryId = Guid.NewGuid(),
            ShortDescription = new string('ش', 25),
            Description = new string('ت', 90),
            Material = "پنبه",
            Fit = "آزاد",
            CareGuide = "شست‌وشو با آب سرد",
            Images = [new() { Url = "https://example.com/image.jpg", AltText = "تصویر اصلی محصول", IsPrimary = true }],
            Variants = [variant, new() { Sku = "TEST-001", Size = "XL", ColorName = "سفید", ColorHex = "#FFFFFF", RegularPrice = 100_000 }]
        };

        var errors = ObjectGraphValidator.Validate(request);

        Assert.Contains(errors, x => ReferenceEquals(x.Instance, request) && x.MemberName == nameof(AdminProductRequest.Variants) && x.ErrorMessage.Contains("SKU"));
        Assert.Contains(errors, x => ReferenceEquals(x.Instance, variant) && x.MemberName == nameof(AdminProductVariantRequest.SalePrice));
        Assert.Contains(errors, x => ReferenceEquals(x.Instance, variant) && x.MemberName == nameof(AdminProductVariantRequest.ReservedQuantity));
    }

    [Fact]
    public void ObjectGraphValidator_ReportsCrossFieldRulesBesidePropertyErrors()
    {
        var request = new AdminProductRequest
        {
            Name = string.Empty,
            Slug = "محصول-تست",
            CategoryId = Guid.NewGuid(),
            ShortDescription = new string('ش', 25),
            Description = new string('ت', 90),
            Material = "پنبه",
            Fit = "آزاد",
            CareGuide = "شست‌وشو با آب سرد",
            Variants =
            [
                new() { Sku = "DUP-001", Size = "L", ColorName = "مشکی", ColorHex = "#111111", RegularPrice = 100_000 },
                new() { Sku = "dup-001", Size = "XL", ColorName = "سفید", ColorHex = "#FFFFFF", RegularPrice = 100_000 }
            ],
            Images = [new() { Url = "https://example.com/image.jpg", AltText = "تصویر اصلی محصول", IsPrimary = true }]
        };

        var errors = ObjectGraphValidator.Validate(request);

        Assert.Contains(errors, x => ReferenceEquals(x.Instance, request) && x.MemberName == nameof(AdminProductRequest.Name));
        Assert.Contains(errors, x => ReferenceEquals(x.Instance, request) && x.MemberName == nameof(AdminProductRequest.Variants) && x.ErrorMessage.Contains("SKU"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ProductValidation_AllowsBlankOptionalSizeGuideUrl(string sizeGuideUrl)
    {
        var request = new AdminProductRequest
        {
            Name = "محصول تست معتبر",
            Slug = "محصول-تست-معتبر",
            CategoryId = Guid.NewGuid(),
            ShortDescription = new string('ش', 25),
            Description = new string('ت', 90),
            Material = "پنبه",
            Fit = "آزاد",
            CareGuide = "شست‌وشو با آب سرد",
            SizeGuideUrl = sizeGuideUrl,
            Images = [new() { Url = "https://example.com/image.jpg", AltText = "تصویر اصلی محصول", IsPrimary = true }],
            Variants = [new() { Sku = "TEST-001", Size = "L", ColorName = "مشکی", ColorHex = "#111111", RegularPrice = 100_000 }]
        };

        var errors = ObjectGraphValidator.Validate(request);

        Assert.DoesNotContain(errors, x => ReferenceEquals(x.Instance, request) && x.MemberName == nameof(AdminProductRequest.SizeGuideUrl));
    }

    [Fact]
    public void CouponValidation_RejectsPercentageAboveOneHundredAndInvalidDateRange()
    {
        var request = new AdminCouponRequest
        {
            Code = "SUMMER-1405",
            Type = "Percentage",
            Value = 120,
            StartsAt = new DateTimeOffset(2026, 7, 20, 0, 0, 0, TimeSpan.Zero),
            EndsAt = new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero)
        };

        var errors = ObjectGraphValidator.Validate(request);

        Assert.Contains(errors, x => x.MemberName == nameof(AdminCouponRequest.Value));
        Assert.Contains(errors, x => x.MemberName == nameof(AdminCouponRequest.EndsAt));
    }

    [Fact]
    public void ShippingValidation_RejectsReversedDeliveryWindow()
    {
        var request = new UpsertManualShippingMethodRequest
        {
            Code = "express",
            Title = "ارسال سریع",
            Description = "تحویل سریع در شهرهای پشتیبانی‌شده",
            BasePrice = 100_000,
            FreeShippingThreshold = 50_000,
            EstimatedMinDays = 5,
            EstimatedMaxDays = 2
        };

        var errors = ObjectGraphValidator.Validate(request);

        Assert.Contains(errors, x => x.MemberName == nameof(UpsertManualShippingMethodRequest.EstimatedMaxDays));
        Assert.Contains(errors, x => x.MemberName == nameof(UpsertManualShippingMethodRequest.FreeShippingThreshold));
    }

    [Theory]
    [InlineData("۰۹۱۲-۱۲۳-۴۵۶۷", "09121234567")]
    [InlineData("+98 912 123 4567", "09121234567")]
    [InlineData("9121234567", "09121234567")]
    public void NormalizeIranianMobile_AcceptsCommonIranianFormats(string input, string expected)
        => Assert.Equal(expected, IranianInputNormalizer.NormalizeIranianMobile(input));

    [Theory]
    [InlineData("۱۲۳٬۴۵۶", true, 123456L)]
    [InlineData("100,500", true, 100500L)]
    [InlineData("-۱", false, 0L)]
    [InlineData("abc", false, 0L)]
    public void TryParseNonNegativeDecimal_HandlesPersianAndInvalidValues(string input, bool expectedValid, long expected)
    {
        var valid = IranianInputNormalizer.TryParseNonNegativeDecimal(input, out var actual);

        Assert.Equal(expectedValid, valid);
        if (expectedValid) Assert.Equal((decimal)expected, actual);
    }

    [Fact]
    public void NotificationValidation_RequiresRecipientAndValidActionUrl()
    {
        var request = new CreateNotificationRequest
        {
            Subject = "اعلان تست",
            Body = "متن اعلان تست",
            ActionUrl = "not a valid url"
        };

        var errors = ObjectGraphValidator.Validate(request);

        Assert.Contains(errors, x => x.MemberName == nameof(CreateNotificationRequest.Recipient));
        Assert.Contains(errors, x => x.MemberName == nameof(CreateNotificationRequest.ActionUrl));
    }

    [Fact]
    public void ArtworkModeration_RequiresReasonForRejectedStatus()
    {
        var request = new AdminArtworkModerationRequest { Status = "Rejected" };

        var errors = ObjectGraphValidator.Validate(request);

        Assert.Contains(errors, x => x.MemberName == nameof(AdminArtworkModerationRequest.RejectionReason));
    }

    [Fact]
    public void QuestionModeration_RequiresAnswerForAnsweredStatus()
    {
        var request = new AdminQuestionModerationRequest { Status = "Answered" };

        var errors = ObjectGraphValidator.Validate(request);

        Assert.Contains(errors, x => x.MemberName == nameof(AdminQuestionModerationRequest.AnswerText));
    }

    [Fact]
    public void ProductQuestion_RejectsMalformedIranianMobile()
    {
        var request = new SubmitProductQuestionRequest
        {
            ProductId = Guid.NewGuid(),
            AuthorName = "کاربر تست",
            Mobile = "0912",
            QuestionText = "آیا این محصول قابل شست‌وشو است؟"
        };

        var errors = ObjectGraphValidator.Validate(request);

        Assert.Contains(errors, x => x.MemberName == nameof(SubmitProductQuestionRequest.Mobile));
    }

    [Theory]
    [InlineData("۰۹۱۲۱۲۳۴۵۶۷")]
    [InlineData("٠٩١٢١٢٣٤٥٦٧")]
    [InlineData("+۹۸۹۱۲۱۲۳۴۵۶۷")]
    public void MobileValidation_AcceptsPersianAndArabicIndicDigits(string mobile)
    {
        var request = new SubmitProductQuestionRequest
        {
            ProductId = Guid.NewGuid(),
            AuthorName = "کاربر تست",
            Mobile = mobile,
            QuestionText = "آیا این محصول قابل شست‌وشو است؟"
        };

        var errors = ObjectGraphValidator.Validate(request);

        Assert.DoesNotContain(errors, x => x.MemberName == nameof(SubmitProductQuestionRequest.Mobile));
    }

    private static EmbroideryCustomizationRequest ValidEmbroidery() => new()
    {
        ProductId = Guid.NewGuid(),
        VariantId = Guid.NewGuid(),
        GarmentType = "TShirt",
        GarmentSize = "L",
        GarmentColorHex = "#111111",
        Placement = "CenterChest",
        WidthCm = 9,
        HeightCm = 9,
        ThreadColorCount = 1,
        ThreadColorHexes = ["#FFFFFF"],
        DesignSource = "Motif",
        MotifKey = "dragon"
    };
}
