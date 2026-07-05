using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.UI.Text;

public sealed class TextBlockTextServiceIntegrationTests
{
    [Fact]
    public void TextBoxBaseUsesRootResourceProviderWhenLocalProviderIsNull()
    {
        ResourceStore store = new();
        ResourceId<FontResource> id = new("Input");
        store.SetResource(id, new FontResource(new TestFont("Input", 12)));
        UIRoot root = new(100, 40);
        root.SetResourceProvider(store);
        TextBox textBox = new()
        {
            Text = "Hello",
            FontResourceId = id
        };
        root.VisualChildren.Add(textBox);

        Exception? exception = Record.Exception(() => textBox.Measure(new MeasureContext(new LayoutSize(100, 40))));

        Assert.Null(exception);
    }

    private sealed record TestFont(string FamilyName, float Size) : IDrawFont;
}
