namespace Cerneala.VisualStudio;

using System;
using System.ComponentModel.Design;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

internal sealed class RestartLanguageServerCommand
{
    public const int CommandId = 0x0100;
    public static readonly Guid CommandSet = new("e3f8e801-d52b-4d85-a65a-749d33594e8c");

    private readonly CernealaOutputChannel output;
    private readonly CernealaLanguageServerProvider languageServer;
    private readonly AsyncPackage package;

    private RestartLanguageServerCommand(
        AsyncPackage package,
        CernealaOutputChannel output,
        CernealaLanguageServerProvider languageServer)
    {
        this.package = package;
        this.output = output;
        this.languageServer = languageServer;
    }

    public static async Task InitializeAsync(
        AsyncPackage package,
        CernealaOutputChannel output,
        CernealaLanguageServerProvider languageServer,
        CancellationToken cancellationToken)
    {
        await package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService))
            as OleMenuCommandService
            ?? throw new InvalidOperationException("Visual Studio command service is unavailable.");
        RestartLanguageServerCommand command = new(package, output, languageServer);
        commandService.AddCommand(new MenuCommand(
            command.Execute,
            new CommandID(CommandSet, CommandId)));
    }

    private void Execute(object? sender, EventArgs args)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        output.WriteLine("Restarting the Cerneala language server...");
        output.Show();
        package.JoinableTaskFactory.RunAsync(async delegate
            {
                try
                {
                    await languageServer.RestartAsync(CancellationToken.None).ConfigureAwait(false);
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    output.WriteLine("Cerneala language server restart requested successfully.");
                }
                catch (Exception exception)
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    output.WriteLine("Cerneala language server restart failed: " + exception.Message);
                    output.Show();
                }
            })
            .FileAndForget("Cerneala/RestartLanguageServer");
    }
}
