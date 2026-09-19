using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Buttons;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Aspect;

public static class DefaultAspectPackage
{
    public static AspectPackage Create()
    {
        return AspectPackage.Create("Default")
            .Tokens(tokens => AddTokens(new PackageTokenWriter(tokens)))
            .Components(components =>
            {
                components.AddTemplate(new ComponentTemplateDefinition("Button.Modern", typeof(Button), ButtonTemplates.Modern));
                components.AddRule(Rule("button.base", typeof(Button),
                    new AspectDeclaration(Control.BackgroundProperty, ButtonTokens.Background.Ref()),
                    new AspectDeclaration(Control.ForegroundProperty, ButtonTokens.Foreground.Ref()),
                    new AspectDeclaration(Control.BorderBrushProperty, ButtonTokens.BorderBrush.Ref()),
                    new AspectDeclaration(Control.BorderThicknessProperty, DefaultAspectTokens.Stroke.ControlBorderThickness.Ref()),
                    new AspectDeclaration(Control.PaddingProperty, ButtonTokens.Padding.Ref())));
                components.AddRule(Rule("border.base", typeof(Border),
                    new AspectDeclaration(Control.BorderBrushProperty, DefaultAspectTokens.Brush.Border.Ref())));
            });
    }

    public static AspectEnvironment CreateEnvironment()
    {
        AspectEnvironment environment = new("default");
        AddTokens(new EnvironmentTokenWriter(environment));
        return environment;
    }

    private static void AddTokens(ITokenWriter tokens)
    {
        tokens.Set(DefaultAspectTokens.Color.Background, new Color(248, 250, 252));
        tokens.Set(DefaultAspectTokens.Color.Foreground, new Color(28, 35, 48));
        tokens.Set(DefaultAspectTokens.Color.Surface, new Color(255, 255, 255));
        tokens.Set(DefaultAspectTokens.Color.Border, new Color(148, 163, 184));
        tokens.Set(DefaultAspectTokens.Color.Accent, new Color(37, 99, 235));
        tokens.Set(DefaultAspectTokens.Brush.Background, new SolidColorBrush(new Color(248, 250, 252)));
        tokens.Set(DefaultAspectTokens.Brush.Surface, new SolidColorBrush(new Color(255, 255, 255)));
        tokens.Set(DefaultAspectTokens.Brush.Border, new SolidColorBrush(new Color(148, 163, 184)));
        tokens.Set(DefaultAspectTokens.Brush.Foreground, new SolidColorBrush(new Color(28, 35, 48)));
        tokens.Set(DefaultAspectTokens.Typography.FontFamily, "Default");
        tokens.Set(DefaultAspectTokens.Typography.FontSize, 16f);
        tokens.Set(DefaultAspectTokens.Spacing.ControlPadding, new Thickness(8));
        tokens.Set(DefaultAspectTokens.Stroke.ControlBorderThickness, new Thickness(1));
        tokens.Set(DefaultAspectTokens.Motion.Fast, new TweenSpec<float>(TimeSpan.FromMilliseconds(120)));
        tokens.Set(DefaultAspectTokens.Motion.Normal, new TweenSpec<float>(TimeSpan.FromMilliseconds(200)));
        tokens.Set(ButtonTokens.Background, new SolidColorBrush(new Color(255, 255, 255)));
        tokens.Set(ButtonTokens.Foreground, new SolidColorBrush(new Color(28, 35, 48)));
        tokens.Set(ButtonTokens.BorderBrush, new SolidColorBrush(new Color(148, 163, 184)));
        tokens.Set(ButtonTokens.HoverBackground, new SolidColorBrush(new Color(37, 99, 235)));
        tokens.Set(ButtonTokens.PressedBackground, new SolidColorBrush(new Color(148, 163, 184)));
        tokens.Set(ButtonTokens.DisabledOpacity, 0.5f);
        tokens.Set(ButtonTokens.Padding, new Thickness(8));
    }

    private static AspectRuleSet Rule(string name, Type type, params AspectDeclaration[] declarations)
    {
        return new AspectRuleSet(name, AspectLayer.Theme, new AspectTarget(type), declarations, 0);
    }

    private interface ITokenWriter
    {
        void Set<T>(AspectToken<T> token, T value);
    }

    private sealed class PackageTokenWriter(AspectTokenBuilder builder) : ITokenWriter
    {
        public void Set<T>(AspectToken<T> token, T value)
        {
            builder.Set(token, value);
        }
    }

    private sealed class EnvironmentTokenWriter(AspectEnvironment environment) : ITokenWriter
    {
        public void Set<T>(AspectToken<T> token, T value)
        {
            environment.Set(token, value);
        }
    }
}
