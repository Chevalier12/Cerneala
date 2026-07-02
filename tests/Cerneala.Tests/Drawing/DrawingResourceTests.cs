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
    public void BackendInterfaceConsumesCommandList()
    {
        Assert.NotNull(typeof(IDrawingBackend).GetMethod(nameof(IDrawingBackend.Render)));
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

        object firstKey = fromMethod.Invoke(null, [firstRun, DrawColor.White])!;
        object secondKey = fromMethod.Invoke(null, [secondRun, DrawColor.White])!;

        Assert.NotEqual(firstKey, secondKey);
    }
}
