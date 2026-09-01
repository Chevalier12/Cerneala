using System.Diagnostics;
using Cerneala.Drawing;
using Cerneala.Tetris.Game;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;
using Cerneala.UI.Resources;

namespace Cerneala.Tetris;

public sealed class TetrisGameSurface : RenderSurface2D
{
    private const float BlurSampleDistance = 1;
    private const float MaximumBlurDistance = 24;
    private static readonly TimeSpan SoftDropRamp = TimeSpan.FromMilliseconds(700);
    private static readonly TimeSpan SoftDropRelease = TimeSpan.FromMilliseconds(120);

    internal static readonly UiProperty<float> BlurDistanceProperty = UiProperty<float>.Register(
        nameof(BlurDistance),
        typeof(TetrisGameSurface),
        new UiPropertyMetadata<float>(
            0,
            equalityComparer: new QuantizedFloatComparer(BlurSampleDistance),
            validateValue: float.IsFinite));

    internal static readonly UiProperty<bool> IsBlurActiveProperty =
        UiProperty<bool>.Register(
            nameof(IsBlurActive),
            typeof(TetrisGameSurface),
            new UiPropertyMetadata<bool>(false));

    public static readonly UiProperty<TetrisSceneModel> SceneModelProperty =
        UiProperty<TetrisSceneModel>.Register(
            nameof(SceneModel),
            typeof(TetrisGameSurface),
            new UiPropertyMetadata<TetrisSceneModel>(
                null!,
                validateValue: value => value is not null));

    private static readonly Color BoardBackground = new(5, 10, 20);
    private static readonly Color BoardBorder = new(53, 216, 255);
    private static readonly Color GridColor = new(35, 50, 76, 150);
    private readonly TetrisGame game;
    private IDrawImage? tetrominoAtlas;
    private MotionHandle? blurMotion;
    private float appliedBlurDistance;
    private long lastAdvanceTimestamp;
    private long sceneLockedStateVersion = -1;
    private IDrawImage? sceneAtlas;

    public TetrisGameSurface()
    {
        game = new TetrisGame();
        SceneModel = new TetrisSceneModel();
        ClearColor = new Color(7, 11, 20);
        RedrawMode = RenderSurface2DRedrawMode.Continuous;
    }

    public TetrisSceneModel SceneModel
    {
        get => GetValue(SceneModelProperty);
        private set => SetValue(SceneModelProperty, value);
    }

    internal float BlurDistance
    {
        get => GetValue(BlurDistanceProperty);
        set => SetValue(BlurDistanceProperty, value);
    }

    internal bool IsBlurActive
    {
        get => GetValue(IsBlurActiveProperty);
        private set => SetValue(IsBlurActiveProperty, value);
    }

    internal float AppliedBlurDistance => appliedBlurDistance;

    internal static float BlurDistanceFor(TimeSpan heldDuration)
    {
        if (heldDuration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(heldDuration));
        }

