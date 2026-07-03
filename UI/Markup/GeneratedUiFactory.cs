using Cerneala.UI.Elements;

namespace Cerneala.UI.Markup;

public sealed class GeneratedUiFactory
{
    private readonly Func<MarkupResult<UIElement>> create;

    public GeneratedUiFactory(Func<UIElement> create)
    {
        ArgumentNullException.ThrowIfNull(create);
        this.create = () => new MarkupResult<UIElement>(create());
    }

    public GeneratedUiFactory(Func<MarkupResult<UIElement>> create)
    {
        this.create = create ?? throw new ArgumentNullException(nameof(create));
    }

    public MarkupResult<UIElement> Create()
    {
        try
        {
            MarkupResult<UIElement>? result = create();
            if (result is null)
            {
                return CreateFailure("Generated UI factory returned no result.");
            }

            if (result.Value is null && !result.HasErrors)
            {
                List<MarkupDiagnostic> diagnostics = [.. result.Diagnostics];
                diagnostics.Add(MarkupDiagnostic.Error("MARKUP030", "Generated UI factory returned no root element."));
                return new MarkupResult<UIElement>(null, diagnostics);
            }

            return result;
        }
        catch (Exception ex)
        {
            return CreateFailure($"Generated UI factory failed: {ex.Message}");
        }
    }

    private static MarkupResult<UIElement> CreateFailure(string message)
    {
        return new MarkupResult<UIElement>(null, [MarkupDiagnostic.Error("MARKUP030", message)]);
    }
}
