using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Resources;

namespace Cerneala.SceneVillage;

internal static class VillageArt
{
    internal const float SourceTileSize = 16f;
    internal const float CharacterCellSize = 32f;
    internal const float WorldTileSize = VillageLayout.TileSize;
    private const DrawSamplingMode CharacterSampling = DrawSamplingMode.Point;

    internal static readonly SpriteAnimationSet VillagerAnimations = CreateVillagerAnimations();

    internal static Sprite2D TownTile(ImageReference townImage, int tileIndex, float x, float y)
    {
        return new Sprite2D
        {
            Image = townImage,
            X = x,
            Y = y,
            Width = WorldTileSize,
            Height = WorldTileSize,
            SourceX = tileIndex % 12 * SourceTileSize,
            SourceY = tileIndex / 12 * SourceTileSize,
            SourceWidth = SourceTileSize,
            SourceHeight = SourceTileSize,
            Sampling = DrawSamplingMode.Point
        };
    }

    internal static Sprite2D GreenTree(ImageReference townImage, float x, float lowerCellY)
    {
        // Cells 4 and 16 form one vertically contiguous tree. Keep the
        // existing decoration site at the lower cell's original position.
        Sprite2D tree = TownTile(townImage, 4, x, lowerCellY - WorldTileSize);
        tree.SourceHeight = SourceTileSize * 2f;
        tree.Height = WorldTileSize * 2f;
        return tree;
    }

    internal static Sprite2D Player(ImageReference villagerImage, float x, float y)
    {
        return new Sprite2D
        {
            Image = villagerImage,
            X = x,
            Y = y,
            Width = VillageLayout.CharacterSize,
            Height = VillageLayout.CharacterSize,
            SourceX = CharacterCellSize,
            SourceY = 0,
            SourceWidth = CharacterCellSize,
            SourceHeight = CharacterCellSize,
            Sampling = CharacterSampling,
            Animations = VillagerAnimations,
            AnimationState = "IdleDown",
            Collider = new BoxCollider2D
            {
                Width = 16f,
                Height = 12f,
                OffsetX = 8f,
                OffsetY = 18f,
                CollisionLayer = 1u,
                CollisionMask = 1u,
                IsSimulated = true
            }
        };
    }

    internal static Sprite2D StressActor(ImageReference villagerImage, StressItem item)
    {
        var sprite = new Sprite2D
        {
            Image = villagerImage,
            X = item.X,
            Y = item.Y,
            Width = VillageLayout.CharacterSize,
            Height = VillageLayout.CharacterSize,
            SourceX = CharacterCellSize,
            SourceY = 0,
            SourceWidth = CharacterCellSize,
            SourceHeight = CharacterCellSize,
            Sampling = CharacterSampling
        };

        if (item.Preset == StressPreset.Animated)
        {
            sprite.Animations = VillagerAnimations;
            sprite.AnimationState = "WalkDown";
        }
        else if (item.Preset == StressPreset.Collision)
        {
            sprite.Collider = new BoxCollider2D
            {
                Width = 16f,
                Height = 12f,
                OffsetX = 8f,
                OffsetY = 18f,
                CollisionLayer = 1u,
                CollisionMask = 1u
            };
        }

        return sprite;
    }

    internal static IReadOnlyList<Sprite2D> House(ImageReference townImage, HouseSite site)
    {
        int[,] tiles = site.RedRoof
            ? new[,] { { 52, 53, 53, 54 }, { 64, 65, 67, 66 }, { 72, 73, 73, 75 }, { 72, 84, 85, 75 } }
            : new[,] { { 48, 49, 49, 50 }, { 60, 61, 63, 62 }, { 72, 73, 73, 75 }, { 72, 84, 85, 75 } };
        var parts = new List<Sprite2D>(16);
        for (int row = 0; row < 4; row++)
        {
            for (int column = 0; column < 4; column++)
            {
                var sprite = TownTile(
                    townImage,
                    tiles[row, column],
                    site.X + column * WorldTileSize,
                    site.Y + row * WorldTileSize);
                if (row == 0 && column == 0)
                {
                    // One stable obstacle owns the whole house footprint.
                    sprite.Collider = new BoxCollider2D
                    {
                        Width = 4 * WorldTileSize,
                        Height = 2 * WorldTileSize,
                        OffsetY = 2 * WorldTileSize,
                        CollisionLayer = 1u,
                        CollisionMask = 1u
                    };
                }

                parts.Add(sprite);
            }
        }

        return parts;
    }

    private static SpriteAnimationSet CreateVillagerAnimations()
    {
        var clips = new List<SpriteAnimationClip>(8);
        // Use the verified down, up, and right rows. The left clip deliberately
        // mirrors right with frame Flip; no direction mutates Sprite2D.Flip.
        // Columns 0/2 alternate steps, while 1/3 have neutral feet.
        (string Direction, int Row, RenderSurface2DSpriteFlip Flip)[] directions =
        [
            ("Down", 0, RenderSurface2DSpriteFlip.None),
            ("Up", 1, RenderSurface2DSpriteFlip.None),
            ("Left", 3, RenderSurface2DSpriteFlip.Horizontal),
            ("Right", 3, RenderSurface2DSpriteFlip.None)
        ];
        foreach ((string direction, int row, RenderSurface2DSpriteFlip flip) in directions)
        {
            float sourceY = row * CharacterCellSize;
            clips.Add(new SpriteAnimationClip(
                "Idle" + direction,
                [new SpriteAnimationFrame(new DrawRect(CharacterCellSize, sourceY, CharacterCellSize, CharacterCellSize), TimeSpan.FromMilliseconds(180), flip)]));
            clips.Add(new SpriteAnimationClip(
                "Walk" + direction,
                [
                    new SpriteAnimationFrame(new DrawRect(0f, sourceY, CharacterCellSize, CharacterCellSize), TimeSpan.FromMilliseconds(120), flip),
                    new SpriteAnimationFrame(new DrawRect(CharacterCellSize, sourceY, CharacterCellSize, CharacterCellSize), TimeSpan.FromMilliseconds(120), flip),
                    new SpriteAnimationFrame(new DrawRect(CharacterCellSize * 2, sourceY, CharacterCellSize, CharacterCellSize), TimeSpan.FromMilliseconds(120), flip),
                    new SpriteAnimationFrame(new DrawRect(CharacterCellSize * 3, sourceY, CharacterCellSize, CharacterCellSize), TimeSpan.FromMilliseconds(120), flip)
                ]));
        }

        return new SpriteAnimationSet(clips);
    }
}
