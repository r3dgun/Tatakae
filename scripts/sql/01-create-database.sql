/*
    Tatakae Embroidery Commerce - SQL Server schema
    This script is optional. The API also creates the same database automatically with EF Core EnsureCreated.
*/
IF DB_ID(N'TatakaeEmbroideryCommerce') IS NULL
BEGIN
    CREATE DATABASE TatakaeEmbroideryCommerce;
END
GO
USE TatakaeEmbroideryCommerce;
GO

IF OBJECT_ID(N'dbo.OrderLines', N'U') IS NOT NULL DROP TABLE dbo.OrderLines;
IF OBJECT_ID(N'dbo.Orders', N'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID(N'dbo.CustomerAddresses', N'U') IS NOT NULL DROP TABLE dbo.CustomerAddresses;
IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL DROP TABLE dbo.Customers;
IF OBJECT_ID(N'dbo.Coupons', N'U') IS NOT NULL DROP TABLE dbo.Coupons;
IF OBJECT_ID(N'dbo.ProductAllowedThreadColors', N'U') IS NOT NULL DROP TABLE dbo.ProductAllowedThreadColors;
IF OBJECT_ID(N'dbo.ProductAllowedPlacements', N'U') IS NOT NULL DROP TABLE dbo.ProductAllowedPlacements;
IF OBJECT_ID(N'dbo.ProductEmbroideryPolicies', N'U') IS NOT NULL DROP TABLE dbo.ProductEmbroideryPolicies;
IF OBJECT_ID(N'dbo.ProductTags', N'U') IS NOT NULL DROP TABLE dbo.ProductTags;
IF OBJECT_ID(N'dbo.ProductSpecifications', N'U') IS NOT NULL DROP TABLE dbo.ProductSpecifications;
IF OBJECT_ID(N'dbo.ProductVariants', N'U') IS NOT NULL DROP TABLE dbo.ProductVariants;
IF OBJECT_ID(N'dbo.ProductImages', N'U') IS NOT NULL DROP TABLE dbo.ProductImages;
IF OBJECT_ID(N'dbo.Products', N'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL DROP TABLE dbo.Categories;
GO

CREATE TABLE dbo.Categories (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    Name nvarchar(180) NOT NULL,
    Slug nvarchar(220) NOT NULL UNIQUE,
    Description nvarchar(1400) NOT NULL,
    CoverImageUrl nvarchar(1000) NULL,
    SeoMetaTitle nvarchar(260) NOT NULL,
    SeoMetaDescription nvarchar(500) NOT NULL,
    SeoCanonicalPath nvarchar(600) NULL,
    SeoOpenGraphImageUrl nvarchar(1000) NULL,
    SeoAllowIndex bit NOT NULL,
    SeoAllowFollow bit NOT NULL,
    ParentId uniqueidentifier NULL,
    SortOrder int NOT NULL,
    IsActive bit NOT NULL
);

CREATE TABLE dbo.Products (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    Name nvarchar(240) NOT NULL,
    Slug nvarchar(260) NOT NULL UNIQUE,
    ApparelCategory nvarchar(60) NOT NULL,
    CategoryId uniqueidentifier NOT NULL,
    ShortDescription nvarchar(700) NOT NULL,
    Description nvarchar(max) NOT NULL,
    Material nvarchar(220) NOT NULL,
    Fit nvarchar(120) NOT NULL,
    CareGuide nvarchar(1400) NOT NULL,
    SizeGuideUrl nvarchar(1000) NOT NULL,
    SeoMetaTitle nvarchar(260) NOT NULL,
    SeoMetaDescription nvarchar(500) NOT NULL,
    SeoCanonicalPath nvarchar(600) NULL,
    SeoOpenGraphImageUrl nvarchar(1000) NULL,
    SeoAllowIndex bit NOT NULL,
    SeoAllowFollow bit NOT NULL,
    IsPublished bit NOT NULL,
    IsFeatured bit NOT NULL,
    CreatedAt datetimeoffset NOT NULL,
    UpdatedAt datetimeoffset NOT NULL
);
CREATE INDEX IX_Products_CategoryId ON dbo.Products(CategoryId);

CREATE TABLE dbo.ProductImages (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    ProductId uniqueidentifier NOT NULL,
    Url nvarchar(1000) NOT NULL,
    AltText nvarchar(260) NOT NULL,
    IsPrimary bit NOT NULL,
    SortOrder int NOT NULL,
    CONSTRAINT FK_ProductImages_Products FOREIGN KEY(ProductId) REFERENCES dbo.Products(Id) ON DELETE CASCADE
);

CREATE TABLE dbo.ProductVariants (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    ProductId uniqueidentifier NOT NULL,
    Sku nvarchar(80) NOT NULL UNIQUE,
    Size nvarchar(30) NOT NULL,
    ColorName nvarchar(80) NOT NULL,
    ColorHex nvarchar(20) NOT NULL,
    RegularPrice decimal(18,2) NOT NULL,
    SalePrice decimal(18,2) NULL,
    StockQuantity int NOT NULL,
    IsActive bit NOT NULL,
    CONSTRAINT FK_ProductVariants_Products FOREIGN KEY(ProductId) REFERENCES dbo.Products(Id) ON DELETE CASCADE
);

CREATE TABLE dbo.ProductSpecifications (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    ProductId uniqueidentifier NOT NULL,
    Name nvarchar(140) NOT NULL,
    Value nvarchar(700) NOT NULL,
    SortOrder int NOT NULL,
    CONSTRAINT FK_ProductSpecifications_Products FOREIGN KEY(ProductId) REFERENCES dbo.Products(Id) ON DELETE CASCADE
);

CREATE TABLE dbo.ProductTags (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    ProductId uniqueidentifier NOT NULL,
    Value nvarchar(120) NOT NULL,
    CONSTRAINT FK_ProductTags_Products FOREIGN KEY(ProductId) REFERENCES dbo.Products(Id) ON DELETE CASCADE
);

CREATE TABLE dbo.ProductEmbroideryPolicies (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    ProductId uniqueidentifier NOT NULL UNIQUE,
    BasePrice decimal(18,2) NOT NULL,
    PerThreadColorPrice decimal(18,2) NOT NULL,
    PerSquareCentimeterPrice decimal(18,2) NOT NULL,
    MaxThreadColors int NOT NULL,
    MaxWidthCm decimal(9,2) NOT NULL,
    MaxHeightCm decimal(9,2) NOT NULL,
    AllowArtworkUpload bit NOT NULL,
    AllowTextEmbroidery bit NOT NULL,
    CONSTRAINT FK_ProductEmbroideryPolicies_Products FOREIGN KEY(ProductId) REFERENCES dbo.Products(Id) ON DELETE CASCADE
);

CREATE TABLE dbo.ProductAllowedPlacements (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    ProductEmbroideryPolicyId uniqueidentifier NOT NULL,
    Placement nvarchar(80) NOT NULL,
    CONSTRAINT FK_ProductAllowedPlacements_Policies FOREIGN KEY(ProductEmbroideryPolicyId) REFERENCES dbo.ProductEmbroideryPolicies(Id) ON DELETE CASCADE
);

CREATE TABLE dbo.ProductAllowedThreadColors (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    ProductEmbroideryPolicyId uniqueidentifier NOT NULL,
    ColorHex nvarchar(20) NOT NULL,
    CONSTRAINT FK_ProductAllowedThreadColors_Policies FOREIGN KEY(ProductEmbroideryPolicyId) REFERENCES dbo.ProductEmbroideryPolicies(Id) ON DELETE CASCADE
);

CREATE TABLE dbo.Customers (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    FullName nvarchar(180) NOT NULL,
    Mobile nvarchar(20) NOT NULL UNIQUE,
    Email nvarchar(260) NULL,
    CreatedAt datetimeoffset NOT NULL
);

CREATE TABLE dbo.CustomerAddresses (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    CustomerId uniqueidentifier NOT NULL,
    RecipientName nvarchar(180) NOT NULL,
    Mobile nvarchar(20) NOT NULL,
    Province nvarchar(90) NOT NULL,
    City nvarchar(90) NOT NULL,
    PostalCode nvarchar(20) NOT NULL,
    AddressLine nvarchar(900) NOT NULL,
    Plaque nvarchar(30) NULL,
    Unit nvarchar(30) NULL,
    IsDefault bit NOT NULL,
    CONSTRAINT FK_CustomerAddresses_Customers FOREIGN KEY(CustomerId) REFERENCES dbo.Customers(Id) ON DELETE CASCADE
);

CREATE TABLE dbo.Coupons (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    Code nvarchar(80) NOT NULL UNIQUE,
    Type nvarchar(40) NOT NULL,
    Value decimal(18,2) NOT NULL,
    StartsAt datetimeoffset NOT NULL,
    EndsAt datetimeoffset NULL,
    UsageLimit int NULL,
    UsageCount int NOT NULL,
    MinimumOrderAmount decimal(18,2) NULL,
    IsActive bit NOT NULL
);

CREATE TABLE dbo.Orders (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    OrderNumber nvarchar(40) NOT NULL UNIQUE,
    CustomerId uniqueidentifier NOT NULL,
    CustomerName nvarchar(180) NOT NULL,
    CustomerMobile nvarchar(20) NOT NULL,
    ShippingRecipientName nvarchar(180) NOT NULL,
    ShippingMobile nvarchar(20) NOT NULL,
    ShippingProvince nvarchar(90) NOT NULL,
    ShippingCity nvarchar(90) NOT NULL,
    ShippingPostalCode nvarchar(20) NOT NULL,
    ShippingAddressLine nvarchar(900) NOT NULL,
    ShippingPlaque nvarchar(30) NULL,
    ShippingUnit nvarchar(30) NULL,
    CreatedAt datetimeoffset NOT NULL,
    Status nvarchar(60) NOT NULL,
    PaymentStatus nvarchar(60) NOT NULL,
    Subtotal decimal(18,2) NOT NULL,
    ShippingAmount decimal(18,2) NOT NULL,
    DiscountAmount decimal(18,2) NOT NULL,
    Total decimal(18,2) NOT NULL,
    TrackingCode nvarchar(120) NULL,
    AdminNote nvarchar(1200) NULL
);

CREATE TABLE dbo.OrderLines (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    OrderId uniqueidentifier NOT NULL,
    ProductId uniqueidentifier NOT NULL,
    VariantId uniqueidentifier NOT NULL,
    ProductName nvarchar(240) NOT NULL,
    ProductSlug nvarchar(260) NOT NULL,
    ProductImageUrl nvarchar(1000) NOT NULL,
    Sku nvarchar(80) NOT NULL,
    Size nvarchar(30) NOT NULL,
    ColorName nvarchar(80) NOT NULL,
    ColorHex nvarchar(20) NOT NULL,
    Quantity int NOT NULL,
    UnitGarmentPrice decimal(18,2) NOT NULL,
    EmbroideryId uniqueidentifier NOT NULL,
    EmbroideryPlacement nvarchar(80) NOT NULL,
    EmbroideryWidthCm decimal(9,2) NOT NULL,
    EmbroideryHeightCm decimal(9,2) NOT NULL,
    EmbroideryThreadColorCount int NOT NULL,
    EmbroideryThreadColorHexesCsv nvarchar(400) NOT NULL,
    EmbroideryArtworkFileUrl nvarchar(1000) NULL,
    EmbroideryArtworkFileName nvarchar(260) NULL,
    EmbroideryText nvarchar(400) NULL,
    EmbroideryFontName nvarchar(120) NULL,
    EmbroideryNote nvarchar(1200) NULL,
    EmbroideryCalculatedPrice decimal(18,2) NOT NULL,
    EmbroideryGarmentType nvarchar(80) NOT NULL,
    EmbroideryGarmentSize nvarchar(30) NOT NULL,
    EmbroideryGarmentColorHex nvarchar(20) NOT NULL,
    EmbroideryDesignSource nvarchar(60) NOT NULL,
    EmbroideryMotifKey nvarchar(80) NULL,
    EmbroideryPositionX int NOT NULL,
    EmbroideryPositionY int NOT NULL,
    EmbroideryScalePercent int NOT NULL,
    EmbroideryRotationDegrees int NOT NULL,
    EmbroideryOpacityPercent int NOT NULL,
    CONSTRAINT FK_OrderLines_Orders FOREIGN KEY(OrderId) REFERENCES dbo.Orders(Id) ON DELETE CASCADE
);
GO
