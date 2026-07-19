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
    public void MonoGameDrawingBackendImplementsBackendInterface()
    {
        Assert.True(typeof(IDrawingBackend).IsAssignableFrom(typeof(MonoGameDrawingBackend)));
    }

    [Fact]
    public void MonoGameTextTextureKeyDistinguishesFontInstances()
    {
        Type keyType = typeof(MonoGameDrawingBackend).GetNestedType("TextTextureKey", System.Reflection.BindingFlags.NonPublic)!;
        System.Reflection.MethodInfo fromMethod = keyType.GetMethod("From")!;
        DrawTextRun firstRun = new(new SkiaFont(SkiaSharp.SKTypeface.Default, "Same", 16), "Cerneala", 16);
        DrawTextRun secondRun = new(new SkiaFont(SkiaSharp.SKTypeface.FromFamilyName("Times New Roman"), "Same", 16), "Cerneala", 16);

        object firstKey = fromMethod.Invoke(null, [firstRun, 1f, default(DrawPoint)])!;
        object secondKey = fromMethod.Invoke(null, [secondRun, 1f, default(DrawPoint)])!;

        Assert.NotEqual(firstKey, secondKey);
    }

    [Fact]
    public void MonoGameTextTextureKeyReusesEquivalentSkiaTypefaceWrappers()
    {
        Type keyType = typeof(MonoGameDrawingBackend).GetNestedType("TextTextureKey", System.Reflection.BindingFlags.NonPublic)!;
        System.Reflection.MethodInfo fromMethod = keyType.GetMethod("From")!;
        DrawTextRun firstRun = new(new SkiaFont(SkiaSharp.SKTypeface.Default, "Same", 16), "CONTINUE  ->", 16);
        DrawTextRun secondRun = new(new SkiaFont(SkiaSharp.SKTypeface.Default, "Same", 16), "CONTINUE  ->", 16);

        object firstKey = fromMethod.Invoke(null, [firstRun, 1f, default(DrawPoint)])!;
        object secondKey = fromMethod.Invoke(null, [secondRun, 1f, default(DrawPoint)])!;

        Assert.Equal(firstKey, secondKey);
    }
}
