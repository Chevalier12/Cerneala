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
    public void LiveColliderOwnerMarkupRejectsASecondCollider()
    {
        const string markup = "<Scene2D><Sprite2D><BoxCollider2D /><CircleCollider2D /></Sprite2D></Scene2D>";
        GeneratorRunResult result = RunGenerator([new MarkupFile("Ownership.crn", markup)], out _, "");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI005" &&
            diagnostic.GetMessage().Contains("one collider", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void FreePlacementTileMarkupRejectsASecondCollider()
    {
        const string markup = """
            <Scene2D xmlns:resources="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala">
                <Scene2D.Resources>
                    <resources:ImageResource Name="Wall" Source="Assets/wall-32.png" />
                </Scene2D.Resources>
                <TileMap2D>
                    <Tile Image="$Wall">
                        <BoxCollider2D />
                        <CircleCollider2D />
                    </Tile>
                </TileMap2D>
            </Scene2D>
            """;
        GeneratorRunResult result = RunGenerator([new MarkupFile("Ownership.crn", markup)], out _, "");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI005" &&
            diagnostic.GetMessage().Contains("one collider", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(result.GeneratedSources);
    }

    [Theory]
    [InlineData("BoxCollider2D")]
    [InlineData("CircleCollider2D")]
    [InlineData("PolygonCollider2D")]
    [InlineData("SegmentCollider2D")]
    public void SpriteMarkupOwnsEachColliderShapeThroughItsSingularSlot(string shape)
    {
        string markup = $$"""
            <Scene2D>
                <Sprite2D X="32" Y="64" Width="32" Height="32">
                    <{{shape}} />
                </Sprite2D>
            </Scene2D>
            """;
        Scene2D scene = (Scene2D)CompileColliderMarkup(markup);
        Sprite2D sprite = Assert.IsType<Sprite2D>(Assert.Single(scene.Children));
        Collider2D collider = Assert.IsAssignableFrom<Collider2D>(Assert.Single(sprite.LogicalChildren));
        Assert.Equal(shape, collider.GetType().Name);
        Assert.Same(collider, sprite.Collider);
        Assert.Same(sprite, collider.LogicalParent);
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
    public void FreePlacementTileMarkupLowersOneColliderToAnImmutableDescriptor()
    {
        const string markup = """
            <Scene2D xmlns:resources="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala">
                <Scene2D.Resources>
                    <resources:ImageResource Name="Wall" Source="Assets/wall-32.png" />
                </Scene2D.Resources>
                <TileMap2D>
                    <Tile Image="$Wall" X="32" Y="64" Width="64" Height="64">
                        <BoxCollider2D Width="32" Height="32" CollisionLayer="2" CollisionMask="1" />
                    </Tile>
                </TileMap2D>
            </Scene2D>
            """;
        Scene2D scene = (Scene2D)CompileColliderMarkup(markup);
        TileMap2D map = (TileMap2D)Assert.Single(scene.Children);
        Tile tile = Assert.Single(map.Model!.Tiles);
        TileColliderDescriptor2D descriptor = Assert.IsType<TileColliderDescriptor2D>(tile.Collider);
        Assert.Equal(TileColliderShape2D.Box, descriptor.Shape);
        Assert.Equal(32, descriptor.Width);
        Assert.Equal(2u, descriptor.CollisionLayer);
        Assert.Equal(1u, descriptor.CollisionMask);
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
        TileColliderDescriptor2D descriptor = Assert.IsType<TileColliderDescriptor2D>(Assert.Single(map.Model!.Tiles).Collider);
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
