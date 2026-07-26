using Microsoft.EntityFrameworkCore;
using Tatakae.Api.Controllers;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Legal;
using Tatakae.Application.Contracts.Payments;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Interfaces;
using Tatakae.Application.Interfaces.Gateways;
using Tatakae.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Tatakae.Application.Security;
using Tatakae.Infrastructure.Persistence;
using Tatakae.Infrastructure.Gateways;
using Tatakae.Infrastructure.Payments.Zarinpal;

namespace Tatakae.Api.Tests;

public sealed class CleanArchitectureServiceBoundaryTests
{
    [Fact]
    public void Api_DoesNotContainBusinessServiceImplementations()
    {
        var apiAssembly = typeof(AccountController).Assembly;

        Assert.DoesNotContain(
            apiAssembly.GetTypes(),
            type => type.Namespace is not null
                    && type.Namespace.StartsWith("Tatakae.Api.Services", StringComparison.Ordinal));
    }

    [Fact]
    public void Application_DoesNotReferenceApiOrInfrastructure()
    {
        var references = typeof(IIdentityAuthService).Assembly
            .GetReferencedAssemblies()
            .Select(x => x.Name)
            .ToArray();

        Assert.DoesNotContain("Tatakae.Api", references);
        Assert.DoesNotContain("Tatakae.Infrastructure", references);
    }


    [Fact]
    public void ApiControllers_DoNotDependOnInfrastructureTypes()
    {
        var controllerTypes = typeof(AccountController).Assembly
            .GetTypes()
            .Where(type => type.IsClass
                           && !type.IsAbstract
                           && typeof(Microsoft.AspNetCore.Mvc.ControllerBase).IsAssignableFrom(type))
            .ToArray();

        foreach (var controllerType in controllerTypes)
        {
            foreach (var parameter in controllerType.GetConstructors().SelectMany(x => x.GetParameters()))
            {
                Assert.NotEqual("Tatakae.Infrastructure", parameter.ParameterType.Assembly.GetName().Name);
            }
        }
    }

    [Fact]
    public void Application_ImplementsUseCaseContracts()
    {
        Assert.True(typeof(IIdentityAuthService).IsAssignableFrom(typeof(IdentityAuthService)));
        Assert.True(typeof(ILegalContentService).IsAssignableFrom(typeof(LegalContentService)));
        Assert.True(typeof(IPaymentService).IsAssignableFrom(typeof(PaymentService)));
        Assert.True(typeof(ISecurityAdminService).IsAssignableFrom(typeof(SecurityAdminService)));
        Assert.True(typeof(IPermissionService).IsAssignableFrom(typeof(PermissionService)));
        Assert.True(typeof(ICartPersistenceService).IsAssignableFrom(typeof(CartPersistenceService)));
        Assert.True(typeof(ILocationService).IsAssignableFrom(typeof(LocationService)));
    }

    [Fact]
    public void Infrastructure_ImplementsApplicationGatewayPorts()
    {
        Assert.True(typeof(IIdentityAuthGateway).IsAssignableFrom(typeof(AspNetIdentityAuthGateway)));
        Assert.True(typeof(ILegalContentGateway).IsAssignableFrom(typeof(EfLegalContentGateway)));
        Assert.True(typeof(IZarinpalPaymentGateway).IsAssignableFrom(typeof(ZarinpalPaymentGateway)));
        Assert.True(typeof(IPaymentRepository).IsAssignableFrom(typeof(EfPaymentRepository)));
        Assert.True(typeof(ISecurityAdminGateway).IsAssignableFrom(typeof(EfSecurityAdminGateway)));
        Assert.True(typeof(IPermissionGateway).IsAssignableFrom(typeof(EfPermissionGateway)));
        Assert.True(typeof(ICartPersistenceGateway).IsAssignableFrom(typeof(EfCartPersistenceGateway)));
        Assert.True(typeof(ILocationGateway).IsAssignableFrom(typeof(EfLocationGateway)));
    }


