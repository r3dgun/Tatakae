using Tatakae.Application.Contracts.Common;

namespace Tatakae.Application.Security;

public static class PermissionClaimTypes
{
    public const string Permission = "permission";
}


public static class PermissionIds
{
    public const int AdminDashboardView = 1000;
    public const int AdminProductsView = 1100;
    public const int AdminProductsManage = 1101;
    public const int AdminCategoriesView = 1200;
    public const int AdminCategoriesManage = 1201;
    public const int AdminOrdersView = 1300;
    public const int AdminOrdersManage = 1301;
    public const int AdminCustomersView = 1400;
    public const int AdminCouponsView = 1500;
    public const int AdminCouponsManage = 1501;
    public const int AdminShippingView = 1600;
    public const int AdminShippingManage = 1601;
    public const int AdminMediaView = 1700;
    public const int AdminMediaManage = 1701;
    public const int AdminSeoView = 1800;
    public const int AdminSeoManage = 1801;
    public const int AdminLegalView = 1850;
    public const int AdminLegalManage = 1851;
    public const int AdminQuestionsView = 1860;
    public const int AdminQuestionsManage = 1861;
    public const int AdminNotificationsView = 1870;
    public const int AdminNotificationsManage = 1871;
    public const int AdminSecurityView = 1900;
    public const int AdminSecurityManage = 1901;

    public static readonly IReadOnlyDictionary<string, int> ByKey = new Dictionary<string, int>
    {
        [PermissionNames.AdminDashboardView] = AdminDashboardView,
        [PermissionNames.AdminProductsView] = AdminProductsView,
        [PermissionNames.AdminProductsManage] = AdminProductsManage,
        [PermissionNames.AdminCategoriesView] = AdminCategoriesView,
        [PermissionNames.AdminCategoriesManage] = AdminCategoriesManage,
        [PermissionNames.AdminOrdersView] = AdminOrdersView,
        [PermissionNames.AdminOrdersManage] = AdminOrdersManage,
        [PermissionNames.AdminCustomersView] = AdminCustomersView,
        [PermissionNames.AdminCouponsView] = AdminCouponsView,
        [PermissionNames.AdminCouponsManage] = AdminCouponsManage,
        [PermissionNames.AdminShippingView] = AdminShippingView,
        [PermissionNames.AdminShippingManage] = AdminShippingManage,
        [PermissionNames.AdminMediaView] = AdminMediaView,
        [PermissionNames.AdminMediaManage] = AdminMediaManage,
        [PermissionNames.AdminSeoView] = AdminSeoView,
        [PermissionNames.AdminSeoManage] = AdminSeoManage,
        [PermissionNames.AdminLegalView] = AdminLegalView,
        [PermissionNames.AdminLegalManage] = AdminLegalManage,
        [PermissionNames.AdminQuestionsView] = AdminQuestionsView,
        [PermissionNames.AdminQuestionsManage] = AdminQuestionsManage,
        [PermissionNames.AdminNotificationsView] = AdminNotificationsView,
        [PermissionNames.AdminNotificationsManage] = AdminNotificationsManage,
        [PermissionNames.AdminSecurityView] = AdminSecurityView,
        [PermissionNames.AdminSecurityManage] = AdminSecurityManage
    };

    public static readonly IReadOnlyDictionary<int, string> ById = ByKey.ToDictionary(x => x.Value, x => x.Key);

    public static int FromKey(string key) => ByKey.TryGetValue(key, out var id) ? id : 0;
    public static string? ToKey(long id) => ById.TryGetValue((int)id, out var key) ? key : null;
}

public sealed record PermissionCheckResult(bool IsSuccess, string? Message = null);

