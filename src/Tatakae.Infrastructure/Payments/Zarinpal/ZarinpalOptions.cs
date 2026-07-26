namespace Tatakae.Infrastructure.Payments.Zarinpal;

public sealed class ZarinpalOptions
{
    public const string SectionName = "Zarinpal";

    public string MerchantId { get; set; } = string.Empty;

    /// <summary>
    /// Separate Zarinpal access token used by GraphQL services such as Refund.
    /// It must be supplied through a secret store/environment variable.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Enables the Zarinpal adapter. Keep this true for sandbox testing and false
    /// when the store should not offer online payments at all.
    /// </summary>
    public bool Enabled { get; set; } = true;

    public bool Sandbox { get; set; } = true;

    /// <summary>
    /// Refund is disabled by default in sandbox because the configured production
    /// GraphQL endpoint must never be called accidentally from a test environment.
    /// </summary>
    public bool RefundEnabled { get; set; } = false;

    /// <summary>
    /// Tatakae stores and displays prices in toman, therefore IRT is the safe default.
    /// Zarinpal also supports IRR when the whole application stores rial amounts.
    /// </summary>
    public string Currency { get; set; } = "IRT";

    public string CallbackUrl { get; set; } = "https://localhost:7075/api/payments/zarinpal/callback";
    public string ProductionApiBaseUrl { get; set; } = "https://payment.zarinpal.com/";
    public string SandboxApiBaseUrl { get; set; } = "https://sandbox.zarinpal.com/";
    public string ProductionStartPayBaseUrl { get; set; } = "https://www.zarinpal.com/pg/StartPay/";
    public string SandboxStartPayBaseUrl { get; set; } = "https://sandbox.zarinpal.com/pg/StartPay/";
    public string GraphQlUrl { get; set; } = "https://next.zarinpal.com/api/v4/graphql/";

    /// <summary>
    /// Optional sandbox-only GraphQL endpoint. Leave empty unless Zarinpal has
    /// explicitly provided a sandbox Refund endpoint for the merchant account.
    /// </summary>
    public string SandboxGraphQlUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}
