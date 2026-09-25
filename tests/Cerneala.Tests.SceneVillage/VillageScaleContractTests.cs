using System.Reflection;
using Cerneala.Drawing;
using Cerneala.SceneVillage;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.SceneVillage;

public sealed class VillageScaleContractTests
{
    [Fact]
    public void SurfaceOwnsControllableZoomAndNullableInheritedContentScale()
    {
        PropertyInfo? zoom = typeof(VillageGameSurface).GetProperty("Zoom", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        PropertyInfo? contentScale = typeof(VillageGameSurface).GetProperty("ContentScale", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Assert.NotNull(zoom);
        Assert.NotNull(contentScale);
        Assert.Equal(typeof(float), zoom.PropertyType);
        Assert.Equal(typeof(float?), contentScale.PropertyType);

        var surface = new VillageGameSurface();
        Assert.Equal(1f, Assert.IsType<float>(zoom.GetValue(surface)));
        Assert.Null(contentScale.GetValue(surface));
        DrawRect initial = Assert.IsType<DrawRect>(surface.ViewBox);
        Assert.Equal(1200f, initial.Width);
        Assert.Equal(720f, initial.Height);

        zoom.SetValue(surface, 2f);
        DrawRect zoomed = Assert.IsType<DrawRect>(surface.ViewBox);
        Assert.Equal(initial.Width / 2f, zoomed.Width);
        Assert.Equal(initial.Height / 2f, zoomed.Height);
        Assert.Equal(initial.X + initial.Width / 2f, zoomed.X + zoomed.Width / 2f);

        contentScale.SetValue(surface, 2f);
        DrawRect scaled = Assert.IsType<DrawRect>(surface.ViewBox);
        Assert.Equal(initial.Width / 4f, scaled.Width);
        Assert.Equal(initial.Height / 4f, scaled.Height);

        contentScale.SetValue(surface, null);
        Assert.Equal(zoomed, Assert.IsType<DrawRect>(surface.ViewBox));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void InvalidCameraScaleValuesAreRejectedWithoutChangingTheView(float invalid)
    {
        var surface = new VillageGameSurface();
        PropertyInfo? zoom = typeof(VillageGameSurface).GetProperty("Zoom", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        PropertyInfo? contentScale = typeof(VillageGameSurface).GetProperty("ContentScale", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Assert.NotNull(zoom);
        Assert.NotNull(contentScale);
        DrawRect before = Assert.IsType<DrawRect>(surface.ViewBox);

        Assert.IsType<ArgumentOutOfRangeException>(Assert.Throws<TargetInvocationException>(() => zoom.SetValue(surface, invalid)).InnerException);
        Assert.IsType<ArgumentOutOfRangeException>(Assert.Throws<TargetInvocationException>(() => contentScale.SetValue(surface, invalid)).InnerException);
        Assert.Equal(1f, Assert.IsType<float>(zoom.GetValue(surface)));
        Assert.Null(contentScale.GetValue(surface));
        Assert.Equal(before, Assert.IsType<DrawRect>(surface.ViewBox));
    }

    [Theory]
    [InlineData(1f, 320f, 180f)]
    [InlineData(1.25f, 400f, 225f)]
    [InlineData(2f, 640f, 360f)]
    public void CameraSeparatesWindowDpiContentScaleAndZoom(float windowScale, float targetWidth, float targetHeight)
    {
        var playerCenter = new System.Numerics.Vector2(2048f, 2048f);
        DrawRect inherited = VillageCamera.Follow(playerCenter, 320f, 180f, windowScale, null, 1f);
        DrawRect native = VillageCamera.Follow(playerCenter, 320f, 180f, windowScale, 1f, 1f);
        DrawRect zoomed = VillageCamera.Follow(playerCenter, 320f, 180f, windowScale, 1f, 2f);

        Assert.Equal(320f, inherited.Width);
        Assert.Equal(180f, inherited.Height);
        Assert.Equal(targetWidth, native.Width);
        Assert.Equal(targetHeight, native.Height);
        Assert.Equal(targetWidth / 2f, zoomed.Width);
        Assert.Equal(targetHeight / 2f, zoomed.Height);
        Assert.Equal(playerCenter.X, native.X + native.Width / 2f);
        Assert.Equal(playerCenter.Y, native.Y + native.Height / 2f);
    }

    [Fact]
    public void FractionalWindowScaleUsesTheSameCeiledTargetExtentAsTheSurface()
    {
        var center = new System.Numerics.Vector2(2048f, 2048f);
        DrawRect native = VillageCamera.Follow(center, 331f, 181f, 1.25f, 1f, 1f);
        DrawRect inherited = VillageCamera.Follow(center, 331f, 181f, 1.25f, null, 1f);

        Assert.Equal(414f, native.Width);
        Assert.Equal(227f, native.Height);
        Assert.Equal(414f / 1.25f, inherited.Width);
        Assert.Equal(227f / 1.25f, inherited.Height);
    }

    [Fact]
    public void VillageCompositionOptsIntoPhysicalScaleWithoutChangingTheSurfaceTypeDefault()
    {
        Assert.Null(new VillageGameSurface().ContentScale);
        var window = new MainWindow();
        VillageGameSurface game = Assert.Single(DescendantsAndSelf(window).OfType<VillageGameSurface>());
        Assert.Equal(1f, game.ContentScale);
        Assert.Equal(1f, game.Zoom);
    }

    [Fact]
    public void ZoomedCameraStillUsesUnscaledWorldCollisionGeometry()
    {
        var surface = new VillageGameSurface { Zoom = 2f, ContentScale = 1f };
        surface.SetMovementKey(Cerneala.UI.Input.InputKey.A, true);
        surface.Advance(TimeSpan.FromSeconds(2));

        Assert.InRange(surface.PlayerPosition.X, 1920f, 1920.1f);
        Assert.Equal(surface.PlayerCenter.X, surface.ViewBox!.Value.X + surface.ViewBox.Value.Width / 2f, 3);
    }

    [Fact]
    public void TownAndCharacterArtworkUseIndependentNativeWorldSizes()
    {
        var townImage = new ImageReference(new ResourceId<ImageResource>("TestTown"));
        var actorImage = new ImageReference(new ResourceId<ImageResource>("TestVillager"));
        Assert.Equal(16f, VillageLayout.TileSize);

        Sprite2D town = VillageArt.TownTile(townImage, 0, 0f, 0f);
        Assert.Equal(16f, town.SourceWidth);
        Assert.Equal(16f, town.SourceHeight);
        Assert.Equal(16f, town.Width);
        Assert.Equal(16f, town.Height);

        Sprite2D player = VillageArt.Player(actorImage, 0f, 0f);
        Sprite2D stress = VillageArt.StressActor(actorImage, new StressItem(0f, 0f, StressPreset.Collision));
        Assert.Equal(32f, player.SourceWidth);
        Assert.Equal(32f, player.SourceHeight);
        Assert.Equal(32f, player.Width);
        Assert.Equal(32f, player.Height);
        Assert.Equal(32f, stress.Width);
        Assert.Equal(32f, stress.Height);

        Sprite2D tree = VillageArt.GreenTree(townImage, 100f, 200f);
        Assert.Equal(184f, tree.Y);
        Assert.Equal(16f, tree.Width);
        Assert.Equal(32f, tree.Height);

        IReadOnlyList<Sprite2D> house = VillageArt.House(townImage, new HouseSite(300f, 400f, true));
        Assert.Equal(16, house.Count);
        BoxCollider2D body = Assert.IsType<BoxCollider2D>(house[0].Collider);
        Assert.Equal(64f, body.Width);
        Assert.Equal(32f, body.Height);
        Assert.Equal(32f, body.OffsetY);
    }

    [Fact]
    public void NativeTownTilesRetainTheExistingWorldArea()
    {
        var surface = new VillageGameSurface();
        Sprite2D[] ground = surface.Scene!.Children.OfType<Sprite2D>()
            .Where(sprite => sprite.X >= 1664f && sprite.X < 2464f &&
                             sprite.Y >= 1664f && sprite.Y < 2464f &&
                             sprite.SourceWidth == 16f && sprite.SourceHeight == 16f &&
                             sprite.SourceY == 0f && sprite.SourceX is 0f or 16f)
            .ToArray();

        Assert.Equal(2_500, ground.Length);
        Assert.Equal(1664f, ground.Min(sprite => sprite.X));
        Assert.Equal(1664f, ground.Min(sprite => sprite.Y));
        Assert.Equal(2464f, ground.Max(sprite => sprite.X + sprite.Width));
        Assert.Equal(2464f, ground.Max(sprite => sprite.Y + sprite.Height));

        Sprite2D[] paths = surface.Scene.Children.OfType<Sprite2D>()
            .Where(sprite => sprite.SourceX == 112f && sprite.SourceY == 48f &&
                             sprite.SourceWidth == 16f && sprite.SourceHeight == 16f)
            .ToArray();
        Assert.Equal(92, paths.Length);
        Assert.Contains(paths, sprite => sprite.X == 1696f && sprite.Y == 2032f);
        Assert.Contains(paths, sprite => sprite.X + sprite.Width == 2432f && sprite.Y == 2032f);
        Assert.Contains(paths, sprite => sprite.X == 2032f && sprite.Y + sprite.Height == 2432f);
    }

    [Fact]
    public void StressGridRetainsItsThirtyTwoUnitPitchAndFullFieldExtent()
    {
        Assert.Equal(10_000, VillageLayout.StressPositions.Count);
        Assert.All(VillageLayout.StressPositions, position =>
        {
            Assert.Equal(0f, position.X % 32f);
            Assert.Equal(0f, position.Y % 32f);
        });
        Assert.Contains(VillageLayout.StressPositions, position => position.X >= 3900f);
        Assert.Contains(VillageLayout.StressPositions, position => position.Y >= 3900f);
    }

    private static IEnumerable<UIElement> DescendantsAndSelf(UIElement element)
    {
        HashSet<UIElement> visited = new(ReferenceEqualityComparer.Instance);
        return Visit(element);

        IEnumerable<UIElement> Visit(UIElement current)
        {
            if (!visited.Add(current))
            {
                yield break;
            }

            yield return current;
            foreach (UIElement child in current.LogicalChildren.Concat(current.VisualChildren))
            {
                foreach (UIElement descendant in Visit(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}
