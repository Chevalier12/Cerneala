using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

namespace Cerneala.Tests.Controls;

public sealed class DataTemplateTests
{
    [Fact]
    public void TypedTemplateReceivesTypedData()
    {
        string? received = null;
        DataTemplate<string> template = new(value =>
        {
            received = value;
            return new UIElement();
        });

        UIElement? element = template.CreateElement("data");

        Assert.NotNull(element);
        Assert.Equal("data", received);
    }

    [Fact]
    public void TypedTemplateRejectsIncompatibleData()
    {
        DataTemplate<string> template = new(_ => new UIElement());

        Assert.Throws<InvalidOperationException>(() => template.CreateElement(42));
    }

    [Fact]
    public void NullDataProducesNoChild()
    {
        DataTemplate<string> template = new(_ => new UIElement());

        Assert.Null(template.CreateElement(null));
    }
}
