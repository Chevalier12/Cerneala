using System.Text.Json;

namespace RoslynRepoIndexer.Core;

public enum CallGraphDirection { Callers, Callees, Both }
public enum InspectInclude { Source, Signature, Documentation, ContainingType, BaseTypes, Members, Callers, Callees, References, Implementations, Tests }

public sealed record SourceSpanSummary(string Path, int Line, int Column, int EndLine, int EndColumn, int SpanStart, int SpanLength);
public sealed record SymbolSummary(string SymbolId, string Name, string Kind, string FullyQualifiedName, string Signature, string Accessibility, SourceSpanSummary Span, string? ProjectName = null, string? ContainerName = null);
public sealed record OutlineItem(SymbolSummary Symbol, string? ParentSymbolId, int Depth);
public sealed record OutlineResult(IReadOnlyList<OutlineItem> Items, bool Truncated);
public sealed record InspectResult(
    SymbolSummary Symbol,
    string? Source,
    string? Documentation,
    SymbolSummary? ContainingType,
    IReadOnlyList<SymbolSummary> BaseTypes,
    IReadOnlyList<SymbolSummary> Members,
    IReadOnlyList<SymbolSummary> Callers,
    IReadOnlyList<SymbolSummary> Callees,
    IReadOnlyList<SourceSpanSummary> References,
    IReadOnlyList<SymbolSummary> Implementations,
    IReadOnlyList<TestCandidate> Tests,
    bool Truncated);
public sealed record ContextResult(SymbolSummary Symbol, string? Source, IReadOnlyList<SymbolSummary> Related, IReadOnlyList<TestCandidate> Tests, bool Truncated);
public sealed record CallGraphNode(string Id, string Name, string Kind, string? Path, int? Line, bool External = false);
public sealed record CallGraphEdge(string From, string To, string Kind);
public sealed record CallGraphResult(IReadOnlyList<CallGraphNode> Nodes, IReadOnlyList<CallGraphEdge> Edges, bool Truncated);
public sealed record ImpactLink(string SymbolId, string Relationship, string Reason, double Confidence);
public sealed record ImpactResult(SymbolSummary Target, bool PublicApiExposure, IReadOnlyList<string> AffectedProjects, IReadOnlyList<ImpactLink> Links, IReadOnlyList<TestCandidate> Tests, bool Truncated);
public sealed record TestCandidate(string Path, string? ProjectName, double Score, IReadOnlyList<string> Reasons);

public sealed class SymbolQueryException : Exception
{
    public SymbolQueryException(string code, string message, IReadOnlyList<SymbolSummary>? candidates = null) : base(message)
    {
        Code = code;
        Candidates = candidates ?? Array.Empty<SymbolSummary>();
    }
    public string Code { get; }
    public IReadOnlyList<SymbolSummary> Candidates { get; }
}

public sealed class SemanticQueryService
{
    private readonly QueryIndex index;
    private readonly string repoRoot;

    public SemanticQueryService(QueryIndex index, string repoRoot)
    {
        this.index = index;
        this.repoRoot = Path.GetFullPath(repoRoot);
    }

