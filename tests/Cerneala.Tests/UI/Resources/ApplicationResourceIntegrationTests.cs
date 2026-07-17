using Cerneala.Drawing;
using Cerneala.UI;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Markup;
using Cerneala.UI.Media;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.UI.Resources;

public sealed class ApplicationResourceIntegrationTests
{
    [Fact]
    public void GlobalResourceUpdatesAllRealConsumersAndReturnsToIdle()
    {
        Application application = new();
        SolidColorBrush initial = new(new Color(10, 20, 30));
        application.Resources.SetResource(new ResourceId<Brush>("Accent"), initial);

        UIRoot firstRoot = CreateRoot(application);
        UIRoot secondRoot = CreateRoot(application);
        Border first = CreateConsumer(firstRoot);
        Border second = CreateConsumer(secondRoot);
        Border unaffected = new();
        firstRoot.VisualChildren.Add(unaffected);
        firstRoot.ProcessFrame();
        secondRoot.ProcessFrame();

        SolidColorBrush updated = new(new Color(40, 50, 60));
        application.Resources.SetResource(new ResourceId<Brush>("Accent"), updated);

        Assert.Same(updated, first.Background);
        Assert.Same(updated, second.Background);
        Assert.False(unaffected.DirtyState.IsDirty);
        Assert.Contains(first, firstRoot.ResourceDependencyTracker.GetDependents(new ResourceId<Brush>("Accent")));
        Assert.Contains(second, secondRoot.ResourceDependencyTracker.GetDependents(new ResourceId<Brush>("Accent")));

        FrameStats firstUpdate = firstRoot.ProcessFrame();
        FrameStats secondUpdate = secondRoot.ProcessFrame();
        Assert.True(firstUpdate.RenderedElements > 0);
        Assert.True(secondUpdate.RenderedElements > 0);

        FrameStats firstIdle = firstRoot.ProcessFrame();
        FrameStats secondIdle = secondRoot.ProcessFrame();
        Assert.False(firstIdle.HasWork);
        Assert.False(secondIdle.HasWork);
    }

    [Fact]
    public void LocalResourceShadowsApplicationAndLaterRootsSeeLatestValue()
    {
        Application application = new();
        application.Resources.SetResource(
            new ResourceId<Brush>("Accent"),
            new SolidColorBrush(new Color(1, 2, 3)));
        UIRoot firstRoot = CreateRoot(application);
        Border shadowed = new();
        SolidColorBrush local = new(new Color(9, 8, 7));
        shadowed.Resources["Accent"] = local;
        GeneratedMarkup.AttachResource(
            shadowed,
            shadowed,
            Control.BackgroundProperty,
            "Accent",
            UiPropertyValueSource.MarkupBase);
        firstRoot.VisualChildren.Add(shadowed);

        SolidColorBrush latest = new(new Color(7, 8, 9));
        application.Resources.SetResource(new ResourceId<Brush>("Accent"), latest);

        Assert.Same(local, shadowed.Background);
        Assert.DoesNotContain(
            shadowed,
            firstRoot.ResourceDependencyTracker.GetDependents(new ResourceId<Brush>("Accent")));

        UIRoot laterRoot = CreateRoot(application);
        Border later = CreateConsumer(laterRoot);
        Assert.Same(latest, later.Background);
    }

    private static UIRoot CreateRoot(Application application)
    {
        UIRoot root = new(100, 100);
        root.SetResourceProvider(application.Resources);
        return root;
    }

    private static Border CreateConsumer(UIRoot root)
    {
        Border border = new();
        GeneratedMarkup.AttachResource(
            border,
            border,
            Control.BackgroundProperty,
            "Accent",
            UiPropertyValueSource.MarkupBase);
        root.VisualChildren.Add(border);
        return border;
    }
}
