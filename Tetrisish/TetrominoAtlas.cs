using Cerneala.Drawing;
using Cerneala.Tetris.Game;
using Cerneala.UI.Resources;

namespace Cerneala.Tetris;

internal static class TetrominoAtlas
{
    public const int TileSize = 64;
    public const int LockedCellTileSize = 64;
    private const int LockedCellAtlasTop = 448;
    private const int LockedCellTilePitch = 68;
    private const int LockedCellTilePadding = 2;
    private const string AssetPath = "Assets/tetromino-atlas.svg.cerneala.png";

    public static IDrawImage Create(IImageLoader imageLoader)
    {
        ArgumentNullException.ThrowIfNull(imageLoader);
        return imageLoader.Load(AssetPath);
    }

    public static DrawRect SourceFor(TetrominoKind kind, int rotation) => new(
        (rotation & 3) * TileSize,
        (int)kind * TileSize,
        TileSize,
        TileSize);

    public static DrawRect SourceForLockedCell(
        bool hasLeftNeighbor,
        bool hasRightNeighbor,
        bool hasTopNeighbor,
        bool hasBottomNeighbor)
    {
        int mask =
            (hasLeftNeighbor ? 1 : 0) |
            (hasRightNeighbor ? 2 : 0) |
            (hasTopNeighbor ? 4 : 0) |
            (hasBottomNeighbor ? 8 : 0);
        return new DrawRect(
            mask % 4 * LockedCellTilePitch + LockedCellTilePadding,
            LockedCellAtlasTop + mask / 4 * LockedCellTilePitch + LockedCellTilePadding,
            LockedCellTileSize,
            LockedCellTileSize);
    }
}