    [Fact]
    public void ApplicationUseCases_DependOnGatewayPorts_NotInfrastructureImplementations()
    {
        var useCases = new[]
        {
            typeof(IdentityAuthService),
            typeof(LegalContentService),
            typeof(PaymentService),
            typeof(SecurityAdminService),
            typeof(PermissionService),
            typeof(CartPersistenceService),
            typeof(LocationService)
        };

        foreach (var useCase in useCases)
        {
            var constructor = Assert.Single(useCase.GetConstructors());
            Assert.DoesNotContain(
                constructor.GetParameters(),
                parameter => parameter.ParameterType.Assembly.GetName().Name == "Tatakae.Infrastructure");

            Assert.Contains(
                constructor.GetParameters(),
                parameter => parameter.ParameterType.IsInterface
                             && parameter.ParameterType.Namespace == "Tatakae.Application.Interfaces.Gateways");
        }
    }

    [Theory]
    [InlineData(typeof(AccountController), typeof(IIdentityAuthService))]
    [InlineData(typeof(StorePagesController), typeof(ILegalContentService))]
    [InlineData(typeof(AdminLegalController), typeof(ILegalContentService))]
    [InlineData(typeof(PaymentsController), typeof(IPaymentService))]
    [InlineData(typeof(AdminPaymentsController), typeof(IPaymentService))]
    [InlineData(typeof(AdminSecurityController), typeof(ISecurityAdminService))]
    [InlineData(typeof(CartController), typeof(ICartPersistenceService))]
    [InlineData(typeof(LocationsController), typeof(ILocationService))]
    public void ApiControllers_DependOnApplicationInterfaces(Type controllerType, Type serviceContract)
    {
        var constructor = Assert.Single(controllerType.GetConstructors());
        Assert.Contains(constructor.GetParameters(), parameter => parameter.ParameterType == serviceContract);
    }


    [Fact]
    public void EveryApiController_ServiceDependency_IsAnApplicationInterface()
    {
        var controllerTypes = typeof(AccountController).Assembly
            .GetTypes()
            .Where(type => type.IsClass
                           && !type.IsAbstract
                           && typeof(Microsoft.AspNetCore.Mvc.ControllerBase).IsAssignableFrom(type))
            .ToArray();

        foreach (var controllerType in controllerTypes)
        {
            foreach (var parameter in controllerType.GetConstructors().SelectMany(x => x.GetParameters()))
            {
                if (!parameter.ParameterType.Name.EndsWith("Service", StringComparison.Ordinal))
                    continue;

                Assert.True(
                    parameter.ParameterType.IsInterface,
                    $"{controllerType.Name} must depend on an interface, but receives {parameter.ParameterType.FullName}.");
                Assert.Equal("Tatakae.Application.Interfaces.Services", parameter.ParameterType.Namespace);
            }
        }
    }

