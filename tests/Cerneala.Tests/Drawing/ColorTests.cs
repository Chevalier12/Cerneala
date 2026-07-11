using System.Reflection;
using Cerneala.Drawing;

namespace Cerneala.Tests.Drawing;

public sealed class ColorTests
{
    [Fact]
    public void ExposesCompleteWpfNamedColorCatalog()
    {
        string[] names = typeof(Color)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(Color))
            .Select(property => property.Name)
            .ToArray();

        Assert.Equal(141, names.Length);
        Assert.Contains(nameof(Color.Transparent), names);
        Assert.Contains(nameof(Color.AliceBlue), names);
        Assert.Contains(nameof(Color.YellowGreen), names);
        Assert.Equal(new Color(240, 248, 255), Color.AliceBlue);
        Assert.Equal(new Color(154, 205, 50), Color.YellowGreen);
    }

    [Theory]
    [InlineData("aliceblue", 240, 248, 255, 255)]
    [InlineData("Transparent", 0, 0, 0, 0)]
    [InlineData("#1E90FF", 30, 144, 255, 255)]
    [InlineData("#801E90FF", 30, 144, 255, 128)]
    [InlineData("30, 144, 255", 30, 144, 255, 255)]
    [InlineData("30, 144, 255, 128", 30, 144, 255, 128)]
    public void TryParseAcceptsWpfNamesHexAndChannels(string text, byte r, byte g, byte b, byte a)
    {
        Assert.True(Color.TryParse(text, out Color color));
        Assert.Equal(new Color(r, g, b, a), color);
    }

    [Fact]
    public void FromArgbUsesWpfChannelOrder()
    {
        Assert.Equal(new Color(30, 144, 255, 128), Color.FromArgb(128, 30, 144, 255));
        Assert.Equal(new Color(30, 144, 255), Color.FromRgb(30, 144, 255));
    }

    [Theory]
    [InlineData("")]
    [InlineData("unknown")]
    [InlineData("#12")]
    [InlineData("1,2")]
    [InlineData("256,2,3")]
    public void TryParseRejectsInvalidValues(string text)
    {
        Assert.False(Color.TryParse(text, out _));
    }
}
