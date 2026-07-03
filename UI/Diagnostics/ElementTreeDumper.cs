using System.Globalization;
using System.Text;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Diagnostics;

public sealed class ElementTreeDumper
{
    public string Dump(UIElement root, ElementChildRole role = ElementChildRole.Visual)
    {
        ArgumentNullException.ThrowIfNull(root);

        StringBuilder builder = new();
        builder.Append(CultureInfo.InvariantCulture, $"Element tree ({role})");
        builder.AppendLine();
        AppendElement(builder, root, role, 0);
        return builder.ToString().TrimEnd();
    }

    private static void AppendElement(StringBuilder builder, UIElement element, ElementChildRole role, int depth)
    {
        builder.Append(' ', depth * 2);
        builder.Append(CultureInfo.InvariantCulture, $"- {element.GetType().Name}#{element.ElementId?.ToString() ?? "unattached"}");
        builder.Append(CultureInfo.InvariantCulture, $" visibility={element.Visibility}");
        builder.Append(CultureInfo.InvariantCulture, $" bounds={element.ArrangedBounds}");
        builder.Append(CultureInfo.InvariantCulture, $" dirty={element.DirtyState.Flags}");
        builder.AppendLine();

        IReadOnlyList<UIElement> children = role == ElementChildRole.Logical
            ? element.LogicalChildren
            : element.VisualChildren;

        foreach (UIElement child in children)
        {
            AppendElement(builder, child, role, depth + 1);
        }
    }
}
