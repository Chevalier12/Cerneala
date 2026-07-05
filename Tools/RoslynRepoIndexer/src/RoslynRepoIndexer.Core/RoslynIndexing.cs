using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;

namespace RoslynRepoIndexer.Core;

public static class MSBuildRegistration
{
    private static int registered;

    public static void RegisterDefaults()
    {
        if (Interlocked.Exchange(ref registered, 1) == 1 || MSBuildLocator.IsRegistered)
        {
            return;
        }

        var instances = MSBuildLocator.QueryVisualStudioInstances().OrderByDescending(i => i.Version).ToArray();
        if (instances.Length > 0)
        {
            MSBuildLocator.RegisterInstance(instances[0]);
            return;
        }

        var sdkPath = DotnetSdkMSBuildPath();
        if (sdkPath is not null)
        {
            MSBuildLocator.RegisterMSBuildPath(sdkPath);
            return;
        }

        MSBuildLocator.RegisterDefaults();
    }

    private static string? DotnetSdkMSBuildPath()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("dotnet")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }.WithArguments("--list-sdks"));
            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(3000);
            if (process.ExitCode != 0)
            {
                return null;
            }

            return output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line =>
                {
                    var bracketStart = line.IndexOf('[', StringComparison.Ordinal);
                    var bracketEnd = line.IndexOf(']', StringComparison.Ordinal);
                    if (bracketStart < 0 || bracketEnd <= bracketStart)
                    {
                        return null;
                    }

                    var version = line[..bracketStart].Trim();
                    var basePath = line[(bracketStart + 1)..bracketEnd];
                    var sdkPath = Path.Combine(basePath, version);
                    return File.Exists(Path.Combine(sdkPath, "MSBuild.dll")) ? sdkPath : null;
                })
                .Where(path => path is not null)
                .LastOrDefault();
        }
        catch
        {
            return null;
        }
    }
}

public sealed class DiagnosticsCollector
{
    private readonly List<string> warnings = new();
    private readonly List<string> errors = new();

    public IReadOnlyList<string> Warnings => warnings;
    public IReadOnlyList<string> Errors => errors;

    public void Warn(string message) => warnings.Add(message);
    public void Error(string message) => errors.Add(message);
}

public sealed class WorkspaceLoader
{
    public IReadOnlyList<WorkspaceInput> Discover(string repoRoot, IndexerConfig config)
        => WorkspaceDiscovery.Discover(repoRoot, config);

    public async Task<LoadedWorkspace> LoadAsync(string repoRoot, WorkspaceInput input, CancellationToken cancellationToken = default)
    {
        _ = repoRoot;
        MSBuildRegistration.RegisterDefaults();
        var workspace = IndexBuilder.CreateWorkspace();
        if (input.Kind.Equals("csproj", StringComparison.OrdinalIgnoreCase))
        {
            var project = await workspace.OpenProjectAsync(input.Path, cancellationToken: cancellationToken).ConfigureAwait(false);
            return new LoadedWorkspace(workspace, IndexBuilder.RemoveAnalyzerReferences(project).Solution);
        }
        else
        {
            var solution = await workspace.OpenSolutionAsync(input.Path, cancellationToken: cancellationToken).ConfigureAwait(false);
            return new LoadedWorkspace(workspace, IndexBuilder.RemoveAnalyzerReferences(solution));
        }
    }
}

public sealed class LoadedWorkspace : IDisposable
{
    private readonly MSBuildWorkspace workspace;

    public LoadedWorkspace(MSBuildWorkspace workspace, Solution solution)
    {
        this.workspace = workspace;
        Solution = solution;
    }

    public Solution Solution { get; }

    public void Dispose() => workspace.Dispose();
}

public sealed class SymbolIdProvider
{
    public string Create(string kind, string fullyQualifiedName, string signature, string path, int line, int column)
        => ConfigLoader.HashText(string.Join('|', kind, fullyQualifiedName, signature, path, line, column))[..16];

    internal static SymbolIdentity CreateIdentity(ISymbol symbol, string? projectId, string relativePath, CancellationToken cancellationToken)
    {
        if (symbol.Kind is SymbolKind.Local or SymbolKind.Parameter)
        {
            return DeterministicLocal(symbol, projectId, relativePath);
        }

        var documentationId = DocumentationCommentId.CreateDeclarationId(symbol);
        if (!string.IsNullOrWhiteSpace(documentationId))
        {
            return new SymbolIdentity(documentationId, "doc-comment-id");
        }

        var symbolKey = CreateSymbolKey(symbol, cancellationToken);
        if (!string.IsNullOrWhiteSpace(symbolKey))
        {
            return new SymbolIdentity("symbol-key:" + ConfigLoader.HashText(symbolKey)[..16], symbolKey);
        }

        return DeterministicLocal(symbol, projectId, relativePath);
    }

    private static SymbolIdentity DeterministicLocal(ISymbol symbol, string? projectId, string relativePath)
    {
        var location = symbol.Locations.FirstOrDefault(location => location.IsInSource);
        var span = location?.SourceSpan ?? default;
        var kind = IndexBuilder.MapKind(symbol);
        var id = string.Join('|', projectId ?? string.Empty, relativePath, span.Start, span.Length, symbol.Name, kind);
        return new SymbolIdentity("local:" + id, "deterministic-local");
    }

    private static string CreateSymbolKey(ISymbol symbol, CancellationToken cancellationToken)
    {
        var symbolKeyType = typeof(Workspace).Assembly.GetType("Microsoft.CodeAnalysis.SymbolKey");
        var create = symbolKeyType?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .FirstOrDefault(method =>
            {
                if (method.Name != "Create")
                {
                    return false;
                }

                var parameters = method.GetParameters();
                return parameters.Length == 2
                       && parameters[0].ParameterType.IsAssignableFrom(typeof(ISymbol))
                       && parameters[1].ParameterType == typeof(CancellationToken);
            });

        if (create is not null)
        {
            var key = create.Invoke(null, new object[] { symbol, cancellationToken });
            var value = key?.ToString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Join(
            '|',
            "semantic",
            IndexBuilder.MapKind(symbol),
            IndexBuilder.DisplayName(symbol, SymbolDisplayFormat.FullyQualifiedFormat),
            IndexBuilder.DisplayName(symbol, SymbolDisplayFormat.CSharpErrorMessageFormat),
            IndexBuilder.AccessibilityName(symbol),
            string.Join(',', IndexBuilder.Modifiers(symbol).OrderBy(modifier => modifier, StringComparer.Ordinal)));
    }
}

internal sealed record SymbolIdentity(string SymbolId, string SymbolKey);

public sealed class SymbolCollector
{
    public IReadOnlyList<SymbolEntry> Collect(SyntaxNode root, SemanticModel semanticModel, string documentId, string projectId, string relativePath, string projectName)
    {
        return IndexBuilder.IndexableDeclarationNodes(root)
            .Select(node => (Node: node, Symbol: semanticModel.GetDeclaredSymbol(node)))
            .Where(pair => pair.Symbol is not null && IndexBuilder.IsIndexableDeclaration(pair.Symbol))
            .Select(pair => IndexBuilder.ToSymbolEntry(pair.Symbol!, pair.Node.GetLocation(), documentId, projectId, relativePath, projectName))
            .ToArray();
    }
}

public sealed class ReferenceCollector
{
    public IReadOnlyList<ReferenceEntry> Collect(SyntaxNode root, SemanticModel semanticModel, string documentId, string projectId, string relativePath, string projectName)
    {
        var localSymbolIds = new Dictionary<ISymbol, string>(SymbolEqualityComparer.Default);
        foreach (var node in IndexBuilder.IndexableDeclarationNodes(root))
        {
            var declared = semanticModel.GetDeclaredSymbol(node);
            if (declared is null || !IndexBuilder.IsIndexableDeclaration(declared))
            {
                continue;
            }

            var entry = IndexBuilder.ToSymbolEntry(declared, node.GetLocation(), documentId, projectId, relativePath, projectName);
            localSymbolIds.TryAdd(declared, entry.SymbolId);
            if (!SymbolEqualityComparer.Default.Equals(declared, declared.OriginalDefinition))
            {
                localSymbolIds.TryAdd(declared.OriginalDefinition, entry.SymbolId);
            }
        }

        var references = new List<ReferenceEntry>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in root.DescendantNodesAndSelf())
        {
            var declared = IndexBuilder.IsDeclarationSyntax(node) ? semanticModel.GetDeclaredSymbol(node) : null;
            var referenced = IndexBuilder.ReferencedSymbol(semanticModel, node, CancellationToken.None);
            if (referenced is null
                || SymbolEqualityComparer.Default.Equals(referenced, declared)
                || (!localSymbolIds.TryGetValue(referenced, out var symbolId)
                    && (SymbolEqualityComparer.Default.Equals(referenced, referenced.OriginalDefinition)
                        || !localSymbolIds.TryGetValue(referenced.OriginalDefinition, out symbolId!))))
            {
                continue;
            }

            var sourceSpan = node.GetLocation().SourceSpan;
            if (seen.Add($"{symbolId}|{documentId}|{sourceSpan.Start}|{sourceSpan.Length}"))
            {
                references.Add(IndexBuilder.ToReferenceEntry(referenced, symbolId, node.GetLocation(), documentId, projectId, relativePath, projectName, node));
            }
        }

