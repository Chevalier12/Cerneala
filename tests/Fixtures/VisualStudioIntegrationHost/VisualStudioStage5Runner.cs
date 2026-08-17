namespace Cerneala.VisualStudio.IntegrationHost;

#pragma warning disable VSTHRD010 // Every polling helper resumes on the Visual Studio UI thread before returning.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.TextManager.Interop;

internal sealed class VisualStudioStage5Runner
{
    private const double ProviderActivationCpuBudgetMs = 100;
    private const double ServerReadyColdBudgetMs = 2_000;
    private const double FirstCompletionColdBudgetMs = 2_500;
    private const long DevenvPlateauBudgetBytes = 96L * 1024 * 1024;
    private const long ServerPlateauBudgetBytes = 32L * 1024 * 1024;
    private const string RestartLanguageServerCommandName = "Tools.CernealaRestartLanguageServer";
    private static readonly Guid CernealaPackageGuid =
        new("f7d79e1c-8074-46ec-80ca-79347f6d896a");

    private readonly AsyncPackage package;
    private readonly Stage5Request request;
    private readonly CancellationToken cancellationToken;
    private readonly DTE2 dte;
    private readonly IVsShell shell;
    private readonly IVsSolution solution;
    private readonly IComponentModel componentModel;
    private readonly IVsEditorAdaptersFactoryService editorAdapters;
    private readonly IEditorCommandHandlerServiceFactory commandHandlers;
    private readonly ICompletionBroker completionBroker;
    private readonly IViewTagAggregatorFactoryService tagAggregators;
    private readonly Dictionary<IWpfTextView, IOleCommandTarget> commandTargets = new();
    private readonly Dictionary<IWpfTextView, ITagAggregator<IErrorTag>> errorTagAggregators = new();
    private readonly HashSet<int> baselineServerPids;
    private readonly HashSet<int> observedServerPids = new();
    private readonly Stage5Report report = new();

    private VisualStudioStage5Runner(
        AsyncPackage package,
        Stage5Request request,
        CancellationToken cancellationToken,
        DTE2 dte,
        IVsShell shell,
        IVsSolution solution,
        IComponentModel componentModel)
    {
        this.package = package;
        this.request = request;
        this.cancellationToken = cancellationToken;
        this.dte = dte;
        this.shell = shell;
        this.solution = solution;
        this.componentModel = componentModel;
        editorAdapters = componentModel.GetService<IVsEditorAdaptersFactoryService>();
        commandHandlers = componentModel.GetService<IEditorCommandHandlerServiceFactory>();
        completionBroker = componentModel.GetService<ICompletionBroker>();
        tagAggregators = componentModel.GetService<IViewTagAggregatorFactoryService>();
        baselineServerPids = GetServerPids().ToHashSet();
    }

    public static async Task<VisualStudioStage5Runner> CreateAsync(
        AsyncPackage package,
        string requestPath,
        CancellationToken cancellationToken)
    {
        await package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        Stage5Request request = Stage5Request.Read(requestPath);
        DTE2 dte = (DTE2)(await package.GetServiceAsync(typeof(EnvDTE.DTE))
            ?? throw new InvalidOperationException("Visual Studio DTE is unavailable."));
        IVsShell shell = (IVsShell)(await package.GetServiceAsync(typeof(SVsShell))
            ?? throw new InvalidOperationException("Visual Studio shell service is unavailable."));
        IVsSolution solution = (IVsSolution)(await package.GetServiceAsync(typeof(SVsSolution))
            ?? throw new InvalidOperationException("Visual Studio solution service is unavailable."));
        IComponentModel componentModel = (IComponentModel)(await package.GetServiceAsync(typeof(SComponentModel))
            ?? throw new InvalidOperationException("Visual Studio component model is unavailable."));
        return new VisualStudioStage5Runner(
            package,
            request,
            cancellationToken,
            dte,
            shell,
            solution,
            componentModel);
    }

