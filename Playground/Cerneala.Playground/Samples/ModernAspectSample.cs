#nullable enable

using System;
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;
using PanelOrientation = Cerneala.UI.Layout.Panels.Orientation;

namespace Cerneala.Playground.Samples;

public sealed class ModernAspectSample : IPlaygroundSample
{
    public static readonly AspectToken<DrawColor> LiveAccentToken = AspectToken.Color("playground.live-accent");

    private readonly IResourceProvider? resourceProvider;
    private readonly ResourceId<FontResource>? fontResourceId;
    private readonly PlaygroundText text;
    private readonly ContentTemplateRegistry contentTemplates;

    public ModernAspectSample(IResourceProvider? resourceProvider = null, ResourceId<FontResource>? fontResourceId = null)
    {
        this.resourceProvider = resourceProvider;
        this.fontResourceId = fontResourceId;
        text = new PlaygroundText(resourceProvider, fontResourceId);
        contentTemplates = CreateContentTemplates(text);
    }

    public string Name => "Modern Aspect";

    public static AspectPackage CreatePackage()
    {
        return AspectPackage.Create("Playground.ModernAspect")
            .Tokens(tokens => tokens
                .Set(LiveAccentToken, new DrawColor(79, 70, 229))
                .Set(DefaultAspectTokens.Color.Accent, new DrawColor(79, 70, 229)))
            .Components(components =>
            {
                components.AddTemplate(new ComponentTemplateDefinition("Playground.Button.Modern", typeof(Button), ButtonTemplates.Modern));
                components.AddRule(new AspectRuleSet(
                    "playground.button.primary",
                    AspectLayer.App,
                    new AspectTarget(typeof(Button), conditions: [AspectCondition.Variant(ButtonVariants.Kind, ButtonKind.Primary)]),
                    [
                        new AspectDeclaration(Control.BackgroundProperty, AspectRef.To(LiveAccentToken)),
                        new AspectDeclaration(Control.ForegroundProperty, AspectValue<DrawColor>.Literal(DrawColor.White))
                    ],
                    0));
                components.AddRule(new AspectRuleSet(
                    "playground.button.danger",
                    AspectLayer.App,
                    new AspectTarget(typeof(Button), conditions: [AspectCondition.Variant(ButtonVariants.Kind, ButtonKind.Danger)]),
                    [
                        new AspectDeclaration(Control.BackgroundProperty, AspectValue<DrawColor>.Literal(new DrawColor(220, 38, 38))),
                        new AspectDeclaration(Control.ForegroundProperty, AspectValue<DrawColor>.Literal(DrawColor.White))
                    ],
                    1));
                components.AddRule(new AspectRuleSet(
                    "playground.button.small",
                    AspectLayer.App,
                    new AspectTarget(typeof(Button), conditions: [AspectCondition.Variant(ButtonVariants.Size, ButtonSize.Small)]),
                    [
                        new AspectDeclaration(Control.PaddingProperty, AspectValue<Thickness>.Literal(new Thickness(8, 4, 8, 4))),
                        new AspectDeclaration(Control.FontSizeProperty, AspectValue<float>.Literal(13f))
                    ],
                    2));
                components.AddRule(new AspectRuleSet(
                    "playground.button.hover",
                    AspectLayer.Runtime,
                    new AspectTarget(typeof(Button), conditions: [AspectCondition.State(AspectState.Hover)]),
                    [new AspectDeclaration(Control.BorderColorProperty, AspectRef.To(DefaultAspectTokens.Color.Accent))],
                    3));
                components.AddRule(new AspectRuleSet(
                    "playground.button.pressed",
                    AspectLayer.Runtime,
                    new AspectTarget(typeof(Button), conditions: [AspectCondition.State(AspectState.Pressed)]),
                    [new AspectDeclaration(Control.BackgroundProperty, AspectValue<DrawColor>.Literal(new DrawColor(67, 56, 202)))],
                    4));
                components.AddRule(new AspectRuleSet(
                    "playground.button.focus-content-slot",
                    AspectLayer.Runtime,
                    new AspectTarget(typeof(ContentPresenter), ButtonSlots.Content, [AspectCondition.State(AspectState.Focus)]),
                    [new AspectDeclaration(Control.ForegroundProperty, AspectRef.To(LiveAccentToken))],
                    5));
            })
            .Content(content => content.Add(new ContentTemplateDefinition(
                "Playground.ModernAspect.Card",
                typeof(AspectDemoCard),
                "aspect-card",
                CreateCardTemplate(new PlaygroundText()))));
    }

