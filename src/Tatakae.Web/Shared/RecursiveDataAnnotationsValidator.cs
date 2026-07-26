using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Tatakae.Application.Validation;

namespace Tatakae.Web.Shared;

public sealed class RecursiveDataAnnotationsValidator : ComponentBase, IDisposable
{
    [CascadingParameter] private EditContext? CurrentEditContext { get; set; }

    private EditContext? subscribedEditContext;
    private ValidationMessageStore? messages;

    protected override void OnParametersSet()
    {
        if (CurrentEditContext is null)
        {
            throw new InvalidOperationException($"{nameof(RecursiveDataAnnotationsValidator)} requires a cascading EditContext. Place it inside an EditForm.");
        }

        if (ReferenceEquals(CurrentEditContext, subscribedEditContext)) return;

        Unsubscribe();
        subscribedEditContext = CurrentEditContext;
        messages = new ValidationMessageStore(subscribedEditContext);
        subscribedEditContext.OnValidationRequested += OnValidationRequested;
        subscribedEditContext.OnFieldChanged += OnFieldChanged;
    }

    private void OnValidationRequested(object? sender, ValidationRequestedEventArgs args)
        => ValidateObjectGraph();

    private void OnFieldChanged(object? sender, FieldChangedEventArgs args)
        => ValidateObjectGraph();

    private void ValidateObjectGraph()
    {
        if (subscribedEditContext is null || messages is null) return;

        messages.Clear();
        foreach (var error in ObjectGraphValidator.Validate(subscribedEditContext.Model))
        {
            messages.Add(new FieldIdentifier(error.Instance, error.MemberName), error.ErrorMessage);
        }
        subscribedEditContext.NotifyValidationStateChanged();
    }

    private void Unsubscribe()
    {
        if (subscribedEditContext is null) return;
        subscribedEditContext.OnValidationRequested -= OnValidationRequested;
        subscribedEditContext.OnFieldChanged -= OnFieldChanged;
        subscribedEditContext = null;
        messages = null;
    }

    public void Dispose() => Unsubscribe();
}
