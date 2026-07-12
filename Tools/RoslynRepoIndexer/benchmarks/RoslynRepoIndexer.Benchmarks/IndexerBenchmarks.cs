using BenchmarkDotNet.Attributes;
using RoslynRepoIndexer.Core;

namespace RoslynRepoIndexer.Benchmarks;

[MemoryDiagnoser]
public class QueryBenchmarks
{
    private QueryIndex queryIndex = null!;
    private SearchService search = null!;
    private RepositoryIndexSession session = null!;
    private RoslynIndexerApplicationService application = null!;
    private string repoRoot = null!;

    [Params("UIElement", "InvalidateMeasure", "render cache")]
    public string Query { get; set; } = null!;

    [GlobalSetup]
    public void Setup()
    {
        repoRoot = BenchmarkRepository.Find();
        queryIndex = new QueryIndex(IndexStore.Read(repoRoot));
        search = new SearchService(queryIndex, (_, _) => string.Empty);
        session = new RepositoryIndexSession(repoRoot);
        application = new RoslynIndexerApplicationService(repoRoot, _ => session.GetQueryIndex());
    }

    [Benchmark(Baseline = true)]
    public IReadOnlyList<SearchResult> WarmSearch()
        => search.Search(new SearchRequest(Query, SearchMode.All, 20));

    [Benchmark]
    public void TwentyPersistentApplicationServiceCalls()
    {
        for (var index = 0; index < 20; index++)
        {
            var result = application.Search(new SearchCommandRequest(Query, Limit: 20));
            if (!result.Success) throw new InvalidOperationException("Application-service search failed during benchmark.");
        }
    }

    [GlobalCleanup]
    public void Cleanup() => session.Dispose();
}

[MemoryDiagnoser]
public class LoadAndIndexBenchmarks
{
    private string repoRoot = null!;
    private string? temporaryCorpus;
    private IndexerConfig config = IndexerConfig.Default;

    [ParamsAllValues]
    public BenchmarkCorpusSize Corpus { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        if (Corpus == BenchmarkCorpusSize.CernealaLike)
        {
            repoRoot = BenchmarkRepository.Find();
            config = IndexerConfig.Default with { IncludeNonCSharpText = false };
            return;
        }

        temporaryCorpus = await BenchmarkCorpus.CreateAsync(Corpus == BenchmarkCorpusSize.Small ? 10 : 100).ConfigureAwait(false);
        repoRoot = temporaryCorpus;
    }

    [Benchmark]
    public QueryIndex ColdIndexLoadAndLookupBuild()
        => new(IndexStore.Read(repoRoot));

    [Benchmark]
    public async Task<IndexSummary> NoOpIncremental()
    {
        var summary = await new IndexBuilder().BuildAsync(repoRoot, force: false, config).ConfigureAwait(false);
        if (summary.DirtyDocuments != 0 || summary.Timings.WorkspaceLoadMs != 0) throw new InvalidOperationException("No-op benchmark opened the workspace.");
        return summary;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (temporaryCorpus is not null && Directory.Exists(temporaryCorpus)) Directory.Delete(temporaryCorpus, recursive: true);
    }
}

public enum BenchmarkCorpusSize
{
    Small,
    Medium,
    CernealaLike
}

internal static class BenchmarkRepository
{
    public static string Find()
    {
        var configured = Environment.GetEnvironmentVariable("RI_BENCH_REPO");
        if (!string.IsNullOrWhiteSpace(configured)) return Path.GetFullPath(configured);
        return RepositoryDiscovery.FindRoot(AppContext.BaseDirectory).RootPath;
    }
}

internal static class BenchmarkCorpus
{
    public static async Task<string> CreateAsync(int documentCount)
    {
        var root = Path.Combine(Path.GetTempPath(), "ri-benchmark-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "Corpus.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework><Nullable>enable</Nullable></PropertyGroup>
            </Project>
            """).ConfigureAwait(false);
        for (var index = 0; index < documentCount; index++)
        {
            await File.WriteAllTextAsync(Path.Combine(root, $"Feature{index:D4}.cs"), $$"""
                namespace DeterministicCorpus;
                public sealed class Feature{{index:D4}}
                {
                    public string Execute(string value) => value + "-{{index:D4}}";
                }
                """).ConfigureAwait(false);
        }
        await new IndexBuilder().BuildAsync(root, force: true, IndexerConfig.Default).ConfigureAwait(false);
        return root;
    }
}
