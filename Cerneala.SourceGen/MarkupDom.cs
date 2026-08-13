using System.Net;
using System.Text;
using Cerneala.Language.Syntax;
using LanguageTextSpan = Cerneala.Language.Text.TextSpan;

namespace Cerneala.SourceGen;

internal sealed class MarkupName : IEquatable<MarkupName>
{
    public MarkupName(string value)
    {
        Value = value;
        int separator = value.IndexOf(':');
        LocalName = separator < 0 ? value : value.Substring(separator + 1);
    }

    public string Value { get; }

    public string LocalName { get; }

    public bool Equals(MarkupName? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is MarkupName other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;

    public static implicit operator MarkupName(string value) => new(value);

    public static bool operator ==(MarkupName? left, MarkupName? right) => Equals(left, right);

    public static bool operator !=(MarkupName? left, MarkupName? right) => !Equals(left, right);
}

internal abstract class MarkupObject
{
    protected MarkupObject(LanguageTextSpan span)
    {
        Span = span;
    }

    public LanguageTextSpan Span { get; }

    public MarkupElement? Parent { get; internal set; }
}

internal abstract class MarkupNode : MarkupObject
{
    protected MarkupNode(LanguageTextSpan span) : base(span)
    {
    }

    public MarkupNode? NextNode
    {
        get
        {
            if (Parent is null)
            {
                return null;
            }

            int index = Parent.NodeList.IndexOf(this);
            return index >= 0 && index + 1 < Parent.NodeList.Count ? Parent.NodeList[index + 1] : null;
        }
    }

    public void Remove()
    {
        Parent?.NodeList.Remove(this);
        Parent = null;
    }
}

internal sealed class MarkupText : MarkupNode
{
    public MarkupText(string value, LanguageTextSpan span) : base(span)
    {
        Value = value;
    }

    public string Value { get; set; }
}

internal sealed class MarkupComment : MarkupNode
{
    public MarkupComment(string value, LanguageTextSpan span) : base(span)
    {
        Value = value;
    }

    public string Value { get; }
}

internal sealed class MarkupAttribute : MarkupObject
{
    public MarkupAttribute(string name, string value) : this(name, value, new LanguageTextSpan(0, 0))
    {
    }

    public MarkupAttribute(string name, string value, LanguageTextSpan span) : base(span)
    {
        Name = new MarkupName(name);
        Value = value;
    }

    public MarkupName Name { get; }

    public string Value { get; set; }

    public bool IsNamespaceDeclaration => Name.Value == "xmlns" || Name.Value.StartsWith("xmlns:", StringComparison.Ordinal);
}

internal sealed class MarkupNamespace
{
    public MarkupNamespace(string namespaceName)
    {
        NamespaceName = namespaceName;
    }

    public string NamespaceName { get; }
}

internal enum MarkupSaveOptions
{
    DisableFormatting
}

internal sealed class MarkupElement : MarkupNode
{
    private readonly List<MarkupAttribute> attributes;

    private MarkupElement(string name, LanguageTextSpan span, IEnumerable<MarkupAttribute> attributes) : base(span)
    {
        Name = new MarkupName(name);
        this.attributes = attributes.ToList();
        foreach (MarkupAttribute attribute in this.attributes)
        {
            attribute.Parent = this;
        }
    }

    public MarkupElement(string name) : this(name, new LanguageTextSpan(0, 0), Array.Empty<MarkupAttribute>())
    {
    }

    public MarkupName Name { get; }

    public List<MarkupNode> NodeList { get; } = new();

    public bool HasElements => NodeList.OfType<MarkupElement>().Any();

    public bool HasAttributes => attributes.Count > 0;

    public string Value => string.Concat(DescendantNodesAndSelf().OfType<MarkupText>().Select(text => text.Value));

    public MarkupAttribute? Attribute(string name) =>
        attributes.FirstOrDefault(attribute => attribute.Name.Value == name || attribute.Name.LocalName == name);

    public IEnumerable<MarkupAttribute> Attributes() => attributes;

    public IEnumerable<MarkupNode> Nodes() => NodeList;

    public IEnumerable<MarkupElement> Elements() => NodeList.OfType<MarkupElement>();

    public IEnumerable<MarkupElement> Elements(string name) => Elements().Where(element => element.Name == name);

    public MarkupElement? Element(string name) => Elements(name).FirstOrDefault();

    public IEnumerable<MarkupElement> Descendants() => Elements().SelectMany(element => element.DescendantsAndSelf());

    public IEnumerable<MarkupElement> Descendants(string name) => Descendants().Where(element => element.Name == name);

