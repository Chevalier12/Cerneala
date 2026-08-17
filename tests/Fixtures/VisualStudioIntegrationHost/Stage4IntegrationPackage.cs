namespace Cerneala.VisualStudio.IntegrationHost;

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[InstalledProductRegistration("Cerneala Stage 4 Integration Host", "Headless test host", "0.1.0")]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
[Guid(PackageGuid)]
public sealed class Stage4IntegrationPackage : AsyncPackage
{
    public const string PackageGuid = "9e0c367a-03d5-4f16-845d-2751454e12d8";
    private static readonly Guid CommandSet = new("52f83415-3e31-41d1-9017-a7ce582b2642");
    private const int RunCommandId = 0x0100;
    private JoinableTask? integrationTask;

    protected override async Task InitializeAsync(
        CancellationToken cancellationToken,
        IProgress<ServiceProgressData> progress)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        if (await GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
        {
            commandService.AddCommand(new MenuCommand(
                (_, _) => { },
                new CommandID(CommandSet, RunCommandId)));
        }

        string? extensionState = Environment.GetEnvironmentVariable("CERNEALA_STAGE5_EXTENSION_STATE");
        if (!string.IsNullOrWhiteSpace(extensionState))
        {
            string reportPath = Environment.GetEnvironmentVariable("CERNEALA_STAGE5_EXTENSION_STATE_REPORT")
                ?? throw new InvalidOperationException("The Stage 5 extension-state report path is missing.");
            Type serviceType = Type.GetType(
                "Microsoft.VisualStudio.ExtensionManager.SVsExtensionManager, Microsoft.VisualStudio.ExtensionManager",
                throwOnError: true);
            Type managerType = Type.GetType(
                "Microsoft.VisualStudio.ExtensionManager.IVsExtensionManager, Microsoft.VisualStudio.ExtensionManager",
                throwOnError: true);
            object extensionManager = await GetServiceAsync(serviceType)
                ?? throw new InvalidOperationException("Visual Studio extension manager is unavailable.");
            MethodInfo getInstalledExtension = managerType.GetMethod("GetInstalledExtension", new[] { typeof(string) })
                ?? throw new MissingMethodException(managerType.FullName, "GetInstalledExtension");
            object extension = getInstalledExtension.Invoke(
                extensionManager,
                new object[] { "Cerneala.Cerneala.VisualStudio" })
                ?? throw new InvalidOperationException("The Cerneala VSIX is not installed in the Experimental Instance.");
            string methodName = string.Equals(extensionState, "disable", StringComparison.OrdinalIgnoreCase)
                ? "Disable"
                : string.Equals(extensionState, "enable", StringComparison.OrdinalIgnoreCase)
                    ? "Enable"
                    : throw new InvalidOperationException("Unknown Stage 5 extension-state action: " + extensionState);
            MethodInfo stateMethod = managerType.GetMethods()
                .Single(method => method.Name == methodName && method.GetParameters().Length == 1);
            object? restartReason = stateMethod.Invoke(extensionManager, new[] { extension });
            object? installedState = extension.GetType().GetProperty("State")?.GetValue(extension);
            File.WriteAllText(reportPath, extensionState + ":" + restartReason + ":" + installedState);
            if (await GetServiceAsync(typeof(EnvDTE.DTE)) is EnvDTE80.DTE2 stateDte)
            {
                stateDte.ExecuteCommand("File.Exit");
            }
            return;
        }

        string? resilienceName = Environment.GetEnvironmentVariable("CERNEALA_STAGE5_RESILIENCE");
        if (!string.IsNullOrWhiteSpace(resilienceName))
        {
            integrationTask = JoinableTaskFactory.RunAsync(() => RunStage5ResilienceProbeAsync(
                resilienceName!,
                Environment.GetEnvironmentVariable("CERNEALA_STAGE5_RESILIENCE_VIEW")
                    ?? throw new InvalidOperationException("The resilience view path is missing."),
                Environment.GetEnvironmentVariable("CERNEALA_STAGE5_RESILIENCE_REPORT")
                    ?? throw new InvalidOperationException("The resilience report path is missing."),
                int.Parse(Environment.GetEnvironmentVariable("CERNEALA_STAGE5_RESILIENCE_SECONDS") ?? "5")));
            integrationTask.FileAndForget("Cerneala/Stage5Resilience");
            return;
        }

        string? stage5RequestPath = Environment.GetEnvironmentVariable("CERNEALA_STAGE5_REQUEST");
        if (!string.IsNullOrWhiteSpace(stage5RequestPath))
        {
            integrationTask = JoinableTaskFactory.RunAsync(async () =>
            {
                VisualStudioStage5Runner runner = await VisualStudioStage5Runner.CreateAsync(
                    this,
                    stage5RequestPath!,
                    DisposalToken);
                await runner.RunAsync();
            });
            integrationTask.FileAndForget("Cerneala/Stage5Integration");
            return;
        }

        string? requestPath = Environment.GetEnvironmentVariable("CERNEALA_STAGE4_REQUEST");
        if (string.IsNullOrWhiteSpace(requestPath))
        {
            return;
        }

        integrationTask = JoinableTaskFactory.RunAsync(async () =>
        {
            VisualStudioStage4Runner runner = await VisualStudioStage4Runner.CreateAsync(
                this,
                requestPath!,
                DisposalToken);
            await runner.RunAsync();
        });
        integrationTask.FileAndForget("Cerneala/Stage4Integration");
    }

