using System.Diagnostics;
using System.Runtime.InteropServices;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windows;
using Cerneala.Drawing;

namespace Cerneala.Tests.UI.Hosting;

public sealed class Win32WindowPlatformTests
{
    [Fact]
    public void NativeWindowsBelongToCurrentProcessAndUseNativeOwnership()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using Win32WindowPlatform platform = new();
        CallbackSink callbacks = new();
        using IPlatformWindow owner = platform.CreateWindow(new Window { Title = "Owner" }, callbacks);
        using IPlatformWindow child = platform.CreateWindow(new Window { Title = "Child" }, callbacks);

        child.SetOwner(owner);
        GetWindowThreadProcessId(owner.Handle, out uint ownerProcessId);
        GetWindowThreadProcessId(child.Handle, out uint childProcessId);

        Assert.Equal((uint)Process.GetCurrentProcess().Id, ownerProcessId);
        Assert.Equal(ownerProcessId, childProcessId);
        Assert.Equal(owner.Handle, GetWindow(child.Handle, 4));
        Assert.True(IsWindow(owner.Handle));
        Assert.True(IsWindow(child.Handle));

        DrawCommandList commands = new();
        commands.Add(DrawCommand.FillRectangle(new DrawRect(0, 0, 32, 32), new DrawColor(20, 40, 60)));
        owner.DrawingBackend.Render(commands);
        owner.Show();
        owner.Present();
        platform.PumpEvents();

        child.Destroy();
        owner.Destroy();
        Assert.False(IsWindow(child.Handle));
        Assert.False(IsWindow(owner.Handle));
    }

    private sealed class CallbackSink : IWindowPlatformCallbacks
    {
        public void RequestClose() { }

        public void ActivationChanged(bool active) { }

        public void BoundsChanged(UiViewport viewport, float left, float top, WindowState state) { }

        public void RenderRequested() { }
    }

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint window, out uint processId);

    [DllImport("user32.dll")]
    private static extern nint GetWindow(nint window, uint command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(nint window);
}
