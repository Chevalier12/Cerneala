using Cerneala.UI.Elements;
using Cerneala.UI.Controls;
using System.Xml.Linq;

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

        ApplyResources(element, node, diagnostics);
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
            if (IsResourcePropertyElement(node, childNode))
            {
                continue;
            }

            if (TryApplyBrushPropertyElement(element, node, childNode, diagnostics))
            {
                continue;
            }

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

    private static void ApplyResources(UIElement element, UiMarkupNode ownerNode, List<MarkupDiagnostic> diagnostics)
    {
        UiMarkupNode[] resourceElements = ownerNode.Children.Where(child => IsResourcePropertyElement(ownerNode, child)).ToArray();
        if (resourceElements.Length == 0)
        {
            return;
        }

        if (resourceElements.Length > 1)
        {
            diagnostics.Add(MarkupDiagnostic.Error("MARKUP029", $"Element '{ownerNode.Name}' may declare only one Resources property element."));
            return;
        }

        foreach (UiMarkupNode resourceNode in resourceElements[0].Children)
        {
            string? name = resourceNode.Attributes.FirstOrDefault(attribute => attribute.Name == "Name")?.Value;
            if (string.IsNullOrWhiteSpace(name))
            {
                diagnostics.Add(MarkupDiagnostic.Error("MARKUP029", $"Brush resource '{resourceNode.Name}' requires a Name."));
                continue;
            }

            MarkupResult<Cerneala.UI.Media.Brush> result = new BrushMarkupReader().Read(ToXElement(resourceNode).ToString(SaveOptions.DisableFormatting));
            if (result.Value is null)
            {
                diagnostics.AddRange(result.Diagnostics);
                continue;
            }

            element.Resources[name] = result.Value;
        }
    }

    private static bool IsResourcePropertyElement(UiMarkupNode ownerNode, UiMarkupNode childNode)
    {
        return string.Equals(childNode.Name, ownerNode.Name + ".Resources", StringComparison.Ordinal);
    }

    private static bool TryApplyBrushPropertyElement(
        UIElement element,
        UiMarkupNode ownerNode,
        UiMarkupNode childNode,
        List<MarkupDiagnostic> diagnostics)
    {
        string prefix = ownerNode.Name + ".";
        if (!childNode.Name.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        string propertyName = childNode.Name[prefix.Length..];
        if (propertyName is not ("Background" or "BorderBrush"))
        {
            return false;
        }

        if (element is not Control control || childNode.Children.Count != 1)
        {
            diagnostics.Add(MarkupDiagnostic.Error(
                "MARKUP028",
                $"Property element '{childNode.Name}' requires exactly one brush child on a Control.",
                childNode.Line,
                childNode.Column));
            return true;
        }

        XElement brushElement = ToXElement(childNode.Children[0]);
        MarkupResult<Cerneala.UI.Media.Brush> result = new BrushMarkupReader().Read(brushElement.ToString(SaveOptions.DisableFormatting));
        if (result.Value is null)
        {
            diagnostics.AddRange(result.Diagnostics);
            return true;
        }

        if (propertyName == "Background")
        {
            control.Background = result.Value;
        }
        else
        {
            control.BorderBrush = result.Value;
        }

        return true;
    }

    private static XElement ToXElement(UiMarkupNode node)
    {
        XElement element = new(node.Name, node.Attributes.Select(attribute => new XAttribute(attribute.Name, attribute.Value)));
        foreach (UiMarkupContent content in node.Content)
        {
            switch (content)
            {
                case UiMarkupTextContent text:
                    element.Add(new XText(text.Text));
                    break;
                case UiMarkupChildContent child:
                    element.Add(ToXElement(child.Node));
                    break;
            }
        }

        return element;
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
