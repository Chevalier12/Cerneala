using System.Xml.Linq;
using System.Collections.Immutable;
using Cerneala.LanguageServer.Features;
using Cerneala.LanguageServer.Logging;
using Cerneala.LanguageServer.Protocol;
using Cerneala.LanguageServer.Workspace;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cerneala.Tests.LanguageServer;

public sealed class WorkspaceTests
{
    private static readonly CancellationToken TestCancellation = CancellationToken.None;

    [Fact]
    public async Task ProjectContextDoesNotEvaluateProjectAnalyzers()
    {
        using AdhocWorkspace workspace = new();
        ProjectId projectId = ProjectId.CreateNewId();
        Solution solution = workspace.CurrentSolution.AddProject(ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            "Fixture",
            "Fixture",
            LanguageNames.CSharp,
            filePath: Path.Combine(Path.GetTempPath(), "cerneala-analyzer-probe.csproj"),
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            metadataReferences: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]));
        TrackingAnalyzerReference analyzer = new();
        Project project = solution.GetProject(projectId)!.AddAnalyzerReference(analyzer);

        using ProjectContext context = Assert.IsType<ProjectContext>(
            await ProjectContext.CreateAsync(project, revision: 1, TestCancellation));

        Assert.Equal(0, analyzer.RequestCount);
    }

    [Fact]
    public async Task CrnDocumentHasProjectOwnershipAndSemanticContext()
    {
        using TemporaryWorkspace fixture = TemporaryWorkspace.CreateExtensionMigrationProject();
        await using CernealaWorkspace workspace = await CreateWorkspaceAsync(fixture.ProjectPath);

        Assert.Single(workspace.GetOwnerSummaries(fixture.MarkupUri));
        using WorkspaceDocumentSnapshot snapshot = await workspace.GetSnapshotAsync(
            fixture.MarkupUri,
            TestCancellation);

        Assert.False(snapshot.IsStandalone);
        Assert.Contains("System.Runtime", snapshot.ResolveTypeAssemblies("System.String"));
        Assert.Single(snapshot.GetSemanticModels(TestCancellation));
    }

    [Fact]
    public async Task LegacyCuiXmlDocumentHasNoProjectOwnership()
    {
        using TemporaryWorkspace fixture = TemporaryWorkspace.CreateExtensionMigrationProject();
        await using CernealaWorkspace workspace = await CreateWorkspaceAsync(fixture.ProjectPath);
        string legacyUri = new Uri(fixture.LegacyMarkupPath!).AbsoluteUri;

        Assert.Empty(workspace.GetOwnerSummaries(legacyUri));
        using WorkspaceDocumentSnapshot snapshot = await workspace.GetSnapshotAsync(legacyUri, TestCancellation);
        Assert.True(snapshot.IsStandalone);
    }

    [Fact]
    public async Task ProjectWorkspaceUsesUnsavedOverlayAndReturnsToSavedSemanticContext()
    {
        using TemporaryWorkspace fixture = TemporaryWorkspace.CreateSingleProject();
        await using CernealaWorkspace workspace = await CreateWorkspaceAsync(fixture.ProjectPath);

        using WorkspaceDocumentSnapshot saved = await workspace.GetSnapshotAsync(fixture.MarkupUri, TestCancellation);
        Assert.False(saved.IsStandalone);
        Assert.Equal("<Window />", saved.Document.Text.ToString());
        Assert.Contains("System.Runtime", saved.ResolveTypeAssemblies("System.String"));

        Assert.True(workspace.OpenDocument(fixture.MarkupUri, "<Window Title=\"unsaved\" />", 10));
        using WorkspaceDocumentSnapshot overlay = await workspace.GetSnapshotAsync(fixture.MarkupUri, TestCancellation);
        Assert.Equal(10, overlay.Version);
        Assert.Contains("unsaved", overlay.Document.Text.ToString(), StringComparison.Ordinal);
        Assert.Equal(saved.ResolveTypeAssemblies("System.String"), overlay.ResolveTypeAssemblies("System.String"));

        fixture.WriteMarkup("<Window Title=\"saved\" />");
        workspace.CloseDocument(fixture.MarkupUri);
        await workspace.ReloadAsync(TestCancellation);
        using WorkspaceDocumentSnapshot reloaded = await workspace.GetSnapshotAsync(fixture.MarkupUri, TestCancellation);

        Assert.Contains("saved", reloaded.Document.Text.ToString(), StringComparison.Ordinal);
        Assert.Equal(saved.ResolveTypeAssemblies("System.String"), reloaded.ResolveTypeAssemblies("System.String"));
    }

    [Fact]
    public async Task OlderDocumentVersionsAreIgnoredAndStaleRequestsCannotPublish()
    {
        using TemporaryWorkspace fixture = TemporaryWorkspace.CreateSingleProject();
        await using CernealaWorkspace workspace = await CreateWorkspaceAsync(fixture.ProjectPath);
        Assert.True(workspace.OpenDocument(fixture.MarkupUri, "<Window />", 1));

        TaskCompletionSource requestStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<VersionedDocumentResult<string>?> stale = workspace.RunDocumentRequestAsync(
            fixture.MarkupUri,
            async (snapshot, cancellationToken) =>
            {
                requestStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return snapshot.Document.Text.ToString();
            },
            TestCancellation);
        await requestStarted.Task.WaitAsync(TestCancellation);

        Assert.True(workspace.ApplyChanges(
            fixture.MarkupUri,
            2,
            [FullReplacement("<Window Title=\"new\" />")]));
        Assert.False(workspace.ApplyChanges(
            fixture.MarkupUri,
            1,
            [FullReplacement("<Window Title=\"old\" />")]));

        Assert.Null(await stale.WaitAsync(TestCancellation));
        Assert.Contains(workspace.Telemetry.Snapshot(), measurement => measurement.Cancelled);
        using WorkspaceDocumentSnapshot latest = await workspace.GetSnapshotAsync(fixture.MarkupUri, TestCancellation);
        Assert.Equal(2, latest.Version);
        Assert.Contains("new", latest.Document.Text.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReloadPreservesOwnershipForBrokenCompilationAndTracksRenameAndDelete()
    {
        using TemporaryWorkspace fixture = TemporaryWorkspace.CreateSingleProject();
        await using CernealaWorkspace workspace = await CreateWorkspaceAsync(fixture.ProjectPath);
        Assert.Single(workspace.GetOwnerSummaries(fixture.MarkupUri));

        fixture.WriteCode("namespace Fixture; internal sealed class Broken {");
        await workspace.ReloadAsync(TestCancellation);
        Assert.Single(workspace.GetOwnerSummaries(fixture.MarkupUri));

        string originalUri = fixture.MarkupUri;
        string renamedPath = fixture.RenameMarkup("Renamed.crn");
        await workspace.ReloadAsync(TestCancellation);
        Assert.Empty(workspace.GetOwnerSummaries(originalUri));
        Assert.Single(workspace.GetOwnerSummaries(new Uri(renamedPath).AbsoluteUri));

        File.Delete(renamedPath);
        await workspace.ReloadAsync(TestCancellation);
        Assert.Empty(workspace.GetOwnerSummaries(new Uri(renamedPath).AbsoluteUri));
    }

    [Fact]
    public async Task LinkedDocumentCanHaveTwoOwnersWithoutMixingTheirSymbols()
    {
        using TemporaryWorkspace fixture = TemporaryWorkspace.CreateTwoProjectsWithLinkedMarkup();
        await using CernealaWorkspace workspace = await CreateWorkspaceAsync(fixture.SolutionPath!);

        using WorkspaceDocumentSnapshot snapshot = await workspace.GetSnapshotAsync(
            fixture.MarkupUri,
            TestCancellation);

        Assert.Equal(2, snapshot.ProjectSummaries.Count);
        Assert.Equal(["Alpha", "Beta"], snapshot.ResolveTypeAssemblies("Shared.Context"));
        Assert.Single(snapshot.ResolveTypeAssemblies("Alpha.OnlyAlpha"));
        Assert.Single(snapshot.ResolveTypeAssemblies("Beta.OnlyBeta"));
    }

    [Fact]
    public async Task LegacySolutionAndProjectReferencesPreserveLinkedDocumentOwners()
    {
        using TemporaryWorkspace fixture = TemporaryWorkspace.CreateTwoProjectsWithLinkedMarkup(useLegacySolution: true);
        await using CernealaWorkspace workspace = await CreateWorkspaceAsync(fixture.SolutionPath!);

        using WorkspaceDocumentSnapshot snapshot = await workspace.GetSnapshotAsync(
            fixture.MarkupUri,
            TestCancellation);

        Assert.Equal(2, snapshot.ProjectSummaries.Count);
        Assert.Single(snapshot.ResolveTypeAssemblies("Alpha.OnlyAlpha"));
        Assert.Single(snapshot.ResolveTypeAssemblies("Beta.OnlyBeta"));
    }

    [Fact]
    public async Task ActiveTargetFrameworkSelectsOneContextAndDeduplicatesIdenticalResults()
    {
        using TemporaryWorkspace fixture = TemporaryWorkspace.CreateMultiTargetProject();
        WorkspaceConfiguration configuration = new(
            fixture.RootPath,
            fixture.ProjectPath,
            "net10.0",
            "Debug",
            WatchFileSystem: false);
        await using CernealaWorkspace workspace = await CernealaWorkspace.CreateAsync(
            configuration,
            new StructuredServerLogger(TextWriter.Null),
            TestCancellation);

        using WorkspaceDocumentSnapshot snapshot = await workspace.GetSnapshotAsync(
            fixture.MarkupUri,
            TestCancellation);

        WorkspaceProjectSummary owner = Assert.Single(snapshot.ProjectSummaries);
        Assert.Equal("net10.0", owner.TargetFramework);
        Assert.Single(snapshot.ResolveTypeAssemblies("System.String"));
    }

    [Fact]
    public async Task StandaloneDocumentHasSyntaxAndOneInformationalDiagnostic()
    {
        using TemporaryWorkspace fixture = TemporaryWorkspace.CreateStandaloneDocument();
        WorkspaceConfiguration configuration = new(
            fixture.RootPath,
            null,
            null,
            "Debug",
            WatchFileSystem: false);
        await using CernealaWorkspace workspace = await CernealaWorkspace.CreateAsync(
            configuration,
            new StructuredServerLogger(TextWriter.Null),
            TestCancellation);

        using WorkspaceDocumentSnapshot snapshot = await workspace.GetSnapshotAsync(
            fixture.MarkupUri,
            TestCancellation);

        Assert.True(snapshot.IsStandalone);
        Assert.NotEmpty(snapshot.Syntax.Children);
        Assert.Equal("CERNEALAWORKSPACE001", Assert.Single(snapshot.InformationDiagnostics).Id);
    }

    [Fact]
    public async Task RestartLoadsSavedStateAndDropsThePreviousUnsavedOverlay()
    {
        using TemporaryWorkspace fixture = TemporaryWorkspace.CreateSingleProject();
        await using (CernealaWorkspace first = await CreateWorkspaceAsync(fixture.ProjectPath))
        {
            Assert.True(first.OpenDocument(fixture.MarkupUri, "<Window Title=\"unsaved\" />", 5));
            using WorkspaceDocumentSnapshot overlay = await first.GetSnapshotAsync(
                fixture.MarkupUri,
                TestCancellation);
            Assert.Contains("unsaved", overlay.Document.Text.ToString(), StringComparison.Ordinal);
        }

        await using CernealaWorkspace restarted = await CreateWorkspaceAsync(fixture.ProjectPath);
        using WorkspaceDocumentSnapshot saved = await restarted.GetSnapshotAsync(
            fixture.MarkupUri,
            TestCancellation);
        Assert.Equal("<Window />", saved.Document.Text.ToString());
    }

    [Fact]
    public async Task OpenChangeCloseCyclesPlateauAndBoundedCachesAreReleased()
    {
        using TemporaryWorkspace fixture = TemporaryWorkspace.CreateSingleProject();
        await using CernealaWorkspace workspace = await CreateWorkspaceAsync(fixture.ProjectPath);
        using StructureService structure = new(workspace);
        long before = GC.GetTotalMemory(forceFullCollection: true);

        for (int cycle = 0; cycle < 1000; cycle++)
        {
            string uri = new Uri(Path.Combine(fixture.RootPath, "Cycle" + cycle + ".crn")).AbsoluteUri;
            Assert.True(workspace.OpenDocument(uri, "<Window />", 1));
            Assert.True(workspace.ApplyChanges(uri, 2, [FullReplacement("<Window Title=\"" + cycle + "\" />")]));
            using WorkspaceDocumentSnapshot snapshot = await workspace.GetSnapshotAsync(uri, TestCancellation);
            Assert.Equal(2, snapshot.Version);
            workspace.CloseDocument(uri);
        }

        for (int index = 0; index < StructureService.MaximumTokenCacheEntries + 32; index++)
        {
            string uri = new Uri(Path.Combine(fixture.RootPath, "Token" + index + ".crn")).AbsoluteUri;
            Assert.True(workspace.OpenDocument(uri, "<Window />", 1));
            Assert.NotNull(await structure.GetSemanticTokensAsync(uri, TestCancellation));
        }

        Assert.Equal(StructureService.MaximumTokenCacheEntries, structure.CachedDocumentCount);
        Assert.Equal(StructureService.MaximumTokenCacheEntries + 32, workspace.OpenDocumentCount);
        for (int index = 0; index < StructureService.MaximumTokenCacheEntries + 32; index++)
        {
            string uri = new Uri(Path.Combine(fixture.RootPath, "Token" + index + ".crn")).AbsoluteUri;
            workspace.CloseDocument(uri);
            structure.Clear(uri);
        }

        Assert.Equal(0, workspace.OpenDocumentCount);
        Assert.Equal(0, structure.CachedDocumentCount);
        Assert.True(workspace.Telemetry.Snapshot().Count <= ServerTelemetry.MaximumRetainedMeasurements);
        long retained = GC.GetTotalMemory(forceFullCollection: true) - before;
        Assert.True(retained < 32 * 1024 * 1024, "Retained memory grew by " + retained + " bytes.");
    }

    private static TextDocumentContentChangeEvent FullReplacement(string text) => new() { Text = text };

    private static Task<CernealaWorkspace> CreateWorkspaceAsync(string workspacePath)
    {
        WorkspaceConfiguration configuration = new(
            Path.GetDirectoryName(workspacePath),
            workspacePath,
            null,
            "Debug",
            WatchFileSystem: false);
        return CernealaWorkspace.CreateAsync(
            configuration,
            new StructuredServerLogger(TextWriter.Null),
            TestCancellation);
    }

    private sealed class TrackingAnalyzerReference : AnalyzerReference
    {
        public int RequestCount { get; private set; }

        public override string FullPath => "tracking-analyzer.dll";

        public override object Id { get; } = new();

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language)
        {
            RequestCount++;
            return [];
        }

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages()
        {
            RequestCount++;
            return [];
        }

        public override ImmutableArray<ISourceGenerator> GetGenerators(string language)
        {
            RequestCount++;
            return [];
        }

        public override ImmutableArray<ISourceGenerator> GetGeneratorsForAllLanguages()
        {
            RequestCount++;
            return [];
        }
    }

    private sealed class TemporaryWorkspace : IDisposable
    {
        private TemporaryWorkspace(
            string rootPath,
            string projectPath,
            string markupPath,
            string? solutionPath = null,
            string? legacyMarkupPath = null)
        {
            RootPath = rootPath;
            ProjectPath = projectPath;
            MarkupPath = markupPath;
            SolutionPath = solutionPath;
            LegacyMarkupPath = legacyMarkupPath;
        }

        public string RootPath { get; }

        public string ProjectPath { get; }

        public string MarkupPath { get; private set; }

        public string MarkupUri => new Uri(MarkupPath).AbsoluteUri;

        public string? SolutionPath { get; }

        public string? LegacyMarkupPath { get; }

        public static TemporaryWorkspace CreateExtensionMigrationProject()
        {
            string root = CreateRoot();
            string project = Path.Combine(root, "Fixture.csproj");
            string markup = Path.Combine(root, "View.crn");
            string legacyMarkup = Path.Combine(root, "Legacy.cui.xml");
            WriteProject(project, "Fixture", [markup, legacyMarkup]);
            File.WriteAllText(markup, "<Window />");
            File.WriteAllText(legacyMarkup, "<Window />");
            return new TemporaryWorkspace(root, project, markup, legacyMarkupPath: legacyMarkup);
        }

        public static TemporaryWorkspace CreateSingleProject()
        {
            string root = CreateRoot();
            string project = Path.Combine(root, "Fixture.csproj");
            string markup = Path.Combine(root, "View.crn");
            WriteProject(project, "Fixture", [markup]);
            File.WriteAllText(markup, "<Window />");
            File.WriteAllText(Path.Combine(root, "View.crn.cs"), "namespace Fixture; internal sealed class View { }");
            return new TemporaryWorkspace(root, project, markup);
        }

        public static TemporaryWorkspace CreateTwoProjectsWithLinkedMarkup(bool useLegacySolution = false)
        {
            string root = CreateRoot();
            string shared = Path.Combine(root, "Shared.crn");
            File.WriteAllText(shared, "<Window />");
            string alphaDirectory = Directory.CreateDirectory(Path.Combine(root, "Alpha")).FullName;
            string betaDirectory = Directory.CreateDirectory(Path.Combine(root, "Beta")).FullName;
            string alphaProject = Path.Combine(alphaDirectory, "Alpha.csproj");
            string betaProject = Path.Combine(betaDirectory, "Beta.csproj");
            WriteProject(alphaProject, "Alpha", [shared]);
            WriteProject(betaProject, "Beta", [shared]);
            XDocument beta = XDocument.Load(betaProject);
            beta.Root!.Add(new XElement(
                "ItemGroup",
                new XElement("ProjectReference", new XAttribute("Include", "../Alpha/Alpha.csproj"))));
            beta.Save(betaProject);
            File.WriteAllText(Path.Combine(alphaDirectory, "Types.cs"), "namespace Shared { internal sealed class Context { } } namespace Alpha { internal sealed class OnlyAlpha { } }");
            File.WriteAllText(Path.Combine(betaDirectory, "Types.cs"), "namespace Shared { internal sealed class Context { } } namespace Beta { internal sealed class OnlyBeta { } }");
            string solution = useLegacySolution
                ? WriteLegacySolution(root)
                : WriteXmlSolution(root);
            return new TemporaryWorkspace(root, alphaProject, shared, solution);
        }

        public static TemporaryWorkspace CreateMultiTargetProject()
        {
            string root = CreateRoot();
            string project = Path.Combine(root, "Multi.csproj");
            string markup = Path.Combine(root, "View.crn");
            File.WriteAllText(project, """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFrameworks>net8.0;net10.0</TargetFrameworks>
                    <AssemblyName>Multi</AssemblyName>
                  </PropertyGroup>
                  <ItemGroup>
                    <AdditionalFiles Include="View.crn" />
                  </ItemGroup>
                </Project>
                """);
            File.WriteAllText(markup, "<Window />");
            File.WriteAllText(Path.Combine(root, "Types.cs"), "namespace Multi; internal sealed class Context { }");
            return new TemporaryWorkspace(root, project, markup);
        }

        public static TemporaryWorkspace CreateStandaloneDocument()
        {
            string root = CreateRoot();
            string markup = Path.Combine(root, "Loose.crn");
            File.WriteAllText(markup, "<Window />");
            return new TemporaryWorkspace(root, Path.Combine(root, "missing.csproj"), markup);
        }

        public void WriteMarkup(string text) => File.WriteAllText(MarkupPath, text);

        public void WriteCode(string text) => File.WriteAllText(Path.Combine(RootPath, "View.crn.cs"), text);

        public string RenameMarkup(string fileName)
        {
            string next = Path.Combine(RootPath, fileName);
            File.Move(MarkupPath, next);
            XDocument project = XDocument.Load(ProjectPath);
            project.Descendants("AdditionalFiles").Single().SetAttributeValue("Include", fileName);
            project.Save(ProjectPath);
            MarkupPath = next;
            return next;
        }

        public void Dispose()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }

        private static string CreateRoot()
        {
            string path = Path.Combine(Path.GetTempPath(), $"cerneala-lsp-workspace-{Guid.NewGuid():N}");
            Directory.CreateDirectory(path);
            return path;
        }

        private static void WriteProject(string path, string assemblyName, IReadOnlyList<string> additionalFiles)
        {
            string projectDirectory = Path.GetDirectoryName(path)!;
            XElement itemGroup = new("ItemGroup",
                additionalFiles.Select(file => new XElement(
                    "AdditionalFiles",
                    new XAttribute("Include", Path.GetRelativePath(projectDirectory, file)))));
            XDocument project = new(
                new XElement("Project",
                    new XAttribute("Sdk", "Microsoft.NET.Sdk"),
                    new XElement("PropertyGroup",
                        new XElement("TargetFramework", "net10.0"),
                        new XElement("AssemblyName", assemblyName)),
                    itemGroup));
            project.Save(path);
        }

        private static string WriteXmlSolution(string root)
        {
            string path = Path.Combine(root, "Fixture.slnx");
            XDocument document = new(
                new XElement("Solution",
                    new XElement("Project", new XAttribute("Path", "Alpha/Alpha.csproj")),
                    new XElement("Project", new XAttribute("Path", "Beta/Beta.csproj"))));
            document.Save(path);
            return path;
        }

        private static string WriteLegacySolution(string root)
        {
            string path = Path.Combine(root, "Fixture.sln");
            File.WriteAllText(path, """
                Microsoft Visual Studio Solution File, Format Version 12.00
                # Visual Studio Version 17
                VisualStudioVersion = 17.0.31903.59
                MinimumVisualStudioVersion = 10.0.40219.1
                Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Alpha", "Alpha\Alpha.csproj", "{11111111-1111-1111-1111-111111111111}"
                EndProject
                Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Beta", "Beta\Beta.csproj", "{22222222-2222-2222-2222-222222222222}"
                EndProject
                Global
                  GlobalSection(SolutionConfigurationPlatforms) = preSolution
                    Debug|Any CPU = Debug|Any CPU
                  EndGlobalSection
                  GlobalSection(ProjectConfigurationPlatforms) = postSolution
                    {11111111-1111-1111-1111-111111111111}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
                    {11111111-1111-1111-1111-111111111111}.Debug|Any CPU.Build.0 = Debug|Any CPU
                    {22222222-2222-2222-2222-222222222222}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
                    {22222222-2222-2222-2222-222222222222}.Debug|Any CPU.Build.0 = Debug|Any CPU
                  EndGlobalSection
                EndGlobal
                """);
            return path;
        }
    }
}
