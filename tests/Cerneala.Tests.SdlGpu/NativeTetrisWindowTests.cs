using System.Collections.Specialized;
using System.Numerics;
using Cerneala.Backends.SdlGpu;
using Cerneala.Platforms.Sdl3;
using Cerneala.Tetris;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Input;
using Cerneala.UI.Servo;
using SkiaSharp;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class NativeTetrisWindowTests
{
    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void RealWindowInputRendersObservableLockedPiecesThroughTheSceneTemplate()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"Cerneala-native-tetris-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string beforePath = Path.Combine(directory, "before-drop.png");
        string afterPath = Path.Combine(directory, "after-drop.png");
        NativeSdlApi api = new();
        using SdlGpuWindowGraphicsSessionFactory graphics = new(api, useMultisampling: false);
        using SdlWindowPlatform platform = new(api, graphics, coordinateScaleOverride: 1);
        using WindowApplicationRuntime runtime = new(platform);
        MainWindow window = new();
        int addedModels = 0;
        try
        {
            runtime.Show(window, modal: false);
            TetrisGameSurface surface = Assert.Single(DescendantsAndSelf(window).OfType<TetrisGameSurface>());
            SceneItems2D items = Assert.Single(DescendantsAndSelf(window).OfType<SceneItems2D>());
            TetrisSceneModel model = surface.SceneModel;
            Wait(() => surface.PresentationState == RenderSurface2DPresentationState.Ready &&
                model.CurrentVisible && model.CurrentImage?.DirectImage is not null &&
                ReferenceEquals(items.ItemsSource, model.LockedPieces));
            Assert.Empty(model.LockedPieces);
            Assert.Equal(0, items.RealizedItemCount);

            ServoApi.SetId(surface, "tetris-board");
            ServoApi servo = new(window);
            Run(servo.ClickAsync(ServoTarget.ById("tetris-board")));
            float startingX = model.CurrentDestination.X;
            Run(servo.PressKeyAsync(InputKey.Right));
            Wait(() => model.CurrentDestination.X == startingX + 1);

            Run(servo.PressKeyAsync(InputKey.P));
            Wait(() => surface.IsPaused && DescendantsAndSelf(window).OfType<TextBlock>()
                .Any(text => text.Text == "PAUSED"));
            Run(servo.PressKeyAsync(InputKey.P));
            Wait(() => !surface.IsPaused && !DescendantsAndSelf(window).OfType<TextBlock>()
                .Any(text => text.Text == "PAUSED"));

            runtime.PumpOnce(TimeSpan.Zero);
            window.SaveScreenshot(beforePath);
            model.LockedPieces.CollectionChanged += CountAdds;
            try
            {
                Run(servo.PressKeyAsync(InputKey.Space));
                Wait(() => model.LockedPieces.Count == 4 && items.RealizedItemCount == 4);
            }
            finally { model.LockedPieces.CollectionChanged -= CountAdds; }
            Assert.Equal(4, addedModels);
            Assert.Same(model.LockedPieces, items.ItemsSource);
            Sprite2D[] sprites = items.LogicalChildren.Cast<Sprite2D>().ToArray();
            Assert.Equal(4, sprites.Length);
            for (int index = 0; index < sprites.Length; index++)
            {
                TetrisSpriteModel locked = model.LockedPieces[index];
                Assert.Same(locked, sprites[index].DataContext);
                Assert.Same(locked.Image, sprites[index].Image);
                Assert.Equal(locked.Destination, new Cerneala.Drawing.DrawRect(
                    sprites[index].X, sprites[index].Y, sprites[index].Width, sprites[index].Height));
            }

            runtime.PumpOnce(TimeSpan.Zero);
            window.SaveScreenshot(afterPath);
            using SKBitmap before = SKBitmap.Decode(beforePath);
            using SKBitmap after = SKBitmap.Decode(afterPath);
            Assert.Equal(before.Width, after.Width);
            Assert.Equal(before.Height, after.Height);
            foreach (TetrisSpriteModel locked in model.LockedPieces)
            {
                Vector2 point = surface.SceneToRoot(new Vector2(locked.X + 0.5f, locked.Y + 0.5f));
                int x = (int)MathF.Round(point.X), y = (int)MathF.Round(point.Y);
                Assert.InRange(x, 0, after.Width - 1);
                Assert.InRange(y, 0, after.Height - 1);
                Assert.NotEqual(before.GetPixel(x, y), after.GetPixel(x, y));
            }
        }
        finally
        {
            runtime.Close(window, force: true);
            File.Delete(beforePath);
            File.Delete(afterPath);
            Directory.Delete(directory);
        }

        void CountAdds(object? sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.Action == NotifyCollectionChangedAction.Add) { addedModels += args.NewItems?.Count ?? 0; }
        }
        void Run(Task action)
        {
            Wait(() => action.IsCompleted);
            Assert.True(action.IsCompletedSuccessfully, action.Exception?.ToString());
        }
        void Wait(Func<bool> done) => Assert.True(SpinWait.SpinUntil(() =>
        {
            runtime.PumpOnce(TimeSpan.Zero);
            return done();
        }, TimeSpan.FromSeconds(15)), "Native Tetris window did not reach the required state.");
    }

    private static IEnumerable<UIElement> DescendantsAndSelf(UIElement element)
    {
        HashSet<UIElement> visited = new(ReferenceEqualityComparer.Instance);
        return Visit(element);

        IEnumerable<UIElement> Visit(UIElement current)
        {
            if (!visited.Add(current)) { yield break; }
            yield return current;
            foreach (UIElement child in current.LogicalChildren.Concat(current.VisualChildren))
            {
                foreach (UIElement descendant in Visit(child)) { yield return descendant; }
            }
        }
    }
}
