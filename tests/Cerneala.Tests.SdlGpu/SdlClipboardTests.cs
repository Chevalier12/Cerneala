using Cerneala.Platforms.Sdl3;
using Cerneala.UI;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Platform;

namespace Cerneala.Tests.SdlGpu;

// The SDL platform supplies the system clipboard, so TextBox and PasswordBox shortcuts work
// without application code (#716).
public sealed class SdlClipboardTests
{
    private const int AScancode = 4;
    private const int CScancode = 6;
    private const int VScancode = 25;
    private const int XScancode = 27;
    private const int LeftCtrlScancode = 224;

    [Fact]
    public void PlatformServicesExposeTheSystemClipboard()
    {
        FakeSdlApi api = new();
        using SdlWindowPlatform platform = new(api, new RecordingGraphicsFactory());

        IClipboard clipboard = Assert.IsType<SdlClipboard>(platform.PlatformServices.Clipboard);

        Assert.False(clipboard.HasText);
        api.ClipboardText = "x";
        Assert.True(clipboard.HasText);
    }

    [Fact]
    public void GetTextIsNullWhenTheSystemClipboardHoldsNoText()
    {
        FakeSdlApi api = new();
        SdlClipboard clipboard = new(api);

        Assert.False(clipboard.HasText);
        Assert.Null(clipboard.GetText());
    }

    [Fact]
    public void GetTextReturnsTheSystemClipboardText()
    {
        FakeSdlApi api = new() { ClipboardText = "a\r\nb" };
        SdlClipboard clipboard = new(api);

        Assert.True(clipboard.HasText);
        Assert.Equal("a\r\nb", clipboard.GetText());
    }

    [Fact]
    public void SetTextWritesTheSystemClipboard()
    {
        FakeSdlApi api = new();
        SdlClipboard clipboard = new(api);

        clipboard.SetText("hello");

        Assert.Equal("hello", api.ClipboardText);
    }

    [Fact]
    public void SetTextDoesNothingAndDoesNotThrowWhenSdlRefusesTheWrite()
    {
        FakeSdlApi api = new() { ClipboardText = "keep", SetClipboardTextResult = false };
        SdlClipboard clipboard = new(api);

        clipboard.SetText("new");

        Assert.Equal("keep", api.ClipboardText);
    }

    [Fact]
    public void SetTextRejectsNull()
    {
        SdlClipboard clipboard = new(new FakeSdlApi());

        Assert.Throws<ArgumentNullException>(() => clipboard.SetText(null!));
    }

    [Fact]
    public void CtrlCCopiesTheSelectedTextBoxTextToTheSystemClipboard()
    {
        TextBox box = new() { Text = "hello", Width = 200, Height = 40 };
        using Scenario scenario = Scenario.Start(box);
        scenario.Chord(AScancode);

        scenario.Chord(CScancode);

        Assert.Equal("hello", scenario.Api.ClipboardText);
        Assert.Equal("hello", box.Text);
    }

    [Fact]
    public void CtrlVPastesTheSystemClipboardTextIntoTheTextBox()
    {
        TextBox box = new() { Width = 200, Height = 40 };
        using Scenario scenario = Scenario.Start(box);
        scenario.Api.ClipboardText = "world";

        scenario.Chord(VScancode);

        Assert.Equal("world", box.Text);
    }

    [Fact]
    public void CtrlXCutsTheSelectedTextBoxTextToTheSystemClipboard()
    {
        TextBox box = new() { Text = "hello", Width = 200, Height = 40 };
        using Scenario scenario = Scenario.Start(box);
        scenario.Chord(AScancode);

        scenario.Chord(XScancode);

        Assert.Equal("hello", scenario.Api.ClipboardText);
        Assert.Equal(string.Empty, box.Text);
    }

    [Fact]
    public void PasswordBoxCopyAndCutLeaveTheSystemClipboardAndThePasswordUnchanged()
    {
        PasswordBox box = new() { Password = "secret", Width = 200, Height = 40 };
        using Scenario scenario = Scenario.Start(box);
        scenario.Api.ClipboardText = "keep";

        scenario.Chord(AScancode);
        scenario.Chord(CScancode);
        scenario.Chord(XScancode);

        Assert.Equal("keep", scenario.Api.ClipboardText);
        Assert.Equal("secret", box.Password);
    }

    [Fact]
    public void RefusedClipboardWriteDoesNotThrowAndChangesNothing()
    {
        TextBox box = new() { Text = "hello", Width = 200, Height = 40 };
        using Scenario scenario = Scenario.Start(box);
        scenario.Api.ClipboardText = "keep";
        scenario.Api.SetClipboardTextResult = false;
        scenario.Chord(AScancode);

        scenario.Chord(CScancode);

        Assert.Equal("keep", scenario.Api.ClipboardText);
        Assert.Equal("hello", box.Text);
    }

    private sealed class Scenario : IDisposable
    {
        private readonly WindowApplicationRuntime runtime;
        private readonly uint windowId;

        private Scenario(FakeSdlApi api, WindowApplicationRuntime runtime, uint windowId)
        {
            Api = api;
            this.runtime = runtime;
            this.windowId = windowId;
        }

        public FakeSdlApi Api { get; }

        // Opens a window around the element and focuses it with a real mouse click.
        public static Scenario Start(Control element)
        {
            FakeSdlApi api = new() { WindowPixelDensity = 1, WindowDisplayScale = 1 };
            RecordingGraphicsFactory graphics = new();
            WindowApplicationRuntime runtime = new(new SdlWindowPlatform(api, graphics));
            runtime.StartMainWindow(new Window { Content = element, Width = 320, Height = 200 });
            uint windowId = Assert.IsType<SdlWindowSurface>(Assert.Single(graphics.Surfaces)).WindowId;
            Scenario scenario = new(api, runtime, windowId);
            var bounds = element.ArrangedBounds;
            float x = bounds.X + bounds.Width / 2;
            float y = bounds.Y + bounds.Height / 2;
            scenario.Pump(new SdlEvent(SdlEventKind.MouseButtonDown, windowId, X: x, Y: y, MouseButton: 1));
            scenario.Pump(new SdlEvent(SdlEventKind.MouseButtonUp, windowId, X: x, Y: y, MouseButton: 1));
            scenario.Drain();
            Assert.True(element.IsKeyboardFocused);
            return scenario;
        }

        // Control down, the key down and up, then Control up, each in its own pump.
        public void Chord(int scancode)
        {
            Pump(Key(SdlEventKind.KeyDown, LeftCtrlScancode));
            Pump(Key(SdlEventKind.KeyDown, scancode));
            Pump(Key(SdlEventKind.KeyUp, scancode));
            Pump(Key(SdlEventKind.KeyUp, LeftCtrlScancode));
            Drain();
        }

        public void Dispose() => runtime.Dispose();

        private SdlEvent Key(SdlEventKind kind, int scancode) => new(kind, windowId, Scancode: scancode);

        private void Pump(params SdlEvent[] events)
        {
            Api.Enqueue(events);
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        }

        // Runs the frames that events deferred by the input source wait for.
        private void Drain()
        {
            for (int frame = 0; frame < 6; frame++)
            {
                runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
            }
        }
    }
}