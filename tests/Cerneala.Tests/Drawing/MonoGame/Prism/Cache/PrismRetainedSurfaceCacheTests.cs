using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame.Prism.Surfaces;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windows;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Prism.Runtime;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Tests.Drawing.MonoGame.Prism.Cache;

public sealed class PrismRetainedSurfaceCacheTests
{
    private static readonly PrismSurfaceKey DefaultSurfaceKey =
        new(
            8,
            8,
            SurfaceFormat.Color,
            0,
            PrismColorProfile.Srgb);

    [Fact]
    public void PromotionTransfersOneOwnerAndExactByteAccounting()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        PrismGraphExecutionPlan plan = CreatePlan();
        long bytes = DefaultSurfaceKey.CalculateByteSize();
        PrismSurfaceBudget budget = Budget(
            plan,
            retainedSurfaces: 2,
            retainedEntries: 2);
        using PrismSurfacePool pool = new(
            fixture.Session.GraphicsDevice,
            budget);
        using PrismRetainedSurfaceCache cache = new(pool);
        PrismRetainedCacheKey key = FinalKey(plan, shaderVersion: 1);
        PrismSurfaceKey[] keys = CreateKeys(
            plan,
            DefaultSurfaceKey);
        int finalIndex = FinalIndex(plan);
        RenderTarget2D target;

        using (PrismSurfaceFrame frame = pool.BeginFrame(plan))
        {
            AdvanceAll(frame, plan, keys);
            target = frame.GetSurface(finalIndex);
            long transientBefore = pool.TransientByteCount;
            long totalBefore = pool.TotalByteCount;

            Assert.True(
                cache.TryPromote(
                    key,
                    frame,
                    finalIndex));
            Assert.Equal(
                transientBefore - bytes,
                pool.TransientByteCount);
            Assert.Equal(bytes, cache.RetainedByteCount);
            Assert.Equal(totalBefore, pool.TotalByteCount);
            Assert.Equal(1, cache.EntryCount);
        }

        Assert.Equal(0, pool.ActiveLeaseCount);
        Assert.Throws<InvalidOperationException>(
            (Action)(() =>
            {
                using PrismRetainedSurfaceLease lease =
                    Acquire(cache, key);
                Assert.Same(target, lease.Surface);
                Assert.Equal(1, cache.ActiveLeaseCount);
                throw new InvalidOperationException(
                    "Injected cached draw failure.");
            }));
        Assert.Equal(0, cache.ActiveLeaseCount);
        Assert.Equal(0, cache.PinnedEntryCount);

        cache.Clear();

