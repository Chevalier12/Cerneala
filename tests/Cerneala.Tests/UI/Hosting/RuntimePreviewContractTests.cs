using System.Runtime.CompilerServices;
using Cerneala.Drawing;
using Cerneala.Playground.Samples;
using Cerneala.UI.Controls;
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.MonoGame;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Platform;
using Cerneala.UI.Resources;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Tests.UI.Hosting;

public sealed class RuntimePreviewContractTests
{
    private static readonly UiViewport Viewport = new(420, 320, 2);

    [Fact]
    public void RuntimePreviewFirstFrameDoesRetainedWork()
    {
        UiHost host = RuntimeHost(out _, out _);

        UiFrame frame = host.Update(EmptyFrame(), Viewport, TimeSpan.Zero);

        Assert.True(frame.Stats.HasWork);
        Assert.True(frame.Stats.MeasuredElements > 0);
        Assert.True(frame.Stats.ArrangedElements > 0);
        Assert.True(frame.Stats.RenderedElements > 0);
        Assert.True(frame.Stats.HitTestElements > 0);
    }

    [Fact]
    public void RuntimePreviewSecondUnchangedFrameDoesNoRetainedWork()
    {
        UiHost host = RuntimeHost(out _, out _);
        host.Update(EmptyFrame(), Viewport, TimeSpan.Zero);

        UiFrame second = host.Update(EmptyFrame(), Viewport, TimeSpan.Zero);

        Assert.Equal(0, second.Stats.MeasuredElements);
        Assert.Equal(0, second.Stats.ArrangedElements);
        Assert.Equal(0, second.Stats.RenderedElements);
        Assert.Equal(0, second.Stats.HitTestElements);
        Assert.Equal(1, second.Stats.NoWorkFrames);
    }

    [Fact]
    public void RuntimePreviewDrawDoesNotGenerateRetainedWork()
    {
        UiHost host = RuntimeHost(out UIRoot root, out _);
        FakeDrawingBackend backend = new();
        host.Update(EmptyFrame(), Viewport, TimeSpan.Zero);
        host.Update(EmptyFrame(), Viewport, TimeSpan.Zero);

        host.Draw(backend);
        host.Draw(backend);

        Assert.Equal(2, backend.RenderCalls);
        Assert.NotNull(backend.LastCommands);
        Assert.NotEmpty(backend.LastCommands);
        Assert.False(root.Scheduler.HasWork);
    }

    [Fact]
    public void RuntimePreviewScaleChangeInvalidatesExpectedWorkOnly()
    {
        UiHost host = RuntimeHost(out _, out _);
        host.Update(EmptyFrame(), new UiViewport(420, 320, 1), TimeSpan.Zero);
        host.Update(EmptyFrame(), new UiViewport(420, 320, 1), TimeSpan.Zero);

        UiFrame changed = host.Update(EmptyFrame(), Viewport, TimeSpan.Zero);

        Assert.True(changed.Stats.MeasuredElements > 0);
        Assert.True(changed.Stats.ArrangedElements > 0);
        Assert.True(changed.Stats.RenderedElements > 0);
        Assert.True(changed.Stats.HitTestElements > 0);
        Assert.Equal(0, changed.Stats.CommandStateElements);
    }

    [Fact]
    public void RuntimePreviewDiagnosticsCaptureDoesNotCreateWork()
    {
        UiHost host = RuntimeHost(out UIRoot root, out _);
        host.Update(EmptyFrame(), Viewport, TimeSpan.Zero);
        UiFrame frame = host.Update(EmptyFrame(), Viewport, TimeSpan.Zero);
        int versionBeforeCapture = root.RetainedRenderCache.Version;

        RuntimeDiagnosticsSnapshot snapshot = RuntimeDiagnostics.Capture(root, frame.Viewport, frame.Stats);

        Assert.Equal(1, snapshot.Frame.NoWorkFrames);
        Assert.False(root.Scheduler.HasWork);
        Assert.Equal(versionBeforeCapture, root.RetainedRenderCache.Version);
    }

