using System.Text.Json;
using System.Xml.Linq;
using Cerneala.Language.Diagnostics;
using Cerneala.Language.Text;
using Cerneala.LanguageServer.Features;
using Cerneala.LanguageServer.Logging;
using Cerneala.LanguageServer.Protocol;
using Cerneala.LanguageServer.Workspace;
using Cerneala.SourceGen;
using Microsoft.CodeAnalysis;
using RoslynSourceText = Microsoft.CodeAnalysis.Text.SourceText;

namespace Cerneala.Tests.LanguageServer;

public sealed class DiagnosticsTests
{
    private static readonly JsonSerializerOptions GoldenJson = new() { WriteIndented = true };

    [Fact]
    public void CatalogGoldenMatchesLspAndSourceGeneratorForEveryDiagnostic()
    {
        const string path = "catalog.crn";
        SourceText languageSource = SourceText.From("A\U0001F600\r\n0123456789");
        RoslynSourceText roslynSource = RoslynSourceText.From(languageSource.ToString());
        TextSpan span = new(7, 3);
        object[] arguments = ["arg0", "arg1", "arg2", "arg3"];

        DiagnosticGolden[] actual = CernealaDiagnosticCatalog.All.Select(descriptor =>
        {
            LanguageDiagnostic language = new(descriptor, span, AnalysisMode.Editor, arguments);
            LspDiagnostic lsp = DiagnosticService.ToLspDiagnostic(languageSource, language);
            Diagnostic sourceGenerator = SourceGeneratorDiagnosticAdapter.ToDiagnostic(language, path, roslynSource);
            FileLinePositionSpan sourceSpan = sourceGenerator.Location.GetLineSpan();

            Assert.Equal(sourceGenerator.Id, lsp.Code);
            Assert.Equal(sourceGenerator.GetMessage(), lsp.Message);
            Assert.Equal(sourceSpan.StartLinePosition.Line, lsp.Range.Start.Line);
            Assert.Equal(sourceSpan.StartLinePosition.Character, lsp.Range.Start.Character);
            Assert.Equal(sourceSpan.EndLinePosition.Line, lsp.Range.End.Line);
            Assert.Equal(sourceSpan.EndLinePosition.Character, lsp.Range.End.Character);
            if (descriptor.EditorSeverity == descriptor.BuildSeverity)
            {
                Assert.Equal(ToLspSeverity(sourceGenerator.Severity), lsp.Severity);
            }
            else
            {
                Assert.Equal(3, lsp.Severity);
                Assert.Equal(1, ToLspSeverity(sourceGenerator.Severity));
            }

            return new DiagnosticGolden(
                lsp.Code,
                lsp.Severity,
                ToLspSeverity(sourceGenerator.Severity),
                lsp.Message,
                lsp.Range.Start.Line,
                lsp.Range.Start.Character,
                lsp.Range.End.Line,
                lsp.Range.End.Character);
        }).ToArray();

        Assert.Equal(CernealaDiagnosticCatalog.All.Count, actual.Length);
        Assert.Contains(actual, diagnostic => diagnostic.Id.StartsWith("CERNEALAUI", StringComparison.Ordinal));
        Assert.Contains(actual, diagnostic => diagnostic.Id.StartsWith("PRISM", StringComparison.Ordinal));

        string goldenPath = Path.Combine(
            RepositoryRoot(),
            "tests",
            "Cerneala.Tests.LanguageServer",
            "Diagnostics",
            "diagnostic-catalog-golden.json");
        if (string.Equals(Environment.GetEnvironmentVariable("CERNEALA_UPDATE_BASELINES"), "1", StringComparison.Ordinal))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(goldenPath)!);
            File.WriteAllText(goldenPath, JsonSerializer.Serialize(actual, GoldenJson) + Environment.NewLine);
        }

        DiagnosticGolden[] expected = JsonSerializer.Deserialize<DiagnosticGolden[]>(
            File.ReadAllText(goldenPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MappingUsesUtf16CodeUnitsAndClampsTheSourceSpan()
    {
        SourceText source = SourceText.From("\U0001F600\nabc");
        LanguageDiagnostic language = new(
            CernealaDiagnosticCatalog.Get("CERNEALAUI002"),
            new TextSpan(4, 100),
            AnalysisMode.Editor,
            "Missing");

        LspDiagnostic diagnostic = DiagnosticService.ToLspDiagnostic(source, language);

        Assert.Equal(1, diagnostic.Range.Start.Line);
        Assert.Equal(1, diagnostic.Range.Start.Character);
        Assert.Equal(1, diagnostic.Range.End.Line);
        Assert.Equal(3, diagnostic.Range.End.Character);
    }

    [Fact]
    public async Task StandaloneDocumentShapeUsesTheCommonTransientDiagnostic()
    {
        string root = Path.Combine(Path.GetTempPath(), $"cerneala-lsp-standalone-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            string path = Path.Combine(root, "Loose.crn");
            File.WriteAllText(path, "<Window /><Window />");
            WorkspaceConfiguration configuration = new(root, null, null, "Debug", WatchFileSystem: false);
            await using CernealaWorkspace workspace = await CernealaWorkspace.CreateAsync(
                configuration,
                new StructuredServerLogger(TextWriter.Null),
                CancellationToken.None);
            DiagnosticService service = new(workspace, new BuildDiagnosticStore());

            VersionedDocumentResult<IReadOnlyList<LspDiagnostic>>? result = await service.AnalyzeAsync(
                new Uri(path).AbsoluteUri,
                CancellationToken.None);

            Assert.NotNull(result);
            LspDiagnostic shape = Assert.Single(result.Value, diagnostic => diagnostic.Code == "CERNEALAUI001");
            Assert.Equal(3, shape.Severity);
            Assert.Contains("exactly one UI root element", shape.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task MissingMotionAssignmentSemicolonIsPublishedAsEditorError()
    {
        string root = RepositoryRoot();
        string projectPath = Path.Combine(root, "CernealaPresentation", "CernealaPresentation.csproj");
        string markupPath = Path.Combine(root, "CernealaPresentation", "OpeningView.crn");
        const string validAssignment = "$VisualStage.Opacity = 1;";
        const string invalidAssignment = "$VisualStage.Opacity = 1";
        string markup = File.ReadAllText(markupPath);
        Assert.Contains(validAssignment, markup, StringComparison.Ordinal);
        markup = markup.Replace(validAssignment, invalidAssignment, StringComparison.Ordinal);

        await using CernealaWorkspace workspace = await CreateWorkspaceAsync(projectPath);
        string uri = new Uri(markupPath).AbsoluteUri;
        Assert.True(workspace.OpenDocument(uri, markup, 1));
        DiagnosticService service = new(workspace, new BuildDiagnosticStore());

        VersionedDocumentResult<IReadOnlyList<LspDiagnostic>>? result = await service.AnalyzeAsync(
            uri,
            CancellationToken.None);

        Assert.NotNull(result);
        LspDiagnostic diagnostic = Assert.Single(
            result.Value,
            candidate => candidate.Code == "CERNEALAUI020" &&
                candidate.Message.Contains("must end with ';'", StringComparison.Ordinal));
        Assert.Equal(1, diagnostic.Severity);
        Assert.Equal(diagnostic.Range.Start.Line, diagnostic.Range.End.Line);
        Assert.True(diagnostic.Range.End.Character > diagnostic.Range.Start.Character);
    }

    [Fact]
    public async Task MissingRequiredSemicolonsAcrossAspectAndMotionArePublishedAsEditorErrors()
    {
        string root = RepositoryRoot();
        string projectPath = Path.Combine(root, "CernealaPresentation", "CernealaPresentation.csproj");
        string markupPath = Path.Combine(root, "CernealaPresentation", "OpeningView.crn");
        const string validRun = "@run $LoadingSequence as Loading;";
        const string validAspectAssignment = "FontFamily = \"Segoe UI Variable Text\";";
        string markup = File.ReadAllText(markupPath);

        int runIndex = markup.IndexOf(validRun, StringComparison.Ordinal);
        Assert.True(runIndex >= 0);
        markup = markup.Remove(runIndex + validRun.Length - 1, 1);

        int aspectIndex = markup.IndexOf(validAspectAssignment, StringComparison.Ordinal);
        Assert.True(aspectIndex >= 0);
        markup = markup.Remove(aspectIndex + validAspectAssignment.Length - 1, 1);

        await using CernealaWorkspace workspace = await CreateWorkspaceAsync(projectPath);
        string uri = new Uri(markupPath).AbsoluteUri;
        Assert.True(workspace.OpenDocument(uri, markup, 1));
        DiagnosticService service = new(workspace, new BuildDiagnosticStore());

        VersionedDocumentResult<IReadOnlyList<LspDiagnostic>>? result = await service.AnalyzeAsync(
            uri,
            CancellationToken.None);

        Assert.NotNull(result);
        LspDiagnostic motionDiagnostic = Assert.Single(
            result.Value,
            candidate => candidate.Code == "CERNEALAUI020" &&
                candidate.Message.Contains("must end with ';'", StringComparison.Ordinal));
        LspDiagnostic aspectDiagnostic = Assert.Single(
            result.Value,
            candidate => candidate.Code == "CERNEALAUI006" &&
                candidate.Message.Contains("must end with ';'", StringComparison.Ordinal));

        Assert.Equal(1, motionDiagnostic.Severity);
        Assert.Equal(1, aspectDiagnostic.Severity);
        Assert.True(motionDiagnostic.Range.End.Character > motionDiagnostic.Range.Start.Character);
        Assert.True(aspectDiagnostic.Range.End.Character > aspectDiagnostic.Range.Start.Character);
    }

    [Fact]
    public void BuildDedupeRemovesOnlyTheExactSourceGeneratorIdentity()
    {
        string uri = new Uri(Path.GetFullPath("Dedupe.crn")).AbsoluteUri;
        LspDiagnostic duplicate = Diagnostic("CERNEALAUI003", 2, 4, 2, 11, "same");
        LspDiagnostic distinctSpan = Diagnostic("CERNEALAUI003", 3, 4, 3, 11, "same");
        LspDiagnostic distinctCause = Diagnostic("CERNEALAUI003", 2, 4, 2, 11, "different");
        BuildDiagnosticStore store = new();
        store.Replace(
        [
            new BuildDiagnosticItem
            {
                Uri = uri,
                Code = duplicate.Code,
                Range = duplicate.Range,
                Message = duplicate.Message
            }
        ]);

        IReadOnlyList<LspDiagnostic> filtered = store.RemoveDuplicates(
            uri,
            [duplicate, distinctSpan, distinctCause]);

        Assert.Equal([distinctCause, distinctSpan], filtered.OrderBy(item => item.Message).ToArray());
    }

    [Fact]
    public async Task CharacterByCharacterRecoveryNeverPublishesDependentSemanticErrors()
    {
        using TemporaryDiagnosticWorkspace fixture = TemporaryDiagnosticWorkspace.Create();
        await using CernealaWorkspace workspace = await CreateWorkspaceAsync(fixture.ProjectPath);
        BuildDiagnosticStore buildDiagnostics = new();
        DiagnosticService service = new(workspace, buildDiagnostics);
        Assert.True(workspace.OpenDocument(fixture.MarkupUri, string.Empty, 1));
        long version = 1;
        string[] scenarios =
        [
            "<StackPanel />",
            "<StackPanel></StackPanel>",
            "<TextBlock Text=\"ok\" />",
            "<Button Content=\"$self.IsEnabled\" />",
            "<StackPanel>@when IsEnabled { IsEnabled = True; }</StackPanel>",
            "<Button>@template { <Border Name=\"Chrome\" /> }</Button>"
        ];

        foreach (string scenario in scenarios)
        {
            for (int length = 1; length <= scenario.Length; length++)
            {
                string prefix = scenario[..length];
                Assert.True(workspace.ApplyChanges(
                    fixture.MarkupUri,
                    ++version,
                    [new TextDocumentContentChangeEvent { Text = prefix }]));
                VersionedDocumentResult<IReadOnlyList<LspDiagnostic>>? result =
                    await service.AnalyzeAsync(fixture.MarkupUri, CancellationToken.None);
                Assert.NotNull(result);
                LspDiagnostic[] errors = result.Value.Where(diagnostic => diagnostic.Severity == 1).ToArray();
                Assert.True(
                    errors.Length == 0,
                    $"Prefix '{prefix}' produced: {string.Join(" | ", errors.Select(error => error.Code + ": " + error.Message))}");
            }
        }
    }

    [Fact]
    public async Task CompilingRepositoryDocumentsHaveNoEditorDiagnostics()
    {
        string root = RepositoryRoot();
        string[] projects =
        [
            Path.Combine(root, "CernealaPresentation", "CernealaPresentation.csproj"),
            Path.Combine(root, "Playground", "Cerneala.ComboBoxLab", "Cerneala.ComboBoxLab.csproj"),
            Path.Combine(root, "Playground", "Cerneala.Playground", "Cerneala.Playground.csproj"),
            Path.Combine(root, "Playground", "CernealaOracle", "CernealaOracle.csproj")
        ];

        foreach (string project in projects)
        {
            await using CernealaWorkspace workspace = await CreateWorkspaceAsync(project);
            DiagnosticService service = new(workspace, new BuildDiagnosticStore());
            string projectDirectory = Path.GetDirectoryName(project)!;
            string[] documents = Directory.EnumerateFiles(projectDirectory, "*.crn", SearchOption.AllDirectories)
                .Where(path => !IsBuildOutput(path))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            Assert.NotEmpty(documents);

            foreach (string document in documents)
            {
                string uri = new Uri(document).AbsoluteUri;
                Assert.NotEmpty(workspace.GetOwnerSummaries(uri));
                VersionedDocumentResult<IReadOnlyList<LspDiagnostic>>? result =
                    await service.AnalyzeAsync(uri, CancellationToken.None);
                Assert.NotNull(result);
                Assert.True(
                    result.Value.Count == 0,
                    $"{Path.GetRelativePath(root, document)}: {string.Join(" | ", result.Value.Select(error => error.Code + ": " + error.Message))}");
            }
        }
    }

    private static LspDiagnostic Diagnostic(
        string code,
        int startLine,
        int startCharacter,
        int endLine,
        int endCharacter,
        string message) => new()
    {
        Code = code,
        Message = message,
        Severity = 1,
        Range = new LspRange
        {
            Start = new LspPosition { Line = startLine, Character = startCharacter },
            End = new LspPosition { Line = endLine, Character = endCharacter }
        }
    };

    private static int ToLspSeverity(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Error => 1,
        DiagnosticSeverity.Warning => 2,
        DiagnosticSeverity.Info => 3,
        _ => 4
    };

    private static bool IsBuildOutput(string path)
    {
        string normalized = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        string separator = Path.DirectorySeparatorChar.ToString();
        return normalized.Contains(separator + "bin" + separator, StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains(separator + "obj" + separator, StringComparison.OrdinalIgnoreCase);
    }

    private static Task<CernealaWorkspace> CreateWorkspaceAsync(string projectPath)
    {
        WorkspaceConfiguration configuration = new(
            Path.GetDirectoryName(projectPath),
            projectPath,
            null,
            "Debug",
            WatchFileSystem: false);
        return CernealaWorkspace.CreateAsync(
            configuration,
            new StructuredServerLogger(TextWriter.Null),
            CancellationToken.None);
    }

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate the Cerneala repository root.");
    }

    private sealed class TemporaryDiagnosticWorkspace : IDisposable
    {
        private TemporaryDiagnosticWorkspace(string rootPath, string projectPath, string markupPath)
        {
            RootPath = rootPath;
            ProjectPath = projectPath;
            MarkupPath = markupPath;
        }

        public string RootPath { get; }

        public string ProjectPath { get; }

        public string MarkupPath { get; }

        public string MarkupUri => new Uri(MarkupPath).AbsoluteUri;

        public static TemporaryDiagnosticWorkspace Create()
        {
            string repositoryRoot = RepositoryRoot();
            string root = Path.Combine(Path.GetTempPath(), $"cerneala-lsp-diagnostics-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            string projectPath = Path.Combine(root, "Diagnostics.csproj");
            string markupPath = Path.Combine(root, "View.crn");
            XDocument project = new(
                new XElement("Project",
                    new XAttribute("Sdk", "Microsoft.NET.Sdk"),
                    new XElement("PropertyGroup",
                        new XElement("TargetFramework", "net10.0")),
                    new XElement("ItemGroup",
                        new XElement("ProjectReference",
                            new XAttribute("Include", Path.Combine(repositoryRoot, "Cerneala.csproj"))),
                        new XElement("AdditionalFiles", new XAttribute("Include", "View.crn")))));
            project.Save(projectPath);
            File.WriteAllText(markupPath, "<StackPanel />");
            File.WriteAllText(
                Path.Combine(root, "Anchor.cs"),
                "namespace DiagnosticsFixture; internal sealed class Anchor { }");
            return new TemporaryDiagnosticWorkspace(root, projectPath, markupPath);
        }

        public void Dispose()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
    }

    private sealed record DiagnosticGolden(
        string Id,
        int EditorSeverity,
        int BuildSeverity,
        string Message,
        int StartLine,
        int StartCharacter,
        int EndLine,
        int EndCharacter);
}
