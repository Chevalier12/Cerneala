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
    public void ScancodesMapToInputKeys(int scancode, InputKey expected)
    {
        SdlInputSource source = new();
        source.SetKey(scancode, true);
        InputFrame frame = source.GetFrame();
        Assert.True(frame.Keyboard.IsDown(expected));
        Assert.True(frame.Keyboard.IsPressed(expected));
        source.SetKey(scancode, false);
        Assert.True(source.GetFrame().Keyboard.IsReleased(expected));
    }

    [Fact]
    public void UnknownScancodesDoNotPublishKeys()
    {
        SdlInputSource source = new();
        source.SetKey(9999, true);
        Assert.False(source.GetFrame().Keyboard.IsDown(InputKey.Unknown));
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
        InputFrame frame = source.GetFrame();
        Assert.Equal(20, frame.Pointer.X);
        Assert.Equal(40, frame.Pointer.Y);
        Assert.Equal(240, frame.Pointer.WheelValue);
        Assert.Equal(240, frame.Pointer.WheelDelta);
        Assert.True(frame.Pointer.IsDown(InputMouseButton.Left));
        Assert.True(frame.Pointer.IsDown(InputMouseButton.Right));
        Assert.True(frame.Pointer.IsPressed(InputMouseButton.Left));
        Assert.False(frame.Pointer.IsDown(InputMouseButton.Middle));
    }
}
