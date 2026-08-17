namespace Cerneala.VisualStudio;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

internal sealed class CernealaOutputChannel(IVsOutputWindowPane pane)
{
    private static readonly Guid PaneGuid = new("5ea35394-9764-4253-9172-42e6afe2508c");

    public static async Task<CernealaOutputChannel> CreateAsync(
        AsyncPackage package,
        CancellationToken cancellationToken)
    {
        await package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        IVsOutputWindow outputWindow = await package.GetServiceAsync(typeof(SVsOutputWindow))
            as IVsOutputWindow
            ?? throw new InvalidOperationException("Visual Studio output service is unavailable.");
        Guid paneGuid = PaneGuid;
        ErrorHandler.ThrowOnFailure(outputWindow.CreatePane(
            ref paneGuid,
            "Cerneala",
            fInitVisible: 1,
            fClearWithSolution: 0));
        ErrorHandler.ThrowOnFailure(outputWindow.GetPane(ref paneGuid, out IVsOutputWindowPane outputPane));
        return new CernealaOutputChannel(outputPane);
    }

    public void WriteLine(string message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        ErrorHandler.ThrowOnFailure(pane.OutputString(message + Environment.NewLine));
    }

    public void Show()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        ErrorHandler.ThrowOnFailure(pane.Activate());
    }
}
