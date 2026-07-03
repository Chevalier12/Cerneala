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
        root.ProcessFrame();

        child.Invalidate(InvalidationFlags.Render, "manual render");

        string dump = new DirtyTreeDumper().Dump(root, root.Trace);

        Assert.Contains("Dirty tree", dump, StringComparison.Ordinal);
        Assert.Contains($"UIElement#{child.ElementId}", dump, StringComparison.Ordinal);
        Assert.Contains("flags=Render", dump, StringComparison.Ordinal);
        Assert.Contains("version=", dump, StringComparison.Ordinal);
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
    public void DumpKeepsRequeuedStyleReasonWhenStyleProcessorInvalidatesAgain()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        root.ProcessFrame();

        child.Invalidate(InvalidationFlags.Style, "initial style");

        root.ProcessFrame(new FramePhaseProcessors
        {
            Style = element =>
            {
                if (ReferenceEquals(element, child))
                {
                    element.Invalidate(InvalidationFlags.Style, "style requeued during style processor");
                }
            }
        });

        Assert.True(child.DirtyState.IsDirty);
        Assert.True(child.DirtyState.Has(InvalidationFlags.Style));
        Assert.Equal(1, root.StyleQueue.Count);

        string dump = new DirtyTreeDumper().Dump(root, root.Trace);
        string childLine = Assert.Single(
            dump.Split(Environment.NewLine),
            line => line.Contains($"UIElement#{child.ElementId}", StringComparison.Ordinal));

        Assert.Contains("reason=style requeued during style processor", childLine, StringComparison.Ordinal);
        Assert.DoesNotContain("reason=Clear", childLine, StringComparison.Ordinal);
    }

    [Fact]
    public void DumpReportsNoneForCleanTree()
    {
        UIRoot root = new();

        string dump = new DirtyTreeDumper().Dump(root);

        Assert.Equal("Dirty tree\r\n- none".Replace("\r\n", Environment.NewLine), dump);
    }
}
