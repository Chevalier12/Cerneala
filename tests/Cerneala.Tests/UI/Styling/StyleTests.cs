using Cerneala.UI.Controls;
using Cerneala.UI.Styling;

namespace Cerneala.Tests.UI.Styling;

public sealed class StyleTests
{
    [Fact]
    public void StyleStoresRulesInOrder()
    {
        StyleRule first = new(StyleSelector.ForType<Button>());
        StyleRule second = new(StyleSelector.ForType<TextBlock>());
        Style style = new("Controls");

        style.Add(first).Add(second);

        Assert.Equal("Controls", style.Name);
        Assert.Equal(new[] { first, second }, style.Rules);
    }

    [Fact]
    public void StyleRejectsEmptyName()
    {
        Assert.Throws<ArgumentException>(() => new Style(" "));
    }
}
