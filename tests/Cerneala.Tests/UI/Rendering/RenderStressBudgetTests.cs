using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Resources;
using Cerneala.UI.Theming;
using StackPanel = Cerneala.UI.Controls.StackPanel;

namespace Cerneala.Tests.UI.Rendering;

public sealed class RenderStressBudgetTests
{
    private const int ResourceDependents = 5;
    private const int UnrelatedElements = 40;

    [Fact]
    public void ThemeColorChangeDoesNotMeasureLargeTreeWhenOnlyRenderAspectChanges()
    {
        UIRoot root = ThemedRoot(out ThemeProvider provider);
        root.VisualChildren.Add(BuildThemedTree());
        root.ProcessFrame();

        provider.Theme = new Theme("Changed")
            .Set(DefaultTheme.PaletteKey, DefaultTheme.Create().Get(DefaultTheme.PaletteKey))
            .Set(DefaultTheme.BackgroundKey, new DrawColor(10, 20, 30))
            .Set(DefaultTheme.ForegroundKey, new DrawColor(40, 50, 60))
            .Set(DefaultTheme.SurfaceKey, new DrawColor(70, 80, 90))
            .Set(DefaultTheme.BorderKey, new DrawColor(100, 110, 120))
            .Set(DefaultTheme.AccentKey, new DrawColor(130, 140, 150));

        FrameStats stats = root.ProcessFrame();

        Assert.True(stats.AspectElements > 0);
        Assert.Equal(0, stats.MeasuredElements);
        Assert.Equal(0, stats.ArrangedElements);
        Assert.Equal(0, stats.MeasureCalls);
        Assert.Equal(0, stats.ArrangeCalls);
    }

    [Fact]
    public void ResourceChangeInvalidatesOnlyRegisteredDependentsWithinBudget()
    {
        ResourceId<ImageResource> id = new("Logo");
        ObservableProvider provider = new();
        provider.SetResource(id, new ImageResource(new TestImage(16, 16)));
        UIRoot root = RootWithResources(provider);
        root.VisualChildren.Add(BuildImageResourceTree(id));
        root.ProcessFrame();

        provider.SetResource(id, new ImageResource(new TestImage(24, 24)));
        FrameStats stats = root.ProcessFrame();

        Assert.Equal(0, stats.MeasuredElements);
        Assert.Equal(0, stats.ArrangedElements);
        Assert.True(stats.RenderedElements > 0);
        Assert.True(stats.RenderedElements <= ResourceDependents + 2, $"Rendered {stats.RenderedElements} elements for {ResourceDependents} image dependents.");
        Assert.True(stats.AspectElements <= 1, $"Aspect {stats.AspectElements} elements for an image resource change.");
    }

    [Fact]
    public void FontResourceChangeInvalidatesOnlyTextDependentsWithinBudget()
    {
        ResourceId<FontResource> id = new("Body");
        ObservableProvider provider = new();
        provider.SetResource(id, new FontResource(new TestFont("Body", 16)));
        UIRoot root = RootWithResources(provider);
        root.VisualChildren.Add(BuildFontResourceTree(id));
        root.ProcessFrame();

        provider.SetResource(id, new FontResource(new TestFont("Updated", 16)));
        FrameStats stats = root.ProcessFrame();

        Assert.True(stats.MeasuredElements > 0);
        Assert.True(stats.RenderedElements > 0);
        Assert.True(stats.MeasuredElements <= ResourceDependents + 3, $"Measured {stats.MeasuredElements} elements for {ResourceDependents} text dependents.");
        Assert.True(stats.RenderedElements <= ResourceDependents + 3, $"Rendered {stats.RenderedElements} elements for {ResourceDependents} text dependents.");
        Assert.True(stats.HitTestElements <= ResourceDependents + 3, $"Rebuilt hit-test for {stats.HitTestElements} elements.");
    }

    private static UIRoot ThemedRoot(out ThemeProvider provider)
    {
        provider = new ThemeProvider(DefaultTheme.Create());
        UIRoot root = new(800, 600);
        root.SetThemeProvider(provider);
        return root;
    }

    private static UIRoot RootWithResources(IResourceProvider provider)
    {
        UIRoot root = new(800, 600);
        root.SetResourceProvider(provider);
        return root;
    }

    private static UIElement BuildThemedTree()
    {
        StackPanel root = new()
        {
            Orientation = Orientation.Vertical
        };

        for (int i = 0; i < UnrelatedElements; i++)
        {
            root.VisualChildren.Add(new Button { Content = $"Action {i}" });
        }

        return root;
    }

    private static UIElement BuildImageResourceTree(ResourceId<ImageResource> id)
    {
        StackPanel root = new()
        {
            Orientation = Orientation.Vertical
        };

        for (int i = 0; i < ResourceDependents; i++)
        {
            root.VisualChildren.Add(new Image
            {
                SourceResourceId = id,
                UseIntrinsicSize = false
            });
        }

        AddUnrelatedRows(root);
        return root;
    }

    private static UIElement BuildFontResourceTree(ResourceId<FontResource> id)
    {
        StackPanel root = new()
        {
            Orientation = Orientation.Vertical
        };

        for (int i = 0; i < ResourceDependents; i++)
        {
            root.VisualChildren.Add(new TextBlock
            {
                Text = $"Text {i}",
                FontResourceId = id
            });
        }

        AddUnrelatedRows(root);
        return root;
    }

    private static void AddUnrelatedRows(StackPanel root)
    {
        for (int i = 0; i < UnrelatedElements; i++)
        {
            root.VisualChildren.Add(new Button { Content = $"Unrelated {i}" });
        }
    }

    private sealed record TestFont(string FamilyName, float Size) : IDrawFont;

    private sealed class TestImage(int width, int height) : IDrawImage
    {
        public int Width { get; } = width;

        public int Height { get; } = height;
    }

    private sealed class ObservableProvider : IResourceProvider, IObservableResourceProvider
    {
        private readonly Dictionary<(Type Type, string Key), object?> resources = new();

        public event EventHandler<ResourceChangedEventArgs>? ResourceChanged;

        public void SetResource<T>(ResourceId<T> id, T resource)
        {
            object? oldValue = TryGetResource(id, out T? existing) ? existing : null;
            resources[(typeof(T), id.Key)] = resource;
            ResourceChanged?.Invoke(this, new ResourceChangedEventArgs(typeof(T), id.Key, oldValue, resource, 1));
        }

        public bool TryGetResource<T>(ResourceId<T> id, out T resource)
        {
            if (resources.TryGetValue((typeof(T), id.Key), out object? value) && value is T typed)
            {
                resource = typed;
                return true;
            }

            resource = default!;
            return false;
        }
    }
}
