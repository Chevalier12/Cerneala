using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.Presentation;

public partial class MotionLabWindow : Window
{
    private bool initialized;

    private void OnContentRendered(object? sender, EventArgs args)
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        _ = CaptureIfRequestedAsync();
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
