using Cerneala.Drawing;

namespace Cerneala.Tetris.Game;

internal sealed class ZPiece : TetrominoPiece
{
    internal static readonly Color PieceColor = new(255, 89, 112);

    public ZPiece()
        : base(TetrominoKind.Z, PieceColor,
        [
            [new(0, 0), new(1, 0), new(1, 1), new(2, 1)],
            [new(2, 0), new(1, 1), new(2, 1), new(1, 2)],
            [new(0, 1), new(1, 1), new(1, 2), new(2, 2)],
            [new(1, 0), new(0, 1), new(1, 1), new(0, 2)]
        ])
    {
    }
}
