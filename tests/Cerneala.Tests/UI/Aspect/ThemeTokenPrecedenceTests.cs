using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Buttons;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Cerneala.UI.Media;
using Cerneala.UI.Resources;
using Cerneala.UI.Theming;

namespace Cerneala.Tests.UI.Aspect;

public sealed class ThemeTokenPrecedenceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ScopedOverrideWinsWithOrWithoutTheme(bool installTheme)
    {
        SolidColorBrush scoped = new(new Color(255, 0, 0));
        UIRoot root = new(200, 200);
        if (installTheme)
        {
            root.SetThemeProvider(new ThemeProvider(DefaultTheme.Create()));
        }

        Border scope = new();
        scope.Resources.Add("ScopedTokens", BackgroundPackage("Scoped", scoped));
        Button button = new();
        scope.Child = button;
        root.LogicalChildren.Add(scope);
        root.VisualChildren.Add(scope);
        root.ProcessFrame();

        Assert.Same(scoped, Background(root, button));
        Assert.Same(scoped, button.Background);
    }

    [Fact]
    public void ApplicationOverrideBeatsTheme()
    {
        SolidColorBrush application = new(new Color(255, 0, 0));
        ResourceDictionary resources = new();
        resources.Add("ApplicationTokens", BackgroundPackage("Application", application));
        UIRoot root = new(200, 200);
        root.SetResourceProvider(resources);
        root.SetThemeProvider(new ThemeProvider(DefaultTheme.Create()));
        Button button = new();
        root.VisualChildren.Add(button);
        root.ProcessFrame();

        Assert.Same(application, Background(root, button));
        Assert.Same(application, button.Background);
    }

    [Fact]
    public void ThemeBeatsFrameworkDefaults()
    {
        Color surface = new(12, 34, 56);
        UIRoot root = new(200, 200);
        Button button = new();
        root.VisualChildren.Add(button);
        root.ProcessFrame();
        Color frameworkSurface = Assert.IsType<SolidColorBrush>(Background(root, button)).Color;
        Assert.NotEqual(surface, frameworkSurface);

        root.SetThemeProvider(new ThemeProvider(DefaultTheme.Create().Set(DefaultTheme.SurfaceKey, surface)));
        root.ProcessFrame();

        Assert.Equal(surface, Assert.IsType<SolidColorBrush>(Background(root, button)).Color);
        Assert.Equal(surface, Assert.IsType<SolidColorBrush>(button.Background).Color);
    }

    [Fact]
    public void ThemeAppliesWithoutOverridesEvenWhenFrameworkPackageIsRemoved()
    {
        Color surface = new(12, 34, 56);
        UIRoot root = new(200, 200);
        AspectPackage framework = Assert.Single(root.AspectRegistry.Packages);
        Assert.True(root.AspectRegistry.Unregister(framework.Name));
        root.SetThemeProvider(new ThemeProvider(DefaultTheme.Create().Set(DefaultTheme.SurfaceKey, surface)));
        Button button = new();
        root.VisualChildren.Add(button);
        root.ProcessFrame();

        Assert.Equal(surface, Assert.IsType<SolidColorBrush>(Background(root, button)).Color);
    }

    [Fact]
    public void ExplicitRootRegistrationIsNotAFrameworkDefaultEvenWithSameNameAndOrigin()
    {
        SolidColorBrush explicitBrush = new(new Color(255, 0, 0));
        UIRoot root = new(200, 200);
        AspectPackage framework = Assert.Single(root.AspectRegistry.Packages);
        Assert.True(root.AspectRegistry.Unregister(framework.Name));
        AspectPackage replacement = BackgroundPackage(framework.Name, explicitBrush);
        Assert.Equal(framework.Origin, replacement.Origin);
        root.AspectRegistry.Register(replacement);
        root.SetThemeProvider(new ThemeProvider(DefaultTheme.Create()));
        Button button = new();
        root.VisualChildren.Add(button);
        root.ProcessFrame();
        Assert.Same(explicitBrush, Background(root, button));

        Assert.True(root.AspectRegistry.Unregister(replacement.Name));
        root.ProcessFrame();
        Assert.Equal(Color.White, Assert.IsType<SolidColorBrush>(Background(root, button)).Color);
    }

    [Fact]
    public void NearerScopesWinAndRemovalRestoresApplicationThenCurrentTheme()
    {
        SolidColorBrush application = new(new Color(10, 0, 0));
        SolidColorBrush outerBrush = new(new Color(20, 0, 0));
        SolidColorBrush innerBrush = new(new Color(30, 0, 0));
        ResourceDictionary resources = new();
        resources.Add("Tokens", BackgroundPackage("Application", application));
        UIRoot root = new(200, 200);
        ThemeProvider provider = new(DefaultTheme.Create());
        root.SetResourceProvider(resources);
        root.SetThemeProvider(provider);
        Border outer = new();
        outer.Resources.Add("Tokens", BackgroundPackage("Outer", outerBrush));
        StackPanel children = new();
        Border inner = new();
        inner.Resources.Add("Tokens", BackgroundPackage("Inner", innerBrush));
        Button button = new();
        Button sibling = new();
        inner.Child = button;
        children.VisualChildren.Add(inner);
        children.VisualChildren.Add(sibling);
        outer.Child = children;
        root.VisualChildren.Add(outer);
        root.ProcessFrame();
        AspectEnvironment environment = root.AspectProcessor.GetEnvironment(button);
        Assert.Same(innerBrush, Background(root, button));
        Assert.Same(outerBrush, Background(root, sibling));
        int version = environment.Version;
        Assert.Same(environment, root.AspectProcessor.GetEnvironment(button));
        Assert.Equal(version, environment.Version);
        Assert.False(root.ProcessFrame().HasWork);

        Color changedSurface = new(12, 34, 56);
        provider.Theme = DefaultTheme.Create().Set(DefaultTheme.SurfaceKey, changedSurface);
        root.ProcessFrame();
        Assert.Same(innerBrush, Background(root, button));

        inner.Resources.Remove("Tokens");
        root.ProcessFrame();
        Assert.Same(outerBrush, Background(root, button));
        outer.Resources.Remove("Tokens");
        root.ProcessFrame();
        Assert.Same(application, Background(root, button));
        resources.Remove("Tokens");
        root.ProcessFrame();
        Assert.Equal(changedSurface, Assert.IsType<SolidColorBrush>(Background(root, button)).Color);
        Assert.Same(environment, root.AspectProcessor.GetEnvironment(button));

        root.SetThemeProvider(null);
        root.ProcessFrame();
        Assert.Equal(Color.White, Assert.IsType<SolidColorBrush>(Background(root, button)).Color);
        Assert.False(root.ProcessFrame().HasWork);
    }

    [Fact]
    public void NullIsAnExplicitOverrideAndTemplateBindingsObserveIt()
    {
        UIRoot root = new(200, 200);
        root.SetThemeProvider(new ThemeProvider(DefaultTheme.Create()));
        Border scope = new();
        scope.Resources.Add("Tokens", BackgroundPackage("Null", null));
        Button button = new();
        Border templateRoot = new();
        scope.Child = button;
        root.VisualChildren.Add(scope);
        button.ComponentTemplate = new ComponentTemplate<Button>("token", context =>
        {
            context.BindToken(ButtonTokens.Background, templateRoot, Control.BackgroundProperty);
            return templateRoot;
        });
        root.ProcessFrame();

        Assert.Null(Background(root, button));
        Assert.Null(button.Background);
        Assert.Null(templateRoot.Background);

        scope.Resources.Remove("Tokens");
        root.ProcessFrame();
        Assert.Equal(Color.White, Assert.IsType<SolidColorBrush>(Background(root, button)).Color);
        Assert.Equal(Color.White, Assert.IsType<SolidColorBrush>(button.Background).Color);
        Assert.Same(templateRoot, button.ComponentTemplateInstance!.Root);
        Assert.Equal(Color.White, Assert.IsType<SolidColorBrush>(templateRoot.Background).Color);
    }

    [Fact]
    public void SemanticAndRawThemeTokensAcceptExplicitOverridesAndReferencesSeeThemeFallback()
    {
        Color themeSurface = new(12, 34, 56);
        Color explicitAccent = new(255, 0, 0);
        AspectToken<Color> rawAccent = ThemeTokenBridge.ToToken(DefaultTheme.AccentKey);
        AspectToken<Color> custom = AspectToken.Color("application.surface");
        ResourceDictionary resources = new();
        resources.Add("Tokens", new AspectPackage("Application", AspectOrigin.Code(),
            [
                new(DefaultAspectTokens.Color.Accent, AspectValue<Color>.Literal(explicitAccent)),
                new(rawAccent, AspectValue<Color>.Literal(explicitAccent)),
                new(custom, DefaultAspectTokens.Color.Surface.Ref())
            ], [], [], [], []));
        UIRoot root = new(200, 200);
        root.SetResourceProvider(resources);
        root.SetThemeProvider(new ThemeProvider(DefaultTheme.Create().Set(DefaultTheme.SurfaceKey, themeSurface)));
        Button button = new();
        root.VisualChildren.Add(button);
        root.ProcessFrame();
        AspectEnvironment environment = root.AspectProcessor.GetEnvironment(button);

        Assert.True(environment.TryGet(DefaultAspectTokens.Color.Accent, out Color semantic));
        Assert.Equal(explicitAccent, semantic);
        Assert.True(environment.TryGet(rawAccent, out Color raw));
        Assert.Equal(explicitAccent, raw);
        Assert.True(environment.TryGet(custom, out Color fallback));
        Assert.Equal(themeSurface, fallback);
    }

    [Fact]
    public void MissingThemeKeyLeavesFrameworkDefaultAvailable()
    {
        UIRoot root = new(200, 200);
        root.SetThemeProvider(new ThemeProvider(new Theme("Partial").Set(DefaultTheme.AccentKey, Color.Black)));
        Button button = new();
        root.VisualChildren.Add(button);
        root.ProcessFrame();

        Assert.Equal(Color.White, Assert.IsType<SolidColorBrush>(Background(root, button)).Color);
    }

    private static AspectPackage BackgroundPackage(string name, Brush? brush) =>
        AspectPackage.Create(name).Tokens(tokens => tokens.Set(ButtonTokens.Background, brush)).Build();

    private static Brush? Background(UIRoot root, Button button)
    {
        Assert.True(root.AspectProcessor.GetEnvironment(button).TryGet(ButtonTokens.Background, out Brush? value));
        return value;
    }
}