public static class PermissionNames
{
    public const string AdminDashboardView = "admin.dashboard.view";
    public const string AdminProductsView = "admin.products.view";
    public const string AdminProductsManage = "admin.products.manage";
    public const string AdminCategoriesView = "admin.categories.view";
    public const string AdminCategoriesManage = "admin.categories.manage";
    public const string AdminOrdersView = "admin.orders.view";
    public const string AdminOrdersManage = "admin.orders.manage";
    public const string AdminCustomersView = "admin.customers.view";
    public const string AdminCouponsView = "admin.coupons.view";
    public const string AdminCouponsManage = "admin.coupons.manage";
    public const string AdminShippingView = "admin.shipping.view";
    public const string AdminShippingManage = "admin.shipping.manage";
    public const string AdminMediaView = "admin.media.view";
    public const string AdminMediaManage = "admin.media.manage";
    public const string AdminSeoView = "admin.seo.view";
    public const string AdminSeoManage = "admin.seo.manage";
    public const string AdminLegalView = "admin.legal.view";
    public const string AdminLegalManage = "admin.legal.manage";
    public const string AdminQuestionsView = "admin.questions.view";
    public const string AdminQuestionsManage = "admin.questions.manage";
    public const string AdminNotificationsView = "admin.notifications.view";
    public const string AdminNotificationsManage = "admin.notifications.manage";
    public const string AdminSecurityView = "admin.security.view";
    public const string AdminSecurityManage = "admin.security.manage";

    public static readonly IReadOnlyCollection<string> All =
    [
        AdminDashboardView,
        AdminProductsView,
        AdminProductsManage,
        AdminCategoriesView,
        AdminCategoriesManage,
        AdminOrdersView,
        AdminOrdersManage,
        AdminCustomersView,
        AdminCouponsView,
        AdminCouponsManage,
        AdminShippingView,
        AdminShippingManage,
        AdminMediaView,
        AdminMediaManage,
        AdminSeoView,
        AdminSeoManage,
        AdminLegalView,
        AdminLegalManage,
        AdminQuestionsView,
        AdminQuestionsManage,
        AdminNotificationsView,
        AdminNotificationsManage,
        AdminSecurityView,
        AdminSecurityManage
    ];
}

public sealed record AdminPermissionDefinition(string Key, string DisplayName, string PagePath, string GroupName, string Description, int SortOrder);

