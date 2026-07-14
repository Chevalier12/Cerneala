using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Cerneala.UI.Aspect;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;

namespace Cerneala.Tests.UI.Relay;

public sealed class RelayStageZeroTests
{
    [Fact]
    public void WorkerPostRunsExactlyOnceOnOwnerThreadAndWakesIdleRoot()
    {
        UIRoot root = new();
        RelayApi relay = RelayApi.For(root);
        int ownerThreadId = Environment.CurrentManagedThreadId;
        int callbackThreadId = 0;
        int executions = 0;

        RunOffThread(() => relay.Post(() =>
        {
            callbackThreadId = Environment.CurrentManagedThreadId;
            executions++;
        }));

        Assert.True(relay.HasPendingWork);
        Assert.Equal(1, relay.PendingCount);
        Assert.Equal(0, executions);

        root.ProcessFrame();

        Assert.Equal(1, executions);
        Assert.Equal(ownerThreadId, callbackThreadId);
        Assert.False(relay.HasPendingWork);
        Assert.Equal(0, relay.PendingCount);

        root.ProcessFrame();
        Assert.Equal(1, executions);
    }

    [Fact]
    public void DrainIsFifoSnapshotBasedAndDefersRepostedWork()
    {
        UIRoot root = new();
        RelayApi relay = RelayApi.For(root);
        List<int> order = [];

        relay.Post(() =>
        {
            order.Add(1);
            relay.Post(() => order.Add(3));
        });
        relay.Post(() => order.Add(2));

        root.ProcessFrame();

        Assert.Equal([1, 2], order);
        Assert.Equal(1, relay.PendingCount);

        root.ProcessFrame();
        Assert.Equal([1, 2, 3], order);
    }

    [Fact]
    public void NumericBudgetLeavesDeterministicBacklogForLaterUpdates()
    {
        UIRoot root = RelayApi.CreateRoot(maxCallbacksPerUpdate: 2);
        RelayApi relay = RelayApi.For(root);
        List<int> order = [];
        for (int i = 0; i < 5; i++)
        {
            int value = i;
            relay.Post(() => order.Add(value));
        }

        root.ProcessFrame();
        Assert.Equal([0, 1], order);
        Assert.Equal(3, relay.PendingCount);

        root.ProcessFrame();
        Assert.Equal([0, 1, 2, 3], order);
        Assert.Equal(1, relay.PendingCount);

        root.ProcessFrame();
        Assert.Equal([0, 1, 2, 3, 4], order);
        Assert.Equal(0, relay.PendingCount);
    }

    [Fact]
    public async Task CancellationBeforeDrainPreventsCallbackExecution()
    {
        UIRoot root = new();
        RelayApi relay = RelayApi.For(root);
        using CancellationTokenSource cancellation = new();
        int executions = 0;
        Task operation = relay.InvokeAsync(() => executions++, cancellation.Token);

        cancellation.Cancel();
        root.ProcessFrame();

        Assert.Equal(0, executions);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
    }

    [Fact]
    public async Task PostFaultIsAggregatedAfterSnapshotAndInvokeFaultStaysOnTask()
    {
        UIRoot root = new();
        RelayApi relay = RelayApi.For(root);
        int completedPost = 0;
        relay.Post(() => throw new InvalidOperationException("post-fault"));
        relay.Post(() => completedPost++);

        AggregateException postFault = Assert.Throws<AggregateException>(() => root.ProcessFrame());
        Assert.Single(postFault.InnerExceptions);
        Assert.Equal(1, completedPost);

        Task invoke = relay.InvokeAsync(() => throw new InvalidOperationException("invoke-fault"));
        root.ProcessFrame();
        InvalidOperationException invokeFault = await Assert.ThrowsAsync<InvalidOperationException>(() => invoke);
        Assert.Equal("invoke-fault", invokeFault.Message);
    }

    [Fact]
    public async Task AwaitContinuationUsesOwningRootAndRestoresPreviousContext()
    {
        UIRoot root = new();
        RelayApi relay = RelayApi.For(root);
        SynchronizationContext previous = new();
        SynchronizationContext? callbackContext = null;
        int continuationThreadId = 0;
        int ownerThreadId = Environment.CurrentManagedThreadId;
        SynchronizationContext.SetSynchronizationContext(previous);
        try
        {
            Task operation = relay.InvokeAsync(async _ =>
            {
                callbackContext = SynchronizationContext.Current;
                await Task.Yield();
                continuationThreadId = Environment.CurrentManagedThreadId;
            });

            root.ProcessFrame();
            Assert.Same(previous, SynchronizationContext.Current);
            Assert.NotNull(callbackContext);
            Assert.NotSame(previous, callbackContext);
            Assert.False(operation.IsCompleted);

            root.ProcessFrame();
            await operation;
            Assert.Equal(ownerThreadId, continuationThreadId);
            Assert.Same(previous, SynchronizationContext.Current);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(null);
        }
    }

