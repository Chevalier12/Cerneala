using System.Runtime.CompilerServices;
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
    public void ClearingRootStyleSheetClearsPreviouslyAppliedStyleValues()
    {
        UIRoot root = new(100, 100);
        Button button = new();
        StyleSheet sheet = new StyleSheet().Add(new StyleRule(StyleSelector.ForType<Button>())
            .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.White)));

        root.SetStyleSheet(sheet);
        root.VisualChildren.Add(button);
        root.ProcessFrame();

        root.SetStyleSheet(null);
        root.ProcessFrame();

        Assert.Equal(UiPropertyValueSource.Default, button.GetValueSource(Control.BackgroundProperty));
    }

    [Fact]
    public void MovingElementToRootWithoutStyleSheetClearsPreviousRootStyleValues()
    {
        UIRoot root1 = new(100, 100);
        UIRoot root2 = new(100, 100);
        UIElement container = new();
        Button button = new();
        StyleSheet sheet = new StyleSheet().Add(new StyleRule(StyleSelector.ForType<Button>())
            .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.White)));
        container.VisualChildren.Add(button);

        root1.SetStyleSheet(sheet);
        root1.VisualChildren.Add(container);
        root1.ProcessFrame();

        Assert.Equal(UiPropertyValueSource.StyleBase, button.GetValueSource(Control.BackgroundProperty));
        Assert.Equal(DrawColor.White, button.Background);

        root1.VisualChildren.Remove(container);
        root2.VisualChildren.Add(container);
        root2.ProcessFrame();

        Assert.Equal(UiPropertyValueSource.Default, button.GetValueSource(Control.BackgroundProperty));
        Assert.Equal(DrawColor.Transparent, button.Background);
        Assert.False(root2.Scheduler.HasWork);
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
    public void StyleSetterThatChangesPseudoStateQueuesFollowUpStyleWork()
    {
        UIRoot root = new(100, 100);
        Button button = new();
        StyleSheet sheet = new StyleSheet()
            .Add(new StyleRule(StyleSelector.ForType<Button>())
                .Add(new Setter<bool>(UIElement.IsEnabledProperty, false)))
            .Add(new StyleRule(
                    StyleSelector.ForType<Button>(),
                    new VisualStateRule(PseudoClass.Disabled))
                .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.Black)));
        root.SetStyleSheet(sheet);
        root.VisualChildren.Add(button);

        root.ProcessFrame();

        Assert.False(button.IsEnabled);
        Assert.Contains(button, root.StyleQueue.Snapshot());
        Assert.True(button.DirtyState.Has(InvalidationFlags.Style));

        root.ProcessFrame();

        Assert.Equal(DrawColor.Black, button.Background);
        Assert.False(button.DirtyState.Has(InvalidationFlags.Style));
        Assert.False(root.Scheduler.HasWork);
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
    public void ThemeProviderDoesNotKeepDetachedRootAlive()
    {
        ThemeProvider provider = new(new Theme());
        WeakReference reference = CreateThemeProviderRootReference(provider);

        ForceFullCollection();

        Assert.False(reference.IsAlive);
        GC.KeepAlive(provider);
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
    public void StyleSetterForInheritedParentPropertyPropagatesToChildInSameFrame()
    {
        UIRoot root = new(100, 100);
        Control parent = new();
        TextBlock child = new() { Text = "child" };
        parent.VisualChildren.Add(child);
        root.VisualChildren.Add(parent);
        StyleSheet sheet = new StyleSheet().Add(new StyleRule(StyleSelector.Where("parent", element => ReferenceEquals(element, parent)))
            .Add(new Setter<DrawColor>(Control.ForegroundProperty, DrawColor.White)));
        root.SetStyleSheet(sheet);

        FrameStats stats = root.ProcessFrame();

        Assert.Equal(DrawColor.White, parent.Foreground);
        Assert.Equal(DrawColor.White, child.Foreground);
        Assert.Equal(UiPropertyValueSource.Inherited, child.GetValueSource(Control.ForegroundProperty));
        Assert.True(stats.InheritedElements > 0);
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateThemeProviderRootReference(ThemeProvider provider)
    {
        UIRoot root = new(100, 100);
        root.SetThemeProvider(provider);
        return new WeakReference(root);
    }

    private static void ForceFullCollection()
    {
        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