        double progress = Math.Clamp(
            heldDuration.TotalMilliseconds / SoftDropRamp.TotalMilliseconds,
            0,
            1);
        return MaximumBlurDistance * (float)progress;
    }

    public int Score => game.Score;

    public int Lines => game.Lines;

    public int Level => game.Level;

    public bool IsPaused => game.IsPaused;

    public bool IsGameOver => game.IsGameOver;

    public long StateVersion => game.StateVersion;

    internal TetrominoKind NextKind => game.NextKind;

    internal TetrominoKind? HeldKind => game.HeldKind;

    public bool MoveHorizontal(int direction) => game.MoveHorizontal(direction);

    public bool SoftDrop() => game.SoftDrop();

    public bool BeginSoftDrop()
    {
        bool wasActive = game.IsSoftDropActive;
        bool accepted = game.BeginSoftDrop();
        if (accepted && !wasActive)
        {
            StartSoftDropBlur(TimeSpan.Zero);
        }

        return accepted;
    }

    public void EndSoftDrop()
    {
        game.EndSoftDrop();
        StartBlurRelease();
    }

    public bool HardDrop() => game.HardDrop();

    public bool RotateClockwise() => game.Rotate(1);

    public bool RotateCounterClockwise() => game.Rotate(-1);

    public bool Hold() => game.Hold();

    public bool TogglePause() => game.TogglePause();

    public bool Restart()
    {
        bool restarted = game.Restart();
        ResetBlurPresentation();
        return restarted;
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        EnsureAtlas();
        lastAdvanceTimestamp = Stopwatch.GetTimestamp();
    }

    protected override void OnDetached()
    {
        StopMotion(ref blurMotion);
        lastAdvanceTimestamp = 0;
        if (tetrominoAtlas is IDisposable disposableAtlas)
        {
            disposableAtlas.Dispose();
        }
        tetrominoAtlas = null;
        sceneAtlas = null;
        sceneLockedStateVersion = -1;
        SceneModel.Reset();
        base.OnDetached();
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, BlurDistanceProperty))
        {
            appliedBlurDistance = BlurDistance;
            IsBlurActive = appliedBlurDistance > 0;
        }
    }

    protected override void OnDraw(RenderSurface2DFrame frame)
    {
        EnsureAtlas();
        long now = Stopwatch.GetTimestamp();
        TimeSpan elapsed = lastAdvanceTimestamp == 0
            ? TimeSpan.Zero
            : Stopwatch.GetElapsedTime(lastAdvanceTimestamp, now);
        lastAdvanceTimestamp = now;
        game.Advance(elapsed);
        SynchronizeScene();
        DrawGame(frame);
    }

    private void DrawGame(RenderSurface2DFrame frame)
    {
        frame.FillRectangle(frame.Bounds, new Color(7, 11, 20));

        float cellSize = MathF.Min(
            MathF.Max(1, frame.Bounds.Width / TetrisBoard.Width),
            MathF.Max(1, frame.Bounds.Height / TetrisBoard.Height));
        float boardWidth = cellSize * TetrisBoard.Width;
        float boardHeight = cellSize * TetrisBoard.Height;
        float originX = frame.Bounds.X + (frame.Bounds.Width - boardWidth) / 2;
        float originY = frame.Bounds.Y + (frame.Bounds.Height - boardHeight) / 2;
        DrawRect boardBounds = new(originX, originY, boardWidth, boardHeight);

        frame.FillRectangle(boardBounds, BoardBackground);

        for (int x = 1; x < TetrisBoard.Width; x++)
        {
            float position = originX + x * cellSize;
            frame.DrawLine(
                new DrawPoint(position, originY),
                new DrawPoint(position, originY + boardHeight),
                GridColor,
                1);
        }

        for (int y = 1; y < TetrisBoard.Height; y++)
        {
            float position = originY + y * cellSize;
            frame.DrawLine(
                new DrawPoint(originX, position),
                new DrawPoint(originX + boardWidth, position),
                GridColor,
                1);
        }

        frame.DrawRectangle(boardBounds, BoardBorder, 2);
    }

    private void SynchronizeScene()
    {
        IDrawImage? atlas = tetrominoAtlas;
        if (atlas is null)
        {
            SceneModel.Reset();
            return;
        }

        if (!ReferenceEquals(sceneAtlas, atlas) ||
            sceneLockedStateVersion != game.Board.LockedStateVersion)
        {
            SceneModel.UpdateLockedPieces(
                game.Board.LockedPlacements
                    .SelectMany(placement => placement.Cells.Select(cell =>
                        new TetrisSpriteModel(
                            atlas,
                            TetrominoAtlas.SourceForLockedCell(
                                placement.Contains(cell.X - 1, cell.Y),
                                placement.Contains(cell.X + 1, cell.Y),
                                placement.Contains(cell.X, cell.Y - 1),
                                placement.Contains(cell.X, cell.Y + 1)),
                            new DrawRect(cell.X, cell.Y, 1, 1),
                            TetrominoPiece.ColorFor(placement.Kind)))));
            sceneAtlas = atlas;
            sceneLockedStateVersion = game.Board.LockedStateVersion;
        }

        TetrominoPiece piece = game.CurrentPiece;
        DrawRect? source = piece.GetAtlasSource(game.CurrentRotation);
        SceneModel.UpdateActivePiece(
            atlas,
            atlas,
            source,
            new DrawRect(game.CurrentX, game.CurrentY, 4, 4),
            piece.Color,
            new DrawRect(game.CurrentX, game.GhostY, 4, 4),
            visible: !game.IsGameOver);
    }

    private void EnsureAtlas()
    {
        if (tetrominoAtlas is not null ||
            Root?.ImageLoader is not IImageLoader loader)
        {
            return;
        }

        tetrominoAtlas = TetrominoAtlas.Create(loader);
    }

    private void StartSoftDropBlur(TimeSpan heldDuration)
    {
        StopMotion(ref blurMotion);
        float distance = BlurDistanceFor(heldDuration);
        ClearValue(BlurDistanceProperty);
        if (distance >= MaximumBlurDistance)
        {
            SetValue(
                BlurDistanceProperty,
                distance,
                UiPropertyValueSource.Animation);
            return;
        }

        blurMotion = this.Motion()
            .Animate(BlurDistanceProperty)
            .From(distance)
            .To(MaximumBlurDistance)
            .With(new TweenSpec<float>(SoftDropRamp - heldDuration, Easings.Linear));
    }

    private void StartBlurRelease()
    {
        StopMotion(ref blurMotion);
        if (Root is null)
        {
            BlurDistance = 0;
            return;
        }

        if (AppliedBlurDistance <= 0)
        {
            return;
        }

        blurMotion = this.Motion()
            .Animate(BlurDistanceProperty)
            .From(AppliedBlurDistance)
            .To(0)
            .With(new TweenSpec<float>(SoftDropRelease, Easings.EaseOut));
    }

    private void ResetBlurPresentation()
    {
        StopMotion(ref blurMotion);
        SetValue(
            BlurDistanceProperty,
            0,
            UiPropertyValueSource.Animation);
    }

    private static void StopMotion(ref MotionHandle? handle)
    {
        handle?.Cancel(MotionCancelBehavior.KeepCurrent);
        handle?.Dispose();
        handle = null;
    }

    private sealed class QuantizedFloatComparer(float sampleSize) : IEqualityComparer<float>
    {
        public bool Equals(float left, float right) => Bucket(left) == Bucket(right);

        public int GetHashCode(float value) => Bucket(value);

        private int Bucket(float value) => (int)MathF.Round(value / sampleSize);
    }
}