    [Fact]
    public void ExecutionContextFlowsAndTwoRootsKeepContinuationsSeparate()
    {
        UIRoot firstRoot = new();
        UIRoot secondRoot = new();
        RelayApi firstRelay = RelayApi.For(firstRoot);
        RelayApi secondRelay = RelayApi.For(secondRoot);
        AsyncLocal<string?> ambient = new();
        string? observedAmbient = null;
        string? observedCulture = null;
        List<string> order = [];

        RunOffThread(() =>
        {
            ambient.Value = "captured";
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ro-RO");
            firstRelay.Post(() =>
            {
                observedAmbient = ambient.Value;
                observedCulture = CultureInfo.CurrentCulture.Name;
            });
        });

        Task first = firstRelay.InvokeAsync(async _ =>
        {
            await Task.Yield();
            order.Add("first");
        });
        Task second = secondRelay.InvokeAsync(async _ =>
        {
            await Task.Yield();
            order.Add("second");
        });

        firstRoot.ProcessFrame();
        Assert.Equal("captured", observedAmbient);
        Assert.Equal("ro-RO", observedCulture);
        Assert.Empty(order);

        secondRoot.ProcessFrame();
        Assert.Empty(order);

        firstRoot.ProcessFrame();
        Assert.Equal(["first"], order);
        Assert.True(first.IsCompletedSuccessfully);
        Assert.False(second.IsCompleted);

        secondRoot.ProcessFrame();
        Assert.True(first.IsCompletedSuccessfully);
        Assert.True(second.IsCompletedSuccessfully);
        Assert.Equal(["first", "second"], order);
    }

    [Fact]
    public async Task AttachedPropertyMutationOffThreadThrowsBeforeChangingState()
    {
        UIRoot root = new();
        DrainScheduler(root);
        float opacity = root.Opacity;
        int treeVersion = root.TreeVersion;
        bool hadWork = root.Scheduler.HasWork;

        Exception? exception = await Record.ExceptionAsync(() => Task.Run(() => root.Opacity = 0.25f));

        Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal(opacity, root.Opacity);
        Assert.Equal(treeVersion, root.TreeVersion);
        Assert.Equal(hadWork, root.Scheduler.HasWork);
    }

    [Fact]
    public async Task AttachedTreeMutationOffThreadThrowsBeforeChangingTree()
    {
        UIRoot root = new();
        DrainScheduler(root);
        UIElement child = new();
        int visualCount = root.VisualChildren.Count;
        int treeVersion = root.TreeVersion;

        Exception? exception = await Record.ExceptionAsync(() => Task.Run(() => root.VisualChildren.Add(child)));

        Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal(visualCount, root.VisualChildren.Count);
        Assert.Equal(treeVersion, root.TreeVersion);
        Assert.Null(child.Root);
    }

    [Fact]
    public async Task MotionAndAspectUseRootRelayAuthority()
    {
        UIRoot root = new();
        RelayApi relay = RelayApi.For(root);
        AspectPackage package = AspectPackage.Create("relay-stage-zero");
        int registryVersion = root.AspectRegistry.Version;

        (bool relayAccess, Exception? motionFault, Exception? aspectFault) = await Task.Run(() =>
        {
            bool access = relay.CheckAccess();
            Exception? motion = Record.Exception(() => root.Motion.Graph.CreateValue(0f));
            Exception? aspect = Record.Exception(() => root.AspectRegistry.Register(package));
            return (access, motion, aspect);
        });

        Assert.False(relayAccess);
        Assert.IsType<InvalidOperationException>(motionFault);
        Assert.IsType<InvalidOperationException>(aspectFault);
        Assert.Equal(registryVersion, root.AspectRegistry.Version);
    }

    [Fact]
    public async Task RootOwnedAspectMutationOffThreadThrowsBeforeChangingRegistry()
    {
        UIRoot root = new();
        AspectPackage package = AspectPackage.Create("relay-stage-zero-aspect");
        int registryVersion = root.AspectRegistry.Version;

        Exception? exception = await Record.ExceptionAsync(
            () => Task.Run(() => root.AspectRegistry.Register(package)));

        Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal(registryVersion, root.AspectRegistry.Version);
        Assert.DoesNotContain(root.AspectRegistry.Packages, candidate => ReferenceEquals(candidate, package));
    }

    [Fact]
    public async Task StandaloneMotionConstructorsRemainUsableOnTheirCreatingThread()
    {
        await Task.Run(() =>
        {
            MotionGraph graph = Assert.IsType<MotionGraph>(Activator.CreateInstance(typeof(MotionGraph)));
            Assert.NotNull(graph.CreateValue(0f));

            ManualMotionTimeline timeline = new();
            Assert.NotNull(timeline.CreateValue(0f));
        });
    }

