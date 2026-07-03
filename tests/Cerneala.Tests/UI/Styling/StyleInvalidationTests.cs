using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Styling;

namespace Cerneala.Tests.UI.Styling;

public sealed class StyleInvalidationTests
{
    [Fact]
    public void ManualStyleInvalidationUsesRegistryInsteadOfPropertyNameStrings()
    {
        string source = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "UI",
            "Styling",
            "StyleInvalidation.cs"));

        Assert.DoesNotContain("property.Name ==", source, StringComparison.Ordinal);
        Assert.DoesNotContain("\"IsPressed\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("\"IsSelected\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void RecomputesVisualStateWhenHoverChanges()
    {
        Button button = new();
        StyleSheet sheet = new StyleSheet().Add(new StyleRule(
                StyleSelector.ForType<Button>(),
                new VisualStateRule(PseudoClass.Hover))
            .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.Black)));

        using StyleInvalidation invalidation = new(new StyleApplicator(), sheet);
        invalidation.Track(button);
        button.IsPointerOver = true;

        Assert.Equal(DrawColor.Black, button.Background);
    }

    [Fact]
    public void RenderOnlyStyleChangeDoesNotQueueMeasure()
    {
        UIRoot root = new(100, 100);
        Button button = new();
        root.VisualChildren.Add(button);
        root.ProcessFrame();
        StyleSheet sheet = new StyleSheet().Add(new StyleRule(StyleSelector.ForType<Button>())
            .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.White)));

        using StyleInvalidation invalidation = new(new StyleApplicator(), sheet);
        invalidation.Track(button);

        Assert.Empty(root.LayoutQueue.SnapshotMeasure());
        Assert.Contains(button, root.RenderQueue.Snapshot());
    }

    [Fact]
    public void MeasureAffectingStyleQueuesMeasure()
    {
        UIRoot root = new(100, 100);
        Button button = new();
        root.VisualChildren.Add(button);
        StyleSheet sheet = new StyleSheet().Add(new StyleRule(StyleSelector.ForType<Button>())
            .Add(new Setter<Thickness>(Control.PaddingProperty, new Thickness(4))));

        using StyleInvalidation invalidation = new(new StyleApplicator(), sheet);
        invalidation.Track(button);

        Assert.Contains(button, root.LayoutQueue.SnapshotMeasure());
    }

    [Fact]
    public void SamePseudoStateDoesNotReapplyDuplicateWork()
    {
        Button button = new() { IsPointerOver = true };
        int changes = 0;
        button.PropertyChanged += (_, _) => changes++;
        StyleSheet sheet = new StyleSheet().Add(new StyleRule(
                StyleSelector.ForType<Button>(),
                new VisualStateRule(PseudoClass.Hover))
            .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.Black)));

        using StyleInvalidation invalidation = new(new StyleApplicator(), sheet);
        invalidation.Track(button);
        invalidation.Recompute(button);

        Assert.Equal(1, changes);
    }
}