    public async Task RunAsync()
    {
        await package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        report.StartedUtc = DateTimeOffset.UtcNow;
        report.HostEdition = dte.Edition;
        report.HostVersion = dte.Version;
        report.ProcessorName = request.ProcessorName;
        report.ProcessorCount = Environment.ProcessorCount;
        report.MemoryBytes = request.MemoryBytes;
        report.OperatingSystem = request.OperatingSystem;
        report.VisualStudioInstallation = request.VisualStudioInstallation;

        try
        {
            Check(
                "host.community-sku",
                dte.Edition.IndexOf("Community", StringComparison.OrdinalIgnoreCase) >= 0,
                $"Edition={dte.Edition}; Version={dte.Version}");
            await WaitForSolutionAsync(request.FixtureSolutionPath, TimeSpan.FromMinutes(2));

            DateTimeOffset processStarted = DateTimeOffset.Parse(request.ProcessStartedUtc);
            AddMetric(
                "host-ready-from-process-start",
                "fixture",
                (report.StartedUtc - processStarted).TotalMilliseconds,
                null,
                "wall-ms");

            await DelayOnUiThreadAsync(TimeSpan.FromSeconds(3));
            Check(
                "startup.no-cerneala-assembly",
                !IsCernealaExtensionAssemblyLoaded(),
                "The Cerneala product assembly was not loaded while the solution had no open .crn document.");
            Check(
                "startup.no-server-process",
                CurrentOwnedServerPids().Count == 0,
                "No Cerneala language server process existed before a .crn document was opened.");
            Check(
                "startup.package-remains-lazy",
                !IsCernealaPackageLoaded(),
                "The Cerneala AsyncPackage remained unloaded at solution startup.");

            IWpfTextView fixtureView = await MeasureColdWorkspaceAsync(
                "fixture",
                request.FixtureViewPath,
                enforceColdBudgets: true);
            await MeasureWarmEditorLatencyAsync("fixture", fixtureView);
            fixtureView = await RunSoakAsync(fixtureView);
            fixtureView = await RunRestartScenarioAsync(fixtureView);
            fixtureView = await RunBuildErrorScenarioAsync(fixtureView);
            fixtureView = await MeasureSolutionReloadAsync(
                "fixture",
                request.FixtureSolutionPath,
                request.FixtureViewPath);

            await CloseSolutionAsync();
            Stopwatch fullOpen = Stopwatch.StartNew();
            dte.Solution.Open(request.FullSolutionPath);
            await WaitForSolutionAsync(request.FullSolutionPath, TimeSpan.FromMinutes(3));
            fullOpen.Stop();
            AddMetric("solution-open", "Cerneala.slnx", fullOpen.Elapsed.TotalMilliseconds, null, "wall-ms");

            IWpfTextView fullView = await MeasureColdWorkspaceAsync(
                "Cerneala.slnx",
                request.FullViewPath,
                enforceColdBudgets: true);
            await MeasureWarmEditorLatencyAsync("Cerneala.slnx", fullView);
            await MeasureSolutionReloadAsync(
                "Cerneala.slnx",
                request.FullSolutionPath,
                request.FullViewPath);

            await CloseSolutionAsync();
            Check(
                "shutdown.no-owned-server-process",
                CurrentOwnedServerPids().Count == 0,
                "All language server processes created by the hidden Community instance exited after solution close.");
            report.Passed = report.Checks.All(check => check.Passed);
        }
        catch (Exception exception)
        {
            report.Failure = exception.ToString();
            report.Passed = false;
        }
        finally
        {
            report.ServerPids.AddRange(observedServerPids.OrderBy(pid => pid));
            report.FinishedUtc = DateTimeOffset.UtcNow;
            report.Write(request.ReportPath);
            dte.ExecuteCommand("File.Exit");
        }
    }

    private async Task<IWpfTextView> MeasureColdWorkspaceAsync(
        string workspace,
        string viewPath,
        bool enforceColdBudgets)
    {
        long activationSequenceBefore = GetProviderActivationSequence();
        long serverReadySequenceBefore = GetProviderServerReadySequence();
        Stopwatch cold = Stopwatch.StartNew();
        IWpfTextView view = OpenView(viewPath);

        await WaitAsync(
            IsCernealaExtensionAssemblyLoaded,
            TimeSpan.FromSeconds(10),
            $"The Cerneala extension assembly did not load for {workspace}.");
        double extensionLoadMs = cold.Elapsed.TotalMilliseconds;
        await WaitAsync(
            () => CurrentOwnedServerPids().Count == 1,
            TimeSpan.FromSeconds(15),
            $"The bundled server did not start for {workspace}.");
        await WaitAsync(
            () => GetProviderActivationSequence() > activationSequenceBefore,
            TimeSpan.FromSeconds(5),
            $"The provider activation instrumentation did not complete for {workspace}.");
        double activationCpuMs = GetProviderActivationCpuMilliseconds();
        Check(
            "instrumentation." + NormalizeName(workspace) + ".provider-activation-cpu",
            activationCpuMs >= 0,
            $"Provider activation CPU instrumentation returned {activationCpuMs:F4} ms.");
        await WaitAsync(
            () => GetProviderServerReadySequence() > serverReadySequenceBefore,
            TimeSpan.FromSeconds(15),
            $"The language client did not finish protocol initialization for {workspace}.");
        double serverReadyMs = GetProviderServerReadyMilliseconds();
        AddMetric("extension-load", workspace, extensionLoadMs, null, "wall-ms");
        AddMetric(
            "provider-activation-cpu",
            workspace,
            activationCpuMs,
            ProviderActivationCpuBudgetMs,
            "cpu-ms");
        AddMetric(
            "server-ready-cold",
            workspace,
            serverReadyMs,
            ServerReadyColdBudgetMs,
            "wall-ms");

        const string completionProbe = "\n<";
        MoveCaretBeforeRootClose(view);
        TypeText(view, completionProbe);
        ICompletionSession completion = await WaitForCompletionSessionAsync(view, "StackPanel", TimeSpan.FromSeconds(15));
        double firstCompletionMs = cold.Elapsed.TotalMilliseconds;
        completion.Dismiss();
        AddMetric(
            "first-completion-cold",
            workspace,
            firstCompletionMs,
            FirstCompletionColdBudgetMs,
            "wall-ms");
        DeleteLastTextWithCommands(view, "<");
        await WaitForNoErrorTagsAsync(view, TimeSpan.FromSeconds(10));

        const string diagnosticsProbe = "\n<TextBlok />";
        TimeSpan diagnosticsTimeout = TimeSpan.FromMinutes(3);
        MoveCaretBeforeRootClose(view);
        TypeText(view, diagnosticsProbe);
        await WaitAsync(
            () => GetErrorTags(view).Count > 0,
            diagnosticsTimeout,
            $"First diagnostics did not arrive for {workspace}.");
        double firstDiagnosticsMs = cold.Elapsed.TotalMilliseconds;
        AddMetric(
            "first-diagnostics",
            workspace,
            firstDiagnosticsMs,
            null,
            "wall-ms");
        DeleteLastTextWithCommands(view, "<TextBlok />");
        await WaitForNoErrorTagsAsync(view, TimeSpan.FromSeconds(10));

        if (enforceColdBudgets)
        {
            CheckBudget("provider-activation-cpu", workspace, activationCpuMs, ProviderActivationCpuBudgetMs);
            CheckBudget("server-ready-cold", workspace, serverReadyMs, ServerReadyColdBudgetMs);
            CheckBudget("first-completion-cold", workspace, firstCompletionMs, FirstCompletionColdBudgetMs);
        }

        return view;
    }

