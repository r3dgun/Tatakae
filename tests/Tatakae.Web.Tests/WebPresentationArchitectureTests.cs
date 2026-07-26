using System.Net;
using System.Net.Http.Json;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Products;
using Tatakae.Application.Contracts.Wishlist;
using Tatakae.Web.ApiClients.Abstractions;
using Tatakae.Web.ApiClients.Http;
using Tatakae.Web.ApiClients.Results;
using Tatakae.Web.State;

namespace Tatakae.Web.Tests;

public sealed class WebPresentationArchitectureTests
{
    [Fact]
    public void Api_clients_implement_presentation_abstractions()
    {
        Assert.Contains(typeof(IStoreApiClient), typeof(StoreApiClient).GetInterfaces());
        Assert.Contains(typeof(IAccountApiClient), typeof(AccountApiClient).GetInterfaces());
        Assert.Contains(typeof(IAdminApiClient), typeof(AdminApiClient).GetInterfaces());
        Assert.Contains(typeof(ICheckoutApiClient), typeof(CheckoutApiClient).GetInterfaces());
        Assert.Contains(typeof(IPaymentApiClient), typeof(PaymentApiClient).GetInterfaces());
        Assert.Contains(typeof(ICartApiClient), typeof(CartApiClient).GetInterfaces());
        Assert.Contains(typeof(IWishlistApiClient), typeof(WishlistApiClient).GetInterfaces());
        Assert.Contains(typeof(IArtworkApiClient), typeof(ArtworkApiClient).GetInterfaces());
        Assert.Contains(typeof(IFileUploadApiClient), typeof(FileUploadApiClient).GetInterfaces());
    }

    [Fact]
    public void Web_assembly_does_not_reference_api_or_infrastructure()
    {
        var references = typeof(StoreApiClient).Assembly
            .GetReferencedAssemblies()
            .Select(x => x.Name)
            .ToArray();

        Assert.DoesNotContain("Tatakae.Api", references);
        Assert.DoesNotContain("Tatakae.Infrastructure", references);
    }

    [Fact]
    public void Browser_state_implements_state_abstractions()
    {
        Assert.Contains(typeof(IAuthSessionStore), typeof(BrowserAuthSessionStore).GetInterfaces());
        Assert.Contains(typeof(ICartState), typeof(BrowserCartState).GetInterfaces());
    }

    [Fact]
    public void Legacy_services_namespace_and_fallback_catalog_are_removed()
    {
        var types = typeof(StoreApiClient).Assembly.GetTypes();

        Assert.False(types.Any(type => type.Namespace == "Tatakae.Web.Services"));
        Assert.False(types.Any(type => type.Name == "StoreFallbackCatalog"));
    }

