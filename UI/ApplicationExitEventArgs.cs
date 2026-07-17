namespace Cerneala.UI;

public sealed class ApplicationExitEventArgs : EventArgs
{
    internal ApplicationExitEventArgs(int exitCode)
    {
        ExitCode = exitCode;
    }

    public int ExitCode { get; }
}
