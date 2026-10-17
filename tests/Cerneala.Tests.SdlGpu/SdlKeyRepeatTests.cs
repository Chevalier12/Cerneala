using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Input;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlKeyRepeatTests
{
    private const int BackspaceScancode = 42;
    private const int EndScancode = 77;

    [Fact]
    public void HeldBackspaceAutoRepeatRaisesKeyDownPerRepeatAndDeletesOneCharacterEach()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1, WindowDisplayScale = 1 };
        RecordingGraphicsFactory graphics = new();
        using WindowApplicationRuntime runtime = new(new SdlWindowPlatform(api, graphics));
        TextBox textBox = new() { Text = "GUVINTNYEG", Width = 200, Height = 40 };
        Window window = new() { Content = textBox, Width = 320, Height = 200 };
        runtime.StartMainWindow(window);
        uint windowId = Assert.IsType<SdlWindowSurface>(Assert.Single(graphics.Surfaces)).WindowId;
        var bounds = textBox.ArrangedBounds;
        float centerX = bounds.X + bounds.Width / 2;
        float centerY = bounds.Y + bounds.Height / 2;
        Pump(runtime, api,
            new SdlEvent(SdlEventKind.MouseMotion, windowId, X: centerX, Y: centerY),
            new SdlEvent(SdlEventKind.MouseButtonDown, windowId, X: centerX, Y: centerY, MouseButton: 1));
        Pump(runtime, api, new SdlEvent(SdlEventKind.MouseButtonUp, windowId, X: centerX, Y: centerY, MouseButton: 1));
        Assert.True(textBox.IsKeyboardFocused);
        Pump(runtime, api, new SdlEvent(SdlEventKind.KeyDown, windowId, Scancode: EndScancode));
        Pump(runtime, api, new SdlEvent(SdlEventKind.KeyUp, windowId, Scancode: EndScancode));

        List<(string Event, InputKey Key)> keyEvents = [];
        List<bool> previewKeyDownRepeats = [];
        List<bool> keyDownRepeats = [];
        window.PreviewKeyDown += (_, args) =>
        {
            KeyEventArgs keyArgs = (KeyEventArgs)args;
            keyEvents.Add(("PreviewKeyDown", keyArgs.Key));
            previewKeyDownRepeats.Add(keyArgs.IsRepeat);
        };
        // TextBox handles Back, so observe the bubbling KeyDown including handled events.
        window.AddHandler(UIElement.KeyDownEvent, (_, args) =>
        {
            KeyEventArgs keyArgs = (KeyEventArgs)args;
            keyEvents.Add(("KeyDown", keyArgs.Key));
            keyDownRepeats.Add(keyArgs.IsRepeat);
        }, handledEventsToo: true);
        window.PreviewKeyUp += (_, args) => keyEvents.Add(("PreviewKeyUp", ((KeyEventArgs)args).Key));

        Pump(runtime, api, new SdlEvent(SdlEventKind.KeyDown, windowId, Scancode: BackspaceScancode));
        Pump(runtime, api, new SdlEvent(SdlEventKind.KeyDown, windowId, Scancode: BackspaceScancode, Repeat: true));
        Pump(runtime, api, new SdlEvent(SdlEventKind.KeyDown, windowId, Scancode: BackspaceScancode, Repeat: true));
        Assert.True(window.LastFrame!.Input.Keyboard.IsDown(InputKey.Back));
        Assert.DoesNotContain(keyEvents, entry => entry.Event == "PreviewKeyUp");
        Pump(runtime, api, new SdlEvent(SdlEventKind.KeyUp, windowId, Scancode: BackspaceScancode));

        Assert.Equal(3, keyEvents.Count(entry => entry is ("PreviewKeyDown", InputKey.Back)));
        Assert.Equal(3, keyEvents.Count(entry => entry is ("KeyDown", InputKey.Back)));
        Assert.Equal(1, keyEvents.Count(entry => entry is ("PreviewKeyUp", InputKey.Back)));
        Assert.Equal([false, true, true], previewKeyDownRepeats);
        Assert.Equal([false, true, true], keyDownRepeats);
        Assert.Equal("GUVINTN", textBox.Text);
    }

    private static void Pump(WindowApplicationRuntime runtime, FakeSdlApi api, params SdlEvent[] events)
    {
        api.Enqueue(events);
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
    }
}
