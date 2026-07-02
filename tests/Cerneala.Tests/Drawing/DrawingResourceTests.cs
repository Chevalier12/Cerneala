using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.Drawing.Text;

namespace Cerneala.Tests.Drawing;

public sealed class DrawingResourceTests
{
    [Fact]
    public void MonoGameImageRejectsNullTexture()
    {
        Assert.Throws<ArgumentNullException>(() => new MonoGameImage(null!));
    }

    [Fact]
    public void SkiaFontRejectsNullTypeface()
    {
        Assert.Throws<ArgumentNullException>(() => new SkiaFont(null!, "Arial", 16));
    }

    [Fact]
    public void SystemFontSourceLoadsFontFromOperatingSystem()
    {
        SystemFontSource fonts = new();

        IDrawFont font = fonts.LoadFont("Arial", 16);

        Assert.IsType<SkiaFont>(font);
    }

    [Fact]
    public void SystemFontSourcePreservesRequestedFontMetadata()
    {
        SystemFontSource fonts = new();

        IDrawFont font = fonts.LoadFont("Arial", 16);

        Assert.Equal("Arial", font.FamilyName);
        Assert.Equal(16, font.Size);
    }

    [Fact]
    public void BackendInterfaceConsumesCommandList()
    {
        Assert.NotNull(typeof(IDrawingBackend).GetMethod(nameof(IDrawingBackend.Render)));
    }

    [Fact]
    public void MonoGameDrawingBackendImplementsBackendInterface()
    {
        Assert.True(typeof(IDrawingBackend).IsAssignableFrom(typeof(MonoGameDrawingBackend)));
    }
}
