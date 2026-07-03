using Cerneala.UI.Elements;

namespace Cerneala.UI.Markup;

public sealed class UiFactory
{
    private readonly UiMarkupTypeRegistry registry;

    public UiFactory(UiMarkupTypeRegistry registry)
    {
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public MarkupResult<UIElement> Create(UiMarkupDocument document, MarkupLoadOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        options ??= MarkupLoadOptions.Strict;

        List<MarkupDiagnostic> diagnostics = [.. document.Diagnostics];
        if (document.Root is null)
        {
            diagnostics.Add(MarkupDiagnostic.Error("MARKUP020", "Cannot create UI without a document root."));
            return new MarkupResult<UIElement>(null, diagnostics);
        }

        UIElement? root = CreateNode(document.Root, diagnostics, options);
        if (diagnostics.Any(diagnostic => diagnostic.Severity == MarkupDiagnosticSeverity.Error) && !options.ContinueOnError)
        {
            return new MarkupResult<UIElement>(null, diagnostics);
        }

        return new MarkupResult<UIElement>(root, diagnostics);
    }

    private UIElement? CreateNode(UiMarkupNode node, List<MarkupDiagnostic> diagnostics, MarkupLoadOptions options)
    {
        if (!registry.TryGetElement(node.Name, out UiMarkupElementRegistration? registration))
        {
            diagnostics.Add(MarkupDiagnostic.Error("MARKUP021", $"Unknown markup element '{node.Name}'.", node.Line, node.Column));
            return null;
        }

        UIElement element;
        try
        {
            element = registration.Factory();
        }
        catch (Exception ex)
        {
            diagnostics.Add(MarkupDiagnostic.Error("MARKUP022", $"Could not create markup element '{node.Name}': {ex.Message}", node.Line, node.Column));
            return null;
        }

        ApplyAttributes(registration, element, node, diagnostics, options);
        ApplyTextContent(registration, element, node, diagnostics, options);
        ApplyChildren(registration, element, node, diagnostics, options);
        return element;
    }

    private static void ApplyAttributes(
        UiMarkupElementRegistration registration,
        UIElement element,
        UiMarkupNode node,
        List<MarkupDiagnostic> diagnostics,
        MarkupLoadOptions options)
    {
        foreach (UiMarkupAttribute attribute in node.Attributes)
        {
            if (!registration.TryGetProperty(attribute.Name, out UiMarkupPropertyRegistration? property))
            {
                diagnostics.Add(MarkupDiagnostic.Error("MARKUP023", $"Unknown markup property '{node.Name}.{attribute.Name}'.", attribute.Line, attribute.Column));
                if (!options.ContinueOnError)
                {
                    continue;
                }

                continue;
            }

            TrySetProperty(property, element, attribute.Value, diagnostics, attribute.Line, attribute.Column);
        }
    }

    private static void ApplyTextContent(
        UiMarkupElementRegistration registration,
        UIElement element,
        UiMarkupNode node,
        List<MarkupDiagnostic> diagnostics,
        MarkupLoadOptions options)
    {
        if (string.IsNullOrEmpty(node.Text))
        {
            return;
        }

        if (registration.ContentPropertyName is null ||
            !registration.TryGetProperty(registration.ContentPropertyName, out UiMarkupPropertyRegistration? property))
        {
            diagnostics.Add(MarkupDiagnostic.Error("MARKUP024", $"Element '{node.Name}' does not accept text content.", node.Line, node.Column));
            return;
        }

        TrySetProperty(property, element, node.Text, diagnostics, node.Line, node.Column);
    }

    private void ApplyChildren(
        UiMarkupElementRegistration registration,
        UIElement element,
        UiMarkupNode node,
        List<MarkupDiagnostic> diagnostics,
        MarkupLoadOptions options)
    {
        foreach (UiMarkupNode childNode in node.Children)
        {
            UIElement? child = CreateNode(childNode, diagnostics, options);
            if (child is null)
            {
                continue;
            }

            if (registration.AddChild is null)
            {
                diagnostics.Add(MarkupDiagnostic.Error("MARKUP025", $"Element '{node.Name}' does not accept child elements.", childNode.Line, childNode.Column));
                continue;
            }

            try
            {
                registration.AddChild(element, child);
            }
            catch (Exception ex)
            {
                diagnostics.Add(MarkupDiagnostic.Error("MARKUP026", $"Could not add child '{childNode.Name}' to '{node.Name}': {ex.Message}", childNode.Line, childNode.Column));
            }
        }
    }

    private static void TrySetProperty(
        UiMarkupPropertyRegistration property,
        UIElement element,
        string value,
        List<MarkupDiagnostic> diagnostics,
        int? line,
        int? column)
    {
        try
        {
            property.SetValue(element, value);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or InvalidOperationException)
        {
            diagnostics.Add(MarkupDiagnostic.Error("MARKUP027", $"Could not set markup property '{property.Name}': {ex.Message}", line, column));
        }
    }
}
