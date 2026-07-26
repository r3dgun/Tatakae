using Tatakae.Domain.Entities;
using Tatakae.Domain.Enums;

namespace Tatakae.Infrastructure.Seeding;

internal static class StoreSeed
{
    internal static readonly Guid TShirtCategoryId = Guid.Parse("aa111111-1111-1111-1111-111111111111");
    internal static readonly Guid HoodieCategoryId = Guid.Parse("aa222222-2222-2222-2222-222222222222");
    internal static readonly Guid SweatshirtCategoryId = Guid.Parse("aa333333-3333-3333-3333-333333333333");
    internal static readonly Guid PoloCategoryId = Guid.Parse("aa444444-4444-4444-4444-444444444444");

    internal static IReadOnlyCollection<Category> CreateCategories() =>
    [
        Category(TShirtCategoryId, "تی‌شرت گلدوزی", "embroidered-tshirts", "تی‌شرت‌های پنبه‌ای برای گلدوزی سبک تا متوسط؛ مناسب لوگو، تایپوگرافی و طرح‌های خطی.", "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=900&h=700&fit=crop", 1),
        Category(HoodieCategoryId, "هودی گلدوزی", "embroidered-hoodies", "هودی‌های گرم و باکیفیت با محدوده گلدوزی استاندارد روی سینه، آستین یا پشت یقه.", "https://images.unsplash.com/photo-1556821840-3a63f95609a7?w=900&h=700&fit=crop", 2),
        Category(SweatshirtCategoryId, "دورس و سویشرت", "embroidered-sweatshirts", "دورس‌های فیت‌دار و اورسایز مناسب گلدوزی‌های ماندگار و مینیمال.", "https://images.unsplash.com/photo-1620799140408-edc6dcb6d633?w=900&h=700&fit=crop", 3),
        Category(PoloCategoryId, "پولوشرت", "embroidered-polos", "پولوشرت‌های سازمانی و روزمره برای گلدوزی لوگو و نشان برند.", "https://images.unsplash.com/photo-1625910513391-5fc5d4b0c9e1?w=900&h=700&fit=crop", 4)
    ];