    private async Task MeasureWarmEditorLatencyAsync(string workspace, IWpfTextView view)
    {
        List<double> completionSamples = new();
        const string completionProbe = "\n<StackP";
        for (int index = -10; index < 20; index++)
        {
            MoveCaretBeforeRootClose(view);
            TypeText(view, completionProbe);
            Stopwatch sample = Stopwatch.StartNew();
            ICompletionSession completion = await WaitForCompletionSessionAsync(
                view,
                "StackPanel",
                TimeSpan.FromSeconds(5));
            sample.Stop();
            if (index >= 0)
            {
                completionSamples.Add(sample.Elapsed.TotalMilliseconds);
            }
            completion.Dismiss();
            DeleteLastTextWithCommands(view, "<StackP");
            await WaitForNoErrorTagsAsync(view, TimeSpan.FromSeconds(5));
        }

        double completionP95 = Percentile95(completionSamples);
        report.SampleGroups.Add(new Stage5SampleGroup
        {
            Name = "editor-warm-completion",
            Workspace = workspace,
            Values = completionSamples.ToArray()
        });
        AddMetric("editor-warm-completion-min", workspace, completionSamples.Min(), null, "wall-ms");
        AddMetric("editor-warm-completion-p50", workspace, Percentile(completionSamples, 0.50), null, "wall-ms");
        AddMetric(
            "editor-warm-completion-p95",
            workspace,
            completionP95,
            null,
            "wall-ms");
        AddMetric("editor-warm-completion-max", workspace, completionSamples.Max(), null, "wall-ms");

        List<double> diagnosticsSamples = new();
        const string diagnosticsProbe = "\n<TextBlok />";
        for (int index = -10; index < 20; index++)
        {
            MoveCaretBeforeRootClose(view);
            Stopwatch sample = Stopwatch.StartNew();
            TypeText(view, diagnosticsProbe);
            await WaitAsync(
                () => GetErrorTags(view).Count > 0,
                TimeSpan.FromSeconds(5),
                $"Warm diagnostics sample {index + 1} timed out for {workspace}.");
            sample.Stop();
            if (index >= 0)
            {
                diagnosticsSamples.Add(sample.Elapsed.TotalMilliseconds);
            }
            DeleteLastTextWithCommands(view, "<TextBlok />");
            await WaitForNoErrorTagsAsync(view, TimeSpan.FromSeconds(5));
        }

        double diagnosticsP95 = Percentile95(diagnosticsSamples);
        report.SampleGroups.Add(new Stage5SampleGroup
        {
            Name = "editor-warm-diagnostics",
            Workspace = workspace,
            Values = diagnosticsSamples.ToArray()
        });
        AddMetric("editor-warm-diagnostics-min", workspace, diagnosticsSamples.Min(), null, "wall-ms");
        AddMetric("editor-warm-diagnostics-p50", workspace, Percentile(diagnosticsSamples, 0.50), null, "wall-ms");
        AddMetric(
            "editor-warm-diagnostics-p95",
            workspace,
            diagnosticsP95,
            null,
            "wall-ms");
        AddMetric("editor-warm-diagnostics-max", workspace, diagnosticsSamples.Max(), null, "wall-ms");
    }