    private async Task RunStage5ResilienceProbeAsync(
        string name,
        string viewPath,
        string reportPath,
        int observationSeconds)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
        int[] baselineServerPids = Process.GetProcessesByName("Cerneala.LanguageServer")
            .Select(process =>
            {
                using (process)
                {
                    return process.Id;
                }
            })
            .ToArray();
        EnvDTE80.DTE2 dte = (EnvDTE80.DTE2)(await GetServiceAsync(typeof(EnvDTE.DTE))
            ?? throw new InvalidOperationException("Visual Studio DTE is unavailable."));
        bool passed = false;
        string details = string.Empty;
        try
        {
            await OpenTextViewWithRetryAsync(dte, viewPath, DisposalToken);
            await Task.Delay(TimeSpan.FromSeconds(observationSeconds), DisposalToken);
            await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
            int ownedServerCount = Process.GetProcessesByName("Cerneala.LanguageServer")
                .Count(process =>
                {
                    using (process)
                    {
                        return !baselineServerPids.Contains(process.Id);
                    }
                });
            bool responsive = dte.ActiveDocument is not null &&
                string.Equals(dte.ActiveDocument.FullName, viewPath, StringComparison.OrdinalIgnoreCase);
            passed = responsive && ownedServerCount == 0;
            details = "responsive=" + responsive +
                "; ownedServerCount=" + ownedServerCount +
                "; observationSeconds=" + observationSeconds;
        }
        catch (Exception exception)
        {
            details = exception.ToString();
        }
        finally
        {
            File.WriteAllLines(reportPath, new[] { name, passed.ToString(), details ?? string.Empty });
            dte.Solution.Close(false);
            dte.Quit();
        }
    }

    private async Task OpenTextViewWithRetryAsync(
        EnvDTE80.DTE2 dte,
        string viewPath,
        CancellationToken cancellationToken)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(30);
        COMException? lastFailure = null;
        while (DateTime.UtcNow < deadline)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            try
            {
                dte.ItemOperations.OpenFile(viewPath, EnvDTE.Constants.vsViewKindTextView);
                return;
            }
            catch (COMException exception)
            {
                lastFailure = exception;
            }

            await Task.Delay(250, cancellationToken);
        }

        throw new InvalidOperationException(
            "Visual Studio did not make the generic text editor available for the resilience probe.",
            lastFailure);
    }
}
