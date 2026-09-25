using System.Numerics;
using System.Security.Cryptography;
using Cerneala.Drawing;
using Cerneala.SceneVillage;
using Cerneala.UI.Controls;
using Cerneala.UI.Input;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.SceneVillage;

public sealed class VillageModelTests
{
    [Fact]
    public void DirectionIsNormalizedAndClearingHeldKeysStopsInput()
    {
        var input = new VillageInput();
        Assert.True(input.Set(InputKey.W, true));
        Assert.True(input.Set(InputKey.D, true));
        Assert.Equal(1f, input.Direction.Length(), 5);
        Assert.True(input.Direction.X > 0f);
        Assert.True(input.Direction.Y < 0f);

        Assert.True(input.Set(InputKey.D, false));
        Assert.Equal(new Vector2(0f, -1f), input.Direction);
        input.Clear();
        Assert.Equal(Vector2.Zero, input.Direction);
        Assert.False(input.Set(InputKey.Space, true));
    }

    [Fact]
    public void FollowCameraCentersClampsAtEdgesAndRespondsToResize()
    {
        var centered = VillageCamera.Follow(new Vector2(2048f, 2048f), 1200f, 720f, 1f, null, 1f);
        Assert.Equal(1200f, centered.Width);
        Assert.Equal(720f, centered.Height);
        Assert.Equal(1448f, centered.X);
        Assert.Equal(1688f, centered.Y);

        var topLeft = VillageCamera.Follow(Vector2.Zero, 1200f, 720f, 1f, null, 1f);
        Assert.Equal(0f, topLeft.X);
        Assert.Equal(0f, topLeft.Y);
        var bottomRight = VillageCamera.Follow(new Vector2(VillageLayout.WorldSize), 1200f, 720f, 1f, null, 1f);
        Assert.Equal(VillageLayout.WorldSize - bottomRight.Width, bottomRight.X);
        Assert.Equal(VillageLayout.WorldSize - bottomRight.Height, bottomRight.Y);

        var resized = VillageCamera.Follow(new Vector2(2048f, 2048f), 1600f, 900f, 1f, null, 1f);
        Assert.Equal(1600f, resized.Width);
        Assert.Equal(900f, resized.Height);
        Assert.Equal(2048f, resized.X + resized.Width / 2f);
        Assert.Equal(2048f, resized.Y + resized.Height / 2f);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(1_000)]
    [InlineData(10_000)]
    public void EveryPresetUsesTheSameDeterministicPositionsAndExactCount(int count)
    {
        var staticItems = VillageStress.CreateItems(count, StressPreset.Static);
        var animatedItems = VillageStress.CreateItems(count, StressPreset.Animated);
        var collisionItems = VillageStress.CreateItems(count, StressPreset.Collision);
        Assert.Equal(count, staticItems.Length);
        Assert.Equal(count, animatedItems.Length);
        Assert.Equal(count, collisionItems.Length);

        for (int i = 0; i < count; i++)
        {
            Assert.Equal(staticItems[i].X, animatedItems[i].X);
            Assert.Equal(staticItems[i].Y, animatedItems[i].Y);
            Assert.Equal(staticItems[i].X, collisionItems[i].X);
            Assert.Equal(staticItems[i].Y, collisionItems[i].Y);
        }
    }

    [Fact]
    public void TenThousandStressPositionsAreUniqueInBoundsAndOutsideVillageClearing()
    {
        var positions = VillageLayout.StressPositions;
        Assert.Equal(VillageLayout.MaximumStressCount, positions.Count);
        Assert.Equal(positions.Count, positions.Distinct().Count());
        foreach (Vector2 position in positions)
        {
            Assert.InRange(position.X, 0f, VillageLayout.WorldSize - VillageLayout.CharacterSize);
            Assert.InRange(position.Y, 0f, VillageLayout.WorldSize - VillageLayout.CharacterSize);
            Assert.False(position.X >= 1664f && position.X < 2432f &&
                         position.Y >= 1664f && position.Y < 2432f);
            Assert.False(position.X >= 2480f && position.X < 2640f &&
                         position.Y >= 1968f && position.Y < 2128f);
        }
    }

