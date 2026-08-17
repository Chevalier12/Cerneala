namespace Cerneala.Tests.VisualStudio;

public sealed class Stage4IntegrationHarnessTests
{
    [Fact]
    public void ExternalConsumerCoversTheRequiredMarkupAndBuildContracts()
    {
        string fixture = FixtureDirectory();
        string project = File.ReadAllText(Path.Combine(fixture, "VisualStudioConsumer.csproj"));
        string markup = File.ReadAllText(Path.Combine(fixture, "MainView.crn"));

        Assert.Contains("Cerneala.csproj", project, StringComparison.Ordinal);
        Assert.Contains("Cerneala.SourceGen.csproj", project, StringComparison.Ordinal);
        Assert.Contains("OutputItemType=\"Analyzer\"", project, StringComparison.Ordinal);
        Assert.Contains("AdditionalFiles Include=\"**\\*.crn\"", project, StringComparison.Ordinal);
        Assert.Contains("STAGE4_PACKAGE_REFERENCE", project, StringComparison.Ordinal);

        foreach (string required in new[]
        {
            "DataType=\"VisualStudioConsumer.DashboardViewModel\"",
            "<UserControl.Resources>",
            "<Aspect ",
            "<MotionClip ",
            "Tween(120ms)",
            "<PrismComposition ",
            "@prism ",
            "<consumer:StatusCard",
            "<ItemsControl>",
            "<ItemsControl.Templates>",
            "<ContentTemplate "
        })
        {
            Assert.Contains(required, markup, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void CommunityAutomationUsesHiddenVisualStudioAndEditorApisOnly()
    {
        string fixture = FixtureDirectory();
        string script = File.ReadAllText(Path.Combine(fixture, "Invoke-CommunityIntegration.ps1"));
        string hostDirectory = Path.Combine(
            VisualStudioPackageTests.RepositoryRoot(),
            "tests",
            "Fixtures",
            "VisualStudioIntegrationHost");
        string runner = File.ReadAllText(Path.Combine(hostDirectory, "VisualStudioStage4Runner.cs"));
        string presentation = File.ReadAllText(Path.Combine(
            hostDirectory,
            "VisualStudioStage4Runner.Presentation.cs"));
        string automation = runner + presentation + script;

        Assert.True(Count(script, "-WindowStyle Hidden") >= 2);
        Assert.Contains("CernealaStage4Rot", script, StringComparison.Ordinal);
        Assert.Contains("IRunningObjectTable table", script, StringComparison.Ordinal);
        Assert.Contains("context.GetRunningObjectTable(out table)", script, StringComparison.Ordinal);
        Assert.DoesNotContain("private static extern int GetRunningObjectTable", script, StringComparison.Ordinal);
        Assert.Contains("$dte.ExecuteCommand('Tools.CernealaRunStage4Integration')", script, StringComparison.Ordinal);
        Assert.Contains("IOleCommandTarget", runner, StringComparison.Ordinal);
        Assert.Contains("IEditorCommandHandlerServiceFactory", runner, StringComparison.Ordinal);
        Assert.Contains("GetEditorOperations", runner, StringComparison.Ordinal);
        Assert.Contains("InsertText(largePayload)", runner, StringComparison.Ordinal);
        Assert.Contains("IVsSolution4", runner, StringComparison.Ordinal);
        Assert.Contains("ReloadProject", runner, StringComparison.Ordinal);
        Assert.Contains("UpdateProjectFileAsync", runner, StringComparison.Ordinal);
        Assert.Contains("File.Replace(temporaryPath, path", runner, StringComparison.Ordinal);
        Assert.Contains("IsSharingViolation", runner, StringComparison.Ordinal);
        Assert.Contains("ConfigureAwait(false)", runner, StringComparison.Ordinal);
        Assert.DoesNotContain("project.Save(path", runner, StringComparison.Ordinal);
        Assert.Contains("PresentationPaths", presentation, StringComparison.Ordinal);
        Assert.Contains("presentation.zero-false-errors", presentation, StringComparison.Ordinal);
        Assert.Contains("presentation.repository-files-unchanged", presentation, StringComparison.Ordinal);
        Assert.DoesNotContain("Save(", presentation, StringComparison.Ordinal);
        Assert.Contains("PresentationOpeningCodePath", presentation, StringComparison.Ordinal);
        Assert.Contains("presentation.csharp-generated-fields", presentation, StringComparison.Ordinal);

        foreach (string forbidden in new[]
        {
            "SendKeys",
            "Get-CimInstance",
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
    public void HarnessExercisesPresentationReloadAndPostEditBuild()
    {
        string script = File.ReadAllText(Path.Combine(
            FixtureDirectory(),
            "Invoke-CommunityIntegration.ps1"));

        Assert.Contains("CernealaPresentation.slnx", script, StringComparison.Ordinal);
        Assert.Contains("presentationPaths = $presentationPaths", script, StringComparison.Ordinal);
        Assert.Contains("presentationBrandMarkPath", script, StringComparison.Ordinal);
        Assert.Contains("presentationOpeningCodePath", script, StringComparison.Ordinal);
        Assert.Contains("presentationWindowPath", script, StringComparison.Ordinal);
        Assert.Contains("presentationMotionPath", script, StringComparison.Ordinal);
        Assert.True(Count(script, "dotnet build $solutionPath") >= 2);
        Assert.Contains("The edited external consumer fixture did not build", script, StringComparison.Ordinal);
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

    private static string FixtureDirectory() => Path.Combine(
        VisualStudioPackageTests.RepositoryRoot(),
        "tests",
        "Fixtures",
        "VisualStudioConsumer");
}