    [Fact]
    public void RuntimePreviewScaledPointerHitsLogicalButton()
    {
        UiHost host = RuntimeHost(out _, out RuntimePreviewSample sample);
        host.Update(EmptyFrame(), Viewport, TimeSpan.Zero);
        int initialCount = sample.Items.Count;
        LayoutRect bounds = sample.PrimaryButton!.ArrangedBounds;
        float x = bounds.X + (bounds.Width / 2);
        float y = bounds.Y + (bounds.Height / 2);

        host.Update(PointerFrame(x, y, currentDown: true), Viewport, TimeSpan.Zero);
        host.Update(PointerFrame(x, y, previousDown: true), Viewport, TimeSpan.Zero);

        Assert.Equal(initialCount + 1, sample.Items.Count);
    }

    [Fact]
    public void RuntimePreviewScaledPointerFocusesTextBox()
    {
        UiHost host = RuntimeHost(out _, out RuntimePreviewSample sample);
        host.Update(EmptyFrame(), Viewport, TimeSpan.Zero);
        LayoutRect bounds = sample.InputTextBox!.ArrangedBounds;

        host.Update(PointerFrame(bounds.X + 4, bounds.Y + 4, currentDown: true), Viewport, TimeSpan.Zero);

        Assert.Same(sample.InputTextBox, host.InputBridge.FocusManager.FocusedElement);
    }

    [Fact]
    public void RuntimePreviewCursorPublishesHandAndIBeamForLogicalHitTargets()
    {
        FakeCursorService cursor = new();
        UiHost host = RuntimeHost(out _, out RuntimePreviewSample sample, platformServices: new PlatformServices(Cursor: cursor));
        host.Update(EmptyFrame(), Viewport, TimeSpan.Zero);

        LayoutRect buttonBounds = sample.PrimaryButton!.ArrangedBounds;
        host.Update(PointerFrame(buttonBounds.X + 2, buttonBounds.Y + 2), Viewport, TimeSpan.Zero);
        LayoutRect textBoxBounds = sample.InputTextBox!.ArrangedBounds;
        host.Update(PointerFrame(textBoxBounds.X + 2, textBoxBounds.Y + 2), Viewport, TimeSpan.Zero);

        Assert.Contains(CursorShape.Hand, cursor.Published);
        Assert.Equal(CursorShape.IBeam, cursor.Current);
    }

    [Fact]
    public void RuntimePreviewExplicitInputFrameIsNotDoubleScaled()
    {
        UiHost host = RuntimeHost(out _, out RuntimePreviewSample sample);
        host.Update(EmptyFrame(), Viewport, TimeSpan.Zero);
        LayoutRect bounds = sample.InputTextBox!.ArrangedBounds;

        host.Update(PointerFrame(bounds.X + 3, bounds.Y + 3, currentDown: true), Viewport, TimeSpan.Zero);

        Assert.Same(sample.InputTextBox, host.InputBridge.FocusManager.FocusedElement);
    }

    [Fact]
    public void RuntimePreviewPathBackedImageLoadsOnceAcrossMeasureRenderAndDraw()
    {
        RuntimeImageHarness harness = RuntimeImageHost(out UiHost host, out RuntimePreviewSample sample);
        FakeDrawingBackend backend = new();

        host.Update(EmptyFrame(), Viewport, TimeSpan.Zero);
        host.Draw(backend);
        host.Update(EmptyFrame(), Viewport, TimeSpan.Zero);
        host.Draw(backend);

        Assert.Same(harness.Image, sample.Image!.ResolvedSource);
        Assert.Equal(1, harness.Loader.GetLoadCount("runtime.png"));
    }

