using Cerneala.Tetris.Game;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Markup;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Resources;
using System.ComponentModel;
using System.Globalization;
using System.Xml.Linq;

namespace Cerneala.Tetris.Tests;

public sealed class TetrisGameTests
{
    [Fact]
    public void SurfaceUsesContinuousRedrawForTheRealtimeGameLoop()
    {
        TetrisGameSurface surface = new();

        Assert.Equal(RenderSurface2DRedrawMode.Continuous, surface.RedrawMode);
    }

    [Fact]
    public void RestartClearsAnActiveSoftDropPresentation()
    {
        ProbeMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        TetrisGameSurface surface = new();
        root.VisualChildren.Add(surface);
        root.ProcessFrame();
        Assert.True(surface.BeginSoftDrop());
        for (int frame = 0; frame < 7; frame++)
        {
            clock.Advance(TimeSpan.FromMilliseconds(50));
            root.ProcessFrame();
        }
        Assert.True(surface.AppliedBlurDistance > 0);

        Assert.True(surface.Restart());
        root.ProcessFrame();

        Assert.Equal(0, surface.AppliedBlurDistance);
        Assert.False(surface.IsBlurActive);
    }

    [Fact]
    public void LockedSpriteModelsAreImmutableSnapshots()
    {
        Assert.True(typeof(INotifyPropertyChanged).IsAssignableFrom(
            typeof(TetrisSpriteModel)));
        Assert.All(
            typeof(TetrisSpriteModel).GetProperties(),
            property => Assert.False(property.CanWrite));
    }

    [Fact]
    public void SoftDropMotionPublishesIntermediateQuantizedValues()
    {
        ProbeMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        TetrisGameSurface surface = new();
        root.VisualChildren.Add(surface);
        root.ProcessFrame();

        Assert.True(surface.BeginSoftDrop());
        root.ProcessFrame();
        for (int frame = 0; frame < 7; frame++)
        {
            clock.Advance(TimeSpan.FromMilliseconds(50));
            root.ProcessFrame();
        }

        Assert.InRange(surface.BlurDistance, 11.5f, 12.5f);
        Assert.InRange(surface.AppliedBlurDistance, 11.5f, 12.5f);
    }

    [Fact]
    public void ClosingAfterSoftDroppedPieceLandsDoesNotStartMotionAfterDetach()
    {
        ProbeMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        TetrisGameSurface surface = new();
        root.VisualChildren.Add(surface);
        root.ProcessFrame();
        Assert.True(surface.BeginSoftDrop());
        clock.Advance(TimeSpan.FromMilliseconds(350));
        root.ProcessFrame();
        Assert.True(surface.HardDrop());

        root.VisualChildren.Remove(surface);
        Exception? failure = Record.Exception(surface.EndSoftDrop);

        Assert.Null(failure);
    }

    [Fact]
    public void TetrominoAtlasProvidesOneConnectedSilhouettePerRotation()
    {
        string atlasPath = Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "tetromino-atlas.svg");
        AtlasAlphaCell[] cells = ReadAlphaCells(atlasPath);

