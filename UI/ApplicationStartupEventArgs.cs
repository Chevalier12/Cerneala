namespace Cerneala.UI;

public sealed class ApplicationStartupEventArgs : EventArgs
{
    internal ApplicationStartupEventArgs(IReadOnlyList<string> args)
    {
        Args = args;
    }

    public IReadOnlyList<string> Args { get; }
}