    private async Task<IWpfTextView> RunSoakAsync(IWpfTextView view)
    {
        MemorySnapshot openBaseline = CaptureMemory("open-close-0");
        MemorySnapshot? openMidpoint = null;
        for (int cycle = 1; cycle <= 100; cycle++)
        {
            CloseDocument(view, request.FixtureViewPath);
            view = OpenView(request.FixtureViewPath);
            if (cycle == 50)
            {
                await DelayOnUiThreadAsync(TimeSpan.FromMilliseconds(100));
                openMidpoint = CaptureMemory("open-close-50");
            }
        }

        await WaitForNoErrorTagsAsync(view, TimeSpan.FromSeconds(10));
        MemorySnapshot openFinal = CaptureMemory("open-close-100");
        MemorySnapshot midpoint = openMidpoint
            ?? throw new InvalidOperationException("The open/close midpoint sample was not captured.");
        CheckPlateau("soak.open-close.devenv-plateau", midpoint.DevenvPrivateBytes, openFinal.DevenvPrivateBytes, DevenvPlateauBudgetBytes);
        CheckPlateau("soak.open-close.server-plateau", midpoint.ServerPrivateBytes, openFinal.ServerPrivateBytes, ServerPlateauBudgetBytes);
        Check(
            "soak.open-close-cycles",
            CurrentOwnedServerPids().Count == 1,
            $"100 open/close cycles completed with one owned server; baseline private bytes={openBaseline.DevenvPrivateBytes}.");

        MemorySnapshot editBaseline = CaptureMemory("edits-0");
        MemorySnapshot? editMidpoint = null;
        MoveCaretBeforeRootClose(view);
        for (int edit = 1; edit <= 500; edit++)
        {
            TypeText(view, " ");
            ExecuteOle(view, VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.BACKSPACE);
            if (edit == 250)
            {
                await DelayOnUiThreadAsync(TimeSpan.FromMilliseconds(100));
                editMidpoint = CaptureMemory("edits-500");
            }
        }

        await WaitForNoErrorTagsAsync(view, TimeSpan.FromSeconds(10));
        MemorySnapshot editFinal = CaptureMemory("edits-1000");
        midpoint = editMidpoint
            ?? throw new InvalidOperationException("The edit midpoint sample was not captured.");
        CheckPlateau("soak.edits.devenv-plateau", midpoint.DevenvPrivateBytes, editFinal.DevenvPrivateBytes, DevenvPlateauBudgetBytes);
        CheckPlateau("soak.edits.server-plateau", midpoint.ServerPrivateBytes, editFinal.ServerPrivateBytes, ServerPlateauBudgetBytes);
        Check(
            "soak.one-thousand-edits",
            CurrentOwnedServerPids().Count == 1,
            $"1,000 editor changes completed with one owned server; baseline private bytes={editBaseline.DevenvPrivateBytes}.");

        await AssertCompletionResponsiveAsync(view, "soak completion");
        return view;
    }

    private async Task<IWpfTextView> RunRestartScenarioAsync(IWpfTextView view)
    {
        int oldPid = CurrentOwnedServerPids().Single();
        Stopwatch restart = Stopwatch.StartNew();
        Assembly extensionAssembly = AppDomain.CurrentDomain.GetAssemblies().Single(assembly =>
            string.Equals(assembly.GetName().Name, "Cerneala.VisualStudio", StringComparison.Ordinal));
        Type providerType = extensionAssembly.GetType("Cerneala.VisualStudio.CernealaLanguageServerProvider")
            ?? throw new InvalidOperationException("The Cerneala language server provider type is unavailable.");
        Type languageClientType = providerType.GetInterfaces().Single(type =>
            string.Equals(
                type.FullName,
                "Microsoft.VisualStudio.LanguageServer.Client.ILanguageClient",
                StringComparison.Ordinal));
        MethodInfo getExtensions = typeof(IComponentModel).GetMethods()
            .Single(method => method.Name == "GetExtensions" &&
                method.IsGenericMethodDefinition &&
                method.GetParameters().Length == 0);
        System.Collections.IEnumerable languageClients =
            (System.Collections.IEnumerable)(getExtensions
                .MakeGenericMethod(languageClientType)
                .Invoke(componentModel, null)
                ?? throw new InvalidOperationException("The Visual Studio language client exports are unavailable."));
        object provider = languageClients.Cast<object>().Single(providerType.IsInstanceOfType);
        MethodInfo restartMethod = providerType.GetMethod(
            "RestartAsync",
            BindingFlags.Instance | BindingFlags.Public)
            ?? throw new MissingMethodException(providerType.FullName, "RestartAsync");
        Task restartTask = (Task)(restartMethod.Invoke(provider, new object[] { cancellationToken })
            ?? throw new InvalidOperationException("The provider restart API returned no task."));
        await restartTask.ConfigureAwait(false);
        await package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        Check(
            "resilience.restart-provider-api",
            true,
            $"The provider API behind {RestartLanguageServerCommandName} accepted the restart request.");
        await WaitAsync(
            () =>
            {
                IReadOnlyList<int> pids = CurrentOwnedServerPids();
                return pids.Count == 1 && pids[0] != oldPid;
            },
            TimeSpan.FromSeconds(20),
            $"The {RestartLanguageServerCommandName} command did not replace the language server process.");
        restart.Stop();
        AddMetric("server-restart", "fixture", restart.Elapsed.TotalMilliseconds, null, "wall-ms");
        Check(
            "resilience.restart-server",
            !IsProcessAlive(oldPid),
            $"The restart command replaced PID {oldPid} without blocking the hidden editor.");
        await AssertCompletionResponsiveAsync(view, "completion after restart");
        return view;
    }

