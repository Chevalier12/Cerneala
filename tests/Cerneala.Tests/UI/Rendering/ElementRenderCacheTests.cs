using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Rendering;

public sealed class ElementRenderCacheTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InvalidatedOrFailedCacheDoesNotRetainItsTransformOwner(bool failRebuild)
    {
        ElementRenderCache cache = new();
        WeakReference reference = PopulateThenInvalidate(cache, failRebuild);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.False(reference.IsAlive);
        GC.KeepAlive(cache);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static WeakReference PopulateThenInvalidate(ElementRenderCache cache, bool failRebuild)
    {
        FailingTransformElement element = new();
        cache.Ensure(element, new RenderCounters());
        cache.GetElementTransform(element);
        if (failRebuild)
        {
            element.Fail = true;
            Assert.Throws<InvalidOperationException>(() => cache.Ensure(element, new RenderCounters(), forceRebuild: true));
        }
        else
        {
            cache.Invalidate();
        }
        return new WeakReference(element);
    }

    private sealed class FailingTransformElement : UIElement
    {
        internal bool Fail { get; set; }

        protected override void OnRender(RenderContext context)
        {
            if (Fail)
            {
                throw new InvalidOperationException("Intentional failed rebuild.");
            }
        }
    }

    [Fact]
    public void ComposedTransformTracksEveryChannelAndLayoutBounds()
    {
        UIElement element = new();
        ElementRenderCache cache = new();
        Action[] changes =
        [
            () => element.SetArrangedBounds(new LayoutRect(3, 5, 40, 20)),
            () => element.Scale = 1.2f,
            () => element.ScaleX = 0.7f,
            () => element.ScaleY = 1.4f,
            () => element.SkewX = 0.2f,
            () => element.SkewY = -0.3f,
            () => element.Rotation = 0.4f,
            () => element.TranslateX = 8,
            () => element.TranslateY = -3,
            () => element.RenderTransform = new Transform(Matrix3x2.CreateScale(0.8f, 1.1f)),
            () => element.RenderTransformOrigin = new LayoutPoint(0.1f, 0.9f),
            () => element.SetPresenceVisual(0.6f, 0.75f),
            () => element.SetLayoutCorrectionTransform(new Transform(Matrix3x2.CreateTranslation(9, 4))),
            () => element.SetArrangedBounds(new LayoutRect(11, 13, 80, 30)),
            () => element.SetPresenceVisual(1, 1),
            () => element.SetLayoutCorrectionTransform(Transform.Identity)
        ];
        foreach (Action change in changes)
        {
            Assert.Equal(ElementVisualTransform.GetElementTransform(element), cache.GetElementTransform(element));
            change();
            Assert.Equal(ElementVisualTransform.GetElementTransform(element), cache.GetElementTransform(element));
            Assert.Equal(ElementVisualTransform.GetElementTransform(element), cache.GetElementTransform(element));
        }
    }

    [Fact]
    public void ComposedTransformSeesPropertyWritesBeforeInvalidationCallbacks()
    {
        UIElement element = new();
        ElementRenderCache cache = new();
        cache.GetElementTransform(element);
        int observed = 0;
        element.PropertyChanged += (_, args) =>
        {
            if (ReferenceEquals(args.Property, UIElement.TranslateXProperty))
            {
                observed++;
                Assert.Equal(ElementVisualTransform.GetElementTransform(element), cache.GetElementTransform(element));
            }
        };

        element.SetValue(UIElement.TranslateXProperty, 3, UiPropertyValueSource.AspectBase);
        element.SetValue(UIElement.TranslateXProperty, 7, UiPropertyValueSource.Animation);
        element.TranslateX = 11;
        element.ClearValue(UIElement.TranslateXProperty);
        element.ClearValueUntyped(UIElement.TranslateXProperty, UiPropertyValueSource.Animation);
        element.SetValueUntyped(UIElement.TranslateXProperty, 5f, UiPropertyValueSource.AspectBase);
        element.ClearValue(UIElement.TranslateXProperty, UiPropertyValueSource.AspectBase);
        element.SetFrameworkDefault(UIElement.TranslateXProperty, 2f);

        Assert.Equal(8, observed);
        Assert.Equal(ElementVisualTransform.GetElementTransform(element), cache.GetElementTransform(element));
    }

    [Fact]
    public void ComposedTransformDoesNotCrossElementOrInvalidationLifetimes()
    {
        UIElement first = new() { ScaleX = 2 };
        UIElement second = new() { ScaleY = 3 };
        Assert.Equal(first.PropertyValueVersion, second.PropertyValueVersion);
        Assert.Equal(first.RenderScopeVersion, second.RenderScopeVersion);
        ElementRenderCache cache = new();
        Assert.Equal(ElementVisualTransform.GetElementTransform(first), cache.GetElementTransform(first));
        Assert.Equal(ElementVisualTransform.GetElementTransform(second), cache.GetElementTransform(second));
        cache.Invalidate();
        Assert.Equal(ElementVisualTransform.GetElementTransform(first), cache.GetElementTransform(first));
    }

    [Fact]
    public void DirtyElementCacheRebuildsLocalCommands()
    {
        RenderingTestElement element = new(Color.White);
        ElementRenderCache cache = new();
        RenderCounters counters = new();

        bool rebuilt = cache.Ensure(element, counters, forceRebuild: true);

        Assert.True(rebuilt);
        Assert.True(cache.IsValid);
        Assert.Single(cache.Commands);
        Assert.Equal(1, element.RenderCount);
        Assert.Equal(1, counters.CacheMisses);
        Assert.Equal(1, counters.LocalRebuilds);
    }

    [Fact]
    public void UnchangedElementCacheIsReused()
    {
        RenderingTestElement element = new(Color.White);
        ElementRenderCache cache = new();
        RenderCounters counters = new();
        cache.Ensure(element, counters, forceRebuild: true);

        bool rebuilt = cache.Ensure(element, counters);

        Assert.False(rebuilt);
        Assert.Equal(1, element.RenderCount);
        Assert.Equal(1, counters.CacheHits);
    }

    [Fact]
    public void DependencyChangeMakesCacheStale()
    {
        RenderingTestElement element = new(Color.White);
        ElementRenderCache cache = new();
        RenderCounters counters = new();
        cache.Ensure(element, counters, forceRebuild: true);

        element.ChangeDependencies(RenderDependency.None.WithTextVersion(1));

        Assert.True(cache.IsStale(element));
    }

    [Fact]
    public void CacheBuiltForOneElementIsStaleForAnotherElementWithMatchingVersion()
    {
        RenderingTestElement first = new(Color.White);
        RenderingTestElement second = new(Color.Black);
        ElementRenderCache cache = new();
        cache.Ensure(first, new RenderCounters(), forceRebuild: true);

        Assert.True(cache.IsStale(second));
        Assert.Throws<InvalidOperationException>(() => cache.GetValidCommands(second));
    }

    [Fact]
    public void RebuildStoresContentBounds()
    {
        RenderingTestElement element = new(Color.White);
        element.Arrange(new ArrangeContext(new LayoutRect(2, 3, 20, 10)));
        ElementRenderCache cache = new();

        cache.Ensure(element, new RenderCounters(), forceRebuild: true);

        Assert.Equal(new LayoutRect(2, 3, 20, 10), cache.ContentBounds);
    }

    [Fact]
    public void FailedRebuildInvalidatesPreviouslyValidCache()
    {
        RenderingTestElement first = new(Color.White);
        RenderingTestElement failing = new(Color.White, throwOnRender: true);
        ElementRenderCache cache = new();
        cache.Ensure(first, new RenderCounters(), forceRebuild: true);

        Assert.Throws<InvalidOperationException>(() => cache.Ensure(failing, new RenderCounters(), forceRebuild: true));

        Assert.False(cache.IsValid);
    }
}