        return references;
    }
}

public sealed class TextIndexer
{
    public IReadOnlyList<TokenPosting> IndexText(string relativePath, string text, string? projectName, string documentId)
        => Tokenizer.Tokenize(text)
            .Select(t => new TokenPosting(t.Value, relativePath, t.Line, t.Column, "text", "text", projectName, documentId))
            .Concat(Tokenizer.TokenizePath(relativePath).Select(t => new TokenPosting(t.Value, relativePath, 1, 1, "path", "path", projectName, documentId)))
            .ToArray();
}

public sealed class IndexReader
{
    public IndexSnapshot Read(string repoRoot) => IndexStore.Read(repoRoot);
}

public sealed class JsonOutputWriter
{
    public string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions.Default);
}

public sealed class HumanOutputWriter
{
    public void WriteSearchResults(TextWriter writer, IReadOnlyList<SearchResult> results, string emptyMessage)
    {
        if (results.Count == 0)
        {
            writer.WriteLine(emptyMessage);
            return;
        }

        foreach (var result in results)
        {
            var title = result.FullyQualifiedName ?? result.SymbolName ?? result.Path;
            writer.WriteLine($"[{result.Kind}] {title} {result.Path}:{result.Line}:{result.Column}");
            if (!string.IsNullOrWhiteSpace(result.ReferenceKind))
            {
                writer.WriteLine($"  ref-kind={result.ReferenceKind}");
            }

            if (!string.IsNullOrWhiteSpace(result.Snippet))
            {
                writer.WriteLine($"  {result.Snippet}");
            }
        }
    }
}

public sealed class IndexBuilder
{
    public static MSBuildWorkspace CreateWorkspace()
    {
        var workspace = MSBuildWorkspace.Create();
        var property = typeof(MSBuildWorkspace).GetProperty("LoadMetadataForReferencedProjects");
        if (property is { CanWrite: true } && property.PropertyType == typeof(bool))
        {
            property.SetValue(workspace, true);
        }

        return workspace;
    }

    public static Project RemoveAnalyzerReferences(Project project)
    {
        foreach (var reference in project.AnalyzerReferences.ToArray())
        {
            project = project.RemoveAnalyzerReference(reference);
        }

        return project;
    }

    public static Solution RemoveAnalyzerReferences(Solution solution)
    {
        foreach (var project in solution.Projects.ToArray())
        {
            foreach (var reference in project.AnalyzerReferences.ToArray())
            {
                solution = solution.RemoveAnalyzerReference(project.Id, reference);
            }
        }

        return solution;
    }

    public async Task<IndexSummary> BuildAsync(string startPath, bool force, IndexerConfig config, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var discoveryStopwatch = Stopwatch.StartNew();
        var root = RepositoryDiscovery.FindRoot(startPath);
        var repoRoot = root.RootPath;
        var diagnostics = new DiagnosticsCollector();
        var workspaceInputs = WorkspaceDiscovery.Discover(repoRoot, config);
        var workspaceHash = ComputeWorkspaceInputsHash(repoRoot, config, workspaceInputs);
        var configHash = ConfigLoader.ComputeHash(config);
        if (workspaceInputs.Count == 0)
        {
            throw new WorkspaceLoadingException("No workspace inputs could be discovered. Check configured solution/project path.");
        }

        var oldSnapshot = LoadReusableSnapshot(repoRoot, force, configHash, workspaceHash);
        var incremental = oldSnapshot is not null;
        var stats = new IndexRunStats();
        discoveryStopwatch.Stop();
        long workspaceLoadMs = 0;
        long semanticIndexMs = 0;
        long textIndexMs = 0;

        MSBuildRegistration.RegisterDefaults();
        using var workspace = IndexBuilder.CreateWorkspace();
#pragma warning disable CS0618
        workspace.WorkspaceFailed += (_, e) => diagnostics.Warn(e.Diagnostic.Message);
#pragma warning restore CS0618

        var documents = new List<DocumentEntry>();
        var symbols = new List<SymbolEntry>();
        var references = new List<ReferenceEntry>();
        var tokens = new List<TokenPosting>();
        var projects = new List<ProjectEntry>();
        var fullTextIndexedPaths = new HashSet<string>(StringComparer.Ordinal);

        foreach (var input in workspaceInputs)
        {
            try
            {
                if (input.Kind.Equals("csproj", StringComparison.OrdinalIgnoreCase))
                {
                    var workspaceLoadStart = Stopwatch.GetTimestamp();
                    var project = await workspace.OpenProjectAsync(input.Path, cancellationToken: cancellationToken).ConfigureAwait(false);
                    project = RemoveAnalyzerReferences(project);
                    workspaceLoadMs += (long)Stopwatch.GetElapsedTime(workspaceLoadStart).TotalMilliseconds;
                    var semanticDirtyPlan = await CreateSemanticDirtyPlanAsync(repoRoot, new[] { project }, config, oldSnapshot, cancellationToken).ConfigureAwait(false);
                    var semanticIndexStart = Stopwatch.GetTimestamp();
                    await IndexProjectAsync(repoRoot, project, config, oldSnapshot, semanticDirtyPlan, documents, symbols, references, tokens, projects, stats, fullTextIndexedPaths, cancellationToken).ConfigureAwait(false);
                    semanticIndexMs += (long)Stopwatch.GetElapsedTime(semanticIndexStart).TotalMilliseconds;
                }
                else
                {
                    var workspaceLoadStart = Stopwatch.GetTimestamp();
                    var solution = await workspace.OpenSolutionAsync(input.Path, cancellationToken: cancellationToken).ConfigureAwait(false);
                    solution = RemoveAnalyzerReferences(solution);
                    workspaceLoadMs += (long)Stopwatch.GetElapsedTime(workspaceLoadStart).TotalMilliseconds;
                    var csharpProjects = solution.Projects.Where(p => p.Language == LanguageNames.CSharp).OrderBy(p => p.Name, StringComparer.Ordinal).ToArray();
                    var semanticDirtyPlan = await CreateSemanticDirtyPlanAsync(repoRoot, csharpProjects, config, oldSnapshot, cancellationToken).ConfigureAwait(false);
                    foreach (var project in csharpProjects)
                    {
                        var semanticIndexStart = Stopwatch.GetTimestamp();
                        await IndexProjectAsync(repoRoot, project, config, oldSnapshot, semanticDirtyPlan, documents, symbols, references, tokens, projects, stats, fullTextIndexedPaths, cancellationToken).ConfigureAwait(false);
                        semanticIndexMs += (long)Stopwatch.GetElapsedTime(semanticIndexStart).TotalMilliseconds;
                    }
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or IOException or NotSupportedException)
            {
                diagnostics.Warn($"Failed to load workspace input '{input.Path}': {ex.Message}");
            }
        }

        if (documents.Count == 0 && workspaceInputs.Count > 0 && oldSnapshot is null)
        {
            throw new NoCSharpDocumentsException("No C# documents could be loaded from workspace inputs.");
        }

        if (config.IncludeNonCSharpText)
        {
            var textIndexStart = Stopwatch.GetTimestamp();
            IndexTextFiles(repoRoot, config, oldSnapshot, documents, tokens, diagnostics, stats);
            textIndexMs += (long)Stopwatch.GetElapsedTime(textIndexStart).TotalMilliseconds;
        }

        var currentPaths = documents.Select(d => d.RelativePath).ToHashSet(StringComparer.Ordinal);
        var deletedDocuments = oldSnapshot?.Manifest.DocumentsByRelativePath.Keys.Count(path => !currentPaths.Contains(path)) ?? 0;

        var states = documents
            .GroupBy(d => d.RelativePath, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var first = g.First();
                    var fullPath = Path.Combine(repoRoot, first.RelativePath);
                    var lastWrite = File.Exists(fullPath) ? File.GetLastWriteTimeUtc(fullPath) : DateTime.UtcNow;
                    return new DocumentState(first.ContentHash, first.LengthBytes, new DateTimeOffset(lastWrite, TimeSpan.Zero), first.IsCSharp);
                },
                StringComparer.Ordinal);

        var manifest = IndexManifest.CreateNew(repoRoot, configHash, workspaceHash) with
        {
            WorkspaceInputs = ToPersistedWorkspaceInputs(repoRoot, workspaceInputs),
            DocumentsByRelativePath = states,
            DocumentCount = documents.Count,
            SymbolCount = symbols.Count,
            ReferenceCount = references.Count,
            TokenCount = tokens.Count,
            WarningCount = diagnostics.Warnings.Count,
            RecentWarnings = diagnostics.Warnings.TakeLast(20).ToArray(),
            Timings = new IndexTimingSummary(discoveryStopwatch.ElapsedMilliseconds, workspaceLoadMs, semanticIndexMs, textIndexMs, 0, stopwatch.ElapsedMilliseconds)
        };

        var snapshot = new IndexSnapshot(
            manifest,
            documents.OrderBy(d => d.RelativePath, StringComparer.Ordinal).ThenBy(d => d.ProjectName, StringComparer.Ordinal).ToArray(),
            symbols.OrderBy(s => s.Path, StringComparer.Ordinal).ThenBy(s => s.Line).ThenBy(s => s.Column).ThenBy(s => s.SymbolId, StringComparer.Ordinal).ToArray(),
            references.OrderBy(r => r.Path, StringComparer.Ordinal).ThenBy(r => r.Line).ThenBy(r => r.Column).ThenBy(r => r.SymbolId, StringComparer.Ordinal).ToArray(),
            tokens.OrderBy(t => t.Token, StringComparer.Ordinal).ThenBy(t => t.Path, StringComparer.Ordinal).ThenBy(t => t.Line).ThenBy(t => t.Column).ToArray());

        var timings = IndexStore.Write(repoRoot, snapshot);
        stopwatch.Stop();
        return new IndexSummary(
            repoRoot,
            snapshot.Documents.Count,
            snapshot.Symbols.Count,
            snapshot.References.Count,
            snapshot.Tokens.Count,
            diagnostics.Warnings.Count,
            stopwatch.Elapsed,
            !incremental,
            incremental,
            stats.DirtyDocuments,
            deletedDocuments,
            stats.UnchangedDocuments,
            timings);
    }