    [Fact]
    public void MovementUsesDeltaTimeAndStopsAfterHeldKeysAreCleared()
    {
        var surface = new VillageGameSurface();
        Vector2 start = surface.PlayerPosition;
        surface.SetMovementKey(InputKey.D, true);
        surface.Advance(TimeSpan.FromSeconds(0.25));
        Assert.Equal(start.X + VillageLayout.PlayerSpeed * 0.25f, surface.PlayerPosition.X, 3);
        Assert.Equal(start.Y, surface.PlayerPosition.Y, 3);

        surface.ClearMovementKeys();
        Vector2 released = surface.PlayerPosition;
        surface.Advance(TimeSpan.FromSeconds(0.25));
        Assert.Equal(released, surface.PlayerPosition);
    }

    [Fact]
    public void ViewBoxFollowDoesNotShiftWorldCollisionCoordinates()
    {
        var surface = new VillageGameSurface();
        Assert.True(surface.ViewBox.HasValue);
        Assert.True(surface.ViewBox.Value.X > 0f);
        surface.SetMovementKey(InputKey.A, true);
        surface.Advance(TimeSpan.FromSeconds(2));

        // The west house's native-size body ends at x=1928. The player's
        // collider begins eight units inside its sprite, so x=1920 is contact.
        Assert.InRange(surface.PlayerPosition.X, 1920f, 1920.1f);
        Assert.Equal(surface.PlayerCenter.X, surface.ViewBox!.Value.X + surface.ViewBox.Value.Width / 2f, 3);
    }

    [Fact]
    public void StressPresetsUseTheSameImageAndOnlyTheApprovedBehaviorChanges()
    {
        var image = new ImageReference(new ResourceId<ImageResource>("TestVillager"));
        var staticActor = VillageArt.StressActor(image, new StressItem(100f, 200f, StressPreset.Static));
        var animatedActor = VillageArt.StressActor(image, new StressItem(100f, 200f, StressPreset.Animated));
        var colliderActor = VillageArt.StressActor(image, new StressItem(100f, 200f, StressPreset.Collision));

        Assert.Equal(staticActor.Image, animatedActor.Image);
        Assert.Equal(staticActor.Image, colliderActor.Image);
        Assert.Equal(staticActor.X, animatedActor.X);
        Assert.Equal(staticActor.Y, colliderActor.Y);
        Assert.Null(staticActor.Collider);
        Assert.Null(staticActor.Animations);
        Assert.Null(animatedActor.Collider);
        Assert.Same(VillageArt.VillagerAnimations, animatedActor.Animations);
        Assert.Equal("WalkDown", animatedActor.AnimationState);
        Assert.NotNull(colliderActor.Collider);
        Assert.False(colliderActor.Collider.IsSimulated);
        Assert.Null(colliderActor.Animations);
    }

    [Fact]
    public void PixelArtAtlasesUsePointWithoutChangingTheGlobalSpriteDefault()
    {
        var townImage = new ImageReference(new ResourceId<ImageResource>("TestTown"));
        var characterImage = new ImageReference(new ResourceId<ImageResource>("TestVillager"));
        Sprite2D ground = VillageArt.TownTile(townImage, 0, 0f, 0f);
        Sprite2D path = VillageArt.TownTile(townImage, 43, 32f, 32f);
        Sprite2D decoration = VillageArt.TownTile(townImage, 16, 64f, 64f);
        IReadOnlyList<Sprite2D> house = VillageArt.House(townImage, new HouseSite(96f, 96f, true));

        Assert.All(new[] { ground, path, decoration }.Concat(house),
            sprite => Assert.Equal(DrawSamplingMode.Point, sprite.Sampling));
        Assert.Equal(112f, path.SourceX);
        Assert.Equal(48f, path.SourceY);
        Assert.Equal(16f, path.SourceWidth);
        Assert.Equal(16f, path.SourceHeight);

        Assert.Equal(DrawSamplingMode.Linear, new Sprite2D().Sampling);
        Assert.Equal(DrawSamplingMode.Point, VillageArt.Player(characterImage, 0f, 0f).Sampling);
        foreach (StressPreset preset in Enum.GetValues<StressPreset>())
        {
            Sprite2D actor = VillageArt.StressActor(characterImage, new StressItem(0f, 0f, preset));
            Assert.Equal(DrawSamplingMode.Point, actor.Sampling);
        }
    }

