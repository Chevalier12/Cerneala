using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Styling;
using Cerneala.UI.Motion.Specs;
using Cerneala.UI.Styling;
using Cerneala.Tests.UI.Motion.Core;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.UI.Motion.Styling;

public sealed class MotionVisualStateTests
{
    [Fact]
    public void HoverStateAnimatesBackgroundFromOldStyleValueToNewStyleValue()
    {
        ManualMotionClock clock = new();
        UIRoot root = CreateRoot(clock);
        Button button = AddButton(root);
        root.ProcessFrame();

        button.IsPointerOver = true;
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(60));
        root.ProcessFrame();

        Assert.Equal(UiPropertyValueSource.Animation, button.GetValueSource(Control.BackgroundProperty));
        Assert.NotEqual(DrawColor.Black, button.Background);
        Assert.NotEqual(DrawColor.White, button.Background);
    }

    [Fact]
    public void PressedStateRetargetsActiveHoverAnimationWithoutJumping()
    {
        ManualMotionClock clock = new();
        UIRoot root = CreateRoot(clock);
        Button button = AddButton(root);
        root.ProcessFrame();

        button.IsPointerOver = true;
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(40));
        root.ProcessFrame();
        DrawColor hoverSample = button.Background;
        button.IsPressed = true;
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(20));
        root.ProcessFrame();

        Assert.Equal(1, root.Motion.Properties.BindingCount);
        Assert.NotEqual(DrawColor.Black, hoverSample);
        Assert.NotEqual(DrawColor.White, button.Background);
        Assert.NotEqual(new DrawColor(40, 40, 40), hoverSample);
    }

    [Fact]
    public void DisabledStateCanCancelLowerPriorityInteractiveStateMotion()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        root.SetThemeProvider(new ThemeProvider(DefaultTheme.Create()));
        root.SetStyleSheet(new StyleSheet()
            .Add(new StyleRule(StyleSelector.ForType<UIElement>())
                .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.Black)))
            .Add(new StyleRule(StyleSelector.ForType<UIElement>(), new VisualStateRule(PseudoClass.Hover))
                .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.White))
                .AddMotion(new StyleMotion<DrawColor>(Control.BackgroundProperty, ThemeMotionTokens.FastOut, StyleMotionScope.VisualStateChanges)))
            .Add(new StyleRule(StyleSelector.ForType<UIElement>(), new VisualStateRule(PseudoClass.Disabled))
                .Add(new Setter<DrawColor>(Control.BackgroundProperty, new DrawColor(90, 90, 90)))
                .AddMotion(new StyleMotion<DrawColor>(Control.BackgroundProperty, ThemeMotionTokens.FastOut, StyleMotionScope.VisualStateChanges))));
        UIElement button = new();
        root.VisualChildren.Add(button);
        root.ProcessFrame();

        button.IsPointerOver = true;
        root.ProcessFrame();
        button.IsEnabled = false;
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(60));
        root.ProcessFrame();

        Assert.Equal(1, root.Motion.Properties.BindingCount);
        Assert.NotEqual(DrawColor.White, button.GetValue(Control.BackgroundProperty));
    }

    [Fact]
    public void MultipleStateChangesInOneStylePassProduceOnePropertyAnimation()
    {
        UIRoot root = CreateRoot(new ManualMotionClock());
        Button button = AddButton(root);
        root.ProcessFrame();

        button.IsPointerOver = true;
        button.IsPressed = true;
        root.ProcessFrame();

        Assert.Equal(1, root.Motion.Properties.BindingCount);
    }

    [Fact]
    public void ExplicitAnimationOutranksHoverStateAnimation()
    {
        ManualMotionClock clock = new();
        UIRoot root = CreateRoot(clock);
        Button button = AddButton(root);
        root.ProcessFrame();

        button.Motion()
            .Animate(Control.BackgroundProperty)
            .From(DrawColor.Black)
            .To(new DrawColor(255, 0, 0))
            .With(MotionFactory.Tween<DrawColor>(TimeSpan.FromMilliseconds(100), Easings.Linear));
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(40));
        root.ProcessFrame();

        button.IsPointerOver = true;
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(20));
        root.ProcessFrame();

        Assert.Equal(UiPropertyValueSource.Animation, button.GetValueSource(Control.BackgroundProperty));
        Assert.Equal(0, button.Background.G);
        Assert.Equal(0, button.Background.B);
        Assert.True(button.Background.R > 0);
    }

    private static UIRoot CreateRoot(ManualMotionClock clock)
    {
        UIRoot root = new(motionClock: clock);
        root.SetThemeProvider(new ThemeProvider(DefaultTheme.Create()));
        root.SetStyleSheet(new StyleSheet()
            .Add(new StyleRule(StyleSelector.ForType<Button>())
                .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.Black)))
            .Add(new StyleRule(StyleSelector.ForType<Button>(), new VisualStateRule(PseudoClass.Hover))
                .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.White))
                .AddMotion(new StyleMotion<DrawColor>(Control.BackgroundProperty, ThemeMotionTokens.FastOut, StyleMotionScope.VisualStateChanges)))
            .Add(new StyleRule(StyleSelector.ForType<Button>(), new VisualStateRule(PseudoClass.Pressed))
                .Add(new Setter<DrawColor>(Control.BackgroundProperty, new DrawColor(40, 40, 40)))
                .AddMotion(new StyleMotion<DrawColor>(Control.BackgroundProperty, ThemeMotionTokens.FastOut, StyleMotionScope.VisualStateChanges)))
            .Add(new StyleRule(StyleSelector.ForType<Button>(), new VisualStateRule(PseudoClass.Disabled))
                .Add(new Setter<DrawColor>(Control.BackgroundProperty, new DrawColor(90, 90, 90)))
                .AddMotion(new StyleMotion<DrawColor>(Control.BackgroundProperty, ThemeMotionTokens.FastOut, StyleMotionScope.VisualStateChanges))));
        return root;
    }

    private static Button AddButton(UIRoot root)
    {
        Button button = new();
        root.VisualChildren.Add(button);
        return button;
    }
}
