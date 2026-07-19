using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame.Prism.Surfaces;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windows;
using Cerneala.UI.Prism.Definitions;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Tests.Drawing.MonoGame;

public sealed class PrismSurfacePoolTests
{
    private static readonly PrismSurfaceKey DefaultKey = new(
        8,
        8,
        SurfaceFormat.Color,
        0,
        PrismColorProfile.Srgb);

    [Fact]
    public void SurfaceKeyRejectsInvalidTypedDimensionsAndEnums()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PrismSurfaceKey(
                0,
                8,
                SurfaceFormat.Color,
                0,
                PrismColorProfile.Srgb));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PrismSurfaceKey(
                8,
                8,
                (SurfaceFormat)int.MaxValue,
                0,
                PrismColorProfile.Srgb));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PrismSurfaceKey(
                8,
                8,
                SurfaceFormat.Color,
                -1,
                PrismColorProfile.Srgb));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PrismSurfaceKey(
                8,
                8,
                SurfaceFormat.Color,
                0,
                (PrismColorProfile)int.MaxValue));
    }

    [Fact]
    public void CompatibleSurfacesAreReusedAcrossFrames()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        using PrismSurfacePool pool =
            new(fixture.Session.GraphicsDevice);
        PrismGraphExecutionPlan plan = CreatePlan();
        PrismSurfaceKey[] keys = CreateKeys(plan, DefaultKey);

        ExecuteFrame(pool, plan, keys);
        long createdAfterWarmup = pool.CreatedSurfaceCount;
        long reusedAfterWarmup = pool.ReusedSurfaceCount;

        ExecuteFrame(pool, plan, keys);

        Assert.Equal(createdAfterWarmup, pool.CreatedSurfaceCount);
        Assert.True(pool.ReusedSurfaceCount > reusedAfterWarmup);
        Assert.Equal(0, pool.ActiveLeaseCount);
        Assert.InRange(pool.OwnedSurfaceCount, 0, plan.PeakLiveSurfaces);
    }

    [Fact]
    public void IncompatibleDimensionsFormatsSamplesAndColorProfilesAreNotReused()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        PrismGraphExecutionPlan plan = CreatePlan();
        PrismSurfaceKey[] variants =
        [
            new(
                9,
                DefaultKey.Height,
                DefaultKey.Format,
                DefaultKey.MultiSampleCount,
                DefaultKey.ColorProfile),
            new(
                DefaultKey.Width,
                9,
                DefaultKey.Format,
                DefaultKey.MultiSampleCount,
                DefaultKey.ColorProfile),
            new(
                DefaultKey.Width,
                DefaultKey.Height,
                SurfaceFormat.Bgra32,
                DefaultKey.MultiSampleCount,
                DefaultKey.ColorProfile),
            new(
                DefaultKey.Width,
                DefaultKey.Height,
                DefaultKey.Format,
                1,
                DefaultKey.ColorProfile),
            new(
                DefaultKey.Width,
                DefaultKey.Height,
                DefaultKey.Format,
                DefaultKey.MultiSampleCount,
                PrismColorProfile.LinearSrgb)
        ];

        foreach (PrismSurfaceKey variant in variants)
        {
            using PrismSurfacePool pool =
                new(fixture.Session.GraphicsDevice);
            ExecuteFrame(pool, plan, CreateKeys(plan, DefaultKey));
            long createdBeforeVariant = pool.CreatedSurfaceCount;

            ExecuteFrame(pool, plan, CreateKeys(plan, variant));

            Assert.Equal(
                createdBeforeVariant + plan.PeakLiveSurfaces,
                pool.CreatedSurfaceCount);
            Assert.True(
                pool.DisposedSurfaceCount >= plan.PeakLiveSurfaces);
            Assert.InRange(
                pool.OwnedSurfaceCount,
                0,
                plan.PeakLiveSurfaces);
        }
    }

    [Fact]
    public void FrameFinallyReleasesAllLeasesAfterException()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        using PrismSurfacePool pool =
            new(fixture.Session.GraphicsDevice);
        PrismGraphExecutionPlan plan = CreatePlan();
        PrismSurfaceKey[] keys = CreateKeys(plan, DefaultKey);

        Assert.Throws<InvalidOperationException>(
            () => ExecuteFailingFrame(pool, plan, keys));

        Assert.Equal(0, pool.ActiveLeaseCount);
        ExecuteFrame(pool, plan, keys);
        Assert.Equal(0, pool.ActiveLeaseCount);
    }

    [Fact]
    public void PoolConsumesOptimizerLifetimesAndDeclaredPeak()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        using PrismSurfacePool pool =
            new(fixture.Session.GraphicsDevice);
        PrismGraphExecutionPlan plan = CreatePlan();
        PrismSurfaceKey[] keys = CreateKeys(plan, DefaultKey);

        using (PrismSurfaceFrame frame = pool.BeginFrame(plan))
        {
            for (int step = 0; step < plan.ExecutionOrder.Length; step++)
            {
                frame.AdvanceToStep(step, keys);
                int expectedLive = plan.SurfaceLifetimes.Count(
                    lifetime =>
                        lifetime.FirstStep <= step &&
                        lifetime.LastStep >= step);

                Assert.Equal(expectedLive, pool.ActiveLeaseCount);
                Assert.InRange(
                    pool.ActiveLeaseCount,
                    0,
                    plan.PeakLiveSurfaces);
                Assert.NotNull(frame.GetSurface(step));
            }
        }

        Assert.Equal(plan.PeakLiveSurfaces, pool.PeakActiveLeaseCount);
        Assert.Equal(0, pool.ActiveLeaseCount);
    }

    [Fact]
    public void FinalSurfaceCanTransferToRetainedOwnershipWithoutRecycling()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        using PrismSurfacePool pool =
            new(fixture.Session.GraphicsDevice);
        PrismGraphExecutionPlan plan = CreatePlan();
        PrismSurfaceKey[] keys = CreateKeys(plan, DefaultKey);
        int finalIndex = plan.ExecutionOrder.Length - 1;
        PrismRetainedSurface retained = null!;
        RenderTarget2D retainedTarget = null!;

        using (PrismSurfaceFrame frame = pool.BeginFrame(plan))
        {
            for (int step = 0; step < plan.ExecutionOrder.Length; step++)
            {
                frame.AdvanceToStep(step, keys);
            }

            retainedTarget = frame.GetSurface(finalIndex);
            retained = frame.PromoteToRetainedOwner(finalIndex);
            Assert.Same(retainedTarget, retained.Surface);
        }

        Assert.Equal(1, pool.PromotedSurfaceCount);
        Assert.False(retainedTarget.IsDisposed);

        using (PrismSurfaceFrame frame = pool.BeginFrame(plan))
        {
            for (int step = 0; step < plan.ExecutionOrder.Length; step++)
            {
                frame.AdvanceToStep(step, keys);
                Assert.NotSame(retainedTarget, frame.GetSurface(step));
            }
        }

        retained.Dispose();
        Assert.True(retainedTarget.IsDisposed);
    }

    [Fact]
    public void ExplicitResetEvictsAvailableAndActiveSurfaces()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        using PrismSurfacePool pool =
            new(fixture.Session.GraphicsDevice);
        PrismGraphExecutionPlan plan = CreatePlan();
        PrismSurfaceKey[] keys = CreateKeys(plan, DefaultKey);

        PrismSurfaceFrame frame = pool.BeginFrame(plan);
        frame.AdvanceToStep(0, keys);
        RenderTarget2D activeSurface = frame.GetSurface(0);

        pool.Reset();
        frame.Dispose();

        Assert.True(activeSurface.IsDisposed);
        Assert.Equal(0, pool.ActiveLeaseCount);
        Assert.Equal(0, pool.OwnedSurfaceCount);
        ExecuteFrame(pool, plan, keys);
    }

    [Fact]
    public void ResizeDeviceResetEventEvictsAllSurfaces()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        using PrismSurfacePool pool =
            new(fixture.Session.GraphicsDevice);
        PrismGraphExecutionPlan plan = CreatePlan();
        PrismSurfaceKey[] keys = CreateKeys(plan, DefaultKey);

        ExecuteFrame(pool, plan, keys);
        Assert.True(pool.OwnedSurfaceCount > 0);

        fixture.Session.Resize(97, 65, 1);

        Assert.Equal(0, pool.ActiveLeaseCount);
        Assert.Equal(0, pool.OwnedSurfaceCount);
        Assert.True(pool.DisposedSurfaceCount > 0);
    }

    [Fact]
    public void DisposeEvictsOwnedSurfacesAndRejectsNewFrames()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        PrismSurfacePool pool = new(fixture.Session.GraphicsDevice);
        PrismGraphExecutionPlan plan = CreatePlan();
        PrismSurfaceKey[] keys = CreateKeys(plan, DefaultKey);
        RenderTarget2D trackedSurface;

        using (PrismSurfaceFrame frame = pool.BeginFrame(plan))
        {
            frame.AdvanceToStep(0, keys);
            trackedSurface = frame.GetSurface(0);
        }

        pool.Dispose();

        Assert.True(trackedSurface.IsDisposed);
        Assert.Equal(0, pool.OwnedSurfaceCount);
        Assert.Throws<ObjectDisposedException>(() => pool.BeginFrame(plan));
    }

    [Fact]
    public void ThousandsOfFramesKeepGpuOwnershipBoundedAndEndLeaseFree()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        using PrismSurfacePool pool =
            new(fixture.Session.GraphicsDevice);
        PrismGraphExecutionPlan plan = CreatePlan();
        PrismSurfaceKey[] keys = CreateKeys(plan, DefaultKey);
        PrismSurfaceKey alternateKey = new(
            9,
            9,
            DefaultKey.Format,
            DefaultKey.MultiSampleCount,
            DefaultKey.ColorProfile);

        const int frameCount = 2048;
        for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
        {
            Array.Fill(
                keys,
                frameIndex % 256 == 0 ? alternateKey : DefaultKey);
            ExecuteFrame(pool, plan, keys);

            Assert.Equal(0, pool.ActiveLeaseCount);
            Assert.InRange(
                pool.OwnedSurfaceCount,
                0,
                plan.PeakLiveSurfaces);
        }

        Assert.InRange(
            pool.CreatedSurfaceCount,
            1,
            plan.PeakLiveSurfaces * 17L);
    }

    private static void ExecuteFrame(
        PrismSurfacePool pool,
        PrismGraphExecutionPlan plan,
        PrismSurfaceKey[] keys)
    {
        using PrismSurfaceFrame frame = pool.BeginFrame(plan);
        for (int step = 0; step < plan.ExecutionOrder.Length; step++)
        {
            frame.AdvanceToStep(step, keys);
            _ = frame.GetSurface(step);
        }
    }

    private static void ExecuteFailingFrame(
        PrismSurfacePool pool,
        PrismGraphExecutionPlan plan,
        PrismSurfaceKey[] keys)
    {
        using PrismSurfaceFrame frame = pool.BeginFrame(plan);
        frame.AdvanceToStep(0, keys);
        Assert.True(pool.ActiveLeaseCount > 0);
        throw new InvalidOperationException("Injected Prism pass failure.");
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

    private static PrismGraphExecutionPlan CreatePlan()
    {
        PrismCompositionDefinition definition =
            PrismTestData.Composition(
                "Surface pool",
                PrismTestData.Layer(1, "Filtered layer"));
        PrismDrawScope scope = PrismTestData.Scope(definition);
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(
                new DrawRect(0, 0, 5, 5),
                Color.White),
            DrawCommand.EndPrism());
        PrismGraph graph = new PrismGraphBuilder().Build(
            new PrismFrameAnalyzer().Analyze(commands));
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
                        $"Cerneala surface pool {Guid.NewGuid():N}",
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

    private sealed class CallbackSink : IWindowPlatformCallbacks
    {
        public void RequestClose() { }

        public void ActivationChanged(bool active) { }

        public void BoundsChanged(
            UiViewport viewport,
            float left,
            float top,
            WindowState state)
        {
        }

        public void RenderRequested() { }
    }
}