    [Fact]
    public void VillagerWalkClipsUseTheInspectedDirectionalCells()
    {
        // ch003.png is the unmodified 4x4 source. Left deliberately mirrors
        // its verified right walk rather than reading the authored left row.
        // Columns 0/2 alternate steps; columns 1/3 have neutral feet.
        (string Direction, float SourceY, RenderSurface2DSpriteFlip Flip)[] directions =
        [
            ("Down", 0f, RenderSurface2DSpriteFlip.None),
            ("Up", 32f, RenderSurface2DSpriteFlip.None),
            ("Left", 96f, RenderSurface2DSpriteFlip.Horizontal),
            ("Right", 96f, RenderSurface2DSpriteFlip.None)
        ];
        foreach ((string direction, float sourceY, RenderSurface2DSpriteFlip flip) in directions)
        {
            Assert.True(VillageArt.VillagerAnimations.TryGetClip("Idle" + direction, out SpriteAnimationClip? idle));
            SpriteAnimationFrame idleFrame = Assert.Single(idle!.Frames);
            Assert.Equal(new DrawRect(32f, sourceY, 32f, 32f), idleFrame.SourceRect);
            Assert.Equal(flip, idleFrame.Flip);
            Assert.True(VillageArt.VillagerAnimations.TryGetClip("Walk" + direction, out SpriteAnimationClip? walk));
            Assert.Equal(
                [
                    new DrawRect(0f, sourceY, 32f, 32f),
                    new DrawRect(32f, sourceY, 32f, 32f),
                    new DrawRect(64f, sourceY, 32f, 32f),
                    new DrawRect(96f, sourceY, 32f, 32f)
                ],
                walk!.Frames.Select(frame => frame.SourceRect).ToArray());
            Assert.All(walk.Frames, frame => Assert.Equal(flip, frame.Flip));
        }

        var image = new ImageReference(new ResourceId<ImageResource>("TestVillager"));
        Sprite2D player = VillageArt.Player(image, 0f, 0f);
        Sprite2D staticActor = VillageArt.StressActor(image, new StressItem(0f, 0f, StressPreset.Static));
        Assert.Equal(32f, player.SourceX);
        Assert.Equal(0f, player.SourceY);
        Assert.Equal(32f, player.SourceWidth);
        Assert.Equal(32f, player.SourceHeight);
        Assert.Equal(RenderSurface2DSpriteFlip.None, player.Flip);
        Assert.Equal(32f, staticActor.SourceX);
        Assert.Equal(0f, staticActor.SourceY);
        Assert.Equal(32f, staticActor.SourceWidth);
        Assert.Equal(32f, staticActor.SourceHeight);
    }

    [Fact]
    public void VillageTreesUseTheWholeTwoCellArtworkAndProportionalTrunkColliders()
    {
        var surface = new VillageGameSurface();
        DecorationSite[] treeSites = VillageLayout.Decorations
            .Where(site => site.TileIndex == 16).ToArray();
        Assert.Equal(4, treeSites.Length);

        foreach (DecorationSite site in treeSites)
        {
            // Tiny Town cells 4 and 16 are one contiguous tree. The layout
            // site is the lower-cell top-left, so the 32-world-unit sprite
            // starts 16 units above it; the trunk follows the native artwork.
            Sprite2D tree = Assert.Single(surface.Scene!.Children.OfType<Sprite2D>()
                .Where(sprite => sprite.X == site.X && sprite.Y == site.Y - VillageLayout.TileSize &&
                                 sprite.SourceX == 64f && sprite.SourceY == 0f && sprite.SourceHeight == 32f));
            Assert.Equal(64f, tree.SourceX);
            Assert.Equal(0f, tree.SourceY);
            Assert.Equal(16f, tree.SourceWidth);
            Assert.Equal(32f, tree.SourceHeight);
            Assert.Equal(16f, tree.Width);
            Assert.Equal(32f, tree.Height);
            Assert.Equal(DrawSamplingMode.Point, tree.Sampling);

            BoxCollider2D collider = Assert.IsType<BoxCollider2D>(tree.Collider);
            Assert.Equal(site.X + 4.5f, tree.X + collider.OffsetX);
            Assert.Equal(site.Y + 10f, tree.Y + collider.OffsetY);
            Assert.Equal(7f, collider.Width);
            Assert.Equal(5f, collider.Height);
        }
    }

    [Theory]
    [InlineData("tiny-town.png", "3A54D99ECDE790D4FDEA207A3644CF130FC56FA838F1BEB1507C185A95B8E902")]
    [InlineData("ch003.png", "02AC85F7A6DD90A486ED4126CC1CE80D482F6D9D17C02AD42DBAAC1C80B479DB")]
    public void IncludedArtMatchesInspectedCc0Source(string fileName, string expectedHash)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", fileName);
        Assert.True(File.Exists(path), $"Missing copied artwork: {path}");
        Assert.Equal(expectedHash, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))));
    }
}
