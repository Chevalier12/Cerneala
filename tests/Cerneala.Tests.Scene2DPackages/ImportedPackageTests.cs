using Cerneala.Scene2D.Importers;
using Cerneala.Scene2D.Packages;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.Scene2DPackages;

public sealed class ImportedPackageTests
{
    [Theory]
    [InlineData("tiled-empty-promotion.tmj")]
    [InlineData("tiled-external.tmj")]
    [InlineData("tiled-finite.tmj")]
    [InlineData("tiled-flips.tmj")]
    [InlineData("tiled-group.tmj")]
    [InlineData("tiled-gzip.tmj")]
    [InlineData("tiled-infinite.tmj")]
    [InlineData("tiled-objects.tmj")]
    [InlineData("tiled-raw.tmj")]
    [InlineData("tiled-zlib.tmj")]
    [InlineData("ldtk-inline.ldtk")]
    [InlineData("ldtk-separate.ldtk")]
    public async Task FullImporterCorpusPreservesEveryPayloadAndExplicitAsset(string file)
    {
        string fixtures = Path.Combine(RepoRoot(), "tests", "Fixtures", "Scene2DImport");
        string input = Path.Combine(fixtures, file);
        Scene2DImportOptions options = new() { AssetRootDirectory = fixtures };
        Scene2DImportResult imported = file.EndsWith(".ldtk", StringComparison.Ordinal)
            ? LdtkScene2DImporter.Import(input, options) : TiledScene2DImporter.Import(input, options);
        Assert.True(imported.Success, string.Join(Environment.NewLine, imported.Diagnostics.Select(static item => item.Message)));
        Scene2DDocument document = imported.Document!;
        string output = Path.Combine(Path.GetTempPath(), "CernealaImportedPackage-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Scene2DPackageWriter.WriteAsync(output, document, imported.AssetRootDirectory!, imported.ReferencedFiles);
            using Scene2DPackage package = await Scene2DPackage.OpenAsync(output);
            PackageIndex index = (PackageIndex)PackageValueCodec.Decode(
                File.ReadAllBytes(Path.Combine(output, PackageFiles.CatalogName))[..^32])!;
            Assert.Equal(document.Levels.Count, package.Levels.Count);
            Scene2DPackageMetadata metadata = await package.LoadMetadataAsync();
            EqualPayload(new Scene2DPackageMetadata(document.Properties, []), metadata);
            foreach (string dependency in imported.ReferencedFiles)
            {
                Assert.Equal(await File.ReadAllBytesAsync(Path.Combine(fixtures, dependency)), await File.ReadAllBytesAsync(package.GetFilePath(dependency)));
            }
            Assert.Equal(imported.ReferencedFiles, package.ReferencedFiles);
            var sizes = document.Assets.ToDictionary(static asset => asset.ResourceId.Key, static asset => asset.Size, StringComparer.Ordinal);
            for (int levelIndex = 0; levelIndex < document.Levels.Count; levelIndex++)
            {
                Scene2DLevel expected = document.Levels[levelIndex];
                Scene2DPackageLevel actual = package.Levels[levelIndex];
                Assert.Equal(expected.Id, actual.Id);
                Assert.Equal(expected.WorldOffset, actual.WorldOffset);
                Assert.Equal(expected.TileSize, actual.TileSize);
                Assert.Equal(expected.Bounds, actual.Bounds);
                Scene2DPackageMetadata levelMetadata = await actual.LoadMetadataAsync();
                EqualPayload(new Scene2DPackageMetadata(expected.Properties, expected.TileSets), levelMetadata);
                Assert.Equal(expected.TileMaps.Select(static map => map.Id), actual.TileMapIds);
                for (int mapIndex = 0; mapIndex < expected.TileMaps.Count; mapIndex++)
                {
                    TileMap2DModel model = expected.TileMaps[mapIndex];
                    PackageMap prepared = index.Levels[levelIndex].Maps[mapIndex];
                    TileMapSource2D inMemory = TileMapSource2D.FromModel(model, sizes);
                    Assert.Equal(inMemory.Entries.Count, prepared.Catalog.Chunks.Count);
                    Scene2DPackageMetadata mapMetadata = await actual.LoadMapMetadataAsync(model.Id);
                    EqualPayload(new Scene2DPackageMetadata(model.Properties, model.TileSets,
                        model.Chunks.Select(static chunk => new Scene2DPackageGridChunkMetadata(
                            new(chunk.Origin.X, chunk.Origin.Y, chunk.Width, chunk.Height), chunk.Version, chunk.Properties))), mapMetadata);
                    for (int chunk = 0; chunk < prepared.Chunks.Length; chunk++)
                    {
                        EqualPayload(inMemory.Entries[chunk], prepared.Catalog.Chunks[chunk].Spatial);
                        using var originalData = await inMemory.LoadAsync(inMemory.Entries[chunk]);
                        TileMapChunkData2D packageData = await package.LoadAsync<TileMapChunkData2D>(
                            prepared.Chunks[chunk], CancellationToken.None);
                        EqualPayload(originalData.Value, packageData);
                    }
                }
                Assert.Equal(expected.Entities.Select(static entity => entity.Id), actual.EntityIds);
                foreach (Scene2DEntity entity in expected.Entities)
                {
                    Scene2DEntity loaded = await actual.LoadEntityAsync(entity.Id);
                    EqualPayload(entity, loaded);
                }
                Assert.Equal(expected.Promotions.Select(static promotion => promotion.Cell), actual.PromotionCells);
                foreach (TilePromotion2D promotion in expected.Promotions)
                {
                    TilePromotion2D loaded = await actual.LoadPromotionAsync(promotion.Cell);
                    EqualPayload(promotion, loaded);
                }
            }
        }
        finally
        {
            string resolved = Path.GetFullPath(output);
            Assert.Equal(Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar), Path.GetDirectoryName(resolved));
            Assert.StartsWith("CernealaImportedPackage-", Path.GetFileName(resolved), StringComparison.Ordinal);
            if (Directory.Exists(resolved)) { Directory.Delete(resolved, recursive: true); }
        }
    }

    private static void EqualPayload(object expected, object actual) => Assert.Equal(PackageValueCodec.Encode(expected), PackageValueCodec.Encode(actual));

    private static string RepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx"))) { directory = directory.Parent; }
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
