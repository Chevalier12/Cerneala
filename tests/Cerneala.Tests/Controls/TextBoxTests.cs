using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.Tests.Controls;

public sealed class TextBoxTests
{
    [Fact]
    public void TextInputBridgeUpdatesFocusedTextBoxText()
    {
        UIRoot root = new(200, 80);
        TextBox textBox = new();
        root.VisualChildren.Add(textBox);
        ElementInputRouteMap routeMap = new ElementInputRouteBuilder().Build(root);
        FocusManager focusManager = new();
        focusManager.Focus(textBox, routeMap);

        new TextInputBridge().Dispatch([new TextInputSnapshotEvent("a")], focusManager, routeMap);

        Assert.Equal("a", textBox.Text);
        Assert.True(textBox.DirtyState.Has(Cerneala.UI.Invalidation.InvalidationFlags.Render));
    }

    [Fact]
    public void TextBoxUndoRestoresPreviousText()
    {
        TextBox textBox = new() { Text = "ab" };
        textBox.ReceiveTextInput("c");

        Assert.True(textBox.Undo());

        Assert.Equal("ab", textBox.Text);
    }

    [Fact]
    public void TextBoxUndoDoesNotClearProgrammaticInitialText()
    {
        TextBox textBox = new() { Text = "ab" };
        textBox.ReceiveTextInput("c");

        Assert.True(textBox.Undo());
        Assert.False(textBox.Undo());

        Assert.Equal("ab", textBox.Text);
    }
}
