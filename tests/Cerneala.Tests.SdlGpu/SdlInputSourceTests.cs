using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Input;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlInputSourceTests
{
    [Theory]
    [InlineData(40, InputKey.Enter)]
    [InlineData(41, InputKey.Escape)]
    [InlineData(4, InputKey.A)]
    [InlineData(225, InputKey.LeftShift)]
    [InlineData(224, InputKey.LeftCtrl)]
    [InlineData(226, InputKey.LeftAlt)]
    [InlineData(98, InputKey.NumPad0)]
    [InlineData(89, InputKey.NumPad1)]
    [InlineData(93, InputKey.NumPad5)]
    [InlineData(97, InputKey.NumPad9)]
    [InlineData(85, InputKey.Multiply)]
    [InlineData(87, InputKey.Add)]
    [InlineData(86, InputKey.Subtract)]
    [InlineData(99, InputKey.Decimal)]
    [InlineData(84, InputKey.Divide)]
    [InlineData(51, InputKey.OemSemicolon)]
    [InlineData(46, InputKey.OemPlus)]
    [InlineData(54, InputKey.OemComma)]
    [InlineData(45, InputKey.OemMinus)]
    [InlineData(55, InputKey.OemPeriod)]
    [InlineData(56, InputKey.OemQuestion)]
    [InlineData(53, InputKey.OemTilde)]
    [InlineData(47, InputKey.OemOpenBrackets)]
    [InlineData(49, InputKey.OemPipe)]
    [InlineData(48, InputKey.OemCloseBrackets)]
    [InlineData(52, InputKey.OemQuotes)]
    [InlineData(100, InputKey.OemBackslash)]
    public void ScancodesMapToInputKeys(int scancode, InputKey expected)
    {
        SdlInputSource source = new();
        source.SetKey(scancode, true, numLock: true);
        InputFrame frame = source.GetFrame();
        Assert.True(frame.Keyboard.IsDown(expected));
        Assert.True(frame.Keyboard.IsPressed(expected));
        source.SetKey(scancode, false, numLock: true);
        Assert.True(source.GetFrame().Keyboard.IsReleased(expected));
    }

    [Theory]
    [InlineData(93, InputKey.NumPad5, "5")]
    [InlineData(55, InputKey.OemPeriod, ".")]
    public void KeypadAndPunctuationKeysArePressedWithTheTextTheyType(int scancode, InputKey expected, string text)
    {
        SdlInputSource source = new();
        source.SetKey(scancode, true, numLock: true);
        source.AddText(text);
        InputFrame frame = source.GetFrame();

        Assert.Equal([expected], Enum.GetValues<InputKey>().Where(frame.Keyboard.IsPressed));
        Assert.Equal([text], frame.TextInputEvents.Select(textInput => textInput.Text));
    }

    // WPF reports keypad Enter as Key.Enter.
    [Fact]
    public void KeypadEnterIsEnter()
    {
        SdlInputSource source = new();
        source.SetKey(88, true);
        Assert.True(source.GetFrame().Keyboard.IsPressed(InputKey.Enter));
        source.SetKey(88, false);
        Assert.True(source.GetFrame().Keyboard.IsReleased(InputKey.Enter));
    }

    [Fact]
    public void UnknownScancodesDoNotPublishKeys()
    {
        SdlInputSource source = new();
        source.SetKey(9999, true);
        Assert.False(source.GetFrame().Keyboard.IsDown(InputKey.Unknown));
    }

    [Fact]
    public void RepeatOfHeldKeyIsPublishedOnceForTheNextFrameOnly()
    {
        SdlInputSource source = new();
        source.SetKey(42, true);
        InputFrame pressed = source.GetFrame();
        source.SetKey(42, true, repeat: true);
        source.SetKey(42, true, repeat: true);
        InputFrame repeated = source.GetFrame();
        InputFrame idle = source.GetFrame();

        Assert.True(pressed.Keyboard.IsPressed(InputKey.Back));
        Assert.Equal(0, pressed.Keyboard.GetRepeatCount(InputKey.Back));
        Assert.False(repeated.Keyboard.IsPressed(InputKey.Back));
        Assert.True(repeated.Keyboard.IsDown(InputKey.Back));
        Assert.Equal(2, repeated.Keyboard.GetRepeatCount(InputKey.Back));
        Assert.Equal(0, idle.Keyboard.GetRepeatCount(InputKey.Back));
    }

    [Fact]
    public void ForgottenKeysArePublishedInArrivalOrderAndDroppedWithoutARelease()
    {
        SdlInputSource source = new();
        source.SetKey(4, true);
        InputFrame held = source.GetFrame();
        source.SetKey(40, true);
        source.ForgetKeys();
        source.SetKey(40, true);
        InputFrame pressed = source.GetFrame();
        InputFrame forgotten = source.GetFrame();
        InputFrame pressedAgain = source.GetFrame();

        Assert.True(held.Keyboard.IsPressed(InputKey.A));
        Assert.True(pressed.Keyboard.IsPressed(InputKey.Enter));
        Assert.True(pressed.Keyboard.IsDown(InputKey.A));
        Assert.False(forgotten.Keyboard.IsDown(InputKey.A));
        Assert.False(forgotten.Keyboard.IsReleased(InputKey.A));
        Assert.False(forgotten.Keyboard.IsReleased(InputKey.Enter));
        Assert.True(forgotten.Keyboard.IsPressed(InputKey.Enter));
        Assert.False(pressedAgain.Keyboard.IsPressed(InputKey.Enter));
        Assert.True(pressedAgain.Keyboard.IsDown(InputKey.Enter));
    }

    [Fact]
    public void ForgottenButtonsArePublishedInArrivalOrderAndDroppedWithoutARelease()
    {
        SdlInputSource source = new();
        source.SetButton(3, true);
        InputFrame held = source.GetFrame();
        source.SetButton(1, true);
        source.ForgetButtons();
        source.SetButton(1, true);
        InputFrame pressed = source.GetFrame();
        InputFrame forgotten = source.GetFrame();
        InputFrame pressedAgain = source.GetFrame();

        Assert.True(held.Pointer.IsPressed(InputMouseButton.Right));
        Assert.True(pressed.Pointer.IsPressed(InputMouseButton.Left));
        Assert.True(pressed.Pointer.IsDown(InputMouseButton.Right));
        Assert.False(forgotten.Pointer.IsDown(InputMouseButton.Right));
        Assert.False(forgotten.Pointer.IsReleased(InputMouseButton.Right));
        Assert.False(forgotten.Pointer.IsReleased(InputMouseButton.Left));
        Assert.True(forgotten.Pointer.IsPressed(InputMouseButton.Left));
        Assert.False(pressedAgain.Pointer.IsPressed(InputMouseButton.Left));
        Assert.True(pressedAgain.Pointer.IsDown(InputMouseButton.Left));
    }

    [Fact]
    public void RepeatOfKeyThatIsNotDownIsAPressNotARepeat()
    {
        SdlInputSource source = new();
        source.SetKey(42, true, repeat: true);
        InputFrame frame = source.GetFrame();

        Assert.True(frame.Keyboard.IsPressed(InputKey.Back));
        Assert.Equal(0, frame.Keyboard.GetRepeatCount(InputKey.Back));
    }

    [Fact]
    public void EventsOfDifferentKeysAreReportedInSeparateFramesInArrivalOrder()
    {
        SdlInputSource source = new();
        source.SetKey(225, true);
        _ = source.GetFrame();
        source.SetKey(80, true);
        source.SetKey(225, false);
        source.SetKey(81, true);

        InputFrame left = source.GetFrame();
        InputFrame shiftUp = source.GetFrame();
        InputFrame down = source.GetFrame();

        Assert.True(left.Keyboard.IsPressed(InputKey.Left));
        Assert.True(left.Keyboard.IsDown(InputKey.LeftShift));
        Assert.False(left.Keyboard.IsPressed(InputKey.Down));
        Assert.True(shiftUp.Keyboard.IsReleased(InputKey.LeftShift));
        Assert.False(shiftUp.Keyboard.IsPressed(InputKey.Down));
        Assert.True(down.Keyboard.IsPressed(InputKey.Down));
        Assert.False(down.Keyboard.IsDown(InputKey.LeftShift));
    }

    [Fact]
    public void DefaultCoordinateScaleIsOne() => Assert.Equal(1, new SdlInputSource().CoordinateScale);

    [Fact]
    public void CoordinateScaleDividesMousePositionIntoLogicalCoordinates()
    {
        SdlInputSource source = new() { CoordinateScale = 2.5f };
        source.MovePointer(300, 150);
        InputFrame frame = source.GetFrame();
        Assert.Equal(120, frame.Pointer.X);
        Assert.Equal(60, frame.Pointer.Y);
    }

    [Fact]
    public void CoordinateScaleDoesNotAffectWheelDeltaOrButtons()
    {
        SdlInputSource source = new() { CoordinateScale = 4 };
        source.MovePointer(80, 160);
        source.AddWheel(2, flipped: false);
        source.SetButton(1, true);
        source.SetButton(3, true);
        InputFrame wheeled = source.GetFrame();
        Assert.Equal(20, wheeled.Pointer.X);
        Assert.Equal(40, wheeled.Pointer.Y);
        Assert.Equal(240, wheeled.Pointer.WheelValue);
        Assert.Equal(240, wheeled.Pointer.WheelDelta);
        // A press after a wheel notch waits for the next frame, which hit-tests the scrolled layout.
        Assert.False(wheeled.Pointer.IsDown(InputMouseButton.Left));
        InputFrame frame = source.GetFrame();
        Assert.True(frame.Pointer.IsDown(InputMouseButton.Left));
        Assert.True(frame.Pointer.IsPressed(InputMouseButton.Left));
        Assert.False(frame.Pointer.IsDown(InputMouseButton.Middle));
        // A frame holds one button transition; the Right press follows in the next frame.
        InputFrame next = source.GetFrame();
        Assert.True(next.Pointer.IsPressed(InputMouseButton.Right));
        Assert.True(next.Pointer.IsDown(InputMouseButton.Left));
        Assert.Equal(20, next.Pointer.X);
    }
}
