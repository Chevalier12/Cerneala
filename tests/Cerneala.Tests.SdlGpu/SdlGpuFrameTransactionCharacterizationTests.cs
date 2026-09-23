using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Drawing.Text;
using Cerneala.Platforms.Sdl3;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Media;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlGpuFrameTransactionCharacterizationTests
{
    [Fact]
    public void SuccessfulPresentSubmitsTheActiveBufferWithoutCancellation()
    {
        using SessionFixture fixture = new();

        fixture.Session.BeginFrame(Color.Black);
        fixture.Session.CompleteFrame(present: true);

        FakeGpuCommandBufferAction action = Assert.Single(
            fixture.Api.GpuCommandBufferActions);
        Assert.Equal(FakeGpuCommandBufferActionKind.Submit, action.Kind);
        Assert.True(action.Succeeded);
        Assert.DoesNotContain(
            fixture.Api.GpuCommandBufferActions,
            item => item.Kind == FakeGpuCommandBufferActionKind.Cancel);
    }

    [Fact]
    public void FailedSubmitConsumesTheCommandBufferAndMustNotAttemptCancellation()
    {
        using SessionFixture fixture = new();
        fixture.Api.EnqueueSubmitResults(false);

        fixture.Session.BeginFrame(Color.Black);
        Assert.Throws<InvalidOperationException>(
            () => fixture.Session.CompleteFrame(present: false));

        FakeGpuCommandBufferAction submit = Assert.Single(
            fixture.Api.GpuCommandBufferActions);
        Assert.Equal(FakeGpuCommandBufferActionKind.Submit, submit.Kind);
        Assert.False(submit.Succeeded);
        Assert.False(fixture.Session.IsFrameActive);
    }

    [Fact]
    public void SubmittedFirstBufferSurvivesFailedRecoveryAndOnlyRecoveryBufferIsCancelled()
    {
        using SessionFixture fixture = new();
        fixture.Api.EnqueueSwapchainAcquireResults(false, false);

        fixture.Session.BeginFrame(Color.Black);
        Assert.Throws<InvalidOperationException>(
            () => fixture.Session.CompleteFrame(present: true));

        Assert.Collection(
            fixture.Api.GpuCommandBufferActions,
            submit =>
            {
                Assert.Equal(FakeGpuCommandBufferActionKind.Submit, submit.Kind);
                Assert.True(submit.Succeeded);
            },
            cancel =>
            {
                Assert.Equal(FakeGpuCommandBufferActionKind.Cancel, cancel.Kind);
                Assert.True(cancel.Succeeded);
            });
        Assert.NotEqual(
            fixture.Api.GpuCommandBufferActions[0].CommandBuffer,
            fixture.Api.GpuCommandBufferActions[1].CommandBuffer);
        Assert.False(fixture.Session.IsFrameActive);
    }

    [Fact]
    public void CompleteFrameWithoutPresentStillSubmitsAndDoesNotAcquireSwapchain()
    {
        using SessionFixture fixture = new();

        fixture.Session.BeginFrame(Color.Black);
        fixture.Session.CompleteFrame(present: false);

        FakeGpuCommandBufferAction action = Assert.Single(
            fixture.Api.GpuCommandBufferActions);
        Assert.Equal(FakeGpuCommandBufferActionKind.Submit, action.Kind);
        Assert.True(action.Succeeded);
        Assert.DoesNotContain(
            fixture.Api.GpuActions,
            value => value.StartsWith("acquire-swapchain", StringComparison.Ordinal));
    }

    [Fact]
    public void ScreenshotCallbackExceptionIsPropagatedAfterSubmittingTheFrame()
    {
        using SessionFixture fixture = new();
        using MemoryStream output = new();

        Assert.Throws<InjectedScreenshotException>(() => fixture.Session.RenderPng(
            output,
            Color.Black,
            _ => throw new InjectedScreenshotException()));

        FakeGpuCommandBufferAction action = Assert.Single(
            fixture.Api.GpuCommandBufferActions);
        Assert.Equal(FakeGpuCommandBufferActionKind.Submit, action.Kind);
        Assert.True(action.Succeeded);
        Assert.False(fixture.Session.IsFrameActive);
    }

    [Fact]
    public void SamePrismSceneIsReusedInsideTheSameCommandBuffer()
    {
        using SessionFixture fixture = new();
        DrawCommandList commands = CreatePrismParentWithExisting2DContent();
        DrawingFrameContext frame = CreateFrame(commands);
        SdlGpuDrawingBackend backend = Assert.IsType<SdlGpuDrawingBackend>(
            fixture.Session.DrawingBackend);

        fixture.Session.BeginFrame(Color.Transparent);
        try
        {
            fixture.Session.DrawingBackend.Render(commands, in frame);
            Assert.True(backend.PrismDiagnostics.Counters.CaptureCount > 0);

            fixture.Session.DrawingBackend.Render(commands, in frame);

            Assert.Equal(0, backend.PrismDiagnostics.Counters.CaptureCount);
        }
        finally
        {
            fixture.Session.CompleteFrame(present: false);
        }
    }

    [Fact]
    public void SubmittedPrismCaptureSurvivesFailureOfTheRecoveryBuffer()
    {
        using SessionFixture fixture = new();
        DrawCommandList commands = CreatePrismParentWithExisting2DContent();
        DrawingFrameContext frame = CreateFrame(commands);
        SdlGpuDrawingBackend backend = Assert.IsType<SdlGpuDrawingBackend>(
            fixture.Session.DrawingBackend);
        fixture.Api.EnqueueSwapchainAcquireResults(false, false);

        fixture.Session.BeginFrame(Color.Transparent);
        fixture.Session.DrawingBackend.Render(commands, in frame);
        Assert.True(backend.PrismDiagnostics.Counters.CaptureCount > 0);
        Assert.Throws<InvalidOperationException>(
            () => fixture.Session.CompleteFrame(present: true));

        fixture.Session.BeginFrame(Color.Transparent);
        try
        {
            fixture.Session.DrawingBackend.Render(commands, in frame);

            Assert.Equal(0, backend.PrismDiagnostics.Counters.CaptureCount);
        }
        finally
        {
            fixture.Session.CompleteFrame(present: false);
        }
    }

    [Fact]
    public void DisposingAnActiveFrameCancelsItsUnsubmittedCommandBuffer()
    {
        using SessionFixture fixture = new();

        fixture.Session.BeginFrame(Color.Black);
        fixture.Session.Dispose();

        FakeGpuCommandBufferAction action = Assert.Single(
            fixture.Api.GpuCommandBufferActions);
        Assert.Equal(FakeGpuCommandBufferActionKind.Cancel, action.Kind);
        Assert.True(action.Succeeded);
        Assert.Equal(0, fixture.Api.LiveCommandBufferCount);
        Assert.False(fixture.Session.IsFrameActive);
    }

    [Fact]
    public void InvalidatingAPrismCaptureBeforeSubmitForcesSameBufferRecapture()
    {
        using SessionFixture fixture = new();
        DrawCommandList commands = CreatePrismParentWithExisting2DContent();
        DrawingFrameContext frame = CreateFrame(commands);
        SdlGpuDrawingBackend backend = Assert.IsType<SdlGpuDrawingBackend>(
            fixture.Session.DrawingBackend);

        fixture.Session.BeginFrame(Color.Transparent);
        try
        {
            fixture.Session.DrawingBackend.Render(commands, in frame);
            int capturesBeforeInvalidation = backend.PrismDiagnostics.Counters.CaptureCount;
            Assert.True(capturesBeforeInvalidation > 0);
            fixture.Session.DrawingResources.PrismResources.Invalidate(
                PrismCacheInvalidation.All);

            fixture.Session.DrawingBackend.Render(commands, in frame);

            Assert.True(backend.PrismDiagnostics.Counters.CaptureCount > 0);
        }
        finally
        {
            fixture.Session.CompleteFrame(present: false);
        }
    }

    [Fact]
    public void PendingPrismCaptureCannotCrossInterleavedSessions()
    {
        using SessionFixture fixture = new();
        using SdlGpuWindowGraphicsSession second = fixture.CreateAdditionalSession("second");
        DrawCommandList commands = CreatePrismParentWithExisting2DContent();
        DrawingFrameContext frame = CreateFrame(commands);

        fixture.Session.BeginFrame(Color.Transparent);
        fixture.Session.DrawingBackend.Render(commands, in frame);
        SdlGpuDrawingBackend firstBackend = Assert.IsType<SdlGpuDrawingBackend>(
            fixture.Session.DrawingBackend);
        Assert.True(firstBackend.PrismDiagnostics.Counters.CaptureCount > 0);

        second.BeginFrame(Color.Transparent);
        try
        {
            second.DrawingBackend.Render(commands, in frame);
            SdlGpuDrawingBackend secondBackend = Assert.IsType<SdlGpuDrawingBackend>(
                second.DrawingBackend);

            Assert.True(
                secondBackend.PrismDiagnostics.Counters.CaptureCount > 0,
                "A capture pending in another session must not be a cross-window retained hit.");
        }
        finally
        {
            second.CompleteFrame(present: false);
            fixture.Session.CompleteFrame(present: false);
        }
    }

    [Fact]
    public void FailedSubmitCannotPublishAParentPrismCaptureForTheRecoveryFrame()
    {
        using SessionFixture fixture = new();
        DrawCommandList commands = CreatePrismParentWithExisting2DContent();
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        DrawingFrameContext frame = new(analysis);
        fixture.Api.EnqueueSubmitResults(false);

        fixture.Session.BeginFrame(Color.Transparent);
        fixture.Session.DrawingBackend.Render(commands, in frame);
        SdlGpuDrawingBackend firstBackend = Assert.IsType<SdlGpuDrawingBackend>(
            fixture.Session.DrawingBackend);
        Assert.True(firstBackend.PrismDiagnostics.Counters.CaptureCount > 0);
        Assert.Throws<InvalidOperationException>(
            () => fixture.Session.CompleteFrame(present: false));
        FakeGpuCommandBufferAction submit = Assert.Single(
            fixture.Api.GpuCommandBufferActions.Where(action =>
                action.Kind == FakeGpuCommandBufferActionKind.Submit));
        Assert.False(submit.Succeeded);

        fixture.Session.BeginFrame(Color.Transparent);
        try
        {
            fixture.Session.DrawingBackend.Render(commands, in frame);
            SdlGpuDrawingBackend backend = Assert.IsType<SdlGpuDrawingBackend>(
                fixture.Session.DrawingBackend);

            Assert.True(
                backend.PrismDiagnostics.Counters.CaptureCount > 0,
                "A Prism parent produced only in a failed submission must be recaptured before reuse.");
        }
        finally
        {
            fixture.Session.CompleteFrame(present: false);
        }
    }

    [Fact]
    public void SuccessfullySubmittedParentPrismCaptureIsReusableByTheNextFrame()
    {
        using SessionFixture fixture = new();
        DrawCommandList commands = CreatePrismParentWithExisting2DContent();
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        DrawingFrameContext frame = new(analysis);
        SdlGpuDrawingBackend backend = Assert.IsType<SdlGpuDrawingBackend>(
            fixture.Session.DrawingBackend);

        fixture.Session.BeginFrame(Color.Transparent);
        fixture.Session.DrawingBackend.Render(commands, in frame);
        Assert.True(backend.PrismDiagnostics.Counters.CaptureCount > 0);
        fixture.Session.CompleteFrame(present: false);

        fixture.Session.BeginFrame(Color.Transparent);
        try
        {
            fixture.Session.DrawingBackend.Render(commands, in frame);

            Assert.Equal(0, backend.PrismDiagnostics.Counters.CaptureCount);
        }
        finally
        {
            fixture.Session.CompleteFrame(present: false);
        }
    }

    [Fact]
    public void FailedSubmitCannotPublishOnDemandSurfaceOrItsParentPrismCapture()
    {
        using SessionFixture fixture = new();
        RenderSurface2D surface = new()
        {
            RedrawMode = RenderSurface2DRedrawMode.OnDemand
        };
        int drawCount = 0;
        surface.Draw += (_, frame) =>
        {
            drawCount++;
            frame.FillRectangle(frame.Bounds, Color.Coral);
        };
        DrawCommandList commands = CreatePrismParentWithSurface(surface);
        DrawingFrameContext frameContext = CreateFrame(commands);
        fixture.Api.EnqueueSubmitResults(false);

        fixture.Session.BeginFrame(Color.Transparent);
        fixture.Session.DrawingBackend.Render(commands, in frameContext);
        Assert.Equal(1, drawCount);
        int renderTargetEventIndex = fixture.Api.RenderTargets.Count;
        Assert.True(HasRenderPassForTargetSizeAfter(
            fixture.Api,
            0,
            3,
            5));
        Assert.Throws<InvalidOperationException>(
            () => fixture.Session.CompleteFrame(present: false));

        fixture.Session.BeginFrame(Color.Transparent);
        try
        {
            fixture.Session.DrawingBackend.Render(commands, in frameContext);
            SdlGpuDrawingBackend backend = Assert.IsType<SdlGpuDrawingBackend>(
                fixture.Session.DrawingBackend);

            Assert.True(
                HasRenderPassForTargetSizeAfter(
                    fixture.Api,
                    renderTargetEventIndex,
                    3,
                    5),
                "The child surface raster produced only in the failed submission must be rebuilt.");
            Assert.True(backend.PrismDiagnostics.Counters.CaptureCount > 0);
        }
        finally
        {
            fixture.Session.CompleteFrame(present: false);
            ((IRenderSurface2DFrameSource)surface).SetBackendState(
                fixture.Session.DrawingResources,
                null);
        }
    }

    [Fact]
    public void SameOnDemandSurfaceUsesDistinctTargetsInInterleavedSessions()
    {
        using SessionFixture fixture = new();
        using SdlGpuWindowGraphicsSession second = fixture.CreateAdditionalSession(
            "interleaved shared surface");
        Assert.Same(fixture.Session.DrawingResources, second.DrawingResources);
        RenderSurface2D surface = new()
        {
            RedrawMode = RenderSurface2DRedrawMode.OnDemand
        };
        int drawCount = 0;
        surface.Draw += (_, frame) =>
        {
            drawCount++;
            frame.FillRectangle(frame.Bounds, Color.Coral);
        };
        DrawCommandList commands = new();
        commands.Add(DrawCommand.RenderSurface2D(
            surface,
            new DrawRect(0, 0, 3, 5),
            Color.White));
        DrawingFrameContext frameContext = CreateFrame(commands);

        fixture.Session.BeginFrame(Color.Transparent);
        fixture.Session.DrawingBackend.Render(commands, in frameContext);
        nint firstTarget = GetLastRenderTargetTextureForSize(
            fixture.Api,
            3,
            5);

        second.BeginFrame(Color.Transparent);
        try
        {
            second.DrawingBackend.Render(commands, in frameContext);
            nint secondTarget = GetLastRenderTargetTextureForSize(
                fixture.Api,
                3,
                5);

            Assert.Equal(2, drawCount);
            Assert.NotEqual(firstTarget, secondTarget);
        }
        finally
        {
            second.CompleteFrame(present: false);
            fixture.Session.CompleteFrame(present: false);
            ((IRenderSurface2DFrameSource)surface).SetBackendState(
                fixture.Session.DrawingResources,
                null);
        }
    }

    [Fact]
    public void DisposedTransientSurfaceSessionsReleaseOnlyTheirSessionTargets()
    {
        using SessionFixture fixture = new();
        RenderSurface2D surface = new()
        {
            RedrawMode = RenderSurface2DRedrawMode.OnDemand
        };
        int drawCount = 0;
        surface.Draw += (_, frame) =>
        {
            drawCount++;
            frame.FillRectangle(frame.Bounds, Color.Coral);
        };
        DrawCommandList commands = new();
        commands.Add(DrawCommand.RenderSurface2D(
            surface,
            new DrawRect(0, 0, 3, 5),
            Color.White));
        DrawingFrameContext frameContext = CreateFrame(commands);
        List<WeakReference> transientSessions = [];

        try
        {
            fixture.Session.BeginFrame(Color.Transparent);
            fixture.Session.DrawingBackend.Render(commands, in frameContext);
            nint survivingTarget = GetLastRenderTargetTextureForSize(fixture.Api, 3, 5);
            fixture.Session.CompleteFrame(present: false);

            for (int iteration = 0; iteration < 100; iteration++)
            {
                WeakReference transient = RenderAndDisposeTransientSurfaceSession(
                    fixture,
                    surface,
                    commands,
                    frameContext,
                    iteration,
                    out nint transientTarget);
                transientSessions.Add(transient);
                fixture.Session.DrawingResources.FlushRetired();

                Assert.Contains(transientTarget, fixture.Api.ReleasedGpuTextures);
                Assert.DoesNotContain(survivingTarget, fixture.Api.ReleasedGpuTextures);
                Assert.True(fixture.Api.GpuTextures.ContainsKey(survivingTarget));
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Assert.All(transientSessions, transient => Assert.False(transient.IsAlive));

            fixture.Session.BeginFrame(Color.Transparent);
            fixture.Session.DrawingBackend.Render(commands, in frameContext);
            fixture.Session.CompleteFrame(present: false);

            Assert.Equal(101, drawCount);
            Assert.True(fixture.Api.GpuTextures.ContainsKey(survivingTarget));
        }
        finally
        {
            ((IRenderSurface2DFrameSource)surface).SetBackendState(
                fixture.Session.DrawingResources,
                null);
            fixture.Session.DrawingResources.FlushRetired();
        }
    }

    [Fact]
    public void DetachedSurfaceStateCachesDoNotAccumulateInALiveSession()
    {
        using SessionFixture fixture = new();
        SdlGpuDrawingBackend backend = Assert.IsType<SdlGpuDrawingBackend>(
            fixture.Session.DrawingBackend);

        for (int iteration = 0; iteration < 100; iteration++)
        {
            RenderSurface2D surface = new()
            {
                RedrawMode = RenderSurface2DRedrawMode.OnDemand
            };
            surface.Draw += (_, frame) =>
                frame.FillRectangle(frame.Bounds, Color.Coral);
            DrawCommandList commands = new();
            commands.Add(DrawCommand.RenderSurface2D(
                surface,
                new DrawRect(0, 0, 3, 5),
                Color.White));
            DrawingFrameContext frameContext = CreateFrame(commands);

            fixture.Session.BeginFrame(Color.Transparent);
            fixture.Session.DrawingBackend.Render(commands, in frameContext);
            nint target = GetLastRenderTargetTextureForSize(fixture.Api, 3, 5);
            fixture.Session.CompleteFrame(present: false);
            Assert.Equal(1, GetTrackedRenderSurfaceStateCacheCount(backend));

            ((IRenderSurface2DFrameSource)surface).SetBackendState(
                fixture.Session.DrawingResources,
                null);
            fixture.Session.DrawingResources.FlushRetired();

            Assert.Equal(0, GetTrackedRenderSurfaceStateCacheCount(backend));
            Assert.Contains(target, fixture.Api.ReleasedGpuTextures);
        }
    }

    [Fact]
    public void PartialOnDemandSurfaceFailureIsRetriedUnderParentPrism()
    {
        using SessionFixture fixture = new();
        RenderSurface2D surface = new()
        {
            RedrawMode = RenderSurface2DRedrawMode.OnDemand
        };
        int drawCount = 0;
        bool fail = true;
        surface.Draw += (_, frame) =>
        {
            drawCount++;
            frame.FillRectangle(frame.Bounds, Color.Coral);
            if (fail)
            {
                throw new InjectedSurfaceException();
            }
        };
        DrawCommandList commands = CreatePrismParentWithSurface(surface);
        DrawingFrameContext frameContext = CreateFrame(commands);

        fixture.Session.BeginFrame(Color.Transparent);
        Assert.Throws<InjectedSurfaceException>(
            () => fixture.Session.DrawingBackend.Render(commands, in frameContext));
        fixture.Session.CompleteFrame(present: false);

        fail = false;
        fixture.Session.BeginFrame(Color.Transparent);
        try
        {
            fixture.Session.DrawingBackend.Render(commands, in frameContext);
            SdlGpuDrawingBackend backend = Assert.IsType<SdlGpuDrawingBackend>(
                fixture.Session.DrawingBackend);

            Assert.Equal(2, drawCount);
            Assert.True(backend.PrismDiagnostics.Counters.CaptureCount > 0);
        }
        finally
        {
            fixture.Session.CompleteFrame(present: false);
            ((IRenderSurface2DFrameSource)surface).SetBackendState(
                fixture.Session.DrawingResources,
                null);
        }
    }

    [Fact]
    public void FailedSubmitCannotPublishDrawingBrushCaptureUnderParentPrism()
    {
        using SessionFixture fixture = new();
        DrawRect brushBounds = new(0, 0, 3, 5);
        DrawingBrush brush = new(
            [DrawCommand.FillRectangle(brushBounds, new Color(100, 149, 237))],
            brushBounds);
        DrawCommandList commands = CreatePrismParentWithBrush(brush, brushBounds);
        DrawingFrameContext frame = CreateFrame(commands);
        fixture.Api.EnqueueSubmitResults(false);

        fixture.Session.BeginFrame(Color.Transparent);
        fixture.Session.DrawingBackend.Render(commands, in frame);
        int renderTargetEventIndex = fixture.Api.RenderTargets.Count;
        Assert.True(HasRenderPassForTargetSizeAfter(fixture.Api, 0, 3, 5));
        Assert.Throws<InvalidOperationException>(
            () => fixture.Session.CompleteFrame(present: false));
        fixture.Session.DrawingResources.PrismResources.Invalidate(
            PrismCacheInvalidation.All);

        fixture.Session.BeginFrame(Color.Transparent);
        try
        {
            fixture.Session.DrawingBackend.Render(commands, in frame);

            Assert.True(
                HasRenderPassForTargetSizeAfter(
                    fixture.Api,
                    renderTargetEventIndex,
                    3,
                    5),
                "The drawing-brush raster produced only in the failed submission must be rebuilt.");
        }
        finally
        {
            fixture.Session.CompleteFrame(present: false);
        }
    }

    [Fact]
    public void PartialDrawingBrushFailureIsRetriedUnderParentPrism()
    {
        using SessionFixture fixture = new();
        DrawRect brushBounds = new(0, 0, 3, 5);
        RenderSurface2D surface = new()
        {
            RedrawMode = RenderSurface2DRedrawMode.OnDemand
        };
        int drawCount = 0;
        bool fail = true;
        surface.Draw += (_, frame) =>
        {
            drawCount++;
            frame.FillRectangle(frame.Bounds, Color.Coral);
            if (fail)
            {
                throw new InjectedBrushException();
            }
        };
        DrawingBrush brush = new(
            [DrawCommand.RenderSurface2D(surface, brushBounds, Color.White)],
            brushBounds);
        DrawCommandList commands = CreatePrismParentWithBrush(brush, brushBounds);
        DrawingFrameContext frame = CreateFrame(commands);

        fixture.Session.BeginFrame(Color.Transparent);
        Assert.Throws<InjectedBrushException>(
            () => fixture.Session.DrawingBackend.Render(commands, in frame));
        fixture.Session.CompleteFrame(present: false);

        fail = false;
        fixture.Session.BeginFrame(Color.Transparent);
        try
        {
            fixture.Session.DrawingBackend.Render(commands, in frame);
            SdlGpuDrawingBackend backend = Assert.IsType<SdlGpuDrawingBackend>(
                fixture.Session.DrawingBackend);

            Assert.True(HasRenderPassForTargetSizeAfter(fixture.Api, 0, 3, 5));
            Assert.True(backend.PrismDiagnostics.Counters.CaptureCount > 0);
            Assert.Equal(2, drawCount);
        }
        finally
        {
            fixture.Session.CompleteFrame(present: false);
            ((IRenderSurface2DFrameSource)surface).SetBackendState(
                fixture.Session.DrawingResources,
                null);
        }
    }

    [Fact]
    public void ColdImageUploadFromFailedSubmitMustBeUploadedAgain()
    {
        using SessionFixture fixture = new();
        const int imageWidth = 7;
        const int imageHeight = 5;
        using SdlGpuImage image = new(
            imageWidth,
            imageHeight,
            new byte[imageWidth * imageHeight * 4]);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.DrawImage(
            image,
            new DrawRect(0, 0, imageWidth, imageHeight),
            Color.White));
        DrawingFrameContext frame = CreateFrame(commands);
        fixture.Api.EnqueueSubmitResults(false);

        fixture.Session.BeginFrame(Color.Transparent);
        fixture.Session.DrawingBackend.Render(commands, in frame);
        int uploadEventIndex = fixture.Api.GpuTextureUploads.Count;
        Assert.True(HasTextureUploadForSizeAfter(
            fixture.Api,
            0,
            imageWidth,
            imageHeight));
        Assert.Throws<InvalidOperationException>(
            () => fixture.Session.CompleteFrame(present: false));

        fixture.Session.BeginFrame(Color.Transparent);
        try
        {
            fixture.Session.DrawingBackend.Render(commands, in frame);

            Assert.True(
                HasTextureUploadForSizeAfter(
                    fixture.Api,
                    uploadEventIndex,
                    imageWidth,
                    imageHeight),
                "A cold image upload consumed by a failed submit cannot remain a valid cache hit.");
        }
        finally
        {
            fixture.Session.CompleteFrame(present: false);
        }
    }

    [Fact]
    public void SuccessfullySubmittedImageUploadIsReusedWithoutAnotherUpload()
    {
        using SessionFixture fixture = new();
        const int imageWidth = 7;
        const int imageHeight = 5;
        using SdlGpuImage image = new(
            imageWidth,
            imageHeight,
            new byte[imageWidth * imageHeight * 4]);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.DrawImage(
            image,
            new DrawRect(0, 0, imageWidth, imageHeight),
            Color.White));
        DrawingFrameContext frame = CreateFrame(commands);

        fixture.Session.BeginFrame(Color.Transparent);
        fixture.Session.DrawingBackend.Render(commands, in frame);
        int uploadEventIndex = fixture.Api.GpuTextureUploads.Count;
        Assert.True(HasTextureUploadForSizeAfter(
            fixture.Api,
            0,
            imageWidth,
            imageHeight));
        fixture.Session.CompleteFrame(present: false);

        fixture.Session.BeginFrame(Color.Transparent);
        try
        {
            fixture.Session.DrawingBackend.Render(commands, in frame);

            Assert.False(HasTextureUploadForSizeAfter(
                fixture.Api,
                uploadEventIndex,
                imageWidth,
                imageHeight));
        }
        finally
        {
            fixture.Session.CompleteFrame(present: false);
        }
    }

    [Fact]
    public void PendingImageUploadCannotCrossInterleavedSessions()
    {
        using SessionFixture fixture = new();
        using SdlGpuWindowGraphicsSession second = fixture.CreateAdditionalSession("second image");
        const int imageWidth = 7;
        const int imageHeight = 5;
        using SdlGpuImage image = new(
            imageWidth,
            imageHeight,
            new byte[imageWidth * imageHeight * 4]);
        DrawCommandList commands = CreateImageCommands(image, imageWidth, imageHeight);
        DrawingFrameContext frame = CreateFrame(commands);

        fixture.Session.BeginFrame(Color.Transparent);
        fixture.Session.DrawingBackend.Render(commands, in frame);
        int uploadEventIndex = fixture.Api.GpuTextureUploads.Count;
        Assert.True(HasTextureUploadForSizeAfter(
            fixture.Api,
            0,
            imageWidth,
            imageHeight));

        second.BeginFrame(Color.Transparent);
        try
        {
            second.DrawingBackend.Render(commands, in frame);

            Assert.True(
                HasTextureUploadForSizeAfter(
                    fixture.Api,
                    uploadEventIndex,
                    imageWidth,
                    imageHeight),
                "An image upload pending in another session cannot be a cross-window cache hit.");
        }
        finally
        {
            second.CompleteFrame(present: false);
            fixture.Session.CompleteFrame(present: false);
        }
    }

    [Fact]
    public void AbandonedImageUploadMustBeUploadedAgainByAnotherSession()
    {
        using SessionFixture fixture = new();
        using SdlGpuWindowGraphicsSession second = fixture.CreateAdditionalSession("image recovery");
        const int imageWidth = 7;
        const int imageHeight = 5;
        using SdlGpuImage image = new(
            imageWidth,
            imageHeight,
            new byte[imageWidth * imageHeight * 4]);
        DrawCommandList commands = CreateImageCommands(image, imageWidth, imageHeight);
        DrawingFrameContext frame = CreateFrame(commands);

        fixture.Session.BeginFrame(Color.Transparent);
        fixture.Session.DrawingBackend.Render(commands, in frame);
        int uploadEventIndex = fixture.Api.GpuTextureUploads.Count;
        Assert.True(HasTextureUploadForSizeAfter(
            fixture.Api,
            0,
            imageWidth,
            imageHeight));
        fixture.Session.Dispose();

        second.BeginFrame(Color.Transparent);
        try
        {
            second.DrawingBackend.Render(commands, in frame);

            Assert.True(
                HasTextureUploadForSizeAfter(
                    fixture.Api,
                    uploadEventIndex,
                    imageWidth,
                    imageHeight),
                "An image upload recorded only by an abandoned buffer must be uploaded again.");
        }
        finally
        {
            second.CompleteFrame(present: false);
        }
    }

    [Fact]
    public void FailedSubmitDrawTextAtlasHitUploadsAgainWithoutRerasterizing()
    {
        using SessionFixture fixture = new();
        DrawCommandList commands = CreateTextCommands("failed submit backend text");
        DrawingFrameContext frame = CreateFrame(commands);
        fixture.Api.EnqueueSubmitResults(false);

        fixture.Session.BeginFrame(Color.Transparent);
        fixture.Session.DrawingBackend.Render(commands, in frame);
        int uploadEventIndex = fixture.Api.GpuTextureUploads.Count;
        Assert.True(HasTextAtlasUploadAfter(fixture.Api, 0));
        Assert.Throws<InvalidOperationException>(
            () => fixture.Session.CompleteFrame(present: false));

        fixture.Session.BeginFrame(Color.Transparent);
        try
        {
            fixture.Session.DrawingBackend.Render(commands, in frame);
            SdlGpuDrawingBackend backend = Assert.IsType<SdlGpuDrawingBackend>(
                fixture.Session.DrawingBackend);

            Assert.True(
                HasTextAtlasUploadAfter(fixture.Api, uploadEventIndex),
                "A backend text-atlas hit whose upload failed must require the upload again.");
            Assert.Equal(0, backend.LastFrameTiming.TextRequestCount);
        }
        finally
        {
            fixture.Session.CompleteFrame(present: false);
        }
    }

    [Fact]
    public void AbandonedDrawTextAtlasHitUploadsAgainInAnotherSession()
    {
        using SessionFixture fixture = new();
        using SdlGpuWindowGraphicsSession second = fixture.CreateAdditionalSession(
            "abandoned backend text");
        DrawCommandList commands = CreateTextCommands("abandoned backend text");
        DrawingFrameContext frame = CreateFrame(commands);

        fixture.Session.BeginFrame(Color.Transparent);
        fixture.Session.DrawingBackend.Render(commands, in frame);
        int uploadEventIndex = fixture.Api.GpuTextureUploads.Count;
        Assert.True(HasTextAtlasUploadAfter(fixture.Api, 0));
        fixture.Session.Dispose();

        second.BeginFrame(Color.Transparent);
        try
        {
            second.DrawingBackend.Render(commands, in frame);
            SdlGpuDrawingBackend backend = Assert.IsType<SdlGpuDrawingBackend>(
                second.DrawingBackend);

            Assert.True(
                HasTextAtlasUploadAfter(fixture.Api, uploadEventIndex),
                "A backend text-atlas hit recorded only by an abandoned buffer must upload again.");
            Assert.Equal(0, backend.LastFrameTiming.TextRequestCount);
        }
        finally
        {
            second.CompleteFrame(present: false);
        }
    }

    [Fact]
    public void PendingDrawTextAtlasHitCannotCrossInterleavedSessions()
    {
        using SessionFixture fixture = new();
        using SdlGpuWindowGraphicsSession second = fixture.CreateAdditionalSession(
            "interleaved backend text");
        DrawCommandList commands = CreateTextCommands("interleaved backend text");
        DrawingFrameContext frame = CreateFrame(commands);

        fixture.Session.BeginFrame(Color.Transparent);
        fixture.Session.DrawingBackend.Render(commands, in frame);
        int uploadEventIndex = fixture.Api.GpuTextureUploads.Count;
        Assert.True(HasTextAtlasUploadAfter(fixture.Api, 0));

        second.BeginFrame(Color.Transparent);
        try
        {
            second.DrawingBackend.Render(commands, in frame);
            SdlGpuDrawingBackend backend = Assert.IsType<SdlGpuDrawingBackend>(
                second.DrawingBackend);

            Assert.True(
                HasTextAtlasUploadAfter(fixture.Api, uploadEventIndex),
                "A backend text-atlas hit pending in another session must upload for this buffer.");
            Assert.Equal(0, backend.LastFrameTiming.TextRequestCount);
        }
        finally
        {
            second.CompleteFrame(present: false);
            fixture.Session.CompleteFrame(present: false);
        }
    }

    [Fact]
    public void SuccessfullySubmittedDrawTextAtlasHitReusesWithoutUploadOrRasterization()
    {
        using SessionFixture fixture = new();
        DrawCommandList commands = CreateTextCommands("submitted backend text");
        DrawingFrameContext frame = CreateFrame(commands);

        fixture.Session.BeginFrame(Color.Transparent);
        fixture.Session.DrawingBackend.Render(commands, in frame);
        fixture.Session.CompleteFrame(present: false);
        int uploadEventIndex = fixture.Api.GpuTextureUploads.Count;

        fixture.Session.BeginFrame(Color.Transparent);
        try
        {
            fixture.Session.DrawingBackend.Render(commands, in frame);
            SdlGpuDrawingBackend backend = Assert.IsType<SdlGpuDrawingBackend>(
                fixture.Session.DrawingBackend);

            Assert.False(HasTextAtlasUploadAfter(fixture.Api, uploadEventIndex));
            Assert.Equal(0, backend.LastFrameTiming.TextRequestCount);
            Assert.Equal(0, backend.LastFrameTiming.RasterizedPixelCount);
        }
        finally
        {
            fixture.Session.CompleteFrame(present: false);
        }
    }

    [Fact]
    public void DrawTextAtlasHitStaysPinnedThroughPendingBufferUnderEvictionPressure()
    {
        using SessionFixture fixture = new();
        using SdlGpuWindowGraphicsSession second = fixture.CreateAdditionalSession(
            "backend text eviction pressure");
        TextCommandScenario text = CreateTextScenario("pending backend text entry");
        DrawingFrameContext frame = CreateFrame(text.Commands);
        RasterizedText pressureLayer = CreateSquareTextLayer(256);
        SdlGpuDrawingResources resources = fixture.Session.DrawingResources;

        fixture.Session.BeginFrame(Color.Transparent);
        fixture.Session.DrawingBackend.Render(text.Commands, in frame);
        fixture.Session.CompleteFrame(present: false);
        Assert.True(resources.TryGetTextAtlasEntries(
            text.Keys.Red,
            text.Keys.Green,
            text.Keys.Blue,
            frameToken: 0,
            out _));

        long markerFrame = resources.BeginTextAtlasFrame();
        resources.EndTextAtlasFrame(markerFrame);
        fixture.Session.BeginFrame(Color.Transparent);
        fixture.Session.DrawingBackend.Render(text.Commands, in frame);
        Assert.True(resources.TryGetTextAtlasEntries(
            text.Keys.Red,
            text.Keys.Green,
            text.Keys.Blue,
            frameToken: 0,
            out SdlGpuTextAtlasEntries pendingEntries));
        Assert.All(
            new[] { pendingEntries.Red, pendingEntries.Green, pendingEntries.Blue },
            entry => Assert.Equal(2, entry.ActiveFrameCount));
        resources.EndTextAtlasFrame(checked(markerFrame + 1));
        Assert.All(
            new[] { pendingEntries.Red, pendingEntries.Green, pendingEntries.Blue },
            entry => Assert.Equal(1, entry.ActiveFrameCount));
        for (int index = 0; index < 50; index++)
        {
            TextAtlasKeySet pressureKeys = CreateTextAtlasKeySet(
                $"backend eviction pressure {index}");
            long atlasFrame = resources.BeginTextAtlasFrame();
            try
            {
                Assert.True(resources.GetOrCreateTextAtlasEntries(
                    second,
                    pressureKeys.Red,
                    pressureKeys.Green,
                    pressureKeys.Blue,
                    [pressureLayer, pressureLayer, pressureLayer],
                    atlasFrame).HasValue);
            }
            finally
            {
                resources.EndTextAtlasFrame(atlasFrame);
            }
        }

        Assert.True(resources.TryGetTextAtlasEntries(
            text.Keys.Red,
            text.Keys.Green,
            text.Keys.Blue,
            frameToken: 0,
            out _));
        fixture.Session.CompleteFrame(present: false);

        fixture.Session.BeginFrame(Color.Transparent);
        try
        {
            fixture.Session.DrawingBackend.Render(text.Commands, in frame);
            SdlGpuDrawingBackend backend = Assert.IsType<SdlGpuDrawingBackend>(
                fixture.Session.DrawingBackend);

            Assert.Equal(0, backend.LastFrameTiming.TextRequestCount);
            Assert.Equal(0, backend.LastFrameTiming.RasterizedPixelCount);
        }
        finally
        {
            fixture.Session.CompleteFrame(present: false);
        }
    }

    [Fact]
    public void FailedSubmitTextAtlasUploadMustBeUploadedAgain()
    {
        using SessionFixture fixture = new();
        TextAtlasKeySet keys = CreateTextAtlasKeySet("failed submit");
        fixture.Api.EnqueueSubmitResults(false);

        fixture.Session.BeginFrame(Color.Transparent);
        StageTextAtlasUpload(fixture.Session, keys);
        int uploadEventIndex = fixture.Api.GpuTextureUploads.Count;
        Assert.True(HasTextAtlasUploadAfter(fixture.Api, 0));
        Assert.Throws<InvalidOperationException>(
            () => fixture.Session.CompleteFrame(present: false));

        fixture.Session.BeginFrame(Color.Transparent);
        try
        {
            StageTextAtlasUpload(fixture.Session, keys);

            Assert.True(
                HasTextAtlasUploadAfter(fixture.Api, uploadEventIndex),
                "A text-atlas upload consumed by a failed submit must be uploaded again.");
        }
        finally
        {
            fixture.Session.CompleteFrame(present: false);
        }
    }

    [Fact]
    public void SuccessfullySubmittedTextAtlasUploadIsReusedWithoutAnotherUpload()
    {
        using SessionFixture fixture = new();
        TextAtlasKeySet keys = CreateTextAtlasKeySet("submitted");

        fixture.Session.BeginFrame(Color.Transparent);
        StageTextAtlasUpload(fixture.Session, keys);
        int uploadEventIndex = fixture.Api.GpuTextureUploads.Count;
        Assert.True(HasTextAtlasUploadAfter(fixture.Api, 0));
        fixture.Session.CompleteFrame(present: false);

        fixture.Session.BeginFrame(Color.Transparent);
        try
        {
            StageTextAtlasUpload(fixture.Session, keys);

            Assert.False(HasTextAtlasUploadAfter(fixture.Api, uploadEventIndex));
        }
        finally
        {
            fixture.Session.CompleteFrame(present: false);
        }
    }

    [Fact]
    public void PendingTextAtlasUploadCannotCrossInterleavedSessions()
    {
        using SessionFixture fixture = new();
        using SdlGpuWindowGraphicsSession second = fixture.CreateAdditionalSession("second text atlas");
        Assert.Same(fixture.Session.DrawingResources, second.DrawingResources);
        TextAtlasKeySet keys = CreateTextAtlasKeySet("interleaved");

        fixture.Session.BeginFrame(Color.Transparent);
        StageTextAtlasUpload(fixture.Session, keys);
        int uploadEventIndex = fixture.Api.GpuTextureUploads.Count;
        Assert.True(HasTextAtlasUploadAfter(fixture.Api, 0));

        second.BeginFrame(Color.Transparent);
        try
        {
            StageTextAtlasUpload(second, keys);

            Assert.True(
                HasTextAtlasUploadAfter(fixture.Api, uploadEventIndex),
                "A text-atlas upload pending in another session cannot be a cross-window cache hit.");
        }
        finally
        {
            second.CompleteFrame(present: false);
            fixture.Session.CompleteFrame(present: false);
        }
    }

    [Fact]
    public void AbandonedTextAtlasUploadMustBeUploadedAgainByAnotherSession()
    {
        using SessionFixture fixture = new();
        using SdlGpuWindowGraphicsSession second = fixture.CreateAdditionalSession("text atlas recovery");
        Assert.Same(fixture.Session.DrawingResources, second.DrawingResources);
        TextAtlasKeySet keys = CreateTextAtlasKeySet("abandoned");

        fixture.Session.BeginFrame(Color.Transparent);
        StageTextAtlasUpload(fixture.Session, keys);
        int uploadEventIndex = fixture.Api.GpuTextureUploads.Count;
        Assert.True(HasTextAtlasUploadAfter(fixture.Api, 0));
        fixture.Session.Dispose();

        second.BeginFrame(Color.Transparent);
        try
        {
            StageTextAtlasUpload(second, keys);

            Assert.True(
                HasTextAtlasUploadAfter(fixture.Api, uploadEventIndex),
                "A text-atlas upload recorded only by an abandoned buffer must be uploaded again.");
        }
        finally
        {
            second.CompleteFrame(present: false);
        }
    }

    [Fact]
    public void TextAtlasEntriesUsedByPendingBufferStayPinnedAfterAtlasFrameEnds()
    {
        using SessionFixture fixture = new();
        using SdlGpuWindowGraphicsSession second = fixture.CreateAdditionalSession("text atlas eviction pressure");
        SdlGpuDrawingResources resources = fixture.Session.DrawingResources;
        TextAtlasKeySet retainedKeys = CreateTextAtlasKeySet("pending retained atlas entries");
        RasterizedText fullPageLayer = CreateFullPageTextLayer();

        fixture.Session.BeginFrame(Color.Transparent);
        StageTextAtlasUpload(fixture.Session, retainedKeys, fullPageLayer);
        fixture.Session.CompleteFrame(present: false);

        fixture.Session.BeginFrame(Color.Transparent);
        try
        {
            long pendingFrame = resources.BeginTextAtlasFrame();
            try
            {
                Assert.True(resources.GetOrCreateTextAtlasEntries(
                    fixture.Session,
                    retainedKeys.Red,
                    retainedKeys.Green,
                    retainedKeys.Blue,
                    [fullPageLayer, fullPageLayer, fullPageLayer],
                    pendingFrame).HasValue);
            }
            finally
            {
                resources.EndTextAtlasFrame(pendingFrame);
            }

            for (int index = 0; index < 2; index++)
            {
                second.BeginFrame(Color.Transparent);
                StageTextAtlasUpload(
                    second,
                    CreateTextAtlasKeySet($"eviction pressure {index}"),
                    fullPageLayer);
                second.CompleteFrame(present: false);
            }

            Assert.True(
                resources.TryGetTextAtlasEntries(
                    retainedKeys.Red,
                    retainedKeys.Green,
                    retainedKeys.Blue,
                    frameToken: 0,
                    out _),
                "Ending the atlas frame cannot unpin entries still referenced by a pending command buffer.");
        }
        finally
        {
            fixture.Session.CompleteFrame(present: false);
        }
    }

    [Theory]
    [InlineData(TextAtlasSubmitOrder.OlderThenNewer)]
    [InlineData(TextAtlasSubmitOrder.NewerThenOlder)]
    [InlineData(TextAtlasSubmitOrder.AbandonOlderThenSubmitNewer)]
    public void OlderTextAtlasUploadCannotOverwriteLaterReusedEntry(
        TextAtlasSubmitOrder submitOrder)
    {
        using SessionFixture fixture = new();
        SdlGpuDrawingResources resources = fixture.Session.DrawingResources;
        TextAtlasKeySet seedKeys = CreateTextAtlasKeySet("revision-order seed");

        fixture.Session.BeginFrame(Color.Transparent);
        StageTextAtlasUpload(fixture.Session, seedKeys);
        fixture.Session.CompleteFrame(present: false);
        Assert.True(resources.TryGetTextAtlasEntries(
            seedKeys.Red,
            seedKeys.Green,
            seedKeys.Blue,
            frameToken: 0,
            out SdlGpuTextAtlasEntries seedEntries));
        SdlGpuTextAtlasPage page = seedEntries.Red.Page;

        fixture.Session.BeginFrame(Color.Transparent);
        SdlGpuTextAtlasEntry leftPlaceholder = AllocateDirectAtlasEntry(page, 8, 8, 0x11);
        SdlGpuTextAtlasEntry middle = AllocateDirectAtlasEntry(page, 8, 8, 0x22);
        SdlGpuTextAtlasEntry rightPlaceholder = AllocateDirectAtlasEntry(page, 8, 8, 0x33);
        page.RequireUpload(
            fixture.Session.ActiveCommandBufferToken,
            rightPlaceholder.CreatedRevision);
        resources.FlushTextAtlasUploads(fixture.Session);
        fixture.Session.CompleteFrame(present: false);
        page.Free(leftPlaceholder);
        page.Free(rightPlaceholder);

        using SdlGpuWindowGraphicsSession older = fixture.CreateAdditionalSession(
            $"older atlas {submitOrder}");
        using SdlGpuWindowGraphicsSession newer = fixture.CreateAdditionalSession(
            $"newer atlas {submitOrder}");
        fixture.Api.CaptureGpuTextureUploadSnapshots = true;

        older.BeginFrame(Color.Transparent);
        SdlGpuTextAtlasEntry left = AllocateDirectAtlasEntry(page, 8, 8, 0x44);
        SdlGpuTextAtlasEntry right = AllocateDirectAtlasEntry(page, 8, 8, 0x66);
        Assert.Equal(leftPlaceholder.TextureCoordinates, left.TextureCoordinates);
        Assert.Equal(rightPlaceholder.TextureCoordinates, right.TextureCoordinates);
        page.RequireUpload(older.ActiveCommandBufferToken, right.CreatedRevision);
        int olderUploadStart = fixture.Api.GpuTextureUploadSnapshots.Count;
        resources.FlushTextAtlasUploads(older);
        FakeSdlApi.FakeGpuTextureUpload[] olderUploads =
            fixture.Api.GpuTextureUploadSnapshots.Skip(olderUploadStart).ToArray();
        Assert.NotEmpty(olderUploads);

        newer.BeginFrame(Color.Transparent);
        page.Free(middle);
        SdlGpuTextAtlasEntry replacement = AllocateDirectAtlasEntry(page, 8, 8, 0xA7);
        Assert.Equal(middle.TextureCoordinates, replacement.TextureCoordinates);
        page.RequireUpload(newer.ActiveCommandBufferToken, replacement.CreatedRevision);
        int newerUploadStart = fixture.Api.GpuTextureUploadSnapshots.Count;
        resources.FlushTextAtlasUploads(newer);
        FakeSdlApi.FakeGpuTextureUpload[] newerUploads =
            fixture.Api.GpuTextureUploadSnapshots.Skip(newerUploadStart).ToArray();
        Assert.NotEmpty(newerUploads);

        SdlRect olderEnvelope = GetUploadEnvelope(olderUploads);
        SdlRect replacementRegion = GetAtlasEntryRegion(replacement);
        Assert.True(Contains(olderEnvelope, replacementRegion));

        byte[] submittedPixels = new byte[1024 * 1024 * 4];
        switch (submitOrder)
        {
            case TextAtlasSubmitOrder.OlderThenNewer:
                older.CompleteFrame(present: false);
                ApplyTextureUploads(submittedPixels, 1024, olderUploads);
                newer.CompleteFrame(present: false);
                ApplyTextureUploads(submittedPixels, 1024, newerUploads);
                break;
            case TextAtlasSubmitOrder.NewerThenOlder:
                newer.CompleteFrame(present: false);
                ApplyTextureUploads(submittedPixels, 1024, newerUploads);
                older.CompleteFrame(present: false);
                ApplyTextureUploads(submittedPixels, 1024, olderUploads);
                break;
            case TextAtlasSubmitOrder.AbandonOlderThenSubmitNewer:
                older.Dispose();
                newer.CompleteFrame(present: false);
                ApplyTextureUploads(submittedPixels, 1024, newerUploads);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(submitOrder));
        }

        AssertAtlasRegionEquals(submittedPixels, 1024, replacementRegion, 0xA7);
    }

    [Fact]
    public void OlderTextAtlasUploadPinsEveryCachedEntryCoveredByItsWrites()
    {
        using SessionFixture fixture = new();
        SdlGpuDrawingResources resources = fixture.Session.DrawingResources;
        TextAtlasKeySet seedKeys = CreateTextAtlasKeySet("covered-entry seed");

        fixture.Session.BeginFrame(Color.Transparent);
        StageTextAtlasUpload(fixture.Session, seedKeys);
        fixture.Session.CompleteFrame(present: false);
        Assert.True(resources.TryGetTextAtlasEntries(
            seedKeys.Red,
            seedKeys.Green,
            seedKeys.Blue,
            frameToken: 0,
            out SdlGpuTextAtlasEntries seedEntries));
        SdlGpuTextAtlasPage page = seedEntries.Red.Page;

        SdlGpuTextAtlasEntry leftPlaceholder = AllocateDirectAtlasSlot(page, 8, 8);
        TextAtlasKeySet middleKeys = CreateTextAtlasKeySet("unsubmitted covered middle");
        RasterizedText middleLayer = CreateSolidTextLayer(8, 8, 0x22);
        SdlGpuTextAtlasEntries middleEntries;
        using (SdlGpuWindowGraphicsSession middleProducer =
            fixture.CreateAdditionalSession("abandoned middle producer"))
        {
            middleProducer.BeginFrame(Color.Transparent);
            long middleFrame = resources.BeginTextAtlasFrame();
            try
            {
                middleEntries = Assert.IsType<SdlGpuTextAtlasEntries>(
                    resources.GetOrCreateTextAtlasEntries(
                        middleProducer,
                        middleKeys.Red,
                        middleKeys.Green,
                        middleKeys.Blue,
                        [middleLayer, middleLayer, middleLayer],
                        middleFrame));
            }
            finally
            {
                resources.EndTextAtlasFrame(middleFrame);
            }
            middleProducer.Dispose();
        }
        Assert.Equal(0, middleEntries.Red.ActiveFrameCount);
        Assert.Equal(0, middleEntries.Green.ActiveFrameCount);
        Assert.Equal(0, middleEntries.Blue.ActiveFrameCount);

        SdlGpuTextAtlasEntry rightPlaceholder = AllocateDirectAtlasSlot(page, 8, 8);
        SdlRect rightRegion = GetAtlasEntryRegion(rightPlaceholder);
        int paddedRight = rightRegion.X + rightRegion.Width + 1;
        _ = AllocateDirectAtlasSlot(page, 1024 - paddedRight - 2, 8);
        int shelfBottom = rightRegion.Y + rightRegion.Height + 1;
        _ = AllocateDirectAtlasSlot(page, 1022, 1024 - shelfBottom - 2);

        RasterizedText fullPageLayer = CreateFullPageTextLayer();
        for (int index = 0; index < 2; index++)
        {
            long frameToken = resources.BeginTextAtlasFrame();
            try
            {
                TextAtlasKeySet keys = CreateTextAtlasKeySet($"covered pressure page {index}");
                Assert.True(resources.GetOrCreateTextAtlasEntries(
                    fixture.Session,
                    keys.Red,
                    keys.Green,
                    keys.Blue,
                    [fullPageLayer, fullPageLayer, fullPageLayer],
                    frameToken).HasValue);
            }
            finally
            {
                resources.EndTextAtlasFrame(frameToken);
            }
        }
        long finalPageFrame = resources.BeginTextAtlasFrame();
        try
        {
            TextAtlasKeySet keys = CreateTextAtlasKeySet("covered final pressure page");
            RasterizedText oversized = CreateSolidTextLayer(1023, 1, 0x00);
            Assert.Null(resources.GetOrCreateTextAtlasEntries(
                fixture.Session,
                keys.Red,
                keys.Green,
                keys.Blue,
                [fullPageLayer, oversized, oversized],
                finalPageFrame));
        }
        finally
        {
            resources.EndTextAtlasFrame(finalPageFrame);
        }
        Assert.Equal(8, resources.TextAtlasPageCount);
        Assert.True(resources.TryGetTextAtlasEntries(
            seedKeys.Red,
            seedKeys.Green,
            seedKeys.Blue,
            frameToken: 0,
            out _));

        page.Free(leftPlaceholder);
        page.Free(rightPlaceholder);
        using SdlGpuWindowGraphicsSession older = fixture.CreateAdditionalSession(
            "older covered upload");
        using SdlGpuWindowGraphicsSession newer = fixture.CreateAdditionalSession(
            "newer covered replacement");
        fixture.Api.CaptureGpuTextureUploadSnapshots = true;

        older.BeginFrame(Color.Transparent);
        _ = AllocateDirectAtlasEntry(page, 8, 8, 0x44);
        SdlGpuTextAtlasEntry right = AllocateDirectAtlasEntry(page, 8, 8, 0x66);
        page.RequireUpload(older.ActiveCommandBufferToken, right.CreatedRevision);
        int olderUploadStart = fixture.Api.GpuTextureUploadSnapshots.Count;
        resources.FlushTextAtlasUploads(older);
        FakeSdlApi.FakeGpuTextureUpload[] olderUploads =
            fixture.Api.GpuTextureUploadSnapshots.Skip(olderUploadStart).ToArray();
        Assert.Contains(olderUploads, upload =>
            upload.Destination.Texture == page.Texture.Handle &&
            Contains(
                new SdlRect(
                    checked((int)upload.Destination.X),
                    checked((int)upload.Destination.Y),
                    checked((int)upload.Destination.Width),
                    checked((int)upload.Destination.Height)),
                GetAtlasEntryRegion(middleEntries.Green)));
        bool middleWasProtected = middleEntries.Red.ActiveFrameCount > 0 &&
            middleEntries.Green.ActiveFrameCount > 0 &&
            middleEntries.Blue.ActiveFrameCount > 0;

        newer.BeginFrame(Color.Transparent);
        TextAtlasKeySet replacementKeys = CreateTextAtlasKeySet("covered replacement");
        RasterizedText replacementLayer = CreateSolidTextLayer(8, 8, 0xA7);
        long replacementFrame = resources.BeginTextAtlasFrame();
        SdlGpuTextAtlasEntries replacementEntries;
        try
        {
            replacementEntries = Assert.IsType<SdlGpuTextAtlasEntries>(
                resources.GetOrCreateTextAtlasEntries(
                    newer,
                    replacementKeys.Red,
                    replacementKeys.Green,
                    replacementKeys.Blue,
                    [replacementLayer, replacementLayer, replacementLayer],
                    replacementFrame));
        }
        finally
        {
            resources.EndTextAtlasFrame(replacementFrame);
        }
        int newerUploadStart = fixture.Api.GpuTextureUploadSnapshots.Count;
        resources.FlushTextAtlasUploads(newer);
        FakeSdlApi.FakeGpuTextureUpload[] newerUploads =
            fixture.Api.GpuTextureUploadSnapshots.Skip(newerUploadStart).ToArray();
        bool middleStayedCached = resources.TryGetTextAtlasEntries(
            middleKeys.Red,
            middleKeys.Green,
            middleKeys.Blue,
            frameToken: 0,
            out _);

        newer.CompleteFrame(present: false);
        byte[] submittedPixels = new byte[1024 * 1024 * 4];
        ApplyTextureUploads(
            submittedPixels,
            1024,
            newerUploads.Where(upload =>
                upload.Destination.Texture == replacementEntries.Green.Texture.Handle));
        older.CompleteFrame(present: false);
        ApplyTextureUploads(
            submittedPixels,
            1024,
            olderUploads.Where(upload =>
                upload.Destination.Texture == replacementEntries.Green.Texture.Handle));

        AssertAtlasRegionEquals(
            submittedPixels,
            1024,
            GetAtlasEntryRegion(replacementEntries.Green),
            0xA7);
        Assert.True(middleWasProtected);
        Assert.True(middleStayedCached);
        Assert.Equal(0, middleEntries.Red.ActiveFrameCount);
        Assert.Equal(0, middleEntries.Green.ActiveFrameCount);
        Assert.Equal(0, middleEntries.Blue.ActiveFrameCount);
    }

    [Fact]
    public void OlderTextAtlasUploadCannotOverwriteAllocationInFreedDirtyTail()
    {
        using SessionFixture fixture = new();
        SdlGpuDrawingResources resources = fixture.Session.DrawingResources;
        TextAtlasKeySet seedKeys = CreateTextAtlasKeySet("freed dirty tail seed");

        fixture.Session.BeginFrame(Color.Transparent);
        StageTextAtlasUpload(fixture.Session, seedKeys);
        fixture.Session.CompleteFrame(present: false);
        Assert.True(resources.TryGetTextAtlasEntries(
            seedKeys.Red,
            seedKeys.Green,
            seedKeys.Blue,
            frameToken: 0,
            out SdlGpuTextAtlasEntries seedEntries));
        SdlGpuTextAtlasPage page = seedEntries.Red.Page;

        SdlGpuTextAtlasEntry marker = AllocateDirectAtlasSlot(page, 8, 8);
        TextAtlasKeySet largeKeys = CreateTextAtlasKeySet("abandoned large dirty entry");
        RasterizedText largeLayer = CreateSolidTextLayer(48, 8, 0x22);
        SdlGpuTextAtlasEntries largeEntries;
        using (SdlGpuWindowGraphicsSession largeProducer =
            fixture.CreateAdditionalSession("abandoned large dirty producer"))
        {
            largeProducer.BeginFrame(Color.Transparent);
            long largeFrame = resources.BeginTextAtlasFrame();
            try
            {
                largeEntries = Assert.IsType<SdlGpuTextAtlasEntries>(
                    resources.GetOrCreateTextAtlasEntries(
                        largeProducer,
                        largeKeys.Red,
                        largeKeys.Green,
                        largeKeys.Blue,
                        [largeLayer, largeLayer, largeLayer],
                        largeFrame));
            }
            finally
            {
                resources.EndTextAtlasFrame(largeFrame);
            }
            largeProducer.Dispose();
        }
        Assert.Equal(0, largeEntries.Red.ActiveFrameCount);
        Assert.Equal(0, largeEntries.Green.ActiveFrameCount);
        Assert.Equal(0, largeEntries.Blue.ActiveFrameCount);

        SdlRect largeRedRegion = GetAtlasEntryRegion(largeEntries.Red);
        SdlRect largeBlueRegion = GetAtlasEntryRegion(largeEntries.Blue);
        int paddedRight = largeBlueRegion.X + largeBlueRegion.Width + 1;
        _ = AllocateDirectAtlasSlot(page, 1024 - paddedRight - 2, 8);
        int shelfBottom = largeBlueRegion.Y + largeBlueRegion.Height + 1;
        _ = AllocateDirectAtlasSlot(page, 1022, 1024 - shelfBottom - 2);

        RasterizedText fullPageLayer = CreateFullPageTextLayer();
        for (int index = 0; index < 2; index++)
        {
            long frameToken = resources.BeginTextAtlasFrame();
            try
            {
                TextAtlasKeySet keys = CreateTextAtlasKeySet(
                    $"freed dirty tail pressure page {index}");
                Assert.True(resources.GetOrCreateTextAtlasEntries(
                    fixture.Session,
                    keys.Red,
                    keys.Green,
                    keys.Blue,
                    [fullPageLayer, fullPageLayer, fullPageLayer],
                    frameToken).HasValue);
            }
            finally
            {
                resources.EndTextAtlasFrame(frameToken);
            }
        }
        long finalPageFrame = resources.BeginTextAtlasFrame();
        try
        {
            TextAtlasKeySet keys = CreateTextAtlasKeySet(
                "freed dirty tail final pressure page");
            RasterizedText oversized = CreateSolidTextLayer(1023, 1, 0x00);
            Assert.Null(resources.GetOrCreateTextAtlasEntries(
                fixture.Session,
                keys.Red,
                keys.Green,
                keys.Blue,
                [fullPageLayer, oversized, oversized],
                finalPageFrame));
        }
        finally
        {
            resources.EndTextAtlasFrame(finalPageFrame);
        }
        Assert.Equal(8, resources.TextAtlasPageCount);
        Assert.True(resources.TryGetTextAtlasEntries(
            seedKeys.Red,
            seedKeys.Green,
            seedKeys.Blue,
            frameToken: 0,
            out _));

        TextAtlasKeySet smallerKeys = CreateTextAtlasKeySet(
            "smaller cached replacement");
        RasterizedText smallerLayer = CreateSolidTextLayer(8, 8, 0x44);
        long smallerFrame = resources.BeginTextAtlasFrame();
        SdlGpuTextAtlasEntries smallerEntries;
        try
        {
            smallerEntries = Assert.IsType<SdlGpuTextAtlasEntries>(
                resources.GetOrCreateTextAtlasEntries(
                    fixture.Session,
                    smallerKeys.Red,
                    smallerKeys.Green,
                    smallerKeys.Blue,
                    [smallerLayer, smallerLayer, smallerLayer],
                    smallerFrame));
        }
        finally
        {
            resources.EndTextAtlasFrame(smallerFrame);
        }
        Assert.False(resources.TryGetTextAtlasEntries(
            largeKeys.Red,
            largeKeys.Green,
            largeKeys.Blue,
            frameToken: 0,
            out _));
        Assert.Same(page, smallerEntries.Red.Page);
        Assert.True(Contains(largeRedRegion, GetAtlasEntryRegion(smallerEntries.Red)));
        Assert.True(Contains(largeRedRegion, GetAtlasEntryRegion(smallerEntries.Green)));
        Assert.True(Contains(largeRedRegion, GetAtlasEntryRegion(smallerEntries.Blue)));

        using SdlGpuWindowGraphicsSession older = fixture.CreateAdditionalSession(
            "older freed dirty tail upload");
        using SdlGpuWindowGraphicsSession newer = fixture.CreateAdditionalSession(
            "newer freed dirty tail allocation");
        fixture.Api.CaptureGpuTextureUploadSnapshots = true;

        older.BeginFrame(Color.Transparent);
        byte[] markerPixels = new byte[marker.Width * marker.Height * 4];
        Array.Fill(markerPixels, (byte)0x66);
        SdlRect markerRegion = GetAtlasEntryRegion(marker);
        long markerRevision = page.CopyPixels(
            markerRegion.X,
            markerRegion.Y,
            markerRegion.Width,
            markerRegion.Height,
            markerPixels);
        page.RequireUpload(older.ActiveCommandBufferToken, markerRevision);
        int olderUploadStart = fixture.Api.GpuTextureUploadSnapshots.Count;
        resources.FlushTextAtlasUploads(older);
        FakeSdlApi.FakeGpuTextureUpload[] olderUploads =
            fixture.Api.GpuTextureUploadSnapshots.Skip(olderUploadStart).ToArray();
        Assert.True(smallerEntries.Red.ActiveFrameCount > 0);
        Assert.True(smallerEntries.Green.ActiveFrameCount > 0);
        Assert.True(smallerEntries.Blue.ActiveFrameCount > 0);

        newer.BeginFrame(Color.Transparent);
        TextAtlasKeySet newerKeys = CreateTextAtlasKeySet(
            "new allocation in freed dirty tail");
        RasterizedText newerLayer = CreateSolidTextLayer(8, 8, 0xA7);
        long newerFrame = resources.BeginTextAtlasFrame();
        SdlGpuTextAtlasEntries newerEntries;
        try
        {
            newerEntries = Assert.IsType<SdlGpuTextAtlasEntries>(
                resources.GetOrCreateTextAtlasEntries(
                    newer,
                    newerKeys.Red,
                    newerKeys.Green,
                    newerKeys.Blue,
                    [newerLayer, newerLayer, newerLayer],
                    newerFrame));
        }
        finally
        {
            resources.EndTextAtlasFrame(newerFrame);
        }
        SdlRect reusedTailRegion = GetAtlasEntryRegion(newerEntries.Red);
        Assert.Same(page, newerEntries.Red.Page);
        Assert.True(Contains(largeRedRegion, reusedTailRegion));
        Assert.DoesNotContain(olderUploads, upload =>
            upload.Destination.Texture == page.Texture.Handle &&
            Contains(
                new SdlRect(
                    checked((int)upload.Destination.X),
                    checked((int)upload.Destination.Y),
                    checked((int)upload.Destination.Width),
                    checked((int)upload.Destination.Height)),
                reusedTailRegion));
        int newerUploadStart = fixture.Api.GpuTextureUploadSnapshots.Count;
        resources.FlushTextAtlasUploads(newer);
        FakeSdlApi.FakeGpuTextureUpload[] newerUploads =
            fixture.Api.GpuTextureUploadSnapshots.Skip(newerUploadStart).ToArray();

        newer.CompleteFrame(present: false);
        byte[] submittedPixels = new byte[1024 * 1024 * 4];
        ApplyTextureUploads(
            submittedPixels,
            1024,
            newerUploads.Where(upload => upload.Destination.Texture == page.Texture.Handle));
        older.CompleteFrame(present: false);
        ApplyTextureUploads(
            submittedPixels,
            1024,
            olderUploads.Where(upload => upload.Destination.Texture == page.Texture.Handle));

        AssertAtlasRegionEquals(submittedPixels, 1024, reusedTailRegion, 0xA7);
    }

    [Fact]
    public void GeometryBufferUploadsAreRecordedAgainAfterFailedSubmit()
    {
        using SessionFixture fixture = new();
        fixture.Api.CaptureGpuBufferUploads = true;
        fixture.Api.EnqueueSubmitResults(false);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.FillRectangle(
            new DrawRect(0, 0, 3, 5),
            Color.Coral));
        DrawingFrameContext frame = CreateFrame(commands);

        fixture.Session.BeginFrame(Color.Transparent);
        fixture.Session.DrawingBackend.Render(commands, in frame);
        int uploadEventIndex = fixture.Api.GpuBufferUploads.Count;
        Assert.True(uploadEventIndex > 0);
        Assert.Throws<InvalidOperationException>(
            () => fixture.Session.CompleteFrame(present: false));

        fixture.Session.BeginFrame(Color.Transparent);
        try
        {
            fixture.Session.DrawingBackend.Render(commands, in frame);

            Assert.True(
                fixture.Api.GpuBufferUploads.Count > uploadEventIndex,
                "Geometry is streamed per recording and must be uploaded again after failed submit.");
        }
        finally
        {
            fixture.Session.CompleteFrame(present: false);
        }
    }

    [Fact]
    public void FailedReadbackFenceSubmissionConsumesTheCommandBufferWithoutCancellation()
    {
        using SessionFixture fixture = new();
        fixture.Session.BeginFrame(Color.Transparent);
        fixture.Session.CompleteFrame(present: false);
        fixture.Api.EnqueueSubmitResults(false);

        Assert.Throws<InvalidOperationException>(() => fixture.Session.CapturePresentedFrame());

        FakeGpuCommandBufferAction action = Assert.Single(
            fixture.Api.GpuCommandBufferActions,
            candidate => candidate.Kind == FakeGpuCommandBufferActionKind.SubmitAndAcquireFence);
        Assert.False(action.Succeeded);
        Assert.DoesNotContain(
            fixture.Api.GpuCommandBufferActions,
            candidate => candidate.CommandBuffer == action.CommandBuffer &&
                candidate.Kind == FakeGpuCommandBufferActionKind.Cancel);
        Assert.Equal(0, fixture.Api.LiveCommandBufferCount);
    }

    [Fact]
    public void FailedReadbackFenceWaitDoesNotCancelTheSubmittedCommandBuffer()
    {
        using SessionFixture fixture = new();
        fixture.Session.BeginFrame(Color.Transparent);
        fixture.Session.CompleteFrame(present: false);
        fixture.Api.WaitFenceResult = false;

        Assert.Throws<InvalidOperationException>(() => fixture.Session.CapturePresentedFrame());

        FakeGpuCommandBufferAction action = Assert.Single(
            fixture.Api.GpuCommandBufferActions,
            candidate => candidate.Kind == FakeGpuCommandBufferActionKind.SubmitAndAcquireFence);
        Assert.True(action.Succeeded);
        Assert.DoesNotContain(
            fixture.Api.GpuCommandBufferActions,
            candidate => candidate.CommandBuffer == action.CommandBuffer &&
                candidate.Kind == FakeGpuCommandBufferActionKind.Cancel);
        Assert.Contains(fixture.Api.GpuActions, value => value.StartsWith("release-fence:", StringComparison.Ordinal));
        Assert.Equal(0, fixture.Api.LiveCommandBufferCount);
    }

    [Fact]
    public void ByteAndHalfVectorInputsSharePendingIsolationAndCommitRules()
    {
        using SessionFixture fixture = new();
        using SdlGpuWindowGraphicsSession second = fixture.CreateAdditionalSession("shared input second");
        SdlGpuDrawingResources resources = fixture.Session.DrawingResources;
        object byteKey = new();
        object halfKey = new();
        byte[] bytes = new byte[13 * 3 * 4];
        System.Numerics.Vector4[] half = new System.Numerics.Vector4[5 * 2];

        fixture.Session.BeginFrame(Color.Transparent);
        SdlGpuTextureResource firstBytes = resources.GetOrCreateTexture(
            fixture.Session, byteKey, 13, 3, bytes);
        SdlGpuTextureResource firstHalf = resources.GetOrCreateHalfVector4Texture(
            fixture.Session, halfKey, 5, 2, half);

        second.BeginFrame(Color.Transparent);
        SdlGpuTextureResource secondBytes = resources.GetOrCreateTexture(
            second, byteKey, 13, 3, bytes);
        SdlGpuTextureResource secondHalf = resources.GetOrCreateHalfVector4Texture(
            second, halfKey, 5, 2, half);
        Assert.NotEqual(firstBytes.Handle, secondBytes.Handle);
        Assert.NotEqual(firstHalf.Handle, secondHalf.Handle);

        fixture.Api.EnqueueSubmitResults(false, true);
        Assert.Throws<InvalidOperationException>(
            () => fixture.Session.CompleteFrame(present: false));
        second.CompleteFrame(present: false);
        int uploadCount = fixture.Api.GpuTextureUploads.Count;

        second.BeginFrame(Color.Transparent);
        Assert.Same(secondBytes, resources.GetOrCreateTexture(second, byteKey, 13, 3, bytes));
        Assert.Same(secondHalf, resources.GetOrCreateHalfVector4Texture(second, halfKey, 5, 2, half));
        second.CompleteFrame(present: false);

        Assert.Equal(uploadCount, fixture.Api.GpuTextureUploads.Count);
        Assert.Equal(0, fixture.Api.LiveCommandBufferCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InvalidatedTextureIsNotReleasedBeforeItsCommandBufferEnds(
        bool submitBeforeUse)
    {
        using SessionFixture fixture = new();
        SdlGpuDrawingResources resources = fixture.Session.DrawingResources;
        object key = new();
        byte[] pixels = new byte[7 * 5 * 4];

        if (submitBeforeUse)
        {
            fixture.Session.BeginFrame(Color.Transparent);
            _ = resources.GetOrCreateTexture(
                fixture.Session,
                key,
                7,
                5,
                pixels,
                recycleStorage: true);
            fixture.Session.CompleteFrame(present: false);
        }

        fixture.Session.BeginFrame(Color.Transparent);
        SdlGpuTextureResource pending = resources.GetOrCreateTexture(
            fixture.Session,
            key,
            7,
            5,
            pixels,
            recycleStorage: true);
        resources.InvalidateTexture(key);
        resources.FlushRetired();

        Assert.Null(resources.FindTexture(fixture.Session, key));
        Assert.Contains(pending.Handle, fixture.Api.GpuTextures.Keys);

        fixture.Session.CompleteFrame(present: false);
        resources.FlushRetired();

        Assert.DoesNotContain(pending.Handle, fixture.Api.GpuTextures.Keys);
        Assert.Equal(0, fixture.Api.LiveCommandBufferCount);
    }

    [Fact]
    public void HundredDeterministicSubmitAbandonAndRecoveryCyclesReleaseLocalHandles()
    {
        SessionFixture fixture = new();
        FakeSdlApi api = fixture.Api;
        try
        {
            for (int iteration = 0; iteration < 100; iteration++)
            {
                byte[] pixels = new byte[7 * 5 * 4];
                pixels[0] = checked((byte)iteration);
                SdlGpuImage image = new(7, 5, pixels);
                DrawCommandList commands = CreateImageCommands(image, 7, 5);
                DrawingFrameContext frame = CreateFrame(commands);
                switch (iteration % 4)
                {
                    case 0:
                        fixture.Session.BeginFrame(Color.Transparent);
                        fixture.Session.DrawingBackend.Render(commands, in frame);
                        fixture.Session.CompleteFrame(present: false);
                        break;
                    case 1:
                        fixture.Session.BeginFrame(Color.Transparent);
                        fixture.Session.DrawingBackend.Render(commands, in frame);
                        api.EnqueueSubmitResults(false);
                        Assert.Throws<InvalidOperationException>(
                            () => fixture.Session.CompleteFrame(present: false));
                        break;
                    case 2:
                        using (SdlGpuWindowGraphicsSession abandoned =
                            fixture.CreateAdditionalSession($"abandon-{iteration}"))
                        {
                            abandoned.BeginFrame(Color.Transparent);
                            abandoned.DrawingBackend.Render(commands, in frame);
                        }
                        break;
                    default:
                        fixture.Session.BeginFrame(Color.Transparent);
                        fixture.Session.DrawingBackend.Render(commands, in frame);
                        api.EnqueueSwapchainAcquireResults(false, true);
                        api.EnqueueSubmitResults(true, true);
                        fixture.Session.Present();
                        break;
                }

                Assert.Equal(0, api.LiveCommandBufferCount);
            }

            fixture.Session.DrawingResources.FlushRetired();
        }
        finally
        {
            fixture.Dispose();
        }

        Assert.Equal(0, api.LiveCommandBufferCount);
        Assert.Empty(api.GpuTextures);
        Assert.Empty(api.TransferBuffers);
        Assert.Empty(api.GpuBuffers);
    }

    [Fact]
    public void HundredInterleavedSessionOutcomesDoNotCrossPublishInputs()
    {
        using SessionFixture fixture = new();
        using SdlGpuWindowGraphicsSession second = fixture.CreateAdditionalSession("interleaved stress");
        SdlGpuDrawingResources resources = fixture.Session.DrawingResources;

        for (int iteration = 0; iteration < 100; iteration++)
        {
            object key = new();
            byte[] pixels = new byte[7 * 5 * 4];
            fixture.Session.BeginFrame(Color.Transparent);
            SdlGpuTextureResource first = resources.GetOrCreateTexture(
                fixture.Session, key, 7, 5, pixels);
            second.BeginFrame(Color.Transparent);
            SdlGpuTextureResource other = resources.GetOrCreateTexture(
                second, key, 7, 5, pixels);
            Assert.NotEqual(first.Handle, other.Handle);

            bool firstSucceeds = iteration % 2 == 0;
            bool secondSucceeds = iteration % 3 != 0;
            fixture.Api.EnqueueSubmitResults(firstSucceeds, secondSucceeds);
            if (firstSucceeds)
            {
                fixture.Session.CompleteFrame(present: false);
            }
            else
            {
                Assert.Throws<InvalidOperationException>(
                    () => fixture.Session.CompleteFrame(present: false));
            }
            if (secondSucceeds)
            {
                second.CompleteFrame(present: false);
            }
            else
            {
                Assert.Throws<InvalidOperationException>(
                    () => second.CompleteFrame(present: false));
            }

            Assert.Equal(0, fixture.Api.LiveCommandBufferCount);
            if (firstSucceeds || secondSucceeds)
            {
                SdlGpuWindowGraphicsSession committed = secondSucceeds ? second : fixture.Session;
                committed.BeginFrame(Color.Transparent);
                int uploadCount = fixture.Api.GpuTextureUploads.Count;
                _ = resources.GetOrCreateTexture(committed, key, 7, 5, pixels);
                committed.CompleteFrame(present: false);
                Assert.Equal(uploadCount, fixture.Api.GpuTextureUploads.Count);
            }
        }

        Assert.Equal(0, fixture.Api.LiveCommandBufferCount);
    }

    private static DrawCommandList CreatePrismParentWithExisting2DContent()
    {
        DrawRect bounds = new(0, 0, 8, 6);
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "Failed-submit parent capture",
                PrismTestData.Layer(1, "Parent")),
            ownerToken: 96001,
            bounds: bounds);
        return PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(bounds, Color.Coral),
            DrawCommand.EndPrism());
    }

    private static DrawCommandList CreateImageCommands(
        SdlGpuImage image,
        int width,
        int height)
    {
        DrawCommandList commands = new();
        commands.Add(DrawCommand.DrawImage(
            image,
            new DrawRect(0, 0, width, height),
            Color.White));
        return commands;
    }

    private static DrawCommandList CreateTextCommands(string text, float size = 16) =>
        CreateTextScenario(text, size).Commands;

    private static TextCommandScenario CreateTextScenario(string text, float size = 16)
    {
        IDrawFont font = new SystemFontSource().LoadFont("Arial", size);
        DrawPoint baseline = new(1.125f, size + 0.375f);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.DrawText(
            new DrawTextRun(font, text, size),
            baseline,
            Color.White));
        SdlGpuTextRasterKey raster = new(
            font is SkiaFont skiaFont ? skiaFont.Typeface : font,
            text,
            size,
            CoordinateScale: 1,
            PixelPhase: new DrawPoint(0.125f, 0.375f));
        return new TextCommandScenario(
            commands,
            new TextAtlasKeySet(
                new SdlGpuTextLayerTextureKey(raster, SdlGpuColorWriteMask.Red),
                new SdlGpuTextLayerTextureKey(raster, SdlGpuColorWriteMask.Green),
                new SdlGpuTextLayerTextureKey(raster, SdlGpuColorWriteMask.Blue)));
    }

    private static TextAtlasKeySet CreateTextAtlasKeySet(string text)
    {
        SdlGpuTextRasterKey raster = new(
            new object(),
            text,
            Size: 8,
            CoordinateScale: 1,
            PixelPhase: new DrawPoint(0.125f, 0.375f));
        return new TextAtlasKeySet(
            new SdlGpuTextLayerTextureKey(raster, SdlGpuColorWriteMask.Red),
            new SdlGpuTextLayerTextureKey(raster, SdlGpuColorWriteMask.Green),
            new SdlGpuTextLayerTextureKey(raster, SdlGpuColorWriteMask.Blue));
    }

    private static void StageTextAtlasUpload(
        SdlGpuWindowGraphicsSession session,
        TextAtlasKeySet keys)
    {
        const int width = 11;
        const int height = 7;
        byte[] pixels = new byte[width * height * 4];
        Array.Fill(pixels, byte.MaxValue);
        RasterizedText layer = new(
            width,
            height,
            pixels,
            new TextShapeResult("x", 1));
        SdlGpuDrawingResources resources = session.DrawingResources;
        long token = resources.BeginTextAtlasFrame();
        try
        {
            SdlGpuTextAtlasEntries? entries = resources.GetOrCreateTextAtlasEntries(
                session,
                keys.Red,
                keys.Green,
                keys.Blue,
                [layer, layer, layer],
                token);
            Assert.True(entries.HasValue);
            resources.FlushTextAtlasUploads(session);
        }
        finally
        {
            resources.EndTextAtlasFrame(token);
        }
    }

    private static void StageTextAtlasUpload(
        SdlGpuWindowGraphicsSession session,
        TextAtlasKeySet keys,
        RasterizedText layer)
    {
        SdlGpuDrawingResources resources = session.DrawingResources;
        long token = resources.BeginTextAtlasFrame();
        try
        {
            Assert.True(resources.GetOrCreateTextAtlasEntries(
                session,
                keys.Red,
                keys.Green,
                keys.Blue,
                [layer, layer, layer],
                token).HasValue);
            resources.FlushTextAtlasUploads(session);
        }
        finally
        {
            resources.EndTextAtlasFrame(token);
        }
    }

    private static RasterizedText CreateFullPageTextLayer()
    {
        const int dimension = 1022;
        return new RasterizedText(
            dimension,
            dimension,
            new byte[dimension * dimension * 4],
            new TextShapeResult("full page", dimension));
    }

    private static RasterizedText CreateSquareTextLayer(int dimension) =>
        new(
            dimension,
            dimension,
            new byte[dimension * dimension * 4],
            new TextShapeResult("pressure", dimension));

    private static RasterizedText CreateSolidTextLayer(
        int width,
        int height,
        byte value)
    {
        byte[] pixels = new byte[width * height * 4];
        Array.Fill(pixels, value);
        return new RasterizedText(
            width,
            height,
            pixels,
            new TextShapeResult("solid", width));
    }

    private static SdlGpuTextAtlasEntry AllocateDirectAtlasSlot(
        SdlGpuTextAtlasPage page,
        int width,
        int height)
    {
        Assert.True(page.TryAllocate(width, height, out int x, out int y));
        return CreateDirectAtlasEntry(page, x, y, width, height, createdRevision: 0);
    }

    private static SdlGpuTextAtlasEntry AllocateDirectAtlasEntry(
        SdlGpuTextAtlasPage page,
        int width,
        int height,
        byte value)
    {
        Assert.True(page.TryAllocate(width, height, out int x, out int y));
        byte[] pixels = new byte[width * height * 4];
        Array.Fill(pixels, value);
        long revision = page.CopyPixels(x, y, width, height, pixels);
        return CreateDirectAtlasEntry(page, x, y, width, height, revision);
    }

    private static SdlGpuTextAtlasEntry CreateDirectAtlasEntry(
        SdlGpuTextAtlasPage page,
        int x,
        int y,
        int width,
        int height,
        long createdRevision)
    {
        return new SdlGpuTextAtlasEntry(
            page.Texture,
            new DrawRect(x / 1024f, y / 1024f, width / 1024f, height / 1024f),
            width,
            height,
            default,
            page,
            new SdlGpuTextLayerTextureKey(
                new SdlGpuTextRasterKey(
                    new object(),
                    $"direct-{createdRevision}-{x}-{y}",
                    8,
                    1,
                    default),
                SdlGpuColorWriteMask.Red),
            createdRevision);
    }

    private static SdlRect GetAtlasEntryRegion(SdlGpuTextAtlasEntry entry) => new(
        (int)(entry.TextureCoordinates.X * 1024),
        (int)(entry.TextureCoordinates.Y * 1024),
        entry.Width,
        entry.Height);

    private static SdlRect GetUploadEnvelope(
        IReadOnlyList<FakeSdlApi.FakeGpuTextureUpload> uploads)
    {
        int left = uploads.Min(upload => checked((int)upload.Destination.X));
        int top = uploads.Min(upload => checked((int)upload.Destination.Y));
        int right = uploads.Max(upload => checked((int)(upload.Destination.X + upload.Destination.Width)));
        int bottom = uploads.Max(upload => checked((int)(upload.Destination.Y + upload.Destination.Height)));
        return new SdlRect(left, top, right - left, bottom - top);
    }

    private static bool Contains(SdlRect outer, SdlRect inner) =>
        inner.X >= outer.X &&
        inner.Y >= outer.Y &&
        inner.X + inner.Width <= outer.X + outer.Width &&
        inner.Y + inner.Height <= outer.Y + outer.Height;

    private static void ApplyTextureUploads(
        byte[] texturePixels,
        int textureWidth,
        IEnumerable<FakeSdlApi.FakeGpuTextureUpload> uploads)
    {
        foreach (FakeSdlApi.FakeGpuTextureUpload upload in uploads)
        {
            int width = checked((int)upload.Destination.Width);
            int height = checked((int)upload.Destination.Height);
            int rowLength = checked(width * 4);
            for (int row = 0; row < height; row++)
            {
                upload.Pixels.AsSpan(row * rowLength, rowLength).CopyTo(
                    texturePixels.AsSpan(
                        checked((((int)upload.Destination.Y + row) * textureWidth +
                            (int)upload.Destination.X) * 4),
                        rowLength));
            }
        }
    }

    private static void AssertAtlasRegionEquals(
        byte[] texturePixels,
        int textureWidth,
        SdlRect region,
        byte expected)
    {
        for (int y = region.Y; y < region.Y + region.Height; y++)
        {
            for (int x = region.X; x < region.X + region.Width; x++)
            {
                int offset = checked(((y * textureWidth) + x) * 4);
                Assert.Equal(expected, texturePixels[offset]);
                Assert.Equal(expected, texturePixels[offset + 1]);
                Assert.Equal(expected, texturePixels[offset + 2]);
                Assert.Equal(expected, texturePixels[offset + 3]);
            }
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static WeakReference RenderAndDisposeTransientSurfaceSession(
        SessionFixture fixture,
        RenderSurface2D surface,
        DrawCommandList commands,
        DrawingFrameContext frameContext,
        int iteration,
        out nint target)
    {
        SdlGpuWindowGraphicsSession? transient = fixture.CreateAdditionalSession(
            $"transient surface {iteration}");
        WeakReference reference = new(transient);
        try
        {
            transient.BeginFrame(Color.Transparent);
            transient.DrawingBackend.Render(commands, in frameContext);
            target = GetLastRenderTargetTextureForSize(fixture.Api, 3, 5);
            transient.CompleteFrame(present: false);
        }
        finally
        {
            transient.Dispose();
            transient = null;
        }
        return reference;
    }

    private static int GetTrackedRenderSurfaceStateCacheCount(SdlGpuDrawingBackend backend)
    {
        System.Reflection.FieldInfo field = typeof(SdlGpuDrawingBackend).GetField(
            "renderSurfaceStateCaches",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) ??
            throw new InvalidOperationException("Surface cache tracking field was not found.");
        object value = field.GetValue(backend) ??
            throw new InvalidOperationException("Surface cache tracking field was null.");
        System.Reflection.PropertyInfo count = value.GetType().GetProperty("Count") ??
            throw new InvalidOperationException("Surface cache tracking count was not found.");
        return Assert.IsType<int>(count.GetValue(value));
    }

    private static DrawCommandList CreatePrismParentWithSurface(
        IRenderSurface2DSource surface)
    {
        DrawRect bounds = new(0, 0, 8, 6);
        DrawRect surfaceBounds = new(0, 0, 3, 5);
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "Surface parent capture",
                PrismTestData.Layer(1, "Parent")),
            ownerToken: 96002,
            bounds: bounds);
        return PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.RenderSurface2D(surface, surfaceBounds, Color.White),
            DrawCommand.EndPrism());
    }

    private static DrawCommandList CreatePrismParentWithBrush(
        IDrawBrush brush,
        DrawRect bounds)
    {
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "Brush parent capture",
                PrismTestData.Layer(1, "Parent")),
            ownerToken: 96003,
            bounds: bounds);
        return PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(bounds, brush),
            DrawCommand.EndPrism());
    }

    private static DrawingFrameContext CreateFrame(DrawCommandList commands) =>
        new(new PrismFrameAnalyzer().Analyze(commands));

    private static bool HasRenderPassForTargetSizeAfter(
        FakeSdlApi api,
        int eventIndex,
        int width,
        int height) =>
        api.RenderTargets.Skip(eventIndex).Any(target =>
            api.GpuTextures.TryGetValue(target.Texture, out FakeSdlApi.FakeGpuTexture? texture) &&
            texture.CreateInfo.Width == width &&
            texture.CreateInfo.Height == height);

    private static nint GetLastRenderTargetTextureForSize(
        FakeSdlApi api,
        int width,
        int height) =>
        api.RenderTargets.Last(target =>
            api.GpuTextures.TryGetValue(target.Texture, out FakeSdlApi.FakeGpuTexture? texture) &&
            texture.CreateInfo.Width == width &&
            texture.CreateInfo.Height == height).Texture;

    private static bool HasTextureUploadForSizeAfter(
        FakeSdlApi api,
        int eventIndex,
        int width,
        int height) =>
        api.GpuTextureUploads.Skip(eventIndex).Any(upload =>
            upload.Destination.Width == width &&
            upload.Destination.Height == height);

    private static bool HasTextAtlasUploadAfter(FakeSdlApi api, int eventIndex) =>
        api.GpuTextureUploads.Skip(eventIndex).Any(upload =>
            api.GpuTextures.TryGetValue(
                upload.Destination.Texture,
                out FakeSdlApi.FakeGpuTexture? texture) &&
            texture.CreateInfo.Width == 1024 &&
            texture.CreateInfo.Height == 1024);

    private sealed class SessionFixture : IDisposable
    {
        private readonly SdlGpuWindowGraphicsSessionFactory factory;

        public SessionFixture()
        {
            Api = new FakeSdlApi();
            Api.SupportedTextureFormats.Add(SdlGpuTextureFormat.R16G16B16A16Float);
            nint window = Api.CreateWindow("frame transaction", 8, 6, SdlWindowOptions.Hidden);
            factory = new SdlGpuWindowGraphicsSessionFactory(Api, useMultisampling: false);
            Session = Assert.IsType<SdlGpuWindowGraphicsSession>(factory.Create(
                new SdlWindowSurface(window, Api.GetWindowId(window)),
                8,
                6,
                coordinateScale: 1));
        }

        public FakeSdlApi Api { get; }

        public SdlGpuWindowGraphicsSession Session { get; }

        public SdlGpuWindowGraphicsSession CreateAdditionalSession(string title)
        {
            nint window = Api.CreateWindow(title, 8, 6, SdlWindowOptions.Hidden);
            return Assert.IsType<SdlGpuWindowGraphicsSession>(factory.Create(
                new SdlWindowSurface(window, Api.GetWindowId(window)),
                8,
                6,
                coordinateScale: 1));
        }

        public void Dispose()
        {
            Session.Dispose();
            factory.Dispose();
        }
    }

    private sealed class InjectedScreenshotException : Exception;

    private sealed class InjectedSurfaceException : Exception;

    private sealed class InjectedBrushException : Exception;

    private readonly record struct TextAtlasKeySet(
        SdlGpuTextLayerTextureKey Red,
        SdlGpuTextLayerTextureKey Green,
        SdlGpuTextLayerTextureKey Blue);

    private readonly record struct TextCommandScenario(
        DrawCommandList Commands,
        TextAtlasKeySet Keys);

    public enum TextAtlasSubmitOrder
    {
        OlderThenNewer,
        NewerThenOlder,
        AbandonOlderThenSubmitNewer
    }

}
