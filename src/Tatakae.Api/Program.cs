using Microsoft.AspNetCore.Mvc;
using Hangfire;
using Hangfire.SqlServer;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Tatakae.Api.Filters;
using Tatakae.Api.Middleware;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.DependencyInjection;
using Tatakae.Application.Security;
using Tatakae.Infrastructure.DependencyInjection;
using Tatakae.Infrastructure.Persistence;
using Tatakae.Infrastructure.Inventory;
using Tatakae.Infrastructure.Jobs;
using Tatakae.Infrastructure.Payments.Zarinpal;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ResultDtoErrorFilter>();
}).ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "مقدار واردشده معتبر نیست."
                        : error.ErrorMessage)
                    .ToArray());

        var result = new ResultDto().ValidationFailed(
            "اطلاعات فرم معتبر نیست. فیلدهای مشخص‌شده را اصلاح کنید.",
            errors,
            "model_validation_failed");

        return new BadRequestObjectResult(result);
    };
});

builder.Services.AddProblemDetails();

builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        var result = new ResultDto().Failed(
            "تعداد درخواست‌های شما بیش از حد مجاز است. لطفاً چند دقیقه دیگر دوباره تلاش کنید.",
            ResultStatus.Failure,
            "rate_limit_exceeded");

        await context.HttpContext.Response.WriteAsJsonAsync(result, cancellationToken: token);
    };

    // Policy for Login and Register (e.g., max 5 requests per 2 minutes per IP)
    options.AddPolicy("AuthLimit", context =>
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";

        return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(2),
                QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

builder.Services.AddTatakaeApplication();
builder.Services.AddTatakaeSqlInfrastructure(builder.Configuration);

var sqlConnectionString = builder.Configuration.GetConnectionString("TatakaeSqlServer")
    ?? throw new InvalidOperationException("Connection string 'TatakaeSqlServer' is missing.");

builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(sqlConnectionString, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));

builder.Services.AddHangfireServer(options =>
{
    options.Queues = ["inventory", "default"];
});

var jwtKey = builder.Configuration["Jwt:SigningKey"] ?? "CHANGE_THIS_DEVELOPMENT_KEY_32_CHARS_MINIMUM";
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in PermissionNames.All)
    {
        options.AddPolicy(permission, policy => policy.RequireAuthenticatedUser().RequireClaim(PermissionClaimTypes.Permission, permission));
    }
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").GetChildren().Select(x => x.Value).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().ToArray();
if (allowedOrigins.Length == 0) allowedOrigins = ["http://localhost:5076", "https://localhost:7076"];
builder.Services.AddCors(options => options.AddPolicy("web", policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

var zarinpalOptions = app.Services
    .GetRequiredService<Microsoft.Extensions.Options.IOptions<ZarinpalOptions>>()
    .Value;

app.Logger.LogInformation(
    "Zarinpal configured. Enabled={Enabled} Mode={Mode} MerchantConfigured={MerchantConfigured} RefundEnabled={RefundEnabled} CallbackHost={CallbackHost}",
    zarinpalOptions.Enabled,
    zarinpalOptions.Sandbox ? "Sandbox" : "Production",
    !string.IsNullOrWhiteSpace(zarinpalOptions.MerchantId),
    zarinpalOptions.RefundEnabled,
    Uri.TryCreate(zarinpalOptions.CallbackUrl, UriKind.Absolute, out var zarinpalCallback)
        ? zarinpalCallback.Host
        : "invalid");

if (zarinpalOptions.Enabled && string.IsNullOrWhiteSpace(zarinpalOptions.MerchantId))
{
    app.Logger.LogWarning(
        "Zarinpal is enabled in {Mode} mode, but MerchantId is missing. Online payment requests will be rejected until the secret is configured.",
        zarinpalOptions.Sandbox ? "Sandbox" : "Production");
}

await app.Services.InitialiseTatakaeDatabaseAsync();

using (var scope = app.Services.CreateScope())
{
    var reservationOptions = scope.ServiceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<InventoryReservationOptions>>()
        .Value;
    var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    recurringJobs.AddOrUpdate<InventoryReservationCleanupJob>(
        "inventory-reservations-expire",
        "inventory",
        job => job.RunAsync(CancellationToken.None),
        reservationOptions.CleanupCron,
        new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Utc
        });
}

app.UseMiddleware<ResultDtoExceptionMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseRateLimiter();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok", application = "Tatakae.Embroidery.Api", storage = "sql-server", database = "TatakaeSqlServer", auth = "identity-jwt-permissions" }));

app.Run();