using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.Tests.UI.Invalidation;

public sealed class DirtyPropagationTests
{
    [Fact]
    public void MeasureInvalidationPropagatesLayoutNeedUpward()
    {
        UIRoot root = new();
        UIElement parent = new();
        UIElement child = new();
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);
        ClearInitialMutationWork(root);

        child.Invalidate(InvalidationFlags.Measure, "measure");

        Assert.True(child.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(child.DirtyState.Has(InvalidationFlags.Arrange));
        Assert.True(child.DirtyState.Has(InvalidationFlags.Render));
        Assert.True(parent.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(root.DirtyState.Has(InvalidationFlags.Measure));
        Assert.Equal(3, root.LayoutQueue.MeasureCount);
    }

    [Fact]
    public void ArrangeInvalidationImpliesRenderWork()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        ClearInitialMutationWork(root);

        child.Invalidate(InvalidationFlags.Arrange, "arrange");

        Assert.True(child.DirtyState.Has(InvalidationFlags.Arrange));
        Assert.True(child.DirtyState.Has(InvalidationFlags.Render));
        Assert.Equal(1, root.LayoutQueue.ArrangeCount);
        Assert.Equal(1, root.RenderQueue.Count);
    }

    [Fact]
    public void RenderInvalidationDoesNotImplyMeasure()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        ClearInitialMutationWork(root);

        child.Invalidate(InvalidationFlags.Render, "render");

        Assert.False(child.DirtyState.Has(InvalidationFlags.Measure));
        Assert.Equal(0, root.LayoutQueue.MeasureCount);
        Assert.Equal(1, root.RenderQueue.Count);
    }

    [Fact]
    public void TextInvalidationIsConservative()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        ClearInitialMutationWork(root);

        child.Invalidate(InvalidationFlags.Text, "text");

        Assert.True(child.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(child.DirtyState.Has(InvalidationFlags.Arrange));
        Assert.True(child.DirtyState.Has(InvalidationFlags.Render));
    }

    [Fact]
    public void ImageInvalidationCanBeRenderOnly()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        ClearInitialMutationWork(root);
        InvalidationRequest request = new(child, InvalidationFlags.Image, "image", affectsIntrinsicSize: false);

        child.Invalidate(request);

        Assert.False(child.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(child.DirtyState.Has(InvalidationFlags.Render));
    }

    [Fact]
    public void ResourceInvalidationFollowsSuppliedEffects()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        ClearInitialMutationWork(root);
        InvalidationRequest request = new(
            child,
            InvalidationFlags.Resource,
            "resource",
            resourceEffects: InvalidationFlags.HitTest);

        child.Invalidate(request);

        Assert.True(child.DirtyState.Has(InvalidationFlags.HitTest));
        Assert.False(child.DirtyState.Has(InvalidationFlags.Render));
    }

    [Fact]
    public void InputVisualInvalidationDefaultsToRenderOnly()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        ClearInitialMutationWork(root);

        child.Invalidate(InvalidationFlags.InputVisual, "input visual");

        Assert.True(child.DirtyState.Has(InvalidationFlags.Render));
        Assert.False(child.DirtyState.Has(InvalidationFlags.Measure));
    }

    [Fact]
    public void SubtreeInvalidationDoesNotLeavePropagationModifierDirty()
    {
        UIRoot root = new();
        UIElement parent = new();
        UIElement child = new();
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);
        ClearInitialMutationWork(root);

        parent.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "subtree render");

        Assert.True(parent.DirtyState.Has(InvalidationFlags.Render));
        Assert.True(child.DirtyState.Has(InvalidationFlags.Render));

        root.ProcessFrame();

        Assert.False(parent.DirtyState.Has(InvalidationFlags.Subtree));
        Assert.False(parent.DirtyState.IsDirty);
        Assert.False(child.DirtyState.IsDirty);
    }

    [Fact]
    public void ResourceInvalidationWithMeasureOnlyEffectDoesNotLeaveResourceDirty()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        ClearInitialMutationWork(root);
        InvalidationRequest request = new(
            child,
            InvalidationFlags.Resource,
            "resource measure",
            resourceEffects: InvalidationFlags.Measure);

        child.Invalidate(request);

        Assert.True(child.DirtyState.Has(InvalidationFlags.Measure));
        Assert.False(child.DirtyState.Has(InvalidationFlags.Resource));

        root.ProcessFrame();

        Assert.False(child.DirtyState.IsDirty);
    }

    private static void ClearInitialMutationWork(UIRoot root)
    {
        root.ProcessFrame();
    }
}