    internal static IReadOnlyCollection<Product> CreateProducts() =>
    [
        Product(Guid.Parse("10000000-0000-0000-0000-000000000001"), "تی‌شرت Premium Cotton", "premium-cotton-embroidered-tshirt", ApparelCategory.TShirt, TShirtCategoryId, 890_000m,
            "تی‌شرت ۲۲۰ گرمی با پنبه شانه‌شده؛ انتخاب استاندارد برای گلدوزی لوگو و طرح‌های مینیمال.",
            "این تی‌شرت با پارچه پنبه شانه‌شده و دوخت تقویت‌شده تولید شده است تا گلدوزی روی سینه یا آستین، فرم لباس را حفظ کند. برای لوگوهای تجاری، حروف اول نام، نمادهای ساده و طرح‌های خطی بهترین نتیجه را می‌دهد.",
            "۱۰۰٪ پنبه شانه‌شده", "Regular Fit", "شست‌وشو با آب سرد و پشت‌ورو. اتو مستقیم روی گلدوزی انجام نشود.",
            "https://example.com/size-guides/tshirt.pdf", true,
            [Variant("TT-PRM-BLK-S", "S", "مشکی", "#171717", 890_000m, null, 14), Variant("TT-PRM-BLK-M", "M", "مشکی", "#171717", 890_000m, null, 8), Variant("TT-PRM-CRM-L", "L", "کرم", "#EADDC8", 890_000m, 825_000m, 6), Variant("TT-PRM-NVY-XL", "XL", "سرمه‌ای", "#172554", 890_000m, null, 3)],
            ["مینیمال", "پنبه", "گلدوزی سینه"], "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=900&h=1100&fit=crop"),

        Product(Guid.Parse("10000000-0000-0000-0000-000000000002"), "تی‌شرت اورسایز Heavyweight", "heavyweight-oversize-embroidered-tshirt", ApparelCategory.TShirt, TShirtCategoryId, 1_090_000m,
            "تی‌شرت اورسایز ۲۵۰ گرمی با سطح پایدار برای گلدوزی طرح‌های متوسط و مدرن.",
            "فرم اورسایز این تی‌شرت، یک بوم مناسب برای گلدوزی‌های خلاقانه است. پارچه سنگین‌تر، افت کنترل‌شده دارد و برای برندهای پوشاک، تیم‌ها یا سفارش‌های شخصی‌سازی‌شده انتخابی ممتاز محسوب می‌شود.",
            "پنبه Heavyweight", "Oversized Fit", "با آب سرد بشویید؛ برای دوام بیشتر از خشک‌کن داغ استفاده نکنید.",
            "https://example.com/size-guides/oversize.pdf", true,
            [Variant("TT-HVY-WHT-M", "M", "سفید", "#F7F7F2", 1_090_000m, null, 9), Variant("TT-HVY-GRY-L", "L", "طوسی", "#6B7280", 1_090_000m, null, 7), Variant("TT-HVY-BLK-XL", "XL", "مشکی", "#111111", 1_090_000m, 995_000m, 4)],
            ["اورسایز", "۲۵۰ گرم", "استریت‌ور"], "https://images.unsplash.com/photo-1503341504253-dff4815485f1?w=900&h=1100&fit=crop"),

        Product(Guid.Parse("10000000-0000-0000-0000-000000000003"), "هودی Essential Fleece", "essential-fleece-embroidered-hoodie", ApparelCategory.Hoodie, HoodieCategoryId, 1_790_000m,
            "هودی سه‌نخ با داخل نرم؛ مناسب گلدوزی سینه، آستین و پشت یقه.",
            "هودی Essential Fleece با پارچه سه‌نخ و کلاه دولایه ساخته شده است. به‌دلیل ثبات بافت و ضخامت کافی، گلدوزی روی آن تمیز، برجسته و ماندگار دیده می‌شود. این مدل برای سفارش‌های پاییز و زمستان و لباس تیمی ایده‌آل است.",
            "۸۰٪ پنبه / ۲۰٪ پلی‌استر", "Relaxed Fit", "با آب سرد، پشت‌ورو و دور ملایم شسته شود. از اتو و حرارت مستقیم روی طرح اجتناب کنید.",
            "https://example.com/size-guides/hoodie.pdf", true,
            [Variant("HD-ESS-BLK-M", "M", "مشکی", "#111111", 1_790_000m, null, 8), Variant("HD-ESS-OLV-L", "L", "زیتونی", "#46513C", 1_790_000m, null, 5), Variant("HD-ESS-CRM-XL", "XL", "کرم", "#E9DFCF", 1_790_000m, 1_650_000m, 2)],
            ["سه‌نخ", "هودی", "گلدوزی لوگو"], "https://images.unsplash.com/photo-1556821840-3a63f95609a7?w=900&h=1100&fit=crop"),

        Product(Guid.Parse("10000000-0000-0000-0000-000000000004"), "دورس Crewneck Studio", "studio-crewneck-embroidered-sweatshirt", ApparelCategory.Sweatshirt, SweatshirtCategoryId, 1_460_000m,
            "دورس یقه‌گرد حرفه‌ای برای گلدوزی‌های ظریف، متن و مونگرام.",
            "دورس Studio برای پروژه‌هایی طراحی شده که به ظاهر مرتب و مینیمال نیاز دارند. بافت داخلی نرم و سطح بیرونی پایدار آن، مناسب نشان‌های سینه، حروف و گلدوزی‌های حداکثر ۱۲ سانتی‌متر است.",
            "دورس پنبه‌ای ۲۸۰ گرم", "Boxy Fit", "با رنگ‌های مشابه شسته شود. هنگام اتوکشی از یک لایه پارچه محافظ روی گلدوزی استفاده کنید.",
            "https://example.com/size-guides/sweatshirt.pdf", false,
            [Variant("SW-STU-BRG-M", "M", "زرشکی", "#6B1D2A", 1_460_000m, null, 5), Variant("SW-STU-GRY-L", "L", "طوسی", "#9CA3AF", 1_460_000m, null, 6), Variant("SW-STU-NVY-XL", "XL", "سرمه‌ای", "#1E3A5F", 1_460_000m, null, 4)],
            ["دورس", "یقه‌گرد", "مونگرام"], "https://images.unsplash.com/photo-1620799140408-edc6dcb6d633?w=900&h=1100&fit=crop"),

        Product(Guid.Parse("10000000-0000-0000-0000-000000000005"), "پولوشرت Corporate Piqué", "corporate-pique-embroidered-polo", ApparelCategory.Polo, PoloCategoryId, 1_250_000m,
            "پولوشرت پیکه با یقه ایستاده؛ مناسب سفارش سازمانی و گلدوزی لوگو روی سینه چپ.",
            "پولوشرت Corporate برای تیم‌ها، کسب‌وکارها و هدایای سازمانی ساخته شده است. بافت پیکه امکان عبور نخ منظم را فراهم می‌کند و گلدوزی لوگو روی سینه چپ، ظاهر حرفه‌ای و ماندگار ایجاد می‌کند.",
            "پنبه پیکه", "Classic Fit", "با دمای پایین بشویید و روی آویز خشک کنید. از سفیدکننده استفاده نکنید.",
            "https://example.com/size-guides/polo.pdf", false,
            [Variant("PO-COR-WHT-M", "M", "سفید", "#F9FAFB", 1_250_000m, null, 12), Variant("PO-COR-NVY-L", "L", "سرمه‌ای", "#1D3557", 1_250_000m, null, 9), Variant("PO-COR-BLK-XL", "XL", "مشکی", "#111111", 1_250_000m, null, 7)],
            ["سازمانی", "پیکه", "لوگو"], "https://images.unsplash.com/photo-1625910513391-5fc5d4b0c9e1?w=900&h=1100&fit=crop"),

        Product(Guid.Parse("10000000-0000-0000-0000-000000000006"), "تی‌شرت آماده Dragon Mark", "ready-dragon-mark-embroidered-tshirt", ApparelCategory.TShirt, TShirtCategoryId, 1_180_000m,
            "تی‌شرت آماده با گلدوزی Dragon Mark؛ بدون ورود به استودیو، فقط رنگ و سایز را انتخاب کن و به سبد اضافه کن.",
            "این مدل از قبل گلدوزی شده و برای خرید سریع آماده است. طرح Dragon Mark روی محصول اجرا شده، بنابراین نیازی به انتخاب محل گلدوزی، آپلود فایل یا تنظیم طرح در استودیو ندارد.",
            "پنبه سنگین ۲۴۰ گرم", "Relaxed Fit", "با آب سرد و پشت‌ورو شسته شود. اتو مستقیم روی گلدوزی انجام نشود.",
            "https://example.com/size-guides/tshirt.pdf", true,
            [Variant("RDY-DRG-BLK-M", "M", "مشکی", "#111111", 1_180_000m, null, 6), Variant("RDY-DRG-BLK-L", "L", "مشکی", "#111111", 1_180_000m, null, 5), Variant("RDY-DRG-CRM-XL", "XL", "کرم", "#EADDC8", 1_180_000m, null, 3)],
            ["آماده", "گلدوزی شده", "بدون استودیو"], "https://i.pinimg.com/736x/62/53/e3/6253e3aca16a432bbec7b273a970cea5.jpg", false),

        Product(Guid.Parse("10000000-0000-0000-0000-000000000007"), "هودی آماده Sword Crest", "ready-sword-crest-embroidered-hoodie", ApparelCategory.Hoodie, HoodieCategoryId, 2_050_000m,
            "هودی آماده با گلدوزی Sword Crest؛ محصول نهایی از قبل تولید شده و مستقیماً فروخته می‌شود.",
            "این هودی یک کالای آماده است؛ طرح Sword Crest از قبل روی لباس گلدوزی شده و مشتری فقط سایز و رنگ موجود را انتخاب می‌کند. این مدل وارد استودیو نمی‌شود.",
            "سه‌نخ پنبه‌ای", "Oversized Fit", "با آب سرد و پشت‌ورو شسته شود. از خشک‌کن داغ استفاده نکنید.",
            "https://example.com/size-guides/hoodie.pdf", true,
            [Variant("RDY-SWD-BLK-M", "M", "مشکی", "#111111", 2_050_000m, null, 4), Variant("RDY-SWD-BLK-L", "L", "مشکی", "#111111", 2_050_000m, null, 4), Variant("RDY-SWD-OLV-XL", "XL", "زیتونی", "#46513C", 2_050_000m, null, 2)],
            ["آماده", "هودی", "گلدوزی شده"], "https://i.pinimg.com/736x/8d/83/fc/8d83fcd41cd102d02ebe678de4e8838c.jpg", false),

        Product(DevelopmentSeedCatalog.OutOfStockProductId, "تی‌شرت نمونه ناموجود", "out-of-stock-embroidered-tshirt", ApparelCategory.TShirt, TShirtCategoryId, 940_000m,
            "محصول نمونه برای تست وضعیت ناموجود، غیرفعال شدن خرید و نمایش پیام اتمام موجودی.",
            "این محصول عمداً با موجودی صفر Seed می‌شود تا سناریوهای رابط کاربری، API و تست‌های موجودی بدون دستکاری دستی دیتابیس قابل بررسی باشند.",
            "پنبه شانه‌شده ۲۲۰ گرم", "Regular Fit", "با آب سرد و پشت‌ورو شسته شود.",
            "https://example.com/size-guides/tshirt.pdf", false,
            [Variant("TEST-OOS-BLK-M", "M", "مشکی", "#111111", 940_000m, null, 0), Variant("TEST-OOS-CRM-L", "L", "کرم", "#EADDC8", 940_000m, null, 0)],
            ["تست", "ناموجود", "موجودی صفر"], "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=900&h=1100&fit=crop")
    ];

