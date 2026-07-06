using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.Tests.UI.Accessibility;

public sealed class RetainedSemanticsCacheTests
{
    [Fact]
    public void SemanticsProviderCachesUnchangedRootSemantics()
    {
        UIRoot root = new();
        root.VisualChildren.Add(new Button { Content = "Save" });

        SemanticsTree first = root.GetSemanticsTree();
        SemanticsTree second = root.GetSemanticsTree();

        Assert.Same(first, second);
    }

    [Fact]
    public void AccessibleNameChangeInvalidatesSemanticsWithoutLayoutOrRender()
    {
        UIRoot root = new();
        Button button = new() { Content = "Save" };
        root.VisualChildren.Add(button);
        root.ProcessFrame();
        SemanticsTree first = root.GetSemanticsTree();

        AccessibleName.SetName(button, "Store");
        FrameStats stats = root.ProcessFrame();
        SemanticsTree second = root.GetSemanticsTree();

        Assert.NotSame(first, second);
        Assert.Equal(0, stats.MeasuredElements);
        Assert.Equal(0, stats.RenderedElements);
    }

    [Fact]
    public void TreeMutationInvalidatesSemanticsCache()
    {
        UIRoot root = new();
        SemanticsTree first = root.GetSemanticsTree();

        root.VisualChildren.Add(new TextBlock { Text = "Hello" });
        SemanticsTree second = root.GetSemanticsTree();

        Assert.NotSame(first, second);
    }
}
