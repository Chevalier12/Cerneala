using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Buttons;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Aspect;

public static class DefaultAspectPackage
{
    public static AspectPackage Create()
    {
        return AspectPackage.Create("Default")
            .Tokens(tokens => AddTokens(tokens))
            .Components(components =>
            {
                components.AddTemplate(new ComponentTemplateDefinition("Button.Modern", typeof(Button), ButtonTemplates.Modern));
                components.AddRule(Rule("button.base", typeof(Button),
                    new AspectDeclaration(Control.BackgroundProperty, ButtonTokens.Background.Ref()),
                    new AspectDeclaration(Control.ForegroundProperty, ButtonTokens.Foreground.Ref()),
                    new AspectDeclaration(Control.BorderColorProperty, ButtonTokens.BorderColor.Ref()),
                    new AspectDeclaration(Control.BorderThicknessProperty, DefaultAspectTokens.Stroke.ControlBorderThickness.Ref()),
                    new AspectDeclaration(Control.PaddingProperty, ButtonTokens.Padding.Ref())));
                components.AddRule(Rule("border.base", typeof(Border),
                    new AspectDeclaration(Control.BackgroundProperty, DefaultAspectTokens.Color.Surface.Ref()),
                    new AspectDeclaration(Control.BorderColorProperty, DefaultAspectTokens.Color.Border.Ref())));
            });
    }

    public static AspectEnvironment CreateEnvironment()
    {
        AspectEnvironment environment = new("default");
        SetTokens(environment);
        return environment;
    }

    private static void AddTokens(AspectTokenBuilder tokens)
    {
        tokens.Set(DefaultAspectTokens.Color.Background, new DrawColor(248, 250, 252));
        tokens.Set(DefaultAspectTokens.Color.Foreground, new DrawColor(28, 35, 48));
        tokens.Set(DefaultAspectTokens.Color.Surface, new DrawColor(255, 255, 255));
        tokens.Set(DefaultAspectTokens.Color.Border, new DrawColor(148, 163, 184));
        tokens.Set(DefaultAspectTokens.Color.Accent, new DrawColor(37, 99, 235));
        tokens.Set(DefaultAspectTokens.Typography.FontFamily, "Default");
        tokens.Set(DefaultAspectTokens.Typography.FontSize, 16f);
        tokens.Set(DefaultAspectTokens.Spacing.ControlPadding, new Thickness(8));
        tokens.Set(DefaultAspectTokens.Stroke.ControlBorderThickness, new Thickness(1));
        tokens.Set(DefaultAspectTokens.Motion.Fast, new TweenSpec<float>(TimeSpan.FromMilliseconds(120)));
        tokens.Set(DefaultAspectTokens.Motion.Normal, new TweenSpec<float>(TimeSpan.FromMilliseconds(200)));
        tokens.Set(ButtonTokens.Background, new DrawColor(255, 255, 255));
        tokens.Set(ButtonTokens.Foreground, new DrawColor(28, 35, 48));
        tokens.Set(ButtonTokens.BorderColor, new DrawColor(148, 163, 184));
        tokens.Set(ButtonTokens.HoverBackground, new DrawColor(37, 99, 235));
        tokens.Set(ButtonTokens.PressedBackground, new DrawColor(148, 163, 184));
        tokens.Set(ButtonTokens.DisabledOpacity, 0.5f);
        tokens.Set(ButtonTokens.Padding, new Thickness(8));
    }

    private static void SetTokens(AspectEnvironment environment)
    {
        environment.Set(DefaultAspectTokens.Color.Background, new DrawColor(248, 250, 252));
        environment.Set(DefaultAspectTokens.Color.Foreground, new DrawColor(28, 35, 48));
        environment.Set(DefaultAspectTokens.Color.Surface, new DrawColor(255, 255, 255));
        environment.Set(DefaultAspectTokens.Color.Border, new DrawColor(148, 163, 184));
        environment.Set(DefaultAspectTokens.Color.Accent, new DrawColor(37, 99, 235));
        environment.Set(DefaultAspectTokens.Typography.FontFamily, "Default");
        environment.Set(DefaultAspectTokens.Typography.FontSize, 16f);
        environment.Set(DefaultAspectTokens.Spacing.ControlPadding, new Thickness(8));
        environment.Set(DefaultAspectTokens.Stroke.ControlBorderThickness, new Thickness(1));
        environment.Set(DefaultAspectTokens.Motion.Fast, new TweenSpec<float>(TimeSpan.FromMilliseconds(120)));
        environment.Set(DefaultAspectTokens.Motion.Normal, new TweenSpec<float>(TimeSpan.FromMilliseconds(200)));
        environment.Set(ButtonTokens.Background, new DrawColor(255, 255, 255));
        environment.Set(ButtonTokens.Foreground, new DrawColor(28, 35, 48));
        environment.Set(ButtonTokens.BorderColor, new DrawColor(148, 163, 184));
        environment.Set(ButtonTokens.HoverBackground, new DrawColor(37, 99, 235));
        environment.Set(ButtonTokens.PressedBackground, new DrawColor(148, 163, 184));
        environment.Set(ButtonTokens.DisabledOpacity, 0.5f);
        environment.Set(ButtonTokens.Padding, new Thickness(8));
    }

    private static AspectRuleSet Rule(string name, Type type, params AspectDeclaration[] declarations)
    {
        return new AspectRuleSet(name, AspectLayer.Theme, new AspectTarget(type), declarations, 0);
    }
}
