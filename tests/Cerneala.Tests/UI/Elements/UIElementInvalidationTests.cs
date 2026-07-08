using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.Tests.UI.Elements;

public sealed class UIElementInvalidationTests
{
    [Fact]
    public void TypedPropertyOptionsTranslateIntoRetainedInvalidation()
    {
        UiProperty<int> property = UiProperty<int>.Register(
            UniqueName(),
            typeof(UIElementInvalidationTests),
            new UiPropertyMetadata<int>(
                0,
                UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsHitTest));
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        root.ProcessFrame();

        child.SetValue(property, 1);

        Assert.True(child.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(child.DirtyState.Has(InvalidationFlags.Render));
        Assert.True(child.DirtyState.Has(InvalidationFlags.HitTest));
        Assert.Equal(1, root.RenderQueue.Count);
        Assert.Equal(1, root.HitTestQueue.Count);
    }

    [Fact]
    public void DetachedElementRecordsDirtyStateWithoutRootQueue()
    {
        UIElement element = new();

        element.Invalidate(InvalidationFlags.Render, "detached");

        Assert.True(element.DirtyState.Has(InvalidationFlags.Render));
        Assert.Null(element.Root);
    }

    [Fact]
    public void AspectPropertyInvalidationQueuesAspectWorkAndClearsAfterFrame()
    {
        UiProperty<int> property = UiProperty<int>.Register(
            UniqueName(),
            typeof(UIElementInvalidationTests),
            new UiPropertyMetadata<int>(0, UiPropertyOptions.AffectsAspect));
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        root.ProcessFrame();

        child.SetValue(property, 1);

        Assert.Contains(child, root.AspectQueue.Snapshot());
        Assert.DoesNotContain(child, root.RenderQueue.Snapshot());

        root.ProcessFrame();

        Assert.False(child.DirtyState.IsDirty);
    }

    private static string UniqueName()
    {
        return $"{nameof(UIElementInvalidationTests)}_{Guid.NewGuid():N}";
    }
}
