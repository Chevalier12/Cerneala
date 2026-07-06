using Cerneala.Playground.Samples;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Platform;

namespace Cerneala.Tests.Playground.Samples;

public sealed class RuntimePreviewIntegrationTests
{
    [Fact]
    public void RuntimePreviewSampleIsRegisteredInSampleSelector()
    {
        SampleSelector selector = SampleSelector.CreateDefault();

        (IPlaygroundSample Sample, int Index) registration = selector.Samples
            .Select((sample, index) => (sample, index))
            .Single(entry => entry.sample is RuntimePreviewSample);

        Assert.Equal("Runtime Preview", registration.Sample.Name);

        selector.SelectSample(registration.Index);

        Assert.Equal(registration.Index, selector.ActiveIndex);
        Assert.Same(registration.Sample, selector.ActiveSample);
        Assert.NotNull(selector.ActiveElement);
    }

    [Fact]
    public void RuntimePreviewSampleBuildsWithFakePlatformServices()
    {
        FakePlatformServices services = new();
        RuntimePreviewSample sample = new();
        UiHost host = HostFor(sample, services);

        UiFrame frame = host.Update(EmptyInputFrame(), new UiViewport(640, 360, scale: 2), TimeSpan.FromMilliseconds(16));
        sample.UpdateFrame(frame);

        Assert.Same(services, host.Root!.PlatformServices);
        Assert.NotNull(sample.RootElement);
        Assert.NotNull(sample.PreviewImage);
        Assert.NotNull(sample.InputTextBox);
        Assert.NotNull(sample.ActionButton);
        Assert.NotNull(sample.ItemsList);
        Assert.Contains("platform clipboard=True", sample.DiagnosticsText!.Text, StringComparison.Ordinal);
        Assert.Contains("cursor=True", sample.DiagnosticsText.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void RuntimePreviewSampleBuildsWithoutPlatformServices()
    {
        RuntimePreviewSample sample = new();
        UiHost host = HostFor(sample, platformServices: null);

        UiFrame frame = host.Update(EmptyInputFrame(), new UiViewport(640, 360, scale: 2), TimeSpan.FromMilliseconds(16));
        sample.UpdateFrame(frame);

        Assert.Same(PlatformServices.Empty, host.Root!.PlatformServices);
        Assert.NotNull(sample.RootElement);
        Assert.NotNull(sample.PreviewImage);
        Assert.NotNull(sample.InputTextBox);
        Assert.NotNull(sample.ActionButton);
        Assert.NotNull(sample.ItemsList);
        Assert.Contains("platform clipboard=False", sample.DiagnosticsText!.Text, StringComparison.Ordinal);
        Assert.Contains("cursor=False", sample.DiagnosticsText.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void RuntimePreviewSampleOverlayReportsScaleAndNoWorkFrame()
    {
        RuntimePreviewSample sample = new();
        UiHost host = HostFor(sample, new FakePlatformServices());
        UiViewport viewport = new(640, 360, scale: 2);
        host.Update(EmptyInputFrame(), viewport, TimeSpan.FromMilliseconds(16));

        UiFrame noWorkFrame = host.Update(EmptyInputFrame(), viewport, TimeSpan.FromMilliseconds(16));
        sample.UpdateFrame(noWorkFrame);

        Assert.Equal(0, noWorkFrame.Stats.MeasuredElements);
        Assert.Equal(0, noWorkFrame.Stats.ArrangedElements);
        Assert.Equal(0, noWorkFrame.Stats.RenderedElements);
        Assert.Equal(1, noWorkFrame.Stats.NoWorkFrames);
        Assert.Contains("scale=2", sample.DiagnosticsText!.Text, StringComparison.Ordinal);
        Assert.Contains("noWork=1", sample.DiagnosticsText.Text, StringComparison.Ordinal);
    }

    private static UiHost HostFor(RuntimePreviewSample sample, IPlatformServices? platformServices)
    {
        UIRoot root = new(640, 360, scale: 2);
        root.VisualChildren.Add(sample.Build());

        return new UiHost(new UiHostOptions
        {
            Root = root,
            Viewport = new UiViewport(640, 360, scale: 2),
            PlatformServices = platformServices
        });
    }

    private static InputFrame EmptyInputFrame()
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.Empty,
            Array.Empty<TextInputSnapshotEvent>());
    }

    private sealed class FakePlatformServices : IPlatformServices
    {
        private readonly FakeClipboard clipboard = new();

        public IClipboard? Clipboard => clipboard;

        public ICursorService? Cursor { get; } = new RecordingCursorService();

        public IFileDialogService? FileDialogs => null;

        public ITextInputPlatform? TextInput => new FakeTextInputPlatform(clipboard);

        public IDpiProvider? Dpi { get; } = new FakeDpiProvider();

        public IAccessibilityPlatform? Accessibility => null;
    }

    private sealed class FakeClipboard : IClipboard
    {
        private string? text;

        public bool HasText => !string.IsNullOrEmpty(text);

        public string? GetText()
        {
            return text;
        }

        public void SetText(string value)
        {
            text = value;
        }
    }

    private sealed class RecordingCursorService : ICursorService
    {
        public CursorShape Current { get; private set; } = CursorShape.Default;

        public void SetCursor(CursorShape shape)
        {
            Current = shape;
        }
    }

    private sealed class FakeTextInputPlatform(IClipboard clipboard) : ITextInputPlatform
    {
        public IClipboard? Clipboard => clipboard;

        public bool SupportsIme => false;
    }

    private sealed class FakeDpiProvider : IDpiProvider
    {
        public float Scale => 2;

        public float DpiX => 192;

        public float DpiY => 192;
    }
}
