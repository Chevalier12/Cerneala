using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.Tests.Controls;

public sealed class PasswordBoxTests
{
    [Fact]
    public void PasswordBoxStoresPasswordButRendersMaskText()
    {
        UIRoot root = new(200, 80);
        PasswordBox passwordBox = new() { Password = "secret" };
        root.VisualChildren.Add(passwordBox);
        passwordBox.Invalidate(InvalidationFlags.Measure | InvalidationFlags.Arrange | InvalidationFlags.Render, "Initial password test frame");
        root.ProcessFrame();

        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Equal("secret", passwordBox.Password);
        Assert.Contains(commands, command => command.Kind == DrawCommandKind.DrawText && command.Text == "******");
        Assert.DoesNotContain(commands, command => command.Text == "secret");
    }

    [Fact]
    public void PasswordBoxUndoDoesNotClearProgrammaticInitialPassword()
    {
        PasswordBox passwordBox = new() { Password = "secret" };
        passwordBox.ReceiveTextInput("!");

        Assert.True(passwordBox.Undo());
        Assert.False(passwordBox.Undo());

        Assert.Equal("secret", passwordBox.Password);
    }
}
