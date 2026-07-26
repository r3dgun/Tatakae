namespace Tatakae.Web.Tests;

public sealed class FormValidationContractTests
{
    private static string Fixture(string name)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [Fact]
    public void Index_loads_validation_styles_after_responsive_styles()
    {
        var html = Fixture("index.html");
        var responsive = html.IndexOf("css/phase13-responsive.css", StringComparison.Ordinal);
        var validation = html.IndexOf("css/validation.css", StringComparison.Ordinal);

        Assert.True(responsive >= 0);
        Assert.True(validation > responsive);
    }

    [Fact]
    public void Validation_component_validates_the_complete_object_graph()
    {
        var component = Fixture("RecursiveDataAnnotationsValidator.cs");

        Assert.Contains("ObjectGraphValidator.Validate(subscribedEditContext.Model)", component);
        Assert.Contains("OnValidationRequested", component);
        Assert.Contains("OnFieldChanged", component);
        Assert.Contains("ReferenceEquals(CurrentEditContext, subscribedEditContext)", component);
    }

    [Theory]
    [InlineData("Checkout.razor")]
    [InlineData("ProductDetail.razor")]
    [InlineData("Login.razor")]
    [InlineData("ProductEditor.razor")]
    public void Input_forms_use_recursive_validation(string file)
    {
        var razor = Fixture(file);

        Assert.Contains("<EditForm", razor);
        Assert.Contains("<RecursiveDataAnnotationsValidator", razor);
    }

    [Fact]
    public void AuthorizeView_inside_EditForm_uses_an_explicit_context_name()
    {
        var razor = Fixture("AdminLegal.razor");

        Assert.Contains("<EditForm", razor);
        Assert.Contains("<AuthorizeView", razor);
        Assert.Contains("<Authorized Context=\"legalAuthorization\">", razor);
    }

    [Fact]
    public void Validation_styles_cover_invalid_valid_summary_and_admin_states()
    {
        var css = Fixture("validation.css");

        Assert.Contains("input.invalid", css);
        Assert.Contains(".validation-message", css);
        Assert.Contains(".validation-errors", css);
        Assert.Contains(".admin-shell", css);
    }
}