    private async Task<IWpfTextView> RunBuildErrorScenarioAsync(IWpfTextView fixtureView)
    {
        const string buildError = "\nthis is not valid C sharp;\n";
        IWpfTextView codeView = OpenView(request.FixtureCodePath);
        MoveCaretToEnd(codeView);
        TypeText(codeView, buildError);
        Save(codeView);

        dte.Solution.SolutionBuild.Build(false);
        await WaitAsync(
            () => dte.Solution.SolutionBuild.BuildState == EnvDTE.vsBuildState.vsBuildStateDone,
            TimeSpan.FromMinutes(2),
            "The expected failing build did not finish.");
        Check(
            "resilience.project-build-errors",
            dte.Solution.SolutionBuild.LastBuildInfo > 0,
            "The fixture had an intentional C# build error and Visual Studio remained responsive.");

        fixtureView = OpenView(request.FixtureViewPath);
        await AssertCompletionResponsiveAsync(fixtureView, "completion with C# build errors");

        codeView = OpenView(request.FixtureCodePath);
        DeleteLastTextWithCommands(codeView, "this is not valid C sharp;");
        Save(codeView);
        dte.Solution.SolutionBuild.Build(false);
        await WaitAsync(
            () => dte.Solution.SolutionBuild.BuildState == EnvDTE.vsBuildState.vsBuildStateDone,
            TimeSpan.FromMinutes(2),
            "The repaired fixture build did not finish.");
        Check(
            "resilience.build-repair",
            dte.Solution.SolutionBuild.LastBuildInfo == 0,
            "Removing the intentional error restored a green build without restarting Visual Studio.");
        return fixtureView;
    }

    private async Task<IWpfTextView> MeasureSolutionReloadAsync(
        string workspace,
        string solutionPath,
        string viewPath)
    {
        await CloseSolutionAsync();
        Stopwatch reload = Stopwatch.StartNew();
        dte.Solution.Open(solutionPath);
        await WaitForSolutionAsync(solutionPath, TimeSpan.FromMinutes(3));
        IWpfTextView view = OpenView(viewPath);
        await AssertCompletionResponsiveAsync(view, $"completion after {workspace} reload");
        reload.Stop();
        AddMetric("solution-reload", workspace, reload.Elapsed.TotalMilliseconds, null, "wall-ms");
        Check(
            "resilience.solution-reload." + NormalizeName(workspace),
            CurrentOwnedServerPids().Count == 1,
            $"{workspace} closed, reopened and returned completion with one server process.");
        return view;
    }

    private async Task AssertCompletionResponsiveAsync(IWpfTextView view, string scenario)
    {
        const string probe = "\n<StackP";
        MoveCaretBeforeRootClose(view);
        TypeText(view, probe);
        ICompletionSession completion = await WaitForCompletionSessionAsync(
            view,
            "StackPanel",
            TimeSpan.FromSeconds(10));
        completion.Dismiss();
        DeleteLastTextWithCommands(view, "<StackP");
        await WaitForNoErrorTagsAsync(view, TimeSpan.FromSeconds(10));
        Check("editor-responsive." + NormalizeName(scenario), true, scenario + " succeeded through the Visual Studio completion API.");
    }

    private async Task CloseSolutionAsync()
    {
        await package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        foreach (EnvDTE.Document document in dte.Documents.Cast<EnvDTE.Document>().ToArray())
        {
            document.Close(EnvDTE.vsSaveChanges.vsSaveChangesNo);
        }

        commandTargets.Clear();
        foreach (ITagAggregator<IErrorTag> aggregator in errorTagAggregators.Values)
        {
            aggregator.Dispose();
        }

        errorTagAggregators.Clear();
        if (dte.Solution.IsOpen)
        {
            dte.Solution.Close(false);
        }

        await WaitAsync(
            () => !dte.Solution.IsOpen,
            TimeSpan.FromSeconds(30),
            "Visual Studio did not close the active solution.");
        await WaitAsync(
            () => CurrentOwnedServerPids().Count == 0,
            TimeSpan.FromSeconds(15),
            "The Cerneala language server remained alive after solution close.");
    }

    private async Task WaitForSolutionAsync(string path, TimeSpan timeout) =>
        await WaitAsync(
            () =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (!string.Equals(dte.Solution.FullName, path, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                int result = solution.GetProperty(
                    (int)VsSolutionPropID.IsSolutionFullyLoaded,
                    out object fullyLoaded);
                return ErrorHandler.Succeeded(result) && Convert.ToBoolean(fullyLoaded);
            },
            timeout,
            $"Visual Studio did not load {path}.");

    private IWpfTextView OpenView(string path)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        Guid logicalView = VSConstants.LOGVIEWID_Code;
        VsShellUtilities.OpenDocument(
            package,
            path,
            logicalView,
            out IVsUIHierarchy hierarchy,
            out uint itemId,
            out IVsWindowFrame frame,
            out IVsTextView textView);
        ErrorHandler.ThrowOnFailure(frame.Show());
        IWpfTextView view = editorAdapters.GetWpfTextView(textView)
            ?? throw new InvalidOperationException($"No WPF text view was created for {path}.");
        commandTargets[view] = (IOleCommandTarget)textView;
        if (!errorTagAggregators.ContainsKey(view))
        {
            errorTagAggregators[view] = tagAggregators.CreateTagAggregator<IErrorTag>(view);
        }

        return view;
    }

    private void CloseDocument(IWpfTextView view, string path)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        commandTargets.Remove(view);
        if (errorTagAggregators.TryGetValue(view, out ITagAggregator<IErrorTag>? aggregator))
        {
            errorTagAggregators.Remove(view);
            aggregator.Dispose();
        }

