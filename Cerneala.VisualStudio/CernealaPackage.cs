namespace Cerneala.VisualStudio;

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[InstalledProductRegistration("Cerneala", "Cerneala language support", "0.1.0")]
[ProvideMenuResource("Menus.ctmenu", 1)]
[Guid(PackageGuidString)]
public sealed class CernealaPackage : AsyncPackage
{
    public const string PackageGuidString = "f7d79e1c-8074-46ec-80ca-79347f6d896a";

    protected override async Task InitializeAsync(
        CancellationToken cancellationToken,
        IProgress<ServiceProgressData> progress)
    {
        CernealaOutputChannel output = await CernealaOutputChannel
            .CreateAsync(this, cancellationToken)
            .ConfigureAwait(false);
        IComponentModel componentModel = await GetServiceAsync(typeof(SComponentModel))
            as IComponentModel
            ?? throw new InvalidOperationException("Visual Studio component model is unavailable.");
        CernealaLanguageServerProvider languageServer = componentModel
            .GetService<CernealaLanguageServerProvider>();
        await RestartLanguageServerCommand
            .InitializeAsync(this, output, languageServer, cancellationToken)
            .ConfigureAwait(false);
    }
}
