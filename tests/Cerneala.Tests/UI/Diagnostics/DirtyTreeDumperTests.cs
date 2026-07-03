using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.UI.Diagnostics;

public sealed class DirtyTreeDumperTests
{
    [Fact]
    public void DumpListsDirtyElementsWithFlagsVersionAndReason()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);

        child.Invalidate(InvalidationFlags.Render, "manual render");

        string dump = new DirtyTreeDumper().Dump(root, root.Trace);

        Assert.Contains("Dirty tree", dump, StringComparison.Ordinal);
        Assert.Contains($"UIElement#{child.ElementId}", dump, StringComparison.Ordinal);
        Assert.Contains("flags=Render", dump, StringComparison.Ordinal);
        Assert.Contains("version=1", dump, StringComparison.Ordinal);
        Assert.Contains("reason=manual render", dump, StringComparison.Ordinal);
        Assert.DoesNotContain($"UIRoot#{root.ElementId}", dump, StringComparison.Ordinal);
    }

    [Fact]
    public void DumpIncludesSourcePropertyWhenTraceRecordedPropertyInvalidation()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);

        child.Visibility = Visibility.Collapsed;

        string dump = new DirtyTreeDumper().Dump(root, root.Trace);

        Assert.Contains($"UIElement#{child.ElementId}", dump, StringComparison.Ordinal);
        Assert.Contains("reason=Property changed", dump, StringComparison.Ordinal);
        Assert.Contains("source=Cerneala.UI.Elements.UIElement.Visibility", dump, StringComparison.Ordinal);
    }

    [Fact]
    public void DumpReportsNoneForCleanTree()
    {
        UIRoot root = new();

        string dump = new DirtyTreeDumper().Dump(root);

        Assert.Equal("Dirty tree\r\n- none".Replace("\r\n", Environment.NewLine), dump);
    }
}
