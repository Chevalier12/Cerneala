using System.Reflection;
using Cerneala.Scene2D.Packages;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.Scene2DPackages;

public sealed class Scene2DPublicBoundaryContractTests
{
    [Fact]
    public void Stage0_PublicSpatialSourceAndResidencyFamilyIsGone()
    {
        Assembly core = typeof(TileMap2D).Assembly;
        string[] removedTypes =
        [
            "SceneSpatialEntry2D", "SceneSpatialLease2D`1", "ISceneSpatialSource2D`1",
            "SceneSpatialSource2D`1", "SceneSpatialResidency2D`1", "SceneSpatialRegion2D`1",
            "TileMapSource2D", "TileMapCatalog2D", "TileMapChunkInfo2D"
        ];
        foreach (string name in removedTypes)
        {
            Type? type = core.GetType("Cerneala.UI.Controls." + name, throwOnError: false);
            Assert.True(type is null || !type.IsVisible, $"{name} is still exported by the core assembly.");
        }
    }

    [Fact]
    public void Stage2_NonspatialTileMapChunkDataRemainsPublic()
    {
        // A decoded chunk payload is not a spatial source, catalog, entry, lease, or residency owner.
        // Retaining this pre-cutover value type is the narrower approved API break.
        Assert.True(typeof(TileMapChunkData2D).IsPublic);
    }

    [Fact]
    public void Stage0_SceneItemsDropsSpatialPreparationAndIdMembers()
    {
        Type items = typeof(SceneItems2D);
        Assert.DoesNotContain(items.GetMethods(BindingFlags.Public | BindingFlags.Instance),
            static method => method.Name == "TryGetRealizedNode");
        Assert.Null(items.GetProperty("Preparation", BindingFlags.Public | BindingFlags.Instance));
        Assert.Null(items.GetProperty("PreparationError", BindingFlags.Public | BindingFlags.Instance));
    }

    [Fact]
    public void Stage0_MapAndPackageExposeIdsAndValuesInsteadOfSourcesAndLeases()
    {
        Type level = typeof(Scene2DPackageLevel);
        Assert.Equal(typeof(IReadOnlyList<string>), level.GetProperty("TileMapIds")?.PropertyType);
        Assert.Equal(typeof(TileMap2D), PublicMethod(level, "CreateTileMap", typeof(string)).ReturnType);
        Assert.Null(level.GetProperty("TileMaps", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        Assert.Null(level.GetMethod("CreateEntitySource", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        Assert.Null(typeof(TileMap2D).GetProperty("Source", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        Assert.Null(typeof(TileMap2D).GetField("SourceProperty", BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly));

        Assert.Equal(typeof(ValueTask<Scene2DPackageMetadata>),
            PublicMethod(typeof(Scene2DPackage), "LoadMetadataAsync", typeof(CancellationToken)).ReturnType);
        Assert.Equal(typeof(ValueTask<Scene2DPackageMetadata>),
            PublicMethod(level, "LoadMetadataAsync", typeof(CancellationToken)).ReturnType);
        Assert.Equal(typeof(ValueTask<Scene2DPackageMetadata>),
            PublicMethod(level, "LoadMapMetadataAsync", typeof(string), typeof(CancellationToken)).ReturnType);
        Assert.Equal(typeof(ValueTask<TileMap2DModel>),
            PublicMethod(level, "LoadMapModelAsync", typeof(string), typeof(CancellationToken)).ReturnType);
        Assert.Equal(typeof(ValueTask<Scene2DEntity>),
            PublicMethod(level, "LoadEntityAsync", typeof(string), typeof(CancellationToken)).ReturnType);
        Assert.Equal(typeof(ValueTask<TilePromotion2D>),
            PublicMethod(level, "LoadPromotionAsync", typeof(TileCellKey2D), typeof(CancellationToken)).ReturnType);
        Assert.True(typeof(IAsyncDisposable).IsAssignableFrom(typeof(Scene2DPackage)));
        Assert.True(typeof(IAsyncDisposable).IsAssignableFrom(typeof(TileMap2D)));
        Assert.Equal(typeof(TileMap2D), PublicMethod(typeof(TileMap2D), "FromModel",
            typeof(TileMap2DModel), typeof(IReadOnlyDictionary<string, Cerneala.Drawing.DrawSize>)).ReturnType);
    }

    [Fact]
    public void Stage0_Cpv2RangeReaderHasTheSelectedNonspatialTransportShape()
    {
        Assembly packages = typeof(Scene2DPackage).Assembly;
        Type? part = packages.GetType("Cerneala.Scene2D.Packages.Scene2DPackagePart", throwOnError: false);
        Type? reader = packages.GetType("Cerneala.Scene2D.Packages.IScene2DPackageRangeReader", throwOnError: false);
        Assert.NotNull(part);
        Assert.NotNull(reader);
        Assert.True(part.IsPublic && part.IsEnum);
        Assert.Equal(new[] { "Catalog", "Payloads" }, Enum.GetNames(part));
        Assert.True(reader.IsPublic && reader.IsInterface);
        Assert.True(typeof(IAsyncDisposable).IsAssignableFrom(reader));
        Assert.Equal(typeof(ValueTask<long>), PublicMethod(reader, "GetLengthAsync",
            part, typeof(CancellationToken)).ReturnType);
        Assert.Equal(typeof(ValueTask<int>), PublicMethod(reader, "ReadAsync",
            part, typeof(long), typeof(Memory<byte>), typeof(CancellationToken)).ReturnType);
        Assert.Equal(typeof(Task<Scene2DPackage>), PublicMethod(typeof(Scene2DPackage), "OpenAsync",
            reader, typeof(Scene2DPackageReadOptions), typeof(CancellationToken)).ReturnType);
        Assert.Equal(typeof(Task<Scene2DPackage>), PublicMethod(typeof(Scene2DPackage), "OpenAsync",
            typeof(string), typeof(Scene2DPackageReadOptions), typeof(CancellationToken)).ReturnType);
    }

    private static MethodInfo PublicMethod(Type owner, string name, params Type[] parameterTypes) =>
        owner.GetMethod(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static,
            binder: null, types: parameterTypes, modifiers: null)
        ?? throw new Xunit.Sdk.XunitException($"Missing public {owner.FullName}.{name}({string.Join(", ", parameterTypes.Select(static type => type.Name))}).");
}
