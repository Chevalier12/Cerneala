using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Input;

namespace Cerneala.Tests.SdlGpu;

// Key, modifier and text events that arrive in one SDL event pump must reach the
// focused element in arrival order, each with the modifier state of its own moment.
public sealed class SdlKeyboardEventOrderTests
{
    private const int BackspaceScancode = 42;
    private const int EndScancode = 77;
    private const int LeftScancode = 80;
    private const int DownScancode = 81;
    private const int EnterScancode = 40;
    private const int LeftShiftScancode = 225;

    [Fact]
    public void ShiftReleasedAfterLeftInOnePumpStillExtendsTheSelection()
    {
        using Scenario scenario = Scenario.Start("abcdef");

        scenario.Pump(scenario.Key(SdlEventKind.KeyDown, LeftShiftScancode));
        scenario.Pump(
            scenario.Key(SdlEventKind.KeyDown, LeftScancode),
            scenario.Key(SdlEventKind.KeyUp, LeftShiftScancode));
        scenario.Pump(scenario.Key(SdlEventKind.KeyUp, LeftScancode));
        scenario.Drain();

        Assert.Equal(5, scenario.Box.Selection.Start);
        Assert.Equal(1, scenario.Box.Selection.Length);
    }

    [Fact]
    public void ShiftPressedAfterLeftInOnePumpDoesNotApplyToLeft()
    {
        using Scenario scenario = Scenario.Start("abcd");

        scenario.Pump(
            scenario.Key(SdlEventKind.KeyDown, LeftScancode),
            scenario.Key(SdlEventKind.KeyDown, LeftShiftScancode));
        scenario.Drain();

        Assert.Equal(["Left(Shift=False)", "LeftShift(Shift=True)"], scenario.PreviewKeyDowns);
        Assert.Equal(3, scenario.Box.Selection.Start);
        Assert.Equal(0, scenario.Box.Selection.Length);
    }

    [Fact]
    public void ShiftReleasedAfterLeftRepeatsInOnePumpStillExtendsTheSelection()
    {
        using Scenario scenario = Scenario.Start("abcdef");

        scenario.Pump(scenario.Key(SdlEventKind.KeyDown, LeftShiftScancode));
        scenario.Pump(scenario.Key(SdlEventKind.KeyDown, LeftScancode));
        scenario.Pump(
            scenario.Key(SdlEventKind.KeyDown, LeftScancode, repeat: true),
            scenario.Key(SdlEventKind.KeyDown, LeftScancode, repeat: true),
            scenario.Key(SdlEventKind.KeyUp, LeftShiftScancode));
        scenario.Pump(scenario.Key(SdlEventKind.KeyUp, LeftScancode));
        scenario.Drain();

        Assert.Equal(3, scenario.Box.Selection.Start);
        Assert.Equal(3, scenario.Box.Selection.Length);
    }

    [Fact]
    public void ShiftRepeatsBeforeItsReleaseInOnePumpReportShiftDown()
    {
        using Scenario scenario = Scenario.Start("abcd");

        scenario.Pump(scenario.Key(SdlEventKind.KeyDown, LeftShiftScancode));
        scenario.Pump(
            scenario.Key(SdlEventKind.KeyDown, LeftShiftScancode, repeat: true),
            scenario.Key(SdlEventKind.KeyUp, LeftShiftScancode));
        scenario.Drain();

        Assert.Equal(["LeftShift(Shift=True)", "LeftShift(Shift=True)"], scenario.PreviewKeyDowns);
    }

    [Fact]
    public void DifferentKeysInOnePumpAreRaisedInArrivalOrder()
    {
        using Scenario scenario = Scenario.Start("abcd");

        scenario.Pump(
            scenario.Key(SdlEventKind.KeyDown, DownScancode),
            scenario.Key(SdlEventKind.KeyDown, EnterScancode));
        scenario.Drain();

        Assert.Equal(["Down(Shift=False)", "Enter(Shift=False)"], scenario.PreviewKeyDowns);
    }

    [Fact]
    public void TextTypedBeforeBackspacePressInOnePumpIsInsertedBeforeTheBackspace()
    {
        using Scenario scenario = Scenario.Start("GUVINTNYEG");

        scenario.Pump(
            new SdlEvent(SdlEventKind.TextInput, scenario.WindowId, Text: "x"),
            scenario.Key(SdlEventKind.KeyDown, BackspaceScancode));
        scenario.Pump(scenario.Key(SdlEventKind.KeyUp, BackspaceScancode));
        scenario.Drain();

        Assert.Equal("GUVINTNYEG", scenario.Box.Text);
    }

    private sealed class Scenario : IDisposable
    {
        private readonly FakeSdlApi api;
        private readonly WindowApplicationRuntime runtime;

        private Scenario(FakeSdlApi api, WindowApplicationRuntime runtime, TextBox box, uint windowId)
        {
            this.api = api;
            this.runtime = runtime;
            Box = box;
            WindowId = windowId;
        }

        public TextBox Box { get; }

        public uint WindowId { get; }

        public List<string> PreviewKeyDowns { get; } = [];

        public static Scenario Start(string text)
        {
            FakeSdlApi api = new() { WindowPixelDensity = 1, WindowDisplayScale = 1 };
            RecordingGraphicsFactory graphics = new();
            WindowApplicationRuntime runtime = new(new SdlWindowPlatform(api, graphics));
            TextBox box = new() { Text = text, Width = 200, Height = 40 };
            Window window = new() { Content = box, Width = 320, Height = 200 };
            runtime.StartMainWindow(window);
            uint windowId = Assert.IsType<SdlWindowSurface>(Assert.Single(graphics.Surfaces)).WindowId;
            Scenario scenario = new(api, runtime, box, windowId);
            var bounds = box.ArrangedBounds;
            float x = bounds.X + bounds.Width / 2;
            float y = bounds.Y + bounds.Height / 2;
            scenario.Pump(new SdlEvent(SdlEventKind.MouseButtonDown, windowId, X: x, Y: y, MouseButton: 1));
            scenario.Pump(new SdlEvent(SdlEventKind.MouseButtonUp, windowId, X: x, Y: y, MouseButton: 1));
            scenario.Pump(scenario.Key(SdlEventKind.KeyDown, EndScancode));
            scenario.Pump(scenario.Key(SdlEventKind.KeyUp, EndScancode));
            scenario.Drain();
            Assert.True(box.IsKeyboardFocused);
            Assert.Equal(text.Length, box.Selection.Active);
            window.PreviewKeyDown += (_, args) =>
            {
                KeyEventArgs keyArgs = (KeyEventArgs)args;
                scenario.PreviewKeyDowns.Add($"{keyArgs.Key}(Shift={keyArgs.IsShiftDown})");
            };
            return scenario;
        }

        public SdlEvent Key(SdlEventKind kind, int scancode, bool repeat = false) =>
            new(kind, WindowId, Scancode: scancode, Repeat: repeat);

        public void Pump(params SdlEvent[] events)
        {
            api.Enqueue(events);
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        }

        // Runs the frames that events deferred by the input source wait for.
        public void Drain()
        {
            for (int frame = 0; frame < 6; frame++)
            {
                runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
            }
        }

        public void Dispose() => runtime.Dispose();
    }
}
