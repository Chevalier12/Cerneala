using Cerneala.Drawing;

namespace Cerneala.Tetris.Game;

internal sealed class TPiece : TetrominoPiece
{
    internal static readonly Color PieceColor = new(185, 105, 255);

    public TPiece()
        : base(TetrominoKind.T, PieceColor,
        [
            [new(1, 0), new(0, 1), new(1, 1), new(2, 1)],
            [new(1, 0), new(1, 1), new(2, 1), new(1, 2)],
            [new(0, 1), new(1, 1), new(2, 1), new(1, 2)],
            [new(1, 0), new(0, 1), new(1, 1), new(1, 2)]
        ])
    {
    }
}
