# مدل فروشگاه گلدوزی

## Product Aggregate

```text
Category 1 ─── * Product 1 ─── * ProductVariant
                      │
                      ├── * ProductImage
                      ├── * ProductSpecification
                      ├── 1 SeoMetadata
                      └── 1 EmbroideryPolicy
```

### Product

- نام، Slug و دسته‌بندی
- توضیح کوتاه و کامل
- جنس، فیت، راهنمای نگهداری و لینک جدول سایز
- تصاویر با alt text و ترتیب
- Tagها، انتشار، featured و متادیتای SEO

### ProductVariant

تنها واحد قابل فروش و قابل موجودی‌گیری است:

- SKU یکتا
- سایز و رنگ/HEX
- قیمت اصلی و فروش ویژه
- موجودی و وضعیت فعال

### EmbroideryPolicy

هر لباس قوانین مستقل دارد:

- هزینه پایه
- هزینه رنگ نخ اضافه
- هزینه مساحت گلدوزی
- حد اکثر رنگ نخ، عرض و ارتفاع
- محل‌های مجاز اجرا
- رنگ‌های نخ مجاز
- فعال/غیرفعال‌بودن آپلود طرح و گلدوزی متن

### Order

هر خط سفارش، اسنپ‌شات لباس و گلدوزی را نگه می‌دارد تا تغییرات بعدی محصول، سابقه سفارش را خراب نکند.

```text
Order
 ├── Customer snapshot
 ├── Shipping Address snapshot
 ├── payment/status/shipping/discount totals
 └── OrderLine[]
       ├── Product/Variant snapshot
       └── EmbroideryConfiguration
           ├── placement
           ├── width/height
           ├── thread colors
           ├── artwork/text/font
           └── calculated price
```
