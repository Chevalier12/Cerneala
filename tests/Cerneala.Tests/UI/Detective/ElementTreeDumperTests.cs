using Cerneala.UI.Controls;
using Cerneala.UI.Detective;
using Cerneala.UI.Elements;

namespace Cerneala.Tests.UI.Detective;

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
        Assert.Contains("- UIRoot visibility=", first, StringComparison.Ordinal);
        Assert.Contains("  - Border visibility=", first, StringComparison.Ordinal);
        Assert.Contains("    - TextBlock visibility=", first, StringComparison.Ordinal);
        Assert.Contains("visibility=Visible", first, StringComparison.Ordinal);
        Assert.Contains("dirty=", first, StringComparison.Ordinal);
    }
}
