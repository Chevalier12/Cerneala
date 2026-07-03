using System.Xml;
using System.Xml.Linq;

namespace Cerneala.UI.Markup;

public sealed class UiMarkupReader
{
    public MarkupResult<UiMarkupDocument> Read(string markup, MarkupLoadOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(markup))
        {
            MarkupDiagnostic diagnostic = MarkupDiagnostic.Error("MARKUP001", "Markup must contain a root element.");
            return new MarkupResult<UiMarkupDocument>(new UiMarkupDocument(null, [diagnostic]), [diagnostic]);
        }

        options ??= MarkupLoadOptions.Strict;
        List<MarkupDiagnostic> diagnostics = [];
        try
        {
            XDocument document = XDocument.Parse(markup, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
            if (document.Root is null)
            {
                diagnostics.Add(MarkupDiagnostic.Error("MARKUP001", "Markup must contain a root element."));
                return new MarkupResult<UiMarkupDocument>(new UiMarkupDocument(null, diagnostics), diagnostics);
            }

            UiMarkupNode root = ReadElement(document.Root);
            UiMarkupDocument result = new(root, diagnostics);
            return new MarkupResult<UiMarkupDocument>(result, diagnostics);
        }
        catch (XmlException ex)
        {
            diagnostics.Add(MarkupDiagnostic.Error("MARKUP002", ex.Message, ex.LineNumber, ex.LinePosition));
            UiMarkupDocument? document = options.ContinueOnError ? new UiMarkupDocument(null, diagnostics) : null;
            return new MarkupResult<UiMarkupDocument>(document, diagnostics);
        }
    }

    private static UiMarkupNode ReadElement(XElement element)
    {
        IXmlLineInfo lineInfo = element;
        List<UiMarkupAttribute> attributes = element.Attributes()
            .Where(attribute => !attribute.IsNamespaceDeclaration)
            .Select(ReadAttribute)
            .ToList();
        List<UiMarkupContent> content = ReadContent(element);

        return new UiMarkupNode(
            element.Name.LocalName,
            attributes,
            content,
            lineInfo.HasLineInfo() ? lineInfo.LineNumber : null,
            lineInfo.HasLineInfo() ? lineInfo.LinePosition : null);
    }

    private static UiMarkupAttribute ReadAttribute(XAttribute attribute)
    {
        IXmlLineInfo lineInfo = attribute;
        return new UiMarkupAttribute(
            attribute.Name.LocalName,
            attribute.Value,
            lineInfo.HasLineInfo() ? lineInfo.LineNumber : null,
            lineInfo.HasLineInfo() ? lineInfo.LinePosition : null);
    }

    private static List<UiMarkupContent> ReadContent(XElement element)
    {
        List<UiMarkupContent> content = [];
        foreach (XNode node in element.Nodes())
        {
            if (node is XText text && !string.IsNullOrWhiteSpace(text.Value))
            {
                content.Add(new UiMarkupTextContent(text.Value.Trim()));
                continue;
            }

            if (node is XElement child)
            {
                content.Add(new UiMarkupChildContent(ReadElement(child)));
            }
        }

        return content;
    }
}
