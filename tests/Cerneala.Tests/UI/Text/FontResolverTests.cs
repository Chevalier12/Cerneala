using Cerneala.Drawing;
using Cerneala.UI.Text;

namespace Cerneala.Tests.UI.Text;

public sealed class FontResolverTests
{
    [Fact]
    public void ResolveUsesExplicitFontSource()
    {
        RecordingFontSource source = new();
        FontResolver resolver = new(source);

        ResolvedTextFont font = resolver.Resolve("Serif", 13);

        Assert.Equal("Serif", source.FamilyName);
        Assert.Equal(13, source.Size);
        Assert.Equal("Serif", font.Font.FamilyName);
        Assert.Equal(13, font.Font.Size);
        Assert.Equal("Serif:13", font.Identity);
    }

    [Fact]
    public void ResolveUsesSystemFontSourceByDefault()
    {
        FontResolver resolver = new();

        ResolvedTextFont font = resolver.Resolve("Default", 16);

        Assert.Equal("Default", font.Font.FamilyName);
        Assert.Equal(16, font.Font.Size);
        Assert.Equal("Default:16", font.Identity);
        Assert.IsType<Cerneala.Drawing.Text.SkiaFont>(font.Font);
    }

    [Fact]
    public void ResolveDoesNotFallbackWhenExplicitFontSourceReturnsNull()
    {
        FontResolver resolver = new(new NullFontSource());

        Assert.Throws<ArgumentNullException>(() => resolver.Resolve("Default", 16));
    }

    [Fact]
    public void ResolveRejectsInvalidInputs()
    {
        FontResolver resolver = new();

        Assert.Throws<ArgumentException>(() => resolver.Resolve("", 16));
        Assert.Throws<ArgumentOutOfRangeException>(() => resolver.Resolve("Default", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => resolver.Resolve("Default", float.NaN));
    }

    private sealed class RecordingFontSource : IFontSource
    {
        public string? FamilyName { get; private set; }

        public float Size { get; private set; }

        public IDrawFont LoadFont(string familyName, float size)
        {
            FamilyName = familyName;
            Size = size;
            return new TestFont(familyName, size);
        }
    }

    private sealed record TestFont(string FamilyName, float Size) : IDrawFont;

    private sealed class NullFontSource : IFontSource
    {
        public IDrawFont LoadFont(string familyName, float size)
        {
            return null!;
        }
    }
}
