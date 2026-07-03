using System.Collections.ObjectModel;

namespace Cerneala.UI.Markup;

public sealed class UiMarkupDocument
{
    public UiMarkupDocument(UiMarkupNode? root, IEnumerable<MarkupDiagnostic>? diagnostics = null)
    {
        Root = root;
        Diagnostics = new ReadOnlyCollection<MarkupDiagnostic>((diagnostics ?? []).ToList());
    }

    public UiMarkupNode? Root { get; }

    public IReadOnlyList<MarkupDiagnostic> Diagnostics { get; }

    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity == MarkupDiagnosticSeverity.Error);

    public static UiMarkupDocument FromRoot(UiMarkupNode root)
    {
        ArgumentNullException.ThrowIfNull(root);
        return new UiMarkupDocument(root);
    }
}

public sealed class UiMarkupNode
{
    public UiMarkupNode(
        string name,
        IEnumerable<UiMarkupAttribute>? attributes = null,
        IEnumerable<UiMarkupNode>? children = null,
        string? text = null,
        int? line = null,
        int? column = null)
        : this(name, attributes, CreateContent(children, text), line, column)
    {
    }

    public UiMarkupNode(
        string name,
        IEnumerable<UiMarkupAttribute>? attributes,
        IEnumerable<UiMarkupContent> content,
        int? line = null,
        int? column = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Markup node name cannot be empty.", nameof(name));
        }

        List<UiMarkupContent> orderedContent = content.ToList();
        Name = name;
        Attributes = new ReadOnlyCollection<UiMarkupAttribute>((attributes ?? []).ToList());
        Content = new ReadOnlyCollection<UiMarkupContent>(orderedContent);
        Children = new ReadOnlyCollection<UiMarkupNode>(orderedContent.OfType<UiMarkupChildContent>().Select(child => child.Node).ToList());
        string combinedText = string.Concat(orderedContent.OfType<UiMarkupTextContent>().Select(textContent => textContent.Text));
        Text = combinedText.Length == 0 ? null : combinedText;
        Line = line;
        Column = column;
    }

    public string Name { get; }

    public IReadOnlyList<UiMarkupAttribute> Attributes { get; }

    public IReadOnlyList<UiMarkupContent> Content { get; }

    public IReadOnlyList<UiMarkupNode> Children { get; }

    public string? Text { get; }

    public int? Line { get; }

    public int? Column { get; }

    private static IEnumerable<UiMarkupContent> CreateContent(IEnumerable<UiMarkupNode>? children, string? text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            yield return new UiMarkupTextContent(text);
        }

        foreach (UiMarkupNode child in children ?? [])
        {
            yield return new UiMarkupChildContent(child);
        }
    }
}

public abstract class UiMarkupContent;

public sealed class UiMarkupTextContent : UiMarkupContent
{
    public UiMarkupTextContent(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            throw new ArgumentException("Markup text content cannot be empty.", nameof(text));
        }

        Text = text;
    }

    public string Text { get; }
}

public sealed class UiMarkupChildContent : UiMarkupContent
{
    public UiMarkupChildContent(UiMarkupNode node)
    {
        Node = node ?? throw new ArgumentNullException(nameof(node));
    }

    public UiMarkupNode Node { get; }
}

public sealed record UiMarkupAttribute
{
    public UiMarkupAttribute(string name, string value, int? line = null, int? column = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Markup attribute name cannot be empty.", nameof(name));
        }

        Name = name;
        Value = value;
        Line = line;
        Column = column;
    }

    public string Name { get; }

    public string Value { get; }

    public int? Line { get; }

    public int? Column { get; }
}

public sealed class MarkupResult<T>
{
    public MarkupResult(T? value, IEnumerable<MarkupDiagnostic>? diagnostics = null)
    {
        Value = value;
        Diagnostics = new ReadOnlyCollection<MarkupDiagnostic>((diagnostics ?? []).ToList());
    }

    public T? Value { get; }

    public IReadOnlyList<MarkupDiagnostic> Diagnostics { get; }

    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity == MarkupDiagnosticSeverity.Error);
}
