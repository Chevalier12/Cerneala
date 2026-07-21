using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Markup;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Hosting;

public sealed class PrismBackdropHostingContractTests
{
    [Fact]
    public void BackdropContractsAreReadonlyAndBackendNeutral()
    {
        Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(IBackdropFrameLease)));
        Assert.All(
            typeof(BackdropFrameMetadata).GetProperties(),
            property => Assert.False(property.SetMethod?.IsPublic == true));
        Assert.DoesNotContain(
            typeof(IBackdropFrameLease).GetProperties(),
            property => property.PropertyType.FullName?.StartsWith(
                "Microsoft.Xna.Framework",
                StringComparison.Ordinal) == true);
        IUiBackend backend = new LegacyUiBackend(new RecordingDrawingBackend());
        Assert.Null(backend.BackdropFrameSource);
    }

    [Fact]
    public void IncompatibleProviderIsRejectedWhenTheHostIsCreated()
    {
        RecordingBackdropFrameSource source = new()
        {
            IsCompatible = false
        };
        RecordingDrawingBackend drawing = new();

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => new UiHost(
                new UiHostOptions
                {
                    Root = new UIRoot(),
                    Backend = new TestUiBackend(drawing, source)
                }));

        Assert.Contains(source.GetType().FullName!, error.Message, StringComparison.Ordinal);
        Assert.Contains(drawing.GetType().FullName!, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MetadataRejectsInvalidRasterAndVersionValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Metadata(0, 10, contentVersion: 1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Metadata(10, 10, contentVersion: -1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BackdropFrameMetadata(
                PixelWidth: 10,
                PixelHeight: 10,
                PixelScale: 1,
                ColorProfile: PrismColorProfile.LinearSrgb,
                PixelFormat: BackdropPixelFormat.Rgba8Unorm,
                AlphaMode: BackdropAlphaMode.Premultiplied,
                CoordinateTransform: new Matrix3x2(float.NaN, 0, 0, 1, 0, 0),
                ContentVersion: 1));
    }

    [Fact]
    public void FrameWithoutBackdropSkipsAcquisition()
    {
        UIRoot root = RootWith(new OrderedElement(Color.White, "plain"));
        RecordingBackdropFrameSource source = new();
        RecordingDrawingBackend drawing = new();
        UiHost host = CreateHost(root, drawing, source);

        UpdateAndDraw(host, new UiViewport(80, 60));

        Assert.Equal(0, source.AcquireCalls);
        Assert.Null(drawing.LastFrameContext?.BackdropLease);
        Assert.Equal(0, host.BackdropFrameCounters.RequestedFrames);
        Assert.Equal(1, host.BackdropFrameCounters.SkippedFrames);
        Assert.Equal(0, host.BackdropFrameCounters.FailedFrames);
    }

    [Fact]
    public void SiblingAndNestedBackdropScopesShareExactlyOneFrameLease()
    {
        UIRoot root = new();
        OrderedElement parent = new(new Color(1, 0, 0), "parent");
        OrderedElement child = new(new Color(2, 0, 0), "child");
        OrderedElement sibling = new(new Color(3, 0, 0), "sibling");
        parent.VisualChildren.Add(child);
        root.VisualChildren.Add(parent);
        root.VisualChildren.Add(sibling);
        using IDisposable parentPrism = AttachBackdrop(parent, "Parent", 1);
        using IDisposable childPrism = AttachBackdrop(child, "Child", 10);
        using IDisposable siblingPrism = AttachBackdrop(sibling, "Sibling", 20);
        RecordingBackdropFrameSource source = new();
        RecordingDrawingBackend drawing = new();
        UiHost host = CreateHost(root, drawing, source);

        UpdateAndDraw(host, new UiViewport(100, 70, 2));

        Assert.Equal(1, source.AcquireCalls);
        BackdropFrameRequest request = Assert.Single(source.Requests);
        Assert.Equal(3, request.BackdropRequirement.ScopeCount);
        Assert.Equal(200, request.PixelWidth);
        Assert.Equal(140, request.PixelHeight);
        Assert.Equal(2, request.PixelScale);
        RecordingBackdropFrameLease lease = Assert.Single(source.Leases);
        Assert.Same(lease, drawing.LastFrameContext?.BackdropLease);
        Assert.Same(
            request.BackdropRequirement,
            drawing.LastFrameContext?.PrismAnalysis.BackdropRequirement);
        Assert.Equal(1, lease.DisposeCalls);
        Assert.Equal(1, host.BackdropFrameCounters.RequestedFrames);
        Assert.Equal(1, host.BackdropFrameCounters.AcquiredFrames);
        Assert.Equal(2, host.BackdropFrameCounters.SharedScopeUses);
        Assert.Equal(0, host.BackdropFrameCounters.FailedFrames);
    }

    [Fact]
    public void LeaseIsReleasedWhenDrawingBackendThrows()
    {
        UIRoot root = RootWith(new OrderedElement(Color.White, "control"));
        using IDisposable prism = AttachBackdrop(root.VisualChildren[0], "Failure", 1);
        RecordingBackdropFrameSource source = new();
        RecordingDrawingBackend drawing = new() { ThrowOnRender = true };
        UiHost host = CreateHost(root, drawing, source);
        host.Update(
            FakeInputSource.CreateFrame(),
            new UiViewport(100, 100),
            TimeSpan.Zero);

        Assert.Throws<TestExecutorException>(() => host.Draw());

        RecordingBackdropFrameLease lease = Assert.Single(source.Leases);
        Assert.Equal(1, lease.DisposeCalls);
        Assert.Equal(1, host.BackdropFrameCounters.AcquiredFrames);
        Assert.Equal(1, drawing.RenderCalls);
    }

    [Fact]
    public void HostWithoutProviderSubmitsANullBackdropLease()
    {
        UIRoot root = RootWith(new OrderedElement(Color.White, "control"));
        using IDisposable prism = AttachBackdrop(root.VisualChildren[0], "NoProvider", 1);
        RecordingDrawingBackend drawing = new();
        UiHost host = CreateHost(root, drawing, source: null);

        UpdateAndDraw(host, new UiViewport(100, 100));

        Assert.True(drawing.LastFrameContext?.PrismAnalysis.RequiresBackdrop);
        Assert.Null(drawing.LastFrameContext?.BackdropLease);
        Assert.Equal(1, host.BackdropFrameCounters.RequestedFrames);
        Assert.Equal(0, host.BackdropFrameCounters.AcquiredFrames);
        Assert.Equal(1, host.BackdropFrameCounters.FailedFrames);
        BackdropFrameFailureDiagnostic failure = AssertFailure(
            host,
            "PRISM7101",
            BackdropFrameFailureReason.MissingSource);
        Assert.Contains("no backdrop frame source", failure.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void InvalidViewportBackdropFailureIsDiagnosedPrecisely()
    {
        UIRoot root = RootWith(new OrderedElement(Color.White, "control"));
        using IDisposable prism = AttachBackdrop(root.VisualChildren[0], "InvalidViewport", 1);
        RecordingBackdropFrameSource source = new();
        RecordingDrawingBackend drawing = new();
        UiHost host = CreateHost(root, drawing, source);

        UpdateAndDraw(host, new UiViewport(0, 100));

        Assert.Equal(0, source.AcquireCalls);
        Assert.Equal(1, drawing.RenderCalls);
        BackdropFrameFailureDiagnostic failure = AssertFailure(
            host,
            "PRISM7102",
            BackdropFrameFailureReason.InvalidViewport);
        Assert.Contains("width=0", failure.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void NullBackdropLeaseIsRejectedAndDiagnosedPrecisely()
    {
        UIRoot root = RootWith(new OrderedElement(Color.White, "control"));
        using IDisposable prism = AttachBackdrop(root.VisualChildren[0], "NullLease", 1);
        RecordingBackdropFrameSource source = new()
        {
            ReturnNullLease = true
        };
        RecordingDrawingBackend drawing = new();
        UiHost host = CreateHost(root, drawing, source);
        host.Update(
            FakeInputSource.CreateFrame(),
            new UiViewport(100, 100),
            TimeSpan.Zero);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => host.Draw());

        Assert.Contains("returned a null lease", error.Message, StringComparison.Ordinal);
        Assert.Equal(0, drawing.RenderCalls);
        BackdropFrameFailureDiagnostic failure = AssertFailure(
            host,
            "PRISM7103",
            BackdropFrameFailureReason.NullLease);
        Assert.Contains(source.GetType().FullName!, failure.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void ResizeAndProviderReplacementUseTheCurrentFrameSource()
    {
        UIRoot root = RootWith(new OrderedElement(Color.White, "control"));
        using IDisposable prism = AttachBackdrop(root.VisualChildren[0], "Replace", 1);
        RecordingBackdropFrameSource firstSource = new(
            Metadata(80, 60, contentVersion: 4));
        RecordingDrawingBackend firstDrawing = new();
        UiHost host = CreateHost(root, firstDrawing, firstSource);

        UpdateAndDraw(host, new UiViewport(80, 60));

        RecordingBackdropFrameSource secondSource = new(
            Metadata(240, 160, contentVersion: 9));
        RecordingDrawingBackend secondDrawing = new();
        host.Backend = new TestUiBackend(secondDrawing, secondSource);
        UpdateAndDraw(host, new UiViewport(120, 80, 2));

        Assert.Equal(1, firstSource.AcquireCalls);
        Assert.Equal(1, secondSource.AcquireCalls);
        Assert.Equal(80, Assert.Single(firstSource.Requests).PixelWidth);
        Assert.Equal(240, Assert.Single(secondSource.Requests).PixelWidth);
        Assert.Equal(4, Assert.Single(firstSource.Leases).Metadata.ContentVersion);
        Assert.Equal(9, Assert.Single(secondSource.Leases).Metadata.ContentVersion);
        Assert.Equal(1, Assert.Single(firstSource.Leases).DisposeCalls);
        Assert.Equal(1, Assert.Single(secondSource.Leases).DisposeCalls);
        Assert.Equal(2, host.BackdropFrameCounters.RequestedFrames);
        Assert.Equal(2, host.BackdropFrameCounters.AcquiredFrames);
    }

    [Fact]
    public void HiddenAndCollapsedBackdropOwnersDoNotAcquireAFrame()
    {
        OrderedElement hidden = new(Color.White, "hidden")
        {
            Visibility = Visibility.Hidden
        };
        OrderedElement collapsed = new(Color.White, "collapsed")
        {
            Visibility = Visibility.Collapsed
        };
        UIRoot root = RootWith(hidden, collapsed);
        using IDisposable hiddenPrism = AttachBackdrop(hidden, "Hidden", 1);
        using IDisposable collapsedPrism = AttachBackdrop(collapsed, "Collapsed", 10);
        RecordingBackdropFrameSource source = new();
        UiHost host = CreateHost(root, new RecordingDrawingBackend(), source);

        UpdateAndDraw(host, new UiViewport(100, 100));

        Assert.Equal(0, source.AcquireCalls);
        Assert.Equal(1, host.BackdropFrameCounters.SkippedFrames);
    }

    [Fact]
    public void ZeroOpacityBackdropEliminatedByAnalysisDoesNotAcquireAFrame()
    {
        OrderedElement control = new(Color.White, "control");
        UIRoot root = RootWith(control);
        PrismInstance instance = new(
            PrismTestData.Composition(
                "OptimizedOut",
                PrismTestData.Layer(1, "Content"),
                PrismTestData.Backdrop(2, "Backdrop")));
        using IDisposable prism = GeneratedMarkup.AttachPrism(
            control,
            () => instance);
        instance.Backdrop!.Opacity = 0;
        RecordingBackdropFrameSource source = new();
        UiHost host = CreateHost(
            root,
            new RecordingDrawingBackend(),
            source);

        UpdateAndDraw(host, new UiViewport(100, 100));

        Assert.Equal(0, source.AcquireCalls);
        Assert.Equal(1, host.BackdropFrameCounters.SkippedFrames);
    }

    [Fact]
    public void AcquisitionFailureIsCountedAndDoesNotSubmit()
    {
        UIRoot root = RootWith(new OrderedElement(Color.White, "control"));
        using IDisposable prism = AttachBackdrop(root.VisualChildren[0], "AcquireFailure", 1);
        RecordingBackdropFrameSource source = new()
        {
            ThrowOnAcquire = true
        };
        RecordingDrawingBackend drawing = new();
        UiHost host = CreateHost(root, drawing, source);
        host.Update(
            FakeInputSource.CreateFrame(),
            new UiViewport(100, 100),
            TimeSpan.Zero);

        Assert.Throws<TestAcquireException>(() => host.Draw());

        Assert.Equal(1, source.AcquireCalls);
        Assert.Empty(source.Leases);
        Assert.Equal(0, drawing.RenderCalls);
        Assert.Equal(1, host.BackdropFrameCounters.RequestedFrames);
        Assert.Equal(0, host.BackdropFrameCounters.AcquiredFrames);
        Assert.Equal(1, host.BackdropFrameCounters.FailedFrames);
        BackdropFrameFailureDiagnostic failure = AssertFailure(
            host,
            "PRISM7104",
            BackdropFrameFailureReason.AcquisitionFailed);
        Assert.Contains(nameof(TestAcquireException), failure.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void VisualOrderIsWorldLowerUiBackdropControlThenUpperUi()
    {
        List<string> events = ["game-world"];
        OrderedElement lower = new(new Color(1, 0, 0), "lower-ui");
        OrderedElement control = new(new Color(2, 0, 0), "control-content");
        OrderedElement upper = new(new Color(3, 0, 0), "upper-ui");
        UIRoot root = RootWith(lower, control, upper);
        using IDisposable prism = AttachBackdrop(control, "Ordered", 1);
        RecordingBackdropFrameSource source = new();
        RecordingDrawingBackend drawing = new(events);
        UiHost host = CreateHost(root, drawing, source);

        UpdateAndDraw(host, new UiViewport(100, 100));

        Assert.Equal(
            [
                "game-world",
                "lower-ui",
                "backdrop-plane",
                "control-content",
                "upper-ui"
            ],
            events);
        Assert.NotNull(drawing.LastFrameContext?.BackdropLease);
        Assert.NotSame(
            drawing.ControlCaptureMarker,
            drawing.LastFrameContext?.BackdropLease);
    }

    private static UiHost CreateHost(
        UIRoot root,
        RecordingDrawingBackend drawing,
        IBackdropFrameSource? source)
    {
        return new UiHost(
            new UiHostOptions
            {
                Root = root,
                Backend = new TestUiBackend(drawing, source)
            });
    }

    private static void UpdateAndDraw(UiHost host, UiViewport viewport)
    {
        host.Update(FakeInputSource.CreateFrame(), viewport, TimeSpan.Zero);
        host.Draw();
    }

    private static BackdropFrameFailureDiagnostic AssertFailure(
        UiHost host,
        string code,
        BackdropFrameFailureReason reason)
    {
        BackdropFrameFailureDiagnostic failure = Assert.IsType<BackdropFrameFailureDiagnostic>(
            host.BackdropFrameCounters.LastFailure);
        Assert.Equal(code, failure.Code);
        Assert.Equal(reason, failure.Reason);
        Assert.False(string.IsNullOrWhiteSpace(failure.Detail));
        Assert.Equal(failure, host.BackdropFrameCounters.Snapshot.LastFailure);
        return failure;
    }

    private static UIRoot RootWith(params UIElement[] children)
    {
        UIRoot root = new();
        foreach (UIElement child in children)
        {
            root.VisualChildren.Add(child);
        }

        return root;
    }

    private static IDisposable AttachBackdrop(
        UIElement element,
        string name,
        int nodeId)
    {
        return GeneratedMarkup.AttachPrism(
            element,
            () => new PrismInstance(
                PrismTestData.Composition(
                    name,
                    PrismTestData.Layer(nodeId, "Content"),
                    PrismTestData.Backdrop(nodeId + 1, "Backdrop"))));
    }

    private static BackdropFrameMetadata Metadata(
        int pixelWidth,
        int pixelHeight,
        long contentVersion)
    {
        return new BackdropFrameMetadata(
            PixelWidth: pixelWidth,
            PixelHeight: pixelHeight,
            PixelScale: 1,
            ColorProfile: PrismColorProfile.LinearSrgb,
            PixelFormat: BackdropPixelFormat.Rgba16Float,
            AlphaMode: BackdropAlphaMode.Premultiplied,
            CoordinateTransform: Matrix3x2.Identity,
            ContentVersion: contentVersion);
    }

    private sealed class TestUiBackend : IUiBackend
    {
        public TestUiBackend(
            IDrawingBackend drawingBackend,
            IBackdropFrameSource? backdropFrameSource)
        {
            DrawingBackend = drawingBackend;
            BackdropFrameSource = backdropFrameSource;
        }

        public IInputSource? InputSource => null;

        public IDrawingBackend DrawingBackend { get; }

        public IBackdropFrameSource? BackdropFrameSource { get; }
    }

    private sealed class RecordingBackdropFrameSource : IBackdropFrameSource
    {
        private readonly BackdropFrameMetadata metadata;

        public RecordingBackdropFrameSource()
            : this(Metadata(100, 100, contentVersion: 1))
        {
        }

        public RecordingBackdropFrameSource(BackdropFrameMetadata metadata)
        {
            this.metadata = metadata;
        }

        public int AcquireCalls { get; private set; }

        public bool IsCompatible { get; init; } = true;

        public bool ThrowOnAcquire { get; init; }

        public bool ReturnNullLease { get; init; }

        public List<BackdropFrameRequest> Requests { get; } = [];

        public List<RecordingBackdropFrameLease> Leases { get; } = [];

        public bool IsCompatibleWith(IDrawingBackend drawingBackend)
        {
            ArgumentNullException.ThrowIfNull(drawingBackend);
            return IsCompatible;
        }

        public IBackdropFrameLease AcquireFrame(in BackdropFrameRequest request)
        {
            AcquireCalls++;
            Requests.Add(request);
            if (ThrowOnAcquire)
            {
                throw new TestAcquireException();
            }
            if (ReturnNullLease)
            {
                return null!;
            }

            RecordingBackdropFrameLease lease = new(metadata);
            Leases.Add(lease);
            return lease;
        }
    }

    private sealed class LegacyUiBackend : IUiBackend
    {
        public LegacyUiBackend(IDrawingBackend drawingBackend)
        {
            DrawingBackend = drawingBackend;
        }

        public IInputSource? InputSource => null;

        public IDrawingBackend DrawingBackend { get; }
    }

    private sealed class RecordingBackdropFrameLease : IBackdropFrameLease
    {
        public RecordingBackdropFrameLease(BackdropFrameMetadata metadata)
        {
            Metadata = metadata;
        }

        public BackdropFrameMetadata Metadata { get; }

        public int DisposeCalls { get; private set; }

        public void Dispose()
        {
            DisposeCalls++;
        }
    }

    private sealed class RecordingDrawingBackend : IDrawingBackend
    {
        private readonly List<string>? events;
        private bool insidePrism;

        public RecordingDrawingBackend(List<string>? events = null)
        {
            this.events = events;
        }

        public bool ThrowOnRender { get; init; }

        public int RenderCalls { get; private set; }

        public DrawingFrameContext? LastFrameContext { get; private set; }

        public object ControlCaptureMarker { get; } = new();

        public void Render(
            DrawCommandList commands,
            in DrawingFrameContext frameContext)
        {
            frameContext.EnsureCurrent(commands);
            RenderCalls++;
            LastFrameContext = frameContext;
            if (ThrowOnRender)
            {
                throw new TestExecutorException();
            }

            if (events is null)
            {
                return;
            }

            foreach (DrawCommand command in commands)
            {
                if (command.Kind == DrawCommandKind.BeginPrism)
                {
                    insidePrism = true;
                    events.Add("backdrop-plane");
                    continue;
                }

                if (command.Kind == DrawCommandKind.EndPrism)
                {
                    insidePrism = false;
                    continue;
                }

                if (command.Kind != DrawCommandKind.FillRectangle)
                {
                    continue;
                }

                events.Add(
                    insidePrism
                        ? "control-content"
                        : command.Color.R == 1
                            ? "lower-ui"
                            : "upper-ui");
            }
        }
    }

    private sealed class OrderedElement : UIElement
    {
        private readonly Color color;
        private readonly string name;

        public OrderedElement(Color color, string name)
        {
            this.color = color;
            this.name = name;
        }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return new LayoutSize(10, 10);
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            return new LayoutRect(
                context.FinalRect.X,
                context.FinalRect.Y,
                DesiredSize.Width,
                DesiredSize.Height);
        }

        protected override void OnRender(RenderContext context)
        {
            _ = name;
            context.DrawingContext.FillRectangle(
                new DrawRect(context.Bounds.X, context.Bounds.Y, 5, 5),
                color);
        }
    }

    private sealed class TestExecutorException : Exception;

    private sealed class TestAcquireException : Exception;
}
