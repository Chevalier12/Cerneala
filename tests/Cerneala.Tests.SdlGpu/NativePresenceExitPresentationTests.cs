using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Motion.Presence;
using SkiaSharp;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.SdlGpu;

// Issue #651: an element whose presence exit runs must stay visible in the window, at its last bounds,
// fading with its presence opacity, until the exit ends.
[Collection(SdlNativeTestCollection.Name)]
public sealed class NativePresenceExitPresentationTests
{
    private static readonly Color BoxColor = new(0x2E, 0x86, 0xFF);

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void WindowScreenshotShowsExitingElementsOfEveryOwnerFadingUntilTheExitEnds()
    {
        NativeSdlApi api = new();
        using SdlGpuWindowGraphicsSessionFactory graphics = new(api, useMultisampling: true);
        using SdlWindowPlatform platform = new(api, graphics, coordinateScaleOverride: 1);
        using WindowApplicationRuntime runtime = new(platform);
        ContentPresenter presenter = new() { Width = 90, Height = 70 };
        ContentControl control = new() { Width = 90, Height = 70 };
        StackPanel panel = new() { Width = 90, Height = 70 };
        Border decorator = new() { Width = 90, Height = 70 };
        StackPanel row = new() { Orientation = Orientation.Horizontal };
        row.Children.Add(presenter);
        row.Children.Add(control);
        row.Children.Add(panel);
        row.Children.Add(decorator);
        Window window = new()
        {
            Title = "Presence exit",
            Width = 400,
            Height = 100,
            Background = new SolidColorBrush(new Color(0, 0, 0)),
            Content = row
        };
        Border[] boxes = [CreateBox(), CreateBox(), CreateBox(), CreateBox()];
        string directory = Path.Combine(
            Path.GetTempPath(),
            $"cerneala-presence-exit-{Guid.NewGuid():N}");
        try
        {
            runtime.Show(window, modal: false);
            presenter.Content = boxes[0];
            control.Content = boxes[1];
            panel.Children.Add(boxes[2]);
            decorator.Child = boxes[3];
            PumpUntil(() => boxes.All(box => box.PresenceOpacity >= 1), "the boxes entered");
            LayoutRect[] lastBounds = boxes.Select(box => box.ArrangedBounds).ToArray();

            presenter.Content = null;
            control.Content = null;
            Assert.True(panel.Children.Remove(boxes[2]));
            decorator.Child = null;
            runtime.PumpOnce(TimeSpan.Zero);

            Assert.All(boxes, box => Assert.True(box.PresenceOpacity > 0.9f, $"opacity {box.PresenceOpacity}"));
            AssertBoxesShowPresenceOpacity("removed.png", lastBounds);

            PumpUntil(() => boxes.All(box => box.PresenceOpacity < 0.6f), "the exit reached its middle");
            Assert.All(boxes, box => Assert.Equal(PresenceState.Exiting, window.Root!.Motion.Presence.GetState(box)));
            AssertBoxesShowPresenceOpacity("middle.png", lastBounds);

            PumpUntil(
                () => boxes.All(box => window.Root!.Motion.Presence.GetState(box) == PresenceState.Detached),
                "the exit ended");
            using SKBitmap ended = Capture("ended.png");
            Assert.All(lastBounds, bounds => AssertPixel(ended, bounds, 0));
        }
        finally
        {
            runtime.Close(window, force: true);
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        void PumpUntil(Func<bool> condition, string description)
        {
            Assert.True(SpinWait.SpinUntil(() =>
            {
                runtime.PumpOnce(TimeSpan.Zero);
                return condition();
            }, TimeSpan.FromSeconds(10)), $"Not reached: {description}.");
        }

        void AssertBoxesShowPresenceOpacity(string fileName, LayoutRect[] lastBounds)
        {
            using SKBitmap bitmap = Capture(fileName);
            for (int index = 0; index < boxes.Length; index++)
            {
                AssertPixel(bitmap, lastBounds[index], boxes[index].PresenceOpacity);
            }
        }

        SKBitmap Capture(string fileName)
        {
            string path = Path.Combine(directory, fileName);
            window.SaveScreenshot(path);
            return SKBitmap.Decode(path);
        }
    }

    // The window background is black, so the center pixel is the box color scaled by its opacity.
    private static void AssertPixel(SKBitmap bitmap, LayoutRect bounds, float opacity)
    {
        SKColor pixel = bitmap.GetPixel(
            (int)(bounds.X + (bounds.Width / 2)),
            (int)(bounds.Y + (bounds.Height / 2)));
        Assert.InRange((int)pixel.Red, Expected(BoxColor.R) - 4, Expected(BoxColor.R) + 4);
        Assert.InRange((int)pixel.Green, Expected(BoxColor.G) - 4, Expected(BoxColor.G) + 4);
        Assert.InRange((int)pixel.Blue, Expected(BoxColor.B) - 4, Expected(BoxColor.B) + 4);

        int Expected(byte channel) => (int)MathF.Round(channel * opacity);
    }

    private static Border CreateBox() => new()
    {
        Width = 80,
        Height = 60,
        Background = new SolidColorBrush(BoxColor),
        Presence = PresenceOptions.FadeAndScale(
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(300)),
            MotionFactory.Tween<float>(TimeSpan.FromSeconds(2)))
    };
}