public static class AdminPermissionCatalog
{
    public static readonly IReadOnlyCollection<AdminPermissionDefinition> All =
    [
        Permission(PermissionNames.AdminDashboardView, "مشاهده داشبورد", "/admin", "Dashboard", "اجازه مشاهده صفحه داشبورد مدیریت.", 1),
        Permission(PermissionNames.AdminProductsView, "مشاهده محصولات", "/admin/products", "Products", "اجازه مشاهده لیست و جزئیات محصولات.", 10),
        Permission(PermissionNames.AdminProductsManage, "مدیریت محصولات", "/admin/products", "Products", "اجازه ایجاد، ویرایش و حذف محصول و اصلاح SKU/موجودی.", 11),
        Permission(PermissionNames.AdminCategoriesView, "مشاهده دسته‌بندی‌ها", "/admin/categories", "Categories", "اجازه مشاهده دسته‌بندی‌ها.", 20),
        Permission(PermissionNames.AdminCategoriesManage, "مدیریت دسته‌بندی‌ها", "/admin/categories", "Categories", "اجازه ایجاد، ویرایش و حذف دسته‌بندی.", 21),
        Permission(PermissionNames.AdminOrdersView, "مشاهده سفارش‌ها", "/admin/orders", "Orders", "اجازه مشاهده سفارش‌ها و جزئیات سفارش.", 30),
        Permission(PermissionNames.AdminOrdersManage, "مدیریت سفارش‌ها", "/admin/orders", "Orders", "اجازه تغییر وضعیت سفارش، ثبت کد رهگیری و یادداشت ادمین.", 31),
        Permission(PermissionNames.AdminCustomersView, "مشاهده مشتری‌ها", "/admin/customers", "Customers", "اجازه مشاهده اطلاعات مشتری‌ها.", 40),
        Permission(PermissionNames.AdminCouponsView, "مشاهده کدهای تخفیف", "/admin/coupons", "Coupons", "اجازه مشاهده کوپن‌ها.", 50),
        Permission(PermissionNames.AdminCouponsManage, "مدیریت کدهای تخفیف", "/admin/coupons", "Coupons", "اجازه ایجاد، ویرایش و حذف کوپن.", 51),
        Permission(PermissionNames.AdminShippingView, "مشاهده روش‌های ارسال", "/admin/shipping", "Shipping", "اجازه مشاهده روش‌های ارسال دستی.", 60),
        Permission(PermissionNames.AdminShippingManage, "مدیریت روش‌های ارسال", "/admin/shipping", "Shipping", "اجازه ایجاد، ویرایش و حذف روش ارسال.", 61),
        Permission(PermissionNames.AdminMediaView, "مشاهده فایل‌ها", "/admin/media", "Media", "اجازه مشاهده فایل‌ها و رسانه‌ها.", 70),
        Permission(PermissionNames.AdminMediaManage, "مدیریت فایل‌ها", "/admin/media", "Media", "اجازه حذف فایل‌ها و رسانه‌ها.", 71),
        Permission(PermissionNames.AdminSeoView, "مشاهده سئو", "/admin/seo", "SEO", "اجازه مشاهده صفحه سئو.", 80),
        Permission(PermissionNames.AdminSeoManage, "مدیریت سئو", "/admin/seo", "SEO", "اجازه ویرایش تنظیمات سئو.", 81),
        Permission(PermissionNames.AdminLegalView, "مشاهده صفحات اعتماد و تماس", "/admin/legal", "Legal", "اجازه مشاهده قوانین سایت، درباره ما و پیام‌های تماس.", 85),
        Permission(PermissionNames.AdminLegalManage, "مدیریت صفحات اعتماد و تماس", "/admin/legal", "Legal", "اجازه ویرایش صفحات قانونی و مدیریت پیام‌های تماس.", 86),
        Permission(PermissionNames.AdminQuestionsView, "مشاهده پرسش‌های محصول", "/admin/questions", "Content", "اجازه مشاهده پرسش و پاسخ محصولات.", 87),
        Permission(PermissionNames.AdminQuestionsManage, "مدیریت پرسش‌های محصول", "/admin/questions", "Content", "اجازه پاسخ دادن، تأیید یا پنهان کردن پرسش‌های محصول.", 88),
        Permission(PermissionNames.AdminNotificationsView, "مشاهده اعلان‌ها", "/admin/notifications", "Operations", "اجازه مشاهده صف اعلان‌های مشتری و ادمین.", 89),
        Permission(PermissionNames.AdminNotificationsManage, "مدیریت اعلان‌ها", "/admin/notifications", "Operations", "اجازه ایجاد اعلان دستی و تغییر وضعیت ارسال.", 90),
        Permission(PermissionNames.AdminSecurityView, "مشاهده امنیت و دسترسی", "/admin/security", "Security", "اجازه مشاهده کاربران، نقش‌ها و دسترسی‌ها.", 90),
        Permission(PermissionNames.AdminSecurityManage, "مدیریت امنیت و دسترسی", "/admin/security", "Security", "اجازه مدیریت Role، Permission و دسترسی کاربران.", 91)
    ];

    private static AdminPermissionDefinition Permission(string key, string displayName, string pagePath, string groupName, string description, int sortOrder)
        => new(key, displayName, pagePath, groupName, description, sortOrder);
}


public sealed record AdminPageAccessDefinition(string PageKey, string Title, string Path, string RequiredPermissionKey, string MenuGroup, string Icon, string Description, bool ShowInMenu, int SortOrder);

