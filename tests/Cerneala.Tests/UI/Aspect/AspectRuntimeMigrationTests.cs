using System.ComponentModel;
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
    public void InstalledThemeRejectsMutationAndReplacementRefreshesEnvironmentOnNextFrame()
    {
        Color first = new(10, 20, 30);
        Color second = new(200, 100, 50);
        Theme theme = DefaultTheme.Create().Set(DefaultTheme.SurfaceKey, first);
        ThemeProvider provider = new(theme);
        UIRoot root = new(200, 200);
        root.SetThemeProvider(provider);
        Border border = new();
        root.LogicalChildren.Add(border);
        root.VisualChildren.Add(border);
        root.ProcessFrame();

        Assert.True(root.AspectProcessor.GetEnvironment(border).TryGet(
            DefaultAspectTokens.Color.Surface, out Color initial));
        Assert.Equal(first, initial);

        Assert.Throws<InvalidOperationException>(() => theme.Set(DefaultTheme.SurfaceKey, second));
        root.ProcessFrame();

        Assert.Equal(first, theme.Get(DefaultTheme.SurfaceKey));
        Assert.True(root.AspectProcessor.GetEnvironment(border).TryGet(
            DefaultAspectTokens.Color.Surface, out Color unchanged));
        Assert.Equal(first, unchanged);

        provider.Theme = DefaultTheme.Create().Set(DefaultTheme.SurfaceKey, second);
        root.ProcessFrame();

        Assert.True(root.AspectProcessor.GetEnvironment(border).TryGet(
            DefaultAspectTokens.Color.Surface, out Color after));
        Assert.Equal(second, after);
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

    // #787: a data condition is evaluated again when the data context raises PropertyChanged for a declared dependency.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DataConditionFollowsDeclaredPropertyDependency(bool replaceDataContext)
    {
        UIRoot root = new(200, 200);
        MutableStatus status = new();
        Border border = new() { DataContext = status };
        root.LogicalChildren.Add(border);
        root.VisualChildren.Add(border);
        root.AspectRegistry.Register(ActiveOpacityPackage(() => { }));
        root.ProcessFrame();
        Assert.Equal(1f, border.Opacity);

        if (replaceDataContext)
        {
            border.DataContext = new MutableStatus { IsActive = true };
        }
        else
        {
            status.IsActive = true;
        }

        root.ProcessFrame();

        Assert.Equal(0.5f, border.Opacity);
    }

    // #787: only a declared property re-evaluates the condition, and the subscription follows the data context and the tree.
    [Fact]
    public void DataConditionSubscriptionFollowsTheDataContextAndTheTree()
    {
        UIRoot root = new(200, 200);
        MutableStatus first = new();
        MutableStatus second = new();
        Border border = new() { DataContext = first };
        root.LogicalChildren.Add(border);
        root.VisualChildren.Add(border);
        int evaluations = 0;
        root.AspectRegistry.Register(ActiveOpacityPackage(() => evaluations++));
        root.ProcessFrame();
        Assert.Equal(1, first.SubscriberCount);
        int evaluationsBefore = evaluations;

        first.Other = 3;
        Assert.False(root.ProcessFrame().HasWork);
        Assert.Equal(evaluationsBefore, evaluations);

        border.DataContext = second;
        root.ProcessFrame();
        Assert.Equal(0, first.SubscriberCount);
        Assert.Equal(1, second.SubscriberCount);
        first.IsActive = true;
        Assert.False(root.ProcessFrame().HasWork);
        Assert.Equal(1f, border.Opacity);

        second.IsActive = true;
        root.ProcessFrame();
        Assert.Equal(0.5f, border.Opacity);

        root.VisualChildren.Remove(border);
        root.LogicalChildren.Remove(border);
        root.ProcessFrame();
        Assert.Equal(0, second.SubscriberCount);
    }

    private static AspectPackage ActiveOpacityPackage(Action evaluated)
    {
        return AspectPackage.Create("Data runtime")
            .Components(components => components.AddRule(new AspectRuleSet(
                "active",
                AspectLayer.App,
                new AspectTarget(typeof(Border), conditions:
                [
                    AspectCondition.Data<MutableStatus>(
                        "active",
                        data =>
                        {
                            evaluated();
                            return data.IsActive;
                        },
                        AspectDataDependency.Property<MutableStatus, bool>(nameof(MutableStatus.IsActive)))
                ]),
                [new AspectDeclaration(UIElement.OpacityProperty, AspectValue<float>.Literal(0.5f))],
                declarationOrder: 0)));
    }

    private static Theme CreateTheme(Color surface)
    {
        return DefaultTheme.Create()
            .Set(DefaultTheme.SurfaceKey, surface);
    }

    private sealed record Status(bool IsActive);

    private sealed class MutableStatus : INotifyPropertyChanged
    {
        private bool isActive;
        private int other;
        private PropertyChangedEventHandler? propertyChanged;

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add
            {
                propertyChanged += value;
                SubscriberCount++;
            }
            remove
            {
                propertyChanged -= value;
                SubscriberCount--;
            }
        }

        public int SubscriberCount { get; private set; }

        public bool IsActive
        {
            get => isActive;
            set
            {
                isActive = value;
                propertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActive)));
            }
        }

        public int Other
        {
            get => other;
            set
            {
                other = value;
                propertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Other)));
            }
        }
    }
}
