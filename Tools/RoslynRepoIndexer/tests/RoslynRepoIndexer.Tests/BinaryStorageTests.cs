using RoslynRepoIndexer.Core;
using System.Text.Json;

namespace RoslynRepoIndexer.Tests;

public sealed class BinaryStorageTests
{
    [Fact]
    public void Publishing_keeps_current_and_previous_generations_and_recovers_when_current_directory_is_missing()
    {
        using var repo = TestRepo.Create();
        IndexStore.Write(repo.Root, EmptySnapshot(repo.Root, "first"));
        IndexStore.Write(repo.Root, EmptySnapshot(repo.Root, "second"));
        IndexStore.Write(repo.Root, EmptySnapshot(repo.Root, "third"));

        Assert.Equal("third", IndexStore.Read(repo.Root).Manifest.ConfigHash);
        Assert.Equal("second", IndexStore.ReadPrevious(repo.Root).Manifest.ConfigHash);
        Assert.Equal(2, Directory.GetDirectories(IndexStore.GetGenerationsDirectory(repo.Root)).Length);
        Assert.True(File.Exists(IndexStore.GetCurrentPointerPath(repo.Root)));

        Directory.Delete(IndexStore.GetVersionDirectory(repo.Root), recursive: true);

        Assert.Equal("second", IndexStore.Read(repo.Root).Manifest.ConfigHash);
    }

    [Fact]
    public void Binary_tables_round_trip_rows_and_detect_checksum_corruption()
    {
        using var repo = TestRepo.Create();
        var document = new DocumentEntry("doc", "project", "generated/Example.g.cs", "App", "C#", true, true, false, 123, new DateTimeOffset(2026, 7, 12, 1, 2, 3, TimeSpan.Zero), "content", "declaration", 7);
        var symbol = new SymbolEntry("symbol", "doc", "project", "method", "Run", "Run", "App.Example.Run", "App.Example", "void Run(string value)", "public", new[] { "static" }, document.RelativePath, 2, 3, 4, 5, 10, 20, true, true, new[] { "string" }, "void", "App", "key");
        var reference = new ReferenceEntry("reference", "symbol", "doc", "project", "Run", document.RelativePath, 5, 6, 5, 9, 30, 3, "App", "invocation");
        var token = new TokenPosting("run", document.RelativePath, 2, 3, "symbol", "definition", "App", "doc");
        var snapshot = new IndexSnapshot(IndexManifest.CreateNew(repo.Root, "config", "workspace") with { DocumentCount = 1, SymbolCount = 1, ReferenceCount = 1, TokenCount = 1 }, new[] { document }, new[] { symbol }, new[] { reference }, new[] { token });

        IndexStore.Write(repo.Root, snapshot);
        var actual = IndexStore.Read(repo.Root);

        Assert.Equal(document, actual.Documents.Single());
        Assert.Equivalent(symbol, actual.Symbols.Single(), strict: true);
        Assert.Equal(reference, actual.References.Single());
        Assert.Equal(token, actual.Tokens.Single());
        Assert.Equal("segmented-binary-v1", actual.Manifest.StorageFormat);

        var segmentPath = Directory.GetFiles(Path.Combine(IndexStore.GetIndexDirectory(repo.Root), "segments"), "*.bin").Single();
        var bytes = File.ReadAllBytes(segmentPath);
        bytes[^1] ^= 0x5A;
        File.WriteAllBytes(segmentPath, bytes);

        Assert.Throws<IndexUnavailableException>(() => IndexStore.Read(repo.Root));
    }

    [Fact]
    public void Corrupt_current_generation_falls_back_to_the_last_valid_published_generation()
    {
        using var repo = TestRepo.Create();
        IndexStore.Write(repo.Root, DocumentSnapshot(repo.Root, "first"));
        IndexStore.Write(repo.Root, DocumentSnapshot(repo.Root, "second"));
        var current = IndexStore.GetVersionDirectory(repo.Root);
        var descriptor = JsonDocument.Parse(File.ReadAllText(Path.Combine(current, "segments.json"))).RootElement[0];
        var segmentPath = Path.Combine(IndexStore.GetIndexDirectory(repo.Root), "segments", descriptor.GetProperty("fileName").GetString()!);
        File.WriteAllBytes(segmentPath, new byte[] { 1, 2, 3 });

        var recovered = IndexStore.Read(repo.Root);

        Assert.Equal("first", recovered.Documents.Single().ContentHash);
    }

    private static IndexSnapshot EmptySnapshot(string root, string configHash)
        => new(IndexManifest.CreateNew(root, configHash, "workspace"), Array.Empty<DocumentEntry>(), Array.Empty<SymbolEntry>(), Array.Empty<ReferenceEntry>(), Array.Empty<TokenPosting>());

    private static IndexSnapshot DocumentSnapshot(string root, string value)
    {
        var document = new DocumentEntry("doc", null, "Example.cs", null, "C#", true, false, false, value.Length, DateTimeOffset.UtcNow, value, "declaration", 1);
        return new IndexSnapshot(IndexManifest.CreateNew(root, "config", "workspace") with { DocumentCount = 1 }, new[] { document }, Array.Empty<SymbolEntry>(), Array.Empty<ReferenceEntry>(), Array.Empty<TokenPosting>());
    }
}
