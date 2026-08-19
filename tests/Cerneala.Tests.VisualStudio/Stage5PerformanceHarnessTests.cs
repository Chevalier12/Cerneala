namespace Cerneala.Tests.VisualStudio;

public sealed class Stage5PerformanceHarnessTests
{
    [Fact]
    public void HarnessMeasuresBudgetsReloadAndRequiredSoak()
    {
        string runner = File.ReadAllText(Path.Combine(HostDirectory(), "VisualStudioStage5Runner.cs"));
        string script = File.ReadAllText(Path.Combine(FixtureDirectory(), "Invoke-CommunityPerformance.ps1"));
        string hardening = File.ReadAllText(Path.Combine(
            VisualStudioPackageTests.RepositoryRoot(),
            "tests",
            "Cerneala.Tests.LanguageServer",
            "HardeningProtocolTests.cs"));
        string automation = runner + script;

        foreach (string required in new[]
        {
            "provider-activation-cpu",
            "server-ready-cold",
            "first-diagnostics",
            "first-completion-cold",
            "editor-warm-completion-p95",
            "editor-warm-diagnostics-p95",
            "solution-reload",
            "IsSolutionFullyLoaded",
            "Cerneala.slnx",
            "cycle <= 100",
            "edit <= 500",
            "1,000 editor changes",
            "Tools.CernealaRestartLanguageServer",
            "SolutionBuild.Build(false)",
            "MemorySamples"
        })
        {
            Assert.Contains(required, automation, StringComparison.Ordinal);
        }

        Assert.Contains("ProviderActivationCpuBudgetMs = 100", runner, StringComparison.Ordinal);
        Assert.Contains("ActivationSequence", runner, StringComparison.Ordinal);
        Assert.Contains("LastActivationCpuMilliseconds", runner, StringComparison.Ordinal);
        Assert.Contains("LastServerReadyMilliseconds", runner, StringComparison.Ordinal);
        Assert.Contains("GetThreadTimes", File.ReadAllText(Path.Combine(
            VisualStudioPackageTests.RepositoryRoot(),
            "Cerneala.VisualStudio",
            "CernealaLanguageServerProvider.cs")), StringComparison.Ordinal);
        Assert.Contains("ServerReadyColdBudgetMs = 2_000", runner, StringComparison.Ordinal);
        Assert.Contains("FirstCompletionColdBudgetMs = 2_500", runner, StringComparison.Ordinal);
        Assert.Contains("FullSolutionIncrementalRequestsRespectWarmBudgets", hardening, StringComparison.Ordinal);
        Assert.Contains("AssertBudget(\"full-solution incremental completion\", completionSamples, 100)", hardening, StringComparison.Ordinal);
        Assert.Contains("AssertBudget(\"full-solution incremental diagnostics\", diagnosticSamples, 200)", hardening, StringComparison.Ordinal);
        Assert.Contains("lsp-warm-budgets-full-solution", script, StringComparison.Ordinal);
        Assert.DoesNotContain("WarmCompletionP95BudgetMs", runner, StringComparison.Ordinal);
        Assert.DoesNotContain("WarmDiagnosticsP95BudgetMs", runner, StringComparison.Ordinal);
    }

