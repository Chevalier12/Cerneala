using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

namespace Cerneala.Tests.Controls;

public sealed class ContentTemplateTests
{
    [Fact]
    public void TypedTemplateReceivesTypedData()
    {
        string? received = null;
        ContentTemplate<string> template = new("test", key: null, priority: 0, context =>
        {
            received = context.Data;
            return new UIElement();
        });

        UIElement? element = template.Create(new ContentTemplateContext("data"));

        Assert.NotNull(element);
        Assert.Equal("data", received);
    }

    [Fact]
    public void TypedTemplateDoesNotMatchIncompatibleData()
    {
        ContentTemplate<string> template = new("test", key: null, priority: 0, _ => new UIElement());

        Assert.False(template.CanApply(new ContentTemplateMatchContext(42)));
    }

    [Fact]
    public void TypedTemplateDoesNotMatchNullData()
    {
        ContentTemplate<string> template = new("test", key: null, priority: 0, _ => new UIElement());

        Assert.False(template.CanApply(new ContentTemplateMatchContext(null)));
    }
}
