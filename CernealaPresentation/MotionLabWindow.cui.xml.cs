using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.Presentation;

public partial class MotionLabWindow : Window
{
    private bool initialized;

    internal event EventHandler? TweenSpecRequested;
    internal event EventHandler? SpringLooseRequested;
    internal event EventHandler? SpringBalancedRequested;
    internal event EventHandler? SpringFirmRequested;
    internal event EventHandler? SpringQuickLooseRequested;
    internal event EventHandler? SpringQuickBalancedRequested;
    internal event EventHandler? SpringQuickFirmRequested;
    internal event EventHandler? SpringTightLooseRequested;
    internal event EventHandler? SpringTightBalancedRequested;
    internal event EventHandler? SpringTightFirmRequested;

    private void OnContentRendered(object? sender, EventArgs args)
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CERNEALA_MOTION_LAB_CAPTURE")))
        {
            TweenModeCheck.IsChecked = true;
        }

        RunSpec();
        _ = CaptureIfRequestedAsync();
    }

    private void OnRun(UiElementId sender, RoutedEventArgs args) => RunSpec();

    private void RunSpec()
    {
        LabStatus.Text = "SAMPLING";
        if (TweenModeCheck.IsChecked == true)
        {
            LabReadout.Text = "tween(duration: 820ms, easing: emphasized)";
            TweenSpecRequested?.Invoke(this, EventArgs.Empty);
            _ = MarkSettledAsync(900);
            return;
        }

        int stiffness = StiffnessSlider.Value < 400 ? 280 : StiffnessSlider.Value < 700 ? 520 : 820;
        int damping = DampingSlider.Value < 28 ? 20 : DampingSlider.Value < 50 ? 38 : 60;
        LabReadout.Text = $"spring(stiffness: {stiffness}, damping: {damping})";
        SelectSpringEvent(stiffness, damping)?.Invoke(this, EventArgs.Empty);
        _ = MarkSettledAsync(1_400);
    }

    private EventHandler? SelectSpringEvent(int stiffness, int damping)
    {
        return (stiffness, damping) switch
        {
            (280, 20) => SpringLooseRequested,
            (280, 38) => SpringBalancedRequested,
            (280, 60) => SpringFirmRequested,
            (520, 20) => SpringQuickLooseRequested,
            (520, 38) => SpringQuickBalancedRequested,
            (520, 60) => SpringQuickFirmRequested,
            (820, 20) => SpringTightLooseRequested,
            (820, 38) => SpringTightBalancedRequested,
            _ => SpringTightFirmRequested
        };
    }

    private async Task MarkSettledAsync(int delayMilliseconds)
    {
        await Task.Delay(delayMilliseconds);
        LabStatus.Text = "SETTLED";
    }

    private async Task CaptureIfRequestedAsync()
    {
        string? path = Environment.GetEnvironmentVariable("CERNEALA_MOTION_LAB_CAPTURE");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        await Task.Delay(500);
        string fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            FrameRendered -= handler;
            SaveScreenshot(fullPath);
            completion.TrySetResult();
        };

        FrameRendered += handler;
        InvalidateRenderTree(this);
        await completion.Task;
        Close();
    }

    private static void InvalidateRenderTree(UIElement element)
    {
        element.Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.Render, "motion lab automation screenshot");
        foreach (UIElement child in element.VisualChildren)
        {
            InvalidateRenderTree(child);
        }
    }

    private void OnClose(UiElementId sender, RoutedEventArgs args) => Close();
}
