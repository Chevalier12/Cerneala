using Cerneala.Drawing;

namespace Cerneala.Tetris.Game;

internal sealed class OPiece : TetrominoPiece
{
    internal static readonly Color PieceColor = new(255, 214, 102);

    public OPiece()
        : base(TetrominoKind.O, PieceColor,
        [
            [new(1, 0), new(2, 0), new(1, 1), new(2, 1)],
            [new(1, 0), new(2, 0), new(1, 1), new(2, 1)],
            [new(1, 0), new(2, 0), new(1, 1), new(2, 1)],
            [new(1, 0), new(2, 0), new(1, 1), new(2, 1)]
        ])
    {
    }
}
