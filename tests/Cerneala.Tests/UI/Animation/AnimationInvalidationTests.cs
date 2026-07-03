using Cerneala.Drawing;
using Cerneala.UI.Animation;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.UI.Animation;

public sealed class AnimationInvalidationTests
{
    [Fact]
    public void RenderOnlyAnimationAvoidsMeasure()
    {
        Border border = new();
        AnimationScheduler scheduler = new();
        scheduler.Schedule(
            border,
            Control.BackgroundProperty,
            new Animation<DrawColor>(DrawColor.Transparent, DrawColor.White, TimeSpan.FromSeconds(1), InterpolateColor));
        border.DirtyState.ClearAll();

        scheduler.Tick(TimeSpan.FromMilliseconds(500));

        Assert.True(border.DirtyState.Has(InvalidationFlags.Render));
        Assert.False(border.DirtyState.Has(InvalidationFlags.Measure));
    }

    [Fact]
    public void LayoutAnimationSchedulesMeasureOnChangedTick()
    {
        Border border = new();
        AnimationScheduler scheduler = new();
        scheduler.Schedule(
            border,
            Control.BorderThicknessProperty,
            new Animation<Thickness>(Thickness.Zero, new Thickness(10), TimeSpan.FromSeconds(1), InterpolateThickness));
        border.DirtyState.ClearAll();

        scheduler.Tick(TimeSpan.FromMilliseconds(500));

        Assert.True(border.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(border.DirtyState.Has(InvalidationFlags.Render));
    }

    [Fact]
    public void UnchangedAnimatedValueAvoidsDuplicateInvalidation()
    {
        Border border = new();
        AnimationScheduler scheduler = new();
        scheduler.Schedule(
            border,
            Control.BackgroundProperty,
            new Animation<DrawColor>(DrawColor.White, DrawColor.White, TimeSpan.FromSeconds(1), InterpolateColor));
        border.SetValue(Control.BackgroundProperty, DrawColor.White, UiPropertyValueSource.StyleBase);
        border.DirtyState.ClearAll();

        scheduler.Tick(TimeSpan.FromMilliseconds(500));

        Assert.False(border.DirtyState.Has(InvalidationFlags.Render));
    }

    [Fact]
    public void UnchangedLayoutAnimatedValueAvoidsMeasureInvalidation()
    {
        Border border = new();
        AnimationScheduler scheduler = new();
        scheduler.Schedule(
            border,
            Control.BorderThicknessProperty,
            new Animation<Thickness>(new Thickness(2), new Thickness(2), TimeSpan.FromSeconds(1), InterpolateThickness));
        border.SetValue(Control.BorderThicknessProperty, new Thickness(2), UiPropertyValueSource.StyleBase);
        border.DirtyState.ClearAll();

        scheduler.Tick(TimeSpan.FromMilliseconds(500));

        Assert.False(border.DirtyState.Has(InvalidationFlags.Measure));
        Assert.False(border.DirtyState.Has(InvalidationFlags.Render));
    }

    [Fact]
    public void CompletedAnimationClearsAnimationSourceAndRestoresBaseValue()
    {
        Border border = new();
        border.SetValue(Control.BorderThicknessProperty, new Thickness(2), UiPropertyValueSource.StyleBase);
        AnimationScheduler scheduler = new();
        scheduler.Schedule(
            border,
            Control.BorderThicknessProperty,
            new Animation<Thickness>(Thickness.Zero, new Thickness(10), TimeSpan.FromSeconds(1), InterpolateThickness));
        border.DirtyState.ClearAll();

        AnimationTickResult result = scheduler.Tick(TimeSpan.FromSeconds(1));

        Assert.Equal(new Thickness(2), border.BorderThickness);
        Assert.Equal(UiPropertyValueSource.StyleBase, border.GetValueSource(Control.BorderThicknessProperty));
        Assert.True(border.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(border.DirtyState.Has(InvalidationFlags.Render));
        Assert.Equal(1, result.Completed);
        Assert.False(result.HasPendingWork);
    }

    [Fact]
    public void AnimationSlotUpdatesWhileLocalValueTemporarilyMasksSameEffectiveValue()
    {
        Border border = new();
        AnimationScheduler scheduler = new();
        scheduler.Schedule(
            border,
            Control.BorderThicknessProperty,
            new Animation<Thickness>(Thickness.Zero, new Thickness(10), TimeSpan.FromSeconds(1), InterpolateThickness));

        scheduler.Tick(TimeSpan.FromMilliseconds(400));
        border.SetValue(Control.BorderThicknessProperty, new Thickness(5));

        scheduler.Tick(TimeSpan.FromMilliseconds(100));
        border.ClearValue(Control.BorderThicknessProperty);

        Assert.Equal(new Thickness(5), border.BorderThickness);
        Assert.Equal(UiPropertyValueSource.Animation, border.GetValueSource(Control.BorderThicknessProperty));
    }

    private static DrawColor InterpolateColor(DrawColor from, DrawColor to, float progress)
    {
        if (from == to)
        {
            return from;
        }

        return progress >= 1 ? to : progress <= 0 ? from : new DrawColor(128, 128, 128);
    }

    private static Thickness InterpolateThickness(Thickness from, Thickness to, float progress)
    {
        return new Thickness(from.Left + ((to.Left - from.Left) * progress));
    }
}
