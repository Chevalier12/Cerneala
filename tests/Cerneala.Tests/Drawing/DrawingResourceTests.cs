using Cerneala.Drawing;
using Cerneala.Backends.SdlGpu;
using Cerneala.Tests.Drawing.SdlGpu;
using Cerneala.UI.Hosting;
using Cerneala.Drawing.Text;

namespace Cerneala.Tests.Drawing;

public sealed class DrawingResourceTests
{
    [Fact]
    public void SdlGpuImageRejectsNullPixels()
    {
        Assert.Throws<ArgumentNullException>(() => new SdlGpuImage(1, 1, null!));
    }

    [Fact]
    public void SkiaFontRejectsNullTypeface()
    {
        Assert.Throws<ArgumentNullException>(() => new SkiaFont(null!, "Arial", 16));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SkiaFontRejectsEmptyFamilyName(string familyName)
    {
        Assert.Throws<ArgumentException>(() => new SkiaFont(SkiaSharp.SKTypeface.Default, familyName, 16));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.MaxValue)]
    public void SkiaFontRejectsInvalidSize(float size)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SkiaFont(SkiaSharp.SKTypeface.Default, "Arial", size));
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
    public void SystemFontSourceResolvesNamedFontWeightInsteadOfFallingBack()
    {
        SystemFontSource fonts = new();

        SkiaFont font = Assert.IsType<SkiaFont>(fonts.LoadFont("Arial Bold", 10));

        Assert.Equal("Arial", font.Typeface.FamilyName);
        Assert.Equal(700, font.Typeface.FontWeight);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SystemFontSourceRejectsEmptyFamilyName(string familyName)
    {
        SystemFontSource fonts = new();

        Assert.Throws<ArgumentException>(() => fonts.LoadFont(familyName, 16));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.MaxValue)]
    public void SystemFontSourceRejectsInvalidSize(float size)
    {
        SystemFontSource fonts = new();

        Assert.Throws<ArgumentOutOfRangeException>(() => fonts.LoadFont("Arial", size));
    }

    [Fact]
    public void BackendInterfaceConsumesCommandListAndTypedFrameContext()
    {
        System.Reflection.MethodInfo? render =
            typeof(IDrawingBackend).GetMethod(nameof(IDrawingBackend.Render));
        Assert.NotNull(render);
        System.Reflection.ParameterInfo[] parameters = render.GetParameters();

        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(DrawCommandList), parameters[0].ParameterType);
        Assert.Equal(
            typeof(DrawingFrameContext).MakeByRefType(),
            parameters[1].ParameterType);
    }

    [Fact]
    public void SdlGpuDrawingBackendImplementsBackendInterface()
    {
        Assert.True(typeof(IDrawingBackend).IsAssignableFrom(typeof(SdlGpuDrawingBackend)));
    }

    [Fact]
    public void SdlTextCacheDistinguishesDifferentTypefaces()
    {
        using SdlDrawingFixture fixture = new(160, 80);
        DrawTextRun firstRun = new(new SkiaFont(SkiaSharp.SKTypeface.Default, "Same", 16), "Cerneala", 16);
        DrawTextRun secondRun = new(new SkiaFont(SkiaSharp.SKTypeface.FromFamilyName("Times New Roman"), "Same", 16), "Cerneala", 16);

        RenderText(fixture, firstRun);
        RenderText(fixture, secondRun);
        Assert.True(((IDrawingBackendFrameTimingSource)fixture.Backend).LastFrameTiming.TextRequestCount > 0);
    }

    [Fact]
    public void SdlTextCacheReusesEquivalentSkiaTypefaceWrappers()
    {
        using SdlDrawingFixture fixture = new(160, 80);
        DrawTextRun firstRun = new(new SkiaFont(SkiaSharp.SKTypeface.Default, "Same", 16), "CONTINUE  ->", 16);
        DrawTextRun secondRun = new(new SkiaFont(SkiaSharp.SKTypeface.Default, "Same", 16), "CONTINUE  ->", 16);

        RenderText(fixture, firstRun);
        RenderText(fixture, secondRun);
        Assert.Equal(0, ((IDrawingBackendFrameTimingSource)fixture.Backend).LastFrameTiming.TextRequestCount);
    }

    private static void RenderText(SdlDrawingFixture fixture, DrawTextRun run)
    {
        DrawCommandList commands = new();
        commands.Add(DrawCommand.DrawText(run, default, Color.White));
        fixture.Render(commands);
    }
}
