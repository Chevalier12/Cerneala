namespace Cerneala.VisualStudio;

using System;
using Cerneala.VisualStudio.Server;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

internal sealed class VisualStudioServerLog : ICernealaServerLog
{
    private static readonly Guid PaneGuid = new("5ea35394-9764-4253-9172-42e6afe2508c");
    private static IVsOutputWindowPane? pane;

    public void Info(string message)
    {
        ActivityLog.LogInformation("Cerneala", message);
        WriteOutput("[info] " + message);
    }

    public void Error(string message, Exception? exception = null)
    {
        string details = exception is null ? message : message + " " + exception.Message;
        ActivityLog.LogError("Cerneala", details);
        WriteOutput("[error] " + details);
    }

    public void InitializeOutputPane()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        try
        {
            pane ??= GetOrCreatePane();
        }
        catch (Exception exception)
        {
            ActivityLog.LogError(
                "Cerneala",
                "The Cerneala output pane could not be initialized. " + exception.Message);
        }
    }

    private static void WriteOutput(string message)
    {
        IVsOutputWindowPane? outputPane = pane;
        if (outputPane is not null)
        {
#pragma warning disable VSTHRD010 // OutputStringThreadSafe is the background-thread API for this COM pane.
            ErrorHandler.ThrowOnFailure(outputPane.OutputStringThreadSafe(message + Environment.NewLine));
#pragma warning restore VSTHRD010
        }
    }

    private static IVsOutputWindowPane GetOrCreatePane()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        IVsOutputWindow outputWindow = ServiceProvider.GlobalProvider.GetService(typeof(SVsOutputWindow))
            as IVsOutputWindow
            ?? throw new InvalidOperationException("Visual Studio output service is unavailable.");
        Guid paneGuid = PaneGuid;
        ErrorHandler.ThrowOnFailure(outputWindow.CreatePane(
            ref paneGuid,
            "Cerneala",
            fInitVisible: 1,
            fClearWithSolution: 0));
        ErrorHandler.ThrowOnFailure(outputWindow.GetPane(ref paneGuid, out IVsOutputWindowPane outputPane));
        return outputPane;
    }
}
