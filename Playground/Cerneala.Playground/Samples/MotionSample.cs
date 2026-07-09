#nullable enable

using System;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Resources;
using PanelOrientation = Cerneala.UI.Layout.Orientation;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Playground.Samples;

public sealed class MotionSample : IPlaygroundSample
{
    private readonly PlaygroundText text;

    public MotionSample(IResourceProvider? resourceProvider = null, ResourceId<FontResource>? fontResourceId = null)
    {
        text = new PlaygroundText(resourceProvider, fontResourceId);
    }

    public string Name => "Motion";

    public UIElement Build()
    {
        MotionHandle? active = null;
        Border target = new()
        {
            Padding = new Thickness(20),
            Background = new DrawColor(79, 70, 229),
            BorderColor = new DrawColor(30, 41, 59),
            BorderThickness = new Thickness(1),
            Opacity = 0.88f,
            Scale = 0.98f,
            Child = text.Create("Motion target", 18, DrawColor.White)
        };

        StackPanel panel = new()
        {
            Margin = new Thickness(32, 24, 32, 0),
            Orientation = PanelOrientation.Vertical
        };
        panel.VisualChildren.Add(target);
        panel.VisualChildren.Add(Button("Hover color", () => target.Background = new DrawColor(14, 165, 233)));
        panel.VisualChildren.Add(Button("Press scale", () => active = AnimateScale(target, target.Scale < 1 ? 1.04f : 0.94f)));
        panel.VisualChildren.Add(Button("Animate", () => active = AnimateOpacity(target, 0.35f)));
        panel.VisualChildren.Add(Button("Cancel", () => active?.Cancel()));
        panel.VisualChildren.Add(Button("Restart", () => active = AnimateOpacity(target, 1f)));
        return panel;
    }

    private Button Button(string label, Action action)
    {
        return new Button
        {
            Content = text.Create(label, 14, DrawColor.Black),
            Padding = new Thickness(12, 8, 12, 8),
            Command = new ActionCommand(_ => action())
        };
    }

    private static MotionHandle? AnimateOpacity(Border target, float opacity)
    {
        if (target.Root is null)
        {
            target.Opacity = opacity;
            return null;
        }

        float currentOpacity = target.Opacity;
        target.ClearValue(UIElement.OpacityProperty);
        return target.Motion()
            .Animate(UIElement.OpacityProperty)
            .From(currentOpacity)
            .To(opacity)
            .With(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(180)));
    }

    private static MotionHandle? AnimateScale(Border target, float scale)
    {
        if (target.Root is null)
        {
            target.Scale = scale;
            return null;
        }

        float currentScale = target.Scale;
        target.ClearValue(UIElement.ScaleProperty);
        return target.Motion()
            .Animate(UIElement.ScaleProperty)
            .From(currentScale)
            .To(scale)
            .With(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(180)));
    }
}
