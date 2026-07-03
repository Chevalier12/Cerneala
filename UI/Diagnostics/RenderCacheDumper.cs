using System.Text;
using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Diagnostics;

public sealed class RenderCacheDumper
{
    public string Dump(UIRoot root, ElementChildRole role = ElementChildRole.Visual)
    {
        ArgumentNullException.ThrowIfNull(root);

        StringBuilder builder = new();
        builder.AppendLine("Render cache");
        builder.Append("- ");
        builder.AppendLine(RenderDiagnostics.CaptureRoot(root.RetainedRenderCache).ToString());

        foreach (UIElement element in ElementTreeWalker.PreOrder(root, role))
        {
            builder.Append("- ");
            builder.AppendLine(RenderDiagnostics.CaptureElement(element, root.RetainedRenderCache).ToString());
        }

        return builder.ToString().TrimEnd();
    }
}