public static class AdminPageAccessCatalog
{
    public static readonly IReadOnlyCollection<AdminPageAccessDefinition> All =
    [
        Page("dashboard", "داشبورد", "/admin", PermissionNames.AdminDashboardView, "General", "▦", "نمای کلی فروش، سفارش‌ها و وضعیت فروشگاه.", true, 1),
        Page("products", "محصولات", "/admin/products", PermissionNames.AdminProductsView, "Catalog", "◈", "لیست محصولات، وضعیت انتشار، قیمت و موجودی.", true, 10),
        Page("product-editor", "ویرایش محصول", "/admin/products/edit", PermissionNames.AdminProductsManage, "Catalog", "✎", "ایجاد و ویرایش محصول، واریانت، تصویر، سئو و سیاست گلدوزی.", false, 11),
        Page("inventory", "موجودی و SKU", "/admin/inventory", PermissionNames.AdminProductsView, "Catalog", "▥", "مشاهده SKUها، موجودی قابل فروش و اصلاح دستی موجودی.", true, 12),
        Page("categories", "دسته‌بندی‌ها", "/admin/categories", PermissionNames.AdminCategoriesView, "Catalog", "▤", "مدیریت دسته‌بندی‌ها و مسیرهای دسته‌بندی فروشگاه.", true, 20),
        Page("orders", "سفارش‌ها", "/admin/orders", PermissionNames.AdminOrdersView, "Orders", "◷", "مشاهده سفارش‌ها و وضعیت تولید/ارسال.", true, 30),
        Page("order-detail", "جزئیات سفارش", "/admin/orders/detail", PermissionNames.AdminOrdersView, "Orders", "↳", "مشاهده جزئیات هر سفارش و اطلاعات گلدوزی.", false, 31),
        Page("customers", "مشتری‌ها", "/admin/customers", PermissionNames.AdminCustomersView, "Customers", "♙", "مشاهده مشتری‌ها و اطلاعات پایه سفارش.", true, 40),
        Page("coupons", "کدهای تخفیف", "/admin/coupons", PermissionNames.AdminCouponsView, "Marketing", "✦", "مشاهده و مدیریت کوپن‌های فروشگاه.", true, 50),
        Page("shipping", "روش‌های ارسال", "/admin/shipping", PermissionNames.AdminShippingView, "Operations", "▣", "تعریف روش‌های ارسال دستی قابل انتخاب در checkout.", true, 60),
        Page("media", "فایل‌ها", "/admin/media", PermissionNames.AdminMediaView, "Content", "▧", "مدیریت فایل‌های آپلودی و رسانه‌های فروشگاه.", true, 70),
        Page("artworks", "طرح‌های گلدوزی", "/admin/artworks", PermissionNames.AdminMediaView, "Content", "✳", "بررسی، تأیید، رد یا درخواست اصلاح فایل‌های طرح گلدوزی مشتری‌ها.", true, 72),
        Page("seo", "سئو", "/admin/seo", PermissionNames.AdminSeoView, "Content", "⌁", "سئو، متا، redirect و ساختار قابل ایندکس.", true, 80),
        Page("legal", "قوانین و ارتباط", "/admin/legal", PermissionNames.AdminLegalView, "Trust", "§", "ویرایش درباره ما، قوانین سایت، حریم خصوصی، ارسال، مرجوعی و پیام‌های تماس.", true, 85),
        Page("questions", "پرسش و پاسخ", "/admin/questions", PermissionNames.AdminQuestionsView, "Content", "؟", "مدیریت پرسش‌های محصول و پاسخ ادمین.", true, 87),
        Page("reviews", "نظرات", "/admin/reviews", PermissionNames.AdminQuestionsView, "Content", "★", "مدیریت نظرات محصول، تأیید، رد، مخفی‌سازی و پاسخ ادمین.", true, 89),
        Page("notifications", "اعلان‌ها", "/admin/notifications", PermissionNames.AdminNotificationsView, "Operations", "◌", "صف اعلان‌های مشتری، پیامک/ایمیل دمو و اعلان‌های سیستمی.", true, 89),
        Page("security", "دسترسی‌ها", "/admin/security", PermissionNames.AdminSecurityView, "Security", "⚿", "مدیریت کاربران، نقش‌ها، permissionها و نگاشت صفحه به permission.", true, 90)
    ];

    private static AdminPageAccessDefinition Page(string pageKey, string title, string path, string requiredPermissionKey, string menuGroup, string icon, string description, bool showInMenu, int sortOrder)
        => new(pageKey, title, path, requiredPermissionKey, menuGroup, icon, description, showInMenu, sortOrder);
}