    [Fact]
    public void ApplicationDi_ExposesUseCasesOnlyThroughInterfaces()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        Tatakae.Application.DependencyInjection.ApplicationServiceCollection.AddTatakaeApplication(services);

        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ServiceType.IsClass
                          && descriptor.ServiceType.Namespace == "Tatakae.Application.Services"
                          && descriptor.ServiceType.Name.EndsWith("Service", StringComparison.Ordinal));

        Assert.All(
            services.Where(x => x.ServiceType.Name.EndsWith("Service", StringComparison.Ordinal)),
            descriptor => Assert.True(descriptor.ServiceType.IsInterface));
    }

    [Fact]
    public void PaymentUseCase_SeparatesProviderGatewayFromPersistence()
    {
        var constructor = Assert.Single(typeof(PaymentService).GetConstructors());
        var parameters = constructor.GetParameters().Select(x => x.ParameterType).ToArray();

        Assert.Contains(typeof(IZarinpalPaymentGateway), parameters);
        Assert.Contains(typeof(IPaymentRepository), parameters);
        Assert.Contains(typeof(IOrderRepository), parameters);
        Assert.DoesNotContain(typeof(TatakaeDbContext), parameters);
    }


    [Fact]
    public void ZarinpalAdapter_DependsOnlyOnHttpAndOptions_NotPersistenceOrOrders()
    {
        var constructor = Assert.Single(typeof(ZarinpalPaymentGateway).GetConstructors());
        var parameters = constructor.GetParameters().Select(x => x.ParameterType).ToArray();

        Assert.Contains(typeof(HttpClient), parameters);
        Assert.Contains(
            typeof(Microsoft.Extensions.Options.IOptions<ZarinpalOptions>),
            parameters);
        Assert.DoesNotContain(typeof(TatakaeDbContext), parameters);
        Assert.DoesNotContain(typeof(IOrderRepository), parameters);
        Assert.DoesNotContain(typeof(IPaymentRepository), parameters);
    }

    [Fact]
    public void PaymentPersistenceCommand_UsesImmutableOrderSnapshot_NotDomainAggregate()
    {
        var properties = typeof(PersistPaymentOutcome).GetProperties();

        Assert.Contains(properties, property => property.Name == nameof(PersistPaymentOutcome.OrderState)
                                                && property.PropertyType == typeof(OrderPaymentState));
        Assert.DoesNotContain(properties, property => property.PropertyType == typeof(Tatakae.Domain.Entities.Order));
    }

    [Fact]
    public void PaymentsController_ExposesAnonymousZarinpalCallback_AndNoDemoEndpoint()
    {
        var callback = typeof(PaymentsController).GetMethod(
            nameof(PaymentsController.ZarinpalCallback));

        Assert.NotNull(callback);
        Assert.NotNull(callback!.GetCustomAttributes(
            typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute),
            inherit: true).SingleOrDefault());
        Assert.DoesNotContain(
            typeof(PaymentsController).GetMethods(),
            method => method.Name.Contains("Demo", StringComparison.OrdinalIgnoreCase) ||
                      method.Name.Contains("Simulate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task LegalInterface_ReturnsValidationResultForDuplicateSlug()
    {
        await using var db = CreateDbContext();
        db.StorePolicyPages.AddRange(
            Policy("first-page"),
            Policy("second-page"));
        await db.SaveChangesAsync();
        ILegalContentService service = new LegalContentService(new EfLegalContentGateway(db), NullLogger<LegalContentService>.Instance);

        var result = await service.UpsertPageAsync(
            "first-page",
            new UpsertStorePolicyPageRequest
            {
                Slug = "second-page",
                Title = "عنوان صفحه قانونی فروشگاه",
                Summary = "خلاصه معتبر و کامل برای صفحه قانونی فروشگاه Tatakae نوشته شده است.",
                Body = "<p>این متن کامل برای صفحه قانونی فروشگاه نوشته شده و حداقل طول لازم برای ذخیره‌سازی و تست را دارد.</p>",
                IsPublished = true,
                SortOrder = 10
            });

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.ValidationError, result.Status);
        Assert.Contains("تکراری", result.Message);
        Assert.Null(result.Data);
    }

    private static TatakaeDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TatakaeDbContext>()
            .UseInMemoryDatabase($"tatakae-clean-architecture-{Guid.NewGuid():N}")
            .Options;

        return new TatakaeDbContext(options);
    }

    private static Tatakae.Infrastructure.Persistence.Models.StorePolicyPageDbRecord Policy(string slug)
        => new()
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Title = "عنوان صفحه",
            Summary = "خلاصه معتبر برای صفحه قانونی فروشگاه Tatakae و توضیح هدف این صفحه.",
            Body = "<p>این متن کامل و معتبر برای تست محتوای صفحه قانونی فروشگاه Tatakae نوشته شده است.</p>",
            IsPublished = true,
            UpdatedAt = DateTimeOffset.UtcNow
        };
}
