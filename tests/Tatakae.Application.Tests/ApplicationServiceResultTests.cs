using System.Reflection;
using Tatakae.Infrastructure.Persistence.Repositories;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Coupons;
using Tatakae.Application.Contracts.Embroidery;
using Tatakae.Application.Contracts.Files;
using Tatakae.Application.Interfaces;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Services;
using Tatakae.Domain.Entities;

namespace Tatakae.Application.Tests;

public sealed class ApplicationServiceResultTests
{
    [Fact]
    public void ResultDto_FactoriesSetExpectedState()
    {
        var command = new ResultDto();
        var valueQuery = new ResultDto<int>();
        var referenceQuery = new ResultDto<string>();

        var failed = command.Failed("ناموفق");
        var succeeded = command.Success("موفق");
        var valueFailed = valueQuery.Failed("خطا");
        var referenceFailed = referenceQuery.Failed("خطا");
        var dataSucceeded = valueQuery.Success("انجام شد", 42);

        Assert.False(failed.IsSuccess);
        Assert.Equal("ناموفق", failed.Message);
        Assert.True(succeeded.IsSuccess);
        Assert.Equal("موفق", succeeded.Message);
        Assert.False(valueFailed.IsSuccess);
        Assert.Equal(default(int), valueFailed.Data);
        Assert.False(referenceFailed.IsSuccess);
        Assert.Null(referenceFailed.Data);
        Assert.True(dataSucceeded.IsSuccess);
        Assert.Equal(42, dataSucceeded.Data);
    }

    [Fact]
    public void CreateStoredFileRequest_IsAnApplicationContract()
    {
        Assert.Equal(
            "Tatakae.Application.Contracts.Files",
            typeof(CreateStoredFileRequest).Namespace);
    }

    [Fact]
    public void InjectableApplicationServices_ImplementResultInterfaces()
    {
        var pairs = new (Type Service, Type Contract)[]
        {
            (typeof(AccountService), typeof(IAccountService)),
            (typeof(AdminCatalogService), typeof(IAdminCatalogService)),
            (typeof(AdminCategoryService), typeof(IAdminCategoryService)),
            (typeof(AdminCouponService), typeof(IAdminCouponService)),
            (typeof(AdminDashboardService), typeof(IAdminDashboardService)),
            (typeof(CatalogService), typeof(ICatalogService)),
            (typeof(CouponService), typeof(ICouponService)),
            (typeof(CustomerService), typeof(ICustomerService)),
            (typeof(EmbroideryArtworkService), typeof(IEmbroideryArtworkService)),
            (typeof(EmbroideryPricingService), typeof(IEmbroideryPricingService)),
            (typeof(InventoryService), typeof(IInventoryService)),
            (typeof(MediaAssetService), typeof(IMediaAssetService)),
            (typeof(NotificationService), typeof(INotificationService)),
            (typeof(OrderService), typeof(IOrderService)),
            (typeof(ProductEngagementService), typeof(IProductEngagementService)),
            (typeof(SeoService), typeof(ISeoService)),
            (typeof(ShippingService), typeof(IShippingService)),
            (typeof(WishlistService), typeof(IWishlistService)),
            (typeof(IdentityAuthService), typeof(IIdentityAuthService)),
            (typeof(LegalContentService), typeof(ILegalContentService)),
            (typeof(PaymentService), typeof(IPaymentService)),
            (typeof(SecurityAdminService), typeof(ISecurityAdminService)),
            (typeof(PermissionService), typeof(IPermissionService)),
            (typeof(CartPersistenceService), typeof(ICartPersistenceService)),
            (typeof(LocationService), typeof(ILocationService))
        };

        foreach (var pair in pairs)
        {
            Assert.True(pair.Contract.IsAssignableFrom(pair.Service), $"{pair.Service.Name} must implement {pair.Contract.Name}.");
        }
    }

