namespace Tatakae.Web.Tests;

public sealed class PaymentResultSecurityTests
{
    [Fact]
    public void Payment_result_confirms_success_against_server_state()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "ArchitectureFixtures");
        var source = File.ReadAllText(Path.Combine(root, "Pages", "PaymentResult.razor"));

        Assert.Contains("Payments.GetForOrderAsync", source);
        Assert.Contains("payment.Id != expectedPaymentId", source);
        Assert.Contains("\"Verified\" or \"Succeeded\" => \"success\"", source);
        Assert.Contains("\"Refunded\" or \"Reversed\" => \"refunded\"", source);
        Assert.DoesNotContain(
            "private bool IsSuccess => string.Equals(Status",
            source);
        Assert.DoesNotContain("SupplyParameterFromQuery(Name = \"status\")", source);
        Assert.DoesNotContain("SupplyParameterFromQuery(Name = \"message\")", source);
        Assert.DoesNotContain("SupplyParameterFromQuery(Name = \"errorCode\")", source);
        Assert.DoesNotContain("payment.GatewayMessage ?? Message", source);
    }
}
