using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Hosting.Windowing;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlWindowSurfaceTests
{
    [Fact]
    public void SurfaceCarriesAnOpaqueWindowIdentityAcrossTheCoreContract()
    {
        SdlWindowSurface surface = new((nint)0x42, 7);

        Assert.IsAssignableFrom<IWindowSurface>(surface);
        Assert.Equal((nint)0x42, surface.WindowHandle);
        Assert.Equal(7u, surface.WindowId);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    public void SurfaceRejectsInvalidNativeIdentity(int handle, uint windowId)
    {
        Assert.ThrowsAny<ArgumentException>(() => new SdlWindowSurface((nint)handle, windowId));
    }
}
