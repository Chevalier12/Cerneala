#nullable enable

using System;
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Resources;
using PanelOrientation = Cerneala.UI.Layout.Panels.Orientation;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Playground.Samples;

public sealed class AspectMotionSample : IPlaygroundSample
{
    public static readonly AspectToken<DrawColor> CardSurfaceToken = AspectToken.Color("playground.aspect-motion.card-surface");
    public static readonly AspectToken<DrawColor> CardHoverSurfaceToken = AspectToken.Color("playground.aspect-motion.card-hover-surface");
    public static readonly AspectToken<DrawColor> CardBorderToken = AspectToken.Color("playground.aspect-motion.card-border");

    private readonly PlaygroundText text;

    public AspectMotionSample(IResourceProvider? resourceProvider = null, ResourceId<FontResource>? fontResourceId = null)
    {
        text = new PlaygroundText(resourceProvider, fontResourceId);
    }

    public string Name => "Aspect Motion";

    public static AspectPackage CreatePackage()
    {
        return AspectPackage.Create("Playground.AspectMotion")
            .Tokens(tokens => tokens
                .Set(CardSurfaceToken, new DrawColor(255, 255, 255))
                .Set(CardHoverSurfaceToken, new DrawColor(238, 242, 255))
                .Set(CardBorderToken, new DrawColor(148, 163, 184)))
            .Components(components =>
            {
                components.AddRule(new AspectRuleSet(
                    "playground.aspect-motion.card",
                    AspectLayer.App,
                    new AspectTarget(typeof(HoverMotionCard)),
                    [
                        new AspectDeclaration(Control.BackgroundProperty, AspectRef.To(CardSurfaceToken)),
                        new AspectDeclaration(Control.BorderColorProperty, AspectRef.To(CardBorderToken)),
                        new AspectDeclaration(Control.BorderThicknessProperty, AspectValue<Thickness>.Literal(new Thickness(1))),
                        new AspectDeclaration(Control.PaddingProperty, AspectValue<Thickness>.Literal(new Thickness(18)))
                    ],
                    0));
                components.AddRule(new AspectRuleSet(
                    "playground.aspect-motion.card-hover",
                    AspectLayer.Runtime,
                    new AspectTarget(typeof(HoverMotionCard), conditions: [AspectCondition.State(AspectState.Hover)]),
                    [
                        new AspectDeclaration(
                            Control.BackgroundProperty,
                            AspectRef.To(CardHoverSurfaceToken),
                            new AspectMotion(Control.BackgroundProperty, "playground.aspect-motion.hover")),
                        new AspectDeclaration(
                            Control.BorderColorProperty,
                            AspectValue<DrawColor>.Literal(new DrawColor(99, 102, 241)),
                            new AspectMotion(Control.BorderColorProperty, "playground.aspect-motion.hover"))
                    ],
                    1));
            });
    }

    public UIElement Build()
    {
        TextBlock status = text.Create("State: idle", 13, new DrawColor(71, 85, 105), new Thickness(0, 8, 0, 0));
        HoverMotionCard card = new()
        {
            Margin = new Thickness(0, 0, 0, 12),
            HoverChanged = hovering => status.Text = hovering ? "State: hovered" : "State: idle",
            Child = new StackPanel
            {
                Orientation = PanelOrientation.Vertical,
                VisualChildren =
                {
                    text.Create("Hover motion card", 18, new DrawColor(28, 35, 48), new Thickness(0, 0, 0, 6)),
                    text.Create("Aspect drives the state chrome. Motion moves the same card.", 14, new DrawColor(71, 85, 105))
                }
            }
        };

        StackPanel panel = new()
        {
            Margin = new Thickness(32),
            Orientation = PanelOrientation.Vertical
        };
        panel.VisualChildren.Add(text.Create("Aspect Motion", 22, new DrawColor(20, 28, 42), new Thickness(0, 0, 0, 12)));
        panel.VisualChildren.Add(card);
        panel.VisualChildren.Add(status);
        return panel;
    }

    public sealed class HoverMotionCard : Border
    {
        private const float IdleScale = 1f;
        private const float HoverScale = 1.06f;
        private const float IdleTranslateX = 0f;
        private const float HoverTranslateX = 18f;
        private static readonly TimeSpan HoverDuration = TimeSpan.FromMilliseconds(180);

        private MotionHandle? scaleMotion;
        private MotionHandle? translateMotion;

        public Action<bool>? HoverChanged { get; init; }

        protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
        {
            base.OnPropertyChanged(args);

            if (ReferenceEquals(args.Property, UIElement.IsPointerOverProperty))
            {
                AnimateHover(IsPointerOver);
            }
        }

        private void AnimateHover(bool hovering)
        {
            HoverChanged?.Invoke(hovering);
            AnimateScale(hovering ? HoverScale : IdleScale);
            AnimateTranslateX(hovering ? HoverTranslateX : IdleTranslateX);
        }

        private void AnimateScale(float targetScale)
        {
            scaleMotion?.Cancel();
            if (Root is null)
            {
                Scale = targetScale;
                return;
            }

            float currentScale = Scale;
            ClearValue(UIElement.ScaleProperty);
            scaleMotion = this.Motion()
                .Animate(UIElement.ScaleProperty)
                .From(currentScale)
                .To(targetScale)
                .With(MotionFactory.Tween<float>(HoverDuration));
        }

        private void AnimateTranslateX(float targetTranslateX)
        {
            translateMotion?.Cancel();
            if (Root is null)
            {
                TranslateX = targetTranslateX;
                return;
            }

            float currentTranslateX = TranslateX;
            ClearValue(UIElement.TranslateXProperty);
            translateMotion = this.Motion()
                .Animate(UIElement.TranslateXProperty)
                .From(currentTranslateX)
                .To(targetTranslateX)
                .With(MotionFactory.Tween<float>(HoverDuration));
        }
    }
}
