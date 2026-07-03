using Cerneala.UI.Input;

namespace Cerneala.Tests.Input;

public sealed class ActionCommandTests
{
    [Fact]
    public void ExecutesDelegateWithParameter()
    {
        object? received = null;
        ActionCommand command = new(parameter => received = parameter);

        command.Execute("file");

        Assert.Equal("file", received);
    }

    [Fact]
    public void RespectsCanExecuteDelegate()
    {
        ActionCommand command = new(_ => throw new InvalidOperationException(), _ => false);

        command.Execute("file");

        Assert.False(command.CanExecute("file"));
    }

    [Fact]
    public void DefaultsToExecutable()
    {
        ActionCommand command = new(_ => { });

        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public void RejectsNullExecuteDelegate()
    {
        Assert.Throws<ArgumentNullException>(() => new ActionCommand(null!));
    }
}
