using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;
using MotionSpec = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Presentation;

public partial class MotionLabWindow : Window
{
    private bool initialized;

    internal event EventHandler? SpecRequested;

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

        MotionSpec<float> movement;
        if (TweenModeCheck.IsChecked == true)
        {
            movement = MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(820), Easings.Emphasized);
            LabReadout.Text = "tween(duration: 820ms, easing: emphasized)";
        }
        else
        {
            float stiffness = StiffnessSlider.Value;
            float damping = DampingSlider.Value;
            movement = MotionSpec.Spring<float>(stiffness, damping);
            LabReadout.Text = $"spring(stiffness: {stiffness:0}, damping: {damping:0})";
        }

        SpecRequested?.Invoke(this, EventArgs.Empty);
        MotionHandle movementHandle = LabTarget.Motion()
            .Animate(UIElement.TranslateXProperty)
            .From(0f)
            .To(430f)
            .With(movement);
        _ = MarkSettledAsync(movementHandle);
    }

    private async Task MarkSettledAsync(MotionHandle handle)
    {
        try
        {
            await handle.Completion;
            LabStatus.Text = "SETTLED";
        }
        catch (OperationCanceledException)
        {
        }
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
