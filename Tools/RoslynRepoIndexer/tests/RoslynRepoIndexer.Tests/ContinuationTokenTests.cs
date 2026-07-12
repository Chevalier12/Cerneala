using Ri.Mcp;
using RoslynRepoIndexer.Core;

namespace RoslynRepoIndexer.Tests;

public sealed class ContinuationTokenTests
{
    [Fact]
    public void Tokens_are_signed_and_bound_to_tool_and_generation()
    {
        var codec = new ContinuationTokenCodec();
        var token = codec.Encode("roslyn_search", "generation-a", 42);

        Assert.Equal(42, codec.Decode(token, "roslyn_search", "generation-a"));
        Assert.Throws<ContinuationTokenException>(() => codec.Decode(token, "roslyn_goto", "generation-a"));
        Assert.Throws<ContinuationTokenException>(() => codec.Decode(token, "roslyn_search", "generation-b"));
        Assert.Throws<ContinuationTokenException>(() => codec.Decode(token + "x", "roslyn_search", "generation-a"));
    }

    [Fact]
    public async Task Goto_pages_stably_and_rejects_a_token_after_generation_change()
    {
        using var repo = TestRepo.Create();
        Directory.CreateDirectory(Path.Combine(repo.Root, ".git"));
        IndexStore.Write(repo.Root, Snapshot(repo.Root, "one"));
        var tools = new RoslynMcpTools();

        var first = await tools.GotoAsync(new RoslynGotoRequest(repo.Root, "Widget", Limit: 1));
        var second = await tools.GotoAsync(new RoslynGotoRequest(repo.Root, "Widget", Limit: 1, ContinuationToken: first.ContinuationToken));

        Assert.True(first.Success);
        Assert.True(first.Truncated);
        Assert.NotNull(first.ContinuationToken);
        Assert.Single(first.Data!);
        Assert.Single(second.Data!);
        Assert.NotEqual(first.Data![0].Path, second.Data![0].Path);

        IndexStore.Write(repo.Root, Snapshot(repo.Root, "two"));
        var stale = await tools.GotoAsync(new RoslynGotoRequest(repo.Root, "Widget", Limit: 1, ContinuationToken: first.ContinuationToken));

        Assert.False(stale.Success);
        Assert.Equal("invalid-continuation-token", stale.Errors.Single().Code);
    }

    [Fact]
    public void Random_malformed_tokens_are_rejected_without_unbounded_parsing()
    {
        var codec = new ContinuationTokenCodec();
        var random = new Random(20260712);
        for (var index = 0; index < 2_000; index++)
        {
            var bytes = new byte[random.Next(0, 512)];
            random.NextBytes(bytes);
            var token = Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_') + (index % 3 == 0 ? ".invalid" : string.Empty);
            Assert.Throws<ContinuationTokenException>(() => codec.Decode(token, "roslyn_search", "generation"));
        }

        Assert.Throws<ContinuationTokenException>(() => codec.Decode(new string('a', 4097), "roslyn_search", "generation"));
    }

    private static IndexSnapshot Snapshot(string root, string suffix)
    {
        var documents = Enumerable.Range(1, 3)
            .Select(index => new DocumentEntry($"d{index}", null, $"Widget{index}.cs", null, "C#", true, false, false, 1, DateTimeOffset.UtcNow, "hash", "decl", 1))
            .ToArray();
        var symbols = documents.Select((document, index) => new SymbolEntry(
            $"symbol-{suffix}-{index}", document.DocumentId, null, "class", "Widget", "Widget", $"Example.Widget{index}", "Example", $"class Widget{index}",
            "public", Array.Empty<string>(), document.RelativePath, 1, 1, 1, 7, 0, 6, true, false, Array.Empty<string>(), null, null, null)).ToArray();
        var manifest = IndexManifest.CreateNew(root, "config", "workspace") with
        {
            DocumentCount = documents.Length,
            SymbolCount = symbols.Length,
            UpdatedUtc = DateTimeOffset.UtcNow
        };
        return new IndexSnapshot(manifest, documents, symbols, Array.Empty<ReferenceEntry>(), Array.Empty<TokenPosting>());
    }
}