        AssertConnectedAndPadded(cells);
    }

    [Fact]
    public void ActivePieceOwnsMarkupPrismWithNestedEffectOrder()
    {
        (UIRoot root, MainWindow window, Sprite2D sprite) =
            CreateActivePieceWindow();

        PrismInstance prism = GeneratedMarkup.GetPrismInstance(sprite);
        PrismGroupDefinition group = Assert.IsType<PrismGroupDefinition>(
            Assert.Single(prism.Definition.Nodes));
        PrismLayerDefinition styled = Assert.IsType<PrismLayerDefinition>(
            Assert.Single(group.Children));

        Assert.Equal("CurrentEffects", group.Name);
        Assert.Equal("StyledPiece", styled.Name);
        Assert.Equal(
            [PrismStyleId.BevelEmboss, PrismStyleId.OuterGlow],
            styled.Styles.Select(style => style.Style));
        Assert.Equal(
            PrismFilterId.MotionBlur,
            Assert.Single(group.Filters).Filter);

        root.VisualChildren.Remove(window);
    }

    [Fact]
    public void ActivePieceMarkupMotionAnimatesItsPrismGlow()
    {
        ProbeMotionClock clock = new();
        (UIRoot root, MainWindow window, Sprite2D sprite) =
            CreateActivePieceWindow(clock);
        PrismInstance prism = GeneratedMarkup.GetPrismInstance(sprite);
        long initialVersion = prism.ValueVersion.Value;

        clock.Advance(TimeSpan.FromMilliseconds(450));
        root.ProcessFrame();

        Assert.True(prism.ValueVersion.Value > initialVersion);

        root.VisualChildren.Remove(window);
    }

    [Fact]
    public void MotionBlurVisibilityTracksTheQuantizedSurfaceValue()
    {
        (UIRoot root, MainWindow window, Sprite2D sprite) =
            CreateActivePieceWindow();
        TetrisGameSurface surface = DescendantsAndSelf(window)
            .OfType<TetrisGameSurface>()
            .Single();
        PrismInstance prism = GeneratedMarkup.GetPrismInstance(sprite);
        PrismGroupDefinition groupDefinition = Assert.IsType<PrismGroupDefinition>(
            Assert.Single(prism.Definition.Nodes));
        PrismGroupState group = prism.GetGroupState(groupDefinition.Id);
        PrismFilterState blur = Assert.Single(group.Filters);

        Assert.False(blur.Visible);

        surface.BlurDistance = 1;
        Assert.True(surface.IsBlurActive);
        root.ProcessFrame();
        Assert.True(blur.Visible);

        surface.BlurDistance = 0;
        root.ProcessFrame();
        Assert.False(blur.Visible);

        root.VisualChildren.Remove(window);
    }

    [Fact]
    public void PlacementIdentitySurvivesLineClearAndSeparatesSameKindPieces()
    {
        TetrisBoard board = new();
        OPiece first = new();
        OPiece second = new();
        Assert.True(board.Place(first, rotation: 0, originX: 0, originY: 18));
        Assert.True(board.Place(second, rotation: 0, originX: 4, originY: 16));

        int firstId = board.GetPlacementId(1, 18);
        int secondId = board.GetPlacementId(5, 16);

        Assert.True(firstId > 0);
        Assert.True(secondId > 0);
        Assert.NotEqual(firstId, secondId);
        Assert.Equal(firstId, board.GetPlacementId(2, 19));

        for (int x = 0; x < TetrisBoard.Width; x++)
        {
            if (x is not (1 or 2))
            {
                board.SetCell(x, TetrisBoard.Height - 1, TetrominoKind.I);
            }
        }

        Assert.Equal(1, board.ClearFullLines());
        Assert.Equal(TetrominoKind.O, board[1, TetrisBoard.Height - 1]);
        Assert.Equal(TetrominoKind.O, board[2, TetrisBoard.Height - 1]);
        Assert.Equal(
            firstId,
            board.GetPlacementId(1, TetrisBoard.Height - 1));
        Assert.Equal(
            firstId,
            board.GetPlacementId(2, TetrisBoard.Height - 1));
    }

    [Fact]
    public void LineClearedFragmentsRemainLockedSpriteModels()
    {
        TetrisGameSurface surface = new();
        TetrisGame game = Assert.IsType<TetrisGame>(
            typeof(TetrisGameSurface)
                .GetField(
                    "game",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)!
                .GetValue(surface));
        TetrisBoard board = game.Board;
        Assert.True(board.Place(new OPiece(), rotation: 0, originX: 0, originY: 18));
        for (int x = 0; x < TetrisBoard.Width; x++)
        {
            if (x is not (1 or 2))
            {
                board.SetCell(x, TetrisBoard.Height - 1, TetrominoKind.I);
            }
        }

        Assert.Equal(1, board.ClearFullLines());
        TestImage atlas = new(272, 720);
        typeof(TetrisGameSurface)
            .GetField(
                "tetrominoAtlas",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic)!
            .SetValue(surface, atlas);

        typeof(TetrisGameSurface)
            .GetMethod(
                "SynchronizeScene",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic)!
            .Invoke(surface, null);

        TetrisSpriteModel[] sprites = surface.SceneModel.LockedPieces
            .Cast<TetrisSpriteModel>()
            .ToArray();
        Assert.Equal(2, sprites.Length);
        Assert.All(sprites, sprite => Assert.Same(atlas, sprite.Source));
        Assert.Equal(
            [new Cerneala.Drawing.DrawRect(1, 19, 1, 1),
             new Cerneala.Drawing.DrawRect(2, 19, 1, 1)],
            sprites.Select(sprite => sprite.Destination));
    }

    private static AtlasAlphaCell[] ReadAlphaCells(string path)
    {
        XDocument document = XDocument.Load(path);
        XNamespace svg = "http://www.w3.org/2000/svg";
        return document
            .Descendants(svg + "rect")
            .Where(rectangle => rectangle.Attribute("opacity") is null)
            .Where(rectangle => Number(rectangle, "y") < 448)
            .Select(rectangle => new AtlasAlphaCell(
                Number(rectangle, "x"),
                Number(rectangle, "y"),
                Number(rectangle, "width"),
                Number(rectangle, "height")))
            .ToArray();
    }

    private static void AssertConnectedAndPadded(AtlasAlphaCell[] alphaCells)
    {
        Assert.Equal(7 * 4 * 4, alphaCells.Length);
        var tiles = alphaCells.GroupBy(cell => (
            Column: (int)Math.Floor(cell.CenterX / 64),
            Row: (int)Math.Floor(cell.CenterY / 64)));
        Assert.Equal(7 * 4, tiles.Count());
        foreach (var tile in tiles)
        {
            AtlasAlphaCell[] cells = tile.ToArray();
            Assert.Equal(4, cells.Length);
            Assert.True(cells.Min(cell => cell.X) > tile.Key.Column * 64);
            Assert.True(cells.Max(cell => cell.Right) < (tile.Key.Column + 1) * 64);
            Assert.True(cells.Min(cell => cell.Y) > tile.Key.Row * 64);
            Assert.True(cells.Max(cell => cell.Bottom) < (tile.Key.Row + 1) * 64);

            int connectedEdges = 0;
            foreach (AtlasAlphaCell cell in cells)
            {
                int rightIndex = Array.FindIndex(cells, candidate =>
                    candidate.GridX == cell.GridX + 1 &&
                    candidate.GridY == cell.GridY);
                if (rightIndex >= 0)
                {
                    AtlasAlphaCell rightCell = cells[rightIndex];
                    Assert.Equal(cell.Right, rightCell.X, precision: 3);
                    connectedEdges++;
                }

                int belowIndex = Array.FindIndex(cells, candidate =>
                    candidate.GridX == cell.GridX &&
                    candidate.GridY == cell.GridY + 1);
                if (belowIndex >= 0)
                {
                    AtlasAlphaCell belowCell = cells[belowIndex];
                    Assert.Equal(cell.Bottom, belowCell.Y, precision: 3);
                    connectedEdges++;
                }
            }

            Assert.True(connectedEdges >= 3);
        }
    }

    [Fact]
    public void EveryTetrominoKindCreatesItsOwnConcretePieceClass()
    {
        Type[] expectedTypes =
        [
            typeof(IPiece),
            typeof(JPiece),
            typeof(LPiece),
            typeof(OPiece),
            typeof(SPiece),
            typeof(TPiece),
            typeof(ZPiece)
        ];

        Type[] actualTypes = Enum.GetValues<TetrominoKind>()
            .Select(kind => TetrominoPiece.Create(kind).GetType())
            .ToArray();

        Assert.Equal(expectedTypes, actualTypes);
    }

    private static double Number(XElement element, string attribute) =>
        double.Parse(
            element.Attribute(attribute)?.Value ??
                throw new InvalidOperationException(
                    $"SVG element is missing '{attribute}'."),
            CultureInfo.InvariantCulture);

    private readonly record struct AtlasAlphaCell(
        double X,
        double Y,
        double Width,
        double Height)
    {
        public double Right => X + Width;
        public double Bottom => Y + Height;
        public double CenterX => X + Width / 2;
        public double CenterY => Y + Height / 2;
        public int GridX => (int)Math.Floor(CenterX / 16);
        public int GridY => (int)Math.Floor(CenterY / 16);
    }

    private sealed class TestImage(int width, int height) : Cerneala.Drawing.IDrawImage
    {
        public int Width { get; } = width;

        public int Height { get; } = height;
    }

    private sealed class TestImageLoader : IImageLoader
    {
        public Cerneala.Drawing.IDrawImage Load(string path) => new TestImage(256, 448);
    }

    private sealed class ProbeMotionClock : IMotionClock
    {
        public TimeSpan Now { get; private set; }

        public void Advance(TimeSpan elapsed) => Now += elapsed;
    }

    [Fact]
    public void PieceEffectsMatchTheConfirmedGlowAndSoftDropContract()
    {
        Assert.InRange(
            TetrisGameSurface.BlurDistanceFor(TimeSpan.FromMilliseconds(350)),
            11.999f,
            12.001f);
    }

    [Fact]
    public void ExpensivePrismAnimationPublishesQuantizedSamples()
    {
        TetrisGameSurface surface = new();

        surface.BlurDistance = 0;
        surface.BlurDistance = 0.4f;

        Assert.Equal(0, surface.AppliedBlurDistance);
        Assert.False(surface.IsBlurActive);

        surface.BlurDistance = 0.6f;

        Assert.Equal(0.6f, surface.AppliedBlurDistance);
        Assert.True(surface.IsBlurActive);
    }

    [Fact]
    public void PointerActivationThenRightArrowMovesTheActivePiece()
    {
        MainWindow window = new();
        UIRoot root = new(780, 820);
        root.VisualChildren.Add(window);
        window.Arrange(new ArrangeContext(new LayoutRect(0, 0, 780, 820)));
        TetrisGameSurface surface = DescendantsAndSelf(window)
            .OfType<TetrisGameSurface>()
            .Single();
        long initialVersion = surface.StateVersion;
        ElementInputBridge input = new();

        input.Dispatch(root, PointerPress(200, 300));
        input.Dispatch(root, KeyPress(InputKey.Right));

        Assert.NotNull(input.FocusManager.FocusedElement);
        Assert.True(surface.StateVersion > initialVersion);
    }

    [Fact]
    public void HardDropLocksExactlyOneTetrominoAndSpawnsTheNext()
    {
        TetrisGame game = new(randomSeed: 7);
        TetrominoKind first = game.CurrentKind;
        TetrominoKind expectedNext = game.NextKind;

        Assert.True(game.HardDrop());

        Assert.Equal(expectedNext, game.CurrentKind);
        Assert.Equal(4, OccupiedCellCount(game.Board));
        Assert.True(game.Score > 0);
        Assert.Contains(
            Enumerable.Range(0, TetrisBoard.Width)
                .SelectMany(x => Enumerable.Range(0, TetrisBoard.Height)
                    .Select(y => game.Board[x, y])),
            cell => cell == first);
    }

    [Fact]
    public void ActivePieceMovementDoesNotChangeTheLockedBoardVersion()
    {
        TetrisGame game = new(randomSeed: 41);
        long initialVersion = game.Board.LockedStateVersion;

        Assert.True(game.MoveHorizontal(1));

        Assert.Equal(initialVersion, game.Board.LockedStateVersion);
    }

    [Fact]
    public void LockingAPieceChangesTheLockedBoardVersion()
    {
        TetrisGame game = new(randomSeed: 43);
        long initialVersion = game.Board.LockedStateVersion;

        Assert.True(game.HardDrop());

        Assert.True(game.Board.LockedStateVersion > initialVersion);
    }

    [Fact]
    public void HorizontalMovementCannotMoveCellsOutsideTheBoard()
    {
        TetrisGame game = new(randomSeed: 11);

        while (game.MoveHorizontal(-1))
        {
        }

        Assert.All(
            game.CurrentPiece.GetCells(game.CurrentRotation),
            cell => Assert.InRange(game.CurrentX + cell.X, 0, TetrisBoard.Width - 1));
        Assert.False(game.MoveHorizontal(-1));
    }

    [Fact]
    public void HoldCanBeUsedOnlyOnceUntilThePieceLocks()
    {
        TetrisGame game = new(randomSeed: 19);
        TetrominoKind first = game.CurrentKind;

        Assert.True(game.Hold());
        Assert.Equal(first, game.HeldKind);
        Assert.False(game.Hold());

        Assert.True(game.HardDrop());
        Assert.True(game.Hold());
    }

    [Fact]
    public void PausedGameDoesNotAdvanceGravity()
    {
        TetrisGame game = new(randomSeed: 23);
        int initialY = game.CurrentY;

        Assert.True(game.TogglePause());
        game.Advance(TimeSpan.FromSeconds(10));

        Assert.Equal(initialY, game.CurrentY);
        Assert.True(game.IsPaused);
    }

    [Fact]
    public void HeldSoftDropContinuesAtTheSoftDropCadenceUntilReleased()
    {
        TetrisGame game = new(randomSeed: 31);
        int initialY = game.CurrentY;

        Assert.True(game.BeginSoftDrop());
        for (int frame = 0; frame < 4; frame++)
        {
            game.Advance(TimeSpan.FromMilliseconds(50));
        }

        int heldY = game.CurrentY;
        Assert.True(heldY >= initialY + 5);

        game.EndSoftDrop();
        game.Advance(TimeSpan.FromMilliseconds(200));

        Assert.Equal(heldY, game.CurrentY);
    }

    [Fact]
    public void RepeatedSoftDropKeyDownDoesNotApplyAnotherImmediateStep()
    {
        TetrisGame game = new(randomSeed: 37);

        Assert.True(game.BeginSoftDrop());
        int afterFirstPress = game.CurrentY;

        Assert.True(game.BeginSoftDrop());

        Assert.Equal(afterFirstPress, game.CurrentY);
    }

    [Fact]
    public void FrameTimeAdvancesGravityThroughTheContinuousGameLoop()
    {
        TetrisGame game = new(randomSeed: 29);
        int initialY = game.CurrentY;

        for (int frame = 0; frame < 4; frame++)
        {
            game.Advance(TimeSpan.FromMilliseconds(250));
        }

        Assert.True(game.CurrentY > initialY);
    }

    [Fact]
    public void FullLinesAreRemovedAndRowsAboveMoveDown()
    {
        TetrisBoard board = new();
        for (int x = 0; x < TetrisBoard.Width; x++)
        {
            board.SetCell(x, TetrisBoard.Height - 1, TetrominoKind.I);
        }

        board.SetCell(3, TetrisBoard.Height - 2, TetrominoKind.T);

        Assert.Equal(1, board.ClearFullLines());
        Assert.Equal(TetrominoKind.T, board[3, TetrisBoard.Height - 1]);
        Assert.Equal(1, OccupiedCellCount(board));
    }

    private static int OccupiedCellCount(TetrisBoard board)
    {
        int count = 0;
        for (int y = 0; y < TetrisBoard.Height; y++)
        {
            for (int x = 0; x < TetrisBoard.Width; x++)
            {
                if (board[x, y] is not null)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static IEnumerable<UIElement> DescendantsAndSelf(UIElement element)
    {
        HashSet<UIElement> visited = new(ReferenceEqualityComparer.Instance);
        return Visit(element);

        IEnumerable<UIElement> Visit(UIElement current)
        {
            if (!visited.Add(current))
            {
                yield break;
            }

            yield return current;
            foreach (UIElement child in current.LogicalChildren.Concat(
                         current.VisualChildren))
            {
                foreach (UIElement descendant in Visit(child))
                {
                    yield return descendant;
                }
            }
        }
    }

    private static (UIRoot Root, MainWindow Window, Sprite2D Sprite)
        CreateActivePieceWindow(IMotionClock? motionClock = null)
    {
        MainWindow window = new();
        UIRoot root = new(780, 820, motionClock: motionClock);
        root.VisualChildren.Add(window);
        window.Arrange(new ArrangeContext(new LayoutRect(0, 0, 780, 820)));
        root.ProcessFrame();
        TetrisGameSurface surface = DescendantsAndSelf(window)
            .OfType<TetrisGameSurface>()
            .Single();
        TestImage image = new(256, 448);
        surface.SceneModel.UpdateActivePiece(
            image,
            image,
            new Cerneala.Drawing.DrawRect(0, 0, 64, 64),
            new Cerneala.Drawing.DrawRect(3, 2, 4, 4),
            Cerneala.Drawing.Color.White,
            new Cerneala.Drawing.DrawRect(3, 16, 4, 4),
            visible: true);
        root.ProcessFrame();
        Sprite2D sprite = DescendantsAndSelf(window)
            .OfType<Sprite2D>()
            .Single(candidate =>
                candidate.Source is not null &&
                candidate.Tint.A > 55);
        Assert.True(sprite.IsVisible);
        Assert.True(GeneratedMarkup.TryGetPrismInstance(sprite, out _));
        return (root, window, sprite);
    }

    private static InputFrame PointerPress(float x, float y)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(x, y);
        PointerSnapshot current = previous.WithButton(InputMouseButton.Left, true);
        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private static InputFrame KeyPress(InputKey key)
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.FromDownKeys([key]),
            []);
    }
}
