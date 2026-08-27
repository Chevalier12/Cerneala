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

namespace Cerneala.SdlGpuSmoke;

public partial class MainWindow : Window
{
    private static readonly PrismNodeId SmokeLayerId = new(1);

    private IDisposable? prismLifetime;
    private Window? secondaryWindow;
    private int mainFrames;
    private int secondaryFrames;
    private bool inputObserved;
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

    protected override void OnDetached()
    {
        prismLifetime?.Dispose();
        prismLifetime = null;
        base.OnDetached();
    }
}
