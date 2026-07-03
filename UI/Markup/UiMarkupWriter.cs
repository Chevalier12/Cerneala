using System.Xml.Linq;

namespace Cerneala.UI.Markup;

public sealed class UiMarkupWriter
{
    public MarkupResult<string> Write(UiMarkupDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.Root is null)
        {
            MarkupDiagnostic diagnostic = MarkupDiagnostic.Error("MARKUP010", "Markup document must contain a root node.");
            return new MarkupResult<string>(null, [diagnostic]);
        }

        XElement element = WriteNode(document.Root);
        return new MarkupResult<string>(element.ToString(SaveOptions.DisableFormatting));
    }

    private static XElement WriteNode(UiMarkupNode node)
    {
        XElement element = new(node.Name);
        foreach (UiMarkupAttribute attribute in node.Attributes)
        {
            element.SetAttributeValue(attribute.Name, attribute.Value);
        }

        foreach (UiMarkupContent content in node.Content)
        {
            switch (content)
            {
                case UiMarkupTextContent text:
                    element.Add(new XText(text.Text));
                    break;
                case UiMarkupChildContent child:
                    element.Add(WriteNode(child.Node));
                    break;
            }
        }

        return element;
    }
}
