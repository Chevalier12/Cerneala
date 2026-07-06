using Cerneala.UI.Controls;

namespace Cerneala.Tests.UI.Text;

public sealed class TextBoxEditorIntegrationTests
{
    [Fact]
    public void TextInputReplacesSelectedRangeAndMovesCaretAfterInsertedText()
    {
        TextBox textBox = new() { Text = "hello" };
        textBox.Select(1, 4);

        textBox.ReceiveTextInput("a");

        Assert.Equal("hao", textBox.Text);
        Assert.Equal(2, textBox.Caret.Position);
        Assert.True(textBox.Selection.IsEmpty);
    }
}
