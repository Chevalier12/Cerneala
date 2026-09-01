namespace Cerneala.Tetris.Game;

internal sealed class TetrisGame
{
    private static readonly TimeSpan SoftDropInterval = TimeSpan.FromMilliseconds(50);
    private static readonly (int X, int Y)[] RotationKicks =
    [
        (0, 0),
        (-1, 0),
        (1, 0),
        (-2, 0),
        (2, 0),
        (0, -1)
    ];

    private readonly Random random;
    private readonly Queue<TetrominoKind> bag = new();
    private TimeSpan gravityElapsed;
    private TimeSpan lockElapsed;
    private TetrominoPiece currentPiece = null!;
    private int currentRotation;
    private int currentX;
    private int currentY;
    private TetrominoKind nextKind;
    private TetrominoKind? heldKind;
    private bool holdUsed;
    private bool softDropActive;

    public TetrisGame(int? randomSeed = null)
    {
        random = randomSeed is int seed ? new Random(seed) : Random.Shared;
        Restart();
    }

    public TetrisBoard Board { get; } = new();

    public TetrominoPiece CurrentPiece => currentPiece;

    public TetrominoKind CurrentKind => currentPiece.Kind;

    public int CurrentRotation => currentRotation;

    public int CurrentX => currentX;

    public int CurrentY => currentY;

    public TetrominoKind NextKind => nextKind;

    public TetrominoKind? HeldKind => heldKind;

    public int Score { get; private set; }

    public int Lines { get; private set; }

    public int Level => (Lines / 10) + 1;

    public bool IsPaused { get; private set; }

    public bool IsGameOver { get; private set; }

    public bool IsSoftDropActive => softDropActive;

    public TimeSpan SoftDropHeldDuration { get; private set; }

    public long StateVersion { get; private set; }

    public int GhostY
    {
        get
        {
            int y = currentY;
            while (Board.CanPlace(currentPiece, currentRotation, currentX, y + 1))
            {
                y++;
            }

            return y;
        }
    }

    public void Advance(TimeSpan elapsed)
    {
        if (IsPaused || IsGameOver || elapsed <= TimeSpan.Zero)
        {
            return;
        }

        elapsed = elapsed > TimeSpan.FromMilliseconds(250)
            ? TimeSpan.FromMilliseconds(250)
            : elapsed;

        if (softDropActive)
        {
            SoftDropHeldDuration += elapsed;
        }

        gravityElapsed += elapsed;
        bool moved = false;
        TimeSpan gravityInterval = softDropActive ? SoftDropInterval : GetGravityInterval();
        while (gravityElapsed >= gravityInterval)
        {
            gravityElapsed -= gravityInterval;
            if (!TryMoveDown())
            {
                break;
            }

            moved = true;
            if (softDropActive)
            {
                Score++;
            }
        }

        if (Board.CanPlace(currentPiece, currentRotation, currentX, currentY + 1))
        {
            lockElapsed = TimeSpan.Zero;
        }
        else
        {
            lockElapsed += elapsed;
            if (lockElapsed >= TimeSpan.FromMilliseconds(500))
            {
                LockCurrentPiece();
                return;
            }
        }

        if (moved)
        {
            Touch();
        }
    }

    public bool MoveHorizontal(int direction)
    {
        if (!CanAcceptInput() || direction is not (-1 or 1) ||
            !Board.CanPlace(currentPiece, currentRotation, currentX + direction, currentY))
        {
            return false;
        }

        currentX += direction;
        lockElapsed = TimeSpan.Zero;
        Touch();
        return true;
    }

    public bool SoftDrop()
    {
        if (!CanAcceptInput() || !TryMoveDown())
        {
            return false;
        }

        Score++;
        gravityElapsed = TimeSpan.Zero;
        Touch();
        return true;
    }

    public bool BeginSoftDrop()
    {
        if (!CanAcceptInput())
        {
            return false;
        }

        if (softDropActive)
        {
            return true;
        }

        softDropActive = true;
        SoftDropHeldDuration = TimeSpan.Zero;
        SoftDrop();
        return true;
    }

    public void EndSoftDrop()
    {
        softDropActive = false;
        SoftDropHeldDuration = TimeSpan.Zero;
        gravityElapsed = TimeSpan.Zero;
    }

