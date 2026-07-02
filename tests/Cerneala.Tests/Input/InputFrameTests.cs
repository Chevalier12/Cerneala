using Cerneala.UI.Input;

namespace Cerneala.Tests.Input;

public sealed class InputFrameTests
{
    [Fact]
    public void InputFrameReportsMouseButtonTransitions()
    {
        PointerSnapshot previous = PointerSnapshot.Empty;
        PointerSnapshot current = previous.WithButton(InputMouseButton.Left, isDown: true);

        InputFrame frame = new(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);

        Assert.True(frame.Pointer.IsPressed(InputMouseButton.Left));
        Assert.True(frame.Pointer.IsDown(InputMouseButton.Left));
        Assert.False(frame.Pointer.IsReleased(InputMouseButton.Left));
    }

    [Fact]
    public void InputFrameReportsKeyTransitions()
    {
        KeyboardSnapshot previous = KeyboardSnapshot.Empty;
        KeyboardSnapshot current = KeyboardSnapshot.FromDownKeys([InputKey.Enter]);

        InputFrame frame = new(PointerSnapshot.Empty, PointerSnapshot.Empty, previous, current, []);

        Assert.True(frame.Keyboard.IsPressed(InputKey.Enter));
        Assert.True(frame.Keyboard.IsDown(InputKey.Enter));
        Assert.False(frame.Keyboard.IsReleased(InputKey.Enter));
    }

    [Fact]
    public void InputFrameCarriesTextInputEvents()
    {
        TextInputSnapshotEvent text = new("ă");

        InputFrame frame = new(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, [text]);

        Assert.Equal("ă", frame.TextInputEvents.Single().Text);
    }

    [Fact]
    public void InputFrameCopiesTextInputEvents()
    {
        List<TextInputSnapshotEvent> events = [new("a")];

        InputFrame frame = new(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, events);
        events.Add(new("b"));

        Assert.Equal("a", frame.TextInputEvents.Single().Text);
    }
}