    private static async Task<SemanticDirtyPlan> CreateSemanticDirtyPlanAsync(
        string repoRoot,
        IReadOnlyCollection<Project> projects,
        IndexerConfig config,
        IndexSnapshot? oldSnapshot,
        CancellationToken cancellationToken)
    {
        if (oldSnapshot is null || projects.Count == 0)
        {
            return SemanticDirtyPlan.Empty;
        }

        var changedDeclarationProjects = new HashSet<ProjectId>();
        var csharpDocumentCount = 0;
        var dirtyCSharpDocumentCount = 0;

        foreach (var project in projects)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            foreach (var document in project.Documents)
            {
                if (!TryGetIndexableRelativePath(repoRoot, document.FilePath, config, out var relative))
                {
                    continue;
                }

                csharpDocumentCount++;
                if (!IsChangedComparedToManifest(repoRoot, relative, oldSnapshot.Manifest))
                {
                    continue;
                }

                dirtyCSharpDocumentCount++;
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var declarationHash = root is not null && semanticModel is not null
                    ? DeclarationHash(root, semanticModel, cancellationToken)
                    : DeclarationHash(text.ToString());
                var previousHashes = oldSnapshot.Documents
                    .Where(d => string.Equals(d.RelativePath, relative, StringComparison.Ordinal)
                                && string.Equals(d.ProjectName, project.Name, StringComparison.Ordinal))
                    .Select(d => d.DeclarationHash)
                    .ToArray();
                if (previousHashes.Length > 0 && previousHashes.Any(previous => !string.Equals(previous, declarationHash, StringComparison.Ordinal)))
                {
                    changedDeclarationProjects.Add(project.Id);
                }
            }
        }

        if (csharpDocumentCount >= 5 && dirtyCSharpDocumentCount * 100 > csharpDocumentCount * 20)
        {
            return SemanticDirtyPlan.ForceAll;
        }

        if (changedDeclarationProjects.Count == 0)
        {
            return SemanticDirtyPlan.Empty;
        }