    public bool HardDrop()
    {
        if (!CanAcceptInput())
        {
            return false;
        }

        int startY = currentY;
        currentY = GhostY;
        Score += (currentY - startY) * 2;
        LockCurrentPiece();
        return true;
    }

    public bool Rotate(int direction)
    {
        if (!CanAcceptInput() || direction is not (-1 or 1))
        {
            return false;
        }

        int targetRotation = (currentRotation + direction + 4) & 3;
        foreach ((int kickX, int kickY) in RotationKicks)
        {
            if (!Board.CanPlace(
                    currentPiece,
                    targetRotation,
                    currentX + kickX,
                    currentY + kickY))
            {
                continue;
            }

            currentRotation = targetRotation;
            currentX += kickX;
            currentY += kickY;
            lockElapsed = TimeSpan.Zero;
            Touch();
            return true;
        }

        return false;
    }

    public bool Hold()
    {
        if (!CanAcceptInput() || holdUsed)
        {
            return false;
        }

        TetrominoKind outgoing = currentPiece.Kind;
        if (heldKind is TetrominoKind incoming)
        {
            heldKind = outgoing;
            Spawn(incoming);
        }
        else
        {
            heldKind = outgoing;
            Spawn(nextKind);
            nextKind = TakeFromBag();
        }

        holdUsed = true;
        Touch();
        return true;
    }

    public bool TogglePause()
    {
        if (IsGameOver)
        {
            return false;
        }

        IsPaused = !IsPaused;
        gravityElapsed = TimeSpan.Zero;
        lockElapsed = TimeSpan.Zero;
        Touch();
        return true;
    }

    public bool Restart()
    {
        Board.Clear();
        bag.Clear();
        Score = 0;
        Lines = 0;
        heldKind = null;
        holdUsed = false;
        softDropActive = false;
        SoftDropHeldDuration = TimeSpan.Zero;
        IsPaused = false;
        IsGameOver = false;
        gravityElapsed = TimeSpan.Zero;
        lockElapsed = TimeSpan.Zero;
        TetrominoKind firstKind = TakeFromBag();
        nextKind = TakeFromBag();
        Spawn(firstKind);
        Touch();
        return true;
    }

    private bool CanAcceptInput()
    {
        return !IsPaused && !IsGameOver;
    }

    private bool TryMoveDown()
    {
        if (!Board.CanPlace(currentPiece, currentRotation, currentX, currentY + 1))
        {
            return false;
        }

        currentY++;
        lockElapsed = TimeSpan.Zero;
        return true;
    }

    private void LockCurrentPiece()
    {
        bool fullyVisible = Board.Place(
            currentPiece,
            currentRotation,
            currentX,
            currentY);
        if (!fullyVisible)
        {
            IsGameOver = true;
            Touch();
            return;
        }

        int cleared = Board.ClearFullLines();
        if (cleared > 0)
        {
            int[] lineScores = [0, 100, 300, 500, 800];
            Score += lineScores[cleared] * Level;
            Lines += cleared;
        }

        holdUsed = false;
        Spawn(nextKind);
        nextKind = TakeFromBag();
        Touch();
    }

    private void Spawn(TetrominoKind kind)
    {
        currentPiece = TetrominoPiece.Create(kind);
        currentRotation = 0;
        currentX = 3;
        currentY = -1;
        gravityElapsed = TimeSpan.Zero;
        lockElapsed = TimeSpan.Zero;
        if (!Board.CanPlace(currentPiece, currentRotation, currentX, currentY))
        {
            IsGameOver = true;
        }
    }

    private TetrominoKind TakeFromBag()
    {
        if (bag.Count == 0)
        {
            TetrominoKind[] kinds = Enum.GetValues<TetrominoKind>();
            for (int index = kinds.Length - 1; index > 0; index--)
            {
                int swapIndex = random.Next(index + 1);
                (kinds[index], kinds[swapIndex]) = (kinds[swapIndex], kinds[index]);
            }

            foreach (TetrominoKind kind in kinds)
            {
                bag.Enqueue(kind);
            }
        }

        return bag.Dequeue();
    }

    private TimeSpan GetGravityInterval()
    {
        double milliseconds = Math.Max(80, 800 * Math.Pow(0.84, Level - 1));
        return TimeSpan.FromMilliseconds(milliseconds);
    }

    private void Touch()
    {
        StateVersion = StateVersion == long.MaxValue ? 1 : StateVersion + 1;
    }

}
