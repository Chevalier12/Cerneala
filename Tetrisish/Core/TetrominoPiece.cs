using Cerneala.Drawing;

namespace Cerneala.Tetris.Game;

internal enum TetrominoKind
{
    I,
    J,
    L,
    O,
    S,
    T,
    Z
}

internal readonly record struct CellOffset(int X, int Y);

internal abstract class TetrominoPiece
{
    private readonly IReadOnlyList<CellOffset>[] rotations;

    protected TetrominoPiece(
        TetrominoKind kind,
        Color color,
        IReadOnlyList<CellOffset>[] rotations)
    {
        ArgumentNullException.ThrowIfNull(rotations);
        if (rotations.Length != 4 || rotations.Any(rotation => rotation.Count != 4))
        {
            throw new ArgumentException(
                "A tetromino must define four rotations containing four cells each.",
                nameof(rotations));
        }

        Kind = kind;
        Color = color;
        this.rotations = rotations;
    }

    public TetrominoKind Kind { get; }

    public Color Color { get; }

    public IReadOnlyList<CellOffset> GetCells(int rotation) => rotations[rotation & 3];

    public DrawRect GetAtlasSource(int rotation) =>
        TetrominoAtlas.SourceFor(Kind, rotation);

    public static TetrominoPiece Create(TetrominoKind kind) => kind switch
    {
        TetrominoKind.I => new IPiece(),
        TetrominoKind.J => new JPiece(),
        TetrominoKind.L => new LPiece(),
        TetrominoKind.O => new OPiece(),
        TetrominoKind.S => new SPiece(),
        TetrominoKind.T => new TPiece(),
        TetrominoKind.Z => new ZPiece(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public static Color ColorFor(TetrominoKind kind) => kind switch
    {
        TetrominoKind.I => IPiece.PieceColor,
        TetrominoKind.J => JPiece.PieceColor,
        TetrominoKind.L => LPiece.PieceColor,
        TetrominoKind.O => OPiece.PieceColor,
        TetrominoKind.S => SPiece.PieceColor,
        TetrominoKind.T => TPiece.PieceColor,
        TetrominoKind.Z => ZPiece.PieceColor,
        _ => Color.White
    };

}