        var projectNames = projects
            .Where(project => changedDeclarationProjects.Contains(project.Id)
                              || project.ProjectReferences.Any(reference => changedDeclarationProjects.Contains(reference.ProjectId)))
            .Select(project => project.Name)
            .ToHashSet(StringComparer.Ordinal);
        return new SemanticDirtyPlan(false, projectNames);
    }

    private static bool TryGetIndexableRelativePath(string repoRoot, string? filePath, IndexerConfig config, out string relative)
    {
        relative = string.Empty;
        if (filePath is null || !File.Exists(filePath))
        {
            return false;
        }

        relative = RepositoryDiscovery.NormalizeRelative(Path.GetRelativePath(repoRoot, filePath));
        return !relative.StartsWith("../", StringComparison.Ordinal)
               && !RepositoryDiscovery.IsExcluded(relative, config)
               && (config.IncludeGenerated || !IsGenerated(relative));
    }

    private static bool IsChangedComparedToManifest(string repoRoot, string relative, IndexManifest manifest)
    {
        if (!manifest.DocumentsByRelativePath.TryGetValue(relative, out var state))
        {
            return true;
        }

        var fullPath = Path.Combine(repoRoot, relative);
        if (!File.Exists(fullPath))
        {
            return true;
        }

        var info = new FileInfo(fullPath);
        var lastWrite = new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero);
        return info.Length != state.Length || lastWrite != state.LastWriteUtc;
    }

    private static async Task IndexProjectAsync(
        string repoRoot,
        Project project,
        IndexerConfig config,
        IndexSnapshot? oldSnapshot,
        SemanticDirtyPlan semanticDirtyPlan,
        List<DocumentEntry> documents,
        List<SymbolEntry> symbols,
        List<ReferenceEntry> references,
        List<TokenPosting> tokens,
        List<ProjectEntry> projects,
        IndexRunStats stats,
        HashSet<string> fullTextIndexedPaths,
        CancellationToken cancellationToken)
    {
        var projectId = StableProjectId(repoRoot, project);
        projects.Add(new ProjectEntry(projectId, project.Name, project.FilePath, project.Language, project.ParseOptions?.DocumentationMode.ToString()));
        var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        var documentBatches = new List<DocumentIndexBatch>();
        var documentGate = new object();
        await Parallel.ForEachAsync(
            project.Documents.OrderBy(d => d.FilePath, StringComparer.Ordinal),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, config.MaxDegreeOfParallelism),
                CancellationToken = cancellationToken
            },
            async (document, ct) =>
            {
                var batch = await IndexDocumentAsync(repoRoot, project, projectId, document, compilation, config, oldSnapshot, semanticDirtyPlan, fullTextIndexedPaths, ct).ConfigureAwait(false);
                if (batch is null)
                {
                    return;
                }

                lock (documentGate)
                {
                    documentBatches.Add(batch);
                }
            }).ConfigureAwait(false);

        foreach (var batch in documentBatches
                     .OrderBy(batch => batch.RelativePath, StringComparer.Ordinal)
                     .ThenBy(batch => batch.ProjectName, StringComparer.Ordinal))
        {
            documents.AddRange(batch.Documents);
            symbols.AddRange(batch.Symbols);
            references.AddRange(batch.References);
            tokens.AddRange(batch.Tokens);
            stats.DirtyDocuments += batch.DirtyDocuments;
            stats.UnchangedDocuments += batch.UnchangedDocuments;
        }

        if (!config.IncludeGenerated)
        {
            return;
        }

        var generatedDocuments = await project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false);
        foreach (var document in generatedDocuments.OrderBy(d => d.Name, StringComparer.Ordinal))
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var source = text.ToString();
            if (source.Length == 0)
            {
                continue;
            }

            var relative = "generated/" + RepositoryDiscovery.NormalizeRelative(document.Name);
            if (RepositoryDiscovery.IsExcluded(relative, config) || !IsGenerated(relative))
            {
                continue;
            }

            var documentId = StableId(relative + "|" + project.Name);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var declarationHash = root is not null && semanticModel is not null
                ? DeclarationHash(root, semanticModel, cancellationToken)
                : DeclarationHash(source);
            documents.Add(new DocumentEntry(
                documentId,
                projectId,
                relative,
                project.Name,
                "C#",
                true,
                true,
                false,
                Encoding.UTF8.GetByteCount(source),
                DateTimeOffset.UtcNow,
                DocumentHasher.HashText(source),
                declarationHash,
                CountLines(source)));

            if (fullTextIndexedPaths.Add(relative))
            {
                AddCSharpTokens(tokens, source, root, relative, project.Name, documentId);
                AddPathTokens(tokens, relative, project.Name, documentId);
            }

            if (root is not null && semanticModel is not null)
            {
                IndexSyntaxTree(root, semanticModel, compilation, documentId, projectId, relative, project.Name, symbols, references, tokens, cancellationToken);
            }
        }
    }

    private static async Task<DocumentIndexBatch?> IndexDocumentAsync(
        string repoRoot,
        Project project,
        string projectId,
        Document document,
        Compilation? compilation,
        IndexerConfig config,
        IndexSnapshot? oldSnapshot,
        SemanticDirtyPlan semanticDirtyPlan,
        HashSet<string> fullTextIndexedPaths,
        CancellationToken cancellationToken)
    {
        var documents = new List<DocumentEntry>();
        var symbols = new List<SymbolEntry>();
        var references = new List<ReferenceEntry>();
        var tokens = new List<TokenPosting>();

        if (document.FilePath is null || !File.Exists(document.FilePath))
        {
            return null;
        }

        var relative = RepositoryDiscovery.NormalizeRelative(Path.GetRelativePath(repoRoot, document.FilePath));
        if (relative.StartsWith("../", StringComparison.Ordinal))
        {
            return null;
        }

        if (RepositoryDiscovery.IsExcluded(relative, config))
        {
            return null;
        }

        if (!config.IncludeGenerated && IsGenerated(relative))
        {
            return null;
        }

        var forceSemanticReindex = semanticDirtyPlan.ForceAllCSharpDocuments || semanticDirtyPlan.ProjectNames.Contains(project.Name);
        if (!forceSemanticReindex && TryReuseUnchangedDocument(repoRoot, relative, project.Name, oldSnapshot, documents, symbols, references, tokens))
        {
            return new DocumentIndexBatch(relative, project.Name, documents, symbols, references, tokens, DirtyDocuments: 0, UnchangedDocuments: 1);
        }

        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var source = text.ToString();
        var hash = DocumentHasher.HashText(source);
        var documentId = StableId(relative + "|" + project.Name);
        var fileInfo = new FileInfo(document.FilePath);
        var lastWriteUtc = new DateTimeOffset(fileInfo.LastWriteTimeUtc, TimeSpan.Zero);
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var declarationHash = root is not null && semanticModel is not null
            ? DeclarationHash(root, semanticModel, cancellationToken)
            : DeclarationHash(source);
        documents.Add(new DocumentEntry(documentId, projectId, relative, project.Name, "C#", true, IsGenerated(relative), false, fileInfo.Length, lastWriteUtc, hash, declarationHash, CountLines(source)));

        var shouldIndexFullText = false;
        lock (fullTextIndexedPaths)
        {
            if (fullTextIndexedPaths.Add(relative))
            {
                shouldIndexFullText = true;
            }
        }

        if (shouldIndexFullText)
        {
            AddCSharpTokens(tokens, source, root, relative, project.Name, documentId);
            AddPathTokens(tokens, relative, project.Name, documentId);
        }

        if (root is not null && semanticModel is not null)
        {
            IndexSyntaxTree(root, semanticModel, compilation, documentId, projectId, relative, project.Name, symbols, references, tokens, cancellationToken);
        }

        return new DocumentIndexBatch(relative, project.Name, documents, symbols, references, tokens, DirtyDocuments: 1, UnchangedDocuments: 0);
    }

    private static void IndexSyntaxTree(
        SyntaxNode root,
        SemanticModel semanticModel,
        Compilation? compilation,
        string documentId,
        string projectId,
        string relative,
        string projectName,
        List<SymbolEntry> symbols,
        List<ReferenceEntry> references,
        List<TokenPosting> tokens,
        CancellationToken cancellationToken)
    {
        var localSymbolIds = new Dictionary<ISymbol, string>(SymbolEqualityComparer.Default);

        foreach (var node in IndexableDeclarationNodes(root))
        {
            var declared = semanticModel.GetDeclaredSymbol(node, cancellationToken);
            if (declared is not null && IsIndexableDeclaration(declared))
            {
                var symbolEntry = ToSymbolEntry(declared, node.GetLocation(), documentId, projectId, relative, projectName);
                symbols.Add(symbolEntry);
                AddLocalSymbolId(localSymbolIds, declared, symbolEntry.SymbolId);
                foreach (var token in Tokenizer.Tokenize(declared.Name, includeCodeSingleCharacterTokens: true))
                {
                    tokens.Add(new TokenPosting(token.Value, relative, node.GetLocation().GetLineSpan().StartLinePosition.Line + 1, node.GetLocation().GetLineSpan().StartLinePosition.Character + 1, "symbol", "symbol-name", projectName, documentId));
                }
            }
        }

        var seenReferences = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in root.DescendantNodesAndSelf())
        {
            var declared = IsDeclarationSyntax(node) ? semanticModel.GetDeclaredSymbol(node, cancellationToken) : null;
            var referenced = ReferencedSymbol(semanticModel, node, cancellationToken);
            if (referenced is not null
                && compilation is not null
                && !SymbolEqualityComparer.Default.Equals(referenced, declared)
                && TryGetLocalSymbolId(localSymbolIds, referenced, out var symbolId))
            {
                var sourceSpan = node.GetLocation().SourceSpan;
                if (seenReferences.Add($"{symbolId}|{documentId}|{sourceSpan.Start}|{sourceSpan.Length}"))
                {
                    references.Add(ToReferenceEntry(referenced, symbolId, node.GetLocation(), documentId, projectId, relative, projectName, node));
                }
            }
        }
    }

    private static void IndexTextFiles(
        string repoRoot,
        IndexerConfig config,
        IndexSnapshot? oldSnapshot,
        List<DocumentEntry> documents,
        List<TokenPosting> tokens,
        DiagnosticsCollector diagnostics,
        IndexRunStats stats)
    {
        foreach (var file in RepositoryDiscovery.EnumerateCandidateFiles(repoRoot, config))
        {
            if (file.RelativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                if (file.Length > config.MaxTextFileBytes)
                {
                    diagnostics.Warn($"Skipped text file '{file.RelativePath}': length {file.Length} exceeds maxTextFileBytes {config.MaxTextFileBytes}.");
                    continue;
                }

                if (BinaryFileDetector.IsBinaryFile(file.FullPath))
                {
                    continue;
                }

                if (TryReuseUnchangedDocument(repoRoot, file.RelativePath, projectName: null, oldSnapshot, documents, symbols: null, references: null, tokens))
                {
                    stats.UnchangedDocuments++;
                    continue;
                }

                stats.DirtyDocuments++;

                var text = File.ReadAllText(file.FullPath);
                var hash = DocumentHasher.HashText(text);
                var documentId = StableId(file.RelativePath);
                var fileInfo = new FileInfo(file.FullPath);
                documents.Add(new DocumentEntry(documentId, null, file.RelativePath, null, "text", false, false, true, file.Length, new DateTimeOffset(fileInfo.LastWriteTimeUtc, TimeSpan.Zero), hash, string.Empty, CountLines(text)));
                AddTextFileTokens(tokens, file.FullPath, file.RelativePath, documentId);
                AddPathTokens(tokens, file.RelativePath, projectName: null, documentId);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                diagnostics.Warn($"Skipped text file '{file.RelativePath}': {ex.Message}");
            }
        }
    }

    private static void AddPathTokens(List<TokenPosting> tokens, string relativePath, string? projectName, string documentId)
    {
        tokens.AddRange(Tokenizer.TokenizePath(relativePath)
            .Select(t => new TokenPosting(t.Value, relativePath, 1, 1, "path", "path", projectName, documentId)));
    }

    private static void AddCSharpTokens(List<TokenPosting> tokens, string source, SyntaxNode? root, string relativePath, string? projectName, string documentId)
    {
        if (root is null)
        {
            tokens.AddRange(Tokenizer.Tokenize(source, includeCodeSingleCharacterTokens: true)
                .Select(t => new TokenPosting(t.Value, relativePath, t.Line, t.Column, "csharp", "identifier", projectName, documentId)));
            return;
        }

        foreach (var syntaxToken in root.DescendantTokens(descendIntoTrivia: true))
        {
            if (syntaxToken.IsKind(SyntaxKind.IdentifierToken))
            {
                AddTokenPostings(tokens, syntaxToken.Text, syntaxToken.GetLocation(), relativePath, "identifier", projectName, documentId);
            }
            else if (SyntaxFacts.IsKeywordKind(syntaxToken.Kind()))
            {
                AddTokenPostings(tokens, syntaxToken.Text, syntaxToken.GetLocation(), relativePath, "keyword", projectName, documentId);
            }
            else if (syntaxToken.IsKind(SyntaxKind.StringLiteralToken)
                     || syntaxToken.IsKind(SyntaxKind.Utf8StringLiteralToken)
                     || syntaxToken.IsKind(SyntaxKind.InterpolatedStringTextToken))
            {
                AddTokenPostings(tokens, syntaxToken.ValueText.Length == 0 ? syntaxToken.Text : syntaxToken.ValueText, syntaxToken.GetLocation(), relativePath, "string", projectName, documentId);
            }

            foreach (var trivia in syntaxToken.LeadingTrivia.Concat(syntaxToken.TrailingTrivia))
            {
                if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                    || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
                    || trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                    || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                {
                    AddTokenPostings(tokens, trivia.ToString(), trivia.GetLocation(), relativePath, "comment", projectName, documentId);
                }
            }
        }
    }

    private static void AddTokenPostings(List<TokenPosting> tokens, string text, Location location, string relativePath, string weight, string? projectName, string documentId)
    {
        var span = location.GetLineSpan().StartLinePosition;
        foreach (var token in Tokenizer.Tokenize(text, includeCodeSingleCharacterTokens: true))
        {
            tokens.Add(new TokenPosting(token.Value, relativePath, span.Line + token.Line, token.Line == 1 ? span.Character + token.Column : token.Column, "csharp", weight, projectName, documentId));
        }
    }

    private static void AddTextFileTokens(List<TokenPosting> tokens, string fullPath, string relativePath, string documentId)
    {
        var lineNumber = 0;
        foreach (var line in File.ReadLines(fullPath))
        {
            lineNumber++;
            tokens.AddRange(Tokenizer.Tokenize(line)
                .Select(t => new TokenPosting(t.Value, relativePath, lineNumber, t.Column, "text", "text", null, documentId)));
        }
    }

    private static IndexSnapshot? LoadReusableSnapshot(string repoRoot, bool force, string configHash, string workspaceHash)
    {
        if (force || !IndexStore.Exists(repoRoot))
        {
            return null;
        }

        try
        {
            var snapshot = IndexStore.Read(repoRoot);
            return snapshot.Manifest.SchemaVersion == IndexManifest.CurrentSchemaVersion
                   && string.Equals(snapshot.Manifest.ConfigHash, configHash, StringComparison.Ordinal)
                   && string.Equals(snapshot.Manifest.WorkspaceInputsHash, workspaceHash, StringComparison.Ordinal)
                ? snapshot
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static string ComputeWorkspaceInputsHash(string repoRoot, IndexerConfig config, IReadOnlyList<WorkspaceInput> workspaceInputs)
    {
        var paths = workspaceInputs
            .Select(input => input.Path)
            .Concat(EnumerateWorkspaceTriggerFiles(repoRoot, config))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => RepositoryDiscovery.NormalizeRelative(Path.GetRelativePath(repoRoot, path)), StringComparer.Ordinal)
            .Select(path =>
            {
                var relative = RepositoryDiscovery.NormalizeRelative(Path.GetRelativePath(repoRoot, path));
                var contentHash = File.Exists(path) ? DocumentHasher.HashFile(path) : string.Empty;
                return $"{relative}|{contentHash}";
            });

        return ConfigLoader.HashText(string.Join('\n', paths));
    }

    private static IEnumerable<string> EnumerateWorkspaceTriggerFiles(string repoRoot, IndexerConfig config)
    {
        return Directory.EnumerateFiles(repoRoot, "*", SearchOption.AllDirectories)
            .Where(path =>
            {
                var relative = RepositoryDiscovery.NormalizeRelative(Path.GetRelativePath(repoRoot, path));
                return !RepositoryDiscovery.IsExcluded(relative, config) && IsWorkspaceTriggerFile(path);
            });
    }

    private static bool IsWorkspaceTriggerFile(string path)
    {
        var fileName = Path.GetFileName(path);
        var extension = Path.GetExtension(path);
        return extension.Equals(".sln", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".props", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".targets", StringComparison.OrdinalIgnoreCase)
               || fileName.Equals("global.json", StringComparison.OrdinalIgnoreCase)
               || fileName.Equals("NuGet.config", StringComparison.OrdinalIgnoreCase)
               || fileName.Equals("packages.lock.json", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<WorkspaceInput> ToPersistedWorkspaceInputs(string repoRoot, IReadOnlyList<WorkspaceInput> workspaceInputs)
        => workspaceInputs
            .Select(input => input with { Path = RepositoryDiscovery.NormalizeRelative(Path.GetRelativePath(repoRoot, input.Path)) })
            .ToArray();

    private static string StableProjectId(string repoRoot, Project project)
    {
        var path = project.FilePath is null
            ? project.Name
            : RepositoryDiscovery.NormalizeRelative(Path.GetRelativePath(repoRoot, project.FilePath));
        return StableId(project.Name + "|" + path);
    }

    private static bool TryReuseUnchangedDocument(
        string repoRoot,
        string relativePath,
        string? projectName,
        IndexSnapshot? oldSnapshot,
        List<DocumentEntry> documents,
        List<SymbolEntry>? symbols,
        List<ReferenceEntry>? references,
        List<TokenPosting> tokens)
    {
        if (oldSnapshot is null || !oldSnapshot.Manifest.DocumentsByRelativePath.TryGetValue(relativePath, out var state))
        {
            return false;
        }

        var fullPath = Path.Combine(repoRoot, relativePath);
        if (!File.Exists(fullPath))
        {
            return false;
        }

        var info = new FileInfo(fullPath);
        var lastWrite = new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero);
        if (info.Length != state.Length || lastWrite != state.LastWriteUtc)
        {
            return false;
        }

        documents.AddRange(oldSnapshot.Documents.Where(d => string.Equals(d.RelativePath, relativePath, StringComparison.Ordinal)
                                                             && string.Equals(d.ProjectName, projectName, StringComparison.Ordinal)));
        symbols?.AddRange(oldSnapshot.Symbols.Where(s => string.Equals(s.Path, relativePath, StringComparison.Ordinal)
                                                          && string.Equals(s.ProjectName, projectName, StringComparison.Ordinal)));
        references?.AddRange(oldSnapshot.References.Where(r => string.Equals(r.Path, relativePath, StringComparison.Ordinal)
                                                               && string.Equals(r.ProjectName, projectName, StringComparison.Ordinal)));
        tokens.AddRange(oldSnapshot.Tokens.Where(t => string.Equals(t.Path, relativePath, StringComparison.Ordinal)
                                                       && string.Equals(t.ProjectName, projectName, StringComparison.Ordinal)));
        return true;
    }

    public static SymbolEntry ToSymbolEntry(ISymbol symbol, Location location, string documentId, string? projectId, string relativePath, string? projectName)
    {
        var lineSpan = location.GetLineSpan();
        var span = lineSpan.StartLinePosition;
        var end = lineSpan.EndLinePosition;
        var sourceSpan = location.SourceSpan;
        var name = symbol is IMethodSymbol { MethodKind: MethodKind.Constructor, ContainingType: not null } constructor
            ? constructor.ContainingType.Name
            : symbol.Name.Length == 0 ? symbol.Kind.ToString() : symbol.Name;
        var identity = RoslynRepoIndexer.Core.SymbolIdProvider.CreateIdentity(symbol, projectId, relativePath, cancellationToken: default);
        var fqn = DisplayName(symbol, SymbolDisplayFormat.FullyQualifiedFormat);
        var signature = DisplayName(symbol, SymbolDisplayFormat.CSharpErrorMessageFormat);
        return new SymbolEntry(
            identity.SymbolId,
            documentId,
            projectId,
            MapKind(symbol),
            name,
            symbol.MetadataName,
            fqn,
            ContainerName(symbol),
            signature,
            AccessibilityName(symbol),
            Modifiers(symbol),
            relativePath,
            span.Line + 1,
            span.Character + 1,
            end.Line + 1,
            end.Character + 1,
            sourceSpan.Start,
            sourceSpan.Length,
            true,
            IsPartial(symbol),
            ParameterTypes(symbol),
            ReturnType(symbol),
            projectName,
            identity.SymbolKey);
    }

    public static ReferenceEntry ToReferenceEntry(ISymbol symbol, string symbolId, Location location, string documentId, string? projectId, string relativePath, string? projectName, SyntaxNode node)
    {
        var lineSpan = location.GetLineSpan();
        var span = lineSpan.StartLinePosition;
        var end = lineSpan.EndLinePosition;
        var sourceSpan = location.SourceSpan;
        return new ReferenceEntry(
            StableId(symbolId + "|" + documentId + "|" + sourceSpan.Start + "|" + sourceSpan.Length),
            symbolId,
            documentId,
            projectId,
            symbol.Name,
            relativePath,
            span.Line + 1,
            span.Character + 1,
            end.Line + 1,
            end.Character + 1,
            sourceSpan.Start,
            sourceSpan.Length,
            projectName,
            ReferenceKind(node, symbol));
    }

    public static bool IsIndexableDeclaration(ISymbol symbol)
        => symbol.Kind is SymbolKind.NamedType or SymbolKind.Method or SymbolKind.Property or SymbolKind.Field or SymbolKind.Event or SymbolKind.Parameter or SymbolKind.Local or SymbolKind.TypeParameter or SymbolKind.Namespace;

    public static IEnumerable<SyntaxNode> IndexableDeclarationNodes(SyntaxNode root)
        => root.DescendantNodesAndSelf().Where(IsDeclarationSyntax);

    public static bool IsDeclarationSyntax(SyntaxNode node)
        => node is BaseNamespaceDeclarationSyntax
            or TypeDeclarationSyntax
            or EnumDeclarationSyntax
            or DelegateDeclarationSyntax
            or ConstructorDeclarationSyntax
            or DestructorDeclarationSyntax
            or MethodDeclarationSyntax
            or LocalFunctionStatementSyntax
            or OperatorDeclarationSyntax
            or ConversionOperatorDeclarationSyntax
            or PropertyDeclarationSyntax
            or IndexerDeclarationSyntax
            or EventDeclarationSyntax
            or EventFieldDeclarationSyntax
            or FieldDeclarationSyntax
            or EnumMemberDeclarationSyntax
            or ParameterSyntax
            or VariableDeclaratorSyntax
            or TypeParameterSyntax;

    private static string? ContainerName(ISymbol symbol)
    {
        if (symbol.ContainingType is not null)
        {
            return DisplayName(symbol.ContainingType, SymbolDisplayFormat.CSharpErrorMessageFormat);
        }

        if (symbol.ContainingNamespace is { IsGlobalNamespace: false } ns)
        {
            return DisplayName(ns, SymbolDisplayFormat.CSharpErrorMessageFormat);
        }

        return null;
    }

    internal static string AccessibilityName(ISymbol symbol)
        => symbol.DeclaredAccessibility.ToString().ToLowerInvariant();

    internal static IReadOnlyList<string> Modifiers(ISymbol symbol)
    {
        var modifiers = new List<string>();
        if (symbol.IsStatic) modifiers.Add("static");
        if (symbol.IsAbstract) modifiers.Add("abstract");
        if (symbol.IsVirtual) modifiers.Add("virtual");
        if (symbol.IsOverride) modifiers.Add("override");
        if (symbol.IsSealed) modifiers.Add("sealed");
        if (symbol is IMethodSymbol { IsAsync: true }) modifiers.Add("async");
        if (symbol is IMethodSymbol { IsExtensionMethod: true }) modifiers.Add("extension");
        if (symbol is IFieldSymbol { IsReadOnly: true }) modifiers.Add("readonly");
        if (symbol is IPropertySymbol { IsRequired: true }) modifiers.Add("required");
        if (IsPartial(symbol)) modifiers.Add("partial");
        return modifiers;
    }

    private static bool IsPartial(ISymbol symbol)
        => symbol.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .Any(syntax => syntax.ChildTokens().Any(token => token.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)));

    private static IReadOnlyList<string> ParameterTypes(ISymbol symbol)
        => symbol switch
        {
            INamedTypeSymbol { TypeKind: TypeKind.Delegate, DelegateInvokeMethod: not null } namedType => namedType.DelegateInvokeMethod.Parameters.Select(p => DisplayName(p.Type, SymbolDisplayFormat.MinimallyQualifiedFormat)).ToArray(),
            IMethodSymbol method => method.Parameters.Select(p => DisplayName(p.Type, SymbolDisplayFormat.MinimallyQualifiedFormat)).ToArray(),
            IPropertySymbol { IsIndexer: true } property => property.Parameters.Select(p => DisplayName(p.Type, SymbolDisplayFormat.MinimallyQualifiedFormat)).ToArray(),
            _ => Array.Empty<string>()
        };

    private static string? ReturnType(ISymbol symbol)
        => symbol switch
        {
            INamedTypeSymbol { TypeKind: TypeKind.Delegate, DelegateInvokeMethod: not null } namedType => DisplayName(namedType.DelegateInvokeMethod.ReturnType, SymbolDisplayFormat.MinimallyQualifiedFormat),
            IMethodSymbol method => DisplayName(method.ReturnType, SymbolDisplayFormat.MinimallyQualifiedFormat),
            IPropertySymbol property => DisplayName(property.Type, SymbolDisplayFormat.MinimallyQualifiedFormat),
            _ => null
        };

    public static ISymbol? ReferencedSymbol(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
    {
        var symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken);
        var symbol = symbolInfo.Symbol ?? (symbolInfo.CandidateSymbols.Length == 1 ? symbolInfo.CandidateSymbols[0] : null);
        var referenceSymbol = symbol ?? semanticModel.GetDeclaredSymbol(node, cancellationToken);
        if (referenceSymbol is null)
        {
            return null;
        }

        return ReferenceKind(node, referenceSymbol) switch
        {
            "attribute" when symbol is IMethodSymbol { MethodKind: MethodKind.Constructor, ContainingType: not null } method => method.ContainingType,
            "object-creation" when symbol is IMethodSymbol { MethodKind: MethodKind.Constructor, ContainingType: not null } method => method.ContainingType,
            _ => referenceSymbol
        };
    }

    private static void AddLocalSymbolId(Dictionary<ISymbol, string> localSymbolIds, ISymbol symbol, string symbolId)
    {
        localSymbolIds.TryAdd(symbol, symbolId);
        if (!SymbolEqualityComparer.Default.Equals(symbol, symbol.OriginalDefinition))
        {
            localSymbolIds.TryAdd(symbol.OriginalDefinition, symbolId);
        }
    }

    private static bool TryGetLocalSymbolId(Dictionary<ISymbol, string> localSymbolIds, ISymbol symbol, out string symbolId)
        => localSymbolIds.TryGetValue(symbol, out symbolId!)
           || (!SymbolEqualityComparer.Default.Equals(symbol, symbol.OriginalDefinition)
               && localSymbolIds.TryGetValue(symbol.OriginalDefinition, out symbolId!));

    private static string ReferenceKind(SyntaxNode node, ISymbol? symbol)
    {
        if (node is InvocationExpressionSyntax
            || node.Parent is InvocationExpressionSyntax invocationExpression && ContainsNode(invocationExpression.Expression, node))
        {
            return "invocation";
        }

        if (node is ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax
            || node.Ancestors().OfType<ObjectCreationExpressionSyntax>().Any(creation => ContainsNode(creation.Type, node)))
        {
            return "object-creation";
        }

        if (node is AttributeSyntax || node.Parent is AttributeSyntax || node.Ancestors().OfType<AttributeSyntax>().Any())
        {
            return "attribute";
        }

        if (node is BaseTypeSyntax || node.Parent is BaseListSyntax or BaseTypeSyntax || node.Ancestors().OfType<BaseTypeSyntax>().Any())
        {
            return "inheritance";
        }

        if (IsWriteReference(node))
        {
            return "write";
        }

        if (symbol?.Kind is SymbolKind.Field or SymbolKind.Property or SymbolKind.Local or SymbolKind.Parameter)
        {
            return "read";
        }

        return symbol?.Kind is SymbolKind.NamedType ? "type-use" : "unknown";
    }

    private static bool IsWriteReference(SyntaxNode node)
    {
        if (node.Parent is AssignmentExpressionSyntax assignment && ContainsNode(assignment.Left, node))
        {
            return true;
        }

        if (node.Parent is PrefixUnaryExpressionSyntax or PostfixUnaryExpressionSyntax)
        {
            return true;
        }

        return node.Parent is ArgumentSyntax { RefKindKeyword.RawKind: not 0 } argument
               && ContainsNode(argument.Expression, node);
    }

    private static bool ContainsNode(SyntaxNode container, SyntaxNode node)
        => container == node || container.DescendantNodesAndSelf().Contains(node);

    private static int CountLines(string text)
        => text.Length == 0 ? 0 : text.Count(c => c == '\n') + (text[^1] == '\n' ? 0 : 1);

    internal static string MapKind(ISymbol symbol)
        => symbol switch
        {
            INamedTypeSymbol { TypeKind: TypeKind.Class, IsRecord: true } => "record",
            INamedTypeSymbol { TypeKind: TypeKind.Struct, IsRecord: true } => "record",
            INamedTypeSymbol { TypeKind: TypeKind.Class } => "class",
            INamedTypeSymbol { TypeKind: TypeKind.Struct } => "struct",
            INamedTypeSymbol { TypeKind: TypeKind.Interface } => "interface",
            INamedTypeSymbol { TypeKind: TypeKind.Enum } => "enum",
            INamedTypeSymbol { TypeKind: TypeKind.Delegate } => "delegate",
            IMethodSymbol { MethodKind: MethodKind.Constructor } => "constructor",
            IMethodSymbol { MethodKind: MethodKind.Destructor } => "destructor",
            IMethodSymbol { MethodKind: MethodKind.LocalFunction } => "local-function",
            IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator } => "operator",
            IMethodSymbol { MethodKind: MethodKind.Conversion } => "operator",
            IMethodSymbol => "method",
            IPropertySymbol { IsIndexer: true } => "indexer",
            IPropertySymbol => "property",
            IFieldSymbol { ContainingType.TypeKind: TypeKind.Enum } => "enum-member",
            IFieldSymbol => "field",
            IEventSymbol => "event",
            IParameterSymbol => "parameter",
            ILocalSymbol => "local",
            ITypeParameterSymbol => "type-parameter",
            INamespaceSymbol => "namespace",
            _ => symbol.Kind.ToString().ToLowerInvariant()
        };

    private static bool IsGenerated(string relativePath)
        => relativePath.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
           || relativePath.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase)
           || relativePath.StartsWith("obj/", StringComparison.OrdinalIgnoreCase)
           || relativePath.Contains("/obj/", StringComparison.OrdinalIgnoreCase);

    private static string DeclarationHash(string source)
    {
        var lines = source.Split('\n').Where(line =>
        {
            var trimmed = line.TrimStart();
            return trimmed.StartsWith("class ", StringComparison.Ordinal)
                || trimmed.StartsWith("public ", StringComparison.Ordinal)
                || trimmed.StartsWith("private ", StringComparison.Ordinal)
                || trimmed.StartsWith("internal ", StringComparison.Ordinal)
                || trimmed.StartsWith("protected ", StringComparison.Ordinal)
                || trimmed.StartsWith("namespace ", StringComparison.Ordinal)
                || trimmed.StartsWith("record ", StringComparison.Ordinal)
                || trimmed.StartsWith("struct ", StringComparison.Ordinal)
                || trimmed.StartsWith("interface ", StringComparison.Ordinal);
        });
        return ConfigLoader.HashText(string.Join('\n', lines));
    }

    private static string DeclarationHash(SyntaxNode root, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var declarations = IndexableDeclarationNodes(root)
            .Select(node => semanticModel.GetDeclaredSymbol(node, cancellationToken))
            .Where(symbol => symbol is not null && IsHashableDeclaration(symbol))
            .Select(symbol => DeclarationHashInput(symbol!))
            .OrderBy(value => value, StringComparer.Ordinal);
        return ConfigLoader.HashText(string.Join('\n', declarations));
    }

    private static bool IsHashableDeclaration(ISymbol symbol)
        => symbol.Kind is SymbolKind.NamedType or SymbolKind.Method or SymbolKind.Property or SymbolKind.Field or SymbolKind.Event or SymbolKind.Namespace;

    private static string DeclarationHashInput(ISymbol symbol)
    {
        var fqn = DisplayName(symbol, SymbolDisplayFormat.FullyQualifiedFormat);
        var signature = DisplayName(symbol, SymbolDisplayFormat.CSharpErrorMessageFormat);
        var modifiers = string.Join(',', Modifiers(symbol).OrderBy(modifier => modifier, StringComparer.Ordinal));
        return string.Join('|', MapKind(symbol), fqn, signature, AccessibilityName(symbol), modifiers);
    }

    internal static string DisplayName(ISymbol symbol, SymbolDisplayFormat format)
        => symbol.ToDisplayString(format).Replace("global::", string.Empty, StringComparison.Ordinal);

    private static string StableId(string value)
        => ConfigLoader.HashText(value)[..16];

    private sealed class IndexRunStats
    {
        public int DirtyDocuments { get; set; }
        public int UnchangedDocuments { get; set; }
    }

    private sealed record DocumentIndexBatch(
        string RelativePath,
        string ProjectName,
        IReadOnlyList<DocumentEntry> Documents,
        IReadOnlyList<SymbolEntry> Symbols,
        IReadOnlyList<ReferenceEntry> References,
        IReadOnlyList<TokenPosting> Tokens,
        int DirtyDocuments,
        int UnchangedDocuments);

    private sealed record SemanticDirtyPlan(bool ForceAllCSharpDocuments, HashSet<string> ProjectNames)
    {
        public static SemanticDirtyPlan Empty { get; } = new(false, new HashSet<string>(StringComparer.Ordinal));
        public static SemanticDirtyPlan ForceAll { get; } = new(true, new HashSet<string>(StringComparer.Ordinal));
    }
}

