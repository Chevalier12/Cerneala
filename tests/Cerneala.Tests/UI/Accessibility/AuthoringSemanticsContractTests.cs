using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;

namespace Cerneala.Tests.UI.Accessibility;

public sealed class AuthoringSemanticsContractTests
{
    [Fact]
    public void ButtonSemanticsIncludesRoleNameEnabledAndFocusState()
    {
        UIRoot root = new();
        Button button = new() { Content = "Save", IsKeyboardFocused = true };
        root.VisualChildren.Add(button);

        SemanticsNode node = Find(root.GetSemanticsTree().Root, SemanticsRole.Button);

        Assert.Equal("Save", node.Name);
        Assert.True(node.GetProperty<bool>(SemanticsProperty.IsEnabled));
        Assert.True(node.GetProperty<bool>(SemanticsProperty.IsFocused));
    }

    [Fact]
    public void TextBlockSemanticsUsesTextAsName()
    {
        UIRoot root = new();
        root.VisualChildren.Add(new TextBlock { Text = "Status" });

        SemanticsNode node = Find(root.GetSemanticsTree().Root, SemanticsRole.Text);

        Assert.Equal("Status", node.Name);
    }

    [Fact]
    public void TextBoxSemanticsIncludesEditableTextRoleAndValue()
    {
        UIRoot root = new();
        root.VisualChildren.Add(new TextBox { Text = "abc" });

        SemanticsNode node = Find(root.GetSemanticsTree().Root, SemanticsRole.EditableText);

        Assert.Equal("abc", node.GetProperty<string>(SemanticsProperty.Value));
    }

    [Fact]
    public void ObservableListMutationInvalidatesListSemantics()
    {
        UIRoot root = new();
        ObservableList<string> items = new(["one"]);
        ListBox listBox = new() { ItemsSource = items };
        root.VisualChildren.Add(listBox);
        SemanticsNode before = Find(root.GetSemanticsTree().Root, SemanticsRole.List);

        items.Add("two");
        SemanticsNode after = Find(root.GetSemanticsTree().Root, SemanticsRole.List);

        Assert.Equal(1, before.GetProperty<int>(SemanticsProperty.ItemCount));
        Assert.Equal(2, after.GetProperty<int>(SemanticsProperty.ItemCount));
    }

    private static SemanticsNode Find(SemanticsNode node, SemanticsRole role)
    {
        if (node.Role == role)
        {
            return node;
        }

        foreach (SemanticsNode child in node.Children)
        {
            try
            {
                return Find(child, role);
            }
            catch (InvalidOperationException)
            {
            }
        }

        throw new InvalidOperationException($"No semantics node with role {role}.");
    }
}
