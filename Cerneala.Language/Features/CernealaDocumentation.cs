using System.Xml.Linq;

namespace Cerneala.Language.Features;

internal static class CernealaDocumentation
{
    public static string? Extract(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return null;
        }

        try
        {
            XElement element = XElement.Parse(xml);
            return string.Join(" ", element.DescendantNodes().OfType<XText>()
                .Select(text => text.Value.Trim())
                .Where(text => text.Length > 0));
        }
        catch
        {
            return xml;
        }
    }
}
