using Cerneala.Drawing;

namespace Cerneala.Tetris.Game;

internal sealed class IPiece : TetrominoPiece
{
    internal static readonly Color PieceColor = new(53, 216, 255);

    public IPiece()
        : base(TetrominoKind.I, PieceColor,
        [
            [new(0, 1), new(1, 1), new(2, 1), new(3, 1)],
            [new(2, 0), new(2, 1), new(2, 2), new(2, 3)],
            [new(0, 2), new(1, 2), new(2, 2), new(3, 2)],
            [new(1, 0), new(1, 1), new(1, 2), new(1, 3)]
        ])
    {
    }
}
