using Cerneala.UI.Aspect;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Motion.Core;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.UI.Relay;

public sealed class UiThreadAffinityTests
{
    [Fact]
    public void LegacyMotionGuardSurfaceIsAbsent()
    {
        string legacyTypeName = "Cerneala.UI.Motion.Core.Motion" + "ThreadGuard";
        Assert.Null(typeof(MotionSystem).Assembly.GetType(legacyTypeName));
        Assert.Null(typeof(MotionSystem).GetProperty("ThreadGuard"));
        Assert.DoesNotContain(
            typeof(MotionGraph).GetConstructors(),
            constructor => constructor.GetParameters().Any(parameter => parameter.ParameterType.FullName == legacyTypeName));
    }

    [Fact]
    public void AttachedPropertySetAndClearFailBeforeAnyStateChanges()
    {
        UIRoot root = new();
        root.Opacity = 0.75f;
        DrainScheduler(root);
        UiPropertyValueSource source = root.GetValueSource(UIElement.OpacityProperty);
        InvalidationFlags dirtyFlags = root.DirtyState.Flags;
        long dirtyVersion = root.DirtyState.Version;
        int treeVersion = root.TreeVersion;
        bool schedulerHasWork = root.Scheduler.HasWork;

        Exception? setFault = RunOffThread(() => root.Opacity = 0.25f);
        Exception? clearFault = RunOffThread(() => root.ClearValue(UIElement.OpacityProperty));

        Assert.IsType<InvalidOperationException>(setFault);
        Assert.IsType<InvalidOperationException>(clearFault);
        Assert.Equal(0.75f, root.Opacity);
        Assert.Equal(source, root.GetValueSource(UIElement.OpacityProperty));
        Assert.Equal(dirtyFlags, root.DirtyState.Flags);
        Assert.Equal(dirtyVersion, root.DirtyState.Version);
        Assert.Equal(treeVersion, root.TreeVersion);
        Assert.Equal(schedulerHasWork, root.Scheduler.HasWork);
    }

    [Fact]
    public void DetachedElementCanBeConfiguredOnWorkerThenAttachedOnOwner()
    {
        UIRoot root = new();
        UIElement child = new();

        Exception? fault = RunOffThread(() => child.Opacity = 0.4f);
        root.VisualChildren.Add(child);

        Assert.Null(fault);
        Assert.Equal(0.4f, child.Opacity);
        Assert.Equal(UiPropertyValueSource.Local, child.GetValueSource(UIElement.OpacityProperty));
        Assert.Same(root, child.Root);
    }

    [Fact]
    public void TreeMutationAndLifecycleFailBeforeAttachOrDetach()
    {
        UIRoot root = new();
        UIElement child = new();
        int treeVersion = root.TreeVersion;

        Exception? addFault = RunOffThread(() => root.VisualChildren.Add(child));
        Exception? attachFault = RunOffThread(() => ElementLifecycle.AttachSubtree(root, child));

        Assert.IsType<InvalidOperationException>(addFault);
        Assert.IsType<InvalidOperationException>(attachFault);
        Assert.Empty(root.VisualChildren);
        Assert.Null(child.Root);
        Assert.Equal(treeVersion, root.TreeVersion);

        root.VisualChildren.Add(child);
        int attachedVersion = root.TreeVersion;
        Exception? removeFault = RunOffThread(() => root.VisualChildren.Remove(child));
        Exception? detachFault = RunOffThread(() => ElementLifecycle.DetachSubtree(root, child));

        Assert.IsType<InvalidOperationException>(removeFault);
        Assert.IsType<InvalidOperationException>(detachFault);
        Assert.Same(child, Assert.Single(root.VisualChildren));
        Assert.Same(root, child.Root);
        Assert.Equal(attachedVersion, root.TreeVersion);
    }

    [Fact]
    public void RootMutationMethodsShareRelayAuthority()
    {
        UIRoot root = new(100, 50, 1);
        DrainScheduler(root);
        int treeVersion = root.TreeVersion;
        int viewportVersion = root.ViewportVersion;
        InvalidationFlags dirtyFlags = root.DirtyState.Flags;
        bool schedulerHasWork = root.Scheduler.HasWork;

        Exception?[] faults =
        [
            RunOffThread(() => root.SetResourceProvider(null)),
            RunOffThread(() => root.SetPlatformServices(null)),
            RunOffThread(() => root.SetImageLoader(null)),
            RunOffThread(() => root.SetImageResourceCache(null, null)),
            RunOffThread(() => root.SetViewport(200, 100, 2)),
            RunOffThread(() => root.SetThemeProvider(null)),
            RunOffThread(() => root.Invalidate(InvalidationFlags.Render, "worker"))
        ];

        Assert.All(faults, fault => Assert.IsType<InvalidOperationException>(fault));
        Assert.Equal(100, root.ViewportWidth);
        Assert.Equal(50, root.ViewportHeight);
        Assert.Equal(1, root.Scale);
        Assert.Equal(treeVersion, root.TreeVersion);
        Assert.Equal(viewportVersion, root.ViewportVersion);
        Assert.Equal(dirtyFlags, root.DirtyState.Flags);
        Assert.Equal(schedulerHasWork, root.Scheduler.HasWork);
    }

