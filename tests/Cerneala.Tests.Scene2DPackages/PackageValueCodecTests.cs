using System.Numerics;
using System.Text.Json;
using Cerneala.Drawing;
using Cerneala.Scene2D.Packages;
using Cerneala.UI.Controls;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Scene2DPackages;

public sealed class PackageValueCodecTests
{
    [Fact]
    public void AuthoredChunkMetadataUsesExplicitTagsAndEarlierMetadataStillDecodes()
    {
        byte[] earlier = Convert.FromHexString("435056321C0D0000000000000000");
        var old = Assert.IsType<Scene2DPackageMetadata>(PackageValueCodec.Decode(earlier));
        Assert.Empty(old.GridChunks);
        Assert.Equal(earlier, PackageValueCodec.Encode(old));
        byte[] bulk = new byte[4096];
        Dictionary<string, object?> properties = new() { ["shared"] = bulk };
        Scene2DPackageMetadata input = new(properties, [],
            [new(new(-20, 30, 64, 32), 7, properties), new(new(100, 30, 8, 8), 9, properties)]);
        byte[] encoded = PackageValueCodec.Encode(input, out long charge);
        var result = Assert.IsType<Scene2DPackageMetadata>(PackageValueCodec.Decode(encoded));
        Assert.Equal(encoded, PackageValueCodec.Encode(result, out long roundTrip));
        Assert.Equal(charge, roundTrip);
        Assert.True(charge >= bulk.Length);
        Assert.Equal(new TileMapBounds2D(-20, 30, 64, 32), result.GridChunks[0].Cells);
        Assert.Equal(9, result.GridChunks[1].Version);
        Assert.Same(result.Properties["shared"], result.GridChunks[0].Properties["shared"]);
        Assert.Same(result.Properties["shared"], result.GridChunks[1].Properties["shared"]);
        for (int length = 0; length < encoded.Length; length++)
        {
            Assert.Throws<InvalidDataException>(() => PackageValueCodec.Decode(encoded[..length]));
        }
        Assert.Throws<NotSupportedException>(() => ((IList<Scene2DPackageGridChunkMetadata>)result.GridChunks).Clear());
    }

