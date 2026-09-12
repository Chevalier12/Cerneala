using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    public void SpriteMarkupOwnsAllFourColliderShapesAsLogicalChildren()
    {
        const string markup = """
            <Scene2D>
                <Sprite2D X="32" Y="64" Width="32" Height="32">
                    <BoxCollider2D Width="32" Height="32" />
                    <CircleCollider2D Radius="8" OffsetX="16" OffsetY="16" />
                    <PolygonCollider2D Points="0,0 32,0 0,32" />
                    <SegmentCollider2D EndX="32" />
                </Sprite2D>
            </Scene2D>
            """;
        Scene2D scene = (Scene2D)CompileColliderMarkup(markup);
        Sprite2D sprite = Assert.IsType<Sprite2D>(Assert.Single(scene.Children));
        Assert.Equal(new[] { typeof(BoxCollider2D), typeof(CircleCollider2D), typeof(PolygonCollider2D), typeof(SegmentCollider2D) },
            sprite.LogicalChildren.Select(child => child.GetType()));
        Assert.All(sprite.LogicalChildren, child => Assert.Same(sprite, child.LogicalParent));
        Assert.Empty(sprite.VisualChildren);
    }

    [Fact]
    public void SpriteOwnedColliderInPairedComponentKeepsNameBindingAndIndependentSize()
    {
        const string markup = """
            <Scene2D>
                <Sprite2D Name="Door" Width="32" Height="32">
                    <BoxCollider2D Name="DoorCollider" Width="$root.DoorX:OneWay" Height="32" />
                </Sprite2D>
            </Scene2D>
            """;
        string code = HouseComponentCode.Replace("public Sprite2D DoorNode => Door;",
            "public Sprite2D DoorNode => Door; public BoxCollider2D ColliderNode => DoorCollider;", StringComparison.Ordinal);
        Assembly assembly = CompileSceneComponent(markup, "<Scene2D />", code);
        Scene2D house = (Scene2D)Activator.CreateInstance(assembly.GetType("Game.HouseView")!)!;
        RenderSurface2D surface = new() { Scene = house };
        UIRoot root = new();
        root.VisualChildren.Add(surface);
        root.ProcessFrame();
        try
        {
            Sprite2D door = (Sprite2D)Assert.Single(house.Children);
            BoxCollider2D collider = (BoxCollider2D)house.GetType().GetProperty("ColliderNode")!.GetValue(house)!;
            Assert.Same(door, collider.LogicalParent);
            Assert.Equal(4, collider.Width);
            door.Width = 64;
            Assert.Equal(4, collider.Width);
            house.GetType().GetProperty("DoorX")!.SetValue(house, 12f);
            Assert.Equal(12, collider.Width);
        }
        finally { root.VisualChildren.Remove(surface); }
    }

    [Fact]
    public void FreePlacementTileMarkupLowersAllColliderShapesToImmutableDescriptors()
    {
        const string markup = """
            <Scene2D xmlns:resources="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala">
                <Scene2D.Resources>
                    <resources:ImageResource Name="Wall" Source="Assets/wall-32.png" />
                </Scene2D.Resources>
                <TileMap2D>
                    <Tile Image="$Wall" X="32" Y="64" Width="64" Height="64">
                        <BoxCollider2D Width="32" Height="32" CollisionLayer="2" CollisionMask="1" />
                        <CircleCollider2D Radius="8" OffsetX="16" OffsetY="16" IsTrigger="true" />
                        <PolygonCollider2D Points="0,0 32,0 0,32" />
                        <SegmentCollider2D EndX="32" EndY="8" />
                    </Tile>
                </TileMap2D>
            </Scene2D>
            """;
        Scene2D scene = (Scene2D)CompileColliderMarkup(markup);
        TileMap2D map = (TileMap2D)Assert.Single(scene.Children);
        Tile tile = Assert.Single(map.Model!.Tiles);
        PropertyInfo? property = typeof(Tile).GetProperty("Colliders");
        Assert.NotNull(property);
        var descriptors = Assert.IsAssignableFrom<System.Collections.Generic.IReadOnlyList<TileColliderDescriptor2D>>(property.GetValue(tile));
        Assert.Equal(new[] { TileColliderShape2D.Box, TileColliderShape2D.Circle, TileColliderShape2D.Polygon, TileColliderShape2D.Segment },
            descriptors.Select(item => item.Shape));
        Assert.Equal(32, descriptors[0].Width);
        Assert.Equal(2u, descriptors[0].CollisionLayer);
        Assert.Equal(1u, descriptors[0].CollisionMask);
        Assert.True(descriptors[1].IsTrigger);
        Assert.Equal(new System.Numerics.Vector2(32, 8), descriptors[3].Vertices[1]);
    }

    [Theory]
    [InlineData(" 0 ", " 8 ", 0f, 8f)]
    [InlineData(" 3.2e1 ", " +8 ", 32f, 8f)]
    public void StaticTileSegmentNormalizesNumericLiteralsBeforeConstructingPointText(string endX, string endY, float x, float y)
    {
        string markup = $$"""
            <Scene2D xmlns:resources="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala">
                <Scene2D.Resources>
                    <resources:ImageResource Name="Wall" Source="Assets/wall-32.png" />
                </Scene2D.Resources>
                <TileMap2D>
                    <Tile Image="$Wall">
                        <SegmentCollider2D EndX="{{endX}}" EndY="{{endY}}" />
                    </Tile>
                </TileMap2D>
            </Scene2D>
            """;
        Scene2D scene = (Scene2D)CompileColliderMarkup(markup);
        TileMap2D map = (TileMap2D)Assert.Single(scene.Children);
        TileColliderDescriptor2D descriptor = Assert.Single(Assert.Single(map.Model!.Tiles).Colliders);
        Assert.Equal(new System.Numerics.Vector2(x, y), descriptor.Vertices[1]);
    }

    [Theory]
    [InlineData("<BoxCollider2D />")]
    [InlineData("<Scene2D><BoxCollider2D /></Scene2D>")]
    [InlineData("<Scene2D><CircleCollider2D /></Scene2D>")]
    [InlineData("<Scene2D><PolygonCollider2D /></Scene2D>")]
    [InlineData("<Scene2D><SegmentCollider2D /></Scene2D>")]
    [InlineData("<StackPanel><BoxCollider2D /></StackPanel>")]
    [InlineData("<Border><BoxCollider2D /></Border>")]
    [InlineData("<Button><BoxCollider2D /></Button>")]
    [InlineData("<RenderSurface2D><BoxCollider2D /></RenderSurface2D>")]
    [InlineData("<Scene2D><BoxCollider2D IsTrigger=\"true\" Enabled=\"false\" /></Scene2D>")]
    public void ColliderMarkupRejectsEveryOtherOwner(string markup)
    {
        GeneratorRunResult result = RunGenerator([new MarkupFile("Ownership.crn", markup)], out _, "");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI005" &&
            diagnostic.GetMessage().Contains("Sprite2D", StringComparison.Ordinal));
        Assert.Empty(result.GeneratedSources);
    }

    private static UIElement CompileColliderMarkup(string markup)
    {
        GeneratorRunResult result = RunGenerator([new MarkupFile("Ownership.crn", markup)], out Compilation compilation, "");
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        var emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        Assembly assembly = Assembly.Load(stream.ToArray());
        return (UIElement)assembly.GetType("Cerneala.GeneratedUi.OwnershipFactory")!
            .GetMethod("Create", Type.EmptyTypes)!.Invoke(null, null)!;
    }
}