    public IEnumerable<MarkupElement> DescendantsAndSelf()
    {
        yield return this;
        foreach (MarkupElement element in Elements())
        {
            foreach (MarkupElement descendant in element.DescendantsAndSelf())
            {
                yield return descendant;
            }
        }
    }

    public IEnumerable<MarkupNode> DescendantNodes()
    {
        foreach (MarkupNode node in NodeList)
        {
            yield return node;
            if (node is MarkupElement element)
            {
                foreach (MarkupNode descendant in element.DescendantNodes())
                {
                    yield return descendant;
                }
            }
        }
    }

    public IEnumerable<MarkupElement> Ancestors()
    {
        for (MarkupElement? current = Parent; current is not null; current = current.Parent)
        {
            yield return current;
        }
    }

    public IEnumerable<MarkupElement> AncestorsAndSelf()
    {
        for (MarkupElement? current = this; current is not null; current = current.Parent)
        {
            yield return current;
        }
    }

    public MarkupNamespace? GetNamespaceOfPrefix(string prefix)
    {
        string attributeName = prefix.Length == 0 ? "xmlns" : "xmlns:" + prefix;
        for (MarkupElement? current = this; current is not null; current = current.Parent)
        {
            MarkupAttribute? declaration = current.attributes.FirstOrDefault(attribute => attribute.Name.Value == attributeName);
            if (declaration is not null)
            {
                return new MarkupNamespace(declaration.Value);
            }
        }

        return null;
    }

    public string ToString(MarkupSaveOptions options)
    {
        StringBuilder builder = new();
        WriteTo(builder);
        return builder.ToString();
    }

    public override string ToString() => ToString(MarkupSaveOptions.DisableFormatting);

    public static MarkupElement FromSyntax(ElementSyntax syntax)
    {
        MarkupAttribute[] attributes = syntax.Attributes
            .Where(attribute => !attribute.NameToken.IsMissing)
            .Select(attribute => new MarkupAttribute(
                attribute.NameToken.Text,
                DecodeAttribute(attribute.ValueToken.Text),
                attribute.Span))
            .ToArray();
        MarkupElement result = new(syntax.Name, syntax.Span, attributes);
        foreach (SyntaxNode child in syntax.Children)
        {
            MarkupNode? node = child switch
            {
                ElementSyntax element => FromSyntax(element),
                TextSyntax text when text.Kind == SyntaxKind.Comment =>
                    new MarkupComment(text.Token.Text, text.Span),
                TextSyntax text => new MarkupText(DecodeText(text.Token.Text), text.Span),
                _ => null
            };
            if (node is not null)
            {
                node.Parent = result;
                result.NodeList.Add(node);
            }
        }

        return result;
    }

    private IEnumerable<MarkupNode> DescendantNodesAndSelf()
    {
        foreach (MarkupNode node in NodeList)
        {
            yield return node;
            if (node is MarkupElement element)
            {
                foreach (MarkupNode descendant in element.DescendantNodesAndSelf())
                {
                    yield return descendant;
                }
            }
        }
    }

    private void WriteTo(StringBuilder builder)
    {
        builder.Append('<').Append(Name.Value);
        foreach (MarkupAttribute attribute in attributes)
        {
            builder.Append(' ').Append(attribute.Name.Value).Append("=\"")
                .Append(WebUtility.HtmlEncode(attribute.Value)).Append('"');
        }

        if (NodeList.Count == 0)
        {
            builder.Append(" />");
            return;
        }

        builder.Append('>');
        foreach (MarkupNode node in NodeList)
        {
            switch (node)
            {
                case MarkupElement element:
                    element.WriteTo(builder);
                    break;
                case MarkupComment comment:
                    builder.Append("<!--").Append(comment.Value).Append("-->");
                    break;
                case MarkupText text:
                    builder.Append(WebUtility.HtmlEncode(text.Value));
                    break;
            }
        }

        builder.Append("</").Append(Name.Value).Append('>');
    }

    private static string DecodeAttribute(string text)
    {
        if (text.Length >= 2 && text[0] is '\'' or '"' && text[text.Length - 1] == text[0])
        {
            text = text.Substring(1, text.Length - 2);
        }

        return WebUtility.HtmlDecode(text);
    }

    private static string DecodeText(string text) => WebUtility.HtmlDecode(text);
}

internal sealed class EmissionMarkupDocument
{
    public EmissionMarkupDocument(MarkupElement root)
    {
        Root = root;
    }

    public MarkupElement Root { get; }
}
