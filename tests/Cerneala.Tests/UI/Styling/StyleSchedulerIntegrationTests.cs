using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Styling;

namespace Cerneala.Tests.UI.Styling;

public sealed class StyleSchedulerIntegrationTests
{
    [Fact]
    public void StyleSheetAppliedDuringStylePhaseBeforeRender()
    {
        UIRoot root = new(100, 100);
        Button button = new();
        StyleSheet sheet = new StyleSheet().Add(new StyleRule(StyleSelector.ForType<Button>())
            .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.White)));
        root.SetStyleSheet(sheet);
        root.VisualChildren.Add(button);

        FrameStats stats = root.ProcessFrame();

        Assert.Equal(DrawColor.White, button.Background);
        Assert.True(stats.StyledElements > 0);
        Assert.True(stats.RenderedElements > 0);
        Assert.False(button.DirtyState.Has(InvalidationFlags.Style));
        Assert.False(root.Scheduler.HasWork);
    }

    [Fact]
    public void PseudoClassChangeIsAppliedDuringNextStylePhase()
    {
        UIRoot root = new(100, 100);
        Button button = new();
        StyleSheet sheet = new StyleSheet().Add(new StyleRule(
                StyleSelector.ForType<Button>(),
                new VisualStateRule(PseudoClass.Hover))
            .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.Black)));
        root.SetStyleSheet(sheet);
        root.VisualChildren.Add(button);
        root.ProcessFrame();

        button.IsPointerOver = true;

        Assert.Contains(button, root.StyleQueue.Snapshot());

        FrameStats stats = root.ProcessFrame();

        Assert.Equal(DrawColor.Black, button.Background);
        Assert.True(stats.StyledElements > 0);
        Assert.True(stats.RenderedElements > 0);
        Assert.False(button.DirtyState.Has(InvalidationFlags.Style));
    }

    [Fact]
    public void ThemeChangeQueuesStyleForAttachedTree()
    {
        ThemeKey<DrawColor> key = new("Accent");
        ThemeProvider provider = new(new Theme().Set(key, DrawColor.White));
        UIRoot root = new(100, 100);
        Button button = new();
        StyleSheet sheet = new StyleSheet().Add(new StyleRule(StyleSelector.ForType<Button>())
            .Add(new Setter<DrawColor>(Control.BackgroundProperty, new ThemeResource<DrawColor>(key))));
        root.SetThemeProvider(provider);
        root.SetStyleSheet(sheet);
        root.VisualChildren.Add(button);
        root.ProcessFrame();

        provider.Theme = new Theme().Set(key, DrawColor.Black);

        Assert.Contains(button, root.StyleQueue.Snapshot());

        FrameStats stats = root.ProcessFrame();

        Assert.Equal(DrawColor.Black, button.Background);
        Assert.True(stats.StyledElements > 0);
        Assert.True(stats.RenderedElements > 0);
    }

    [Fact]
    public void StylePhaseQueuesMeasureWorkForMeasureAffectingSetterInSameFrame()
    {
        UIRoot root = new(100, 100);
        Button button = new();
        StyleSheet sheet = new StyleSheet().Add(new StyleRule(StyleSelector.ForType<Button>())
            .Add(new Setter<Thickness>(Control.PaddingProperty, new Thickness(4))));
        root.SetStyleSheet(sheet);
        root.VisualChildren.Add(button);

        FrameStats stats = root.ProcessFrame();

        Assert.Equal(new Thickness(4), button.Padding);
        Assert.True(stats.StyledElements > 0);
        Assert.True(stats.MeasuredElements > 0);
        Assert.True(stats.ArrangedElements > 0);
        Assert.False(root.Scheduler.HasWork);
    }

    [Fact]
    public void StylePropertyInvalidationDoesNotQueueRenderUntilStyleAppliesASetter()
    {
        UiProperty<int> property = UiProperty<int>.Register(
            $"{nameof(StyleSchedulerIntegrationTests)}_{Guid.NewGuid():N}",
            typeof(StyleSchedulerIntegrationTests),
            new UiPropertyMetadata<int>(0, UiPropertyOptions.AffectsStyle));
        UIRoot root = new(100, 100);
        UIElement element = new();
        root.VisualChildren.Add(element);
        root.ProcessFrame();

        element.SetValue(property, 1);

        Assert.Contains(element, root.StyleQueue.Snapshot());
        Assert.DoesNotContain(element, root.RenderQueue.Snapshot());
        Assert.True(element.DirtyState.Has(InvalidationFlags.Style));
    }
}