    internal static IReadOnlyCollection<Coupon> CreateCoupons() =>
    [
        new Coupon(Guid.Parse("cc111111-1111-1111-1111-111111111111"), "WELCOME10", DiscountType.Percentage, 10m, new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2035, 12, 31, 23, 59, 59, TimeSpan.Zero), 500, 800_000m),
        new Coupon(Guid.Parse("cc222222-2222-2222-2222-222222222222"), "EMBROIDERY150", DiscountType.FixedAmount, 150_000m, new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2035, 12, 31, 23, 59, 59, TimeSpan.Zero), null, 1_500_000m)
    ];

    internal static IReadOnlyCollection<Customer> CreateCustomers() =>
    [
        Customer.Rehydrate(
            DevelopmentSeedCatalog.CustomerId,
            "مشتری تست Tatakae",
            DevelopmentSeedCatalog.Customer.Mobile,
            DevelopmentSeedCatalog.Customer.Email,
            DevelopmentSeedCatalog.FixedTimestamp,
            [
                new Address(
                    DevelopmentSeedCatalog.CustomerAddressId,
                    "مشتری تست Tatakae",
                    DevelopmentSeedCatalog.Customer.Mobile,
                    "تهران",
                    "تهران",
                    "1234567890",
                    "میدان ونک، خیابان ملاصدرا، ساختمان تست Tatakae",
                    "۲۳",
                    "۴",
                    true)
            ]),
        Customer.Rehydrate(
            Guid.Parse("dd222222-2222-2222-2222-222222222222"),
            "آرش محمدی",
            "09351234567",
            "arash@example.com",
            DevelopmentSeedCatalog.FixedTimestamp.AddDays(-7),
            Array.Empty<Address>())
    ];

    internal static IReadOnlyCollection<Order> CreateOrders()
    {
        var product = CreateProducts().Single(x => x.Id == DevelopmentSeedCatalog.CustomizableProductId);
        var variant = product.Variants.OrderBy(x => x.Sku, StringComparer.Ordinal).First();
        var configuration = new EmbroideryConfiguration(
            Guid.Parse("ee111111-1111-1111-1111-111111111111"),
            EmbroideryPlacement.LeftChest,
            7m,
            7m,
            2,
            ["#FFFFFF", "#E63946"],
            "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=300&h=300&fit=crop",
            "brand-mark.png",
            null,
            null,
            "نمونه سفارش برای بررسی چرخه سفارش",
            143_300m);

        var line = new OrderLine(
            product.Id,
            variant.Id,
            product.Name,
            product.Slug,
            product.Images.First().Url,
            variant.Sku,
            variant.Size,
            variant.ColorName,
            variant.ColorHex,
            2,
            variant.EffectivePrice,
            configuration);

        var subtotal = line.LineTotal;
        const decimal shippingAmount = 95_000m;
        const decimal discountAmount = 100_000m;

        return
        [
            Order.Rehydrate(
                DevelopmentSeedCatalog.TestOrderId,
                DevelopmentSeedCatalog.TestOrderNumber,
                DevelopmentSeedCatalog.CustomerId,
                "مشتری تست Tatakae",
                DevelopmentSeedCatalog.Customer.Mobile,
                new Address(
                    DevelopmentSeedCatalog.CustomerAddressId,
                    "مشتری تست Tatakae",
                    DevelopmentSeedCatalog.Customer.Mobile,
                    "تهران",
                    "تهران",
                    "1234567890",
                    "میدان ونک، خیابان ملاصدرا، ساختمان تست Tatakae",
                    "۲۳",
                    "۴",
                    true),
                [line],
                shippingAmount,
                discountAmount,
                "post-standard",
                "پست پیشتاز",
                DevelopmentSeedCatalog.FixedTimestamp.AddDays(1),
                OrderStatus.InEmbroidery,
                PaymentStatus.Paid,
                subtotal,
                subtotal + shippingAmount - discountAmount,
                null,
                "سفارش Seed فاز ۱۴؛ طرح تأیید شده و در مرحله گلدوزی است.")
        ];
    }

    private static Category Category(Guid id, string name, string slug, string description, string image, int order) => new(id, name, slug, description, image, new SeoMetadata($"{name} | سفارش لباس گلدوزی Tatakae", description, $"/category/{slug}", image), null, order, true);

    private static Product Product(Guid id, string name, string slug, ApparelCategory apparelCategory, Guid categoryId, decimal startingPrice, string shortDescription, string description, string material, string fit, string care, string guideUrl, bool featured, IReadOnlyCollection<ProductVariant> variants, IReadOnlyCollection<string> tags, string imageUrl, bool supportsEmbroidery = true)
    {
        IReadOnlyCollection<ProductImage> images = new List<ProductImage>
        {
            new ProductImage(SeedIds.From($"product-image:{slug}:primary"), imageUrl, $"{name} - نمای اصلی", true, 0),
            new ProductImage(SeedIds.From($"product-image:{slug}:detail"), imageUrl, $"{name} - جزئیات پارچه و گلدوزی", false, 1)
        };
        var policy = new EmbroideryPolicy(85_000m, 12_000m, 700m, 6, 12m, 12m, [EmbroideryPlacement.LeftChest, EmbroideryPlacement.CenterChest, EmbroideryPlacement.BackNeck, EmbroideryPlacement.LeftSleeve, EmbroideryPlacement.RightSleeve], ["#FFFFFF", "#111111", "#E63946", "#F4A261", "#2A9D8F", "#457B9D", "#6D28D9"], true, true);
        var customizationLabel = supportsEmbroidery ? "قابل شخصی‌سازی در استودیو" : "آماده گلدوزی‌شده";
        var embroideryRange = supportsEmbroidery ? "حداکثر ۱۲ × ۱۲ سانتی‌متر" : "طرح از قبل روی محصول اجرا شده است";
        return Tatakae.Domain.Entities.Product.Rehydrate(id, name, slug, apparelCategory, categoryId, shortDescription, description, material, fit, care, guideUrl, new SeoMetadata($"{name} | سفارش گلدوزی لباس Tatakae", shortDescription, $"/product/{slug}", imageUrl), policy, images, variants, [new ProductSpecification("جنس", material, 1), new ProductSpecification("فیت", fit, 2), new ProductSpecification("محدوده گلدوزی", embroideryRange, 3), new ProductSpecification("نوع سفارش", customizationLabel, 4)], tags, true, featured, supportsEmbroidery, DevelopmentSeedCatalog.FixedTimestamp, DevelopmentSeedCatalog.FixedTimestamp);
    }

    private static ProductVariant Variant(string sku, string size, string colorName, string colorHex, decimal regularPrice, decimal? salePrice, int stock) => new(SeedIds.From($"variant:{sku}"), sku, size, colorName, colorHex, regularPrice, salePrice, stock);
}