    public UIElement Build()
    {
        StackPanel panel = new()
        {
            Margin = new Thickness(32),
            Orientation = PanelOrientation.Vertical
        };

        panel.VisualChildren.Add(text.Create("Modern Aspect", 22, new DrawColor(20, 28, 42), new Thickness(0, 0, 0, 12)));

        panel.VisualChildren.Add(SectionLabel("Button variants"));
        TextBlock actionStatus = text.Create("Button action: none yet", 13, new DrawColor(71, 85, 105), new Thickness(0, 0, 0, 8));
        StackPanel variants = new()
        {
            Orientation = PanelOrientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 12)
        };
        variants.VisualChildren.Add(CreateButton("Primary token-backed chrome", ButtonKind.Primary, onClick: label => actionStatus.Text = $"Button action: {label}"));
        variants.VisualChildren.Add(CreateButton("Danger variant", ButtonKind.Danger, onClick: label => actionStatus.Text = $"Button action: {label}"));
        variants.VisualChildren.Add(CreateButton("Small size", size: ButtonSize.Small, onClick: label => actionStatus.Text = $"Button action: {label}"));
        panel.VisualChildren.Add(variants);
        panel.VisualChildren.Add(actionStatus);

        panel.VisualChildren.Add(SectionLabel("States and slot targeting"));
        panel.VisualChildren.Add(new Border
        {
            Margin = new Thickness(0, 0, 0, 12),
            Padding = new Thickness(12),
            Background = new DrawColor(241, 245, 249),
            BorderColor = new DrawColor(203, 213, 225),
            BorderThickness = new Thickness(1),
            Child = text.Create(
                "Hover changes the button border, pressed changes background, and focus targets the ButtonSlots.Content presenter.",
                14,
                new DrawColor(51, 65, 85))
        });

        panel.VisualChildren.Add(SectionLabel("Semantic token"));
        panel.VisualChildren.Add(new Border
        {
            Margin = new Thickness(0, 0, 0, 12),
            Padding = new Thickness(12),
            Background = new DrawColor(238, 242, 255),
            BorderColor = new DrawColor(129, 140, 248),
            BorderThickness = new Thickness(1),
            Child = text.Create(
                "Live token tweak: swap LiveAccentToken in the package and the same aspect rules update.",
                14,
                new DrawColor(49, 46, 129))
        });

        panel.VisualChildren.Add(SectionLabel("Content templates"));
        StackPanel templates = new()
        {
            Orientation = PanelOrientation.Vertical
        };
        templates.VisualChildren.Add(new ContentPresenter
        {
            Content = new AspectDemoCard("Data template", "Resolved by ContentTemplateRegistry for AspectDemoCard."),
            ContentTemplateKey = "aspect-card",
            LocalTemplateRegistry = contentTemplates
        });
        templates.VisualChildren.Add(new ContentPresenter
        {
            Content = new AspectDemoCard("Typed content", "The same registry can choose visuals by key/type instead of a hard-coded child."),
            ContentTemplateKey = "aspect-card",
            LocalTemplateRegistry = contentTemplates
        });
        panel.VisualChildren.Add(templates);

        return panel;
    }

    private TextBlock SectionLabel(string value)
    {
        return text.Create(value, 13, new DrawColor(71, 85, 105), new Thickness(0, 0, 0, 6));
    }

    private Button CreateButton(string label, ButtonKind? kind = null, ButtonSize? size = null, Action<string>? onClick = null)
    {
        Button button = new()
        {
            Content = label,
            ResourceProvider = resourceProvider,
            FontResourceId = fontResourceId,
            ComponentTemplate = ButtonTemplates.Modern,
            Padding = new Thickness(14, 9, 14, 9),
            Margin = new Thickness(0, 0, 8, 0),
            Command = new ActionCommand(_ => onClick?.Invoke(label))
        };

        if (kind is ButtonKind buttonKind)
        {
            button.SetAspectVariant(ButtonVariants.Kind, buttonKind);
        }

        if (size is ButtonSize buttonSize)
        {
            button.SetAspectVariant(ButtonVariants.Size, buttonSize);
        }

        return button;
    }

    private static ContentTemplateRegistry CreateContentTemplates(PlaygroundText text)
    {
        ContentTemplateRegistry registry = new();
        registry.Register(CreateCardTemplate(text));
        return registry;
    }

    private static ContentTemplate<AspectDemoCard> CreateCardTemplate(PlaygroundText text)
    {
        return new ContentTemplate<AspectDemoCard>(
            "Playground.ModernAspect.Card",
            "aspect-card",
            10,
            context =>
            {
                AspectDemoCard card = context.Data ?? new AspectDemoCard(string.Empty, string.Empty);
                return new Border
                {
                    Padding = new Thickness(12),
                    Background = new DrawColor(255, 255, 255),
                    BorderColor = new DrawColor(148, 163, 184),
                    BorderThickness = new Thickness(1),
                    Child = new StackPanel
                    {
                        Orientation = PanelOrientation.Vertical,
                        VisualChildren =
                        {
                            text.Create(card.Title, 16, new DrawColor(28, 35, 48)),
                            text.Create(card.Body, 13, new DrawColor(71, 85, 105))
                        }
                    }
                };
            });
    }

    public sealed record AspectDemoCard(string Title, string Body);
}