    [Fact]
    public void Razor_components_inject_web_abstractions_instead_of_concrete_clients()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "ArchitectureFixtures");
        var razorFiles = Directory.GetFiles(root, "*.razor", SearchOption.AllDirectories);
        var forbidden = new[]
        {
            "@inject StoreApiClient ",
            "@inject AccountApiClient ",
            "@inject AdminApiClient ",
            "@inject CheckoutApiClient ",
            "@inject PaymentApiClient ",
            "@inject CartApiClient ",
            "@inject WishlistApiClient ",
            "@inject ArtworkApiClient ",
            "@inject FileUploadApiClient ",
            "@inject AuthSessionStore ",
            "@inject CartStore "
        };

        Assert.NotEmpty(razorFiles);
        foreach (var file in razorFiles)
        {
            var source = File.ReadAllText(file);
            Assert.All(forbidden, token => Assert.False(source.Contains(token, StringComparison.Ordinal), $"Concrete dependency {token} found in {file}."));
        }
    }

    [Fact]
    public void Web_does_not_reference_concrete_seo_service_and_uses_typed_http_client_factory()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "ArchitectureFixtures");
        var sources = Directory.GetFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Select(File.ReadAllText)
            .ToArray();

        Assert.NotEmpty(sources);
        Assert.All(sources, source => Assert.False(source.Contains("SeoService.", StringComparison.Ordinal)));

        var program = File.ReadAllText(Path.Combine(root, "Program.cs"));
        Assert.True(program.Contains("AddScoped<HttpClient>(CreateApiHttpClient)", StringComparison.Ordinal));
        Assert.False(program.Contains("AddScoped(sp =>", StringComparison.Ordinal));
    }

    [Fact]
    public void Web_composition_root_registers_interfaces_and_shared_transport()
    {
        var program = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "ArchitectureFixtures",
            "Program.cs"));

        Assert.Contains("AddScoped<IStoreApiClient, StoreApiClient>()", program);
        Assert.Contains("AddScoped<IAuthSessionStore, BrowserAuthSessionStore>()", program);
        Assert.Contains("AddScoped<ICartState, BrowserCartState>()", program);
        Assert.Contains("AddScoped<IApiResultReader, ApiResultReader>()", program);
        Assert.Contains("AddScoped<IApiClientTransport, ApiClientTransport>()", program);
        Assert.DoesNotContain("AddScoped<StoreApiClient>()", program);
        Assert.DoesNotContain("AddScoped<AuthSessionStore>()", program);
        Assert.DoesNotContain("AddScoped<CartStore>()", program);
    }


    [Fact]
    public void Every_web_api_client_method_returns_result_dto()
    {
        var clientInterfaces = new[]
        {
            typeof(IStoreApiClient),
            typeof(IAccountApiClient),
            typeof(IAdminApiClient),
            typeof(ICheckoutApiClient),
            typeof(IPaymentApiClient),
            typeof(ICartApiClient),
            typeof(IWishlistApiClient),
            typeof(IArtworkApiClient),
            typeof(IFileUploadApiClient)
        };

        foreach (var clientInterface in clientInterfaces)
        {
            foreach (var method in clientInterface.GetMethods())
            {
                Assert.True(method.ReturnType.IsGenericType, $"{clientInterface.Name}.{method.Name} must return Task<ResultDto>.");
                Assert.Equal(typeof(Task<>), method.ReturnType.GetGenericTypeDefinition());

                var resultType = method.ReturnType.GetGenericArguments()[0];
                var isResultDto = resultType == typeof(ResultDto)
                    || resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(ResultDto<>);

                Assert.True(isResultDto, $"{clientInterface.Name}.{method.Name} returns {method.ReturnType} instead of ResultDto.");
            }
        }
    }

    [Fact]
    public void Web_transport_exposes_only_result_based_operations()
    {
        var methods = typeof(IApiClientTransport).GetMethods();

        Assert.NotEmpty(methods);
        Assert.All(methods, method =>
        {
            Assert.True(method.ReturnType.IsGenericType);
            Assert.Equal(typeof(Task<>), method.ReturnType.GetGenericTypeDefinition());

            var resultType = method.ReturnType.GetGenericArguments()[0];
            Assert.True(resultType == typeof(ResultDto)
                || resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(ResultDto<>));
        });

        Assert.DoesNotContain(methods, method => method.Name.Contains("Required", StringComparison.Ordinal));
        Assert.DoesNotContain(methods, method => method.Name.Contains("Optional", StringComparison.Ordinal));
    }

    [Fact]
    public void Result_ui_extension_names_do_not_collide_with_application_extensions()
    {
        var applicationNames = typeof(ResultDtoExtensions)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(method => method.Name)
            .ToHashSet(StringComparer.Ordinal);

        var uiNames = typeof(ResultDtoUiExtensions)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(method => method.Name)
            .ToArray();

        Assert.All(uiNames, name => Assert.DoesNotContain(name, applicationNames));
        Assert.Contains("RequireUiData", uiNames);
        Assert.Contains("EnsureUiSuccess", uiNames);
    }

    [Fact]
    public void Result_ui_extensions_preserve_persian_failure_details()
    {
        var failure = new ResultDto<SampleDto>().ValidationFailed(
            "اطلاعات فرم معتبر نیست.",
            new Dictionary<string, string[]> { ["Name"] = ["نام الزامی است."] },
            "validation_failed");

        var exception = Assert.Throws<ApiClientException>(() => failure.RequireUiData());

        Assert.Equal("اطلاعات فرم معتبر نیست.", exception.Message);
        Assert.Equal(ResultStatus.ValidationError, exception.Result.Status);
        Assert.Equal("validation_failed", exception.Result.ErrorCode);
        Assert.Equal("نام الزامی است.", exception.Result.Errors!["Name"][0]);
    }

    [Fact]
    public async Task Api_result_reader_unwraps_successful_result_dto()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new ResultDto<SampleDto>().Success(
                "اطلاعات دریافت شد.",
                new SampleDto(42)))
        };

        var result = await new ApiResultReader().ReadAsync<SampleDto>(
            response,
            "خطای پیش‌فرض");

        Assert.True(result.IsSuccess);
        Assert.Equal("اطلاعات دریافت شد.", result.Message);
        Assert.NotNull(result.Data);
        Assert.Equal(42, result.Data.Value);
    }

    [Fact]
    public async Task Api_result_reader_preserves_persian_error_and_field_errors()
    {
        var fieldErrors = new Dictionary<string, string[]>
        {
            ["Mobile"] = ["شماره موبایل معتبر نیست."]
        };

        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = JsonContent.Create(new ResultDto().ValidationFailed(
                "اطلاعات فرم معتبر نیست.",
                fieldErrors,
                "model_validation_failed"))
        };

        var result = await new ApiResultReader().ReadAsync<SampleDto>(
            response,
            "خطای پیش‌فرض");

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.ValidationError, result.Status);
        Assert.Equal("اطلاعات فرم معتبر نیست.", result.Message);
        Assert.Equal("model_validation_failed", result.ErrorCode);
        Assert.NotNull(result.Errors);
        Assert.Equal("شماره موبایل معتبر نیست.", result.Errors["Mobile"][0]);
    }

    [Fact]
    public async Task Api_result_reader_supports_legacy_raw_success_payloads()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new SampleDto(7))
        };

        var result = await new ApiResultReader().ReadAsync<SampleDto>(
            response,
            "خطای پیش‌فرض");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(7, result.Data.Value);
    }

    [Fact]
    public async Task Api_result_reader_deserializes_wishlist_contract_in_browser_shape()
    {
        var productId = Guid.NewGuid();
        var payload = new WishlistDto(
            Guid.NewGuid(),
            new[]
            {
                new ProductCardDto(
                    productId,
                    "محصول تست",
                    "test-product",
                    "دسته تست",
                    "test-category",
                    "/images/test.webp",
                    "تصویر محصول تست",
                    "توضیح کوتاه محصول تست",
                    125_000m,
                    null,
                    true,
                    false,
                    true,
                    new[] { "test" })
            },
            1,
            DateTimeOffset.UtcNow);

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new ResultDto<WishlistDto>().Success(
                "علاقه‌مندی‌ها دریافت شدند.",
                payload))
        };

        var result = await new ApiResultReader().ReadAsync<WishlistDto>(
            response,
            "دریافت علاقه‌مندی‌ها ناموفق بود.");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Items);
        Assert.Equal(productId, result.Data.Items.Single().Id);
    }

    public sealed record SampleDto(int Value);
}