    [Fact]
    public void RootAndStandaloneMotionCaptureTheirCreatingThread()
    {
        UIRoot root = new();
        MotionGraph standalone = new();
        ManualMotionTimeline timeline = new();

        Assert.NotNull(root.Motion.Graph.CreateValue(0f));
        Assert.NotNull(standalone.CreateValue(0f));
        Assert.NotNull(timeline.CreateValue(0f));

        Assert.IsType<InvalidOperationException>(RunOffThread(() => root.Motion.Graph.CreateValue(1f)));
        Assert.IsType<InvalidOperationException>(RunOffThread(
            () => root.Motion.BeginTransaction(MotionFactory.Tween(TimeSpan.FromMilliseconds(100)))));
        Assert.IsType<InvalidOperationException>(RunOffThread(() => root.Motion.Tick()));
        Assert.IsType<InvalidOperationException>(RunOffThread(() => root.Motion.MaxDelta = TimeSpan.FromSeconds(1)));
        Assert.IsType<InvalidOperationException>(RunOffThread(() => standalone.CreateValue(1f)));
        Assert.IsType<InvalidOperationException>(RunOffThread(() => timeline.CreateValue(1f)));
        Assert.IsType<InvalidOperationException>(RunOffThread(() => timeline.SetProgress(0.5f)));
        Assert.Equal(0, timeline.Progress);
    }

    [Fact]
    public void AspectStandaloneMutatorsCaptureOwnerAndPreserveStateOnFailure()
    {
        AspectRegistry registry = new();
        AspectPackage package = AspectPackage.Create("affinity");
        registry.Register(package);
        Assert.True(registry.Unregister(package.Name));
        int registryVersion = registry.Version;

        AspectToken<int> token = AspectToken.Create<int>("affinity.value");
        AspectEnvironment environment = new("test");
        environment.Set(token, 1);
        int environmentVersion = environment.Version;

        AspectEngine engine = new();
        AspectCatalog catalog = CreateOpacityCatalog();
        UIElement applied = new();
        Assert.True(engine.Apply(applied, catalog, environment).Applied);
        Assert.Equal(0.5f, applied.Opacity);

        Assert.IsType<InvalidOperationException>(RunOffThread(() => registry.Register(package)));
        Assert.IsType<InvalidOperationException>(RunOffThread(() => registry.Unregister(package.Name)));
        Assert.IsType<InvalidOperationException>(RunOffThread(() => environment.Set(token, 2)));
        Assert.IsType<InvalidOperationException>(RunOffThread(() => engine.Apply(new UIElement(), catalog, environment)));
        Assert.IsType<InvalidOperationException>(RunOffThread(() => engine.Clear(applied)));

        Assert.Equal(registryVersion, registry.Version);
        Assert.Empty(registry.Packages);
        Assert.Equal(environmentVersion, environment.Version);
        Assert.True(environment.TryGet(token, out int value));
        Assert.Equal(1, value);
        Assert.Equal(0.5f, applied.Opacity);
        Assert.Equal(UiPropertyValueSource.AspectBase, applied.GetValueSource(UIElement.OpacityProperty));
    }

    [Fact]
    public void AspectInvalidationAndProcessorRejectWorkerMutation()
    {
        AspectEnvironment environment = new("test");
        AspectEngine engine = new();
        using AspectInvalidation invalidation = new(engine, CreateOpacityCatalog(), environment);
        UIElement tracked = new();

        Assert.True(invalidation.Track(tracked).Applied);
        Assert.False(invalidation.Recompute(tracked).Applied);
        Assert.IsType<InvalidOperationException>(RunOffThread(() => invalidation.Recompute(tracked)));
        Assert.IsType<InvalidOperationException>(RunOffThread(() => invalidation.Untrack(tracked)));
        UIElement untracked = new();
        Assert.IsType<InvalidOperationException>(RunOffThread(() => invalidation.Track(untracked)));
        Assert.False(invalidation.Untrack(untracked));
        Assert.True(invalidation.Untrack(tracked));

        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        root.AspectProcessor.Process(child);
        UiPropertyValueSource source = child.GetValueSource(UIElement.OpacityProperty);

        Assert.IsType<InvalidOperationException>(RunOffThread(() => root.AspectProcessor.Process(child)));
        Assert.IsType<InvalidOperationException>(RunOffThread(() => root.AspectProcessor.Clear(child)));
        Assert.Equal(source, child.GetValueSource(UIElement.OpacityProperty));
    }

    private static AspectCatalog CreateOpacityCatalog()
    {
        AspectDeclaration declaration = new(
            UIElement.OpacityProperty,
            AspectValue<float>.Literal(0.5f));
        AspectRuleSet rule = new(
            "opacity",
            AspectLayer.App,
            new AspectTarget(typeof(UIElement)),
            [declaration],
            0);
        AspectPackage package = AspectPackage.Create("affinity.catalog")
            .Components(components => components.AddRule(rule));
        return new AspectRegistry().Register(package).BuildCatalog();
    }

    private static Exception? RunOffThread(Action action)
    {
        Exception? exception = null;
        Thread thread = new(() => exception = Record.Exception(action));
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "Worker thread did not finish.");
        return exception;
    }

    private static void DrainScheduler(UIRoot root)
    {
        while (root.Scheduler.HasWork)
        {
            root.ProcessFrame();
        }
    }
}
