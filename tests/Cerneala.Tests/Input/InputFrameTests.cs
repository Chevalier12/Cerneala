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
    public void PointerSnapshotIgnoresNoMouseButtonSentinel()
    {
        PointerSnapshot snapshot = PointerSnapshot.Empty.WithButton(InputMouseButton.None, isDown: true);

        Assert.False(snapshot.IsDown(InputMouseButton.None));
    }

    [Fact]
    public void InputFrameReportsMouseWheelDelta()
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithWheelValue(120);
        PointerSnapshot current = previous.WithWheelValue(360);

        InputFrame frame = new(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);

        Assert.Equal(360, frame.Pointer.WheelValue);
        Assert.Equal(240, frame.Pointer.WheelDelta);
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
    public void TextInputSnapshotEventRejectsNullText()
    {
        Assert.Throws<ArgumentNullException>(() => new TextInputSnapshotEvent(null!));
    }

    [Fact]
    public void InputFrameCopiesTextInputEvents()
    {
        List<TextInputSnapshotEvent> events = [new("a")];

        InputFrame frame = new(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, events);
        events.Add(new("b"));

        Assert.Equal("a", frame.TextInputEvents.Single().Text);
    }

    [Fact]
    public void InputFrameExposesImmutableTextInputEvents()
    {
        TextInputSnapshotEvent original = new("a");
        InputFrame frame = new(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, [original]);

        if (frame.TextInputEvents is IList<TextInputSnapshotEvent> exposedEvents)
        {
            try
            {
                exposedEvents[0] = new TextInputSnapshotEvent("b");
            }
            catch (NotSupportedException)
            {
            }
        }

        Assert.Same(original, frame.TextInputEvents.Single());
    }
}