    [Fact]
    public void StartupAndResilienceScenariosRemainHiddenAndApiOnly()
    {
        string runner = File.ReadAllText(Path.Combine(HostDirectory(), "VisualStudioStage5Runner.cs"));
        string script = File.ReadAllText(Path.Combine(FixtureDirectory(), "Invoke-CommunityPerformance.ps1"));
        string automation = runner + script;

        Assert.True(Count(script, "-WindowStyle Hidden") >= 3);
        Assert.Contains("startup.no-cerneala-assembly", runner, StringComparison.Ordinal);
        Assert.Contains("startup.no-server-process", runner, StringComparison.Ordinal);
        Assert.Contains("startup.package-remains-lazy", runner, StringComparison.Ordinal);
        Assert.Contains("deferWorkspaceLoad", File.ReadAllText(Path.Combine(
            VisualStudioPackageTests.RepositoryRoot(),
            "Cerneala.VisualStudio",
            "CernealaLanguageServerProvider.cs")), StringComparison.Ordinal);
        string workspace = File.ReadAllText(Path.Combine(
            VisualStudioPackageTests.RepositoryRoot(),
            "Cerneala.LanguageServer",
            "Workspace",
            "CernealaWorkspace.cs"));
        Assert.Contains("StartDeferredInitialLoad", workspace, StringComparison.Ordinal);
        Assert.DoesNotContain("DeferredInitialLoadDelay", workspace, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.Delay(DeferredInitialLoadDelay", workspace, StringComparison.Ordinal);
        Assert.Contains("GetWorkspace().StartDeferredInitialLoad(request.TextDocument.Uri)", File.ReadAllText(Path.Combine(
            VisualStudioPackageTests.RepositoryRoot(),
            "Cerneala.LanguageServer",
            "Protocol",
            "LanguageServerEndpoint.cs")), StringComparison.Ordinal);
        Assert.Contains("Invoke-ExtensionStateAction 'disable'", script, StringComparison.Ordinal);
        Assert.Contains("Invoke-ExtensionStateAction 'enable'", script, StringComparison.Ordinal);
        Assert.Contains("CERNEALA_STAGE5_EXTENSION_STATE", script, StringComparison.Ordinal);
        Assert.Contains("CreateExpInstance.exe", script, StringComparison.Ordinal);
        Assert.Contains("/Reset", script, StringComparison.Ordinal);
        Assert.Contains("server-unavailable", script, StringComparison.Ordinal);
        Assert.Contains("CERNEALA_STAGE5_RESILIENCE", script, StringComparison.Ordinal);
        Assert.Contains("Tools.CernealaRunStage4Integration", script, StringComparison.Ordinal);

        foreach (string forbidden in new[]
        {
            "SendKeys",
            "SetForegroundWindow",
            "SwitchToThisWindow",
            "System.Windows.Clipboard",
            "Clipboard.Set",
            "Set-Clipboard",
            "CreateEdit(",
            "TextBuffer.Properties",
            "ITextBuffer.Properties"
        })
        {
            Assert.DoesNotContain(forbidden, automation, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ProviderAndCommandsContainNoSynchronousUiThreadWaits()
    {
        string repository = VisualStudioPackageTests.RepositoryRoot();
        string source = string.Join("\n", new[]
        {
            File.ReadAllText(Path.Combine(repository, "Cerneala.VisualStudio", "CernealaPackage.cs")),
            File.ReadAllText(Path.Combine(repository, "Cerneala.VisualStudio", "CernealaLanguageServerProvider.cs")),
            File.ReadAllText(Path.Combine(repository, "Cerneala.VisualStudio", "RestartLanguageServerCommand.cs"))
        });

        Assert.Contains("AllowsBackgroundLoading = true", source, StringComparison.Ordinal);
        Assert.Contains("RunAsync", source, StringComparison.Ordinal);
        foreach (string forbidden in new[]
        {
            ".Result",
            ".Wait(",
            "Task.Wait",
            "Thread.Sleep",
            "GetAwaiter().GetResult",
            "JoinableTaskFactory.Run("
        })
        {
            Assert.DoesNotContain(forbidden, source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ResultPublisherRecordsHardwareMetricsAndRawEvidence()
    {
        string script = File.ReadAllText(Path.Combine(FixtureDirectory(), "Invoke-CommunityPerformance.ps1"));

        Assert.Contains("Win32_Processor", script, StringComparison.Ordinal);
        Assert.Contains("TotalPhysicalMemory", script, StringComparison.Ordinal);
        Assert.Contains("Write-PerformanceMarkdown", script, StringComparison.Ordinal);
        Assert.Contains("visual-studio-community-extension.json", script, StringComparison.Ordinal);
        Assert.Contains("visual-studio-community-extension.md", script, StringComparison.Ordinal);
        Assert.Contains("Raw measurements", script, StringComparison.Ordinal);
    }

    private static int Count(string text, string value)
    {
        int count = 0;
        int start = 0;
        while ((start = text.IndexOf(value, start, StringComparison.Ordinal)) >= 0)
        {
            count++;
            start += value.Length;
        }

        return count;
    }

    private static string HostDirectory() => Path.Combine(
        VisualStudioPackageTests.RepositoryRoot(),
        "tests",
        "Fixtures",
        "VisualStudioIntegrationHost");

    private static string FixtureDirectory() => Path.Combine(
        VisualStudioPackageTests.RepositoryRoot(),
        "tests",
        "Fixtures",
        "VisualStudioConsumer");
}
