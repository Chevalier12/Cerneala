using Cerneala.Drawing;

namespace Cerneala.Tetris.Game;

internal sealed class JPiece : TetrominoPiece
{
    internal static readonly Color PieceColor = new(76, 110, 245);

    public JPiece()
        : base(TetrominoKind.J, PieceColor,
        [
            [new(0, 0), new(0, 1), new(1, 1), new(2, 1)],
            [new(1, 0), new(2, 0), new(1, 1), new(1, 2)],
            [new(0, 1), new(1, 1), new(2, 1), new(2, 2)],
            [new(1, 0), new(1, 1), new(0, 2), new(1, 2)]
        ])
    {
    }
}