public sealed class ExactReferenceService
{
    public async Task<IReadOnlyList<SearchResult>> FindExactAsync(string repoRoot, string symbolIdOrQuery, int timeoutSeconds, CancellationToken cancellationToken = default)
    {
        var snapshot = IndexStore.Read(repoRoot);
        var candidates = snapshot.Symbols
            .Where(s => string.Equals(s.SymbolId, symbolIdOrQuery, StringComparison.Ordinal) || s.Name.Contains(symbolIdOrQuery, StringComparison.OrdinalIgnoreCase) || s.FullyQualifiedName.Contains(symbolIdOrQuery, StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .ToArray();
        if (candidates.Length != 1)
        {
            return Array.Empty<SearchResult>();
        }

        var candidate = candidates[0];
        var cacheKey = candidate.SymbolId + "|" + symbolIdOrQuery;
        if (IndexStore.TryReadExactReferenceCache(repoRoot, cacheKey, snapshot.Manifest.UpdatedUtc, out var cachedResults))
        {
            return cachedResults;
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
        MSBuildRegistration.RegisterDefaults();
        using var workspace = IndexBuilder.CreateWorkspace();
        var input = snapshot.Manifest.WorkspaceInputs.FirstOrDefault();
        if (input is null)
        {
            return Array.Empty<SearchResult>();
        }

        var inputPath = WorkspaceInputPaths.Resolve(repoRoot, input.Path);
        var solution = input.Kind.Equals("csproj", StringComparison.OrdinalIgnoreCase)
            ? IndexBuilder.RemoveAnalyzerReferences(await workspace.OpenProjectAsync(inputPath, cancellationToken: cts.Token).ConfigureAwait(false)).Solution
            : await workspace.OpenSolutionAsync(inputPath, cancellationToken: cts.Token).ConfigureAwait(false);
        solution = IndexBuilder.RemoveAnalyzerReferences(solution);

        var declarations = new List<ISymbol>();
        foreach (var project in solution.Projects)
        {
            declarations.AddRange(await SymbolFinder.FindDeclarationsAsync(project, candidate.Name, ignoreCase: false, filter: SymbolFilter.All, cancellationToken: cts.Token).ConfigureAwait(false));
        }
        var symbol = declarations.FirstOrDefault(s => s.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Contains(candidate.FullyQualifiedName, StringComparison.Ordinal));
        if (symbol is null)
        {
            return Array.Empty<SearchResult>();
        }

        var references = await SymbolFinder.FindReferencesAsync(symbol, solution, cts.Token).ConfigureAwait(false);
        var snippets = new SnippetReader(repoRoot);
        var results = references
            .SelectMany(r => r.Locations)
            .Where(location => location.Document.FilePath is not null)
            .Select(location =>
            {
                var span = location.Location.GetLineSpan().StartLinePosition;
                var relative = RepositoryDiscovery.NormalizeRelative(Path.GetRelativePath(repoRoot, location.Document.FilePath!));
                return new SearchResult(relative, span.Line + 1, span.Character + 1, span.Line + 1, span.Character + 1 + candidate.Name.Length, "reference", 1000, "exact Roslyn reference", snippets.ReadSnippet(relative, span.Line + 1), candidate.SymbolId, candidate.Name, candidate.FullyQualifiedName.Contains('.') ? candidate.FullyQualifiedName[..candidate.FullyQualifiedName.LastIndexOf('.')] : null, candidate.FullyQualifiedName, "exact", location.Document.Project.Name);
            })
            .OrderBy(r => r.Path, StringComparer.Ordinal)
            .ThenBy(r => r.Line)
            .ThenBy(r => r.Column)
            .ToArray();
        IndexStore.WriteExactReferenceCache(repoRoot, cacheKey, snapshot.Manifest.UpdatedUtc, results);
        return results;
    }
}

public sealed class DoctorService
{
    public async Task<DoctorSummary> RunAsync(string startPath, IndexerConfig config, CancellationToken cancellationToken = default)
    {
        var root = RepositoryDiscovery.FindRoot(startPath);
        var checks = new List<DoctorCheck>
        {
            new("repo-root", "pass", "info", root.RootPath),
        };

        var inputs = WorkspaceDiscovery.Discover(root.RootPath, config);
        checks.Add(new DoctorCheck("workspace-inputs", inputs.Count > 0 ? "pass" : "fail", inputs.Count > 0 ? "info" : "error", $"{inputs.Count} workspace input(s) detected"));

        try
        {
            MSBuildRegistration.RegisterDefaults();
            checks.Add(new DoctorCheck("msbuild-locator", "pass", "info", "MSBuild registered"));
        }
        catch (Exception ex)
        {
            checks.Add(new DoctorCheck("msbuild-locator", "fail", "error", ex.Message));
        }

        if (inputs.Count > 0)
        {
            using var workspace = IndexBuilder.CreateWorkspace();
            var warnings = new List<string>();
#pragma warning disable CS0618
            workspace.WorkspaceFailed += (_, e) => warnings.Add(e.Diagnostic.Message);
#pragma warning restore CS0618
            try
            {
                var input = inputs[0];
                if (input.Kind.Equals("csproj", StringComparison.OrdinalIgnoreCase))
                {
                    _ = IndexBuilder.RemoveAnalyzerReferences(await workspace.OpenProjectAsync(WorkspaceInputPaths.Resolve(root.RootPath, input.Path), cancellationToken: cancellationToken).ConfigureAwait(false));
                }
                else
                {
                    _ = IndexBuilder.RemoveAnalyzerReferences(await workspace.OpenSolutionAsync(WorkspaceInputPaths.Resolve(root.RootPath, input.Path), cancellationToken: cancellationToken).ConfigureAwait(false));
                }

                checks.Add(new DoctorCheck("workspace-load", "pass", warnings.Count == 0 ? "info" : "warning", warnings.Count == 0 ? "Workspace opened" : string.Join("; ", warnings.Take(3))));
            }
            catch (Exception ex)
            {
                checks.Add(new DoctorCheck("workspace-load", "fail", "warning", ex.Message));
            }
        }

        var status = IndexStore.GetStatus(root.RootPath);
        checks.Add(new DoctorCheck("index", status.Status == IndexStatus.Valid ? "pass" : "warning", status.Status == IndexStatus.Valid ? "info" : "warning", status.Status.ToString()));
        checks.Add(new DoctorCheck("excludes", "pass", "info", string.Join(", ", config.ExcludeDirectories)));
        return new DoctorSummary(root.RootPath, checks);
    }
}

internal static class WorkspaceInputPaths
{
    public static string Resolve(string repoRoot, string path)
        => Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(repoRoot, path));
}
