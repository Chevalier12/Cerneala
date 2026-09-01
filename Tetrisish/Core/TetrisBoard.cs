namespace Cerneala.Tetris.Game;

internal sealed class TetrisBoard
{
    public const int Width = 10;
    public const int Height = 20;

    private readonly TetrominoKind?[,] cells = new TetrominoKind?[Width, Height];
    private readonly int[,] placementIds = new int[Width, Height];
    private readonly List<LockedPiecePlacement> lockedPlacements = [];
    private int nextPlacementId = 1;

    public TetrominoKind? this[int x, int y] => cells[x, y];

    public long LockedStateVersion { get; private set; }

    public IReadOnlyList<LockedPiecePlacement> LockedPlacements => lockedPlacements;

    public int GetPlacementId(int x, int y) => placementIds[x, y];

    public bool CanPlace(
        TetrominoPiece piece,
        int rotation,
        int originX,
        int originY)
    {
        ArgumentNullException.ThrowIfNull(piece);
        foreach (CellOffset cell in piece.GetCells(rotation))
        {
            int x = originX + cell.X;
            int y = originY + cell.Y;
            if (x < 0 || x >= Width || y >= Height)
            {
                return false;
            }

            if (y >= 0 && cells[x, y] is not null)
            {
                return false;
            }
        }

        return true;
    }

    public bool Place(
        TetrominoPiece piece,
        int rotation,
        int originX,
        int originY)
    {
        ArgumentNullException.ThrowIfNull(piece);
        bool fullyVisible = true;
        int placementId = nextPlacementId++;
        List<CellOffset> placedCells = new(4);
        foreach (CellOffset cell in piece.GetCells(rotation))
        {
            int x = originX + cell.X;
            int y = originY + cell.Y;
            if (y < 0)
            {
                fullyVisible = false;
                continue;
            }

            cells[x, y] = piece.Kind;
            placementIds[x, y] = placementId;
            placedCells.Add(new CellOffset(x, y));
        }

        if (placedCells.Count > 0)
        {
            lockedPlacements.Add(new LockedPiecePlacement(
                placementId,
                piece.Kind,
                rotation & 3,
                originX,
                originY,
                piece.GetCells(rotation).ToArray(),
                placedCells.ToArray()));
            LockedStateVersion++;
        }

        return fullyVisible;
    }

    public int ClearFullLines()
    {
        int destinationY = Height - 1;
        int cleared = 0;
        int[] destinationRows = new int[Height];
        Array.Fill(destinationRows, -1);

        for (int sourceY = Height - 1; sourceY >= 0; sourceY--)
        {
            bool full = true;
            for (int x = 0; x < Width; x++)
            {
                if (cells[x, sourceY] is null)
                {
                    full = false;
                    break;
                }
            }

            if (full)
            {
                cleared++;
                continue;
            }

            destinationRows[sourceY] = destinationY;

            if (destinationY != sourceY)
            {
                for (int x = 0; x < Width; x++)
                {
                    cells[x, destinationY] = cells[x, sourceY];
                    placementIds[x, destinationY] = placementIds[x, sourceY];
                }
            }

            destinationY--;
        }

        for (int y = destinationY; y >= 0; y--)
        {
            for (int x = 0; x < Width; x++)
            {
                cells[x, y] = null;
                placementIds[x, y] = 0;
            }
        }

        if (cleared == 0)
        {
            return 0;
        }

        for (int index = lockedPlacements.Count - 1; index >= 0; index--)
        {
            LockedPiecePlacement placement = lockedPlacements[index];
            placement.ApplyLineMap(destinationRows);
            if (placement.Cells.Count == 0)
            {
                lockedPlacements.RemoveAt(index);
            }
        }

        LockedStateVersion++;
        return cleared;
    }

    public void SetCell(int x, int y, TetrominoKind? kind)
    {
        if (cells[x, y] == kind && placementIds[x, y] == 0)
        {
            return;
        }

        cells[x, y] = kind;
        placementIds[x, y] = 0;
        LockedStateVersion++;
    }

    public void Clear()
    {
        Array.Clear(cells);
        Array.Clear(placementIds);
        lockedPlacements.Clear();
        nextPlacementId = 1;
        LockedStateVersion++;
    }
}

internal sealed class LockedPiecePlacement
{
    private readonly CellOffset[] localCells;
    private CellOffset[] cells;

    public LockedPiecePlacement(
        int id,
        TetrominoKind kind,
        int rotation,
        int originX,
        int originY,
        CellOffset[] localCells,
        CellOffset[] cells)
    {
        Id = id;
        Kind = kind;
        Rotation = rotation;
        OriginX = originX;
        OriginY = originY;
        this.localCells = localCells;
        this.cells = cells;
        IsIntact = cells.Length == localCells.Length;
    }

    public int Id { get; }

    public TetrominoKind Kind { get; }

    public int Rotation { get; }

    public int OriginX { get; private set; }

    public int OriginY { get; private set; }

    public IReadOnlyList<CellOffset> Cells => cells;

    public bool IsIntact { get; private set; }

    public bool Contains(int x, int y)
    {
        foreach (CellOffset cell in cells)
        {
            if (cell.X == x && cell.Y == y)
            {
                return true;
            }
        }

        return false;
    }

    public void ApplyLineMap(int[] destinationRows)
    {
        List<CellOffset> surviving = new(cells.Length);
        foreach (CellOffset cell in cells)
        {
            int destinationY = destinationRows[cell.Y];
            if (destinationY >= 0)
            {
                surviving.Add(new CellOffset(cell.X, destinationY));
            }
        }

        cells = surviving.ToArray();
        IsIntact = cells.Length == localCells.Length;
        if (!IsIntact)
        {
            return;
        }

        int originX = cells[0].X - localCells[0].X;
        int originY = cells[0].Y - localCells[0].Y;
        for (int index = 1; index < cells.Length; index++)
        {
            if (cells[index].X - localCells[index].X != originX ||
                cells[index].Y - localCells[index].Y != originY)
            {
                IsIntact = false;
                return;
            }
        }

        OriginX = originX;
        OriginY = originY;
    }
}
