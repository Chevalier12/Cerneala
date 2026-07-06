using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Input;

public sealed class CommandStateSchedulerTests
{
    [Fact]
    public void CommandSourceAttachedWithCannotExecuteCommandDisablesBeforeStyleAndRender()
    {
        UIRoot root = RootWithButton(out ButtonBase button);
        button.Command = new ActionCommand(_ => { }, _ => false);

        FrameStats stats = root.ProcessFrame();

        Assert.False(button.IsEnabled);
        Assert.True(stats.CommandStateElements > 0);
        Assert.True(stats.StyledElements > 0);
        Assert.True(stats.RenderedElements > 0);
    }

    [Fact]
    public void CommandPropertyChangeQueuesSingleCommandStateRefresh()
    {
        UIRoot root = RootWithButton(out ButtonBase button);
        root.ProcessFrame();

        button.Command = new ActionCommand(_ => { }, _ => false);
        FrameStats changed = root.ProcessFrame();
        FrameStats unchanged = root.ProcessFrame();

        Assert.False(button.IsEnabled);
        Assert.Equal(1, changed.CommandStateElements);
        Assert.Equal(0, unchanged.CommandStateElements);
    }

    [Fact]
    public void CommandParameterChangeQueuesSingleCommandStateRefresh()
    {
        UIRoot root = RootWithButton(out ButtonBase button);
        button.Command = new ActionCommand(_ => { }, parameter => Equals(parameter, "enabled"));
        button.CommandParameter = "enabled";
        root.ProcessFrame();

        button.CommandParameter = "disabled";
        FrameStats changed = root.ProcessFrame();
        FrameStats unchanged = root.ProcessFrame();

        Assert.False(button.IsEnabled);
        Assert.Equal(1, changed.CommandStateElements);
        Assert.Equal(0, unchanged.CommandStateElements);
    }

    [Fact]
    public void ObservableCommandCanExecuteChangedQueuesRefreshWithoutGlobalRequery()
    {
        UIRoot root = RootWithButton(out ButtonBase button);
        bool canExecute = true;
        ActionCommand command = new(_ => { }, _ => canExecute);
        button.Command = command;
        root.ProcessFrame();

        canExecute = false;
        command.RaiseCanExecuteChanged();
        FrameStats changed = root.ProcessFrame();
        FrameStats unchanged = root.ProcessFrame();

        Assert.False(button.IsEnabled);
        Assert.Equal(1, changed.CommandStateElements);
        Assert.Equal(0, unchanged.CommandStateElements);
    }

    [Fact]
    public void UnchangedSecondFrameDoesNotRefreshCommandStateAgain()
    {
        UIRoot root = RootWithButton(out ButtonBase button);
        button.Command = new ActionCommand(_ => { }, _ => false);

        FrameStats first = root.ProcessFrame();
        FrameStats second = root.ProcessFrame();

        Assert.True(first.CommandStateElements > 0);
        Assert.Equal(0, second.CommandStateElements);
        Assert.Equal(1, second.NoWorkFrames);
    }

    [Fact]
    public void DetachedCommandStateRefreshRunsAfterAttach()
    {
        UIRoot root = new(100, 100);
        ButtonBase button = new();
        button.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 40)));
        button.Command = new ActionCommand(_ => { }, _ => false);

        root.VisualChildren.Add(button);
        FrameStats stats = root.ProcessFrame();

        Assert.False(button.IsEnabled);
        Assert.Equal(1, stats.CommandStateElements);
    }

    [Fact]
    public void MultipleCommandSourcesRefreshInSameFrame()
    {
        UIRoot root = new(100, 100);
        ButtonBase first = new();
        ButtonBase second = new();
        first.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 40)));
        second.Arrange(new ArrangeContext(new LayoutRect(50, 0, 40, 40)));
        first.Command = new ActionCommand(_ => { }, _ => false);
        second.Command = new ActionCommand(_ => { }, _ => false);
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);

        FrameStats stats = root.ProcessFrame();

        Assert.Equal(2, stats.CommandStateElements);
        Assert.False(first.IsEnabled);
        Assert.False(second.IsEnabled);
    }

    private static UIRoot RootWithButton(out ButtonBase button)
    {
        UIRoot root = new(100, 100);
        button = new ButtonBase();
        button.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 40)));
        root.VisualChildren.Add(button);
        return root;
    }
}
