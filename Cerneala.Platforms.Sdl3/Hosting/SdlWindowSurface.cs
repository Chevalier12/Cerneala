using Cerneala.UI.Hosting.Windowing;

namespace Cerneala.Platforms.Sdl3;

internal sealed class SdlWindowSurface : IWindowSurface
{
    public SdlWindowSurface(nint windowHandle, uint windowId)
    {
        if (windowHandle == 0)
        {
            throw new ArgumentException("An SDL window handle cannot be zero.", nameof(windowHandle));
        }

        if (windowId == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(windowId));
        }

        WindowHandle = windowHandle;
        WindowId = windowId;
    }

    internal nint WindowHandle { get; }

    internal uint WindowId { get; }
}