    [Fact]
    public void RuntimePreviewImageResourceReplacementInvalidatesDependentRender()
    {
        RuntimeImageHarness harness = RuntimeImageHost(out UiHost host, out _);
        host.Update(EmptyFrame(), Viewport, TimeSpan.Zero);
        host.Update(EmptyFrame(), Viewport, TimeSpan.Zero);
        TestImage replacement = new(48, 24);
        harness.Loader.SetImage("replacement.png", replacement);

        harness.Resources.SetResource(harness.ImageId, new ImageResource("replacement.png"));
        UiFrame changed = host.Update(EmptyFrame(), Viewport, TimeSpan.Zero);

        Assert.Equal(0, changed.Stats.MeasuredElements);
        Assert.True(changed.Stats.RenderedElements > 0);
        Assert.Equal(1, harness.Loader.GetLoadCount("runtime.png"));
        Assert.Equal(1, harness.Loader.GetLoadCount("replacement.png"));
    }

    [Fact]
    public void RuntimePreviewDisposingHostDisposesOwnedImageCacheOnce()
    {
        RecordingImageLoader loader = new();
        DisposableTestImage image = new(32, 16);
        loader.SetImage("runtime.png", image);
        ResourceStore resources = new();
        ResourceId<ImageResource> imageId = new("Runtime/Image");
        resources.SetResource(imageId, new ImageResource("runtime.png"));
        RuntimePreviewSample sample = new(resources, imageResourceId: imageId);
        UIRoot root = new(420, 320);
        root.SetResourceProvider(resources);
        root.VisualChildren.Add(sample.Build());
        MonoGameContentServices contentServices = new(imageLoader: loader);
        MonoGameUiHost host = new(new MonoGameUiHostOptions
        {
            SpriteBatch = CreateUninitialized<SpriteBatch>(),
            WhitePixel = CreateUninitialized<Texture2D>(),
            Root = root,
            Viewport = Viewport,
            ContentServices = contentServices
        });
        host.Update(EmptyFrame(), Viewport, TimeSpan.Zero);

        host.Dispose();
        host.Dispose();

        Assert.Equal(1, image.DisposeCount);
    }

    [Fact]
    public void RuntimePreviewTextBoxClipboardPasteUpdatesTypedBinding()
    {
        FakeClipboard clipboard = new("pasted runtime text");
        UiHost host = RuntimeHost(out UIRoot root, out RuntimePreviewSample sample, platformServices: new PlatformServices(Clipboard: clipboard));
        host.Update(EmptyFrame(), Viewport, TimeSpan.Zero);
        FocusTextBox(host, root, sample);
        sample.InputTextBox!.Select(0, sample.InputTextBox.Text.Length);

        host.Update(CtrlShortcutFrame(InputKey.V), Viewport, TimeSpan.Zero);

        Assert.Equal("pasted runtime text", sample.InputTextBox.Text);
        Assert.Equal("pasted runtime text", sample.InputValue.Value);
    }

    [Fact]
    public void RuntimePreviewClipboardMissingDoesNotBreakTextInput()
    {
        UiHost host = RuntimeHost(out UIRoot root, out RuntimePreviewSample sample, platformServices: PlatformServices.Empty);
        host.Update(EmptyFrame(), Viewport, TimeSpan.Zero);
        FocusTextBox(host, root, sample);
        string before = sample.InputTextBox!.Text;

        host.Update(CtrlShortcutFrame(InputKey.V), Viewport, TimeSpan.Zero);

        Assert.Equal(before, sample.InputTextBox.Text);
    }

    [Fact]
    public void RuntimePreviewButtonCommandStateStillRefreshesAfterClipboardTextChange()
    {
        FakeClipboard clipboard = new("runtime item");
        UiHost host = RuntimeHost(out UIRoot root, out RuntimePreviewSample sample, platformServices: new PlatformServices(Clipboard: clipboard));
        host.Update(EmptyFrame(), Viewport, TimeSpan.Zero);
        sample.InputValue.Value = string.Empty;
        host.Update(EmptyFrame(), Viewport, TimeSpan.Zero);
        Assert.False(sample.PrimaryButton!.IsEnabled);
        FocusTextBox(host, root, sample);

        host.Update(CtrlShortcutFrame(InputKey.V), Viewport, TimeSpan.Zero);

        Assert.Equal("runtime item", sample.InputValue.Value);
        Assert.True(sample.PrimaryButton.IsEnabled);
    }