        EnvDTE.Document? document = dte.Documents
            .Cast<EnvDTE.Document>()
            .FirstOrDefault(candidate => string.Equals(candidate.FullName, path, StringComparison.OrdinalIgnoreCase));
        document?.Close(EnvDTE.vsSaveChanges.vsSaveChangesNo);
    }

    private void Save(IWpfTextView view)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        Execute(
            view,
            (textView, buffer) => new SaveCommandArgs(textView, buffer),
            () => dte.ExecuteCommand("File.SaveAll"));
    }

    private void MoveCaretBeforeRootClose(IWpfTextView view)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        int position = view.TextSnapshot.GetText().LastIndexOf("</", StringComparison.Ordinal);
        if (position < 0)
        {
            throw new InvalidOperationException("The performance document has no closing root element.");
        }

        view.Selection.Clear();
        view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, position));
    }

    private void MoveCaretToEnd(IWpfTextView view)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        view.Selection.Clear();
        view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, view.TextSnapshot.Length));
    }

    private void TypeText(IWpfTextView view, string text)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        foreach (char character in text)
        {
            if (character == '\n')
            {
                ExecuteOle(view, VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.RETURN);
            }
            else if (character != '\r')
            {
                ExecuteOle(view, VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.TYPECHAR, character);
            }
        }
    }

    private void DeleteLastTextWithCommands(IWpfTextView view, string text)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        int position = view.TextSnapshot.GetText().LastIndexOf(text, StringComparison.Ordinal);
        if (position < 0)
        {
            throw new InvalidOperationException($"Could not find the editor probe '{text}'.");
        }

        view.Selection.Clear();
        view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, position));
        for (int index = 0; index < text.Length; index++)
        {
            ExecuteOle(view, VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.RIGHT_EXT);
        }

        ExecuteOle(view, VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.BACKSPACE);
    }

    private void Execute<TArgs>(
        IWpfTextView view,
        Func<ITextView, ITextBuffer, TArgs> argsFactory,
        Action? coreFallback = null)
        where TArgs : EditorCommandArgs
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        bool handled = true;
        commandHandlers.GetService(view).Execute(argsFactory, () => handled = false);
        if (!handled)
        {
            if (coreFallback is null)
            {
                throw new InvalidOperationException($"Visual Studio did not handle {typeof(TArgs).Name}.");
            }

            coreFallback();
        }
    }

    private void ExecuteOle(IWpfTextView view, Guid commandGroup, VSConstants.VSStd2KCmdID command, object? input = null) =>
        ExecuteOle(view, commandGroup, (uint)command, input);

    private void ExecuteOle(IWpfTextView view, Guid commandGroup, uint command, object? input)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (!commandTargets.TryGetValue(view, out IOleCommandTarget? target))
        {
            throw new InvalidOperationException("The editor command target is unavailable.");
        }

        IntPtr inputPointer = IntPtr.Zero;
        try
        {
            if (input is not null)
            {
                inputPointer = Marshal.AllocCoTaskMem(16);
                Marshal.GetNativeVariantForObject(input, inputPointer);
            }

            ErrorHandler.ThrowOnFailure(target.Exec(
                ref commandGroup,
                command,
                (uint)OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT,
                inputPointer,
                IntPtr.Zero));
        }
        finally
        {
            if (inputPointer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(inputPointer);
            }
        }
    }

    private async Task<ICompletionSession> WaitForCompletionSessionAsync(
        IWpfTextView view,
        string expected,
        TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        int lastCount = 0;
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ICompletionSession? session = completionBroker.GetSessions(view)
                .LastOrDefault(candidate => !candidate.IsDismissed)
                ?? completionBroker.TriggerCompletion(view);
            if (session is not null && !session.IsDismissed)
            {
                Completion[] completions = session.CompletionSets
                    .SelectMany(set => set.Completions)
                    .ToArray();
                lastCount = completions.Length;
                if (completions.Any(item => string.Equals(item.DisplayText, expected, StringComparison.Ordinal)))
                {
                    return session;
                }

                session.Dismiss();
            }

            await DelayOnUiThreadAsync(TimeSpan.FromMilliseconds(10));
        }

        throw new TimeoutException($"Completion did not expose {expected}; last item count was {lastCount}.");
    }

    private IReadOnlyList<string> GetErrorTags(IWpfTextView view)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (!errorTagAggregators.TryGetValue(view, out ITagAggregator<IErrorTag>? aggregator))
        {
            aggregator = tagAggregators.CreateTagAggregator<IErrorTag>(view);
            errorTagAggregators[view] = aggregator;
        }

        SnapshotSpan span = new(view.TextSnapshot, 0, view.TextSnapshot.Length);
        return aggregator.GetTags(span)
            .Where(tag => tag.Tag.ErrorType.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0)
            .SelectMany(tag => tag.Span.GetSpans(view.TextSnapshot).Select(errorSpan =>
                $"{tag.Tag.ErrorType}@{errorSpan.Start.Position}: {DescribeAdornment(tag.Tag.ToolTipContent)}"))
            .ToArray();
    }

    private static string DescribeAdornment(object? content) => content switch
    {
        null => "<no tooltip>",
        string text => text,
        ClassifiedTextElement classified => string.Concat(classified.Runs.Select(run => run.Text)),
        ContainerElement container => string.Join(" ", container.Elements.Select(DescribeAdornment)),
        _ => content.ToString() ?? content.GetType().FullName
    };

    private async Task WaitForNoErrorTagsAsync(IWpfTextView view, TimeSpan timeout) =>
        await WaitAsync(
            () => GetErrorTags(view).Count == 0,
            timeout,
            "Editor diagnostics did not return to zero.");

    private async Task WaitAsync(Func<bool> condition, TimeSpan timeout, string failure)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (condition())
            {
                return;
            }

            await DelayOnUiThreadAsync(TimeSpan.FromMilliseconds(10));
        }

        throw new TimeoutException(failure);
    }

    private async Task DelayOnUiThreadAsync(TimeSpan delay)
    {
        await Task.Delay(delay, cancellationToken);
        await package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
    }

    private bool IsCernealaPackageLoaded()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        Guid packageGuid = CernealaPackageGuid;
        int result = shell.IsPackageLoaded(ref packageGuid, out IVsPackage loadedPackage);
        return ErrorHandler.Succeeded(result) && loadedPackage is not null;
    }

    private static bool IsCernealaExtensionAssemblyLoaded() =>
        AppDomain.CurrentDomain.GetAssemblies().Any(assembly =>
            string.Equals(assembly.GetName().Name, "Cerneala.VisualStudio", StringComparison.Ordinal));

    private static long GetProviderActivationSequence() =>
        Convert.ToInt64(GetProviderInstrumentationProperty("ActivationSequence") ?? 0L);

    private static double GetProviderActivationCpuMilliseconds() =>
        Convert.ToDouble(GetProviderInstrumentationProperty("LastActivationCpuMilliseconds") ?? -1d);

    private static long GetProviderServerReadySequence() =>
        Convert.ToInt64(GetProviderInstrumentationProperty("ServerReadySequence") ?? 0L);

    private static double GetProviderServerReadyMilliseconds() =>
        Convert.ToDouble(GetProviderInstrumentationProperty("LastServerReadyMilliseconds") ?? -1d);

    private static object? GetProviderInstrumentationProperty(string propertyName)
    {
        Assembly? assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(candidate =>
            string.Equals(candidate.GetName().Name, "Cerneala.VisualStudio", StringComparison.Ordinal));
        Type? provider = assembly?.GetType("Cerneala.VisualStudio.CernealaLanguageServerProvider");
        return provider?
            .GetProperty(propertyName, BindingFlags.Static | BindingFlags.NonPublic)?
            .GetValue(null);
    }

    private IReadOnlyList<int> CurrentOwnedServerPids()
    {
        int[] current = GetServerPids()
            .Where(pid => !baselineServerPids.Contains(pid))
            .OrderBy(pid => pid)
            .ToArray();
        foreach (int pid in current)
        {
            observedServerPids.Add(pid);
        }

        return current;
    }

    private static IEnumerable<int> GetServerPids()
    {
        foreach (Process process in Process.GetProcessesByName("Cerneala.LanguageServer"))
        {
            using (process)
            {
                if (!process.HasExited)
                {
                    yield return process.Id;
                }
            }
        }
    }

    private static bool IsProcessAlive(int pid)
    {
        try
        {
            using Process process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private MemorySnapshot CaptureMemory(string name)
    {
        Process devenv = Process.GetCurrentProcess();
        devenv.Refresh();
        IReadOnlyList<int> serverPids = CurrentOwnedServerPids();
        long serverPrivateBytes = 0;
        if (serverPids.Count == 1)
        {
            using Process server = Process.GetProcessById(serverPids[0]);
            server.Refresh();
            serverPrivateBytes = server.PrivateMemorySize64;
        }

        MemorySnapshot sample = new()
        {
            Name = name,
            DevenvPrivateBytes = devenv.PrivateMemorySize64,
            ServerPrivateBytes = serverPrivateBytes,
            ServerProcessCount = serverPids.Count
        };
        report.MemorySamples.Add(sample);
        return sample;
    }

    private void CheckPlateau(string name, long midpoint, long final, long budget)
    {
        long growth = Math.Max(0, final - midpoint);
        Check(
            name,
            growth <= budget,
            $"Second-half private-byte growth={growth}; budget={budget}; midpoint={midpoint}; final={final}.");
    }

    private void CheckBudget(string metric, string workspace, double value, double budget) =>
        Check(
            "budget." + NormalizeName(workspace) + "." + metric,
            value <= budget,
            $"{metric}={value:F3} ms; budget={budget:F3} ms.");

    private void AddMetric(
        string name,
        string workspace,
        double value,
        double? budget,
        string unit) =>
        report.Metrics.Add(new Stage5Metric
        {
            Name = name,
            Workspace = workspace,
            Value = value,
            Budget = budget,
            Unit = unit,
            Passed = !budget.HasValue || value <= budget.Value
        });

    private void Check(string name, bool passed, string details)
    {
        report.Checks.Add(new Stage5Check { Name = name, Passed = passed, Details = details });
        if (!passed)
        {
            throw new InvalidOperationException($"Stage 5 check failed: {name}. {details}");
        }
    }

    private static double Percentile95(IReadOnlyList<double> values) => Percentile(values, 0.95);

    private static double Percentile(IReadOnlyList<double> values, double percentile)
    {
        double[] ordered = values.OrderBy(value => value).ToArray();
        int index = Math.Max(0, (int)Math.Ceiling(ordered.Length * percentile) - 1);
        return ordered[index];
    }

    private static string NormalizeName(string value) =>
        new(value.Select(character => char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-').ToArray());
}

[DataContract]
internal sealed class Stage5Request
{
    [DataMember(Name = "fixtureSolutionPath")]
    public string FixtureSolutionPath { get; set; } = string.Empty;

    [DataMember(Name = "fixtureViewPath")]
    public string FixtureViewPath { get; set; } = string.Empty;

    [DataMember(Name = "fixtureCodePath")]
    public string FixtureCodePath { get; set; } = string.Empty;

    [DataMember(Name = "fullSolutionPath")]
    public string FullSolutionPath { get; set; } = string.Empty;

    [DataMember(Name = "fullViewPath")]
    public string FullViewPath { get; set; } = string.Empty;

    [DataMember(Name = "reportPath")]
    public string ReportPath { get; set; } = string.Empty;

    [DataMember(Name = "processStartedUtc")]
    public string ProcessStartedUtc { get; set; } = string.Empty;

    [DataMember(Name = "processorName")]
    public string ProcessorName { get; set; } = string.Empty;

    [DataMember(Name = "memoryBytes")]
    public long MemoryBytes { get; set; }

    [DataMember(Name = "operatingSystem")]
    public string OperatingSystem { get; set; } = string.Empty;

    [DataMember(Name = "visualStudioInstallation")]
    public string VisualStudioInstallation { get; set; } = string.Empty;

    public static Stage5Request Read(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return (Stage5Request)(new DataContractJsonSerializer(typeof(Stage5Request)).ReadObject(stream)
            ?? throw new InvalidDataException("The Stage 5 request is empty."));
    }
}

[DataContract]
internal sealed class Stage5Report
{
    [DataMember(Name = "startedUtc")]
    public DateTimeOffset StartedUtc { get; set; }

    [DataMember(Name = "finishedUtc")]
    public DateTimeOffset FinishedUtc { get; set; }

    [DataMember(Name = "hostEdition")]
    public string HostEdition { get; set; } = string.Empty;

    [DataMember(Name = "hostVersion")]
    public string HostVersion { get; set; } = string.Empty;

    [DataMember(Name = "processorName")]
    public string ProcessorName { get; set; } = string.Empty;

    [DataMember(Name = "processorCount")]
    public int ProcessorCount { get; set; }

    [DataMember(Name = "memoryBytes")]
    public long MemoryBytes { get; set; }

    [DataMember(Name = "operatingSystem")]
    public string OperatingSystem { get; set; } = string.Empty;

    [DataMember(Name = "visualStudioInstallation")]
    public string VisualStudioInstallation { get; set; } = string.Empty;

    [DataMember(Name = "passed")]
    public bool Passed { get; set; }

    [DataMember(Name = "failure", EmitDefaultValue = false)]
    public string? Failure { get; set; }

    [DataMember(Name = "checks")]
    public List<Stage5Check> Checks { get; } = new();

    [DataMember(Name = "metrics")]
    public List<Stage5Metric> Metrics { get; } = new();

    [DataMember(Name = "memorySamples")]
    public List<MemorySnapshot> MemorySamples { get; } = new();

    [DataMember(Name = "sampleGroups")]
    public List<Stage5SampleGroup> SampleGroups { get; } = new();

    [DataMember(Name = "serverPids")]
    public List<int> ServerPids { get; } = new();

    public void Write(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using FileStream stream = File.Create(path);
        new DataContractJsonSerializer(typeof(Stage5Report), new DataContractJsonSerializerSettings
        {
            UseSimpleDictionaryFormat = true
        }).WriteObject(stream, this);
    }
}

[DataContract]
internal sealed class Stage5Check
{
    [DataMember(Name = "name")]
    public string Name { get; set; } = string.Empty;

    [DataMember(Name = "passed")]
    public bool Passed { get; set; }

    [DataMember(Name = "details")]
    public string Details { get; set; } = string.Empty;
}

[DataContract]
internal sealed class Stage5Metric
{
    [DataMember(Name = "name")]
    public string Name { get; set; } = string.Empty;

    [DataMember(Name = "workspace")]
    public string Workspace { get; set; } = string.Empty;

    [DataMember(Name = "value")]
    public double Value { get; set; }

    [DataMember(Name = "budget", EmitDefaultValue = false)]
    public double? Budget { get; set; }

    [DataMember(Name = "unit")]
    public string Unit { get; set; } = string.Empty;

    [DataMember(Name = "passed")]
    public bool Passed { get; set; }
}

[DataContract]
internal sealed class Stage5SampleGroup
{
    [DataMember(Name = "name")]
    public string Name { get; set; } = string.Empty;

    [DataMember(Name = "workspace")]
    public string Workspace { get; set; } = string.Empty;

    [DataMember(Name = "values")]
    public double[] Values { get; set; } = Array.Empty<double>();
}

[DataContract]
internal sealed class MemorySnapshot
{
    [DataMember(Name = "name")]
    public string Name { get; set; } = string.Empty;

    [DataMember(Name = "devenvPrivateBytes")]
    public long DevenvPrivateBytes { get; set; }

    [DataMember(Name = "serverPrivateBytes")]
    public long ServerPrivateBytes { get; set; }

    [DataMember(Name = "serverProcessCount")]
    public int ServerProcessCount { get; set; }
}

#pragma warning restore VSTHRD010