    [Fact]
    public void ChargeDoesNotChangeWireBytesAndCountsSharedPayloadsOnlyOnce()
    {
        byte[] bytes = [1, 2, 3];
        object?[] shared = [bytes, bytes];
        byte[] encoded = PackageValueCodec.Encode(shared, out long first);
        Assert.Equal(Convert.FromHexString("4350563211020000000E030000000102030C00000000"), encoded);
        Assert.Equal(encoded, PackageValueCodec.Encode(shared, out long repeated));
        Assert.Equal(first, repeated);
        object?[] decoded = Assert.IsType<object?[]>(PackageValueCodec.Decode(encoded));
        Assert.Equal(encoded, PackageValueCodec.Encode(decoded, out long roundTrip));
        Assert.Equal(first, roundTrip);
        Assert.Same(decoded[0], decoded[1]);
        Assert.Equal(Charge(bytes), Charge(new object?[] { bytes, new byte[] { 1, 2, 3 } }) - first);

        // Strings and boxed primitives are wire values, not shared references.
        object number = 123;
        string text = new('x', 100);
        Assert.Equal(Charge(new object?[] { 123, 123 }), Charge(new object?[] { number, number }));
        Assert.Equal(Charge(new object?[] { text, new string('x', 100) }), Charge(new object?[] { text, text }));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4096)]
    public void CollectionChargeIncludesRetainedElementStorage(int count)
    {
        Assert.True(Charge(new byte[count]) - Charge(Array.Empty<byte>()) >= count);
        Assert.True(Charge(new int[count]) - Charge(Array.Empty<int>()) >= count * 4L);
        Assert.True(Charge(new object?[count]) - Charge(Array.Empty<object?>()) >= count * 8L);
        var values = Enumerable.Range(0, count).ToDictionary(index => index.ToString(), _ => (object?)null);
        Assert.True(Charge(values) - Charge(new Dictionary<string, object?>()) >= count * 24L);
    }

    [Fact]
    public void JsonChargeIncludesStructuralRowsNotJustTextLength()
    {
        string array = "[" + string.Join(',', Enumerable.Repeat("0", 1000)) + "]";
        string scalar = "\"" + new string('x', array.Length - 2) + "\"";
        using JsonDocument structured = JsonDocument.Parse(array);
        using JsonDocument text = JsonDocument.Parse(scalar);
        SceneJsonValue2D json = new(structured.RootElement);
        Assert.True(Charge(json) - Charge(new SceneJsonValue2D(text.RootElement)) >= 1000 * 12L);
        Assert.Equal(Charge(json), Charge(new object?[] { json, new SceneJsonValue2D(structured.RootElement) }) -
            Charge(new object?[] { json, json }));
    }

    [Fact]
    public void ColliderChargeIncludesParsedVerticesBesidesTheirAuthoringString()
    {
        const string points = "0,0 16,0 16,8 0,8";
        TileColliderDescriptor2D polygon = new(TileColliderShape2D.Polygon, points: points);
        TileColliderDescriptor2D box = new(TileColliderShape2D.Box, points: points);
        Assert.True(Charge(polygon) - Charge(box) >= polygon.Vertices.Count * 8L);
        Assert.Equal(polygon.Vertices, Assert.IsType<TileColliderDescriptor2D>(RoundTrip(polygon)).Vertices);
    }

    [Fact]
    public void ScalarsKeepTheirExactTypesAndValues()
    {
        object?[] values = [null, true, false, int.MinValue, long.MaxValue, uint.MaxValue,
            ulong.MaxValue, 0.25f, 0.25d, "\0română\ud800", new Color(1, 2, 3, 4), new DrawPoint(-5, 7)];
        foreach (object? value in values)
        {
            object? decoded = RoundTrip(value);
            Assert.Equal(value?.GetType(), decoded?.GetType());
            Assert.Equal(value, decoded);
        }
        foreach (int bits in new[] { int.MinValue, 0x7fc12345, 0x7f800000 })
        {
            Assert.Equal(bits, BitConverter.SingleToInt32Bits(Assert.IsType<float>(RoundTrip(BitConverter.Int32BitsToSingle(bits)))));
        }
    }

    [Fact]
    public void ObjectGraphsRetainSharingWithoutMergingEqualButDistinctValues()
    {
        byte[] bytes = [1, 2, 3];
        var ints = Array.AsReadOnly(new[] { 1, -2, int.MaxValue });
        object?[] values = [bytes, bytes, new byte[] { 1, 2, 3 }, ints, ints, null];
        var result = Assert.IsType<object?[]>(RoundTrip(values));
        Assert.Same(result[0], result[1]);
        Assert.NotSame(result[0], result[2]);
        Assert.NotSame(bytes, result[0]);
        Assert.Equal(bytes, Assert.IsType<byte[]>(result[0]));
        Assert.Same(result[3], result[4]);
        Assert.Equal(ints, Assert.IsAssignableFrom<IReadOnlyList<int>>(result[3]));
        Assert.Null(result[5]);
        var list = Assert.IsAssignableFrom<IReadOnlyList<object?>>(RoundTrip(Array.AsReadOnly(values)));
        Assert.Throws<NotSupportedException>(() => ((IList<object?>)list).Add(1));
    }

    [Fact]
    public void GridPalettesRetainPrototypeMetadataEqualityAndAllCellBits()
    {
        byte[] shared = new byte[65_536];
        Dictionary<string, object?> properties = new() { ["bulk"] = shared, ["layer"] = 4u };
        TileColliderDescriptor2D first = new(TileColliderShape2D.Box, width: 16, height: 16, properties: properties);
        TileColliderDescriptor2D second = new(TileColliderShape2D.Box, width: 16, height: 16, properties: properties);
        TileCell2D[] cells = Enumerable.Range(0, 8).Select(index => new TileCell2D(index % 2 + 1, (TileFlip2D)index)).ToArray();
        TileMapChunkData2D input = new(new(new(-20, 40), 4, 2, cells, 17, properties),
            [new("a", new("atlas"), [new(1, new(0, 0, 16, 16), collider: first), new(2, new(16, 0, 16, 16), collider: second)], 9, properties)]);
        var decoded = Assert.IsType<TileMapChunkData2D>(RoundTrip(input));
        Assert.Equal(cells, decoded.Grid!.Tiles);
        Assert.Equal(input.Grid!.Origin, decoded.Grid.Origin);
        Assert.Equal(17, decoded.Grid.Version);
        Assert.Equal(9, decoded.TileSets[0].Version);
        Assert.Equal("atlas", decoded.TileSets[0].AtlasResourceId.Key);
        Assert.True(decoded.TryResolveTile(2, out _, out var definition));
        Assert.Equal(new DrawRect(16, 0, 16, 16), definition!.SourceRect);
        object? sharedCopy = decoded.Grid.Properties["bulk"];
        Assert.NotSame(shared, sharedCopy);
        Assert.Same(sharedCopy, decoded.TileSets[0].Properties["bulk"]);
        Assert.Same(sharedCopy, decoded.TileSets[0].Tiles[0].Collider!.Properties["bulk"]);
        Assert.Same(sharedCopy, decoded.TileSets[0].Tiles[1].Collider!.Properties["bulk"]);
        Assert.Equal(PackageValueCodec.Encode(input), PackageValueCodec.Encode(decoded));
        Assert.InRange(Charge(input), shared.Length, 2L * shared.Length - 1);
        Assert.Equal(Charge(input), Charge(decoded));
    }

    [Theory]
    [InlineData(TileColliderShape2D.Box, "0,0 1,0 0,1")]
    [InlineData(TileColliderShape2D.Circle, "0,0 1,0 0,1")]
    [InlineData(TileColliderShape2D.Polygon, "0,0 16,0 0,8")]
    [InlineData(TileColliderShape2D.Segment, "0,0 16,8")]
    public void ColliderGeometryAndFiltersRoundTrip(TileColliderShape2D shape, string points)
    {
        TileColliderDescriptor2D input = new(shape, new Matrix3x2(1, 0.5f, 0, 2, 3, 4),
            width: 5, height: 6, radius: 7, points: points, offsetX: -1, offsetY: 2,
            collisionLayer: 3, collisionMask: 7, isTrigger: true, debugIdentity: "wall");
        var decoded = Assert.IsType<TileColliderDescriptor2D>(RoundTrip(input));
        Assert.Equal(PackageValueCodec.Encode(input), PackageValueCodec.Encode(decoded));
        Assert.Equal(input.Vertices, decoded.Vertices);
        Assert.Equal(input.LocalTransform, decoded.LocalTransform);
    }

    [Fact]
    public void FreePlacementAndEntityPayloadsRetainAuthoringData()
    {
        TileColliderDescriptor2D collider = new(TileColliderShape2D.Circle, radius: 3);
        TileMapChunkData2D input = new([new(new ImageReference(new ResourceId<ImageResource>("atlas")), collider, -5, 7),
            new(new ImageReference(new ResourceId<ImageResource>("atlas")), x: 9, width: 4, height: 5)]);
        var decoded = Assert.IsType<TileMapChunkData2D>(RoundTrip(input));
        Assert.Null(decoded.Grid);
        Assert.Equal(2, decoded.Placements.Count);
        Assert.True(float.IsNaN(decoded.Placements[0].Width));
        Scene2DEntity entity = new("e", "map", new(4, 5), new(6, 7), "Ellipse", rotation: 0.5f,
            pivot: new(1, 2), role: "Spawn", collider: collider, order: -2, isVisible: false, opacity: 0.25f,
            properties: new Dictionary<string, object?> { ["state"] = "Idle" });
        Assert.Equal(PackageValueCodec.Encode(entity), PackageValueCodec.Encode(RoundTrip(entity)));
        TilePromotion2D promotion = new(new("map", 1, 2), 7, new Dictionary<string, object?> { ["entity"] = entity });
        Assert.Equal(PackageValueCodec.Encode(promotion), PackageValueCodec.Encode(RoundTrip(promotion)));
        Assert.Equal(PackageValueCodec.Encode(input), PackageValueCodec.Encode(decoded));
    }

    [Fact]
    public void TruncatedPayloadsAndCorruptHeadersAreRejected()
    {
        byte[] encoded = PackageValueCodec.Encode(new Scene2DEntity("e", "map", default, default));
        for (int length = 0; length < encoded.Length; length++)
        {
            Assert.Throws<InvalidDataException>(() => PackageValueCodec.Decode(encoded[..length]));
        }
        Assert.Throws<InvalidDataException>(() => PackageValueCodec.Decode([.. encoded, 0]));
        encoded[0] ^= 1;
        Assert.Throws<InvalidDataException>(() => PackageValueCodec.Decode(encoded));
        foreach (int count in new[] { -1, int.MaxValue })
        {
            byte[] stringData = PackageValueCodec.Encode("x");
            BitConverter.GetBytes(count).CopyTo(stringData, 5);
            Assert.Throws<InvalidDataException>(() => PackageValueCodec.Decode(stringData));
        }
        byte[] badBoolean = PackageValueCodec.Encode(true);
        badBoolean[5] = 2;
        Assert.Throws<InvalidDataException>(() => PackageValueCodec.Decode(badBoolean));
    }

    [Fact]
    public void CyclesAndUnsupportedClrObjectsFailExplicitly()
    {
        Dictionary<string, object?> cycle = new();
        cycle.Add("self", cycle);
        Assert.Throws<NotSupportedException>(() => PackageValueCodec.Encode(cycle));
        Assert.Throws<NotSupportedException>(() => PackageValueCodec.Encode(new object()));
        object nested = new object?[0];
        for (int index = 0; index < 130; index++) { nested = new object?[] { nested }; }
        Assert.Throws<NotSupportedException>(() => PackageValueCodec.Encode(nested));
    }

    [Fact]
    public void ExplicitJsonMetadataRetainsContentAndSharingButNotContentBasedEquality()
    {
        using JsonDocument document = JsonDocument.Parse("{\"n\":1.0,\"s\":\"română\",\"items\":[null,true]}");
        SceneJsonValue2D shared = new(document.RootElement);
        SceneJsonValue2D distinct = new(document.RootElement);
        object?[] input = [shared, shared, distinct];
        var result = Assert.IsType<object?[]>(RoundTrip(input));
        Assert.Same(result[0], result[1]);
        Assert.NotSame(result[0], result[2]);
        Assert.False(Equals(result[0], result[2]));
        Assert.Equal(document.RootElement.GetRawText(), Assert.IsType<SceneJsonValue2D>(result[0]).Value.GetRawText());
        Assert.Equal(PackageValueCodec.Encode(input), PackageValueCodec.Encode(result));
        Assert.Throws<NotSupportedException>(() => PackageValueCodec.Encode(document.RootElement));
        using JsonDocument deep = JsonDocument.Parse(new string('[', 129) + "0" + new string(']', 129),
            new JsonDocumentOptions { MaxDepth = 129 });
        Assert.Throws<NotSupportedException>(() => PackageValueCodec.Encode(new SceneJsonValue2D(deep.RootElement)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task JsonIdentityKeepsCollisionCoalescingAcrossThePayloadRoundTrip(bool shared)
    {
        using JsonDocument document = JsonDocument.Parse("[1,2,3]");
        SceneJsonValue2D first = new(document.RootElement);
        SceneJsonValue2D second = shared ? first : new(document.RootElement);
        TileDefinition2D Definition(int id, SceneJsonValue2D json) => new(id, new(0, 0, 10, 10),
            collider: new(TileColliderShape2D.Box, width: 10, height: 10,
                properties: new Dictionary<string, object?> { ["json"] = json }));
        TileMap2DModel model = new("map", new(10, 10), [new("set", new("atlas"), [Definition(1, first), Definition(2, second)])],
            [new(default, 2, 1, [new(1), new(2)])]);
        TileMapSource2D backing = TileMapSource2D.FromModel(model);
        using var input = await backing.LoadAsync(backing.Entries[0]);
        byte[] encoded = PackageValueCodec.Encode(input.Value);
        TileMap2D map = new() { Source = new(backing.Catalog, (_, _, _) =>
            ValueTask.FromResult(new SceneSpatialLease2D<TileMapChunkData2D>((TileMapChunkData2D)PackageValueCodec.Decode(encoded)!))) };
        var scene = new global::Cerneala.UI.Controls.Scene2D();
        scene.Children.Add(map);
        using SceneSimulationContext2D context = new(scene);
        var request = scene.CollisionWorld.PrepareRegionAsync(new(0, -10, 20, 30)).AsTask();
        Assert.True(SpinWait.SpinUntil(() => { context.Update(); return request.IsCompleted; }, TimeSpan.FromSeconds(5)));
        using var region = await request;
        Assert.Equal(shared ? 1 : 2, map.LogicalChildren.Count);
        Assert.Single(scene.CollisionWorld.Raycast(new(5, -5), Vector2.UnitY, 20));
        Assert.Single(scene.CollisionWorld.Raycast(new(15, -5), Vector2.UnitY, 20));
        region.Dispose();
        context.Update();
        Assert.Empty(map.LogicalChildren);
    }

    [Fact]
    public void CodecMustNotUseJsonElementDefaultEqualityOrRuntimeTypeDiscovery()
    {
        string source = File.ReadAllText(Path.Combine(RepoRoot(), "Cerneala.Scene2D.Packages", "PackageValueCodec.cs"));
        Assert.DoesNotContain("Dictionary<JsonElement", source);
        Assert.DoesNotContain(".GetType()", source);
        Assert.DoesNotContain("System.Reflection", source);
        Assert.DoesNotContain("JsonSerializer", source);
    }

    private static object? RoundTrip(object? value) => PackageValueCodec.Decode(PackageValueCodec.Encode(value));

    private static long Charge(object? value)
    {
        PackageValueCodec.Encode(value, out long bytes);
        return bytes;
    }

    private static string RepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx"))) { directory = directory.Parent; }
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
