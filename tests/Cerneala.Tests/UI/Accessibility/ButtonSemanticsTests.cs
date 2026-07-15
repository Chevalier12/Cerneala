using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

namespace Cerneala.Tests.UI.Accessibility;

public sealed class ButtonSemanticsTests
{
    [Fact]
    public void ButtonExposesRoleNameAndEnabledState()
    {
        UIRoot root = new();
        Button button = new() { Content = "Save", IsEnabled = false };
        root.VisualChildren.Add(button);

        SemanticsNode node = new SemanticsProvider().Build(root).Root.Children.Single();

        Assert.Equal(SemanticsRole.Button, node.Role);
        Assert.Equal("Save", node.Name);
        Assert.False(node.GetProperty<bool>(SemanticsProperty.IsEnabled));
    }

    [Fact]
    public void ButtonAutomationPeerInvokesEnabledButtonThroughClickRoute()
    {
        Button button = new();
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        ButtonAutomationPeer peer = new(button);

        bool invoked = peer.Invoke();

        Assert.True(invoked);
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void ButtonAutomationPeerDoesNotInvokeDisabledButton()
    {
        Button button = new() { IsEnabled = false };
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        ButtonAutomationPeer peer = new(button);

        bool invoked = peer.Invoke();

        Assert.False(invoked);
        Assert.Equal(0, clicks);
    }
}
