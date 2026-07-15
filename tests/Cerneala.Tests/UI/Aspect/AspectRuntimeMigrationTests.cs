using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Cerneala.UI.Media;
using Cerneala.UI.Theming;

namespace Cerneala.Tests.UI.Aspect;

public sealed class AspectRuntimeMigrationTests
{
    [Fact]
    public void ProcessorEvaluatesDataConditionsAgainstElementDataContext()
    {
        UIRoot root = new();
        Border border = new() { DataContext = new Status(true) };
        root.VisualChildren.Add(border);
        root.AspectRegistry.Register(AspectPackage.Create("Data runtime")
            .Components(components => components.AddRule(new AspectRuleSet(
                "active",
                AspectLayer.App,
                new AspectTarget(typeof(Border), conditions:
                [
                    AspectCondition.Data<Status>(
                        "active",
                        status => status.IsActive,
                        new AspectDataDependency("Status.IsActive", typeof(Status), nameof(Status.IsActive)))
                ]),
                [
                    new AspectDeclaration(UIElement.OpacityProperty, AspectValue<float>.Literal(0.5f))
                ],
                declarationOrder: 0))));

        root.AspectProcessor.Process(border);

        Assert.Equal(0.5f, border.Opacity);
    }

    [Fact]
    public void ProcessorProjectsCurrentThemeBrushesAndRefreshesThemAfterThemeChange()
    {
        Color firstSurface = new(12, 34, 56);
        Color secondSurface = new(65, 43, 21);
        ThemeProvider provider = new(CreateTheme(firstSurface));
        UIRoot root = new();
        root.SetThemeProvider(provider);
        Button button = new();
        root.VisualChildren.Add(button);

        root.AspectProcessor.Process(button);
        Assert.Equal(firstSurface, Assert.IsType<SolidColorBrush>(button.Background).Color);

        provider.Theme = CreateTheme(secondSurface);
        root.AspectProcessor.Process(button);

        Assert.Equal(secondSurface, Assert.IsType<SolidColorBrush>(button.Background).Color);
    }

    [Fact]
    public void AspectInvalidationReappliesTrackedElementAfterRelevantPropertyChange()
    {
        Border border = new();
        AspectCatalog catalog = AspectCatalog.FromPackages(
        [
            AspectPackage.Create("Tracked")
                .Components(components => components.AddRule(new AspectRuleSet(
                    "hover",
                    AspectLayer.App,
                    new AspectTarget(typeof(Border), conditions:
                    [
                        AspectCondition.Property(UIElement.IsPointerOverProperty).Is(true)
                    ]),
                    [
                        new AspectDeclaration(UIElement.OpacityProperty, AspectValue<float>.Literal(0.5f))
                    ],
                    declarationOrder: 0)))
        ],
        version: 1);
        AspectInvalidation invalidation = new(new AspectEngine(), catalog, new AspectEnvironment("tracked"));
        invalidation.Track(border);

        border.IsPointerOver = true;

        Assert.Equal(0.5f, border.Opacity);
    }

    [Fact]
    public void RootTemplateTokenBindingRefreshesWhenThemeChanges()
    {
        Color firstForeground = new(20, 40, 60);
        Color secondForeground = new(60, 40, 20);
        ThemeProvider provider = new(DefaultTheme.Create().Set(DefaultTheme.ForegroundKey, firstForeground));
        UIRoot root = new();
        root.SetThemeProvider(provider);
        Button button = new();
        Border templateRoot = new();
        root.VisualChildren.Add(button);
        button.ComponentTemplate = new ComponentTemplate<Button>("theme-aware", context =>
        {
            context.BindToken(DefaultAspectTokens.Brush.Foreground, templateRoot, Control.BackgroundProperty);
            return templateRoot;
        });

        Assert.Equal(firstForeground, Assert.IsType<SolidColorBrush>(templateRoot.Background).Color);

        provider.Theme = DefaultTheme.Create().Set(DefaultTheme.ForegroundKey, secondForeground);
        root.AspectProcessor.Process(button);

        Assert.Equal(secondForeground, Assert.IsType<SolidColorBrush>(templateRoot.Background).Color);
    }

    [Fact]
    public void AspectInvalidationReappliesTokenChangesUntilDisposed()
    {
        AspectToken<float> opacity = AspectToken.Create<float>("tracked.opacity");
        AspectEnvironment environment = new("tracked");
        environment.Set(opacity, 0.25f);
        AspectCatalog catalog = AspectCatalog.FromPackages(
        [
            AspectPackage.Create("Token tracked")
                .Components(components => components.AddRule(new AspectRuleSet(
                    "opacity",
                    AspectLayer.App,
                    new AspectTarget(typeof(Border)),
                    [new AspectDeclaration(UIElement.OpacityProperty, opacity.Ref())],
                    declarationOrder: 0)))
        ],
        version: 1);
        Border border = new();
        AspectInvalidation invalidation = new(new AspectEngine(), catalog, environment);
        invalidation.Track(border);

        environment.Set(opacity, 0.75f);
        Assert.Equal(0.75f, border.Opacity);

        invalidation.Dispose();
        environment.Set(opacity, 0.5f);
        Assert.Equal(0.75f, border.Opacity);
    }

    private static Theme CreateTheme(Color surface)
    {
        return DefaultTheme.Create()
            .Set(DefaultTheme.SurfaceKey, surface);
    }

    private sealed record Status(bool IsActive);
}