    [Fact]
    public void ResultInterfaces_OnlyExposeResultDtoResponses()
    {
        var interfaces = typeof(IAccountService).Assembly
            .GetTypes()
            .Where(x => x.IsInterface && x.Namespace == "Tatakae.Application.Interfaces.Services")
            .ToArray();

        Assert.NotEmpty(interfaces);

        foreach (var contract in interfaces)
        {
            foreach (var method in contract.GetMethods())
            {
                var returnType = method.ReturnType;
                var resultType = returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>)
                    ? returnType.GetGenericArguments()[0]
                    : returnType;

                Assert.True(
                    resultType == typeof(ResultDto)
                    || resultType == typeof(EmbroideryArtworkPolicyDto)
                    || resultType == typeof(UploadPolicyDto)
                    || (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(ResultDto<>)),
                    $"{contract.Name}.{method.Name} must return ResultDto or ResultDto<T>.");
            }
        }
    }


    [Fact]
    public void RepositoryInterfaces_OnlyExposeResultDtoResponses()
    {
        var repositoryInterfaces = typeof(ICouponRepository).Assembly
            .GetTypes()
            .Where(type => type.IsInterface
                           && type.Namespace == "Tatakae.Application.Interfaces"
                           && type.Name.EndsWith("Repository", StringComparison.Ordinal))
            .OrderBy(type => type.Name)
            .ToArray();

        Assert.NotEmpty(repositoryInterfaces);

        foreach (var repositoryInterface in repositoryInterfaces)
        {
            foreach (var method in repositoryInterface.GetMethods())
            {
                var returnType = method.ReturnType;
                Assert.True(
                    returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>),
                    $"{repositoryInterface.Name}.{method.Name} must return Task<ResultDto> or Task<ResultDto<T>>.");

                var resultType = returnType.GetGenericArguments()[0];
                Assert.True(
                    resultType == typeof(ResultDto)
                    || (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(ResultDto<>)),
                    $"{repositoryInterface.Name}.{method.Name} must return ResultDto or ResultDto<T>.");
            }
        }
    }



    [Fact]
    public void SqlRepositories_ExposeOnlyResultDtoInterfaceContract()
    {
        var sqlRepositories = typeof(SqlProductRepository).Assembly
            .GetTypes()
            .Where(type => type.IsClass
                           && !type.IsAbstract
                           && type.Namespace == "Tatakae.Infrastructure.Persistence.Repositories"
                           && type.Name.StartsWith("Sql", StringComparison.Ordinal)
                           && type.Name.EndsWith("Repository", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(sqlRepositories);

        foreach (var repositoryType in sqlRepositories)
        {
            var repositoryInterfaces = repositoryType.GetInterfaces()
                .Where(type => type.Name.EndsWith("Repository", StringComparison.Ordinal))
                .ToArray();

            Assert.NotEmpty(repositoryInterfaces);

            var publicAsyncMethods = repositoryType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => method.Name.EndsWith("Async", StringComparison.Ordinal))
                .ToArray();

            Assert.Empty(publicAsyncMethods);

            foreach (var repositoryInterface in repositoryInterfaces)
            {
                var map = repositoryType.GetInterfaceMap(repositoryInterface);
                Assert.Equal(
                    repositoryInterface.GetMethods().Length,
                    map.TargetMethods.Length);
            }
        }
    }

    [Fact]
    public async Task CouponResultInterface_PreservesRepositoryMessageStatusAndErrorCode()
    {
        const string repositoryMessage = "ارتباط با مخزن کد تخفیف برقرار نشد.";
        ICouponService service = new CouponService(new FailedCouponRepository(repositoryMessage));

        var result = await service.QuoteAsync(new CouponQuoteRequest
        {
            Code = "TEST",
            CartSubtotal = 1_000_000
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(repositoryMessage, result.Message);
        Assert.Equal(ResultStatus.Failure, result.Status);
        Assert.Equal("coupon_repository_unavailable", result.ErrorCode);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task CouponResultInterface_CatchesRepositoryExceptions()
    {
        ICouponService service = new CouponService(new ThrowingCouponRepository());

        var result = await service.QuoteAsync(new CouponQuoteRequest
        {
            Code = "TEST",
            CartSubtotal = 1_000_000
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("خطا", result.Message);
        Assert.Null(result.Data);
    }

    private sealed class FailedCouponRepository(string message) : ICouponRepository
    {
        private ResultDto<T> Failure<T>()
            => new ResultDto<T>().Failed(message, ResultStatus.Failure, "coupon_repository_unavailable");

        public Task<ResultDto<IReadOnlyCollection<Coupon>>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Failure<IReadOnlyCollection<Coupon>>());

        public Task<ResultDto<Coupon>> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
            => Task.FromResult(Failure<Coupon>());

        public Task<ResultDto<Coupon>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Failure<Coupon>());

        public Task<ResultDto<Coupon>> UpsertAsync(Coupon coupon, CancellationToken cancellationToken = default)
            => Task.FromResult(Failure<Coupon>());

        public Task<ResultDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto().Failed(message, ResultStatus.Failure, "coupon_repository_unavailable"));
    }

    private sealed class ThrowingCouponRepository : ICouponRepository
    {
        public Task<ResultDto<IReadOnlyCollection<Coupon>>> GetAllAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("database unavailable");

        public Task<ResultDto<Coupon>> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("database unavailable");

        public Task<ResultDto<Coupon>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("database unavailable");

        public Task<ResultDto<Coupon>> UpsertAsync(Coupon coupon, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("database unavailable");

        public Task<ResultDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("database unavailable");
    }
}