        Assert.Equal(0, cache.EntryCount);
        Assert.Equal(0, cache.RetainedByteCount);
        Assert.True(target.IsDisposed);
        Assert.True(pool.TotalByteCount <= budget.HardByteLimit);
        Assert.Equal(
            PrismCacheEvictionReason.ExplicitRemoval,
            cache.LastEvictionReason);
        Assert.Equal(
            1,
            cache.GetEvictionCount(
                PrismCacheEvictionReason.ExplicitRemoval));
    }

    [Fact]
    public void PinnedEntryCannotBeEvictedOrDisposed()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        PrismGraphExecutionPlan plan = CreatePlan();
        PrismSurfaceBudget budget = Budget(
            plan,
            retainedSurfaces: 1,
            retainedEntries: 1);
        using PrismSurfacePool pool = new(
            fixture.Session.GraphicsDevice,
            budget);
        using PrismRetainedSurfaceCache cache = new(pool);
        PrismRetainedCacheKey firstKey =
            FinalKey(plan, shaderVersion: 11);
        PrismRetainedCacheKey secondKey =
            FinalKey(plan, shaderVersion: 12);
        RenderTarget2D firstTarget = Promote(
            pool,
            cache,
            plan,
            firstKey,
            DefaultSurfaceKey,
            expected: true);
        PrismRetainedSurfaceLease lease =
            Acquire(cache, firstKey);

        _ = Promote(
            pool,
            cache,
            plan,
            secondKey,
            DefaultSurfaceKey,
            expected: false);

        Assert.True(cache.Contains(firstKey));
        Assert.False(cache.Contains(secondKey));
        Assert.False(firstTarget.IsDisposed);
        Assert.Equal(1, cache.PinnedEntryCount);
        Assert.Equal(1, cache.ActiveLeaseCount);

        lease.Dispose();
        _ = Promote(
            pool,
            cache,
            plan,
            secondKey,
            DefaultSurfaceKey,
            expected: true);

        Assert.False(cache.Contains(firstKey));
        Assert.True(cache.Contains(secondKey));
        Assert.Equal(0, cache.PinnedEntryCount);
        Assert.Equal(0, cache.ActiveLeaseCount);
    }

    [Fact]
    public void OwnerInvalidationVisitsOnlyThatOwnersEntriesAndDefersPinnedRelease()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        PrismGraphExecutionPlan plan = CreatePlan();
        using PrismSurfacePool pool = new(
            fixture.Session.GraphicsDevice,
            Budget(
                plan,
                retainedSurfaces: 2,
                retainedEntries: 2));
        using PrismRetainedSurfaceCache cache = new(pool);
        PrismRetainedCacheKey firstKey = WithOwner(
            FinalKey(plan, shaderVersion: 14),
            ownerToken: 51_001);
        PrismRetainedCacheKey secondKey = WithOwner(
            FinalKey(plan, shaderVersion: 15),
            ownerToken: 51_002);
        RenderTarget2D firstTarget = Promote(
            pool,
            cache,
            plan,
            firstKey,
            DefaultSurfaceKey,
            expected: true);
        _ = Promote(
            pool,
            cache,
            plan,
            secondKey,
            DefaultSurfaceKey,
            expected: true);
        PrismRetainedSurfaceLease firstLease =
            Acquire(cache, firstKey);

        Assert.Equal(2, cache.OwnerIndexCount);
        Assert.Equal(1, cache.LookupCount);
        Assert.Equal(
            1,
            cache.RemoveOwner(
                new PrismCacheOwnerToken(51_001)));

        Assert.Equal(
            1,
            cache.LastOwnerInvalidationVisitCount);
        Assert.Equal(1, cache.OwnerIndexCount);
        Assert.False(cache.Contains(firstKey));
        Assert.True(cache.Contains(secondKey));
        Assert.False(firstTarget.IsDisposed);
        Assert.Equal(1, cache.ActiveLeaseCount);

        firstLease.Dispose();

        Assert.True(firstTarget.IsDisposed);
        Assert.Equal(0, cache.ActiveLeaseCount);
        Assert.Equal(
            1,
            cache.RemoveOwner(
                new PrismCacheOwnerToken(51_002)));
        Assert.Equal(0, cache.EntryCount);
        Assert.Equal(0, cache.OwnerIndexCount);
        Assert.Equal(0, cache.RetainedByteCount);
        Assert.Equal(
            2,
            cache.GetEvictionCount(
                PrismCacheEvictionReason.Invalidation));
    }

    [Fact]
    public void RetainedEntryMetadataContainsNoUiOrLifecycleObjects()
    {
        Type entryType =
            typeof(PrismRetainedSurfaceCache).GetNestedType(
                "CacheEntry",
                BindingFlags.NonPublic) ??
            throw new InvalidOperationException(
                "Could not inspect the retained cache entry type.");
        Type[] forbidden =
        [
            typeof(UIElement),
            typeof(Cerneala.UI.Data.Binding),
            typeof(Delegate),
            typeof(MotionHandle),
            typeof(IBackdropFrameLease),
            typeof(PrismInstance)
        ];

        foreach (FieldInfo field in entryType.GetFields(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic))
        {
            Assert.DoesNotContain(
                forbidden,
                type =>
                    type == field.FieldType ||
                    type.IsAssignableFrom(field.FieldType));
        }
    }

    [Fact]
    public void RejectedPromotionPreservesTransientFrameOwnership()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        PrismGraphExecutionPlan plan = CreatePlan();
        long bytes = DefaultSurfaceKey.CalculateByteSize();
        using PrismSurfacePool pool = new(
            fixture.Session.GraphicsDevice,
            new PrismSurfaceBudget(
                checked(plan.PeakLiveSurfaces * bytes),
                retainedSoftByteLimit: 0,
                retainedEntryLimit: 0));
        using PrismRetainedSurfaceCache cache = new(pool);
        PrismSurfaceKey[] keys = CreateKeys(
            plan,
            DefaultSurfaceKey);
        int finalIndex = FinalIndex(plan);

        using PrismSurfaceFrame frame = pool.BeginFrame(plan);
        AdvanceAll(frame, plan, keys);
        RenderTarget2D target =
            frame.GetSurface(finalIndex);
        long transientBefore = pool.TransientByteCount;

        Assert.False(
            cache.TryPromote(
                FinalKey(plan, shaderVersion: 13),
                frame,
                finalIndex));
        Assert.Same(target, frame.GetSurface(finalIndex));
        Assert.Equal(transientBefore, pool.TransientByteCount);
        Assert.Equal(0, cache.RetainedByteCount);
        Assert.Equal(0, cache.EntryCount);
    }

    [Fact]
    public void EntryAndByteBudgetsEvictDeterministicLeastRecentlyUsed()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        PrismGraphExecutionPlan plan = CreatePlan();
        PrismSurfaceBudget budget = Budget(
            plan,
            retainedSurfaces: 2,
            retainedEntries: 2);
        using PrismSurfacePool pool = new(
            fixture.Session.GraphicsDevice,
            budget);
        using PrismRetainedSurfaceCache cache = new(pool);
        PrismRetainedCacheKey first =
            FinalKey(plan, shaderVersion: 21);
        PrismRetainedCacheKey second =
            FinalKey(plan, shaderVersion: 22);
        PrismRetainedCacheKey third =
            FinalKey(plan, shaderVersion: 23);

        _ = Promote(
            pool,
            cache,
            plan,
            first,
            DefaultSurfaceKey,
            expected: true);
        _ = Promote(
            pool,
            cache,
            plan,
            second,
            DefaultSurfaceKey,
            expected: true);
        using (PrismRetainedSurfaceLease lease =
            Acquire(cache, first))
        {
            Assert.NotNull(lease.Surface);
        }

        _ = Promote(
            pool,
            cache,
            plan,
            third,
            DefaultSurfaceKey,
            expected: true);

        Assert.True(cache.Contains(first));
        Assert.False(cache.Contains(second));
        Assert.True(cache.Contains(third));
        Assert.Equal(2, cache.EntryCount);
        Assert.Equal(
            2 * DefaultSurfaceKey.CalculateByteSize(),
            cache.RetainedByteCount);
        Assert.Equal(1, cache.EvictionCount);
        Assert.Equal(
            PrismCacheEvictionReason.Capacity,
            cache.LastEvictionReason);
        Assert.Equal(
            1,
            cache.GetEvictionCount(
                PrismCacheEvictionReason.Capacity));
    }

    [Fact]
    public void TransientPressureEvictsRetainedBeforeHardCapFailure()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        PrismGraphExecutionPlan plan = CreatePlan();
        long bytes = DefaultSurfaceKey.CalculateByteSize();
        PrismSurfaceBudget budget = new(
            checked(plan.PeakLiveSurfaces * bytes),
            bytes,
            retainedEntryLimit: 1);
        using PrismSurfacePool pool = new(
            fixture.Session.GraphicsDevice,
            budget);
        using PrismRetainedSurfaceCache cache = new(pool);
        PrismRetainedCacheKey retainedKey =
            FinalKey(plan, shaderVersion: 31);

        _ = Promote(
            pool,
            cache,
            plan,
            retainedKey,
            DefaultSurfaceKey,
            expected: true);
        Assert.True(cache.Contains(retainedKey));

        PrismSurfaceKey incompatible = new(
            DefaultSurfaceKey.Width,
            DefaultSurfaceKey.Height,
            SurfaceFormat.Bgra32,
            DefaultSurfaceKey.MultiSampleCount,
            DefaultSurfaceKey.ColorProfile);
        ExecuteFrame(
            pool,
            plan,
            CreateKeys(plan, incompatible));

        Assert.False(cache.Contains(retainedKey));
        Assert.True(cache.EvictionCount > 0);
        Assert.Equal(
            PrismCacheEvictionReason.TransientPressure,
            cache.LastEvictionReason);
        Assert.Equal(
            1,
            cache.GetEvictionCount(
                PrismCacheEvictionReason.TransientPressure));
        Assert.Equal(0, pool.ActiveLeaseCount);
        Assert.True(pool.TotalByteCount <= budget.HardByteLimit);
    }

    [Fact]
    public void HardCapFailureUsesFallbackAndLeavesNoPartialOwnership()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        PrismGraphExecutionPlan plan = CreatePlan();
        long bytes = DefaultSurfaceKey.CalculateByteSize();
        PrismSurfaceBudget budget = new(
            bytes - 1,
            retainedSoftByteLimit: 0,
            retainedEntryLimit: 0);
        using PrismSurfacePool pool = new(
            fixture.Session.GraphicsDevice,
            budget);
        using PrismRetainedSurfaceCache cache = new(pool);
        PrismSurfaceKey[] keys = CreateKeys(
            plan,
            DefaultSurfaceKey);

        using (PrismSurfaceFrame frame = pool.BeginFrame(plan))
        {
            Assert.Throws<PrismSurfaceAllocationException>(
                () => frame.AdvanceToStep(0, keys));
        }

        Assert.Equal(
            PrismFallbackAction.BypassComposition,
            PrismFallbackPolicy.Resolve(
                PrismFallbackReason.SurfaceAllocationFailed));
        Assert.Equal(0, pool.ActiveLeaseCount);
        Assert.Equal(0, pool.TransientByteCount);
        Assert.Equal(0, cache.RetainedByteCount);
        Assert.Equal(0, cache.EntryCount);
    }

    [Fact]
    public void DisposeDefersPinnedSurfaceUntilLeaseRelease()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        PrismGraphExecutionPlan plan = CreatePlan();
        PrismSurfaceBudget budget = Budget(
            plan,
            retainedSurfaces: 1,
            retainedEntries: 1);
        using PrismSurfacePool pool = new(
            fixture.Session.GraphicsDevice,
            budget);
        using PrismRetainedSurfaceCache cache = new(pool);
        PrismRetainedCacheKey key =
            FinalKey(plan, shaderVersion: 41);
        RenderTarget2D target = Promote(
            pool,
            cache,
            plan,
            key,
            DefaultSurfaceKey,
            expected: true);
        PrismRetainedSurfaceLease lease = Acquire(cache, key);

        cache.Dispose();

        Assert.False(target.IsDisposed);
        Assert.Same(target, lease.Surface);
        Assert.Equal(1, cache.EntryCount);
        Assert.Equal(1, cache.ActiveLeaseCount);

        lease.Dispose();

        Assert.True(target.IsDisposed);
        Assert.Equal(0, cache.EntryCount);
        Assert.Equal(0, cache.ActiveLeaseCount);
        Assert.Equal(0, cache.RetainedByteCount);
        Assert.Equal(
            PrismCacheEvictionReason.Disposal,
            cache.LastEvictionReason);
    }

    [Fact]
    public void ReplacementAndDeviceResetReportReasonedEvictions()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        PrismGraphExecutionPlan plan = CreatePlan();
        using PrismSurfacePool pool = new(
            fixture.Session.GraphicsDevice,
            Budget(
                plan,
                retainedSurfaces: 2,
                retainedEntries: 2));
        using PrismRetainedSurfaceCache cache = new(pool);
        PrismRetainedCacheKey key =
            FinalKey(plan, shaderVersion: 42);

        _ = Promote(
            pool,
            cache,
            plan,
            key,
            DefaultSurfaceKey,
            expected: true);
        _ = Promote(
            pool,
            cache,
            plan,
            key,
            DefaultSurfaceKey,
            expected: true);

        Assert.Equal(
            1,
            cache.GetEvictionCount(
                PrismCacheEvictionReason.Replacement));

        pool.Reset();

        Assert.Equal(0, cache.EntryCount);
        Assert.Equal(
            PrismCacheEvictionReason.DeviceReset,
            cache.LastEvictionReason);
        Assert.Equal(
            1,
            cache.GetEvictionCount(
                PrismCacheEvictionReason.DeviceReset));
    }

    [Fact]
    public void CacheRejectsCrossThreadMutationInsteadOfAddingLocks()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        PrismGraphExecutionPlan plan = CreatePlan();
        using PrismSurfacePool pool = new(
            fixture.Session.GraphicsDevice,
            Budget(
                plan,
                retainedSurfaces: 1,
                retainedEntries: 1));
        using PrismRetainedSurfaceCache cache = new(pool);
        Exception? failure = null;
        Thread thread = new(
            () =>
            {
                try
                {
                    cache.Clear();
                }
                catch (Exception exception)
                {
                    failure = exception;
                }
            });

        thread.Start();
        thread.Join();

        Assert.IsType<InvalidOperationException>(failure);
        Assert.Equal(0, cache.EntryCount);
    }

    private static PrismSurfaceBudget Budget(
        PrismGraphExecutionPlan plan,
        int retainedSurfaces,
        int retainedEntries)
    {
        long bytes = DefaultSurfaceKey.CalculateByteSize();
        return new PrismSurfaceBudget(
            checked(
                (plan.PeakLiveSurfaces +
                    retainedSurfaces +
                    2L) *
                bytes),
            checked(retainedSurfaces * bytes),
            retainedEntries);
    }

    private static RenderTarget2D Promote(
        PrismSurfacePool pool,
        PrismRetainedSurfaceCache cache,
        PrismGraphExecutionPlan plan,
        PrismRetainedCacheKey key,
        PrismSurfaceKey surfaceKey,
        bool expected)
    {
        PrismSurfaceKey[] keys =
            CreateKeys(plan, surfaceKey);
        int finalIndex = FinalIndex(plan);
        using PrismSurfaceFrame frame =
            pool.BeginFrame(plan);
        AdvanceAll(frame, plan, keys);
        RenderTarget2D target =
            frame.GetSurface(finalIndex);
        Assert.Equal(
            expected,
            cache.TryPromote(
                key,
                frame,
                finalIndex));
        return target;
    }

    private static PrismRetainedSurfaceLease Acquire(
        PrismRetainedSurfaceCache cache,
        PrismRetainedCacheKey key)
    {
        Assert.True(
            cache.TryAcquire(
                key,
                out PrismRetainedSurfaceLease? lease));
        return Assert.IsType<PrismRetainedSurfaceLease>(lease);
    }

    private static void ExecuteFrame(
        PrismSurfacePool pool,
        PrismGraphExecutionPlan plan,
        PrismSurfaceKey[] keys)
    {
        using PrismSurfaceFrame frame =
            pool.BeginFrame(plan);
        AdvanceAll(frame, plan, keys);
    }

    private static void AdvanceAll(
        PrismSurfaceFrame frame,
        PrismGraphExecutionPlan plan,
        PrismSurfaceKey[] keys)
    {
        for (int step = 0;
            step < plan.ExecutionOrder.Length;
            step++)
        {
            frame.AdvanceToStep(step, keys);
            _ = frame.GetSurface(step);
        }
    }

    private static PrismSurfaceKey[] CreateKeys(
        PrismGraphExecutionPlan plan,
        PrismSurfaceKey key)
    {
        PrismSurfaceKey[] keys =
            new PrismSurfaceKey[plan.ExecutionOrder.Length];
        Array.Fill(keys, key);
        return keys;
    }

    private static int FinalIndex(
        PrismGraphExecutionPlan plan)
    {
        PrismGraphScope scope =
            Assert.Single(plan.OptimizedGraph.Scopes);
        PrismGraphNodeId output =
            Assert.IsType<PrismGraphNodeId>(scope.Output);
        int index = plan.ExecutionOrder.IndexOf(output);
        Assert.True(index >= 0);
        return index;
    }

    private static PrismRetainedCacheKey FinalKey(
        PrismGraphExecutionPlan plan,
        long shaderVersion)
    {
        int finalIndex = FinalIndex(plan);
        PrismRetainedRasterContext context = new(
            DefaultSurfaceKey.Width,
            DefaultSurfaceKey.Height,
            PrismColorProfile.Srgb,
            BackdropPixelFormat.Rgba8Unorm,
            PrismSampling.Linear,
            PrismGraphCapabilities.ControlCapture |
            PrismGraphCapabilities.ColorConversion,
            shaderVersion);
        Assert.True(
            PrismRetainedCacheKey.TryCreate(
                plan,
                plan.ExecutionOrder[finalIndex],
                context,
                out PrismRetainedCacheKey key));
        return key;
    }

    private static PrismRetainedCacheKey WithOwner(
        PrismRetainedCacheKey key,
        long ownerToken)
    {
        PrismCacheOwnerToken owner = new(ownerToken);
        PrismGraphNodeId node = key.StableNodeId;
        return key with
        {
            StableNodeId = new PrismGraphNodeId(
                owner,
                node.DefinitionNodeId,
                node.Kind,
                node.Ordinal),
            DependencyStamp = key.DependencyStamp with
            {
                CacheOwnerToken = owner
            }
        };
    }

    private static PrismGraphExecutionPlan CreatePlan()
    {
        PrismCompositionDefinition definition =
            PrismTestData.Composition(
                "Retained surface cache",
                PrismTestData.Layer(1, "Layer"));
        PrismDrawScope scope =
            PrismTestData.Scope(definition);
        DrawCommandList commands =
            PrismTestData.Commands(
                DrawCommand.BeginPrism(scope),
                DrawCommand.FillRectangle(
                    new DrawRect(0, 0, 5, 5),
                    Color.White),
                DrawCommand.EndPrism());
        PrismGraph graph =
            new PrismGraphBuilder().Build(
                new PrismFrameAnalyzer().Analyze(
                    commands));
        return new PrismGraphOptimizer().Optimize(graph);
    }

    private sealed class WindowsDxFixture : IDisposable
    {
        private readonly Win32WindowPlatform platform = new();
        private readonly IPlatformWindow window;

        public WindowsDxFixture()
        {
            window = platform.CreateWindow(
                new Window
                {
                    Title =
                        $"Cerneala retained cache {Guid.NewGuid():N}",
                    Width = 96,
                    Height = 64
                },
                new CallbackSink());
            window.Show();
            platform.PumpEvents();
            Session =
                Assert.IsType<WindowsDxWindowGraphicsSession>(
                    window.GraphicsSession);
        }

        public WindowsDxWindowGraphicsSession Session { get; }

        public void Dispose()
        {
            window.Dispose();
            platform.Dispose();
        }
    }

    private sealed class CallbackSink :
        IWindowPlatformCallbacks
    {
        public void RequestClose()
        {
        }

        public void ActivationChanged(bool active)
        {
        }

        public void BoundsChanged(
            UiViewport viewport,
            float left,
            float top,
            WindowState state)
        {
        }

        public void RenderRequested()
        {
        }
    }
}