    public OutlineResult Outline(string target, int depth = 2, int maxResults = 200, int maxChars = 30_000, bool includePrivate = false, bool includeGenerated = false)
    {
        var normalized = RepositoryDiscovery.NormalizeRelative(target);
        IEnumerable<SymbolEntry> candidates;
        if (index.DocumentsByPath.TryGetValue(normalized, out var document))
        {
            _ = document;
            candidates = index.Snapshot.Symbols.Where(symbol => string.Equals(symbol.Path, normalized, StringComparison.Ordinal));
        }
        else
        {
            var root = Resolve(target);
            candidates = index.Snapshot.Symbols.Where(symbol => symbol.SymbolId == root.SymbolId
                || string.Equals(symbol.ContainerName, TrimGlobal(root.FullyQualifiedName), StringComparison.Ordinal)
                || symbol.FullyQualifiedName.StartsWith(root.FullyQualifiedName + ".", StringComparison.Ordinal)
                || symbol.FullyQualifiedName.StartsWith(TrimGlobal(root.FullyQualifiedName) + ".", StringComparison.Ordinal));
        }

        var materialized = candidates
            .Where(symbol => (includePrivate || !string.Equals(symbol.Accessibility, "private", StringComparison.OrdinalIgnoreCase))
                             && (includeGenerated || !IsGenerated(symbol)))
            .OrderBy(symbol => symbol.Path, StringComparer.Ordinal).ThenBy(symbol => symbol.SpanStart).ThenBy(symbol => symbol.SymbolId, StringComparer.Ordinal)
            .ToArray();
        var byFqn = materialized.GroupBy(symbol => TrimGlobal(symbol.FullyQualifiedName), StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var items = materialized.Select(symbol =>
        {
            var parent = symbol.ContainerName is not null && byFqn.TryGetValue(TrimGlobal(symbol.ContainerName), out var owner) ? owner.SymbolId : null;
            return new OutlineItem(Summary(symbol), parent, ParentDepth(symbol, byFqn));
        }).Where(item => item.Depth <= Math.Max(0, depth)).ToArray();
        return Budget(items, maxResults, maxChars, selected => new OutlineResult(selected, selected.Count < items.Length));
    }

    public InspectResult Inspect(string query, IReadOnlyCollection<InspectInclude> include, int depth = 1, int maxResults = 80, int maxChars = 30_000)
    {
        var symbol = Resolve(query);
        var flags = include.Count == 0 ? Enum.GetValues<InspectInclude>().ToHashSet() : include.ToHashSet();
        var members = flags.Contains(InspectInclude.Members)
            ? index.Snapshot.Symbols.Where(candidate => string.Equals(TrimGlobal(candidate.ContainerName), TrimGlobal(symbol.FullyQualifiedName), StringComparison.Ordinal)).OrderBy(candidate => candidate.SpanStart).Select(Summary).ToArray()
            : Array.Empty<SymbolSummary>();
        var callerEntries = flags.Contains(InspectInclude.Callers) ? RelatedCallers(symbol.SymbolId) : Array.Empty<SymbolEntry>();
        var calleeEntries = flags.Contains(InspectInclude.Callees) ? RelatedCallees(symbol.SymbolId) : Array.Empty<SymbolEntry>();
        var references = flags.Contains(InspectInclude.References)
            ? index.Snapshot.References.Where(reference => reference.SymbolId == symbol.SymbolId).OrderBy(reference => reference.Path, StringComparer.Ordinal).ThenBy(reference => reference.SpanStart).Select(Span).ToArray()
            : Array.Empty<SourceSpanSummary>();
        var baseTypes = flags.Contains(InspectInclude.BaseTypes) ? ResolveIds(symbol.BaseTypeIds.Concat(symbol.InterfaceTypeIds)).ToArray() : Array.Empty<SymbolSummary>();
        var implementations = flags.Contains(InspectInclude.Implementations)
            ? index.Snapshot.Symbols.Where(candidate => candidate.BaseTypeIds.Contains(symbol.SymbolId, StringComparer.Ordinal) || candidate.InterfaceTypeIds.Contains(symbol.SymbolId, StringComparer.Ordinal) || candidate.OverriddenSymbolId == symbol.SymbolId).Select(Summary).ToArray()
            : Array.Empty<SymbolSummary>();
        var tests = flags.Contains(InspectInclude.Tests) ? TestsFor(symbol.SymbolId, Math.Min(maxResults, 50)).ToArray() : Array.Empty<TestCandidate>();
        var result = new InspectResult(
            Summary(symbol),
            flags.Contains(InspectInclude.Source) ? ReadSource(symbol) : null,
            flags.Contains(InspectInclude.Documentation) ? ReadDocumentation(symbol) : null,
            flags.Contains(InspectInclude.ContainingType) ? ResolveContainer(symbol) : null,
            baseTypes,
            members.Take(maxResults).ToArray(),
            callerEntries.Take(maxResults).Select(Summary).ToArray(),
            calleeEntries.Take(maxResults).Select(Summary).ToArray(),
            references.Take(maxResults).ToArray(),
            implementations.Take(maxResults).ToArray(),
            tests,
            members.Length > maxResults || callerEntries.Length > maxResults || calleeEntries.Length > maxResults || references.Length > maxResults || implementations.Length > maxResults);
        return TrimInspect(result, maxChars);
    }

    public ContextResult Context(string query, int maxChars = 30_000, int maxResults = 40)
    {
        var symbol = Resolve(query);
        var related = RelatedCallers(symbol.SymbolId).Concat(RelatedCallees(symbol.SymbolId)).GroupBy(item => item.SymbolId, StringComparer.Ordinal).Select(group => group.First()).Take(maxResults).Select(Summary).ToArray();
        var tests = TestsFor(symbol.SymbolId, Math.Min(20, maxResults)).ToArray();
        var result = new ContextResult(Summary(symbol), ReadSource(symbol), related, tests, false);
        while (JsonSerializer.Serialize(result, JsonOptions.Compact).Length > maxChars && result.Related.Count > 0)
        {
            result = result with { Related = result.Related.Take(result.Related.Count - 1).ToArray(), Truncated = true };
        }
        if (JsonSerializer.Serialize(result, JsonOptions.Compact).Length > maxChars)
        {
            result = result with { Source = Truncate(result.Source, Math.Max(0, maxChars / 2)), Tests = Array.Empty<TestCandidate>(), Truncated = true };
        }
        return result;
    }

    public CallGraphResult CallGraph(string query, CallGraphDirection direction = CallGraphDirection.Both, int depth = 1, int maxNodes = 100, bool includeTests = true, bool includeExternal = false)
    {
        var root = Resolve(query);
        var nodes = new Dictionary<string, CallGraphNode>(StringComparer.Ordinal) { [root.SymbolId] = Node(root) };
        var edges = new HashSet<CallGraphEdge>();
        var queue = new Queue<(string Id, int Depth)>(); queue.Enqueue((root.SymbolId, 0));
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var truncated = false;
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current.Id) || current.Depth >= Math.Max(0, depth)) continue;
            IEnumerable<(string From, string To)> adjacent = direction switch
            {
                CallGraphDirection.Callers => CallerEdges(current.Id),
                CallGraphDirection.Callees => CalleeEdges(current.Id),
                _ => CallerEdges(current.Id).Concat(CalleeEdges(current.Id))
            };
            foreach (var edge in adjacent.OrderBy(item => item.From, StringComparer.Ordinal).ThenBy(item => item.To, StringComparer.Ordinal))
            {
                if (nodes.Count >= maxNodes && (!nodes.ContainsKey(edge.From) || !nodes.ContainsKey(edge.To))) { truncated = true; continue; }
                if (!TryAddGraphNode(edge.From, nodes, includeTests, includeExternal) || !TryAddGraphNode(edge.To, nodes, includeTests, includeExternal)) continue;
                edges.Add(new CallGraphEdge(edge.From, edge.To, "invocation"));
                var next = edge.From == current.Id ? edge.To : edge.From;
                queue.Enqueue((next, current.Depth + 1));
            }
        }
        return new CallGraphResult(nodes.Values.OrderBy(node => node.Id, StringComparer.Ordinal).ToArray(), edges.OrderBy(edge => edge.From, StringComparer.Ordinal).ThenBy(edge => edge.To, StringComparer.Ordinal).ToArray(), truncated);
    }

    public ImpactResult Impact(string query, int maxResults = 100)
    {
        var symbol = Resolve(query);
        var links = new List<ImpactLink>();
        links.AddRange(RelatedCallers(symbol.SymbolId).Select(item => new ImpactLink(item.SymbolId, "caller", "Indexed invocation/reference originates from this symbol.", 1.0)));
        links.AddRange(index.Snapshot.Symbols.Where(item => item.BaseTypeIds.Contains(symbol.SymbolId, StringComparer.Ordinal)).Select(item => new ImpactLink(item.SymbolId, "derived-type", "Indexed base-type edge targets this symbol.", 1.0)));
        links.AddRange(index.Snapshot.Symbols.Where(item => item.InterfaceTypeIds.Contains(symbol.SymbolId, StringComparer.Ordinal)).Select(item => new ImpactLink(item.SymbolId, "implementation", "Indexed interface implementation edge targets this symbol.", 1.0)));
        links.AddRange(index.Snapshot.Symbols.Where(item => item.OverriddenSymbolId == symbol.SymbolId).Select(item => new ImpactLink(item.SymbolId, "override", "Indexed override edge targets this symbol.", 1.0)));
        var distinct = links.GroupBy(link => (link.SymbolId, link.Relationship)).Select(group => group.First()).OrderBy(link => link.Relationship, StringComparer.Ordinal).ThenBy(link => link.SymbolId, StringComparer.Ordinal).ToArray();
        var projects = distinct.Select(link => index.SymbolsById.GetValueOrDefault(link.SymbolId)?.ProjectName).Where(name => name is not null).Distinct(StringComparer.Ordinal).Cast<string>().OrderBy(name => name, StringComparer.Ordinal).ToArray();
        return new ImpactResult(Summary(symbol), symbol.Accessibility is "public" or "protected" or "protected internal", projects, distinct.Take(maxResults).ToArray(), TestsFor(symbol.SymbolId, 30), distinct.Length > maxResults);
    }

    public IReadOnlyList<TestCandidate> TestsFor(string query, int maxResults = 50)
    {
        var symbol = Resolve(query);
        var referencedDocuments = index.Snapshot.References.Where(reference => reference.SymbolId == symbol.SymbolId).Select(reference => reference.DocumentId).ToHashSet(StringComparer.Ordinal);
        return index.Snapshot.Documents.Where(document => IsTestPath(document.RelativePath) || (document.ProjectName?.Contains("Test", StringComparison.OrdinalIgnoreCase) ?? false))
            .Select(document =>
            {
                var reasons = new List<string>(); double score = 0;
                if (referencedDocuments.Contains(document.DocumentId)) { score += 100; reasons.Add("semantic-reference"); }
                if (Path.GetFileNameWithoutExtension(document.RelativePath).Contains(symbol.Name, StringComparison.OrdinalIgnoreCase)) { score += 30; reasons.Add("name-match"); }
                if (string.Equals(document.ProjectName, symbol.ProjectName, StringComparison.Ordinal)) { score += 10; reasons.Add("same-project"); }
                score += PathProximity(document.RelativePath, symbol.Path);
                if (score > 0) reasons.Add("path-proximity");
                return new TestCandidate(document.RelativePath, document.ProjectName, score, reasons);
            }).Where(candidate => candidate.Score > 0).OrderByDescending(candidate => candidate.Score).ThenBy(candidate => candidate.Path, StringComparer.Ordinal).Take(maxResults).ToArray();
    }

    private SymbolEntry Resolve(string query)
    {
        var trimmed = query.Trim();
        var candidates = index.Snapshot.Symbols.Where(symbol => string.Equals(symbol.SymbolId, trimmed, StringComparison.Ordinal)
            || string.Equals(symbol.FullyQualifiedName, trimmed, StringComparison.OrdinalIgnoreCase)
            || string.Equals(TrimGlobal(symbol.FullyQualifiedName), TrimGlobal(trimmed), StringComparison.OrdinalIgnoreCase)
            || string.Equals(symbol.Name, trimmed, StringComparison.OrdinalIgnoreCase))
            .GroupBy(symbol => symbol.SymbolId, StringComparer.Ordinal).Select(group => group.OrderBy(symbol => symbol.Path, StringComparer.Ordinal).ThenBy(symbol => symbol.SpanStart).First()).OrderBy(symbol => symbol.FullyQualifiedName, StringComparer.Ordinal).ThenBy(symbol => symbol.Path, StringComparer.Ordinal).ToArray();
        if (candidates.Length == 0)
        {
            candidates = index.Snapshot.Symbols.Where(symbol => symbol.Name.Contains(trimmed, StringComparison.OrdinalIgnoreCase) || symbol.FullyQualifiedName.Contains(trimmed, StringComparison.OrdinalIgnoreCase)).GroupBy(symbol => symbol.SymbolId, StringComparer.Ordinal).Select(group => group.First()).Take(20).ToArray();
        }
        if (candidates.Length == 0) throw new SymbolQueryException("symbol-not-found", $"No indexed symbol matches '{query}'.");
        if (candidates.Length > 1) throw new SymbolQueryException("ambiguous-symbol", $"Symbol query '{query}' matched {candidates.Length} symbols.", candidates.Select(Summary).ToArray());
        return candidates[0];
    }

    private SymbolSummary Summary(SymbolEntry symbol) => new(symbol.SymbolId, symbol.Name, symbol.Kind, symbol.FullyQualifiedName, symbol.Signature, symbol.Accessibility, Span(symbol), symbol.ProjectName, symbol.ContainerName);
    private static SourceSpanSummary Span(SymbolEntry symbol) => new(symbol.Path, symbol.Line, symbol.Column, symbol.EndLine, symbol.EndColumn, symbol.SpanStart, symbol.SpanLength);
    private static SourceSpanSummary Span(ReferenceEntry reference) => new(reference.Path, reference.Line, reference.Column, reference.EndLine, reference.EndColumn, reference.SpanStart, reference.SpanLength);
    private SymbolSummary? ResolveContainer(SymbolEntry symbol) => symbol.ContainerName is null ? null : index.Snapshot.Symbols.FirstOrDefault(candidate => string.Equals(TrimGlobal(candidate.FullyQualifiedName), TrimGlobal(symbol.ContainerName), StringComparison.Ordinal)) is { } owner ? Summary(owner) : null;
    private IEnumerable<SymbolSummary> ResolveIds(IEnumerable<string> ids) => ids.Select(id => index.SymbolsById.GetValueOrDefault(id)).Where(symbol => symbol is not null).Cast<SymbolEntry>().Select(Summary);
    private SymbolEntry[] RelatedCallers(string symbolId) => index.Snapshot.References.Where(reference => reference.SymbolId == symbolId && reference.ContainingSymbolId is not null).Select(reference => index.SymbolsById.GetValueOrDefault(reference.ContainingSymbolId!)).Where(symbol => symbol is not null).Cast<SymbolEntry>().GroupBy(symbol => symbol.SymbolId, StringComparer.Ordinal).Select(group => group.First()).OrderBy(symbol => symbol.FullyQualifiedName, StringComparer.Ordinal).ToArray();
    private SymbolEntry[] RelatedCallees(string symbolId) => index.Snapshot.References.Where(reference => reference.ContainingSymbolId == symbolId && reference.IsInvocation).Select(reference => index.SymbolsById.GetValueOrDefault(reference.SymbolId)).Where(symbol => symbol is not null).Cast<SymbolEntry>().GroupBy(symbol => symbol.SymbolId, StringComparer.Ordinal).Select(group => group.First()).OrderBy(symbol => symbol.FullyQualifiedName, StringComparer.Ordinal).ToArray();
    private IEnumerable<(string From, string To)> CallerEdges(string id) => index.Snapshot.References.Where(reference => reference.SymbolId == id && reference.IsInvocation && reference.ContainingSymbolId is not null).Select(reference => (reference.ContainingSymbolId!, id));
    private IEnumerable<(string From, string To)> CalleeEdges(string id) => index.Snapshot.References.Where(reference => reference.ContainingSymbolId == id && reference.IsInvocation).Select(reference => (id, reference.SymbolId));
    private bool TryAddGraphNode(string id, Dictionary<string, CallGraphNode> nodes, bool includeTests, bool includeExternal)
    {
        if (nodes.ContainsKey(id)) return true;
        if (index.SymbolsById.TryGetValue(id, out var symbol))
        {
            if (!includeTests && IsTestPath(symbol.Path)) return false;
            nodes[id] = Node(symbol); return true;
        }
        if (!includeExternal) return false;
        nodes[id] = new CallGraphNode(id, id, "external", null, null, true); return true;
    }
    private static CallGraphNode Node(SymbolEntry symbol) => new(symbol.SymbolId, symbol.Name, symbol.Kind, symbol.Path, symbol.Line);
    private string? ReadSource(SymbolEntry symbol)
    {
        var path = Path.GetFullPath(Path.Combine(repoRoot, symbol.Path));
        if (!path.StartsWith(repoRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) return null;
        var lines = File.ReadAllLines(path); var start = Math.Max(0, symbol.Line - 3); var end = Math.Min(lines.Length, Math.Max(symbol.EndLine + 2, symbol.Line + 3));
        return string.Join(Environment.NewLine, lines[start..end]);
    }
    private string? ReadDocumentation(SymbolEntry symbol)
    {
        var path = Path.Combine(repoRoot, symbol.Path); if (!File.Exists(path)) return null;
        var lines = File.ReadAllLines(path); var docs = new List<string>();
        for (var index = Math.Min(lines.Length - 1, symbol.Line - 2); index >= 0 && lines[index].TrimStart().StartsWith("///", StringComparison.Ordinal); index--) docs.Add(lines[index].Trim());
        docs.Reverse(); return docs.Count == 0 ? null : string.Join(Environment.NewLine, docs);
    }
    private static int ParentDepth(SymbolEntry symbol, IReadOnlyDictionary<string, SymbolEntry> byFqn)
    {
        var depth = 0; var container = symbol.ContainerName;
        while (container is not null && byFqn.TryGetValue(TrimGlobal(container), out var parent) && depth < 32) { depth++; container = parent.ContainerName; }
        return depth;
    }
    private static T Budget<TItem, T>(IReadOnlyList<TItem> items, int maxResults, int maxChars, Func<IReadOnlyList<TItem>, T> create)
    {
        var selected = new List<TItem>();
        foreach (var item in items.Take(Math.Max(0, maxResults)))
        {
            selected.Add(item);
            if (JsonSerializer.Serialize(create(selected), JsonOptions.Compact).Length > Math.Max(1, maxChars)) { selected.RemoveAt(selected.Count - 1); break; }
        }
        return create(selected);
    }
    private static InspectResult TrimInspect(InspectResult result, int maxChars)
    {
        while (JsonSerializer.Serialize(result, JsonOptions.Compact).Length > maxChars && result.Members.Count > 0) result = result with { Members = result.Members.Take(result.Members.Count - 1).ToArray(), Truncated = true };
        while (JsonSerializer.Serialize(result, JsonOptions.Compact).Length > maxChars && result.References.Count > 0) result = result with { References = result.References.Take(result.References.Count - 1).ToArray(), Truncated = true };
        if (JsonSerializer.Serialize(result, JsonOptions.Compact).Length > maxChars) result = result with { Source = Truncate(result.Source, Math.Max(0, maxChars / 3)), Documentation = null, Tests = Array.Empty<TestCandidate>(), Truncated = true };
        return result;
    }
    private static string? Truncate(string? value, int maxChars) => value is null || value.Length <= maxChars ? value : value[..maxChars];
    private static bool IsGenerated(SymbolEntry symbol) => symbol.Path.StartsWith("generated/", StringComparison.OrdinalIgnoreCase) || symbol.Path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase);
    private static bool IsTestPath(string path) => path.Split('/', '\\').Any(segment => segment.Contains("test", StringComparison.OrdinalIgnoreCase));
    private static double PathProximity(string left, string right) => left.Split('/', '\\').Zip(right.Split('/', '\\')).TakeWhile(pair => string.Equals(pair.First, pair.Second, StringComparison.OrdinalIgnoreCase)).Count() * 2;
    private static string TrimGlobal(string? value) => (value ?? string.Empty).Replace("global::", string.Empty, StringComparison.Ordinal);
}
