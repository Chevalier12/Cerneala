using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Transactions;
using Cerneala.Tests.UI.Motion.Core;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.UI.Motion.Transactions;

public sealed class MotionTransactionTests
{
    [Fact]
    public void TransactionAnimatesAnimatablePropertyChanges()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        Control control = new();
        root.VisualChildren.Add(control);
        control.SetValue(Control.BackgroundProperty, DrawColor.Black, UiPropertyValueSource.AspectBase);
        root.ProcessFrame();

        using (root.Motion.BeginTransaction(MotionFactory.Tween(TimeSpan.FromMilliseconds(100))))
        {
            control.SetValue(Control.BackgroundProperty, DrawColor.White, UiPropertyValueSource.AspectBase);
        }

        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(50));
        root.ProcessFrame();

        Assert.Equal(UiPropertyValueSource.Animation, control.GetValueSource(Control.BackgroundProperty));
        Assert.NotEqual(DrawColor.Black, control.Background);
        Assert.NotEqual(DrawColor.White, control.Background);
    }

    [Fact]
    public void NonAnimatablePropertiesSetImmediately()
    {
        UIRoot root = new();
        UIElement element = new();
        root.VisualChildren.Add(element);

        using (root.Motion.BeginTransaction(MotionFactory.Tween(TimeSpan.FromMilliseconds(100))))
        {
            element.IsEnabled = false;
        }

        Assert.False(element.IsEnabled);
        Assert.Equal(0, root.Motion.Properties.BindingCount);
    }

    [Fact]
    public void NestedTransactionUsesInnerSpec()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        Control control = new();
        root.VisualChildren.Add(control);
        control.SetValue(Control.BackgroundProperty, DrawColor.Black, UiPropertyValueSource.AspectBase);

        using (root.Motion.BeginTransaction(MotionFactory.Tween(TimeSpan.FromMilliseconds(1000))))
        using (root.Motion.BeginTransaction(MotionFactory.Tween(TimeSpan.FromMilliseconds(10))))
        {
            control.SetValue(Control.BackgroundProperty, DrawColor.White, UiPropertyValueSource.AspectBase);
        }

        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(10));
        root.ProcessFrame();

        Assert.Equal(DrawColor.White, control.GetValue(Control.BackgroundProperty));
        Assert.Equal(UiPropertyValueSource.AspectBase, control.GetValueSource(Control.BackgroundProperty));
    }

    [Fact]
    public void TransactionScopePopsWhenMutationThrows()
    {
        UIRoot root = new();
        UIElement element = new();
        root.VisualChildren.Add(element);

        Assert.Throws<ArgumentException>(() =>
        {
            using (root.Motion.BeginTransaction(MotionFactory.Tween(TimeSpan.FromMilliseconds(100))))
            {
                element.Opacity = -1;
            }
        });

        element.Opacity = 0.5f;

        Assert.Equal(0, root.Motion.Properties.BindingCount);
    }

    [Fact]
    public void DisposingSameTransactionScopeTwiceIsHarmless()
    {
        UIRoot root = new();

        MotionTransactionScope scope = root.Motion.BeginTransaction(MotionFactory.Tween(TimeSpan.FromMilliseconds(100)));
        scope.Dispose();
        scope.Dispose();

        Assert.Equal(0, root.Motion.Transactions.Depth);
    }

    [Fact]
    public void DisabledTransactionSuppressesAnimation()
    {
        UIRoot root = new();
        Control control = new();
        root.VisualChildren.Add(control);
        control.SetValue(Control.BackgroundProperty, DrawColor.Black, UiPropertyValueSource.AspectBase);

        using (root.Motion.Disable())
        {
            control.SetValue(Control.BackgroundProperty, DrawColor.White, UiPropertyValueSource.AspectBase);
        }

        root.ProcessFrame();

        Assert.Equal(DrawColor.White, control.Background);
        Assert.Equal(UiPropertyValueSource.AspectBase, control.GetValueSource(Control.BackgroundProperty));
        Assert.Equal(0, root.Motion.Properties.BindingCount);
    }

    [Fact]
    public void AnimationSourceWritesInsideTransactionDoNotCreateNewAnimations()
    {
        UIRoot root = new();
        Control control = new();
        root.VisualChildren.Add(control);

        using (root.Motion.BeginTransaction(MotionFactory.Tween(TimeSpan.FromMilliseconds(100))))
        {
            control.SetValue(Control.BackgroundProperty, DrawColor.White, UiPropertyValueSource.Animation);
        }

        Assert.Equal(0, root.Motion.Properties.BindingCount);
    }
}
