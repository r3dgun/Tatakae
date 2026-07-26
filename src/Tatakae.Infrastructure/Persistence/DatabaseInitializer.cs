using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Tatakae.Infrastructure.Persistence.Mappers;
using Tatakae.Infrastructure.Persistence.Models;
using Tatakae.Domain.Enums;
using Tatakae.Infrastructure.Seeding;
using Tatakae.Application.Security;

namespace Tatakae.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitialiseTatakaeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TatakaeDbContext>();

        // For this demo project we use EnsureCreated so the SQL database is ready immediately.
        // In production replace this with migrations: db.Database.MigrateAsync().
        await db.Database.EnsureCreatedAsync(cancellationToken);

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var enabled = ReadBoolean(configuration, $"{SeedDataOptions.SectionName}:Enabled", fallback: true);
        if (!enabled) return;

        var includeDevelopmentFixtures = ReadBoolean(configuration, $"{SeedDataOptions.SectionName}:IncludeDevelopmentFixtures", fallback: false);
        var resetDevelopmentPasswords = ReadBoolean(configuration, $"{SeedDataOptions.SectionName}:ResetDevelopmentPasswords", fallback: false);
        await SeedAsync(db, scope.ServiceProvider, includeDevelopmentFixtures, resetDevelopmentPasswords, cancellationToken);
    }

    private static async Task SeedAsync(
        TatakaeDbContext db,
        IServiceProvider serviceProvider,
        bool includeDevelopmentFixtures,
        bool resetDevelopmentPasswords,
        CancellationToken cancellationToken)
    {
        await StoreDataSeeder.EnsureCatalogAsync(db, includeDevelopmentFixtures, cancellationToken);
        await EnsureIranLocationsAsync(db, cancellationToken);

        if (!await db.Coupons.AnyAsync(cancellationToken))
        {
            db.Coupons.AddRange(StoreSeed.CreateCoupons().Select(x => x.ToRecord()));
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.ShippingMethods.AnyAsync(cancellationToken))
        {
            db.ShippingMethods.AddRange(CreateShippingMethods());
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.StorePolicyPages.AnyAsync(cancellationToken))
        {
            db.StorePolicyPages.AddRange(CreatePolicyPages());
            await db.SaveChangesAsync(cancellationToken);
        }

        if (includeDevelopmentFixtures)
        {
            await StoreDataSeeder.EnsureDevelopmentFixturesAsync(db, cancellationToken);
        }

        await SeedIdentityAsync(db, serviceProvider, includeDevelopmentFixtures, resetDevelopmentPasswords, cancellationToken);
        await EnsureAdminPageAccessesAsync(db, cancellationToken);
    }



    private static async Task SeedIdentityAsync(
        TatakaeDbContext db,
        IServiceProvider serviceProvider,
        bool includeDevelopmentFixtures,
        bool resetDevelopmentPasswords,
        CancellationToken cancellationToken)
    {
        foreach (var definition in AdminPermissionCatalog.All)
        {
            var existing = await db.Permissions.FirstOrDefaultAsync(x => x.Key == definition.Key, cancellationToken);
            if (existing is null)
            {
                db.Permissions.Add(new AppPermissionDbRecord
                {
                    Id = Guid.NewGuid(),
                    PermissionNumber = PermissionIds.FromKey(definition.Key),
                    Key = definition.Key,
                    DisplayName = definition.DisplayName,
                    PagePath = definition.PagePath,
                    GroupName = definition.GroupName,
                    Description = definition.Description,
                    SortOrder = definition.SortOrder,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            else
            {
                existing.PermissionNumber = PermissionIds.FromKey(definition.Key);
                existing.DisplayName = definition.DisplayName;
                existing.PagePath = definition.PagePath;
                existing.GroupName = definition.GroupName;
                existing.Description = definition.Description;
                existing.SortOrder = definition.SortOrder;
                existing.IsActive = true;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRoleIdentity>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUserIdentity>>();

        var roles = new[]
        {
            new ApplicationRoleIdentity { Name = "SuperAdmin", DisplayName = "مدیر کل", Description = "دسترسی کامل به همه بخش‌های مدیریت.", IsSystem = true },
            new ApplicationRoleIdentity { Name = "StoreManager", DisplayName = "مدیر فروشگاه", Description = "مدیریت محصول، سفارش، مشتری، ارسال و تخفیف.", IsSystem = true },
            new ApplicationRoleIdentity { Name = "OrderOperator", DisplayName = "اپراتور سفارش", Description = "مشاهده و مدیریت سفارش‌ها و ارسال.", IsSystem = true },
            new ApplicationRoleIdentity { Name = "ContentManager", DisplayName = "مدیر محتوا", Description = "مدیریت محصول، دسته‌بندی، فایل و سئو.", IsSystem = true },
            new ApplicationRoleIdentity { Name = "Customer", DisplayName = "مشتری", Description = "کاربر عادی سایت.", IsSystem = true }
        };

        foreach (var role in roles)
        {
            var existing = await roleManager.FindByNameAsync(role.Name!);
            if (existing is null)
            {
                role.Id = Guid.NewGuid();
                role.NormalizedName = role.Name!.ToUpperInvariant();
                role.CreatedAt = DateTimeOffset.UtcNow;
                var result = await roleManager.CreateAsync(role);
                if (!result.Succeeded) throw new InvalidOperationException(string.Join(" | ", result.Errors.Select(x => x.Description)));
            }
            else
            {
                existing.DisplayName = role.DisplayName;
                existing.Description = role.Description;
                existing.IsSystem = role.IsSystem;
                await roleManager.UpdateAsync(existing);
            }
        }

        await GrantRolePermissionsAsync(db, roleManager, "SuperAdmin", PermissionNames.All, cancellationToken);
        await GrantRolePermissionsAsync(db, roleManager, "StoreManager",
            PermissionNames.All.Where(x => x != PermissionNames.AdminSecurityManage), cancellationToken);
        await GrantRolePermissionsAsync(db, roleManager, "OrderOperator",
            [PermissionNames.AdminDashboardView, PermissionNames.AdminOrdersView, PermissionNames.AdminOrdersManage, PermissionNames.AdminShippingView], cancellationToken);
        await GrantRolePermissionsAsync(db, roleManager, "ContentManager",
            [PermissionNames.AdminDashboardView, PermissionNames.AdminProductsView, PermissionNames.AdminProductsManage, PermissionNames.AdminCategoriesView, PermissionNames.AdminCategoriesManage, PermissionNames.AdminMediaView, PermissionNames.AdminMediaManage, PermissionNames.AdminSeoView, PermissionNames.AdminSeoManage, PermissionNames.AdminLegalView, PermissionNames.AdminLegalManage], cancellationToken);

        if (includeDevelopmentFixtures)
        {
            await DevelopmentIdentitySeeder.EnsureUsersAsync(userManager, resetDevelopmentPasswords, cancellationToken);
        }

        await SyncPermissionCheckerTablesAsync(db, userManager, roleManager, cancellationToken);
    }

    private static async Task SyncPermissionCheckerTablesAsync(TatakaeDbContext db, UserManager<ApplicationUserIdentity> userManager, RoleManager<ApplicationRoleIdentity> roleManager, CancellationToken cancellationToken)
    {
        foreach (var appPermission in await db.Permissions.AsNoTracking().ToListAsync(cancellationToken))
        {
            var numericId = appPermission.PermissionNumber != 0 ? appPermission.PermissionNumber : PermissionIds.FromKey(appPermission.Key);
            if (numericId == 0) continue;
            var permission = await db.PermissionDefinitions.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.PermissionId == numericId, cancellationToken);
            if (permission is null)
            {
                permission = new Tatakae.Infrastructure.Persistence.Models.Permission { PermissionId = numericId };
                db.PermissionDefinitions.Add(permission);
            }
            else
            {
                db.Restore(permission);
            }
            permission.Key = appPermission.Key;
            permission.DisplayName = appPermission.DisplayName;
            permission.PagePath = appPermission.PagePath;
            permission.GroupName = appPermission.GroupName;
            permission.Description = appPermission.Description;
            permission.SortOrder = appPermission.SortOrder;
            permission.IsActive = appPermission.IsActive;
        }

        foreach (var identityRole in await roleManager.Roles.AsNoTracking().ToListAsync(cancellationToken))
        {
            var role = await db.PermissionRoles.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.IdentityRoleId == identityRole.Id || x.Name == identityRole.Name, cancellationToken);
            if (role is null)
            {
                role = new Tatakae.Infrastructure.Persistence.Models.Role();
                db.PermissionRoles.Add(role);
            }
            else
            {
                db.Restore(role);
            }
            role.IdentityRoleId = identityRole.Id;
            role.Name = identityRole.Name ?? string.Empty;
            role.DisplayName = identityRole.DisplayName;
            role.Description = identityRole.Description;
            role.IsSystem = identityRole.IsSystem;
            role.IsActive = true;
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (var identityUser in await userManager.Users.AsNoTracking().ToListAsync(cancellationToken))
        {
            var mobile = identityUser.PhoneNumber ?? identityUser.UserName ?? string.Empty;
            var user = await db.PermissionUsers.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.IdentityUserId == identityUser.Id || x.InsuranceNumber == mobile || x.Mobile == mobile, cancellationToken);
            if (user is null)
            {
                user = new Tatakae.Infrastructure.Persistence.Models.User();
                db.PermissionUsers.Add(user);
            }
            else
            {
                db.Restore(user);
            }
            user.IdentityUserId = identityUser.Id;
            user.InsuranceNumber = mobile;
            user.UserName = identityUser.UserName ?? mobile;
            user.Mobile = mobile;
            user.FullName = identityUser.FullName;
            user.IsActive = identityUser.IsActive;
        }

        await db.SaveChangesAsync(cancellationToken);

        var existingPermissionUserRoles = await db.PermissionUserRoles
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);
        var existingPermissionsRoles = await db.PermissionsRoles
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);
        db.SoftDeleteRange(existingPermissionUserRoles);
        db.SoftDeleteRange(existingPermissionsRoles);
        await db.SaveChangesAsync(cancellationToken);

        var customRoles = await db.PermissionRoles.AsNoTracking().ToDictionaryAsync(x => x.IdentityRoleId, x => x.RoleId, cancellationToken);
        var customUsers = await db.PermissionUsers.AsNoTracking().ToDictionaryAsync(x => x.IdentityUserId, x => x.UserId, cancellationToken);
        var identityUserRoles = await db.Set<IdentityUserRole<Guid>>().AsNoTracking().ToListAsync(cancellationToken);
        foreach (var identityUserRole in identityUserRoles)
        {
            if (!customUsers.TryGetValue(identityUserRole.UserId, out var customUserId)) continue;
            if (!customRoles.TryGetValue(identityUserRole.RoleId, out var customRoleId)) continue;
            var existingUserRole = existingPermissionUserRoles
                .SingleOrDefault(x => x.UserId == customUserId && x.RoleId == customRoleId);
            if (existingUserRole is null)
                db.PermissionUserRoles.Add(new UserRole { UserId = customUserId, RoleId = customRoleId });
            else
                db.Restore(existingUserRole);
        }

        var appRolePermissions = await db.RolePermissions.Include(x => x.Permission).AsNoTracking().Where(x => x.Permission != null).ToListAsync(cancellationToken);
        foreach (var appRolePermission in appRolePermissions)
        {
            if (!customRoles.TryGetValue(appRolePermission.RoleId, out var customRoleId)) continue;
            var numericPermissionId = appRolePermission.Permission!.PermissionNumber != 0 ? appRolePermission.Permission.PermissionNumber : PermissionIds.FromKey(appRolePermission.Permission.Key);
            if (numericPermissionId == 0) continue;
            var existingPermissionRole = existingPermissionsRoles
                .SingleOrDefault(x => x.RoleId == customRoleId && x.PermissionId == numericPermissionId);
            if (existingPermissionRole is null)
                db.PermissionsRoles.Add(new PermissionsRole { RoleId = customRoleId, PermissionId = numericPermissionId });
            else
                db.Restore(existingPermissionRole);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task GrantRolePermissionsAsync(TatakaeDbContext db, RoleManager<ApplicationRoleIdentity> roleManager, string roleName, IEnumerable<string> permissionKeys, CancellationToken cancellationToken)
    {
        var role = await roleManager.FindByNameAsync(roleName) ?? throw new InvalidOperationException($"Role '{roleName}' was not found.");
        var permissionIds = await db.Permissions
            .Where(x => permissionKeys.Contains(x.Key))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var existingIds = await db.RolePermissions
            .Where(x => x.RoleId == role.Id)
            .Select(x => x.PermissionId)
            .ToListAsync(cancellationToken);

        foreach (var permissionId in permissionIds.Except(existingIds))
        {
            db.RolePermissions.Add(new AppRolePermissionDbRecord
            {
                Id = Guid.NewGuid(),
                RoleId = role.Id,
                PermissionId = permissionId,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }



    private static async Task EnsureAdminPageAccessesAsync(TatakaeDbContext db, CancellationToken cancellationToken)
    {
        foreach (var definition in AdminPageAccessCatalog.All)
        {
            var existing = await db.AdminPageAccesses.FirstOrDefaultAsync(x => x.PageKey == definition.PageKey, cancellationToken);
            if (existing is null)
            {
                db.AdminPageAccesses.Add(new AdminPageAccessDbRecord
                {
                    Id = Guid.NewGuid(),
                    PageKey = definition.PageKey,
                    Title = definition.Title,
                    Path = definition.Path,
                    RequiredPermissionKey = definition.RequiredPermissionKey,
                    MenuGroup = definition.MenuGroup,
                    Icon = definition.Icon,
                    Description = definition.Description,
                    ShowInMenu = definition.ShowInMenu,
                    IsActive = true,
                    SortOrder = definition.SortOrder,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            }
            else
            {
                existing.Title = definition.Title;
                existing.Path = definition.Path;
                existing.RequiredPermissionKey = definition.RequiredPermissionKey;
                existing.MenuGroup = definition.MenuGroup;
                existing.Icon = definition.Icon;
                existing.Description = definition.Description;
                existing.ShowInMenu = definition.ShowInMenu;
                existing.IsActive = true;
                existing.SortOrder = definition.SortOrder;
                existing.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }


    private static async Task EnsureIranLocationsAsync(TatakaeDbContext db, CancellationToken cancellationToken)
    {
        foreach (var item in IranLocationSeed.ProvincesAndCities)
        {
            var provinceName = NormalizePersianLocationName(item.Key);
            var provinceSlug = ToLocationSlug(provinceName);

            var province = await db.IranianProvinces
                .Include(x => x.Cities)
                .FirstOrDefaultAsync(x => x.Name == provinceName || x.Slug == provinceSlug, cancellationToken);

            if (province is null)
            {
                province = new IranianProvinceDbRecord
                {
                    Id = Guid.NewGuid(),
                    Name = provinceName,
                    Slug = provinceSlug,
                    IsActive = true
                };
                db.IranianProvinces.Add(province);
                await db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                province.Name = provinceName;
                province.Slug = provinceSlug;
                province.IsActive = true;
            }

            foreach (var cityNameRaw in item.Value.Distinct(StringComparer.Ordinal))
            {
                var cityName = NormalizePersianLocationName(cityNameRaw);
                var citySlug = ToLocationSlug($"{provinceName}-{cityName}");
                var exists = province.Cities.Any(x => x.Name == cityName);
                if (exists) continue;

                db.IranianCities.Add(new IranianCityDbRecord
                {
                    Id = Guid.NewGuid(),
                    ProvinceId = province.Id,
                    Name = cityName,
                    Slug = citySlug,
                    SupportsSameDayDelivery = IsSameDayDeliveryCity(provinceName, cityName),
                    IsActive = true
                });
            }

        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static bool IsSameDayDeliveryCity(string provinceName, string cityName) =>
        (provinceName == "تهران" && new[] { "تهران", "ری", "شمیرانات", "اسلامشهر" }.Contains(cityName, StringComparer.Ordinal))
        || (provinceName == "البرز" && cityName == "کرج");

    private static string NormalizePersianLocationName(string value) => value
        .Trim()
        .Replace('ك', 'ک')
        .Replace('ي', 'ی')
        .Replace("  ", " ");

    private static string ToLocationSlug(string value)
    {
        var slug = NormalizePersianLocationName(value)
            .Replace("‌", "-")
            .Replace(" ", "-")
            .Replace("/", "-")
            .Replace("\\", "-")
            .Replace(".", "-");

        while (slug.Contains("--", StringComparison.Ordinal)) slug = slug.Replace("--", "-");
        return slug.Trim('-').ToLowerInvariant();
    }

    private static IReadOnlyCollection<ShippingMethodDbRecord> CreateShippingMethods() =>
    [
        new ShippingMethodDbRecord
        {
            Id = Guid.Parse("88000000-0000-0000-0000-000000000001"),
            Code = "post-standard",
            Title = "پست پیشتاز",
            Description = "ارسال اقتصادی برای سراسر ایران؛ مناسب سفارش‌های معمولی.",
            Carrier = ShippingCarrier.Post,
            BasePrice = 95_000m,
            FreeShippingThreshold = 3_000_000m,
            MinDeliveryDays = 3,
            MaxDeliveryDays = 7,
            SupportsCashOnDelivery = false,
            IsDefault = true,
            IsActive = true,
            SortOrder = 1
        },
        new ShippingMethodDbRecord
        {
            Id = Guid.Parse("88000000-0000-0000-0000-000000000002"),
            Code = "tipax",
            Title = "تیپاکس / ارسال سریع",
            Description = "ارسال سریع‌تر برای شهرهای تحت پوشش؛ هزینه در checkout نمایش داده می‌شود.",
            Carrier = ShippingCarrier.Tipax,
            BasePrice = 145_000m,
            FreeShippingThreshold = 5_000_000m,
            MinDeliveryDays = 1,
            MaxDeliveryDays = 4,
            SupportsCashOnDelivery = false,
            IsDefault = false,
            IsActive = true,
            SortOrder = 2
        },
        new ShippingMethodDbRecord
        {
            Id = Guid.Parse("88000000-0000-0000-0000-000000000003"),
            Code = "tehran-courier",
            Title = "پیک تهران",
            Description = "ارسال با پیک برای تهران؛ مناسب سفارش‌های فوری و نزدیک.",
            Carrier = ShippingCarrier.Peyk,
            BasePrice = 180_000m,
            FreeShippingThreshold = 6_000_000m,
            MinDeliveryDays = 0,
            MaxDeliveryDays = 1,
            SupportsCashOnDelivery = true,
            IsDefault = false,
            IsActive = true,
            SortOrder = 3
        },
        new ShippingMethodDbRecord
        {
            Id = Guid.Parse("88000000-0000-0000-0000-000000000004"),
            Code = "customer-pickup",
            Title = "تحویل حضوری",
            Description = "تحویل از کارگاه/دفتر پس از آماده شدن سفارش؛ بدون هزینه ارسال.",
            Carrier = ShippingCarrier.CustomerPickup,
            BasePrice = 0m,
            FreeShippingThreshold = null,
            MinDeliveryDays = 0,
            MaxDeliveryDays = 0,
            SupportsCashOnDelivery = false,
            IsDefault = false,
            IsActive = true,
            SortOrder = 4
        }
    ];

    private static IReadOnlyCollection<StorePolicyPageDbRecord> CreatePolicyPages() =>
    [
        Policy("terms", "قوانین و مقررات سایت", "قوانین ثبت سفارش، انتخاب محصول، گلدوزی اختصاصی، ارسال و پشتیبانی در فروشگاه.", """
        <h2>ثبت سفارش</h2>
        <p>با ثبت سفارش، مشتری تأیید می‌کند که مشخصات محصول، سایز، رنگ، متن یا فایل گلدوزی، محل گلدوزی، آدرس و روش ارسال را بررسی کرده است.</p>
        <h2>سفارش گلدوزی</h2>
        <ul><li>مسئولیت صحت متن، لوگو یا فایل آپلودی با مشتری است.</li><li>در صورت نیاز به اصلاح طرح، فروشگاه قبل از تولید با مشتری هماهنگ می‌کند.</li><li>بعد از ورود سفارش به مرحله تولید، لغو سفارش فقط با تأیید ادمین امکان‌پذیر است.</li></ul>
        <h2>قیمت و موجودی</h2>
        <p>قیمت نهایی شامل قیمت لباس، هزینه گلدوزی، تخفیف احتمالی و هزینه ارسال است. موجودی بر اساس سایز و رنگ محاسبه می‌شود.</p>
        <h2>ارسال و تحویل</h2>
        <p>روش ارسال توسط مشتری در Checkout انتخاب می‌شود. زمان تحویل تقریبی است و به آماده‌سازی گلدوزی، شهر مقصد و روش ارسال وابسته است.</p>
        """, 1),
        Policy("privacy", "حریم خصوصی", "نحوه نگهداری اطلاعات مشتری، آدرس‌ها، فایل‌های آپلودی و پیام‌های تماس.", """
        <h2>اطلاعات ذخیره‌شده</h2>
        <ul><li>نام، موبایل، ایمیل، آدرس، استان، شهر و کدپستی.</li><li>جزئیات محصول، سایز، رنگ، طرح گلدوزی و فایل‌های آپلودی.</li><li>پیام‌های ارسال‌شده از فرم ارتباط با ما.</li></ul>
        <h2>هدف استفاده</h2>
        <p>اطلاعات فقط برای ثبت سفارش، تولید گلدوزی، ارسال، پشتیبانی و بهبود تجربه خرید استفاده می‌شود.</p>
        <h2>فایل‌های گلدوزی</h2>
        <p>فایل‌های آپلودی مشتری بدون اجازه برای نمونه‌کار عمومی یا تبلیغات منتشر نمی‌شوند.</p>
        """, 2),
        Policy("returns", "شرایط مرجوعی", "قوانین مرجوعی برای پوشاک گلدوزی‌شده و سفارش اختصاصی.", """
        <h2>مرجوعی کالای سفارشی</h2>
        <p>محصولی که با متن، لوگو یا فایل اختصاصی مشتری گلدوزی شده باشد، فقط در صورت ایراد تولید، مغایرت با سفارش ثبت‌شده یا آسیب‌دیدگی قابل بررسی است.</p>
        <h2>موارد قابل بررسی</h2>
        <ul><li>اشتباه در سایز یا رنگ نسبت به سفارش ثبت‌شده.</li><li>ایراد واضح در دوخت یا گلدوزی.</li><li>آسیب‌دیدگی هنگام تحویل.</li></ul>
        <h2>روش ثبت درخواست</h2>
        <p>مشتری باید شماره سفارش، تصویر محصول و توضیح مشکل را از طریق پشتیبانی یا فرم تماس ارسال کند.</p>
        """, 3),
        Policy("shipping", "روش‌های ارسال", "روش‌های ارسال قابل انتخاب در Checkout و قابل مدیریت از پنل ادمین.", """
        <h2>انتخاب روش ارسال</h2>
        <p>در Checkout، مشتری فقط روش‌هایی را می‌بیند که مدیر فروشگاه فعال کرده است. هزینه ارسال و زمان تقریبی تحویل همان‌جا نمایش داده می‌شود.</p>
        <h2>روش‌های قابل تعریف</h2>
        <ul><li>پست پیشتاز یا سفارشی.</li><li>تیپاکس یا ارسال سریع.</li><li>پیک شهری برای شهرهای خاص.</li><li>تحویل حضوری بدون هزینه ارسال.</li></ul>
        <h2>کد رهگیری</h2>
        <p>پس از آماده شدن سفارش، مدیر فروشگاه می‌تواند کد رهگیری را در پنل سفارش ثبت کند.</p>
        """, 4),
        Policy("about", "درباره Tatakae", "فروشگاه تخصصی پوشاک گلدوزی و سفارش اختصاصی آنلاین برای بازار ایران.", """
        <h2>ما چه کاری انجام می‌دهیم؟</h2>
        <p>Tatakae برای فروش لباس‌های گلدوزی‌شده و سفارش‌های اختصاصی طراحی شده است. مشتری محصول پایه را انتخاب می‌کند، سایز و رنگ را می‌بیند، طرح یا متن گلدوزی را در استودیو تنظیم می‌کند و سفارش را از Checkout ثبت می‌کند.</p>
        <h2>تمرکز فروشگاه</h2>
        <ul><li>تی‌شرت، هودی، دورس و پولوشرت مناسب گلدوزی.</li><li>پشتیبانی از طرح آماده، متن اختصاصی و فایل آپلودی مشتری.</li><li>مدیریت سفارش، ارسال، فایل‌ها و دسترسی ادمین از پنل مدیریت.</li></ul>
        <h2>تجربه خرید</h2>
        <p>هدف این فروشگاه این است که خرید لباس و سفارش گلدوزی از حالت پیام‌دادن دستی خارج شود و همه مراحل در سایت انجام شود.</p>
        """, 5),
        Policy("contact", "ارتباط با ما", "فرم تماس، راه‌های ارتباطی و پیگیری سفارش‌های گلدوزی.", """
        <h2>پشتیبانی سفارش</h2>
        <p>برای پیگیری سفارش، اصلاح اطلاعات ارسال یا سؤال درباره وضعیت گلدوزی، شماره سفارش خود را همراه پیام ارسال کنید.</p>
        <div class="contact-cards"><div class="contact-card"><b>موبایل پشتیبانی</b><span>بعداً از پنل قوانین و ارتباط وارد شود.</span></div><div class="contact-card"><b>آدرس فروشگاه</b><span>بعداً با آدرس واقعی کسب‌وکار جایگزین شود.</span></div><div class="contact-card"><b>ساعت پاسخ‌گویی</b><span>شنبه تا پنجشنبه، ساعات کاری فروشگاه.</span></div></div>
        <h2>سفارش سازمانی</h2>
        <p>برای سفارش تعداد بالا، گلدوزی لوگوی سازمانی یا تولید کالکشن اختصاصی، از فرم تماس استفاده کنید.</p>
        """, 6)
    ];

    private static StorePolicyPageDbRecord Policy(string slug, string title, string summary, string body, int sortOrder) => new()
    {
        Id = Guid.NewGuid(),
        Slug = slug,
        Title = title,
        Summary = summary,
        Body = body,
        SeoTitle = $"{title} | Tatakae",
        SeoDescription = summary,
        IsPublished = true,
        SortOrder = sortOrder,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private static bool ReadBoolean(IConfiguration configuration, string key, bool fallback)
        => bool.TryParse(configuration[key], out var value) ? value : fallback;
}

