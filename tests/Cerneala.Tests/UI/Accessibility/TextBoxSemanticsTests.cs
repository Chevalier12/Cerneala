using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

namespace Cerneala.Tests.UI.Accessibility;

public sealed class TextBoxSemanticsTests
{
    [Fact]
    public void TextBoxExposesEditableTextRoleAndValue()
    {
        UIRoot root = new();
        TextBox textBox = new() { Text = "hello" };
        root.VisualChildren.Add(textBox);

        SemanticsNode node = new SemanticsProvider().Build(root).Root.Children.Single();

        Assert.Equal(SemanticsRole.EditableText, node.Role);
        Assert.Equal("hello", node.GetProperty<string>(SemanticsProperty.Value));
    }

    [Fact]
    public void PasswordBoxDoesNotExposePasswordValue()
    {
        UIRoot root = new();
        PasswordBox passwordBox = new() { Password = "secret" };
        root.VisualChildren.Add(passwordBox);

        SemanticsNode node = new SemanticsProvider().Build(root).Root.Children.Single();

        Assert.Equal(SemanticsRole.EditableText, node.Role);
        Assert.Null(node.GetProperty<string>(SemanticsProperty.Value));
    }

    [Fact]
    public void TextBoxBaseDerivedControlsExposeEditableTextRoleAndValue()
    {
        UIRoot root = new();
        SearchTextBox textBox = new() { Text = "query" };
        root.VisualChildren.Add(textBox);

        SemanticsNode node = new SemanticsProvider().Build(root).Root.Children.Single();

        Assert.Equal(SemanticsRole.EditableText, node.Role);
        Assert.Equal("query", node.GetProperty<string>(SemanticsProperty.Value));
    }

    private sealed class SearchTextBox : TextBoxBase
    {
    }
}