    private static UiHost RuntimeHost(
        out UIRoot root,
        out RuntimePreviewSample sample,
        IPlatformServices? platformServices = null)
    {
        root = new UIRoot(420, 320);
        sample = new RuntimePreviewSample();
        root.VisualChildren.Add(sample.Build());
        return new UiHost(new UiHostOptions
        {
            Root = root,
            Viewport = Viewport,
            PlatformServices = platformServices
        });
    }

    private static RuntimeImageHarness RuntimeImageHost(out UiHost host, out RuntimePreviewSample sample)
    {
        RecordingImageLoader loader = new();
        TestImage image = new(32, 16);
        loader.SetImage("runtime.png", image);
        ResourceStore resources = new();
        ResourceId<ImageResource> imageId = new("Runtime/Image");
        resources.SetResource(imageId, new ImageResource("runtime.png"));
        UIRoot root = new(420, 320);
        root.SetResourceProvider(resources);
        root.SetImageLoader(loader);
        sample = new RuntimePreviewSample(resources, imageResourceId: imageId);
        root.VisualChildren.Add(sample.Build());
        host = new UiHost(new UiHostOptions { Root = root, Viewport = Viewport });
        return new RuntimeImageHarness(resources, imageId, loader, image);
    }

    private static void FocusTextBox(UiHost host, UIRoot root, RuntimePreviewSample sample)
    {
        ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);
        Assert.True(host.InputBridge.FocusManager.Focus(sample.InputTextBox!, routeMap));
    }

    private static InputFrame EmptyFrame()
    {
        return new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private static InputFrame PointerFrame(float x, float y, bool previousDown = false, bool currentDown = false)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(x, y);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(x, y);
        if (previousDown)
        {
            previous = previous.WithButton(InputMouseButton.Left, true);
        }

        if (currentDown)
        {
            current = current.WithButton(InputMouseButton.Left, true);
        }

        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private static InputFrame CtrlShortcutFrame(InputKey key)
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.FromDownKeys([InputKey.LeftCtrl]),
            KeyboardSnapshot.FromDownKeys([InputKey.LeftCtrl, key]),
            []);
    }

    private static T CreateUninitialized<T>()
        where T : class
    {
        return (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
    }

    private sealed record RuntimeImageHarness(
        ResourceStore Resources,
        ResourceId<ImageResource> ImageId,
        RecordingImageLoader Loader,
        TestImage Image);

    private sealed class RecordingImageLoader : IImageLoader
    {
        private readonly Dictionary<string, IDrawImage> images = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> loadCounts = new(StringComparer.Ordinal);

        public void SetImage(string path, IDrawImage image)
        {
            images[path] = image;
        }

        public int GetLoadCount(string path)
        {
            return loadCounts.GetValueOrDefault(path);
        }

        public IDrawImage Load(string path)
        {
            loadCounts[path] = GetLoadCount(path) + 1;
            return images.TryGetValue(path, out IDrawImage? image)
                ? image
                : throw new InvalidOperationException($"No fake image registered for '{path}'.");
        }
    }

    private class TestImage(int width, int height) : IDrawImage
    {
        public int Width { get; } = width;

        public int Height { get; } = height;
    }

    private sealed class DisposableTestImage(int width, int height) : TestImage(width, height), IDisposable
    {
        public int DisposeCount { get; private set; }

        public void Dispose()
        {
            DisposeCount++;
        }
    }

    private sealed class FakeClipboard(string? text = null) : IClipboard
    {
        public string? Text { get; private set; } = text;

        public bool HasText => Text is not null;

        public string? GetText()
        {
            return Text;
        }

        public void SetText(string text)
        {
            Text = text;
        }
    }

    private sealed class FakeCursorService : ICursorService
    {
        private readonly List<CursorShape> published = [];

        public CursorShape Current { get; private set; }

        public IReadOnlyList<CursorShape> Published => published;

        public void SetCursor(CursorShape shape)
        {
            Current = shape;
            published.Add(shape);
        }
    }
}
