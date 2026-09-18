using System.Text.Json.Nodes;
using Cerneala.Scene2D.Importers;

namespace Cerneala.Tests.Scene2DImporters;

public sealed class ReferencedFileContractTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReferencesAreExplicitNormalizedUniqueAndDetachedFromTheParser(bool ldtk)
    {
        using Fixture fixture = new(ldtk);
        fixture.Write("notes/script.txt", "This mentions missing.dat but is not parsed.");
        fixture.AddProperty("Script", "notes/script.txt");
        fixture.AddProperty("Alias", "notes/../notes/script.txt");
        fixture.AddProperty("Empty", "");
        fixture.AddProperty("Text", "missing.dat", isFile: false);

        Scene2DImportResult result = fixture.Import();
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        Assert.Equal(fixture.Root, result.AssetRootDirectory);
        Assert.Equal(new[] { "atlas.svg", "notes/script.txt" }, result.ReferencedFiles);
        Assert.Equal("notes/script.txt", Assert.Single(result.Document!.Levels).Properties["Script"]);
        Assert.Equal("missing.dat", Assert.Single(result.Document.Levels).Properties["Text"]);
        Assert.Single(result.Document.Assets);
        Assert.Throws<NotSupportedException>(() => ((IList<string>)result.ReferencedFiles)[0] = "changed");
        fixture.Write("later.txt", "later");
        fixture.AddProperty("Later", "later.txt");
        Assert.Equal(3, fixture.Import().ReferencedFiles.Count);
        Assert.Equal(new[] { "atlas.svg", "notes/script.txt" }, result.ReferencedFiles);
    }

    [Fact]
    public void ExternalTiledTilesetReferencesRemainRelativeToTheConfiguredAssetRoot()
    {
        using Fixture fixture = new(false);
        fixture.Write("notes/script.txt", "data");
        JsonObject set = fixture.Source["tilesets"]![0]!.DeepClone().AsObject();
        set.Remove("firstgid");
        set["version"] = "1.11";
        set["image"] = "../atlas.svg";
        set["properties"] = new JsonArray(new JsonObject
        {
            ["name"] = "Script", ["type"] = "file", ["value"] = "../notes/script.txt"
        });
        fixture.Write("sets/tiles.tsj", set.ToJsonString());
        fixture.Source["tilesets"] = new JsonArray(new JsonObject
        {
            ["firstgid"] = 1, ["source"] = "sets/tiles.tsj"
        });

        Scene2DImportResult result = fixture.Import();
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        Assert.Equal(new[] { "atlas.svg", "notes/script.txt" }, result.ReferencedFiles);
        Assert.Equal("notes/script.txt", Assert.Single(result.Document!.Levels[0].TileSets).Properties["Script"]);
    }

    [Fact]
    public void ExternalLdtkLevelFileFieldsStillResolveRelativeToTheProject()
    {
        using Fixture fixture = new(true);
        fixture.Write("notes/script.txt", "data");
        fixture.AddProperty("Script", "notes/script.txt");
        JsonObject level = fixture.Source["levels"]![0]!.AsObject();
        fixture.Write("levels/level.ldtkl", level.ToJsonString());
        level["layerInstances"] = null;
        level["externalRelPath"] = "levels/level.ldtkl";
        fixture.Source["externalLevels"] = true;

        Scene2DImportResult result = fixture.Import();
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        Assert.Equal(new[] { "atlas.svg", "notes/script.txt" }, result.ReferencedFiles);
        Assert.Equal("notes/script.txt", Assert.Single(result.Document!.Levels).Properties["Script"]);
    }

    [Theory]
    [InlineData(false, "missing.dat", "SCN2D001")]
    [InlineData(true, "missing.dat", "SCN2D001")]
    [InlineData(false, "../outside.dat", "SCN2D010")]
    [InlineData(true, "../outside.dat", "SCN2D010")]
    public void InvalidReferencesNeverPublishAPartialDependencyList(bool ldtk, string path, string code)
    {
        using Fixture fixture = new(ldtk);
        fixture.AddProperty("Good", "atlas.svg");
        fixture.AddProperty("Bad", path);
        Scene2DImportResult result = fixture.Import();
        Assert.False(result.Success);
        Assert.Null(result.Document);
        Assert.Null(result.AssetRootDirectory);
        Assert.Empty(result.ReferencedFiles);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == code);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ContentFailureAfterResolvedReferencesIsAlsoAtomic(bool ldtk)
    {
        using Fixture fixture = new(ldtk);
        fixture.AddProperty("Good", "atlas.svg");
        JsonObject layer = ldtk
            ? fixture.Source["levels"]![0]!["layerInstances"]![0]!.AsObject()
            : fixture.Source["layers"]![0]!.AsObject();
        layer["unsupportedRuntimeField"] = true;
        Scene2DImportResult result = fixture.Import();
        Assert.False(result.Success);
        Assert.Null(result.AssetRootDirectory);
        Assert.Empty(result.ReferencedFiles);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "SCN2D004");
    }

    private sealed class Fixture : IDisposable
    {
        private readonly bool ldtk;
        private readonly string artifactsRoot;
        private int nextFieldId = 100;

        public Fixture(bool ldtk)
        {
            this.ldtk = ldtk;
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
                directory = directory.Parent;
            Assert.NotNull(directory);
            string fixtures = Path.Combine(directory.FullName, "tests", "Fixtures", "Scene2DImport");
            artifactsRoot = Path.Combine(directory.FullName, ".artifacts");
            Root = Path.Combine(artifactsRoot, "scene-import-files-" + Guid.NewGuid().ToString("N"));
            Source = JsonNode.Parse(File.ReadAllText(Path.Combine(fixtures,
                ldtk ? "ldtk-inline.ldtk" : "tiled-finite.tmj")))!.AsObject();
            Write("atlas.svg", File.ReadAllText(Path.Combine(fixtures, "atlas.svg")));
        }

        public string Root { get; }
        public JsonObject Source { get; }

        public void Write(string relative, string content)
        {
            string path = Path.Combine(Root, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
        }

        public void AddProperty(string name, string value, bool isFile = true)
        {
            if (!ldtk)
            {
                Source["properties"] ??= new JsonArray();
                Source["properties"]!.AsArray().Add(new JsonObject
                {
                    ["name"] = name, ["type"] = isFile ? "file" : "string", ["value"] = value
                });
                return;
            }
            int id = nextFieldId++;
            string kind = isFile ? "FilePath" : "String";
            Source["defs"]!["levelFields"]!.AsArray().Add(new JsonObject
            {
                ["__type"] = kind, ["type"] = isFile ? "F_Path" : "F_String", ["uid"] = id,
                ["identifier"] = name, ["isArray"] = false, ["canBeNull"] = false
            });
            Source["levels"]![0]!["fieldInstances"]!.AsArray().Add(new JsonObject
            {
                ["__identifier"] = name, ["__type"] = kind, ["__value"] = value, ["defUid"] = id
            });
        }

        public Scene2DImportResult Import()
        {
            string name = ldtk ? "project.ldtk" : "map.tmj";
            Write(name, Source.ToJsonString());
            return ldtk ? LdtkScene2DImporter.Import(Path.Combine(Root, name))
                : TiledScene2DImporter.Import(Path.Combine(Root, name));
        }

        public void Dispose()
        {
            string resolved = Path.GetFullPath(Root);
            Assert.Equal(Path.GetFullPath(artifactsRoot), Path.GetDirectoryName(resolved));
            Assert.StartsWith("scene-import-files-", Path.GetFileName(resolved), StringComparison.Ordinal);
            Directory.Delete(resolved, recursive: true);
        }
    }
}
