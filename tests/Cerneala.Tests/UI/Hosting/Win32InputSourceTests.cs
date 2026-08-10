using Cerneala.UI.Hosting.Windows;
using Cerneala.UI.Input;

namespace Cerneala.Tests.UI.Hosting;

public sealed class Win32InputSourceTests
{
    [Theory]
    [InlineData(0x10u, InputKey.LeftShift)]
    [InlineData(0x11u, InputKey.LeftCtrl)]
    [InlineData(0x12u, InputKey.LeftAlt)]
    public void GenericModifierVirtualKeys_AreMapped(uint virtualKey, InputKey expected)
    {
        Win32InputSource source = new();

        source.SetKey(virtualKey, true);
        InputFrame frame = source.GetFrame();

        Assert.True(frame.Keyboard.IsDown(expected));
        Assert.True(frame.Keyboard.IsPressed(expected));
    }
}
