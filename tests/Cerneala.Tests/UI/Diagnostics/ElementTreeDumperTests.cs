using Cerneala.UI.Controls;
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;

namespace Cerneala.Tests.UI.Diagnostics;

public sealed class ElementTreeDumperTests
{
    [Fact]
    public void DumpUsesStablePreOrderAndIndentation()
    {
        UIRoot root = new();
        Border parent = new();
        TextBlock child = new() { Text = "Hello" };
        parent.Child = child;
        root.VisualChildren.Add(parent);

        string first = new ElementTreeDumper().Dump(root);
        string second = new ElementTreeDumper().Dump(root);

        Assert.Equal(first, second);
        Assert.Contains($"- UIRoot#{root.ElementId}", first, StringComparison.Ordinal);
        Assert.Contains($"  - Border#{parent.ElementId}", first, StringComparison.Ordinal);
        Assert.Contains($"    - TextBlock#{child.ElementId}", first, StringComparison.Ordinal);
        Assert.Contains("visibility=Visible", first, StringComparison.Ordinal);
        Assert.Contains("dirty=", first, StringComparison.Ordinal);
    }
}
