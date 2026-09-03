using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Markup;
using Cerneala.UI.Media;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Servo;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.SdlGpuSmoke;

public partial class MainWindow : Window
{
    private static readonly PrismNodeId SmokeLayerId = new(1);

    private IDisposable? prismLifetime;
    private Window? secondaryWindow;
    private int mainFrames;
    private int secondaryFrames;
    private bool inputObserved;
    private bool servoActivated;
    private bool completed;

    private void OnContentRendered(object? sender, EventArgs args)
    {
        SmokeOptions options = SmokeOptions.Current;
        StatusText.Text = $"mode: {options.Mode}";

        if (options.Mode == "prism")
        {
            PrismInstance prism = new(new PrismCompositionDefinition(
                "SdlGpuSmoke",
                [new PrismLayerDefinition(
                    SmokeLayerId,
                    "SmokeTarget",
                    styles: [new PrismStyleDefinition(PrismStyleId.OuterGlow)])]));
            prismLifetime = GeneratedMarkup.AttachPrism(PrismTarget, () => prism);
            PrismTarget.Invalidate(InvalidationFlags.Render, "SDL_GPU smoke Prism attachment");
        }

        if (options.Mode == "multi-window")
        {
            SmokeDrawingSurface secondarySurface = new();
            secondaryWindow = new Window
            {
                Title = "Cerneala SDL_GPU smoke secondary",
                Width = 420,
                Height = 300,
                Left = 80,
                Top = 80,
                Background = new SolidColorBrush(new Color(22, 41, 57)),
                Content = secondarySurface
            };
            secondaryWindow.FrameRendered += (_, _) => secondaryFrames++;
            secondaryWindow.Show();
        }

        if (options.Mode == "servo")
        {
            _ = RunServoAsync(options);
        }
    }

    private void OnServoClick(UiElementId sender, RoutedEventArgs args)
    {
        servoActivated = true;
        ServoButton.Content = "SERVO ACTIVE";
        StatusText.Text = "servo: activated";
    }

    private void OnPreviewKeyDown(UiElementId sender, RoutedEventArgs args)
    {
        inputObserved = true;
        StatusText.Text = args is KeyEventArgs key
            ? $"input: {key.Key}"
            : "input observed";
    }

    private void OnFrameRendered(object? sender, EventArgs args)
    {
        if (completed)
        {
            return;
        }

        mainFrames++;
        SmokeOptions options = SmokeOptions.Current;
        if (options.Mode == "servo")
        {
            return;
        }

        if (options.Mode == "resize" && mainFrames == 1)
        {
            Width = 720;
            Height = 460;
            return;
        }

        if (options.Mode == "multi-window" && secondaryFrames == 0)
        {
            return;
        }

        if (options.Mode == "input" && options.RequireInput && !inputObserved)
        {
            if (mainFrames < 600)
            {
                return;
            }

            Console.Error.WriteLine("SDL_GPU_SMOKE_FAIL input was required but no key event arrived.");
            completed = true;
            Application.Current?.Shutdown(2);
            return;
        }

        if (mainFrames < 2)
        {
            return;
        }

        completed = true;
        Directory.CreateDirectory(options.ArtifactDirectory);
        if (options.CaptureScreenshots)
        {
            SaveScreenshot(Path.Combine(options.ArtifactDirectory, $"{options.Mode}-main.png"));
            secondaryWindow?.SaveScreenshot(
                Path.Combine(options.ArtifactDirectory, $"{options.Mode}-secondary.png"));
        }

        Console.WriteLine(
            $"SDL_GPU_SMOKE_OK mode={options.Mode} mainFrames={mainFrames} " +
            $"secondaryFrames={secondaryFrames} inputObserved={inputObserved}");
        secondaryWindow?.Close();
        Close();
    }

    private async Task RunServoAsync(SmokeOptions options)
    {
        string fullPath = Path.Combine(options.ArtifactDirectory, "servo-main.png");
        string targetPath = Path.Combine(options.ArtifactDirectory, "servo-target.png");
        string errorPath = Path.Combine(options.ArtifactDirectory, "servo.error.txt");
        try
        {
            Directory.CreateDirectory(options.ArtifactDirectory);
            ServoApi servo = new(this);
            ServoTarget button = ServoTarget.ById("servo-target");
            _ = await servo.FindAsync(button);
            await servo.ClickAsync(button);
            await servo.WaitUntilAsync(async token =>
                (await servo.FindAsync(ServoTarget.ById("servo-status"), token)).Name == "servo: activated");
            if (!servoActivated)
            {
                throw new InvalidOperationException("Servo input completed without the routed click handler running.");
            }

            if (options.CaptureScreenshots)
            {
                await servo.SaveScreenshotAsync(fullPath);
                await servo.SaveScreenshotAsync(button, targetPath);
            }

            completed = true;
            Console.WriteLine(
                $"SDL_GPU_SMOKE_OK mode=servo state={StatusText.Text} " +
                $"full={fullPath} target={targetPath}");
            Close();
        }
        catch (Exception exception)
        {
            completed = true;
            Directory.CreateDirectory(options.ArtifactDirectory);
            await File.WriteAllTextAsync(errorPath, exception.ToString());
            Console.Error.WriteLine($"SDL_GPU_SMOKE_FAIL servo: {exception}");
            Application.Current?.Shutdown(2);
        }
    }

    protected override void OnDetached()
    {
        prismLifetime?.Dispose();
        prismLifetime = null;
        base.OnDetached();
    }
}
