using System.Text.Json;
using Cerneala.Drawing;
using Cerneala.Scene2D.Importers;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.Scene2DImporters;

public sealed class JsonMetadataContractTests
{
    [Theory]
    [InlineData("tiled-finite.tmj")]
    [InlineData("tiled-external.tmj")]
    [InlineData("tiled-group.tmj")]
    [InlineData("tiled-objects.tmj")]
    [InlineData("ldtk-inline.ldtk")]
    [InlineData("ldtk-separate.ldtk")]
    public void ImportedMetadataContainsDetachedExplicitJsonValuesNotRawElements(string file)
    {
        string root = FixtureRoot();
        Scene2DImportOptions options = new() { AssetRootDirectory = root };
        string path = Path.Combine(root, file);
        Scene2DImportResult result = file.EndsWith(".ldtk", StringComparison.Ordinal)
            ? LdtkScene2DImporter.Import(path, options) : TiledScene2DImporter.Import(path, options);
        Assert.True(result.Success, string.Join("; ", result.Diagnostics.Select(item => item.Message)));
        MetadataWalk walk = new();
        walk.Visit(result.Document!);
        Assert.True(walk.JsonValues > 0, "The fixture must exercise retained JSON metadata.");
    }

    private sealed class MetadataWalk
    {
        private readonly HashSet<object> seen = new(ReferenceEqualityComparer.Instance);
        internal int JsonValues;

        internal void Visit(object? value)
        {
            if (value is null || !seen.Add(value)) { return; }
            switch (value)
            {
                case JsonElement:
                    Assert.Fail("Importer metadata must not expose raw JsonElement equality."); break;
                case SceneJsonValue2D json:
                    JsonValues++;
                    string raw = json.Value.GetRawText();
                    using (JsonDocument parsed = JsonDocument.Parse(raw)) { Assert.Equal(raw, parsed.RootElement.GetRawText()); }
                    break;
                case Scene2DDocument document:
                    Visit(document.Properties); Visit(document.Levels); break;
                case Scene2DLevel level:
                    Visit(level.Properties); Visit(level.TileSets); Visit(level.TileMaps); Visit(level.Entities); Visit(level.Promotions); break;
                case TileMap2DModel map:
                    Visit(map.Properties); Visit(map.TileSets); Visit(map.Chunks); Visit(map.Tiles); break;
                case TileSet2D set:
                    Visit(set.Properties); Visit(set.Tiles); break;
                case TileDefinition2D tile:
                    Visit(tile.Properties); Visit(tile.Collider); break;
                case TileChunk2D chunk:
                    Visit(chunk.Properties); break;
                case Tile placement:
                    Visit(placement.Collider); break;
                case TileColliderDescriptor2D collider:
                    Visit(collider.Properties); break;
                case Scene2DEntity entity:
                    Visit(entity.Properties); Visit(entity.Collider); break;
                case TilePromotion2D promotion:
                    Visit(promotion.Properties); break;
                case IReadOnlyDictionary<string, object?> properties:
                    foreach (object? item in properties.Values) { Visit(item); }
                    break;
                case IReadOnlyList<object?> items:
                    foreach (object? item in items) { Visit(item); }
                    break;
                case string or bool or int or long or uint or ulong or float or double or Color or DrawPoint or IReadOnlyList<int>:
                    break;
                default:
                    Assert.Fail("The fixture contains an uncharacterized metadata value."); break;
            }
        }
    }

    private static string FixtureRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx"))) { directory = directory.Parent; }
        return Path.Combine(directory?.FullName ?? throw new InvalidOperationException("Repository root not found."),
            "tests", "Fixtures", "Scene2DImport");
    }
}