    private static void DrainScheduler(UIRoot root)
    {
        while (root.Scheduler.HasWork)
        {
            root.ProcessFrame();
        }
    }

    private static void RunOffThread(Action action)
    {
        Exception? exception = null;
        Thread thread = new(() => exception = Record.Exception(action));
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "Worker thread did not finish.");
        Assert.Null(exception);
    }

    private sealed class RelayApi
    {
        private const string RelayTypeName = "Cerneala.UI.Relay.UiRelay";
        private const string OptionsTypeName = "Cerneala.UI.Relay.UiRelayOptions";
        private readonly object instance;
        private readonly Type type;

        private RelayApi(object instance)
        {
            this.instance = instance;
            type = instance.GetType();
        }

        public bool HasPendingWork => GetProperty<bool>(nameof(HasPendingWork));

        public int PendingCount => GetProperty<int>(nameof(PendingCount));

        public static RelayApi For(UIRoot root)
        {
            PropertyInfo relayProperty = typeof(UIRoot).GetProperty("Relay")
                ?? throw new Xunit.Sdk.XunitException("UIRoot.Relay is missing.");
            object relay = relayProperty.GetValue(root)
                ?? throw new Xunit.Sdk.XunitException("UIRoot.Relay returned null.");
            Assert.Equal(RelayTypeName, relay.GetType().FullName);
            return new RelayApi(relay);
        }

        public static UIRoot CreateRoot(int maxCallbacksPerUpdate)
        {
            Assembly assembly = typeof(UIRoot).Assembly;
            Type optionsType = assembly.GetType(OptionsTypeName)
                ?? throw new Xunit.Sdk.XunitException($"{OptionsTypeName} is missing.");
            object options = Activator.CreateInstance(optionsType)
                ?? throw new Xunit.Sdk.XunitException("UiRelayOptions could not be constructed.");
            PropertyInfo maxCallbacks = optionsType.GetProperty("MaxCallbacksPerUpdate")
                ?? throw new Xunit.Sdk.XunitException("UiRelayOptions.MaxCallbacksPerUpdate is missing.");
            maxCallbacks.SetValue(options, maxCallbacksPerUpdate);

            ConstructorInfo constructor = typeof(UIRoot).GetConstructors()
                .Single(candidate => candidate.GetParameters().LastOrDefault()?.ParameterType == optionsType);
            object?[] arguments = constructor.GetParameters()
                .Select(parameter => parameter.ParameterType == optionsType
                    ? options
                    : parameter.DefaultValue is DBNull
                        ? Type.Missing
                        : parameter.DefaultValue)
                .ToArray();
            return (UIRoot)constructor.Invoke(arguments);
        }

        public bool CheckAccess()
        {
            return (bool)Invoke("CheckAccess")!;
        }

        public void Post(Action callback)
        {
            _ = Invoke("Post", callback);
        }

        public Task InvokeAsync(Action callback, CancellationToken cancellationToken = default)
        {
            return (Task)InvokeOverload("InvokeAsync", [typeof(Action), typeof(CancellationToken)], callback, cancellationToken)!;
        }

        public Task InvokeAsync(Func<CancellationToken, Task> callback, CancellationToken cancellationToken = default)
        {
            return (Task)InvokeOverload(
                "InvokeAsync",
                [typeof(Func<CancellationToken, Task>), typeof(CancellationToken)],
                callback,
                cancellationToken)!;
        }

        private T GetProperty<T>(string name)
        {
            PropertyInfo property = type.GetProperty(name)
                ?? throw new Xunit.Sdk.XunitException($"{RelayTypeName}.{name} is missing.");
            return Assert.IsType<T>(property.GetValue(instance));
        }

        private object? Invoke(string name, params object?[] arguments)
        {
            MethodInfo method = type.GetMethod(name, arguments.Select(argument => argument?.GetType() ?? typeof(object)).ToArray())
                ?? type.GetMethods().SingleOrDefault(candidate => candidate.Name == name && candidate.GetParameters().Length == arguments.Length)
                ?? throw new Xunit.Sdk.XunitException($"{RelayTypeName}.{name} is missing.");
            return InvokeMethod(method, arguments);
        }

        private object? InvokeOverload(string name, Type[] parameterTypes, params object?[] arguments)
        {
            MethodInfo method = type.GetMethod(name, parameterTypes)
                ?? throw new Xunit.Sdk.XunitException($"{RelayTypeName}.{name} overload is missing.");
            return InvokeMethod(method, arguments);
        }

        private object? InvokeMethod(MethodInfo method, object?[] arguments)
        {
            try
            {
                return method.Invoke(instance, arguments);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                Exception inner = exception.InnerException;
                ExceptionDispatchInfo.Capture(inner).Throw();
                throw;
            }
        }
    }
}
