using Cerneala.UI.Controls;
using Cerneala.UI.Servo;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.InvestorReel;

// Verification harness only: the films themselves are authored entirely in markup.
// Input goes through Servo and screenshots through the window-owned API.
//
// CERNEALA_REEL_THEME (Ink, Cyberpunk, Brutalist, Sakura) clicks that theme's poster on the picker
// once it has settled. Capture times are then measured from the click; without a theme they are
// measured from the first rendered frame, which shows the picker.
// Stills:    CERNEALA_REEL_CAPTURE_DIR (+ optional CERNEALA_REEL_CAPTURE_TIMES, comma-separated ms).
// Recording: CERNEALA_REEL_RECORD_DIR (+ optional CERNEALA_REEL_RECORD_SECONDS, default 85) saves
//            frames back to back and writes frames.txt with each frame's capture time in ms.
// CERNEALA_REEL_THEN=home|exit presses that end-of-film button after the captures, once it appears,
//            and saves "after-home.png" when returning to the picker.
// CERNEALA_REEL_CLOSE_AFTER_CAPTURE=1 closes the window when the run finishes.
// CERNEALA_REEL_MARK_FILE receives the UTC ticks of the moment the film starts.
public partial class ReelWindow
{
    private bool captureStarted;

    private void OnContentRendered(object? sender, EventArgs args)
    {
        if (captureStarted)
        {
            return;
        }

        string? recordDirectory = Environment.GetEnvironmentVariable("CERNEALA_REEL_RECORD_DIR");
        string? stillDirectory = Environment.GetEnvironmentVariable("CERNEALA_REEL_CAPTURE_DIR");
        if (string.IsNullOrWhiteSpace(recordDirectory) && string.IsNullOrWhiteSpace(stillDirectory))
        {
            return;
        }

        captureStarted = true;

        // Servo resolves targets in layout coordinates and does not apply render transforms, so a
        // harness run shows the page at its native 1600x900 instead of the 0.8 fit-to-window scale.
        Page.Scale = 1;
        Width = 1600;
        Height = 900;
        _ = RunHarnessAsync(recordDirectory, stillDirectory);
    }

    private async Task RunHarnessAsync(string? recordDirectory, string? stillDirectory)
    {
        ServoApi servo = new(this, new ServoOptions { DefaultTimeout = TimeSpan.FromSeconds(240) });
        string? theme = Environment.GetEnvironmentVariable("CERNEALA_REEL_THEME");
        string log = Path.GetFullPath((stillDirectory ?? recordDirectory)!);
        Directory.CreateDirectory(log);
        try
        {
            if (!string.IsNullOrWhiteSpace(theme))
            {
                // Click the poster once the picker's entrance has made it actionable.
                ServoTarget poster = ServoTarget.ById("theme-" + theme.Trim().ToLowerInvariant());
                for (int attempt = 0; ; attempt++)
                {
                    await Task.Delay(1000);
                    try
                    {
                        await servo.ClickAsync(poster);
                        break;
                    }
                    catch (ServoTargetNotActionableException) when (attempt < 60)
                    {
                    }
                }
            }

            string? markFile = Environment.GetEnvironmentVariable("CERNEALA_REEL_MARK_FILE");
            if (!string.IsNullOrWhiteSpace(markFile))
            {
                File.WriteAllText(markFile, DateTime.UtcNow.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            if (!string.IsNullOrWhiteSpace(recordDirectory))
            {
                await RecordAsync(servo, Path.GetFullPath(recordDirectory));
            }
            else
            {
                await CaptureAsync(servo, Path.GetFullPath(stillDirectory!));
            }

            string? then = Environment.GetEnvironmentVariable("CERNEALA_REEL_THEN")?.Trim().ToLowerInvariant();
            if (then is "home" or "exit")
            {
                ServoTarget button = ServoTarget.ById("reel-" + then);
                await servo.WaitForAsync(button, ServoCondition.Visible);
                await Task.Delay(1000);
                await servo.ClickAsync(button);
                if (then == "home")
                {
                    await Task.Delay(4500);
                    await servo.SaveScreenshotAsync(Path.Combine(log, "after-home.png"));
                }
            }
        }
        catch (Exception exception)
        {
            await File.WriteAllTextAsync(Path.Combine(log, "harness-error.txt"), exception.ToString());
        }

        if (Environment.GetEnvironmentVariable("CERNEALA_REEL_CLOSE_AFTER_CAPTURE") == "1")
        {
            Close();
        }
    }

    private static async Task CaptureAsync(ServoApi servo, string directory)
    {
        Directory.CreateDirectory(directory);
        int[] times = (Environment.GetEnvironmentVariable("CERNEALA_REEL_CAPTURE_TIMES") ?? "2000")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse)
            .Order()
            .ToArray();
        System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
        foreach (int time in times)
        {
            int remaining = time - (int)clock.ElapsedMilliseconds;
            if (remaining > 0)
            {
                await Task.Delay(remaining);
            }

            await servo.SaveScreenshotAsync(Path.Combine(directory, $"reel-{time:000000}ms.png"));
        }
    }

    private static async Task RecordAsync(ServoApi servo, string directory)
    {
        Directory.CreateDirectory(directory);
        double seconds = double.TryParse(
            Environment.GetEnvironmentVariable("CERNEALA_REEL_RECORD_SECONDS"),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out double requested) ? requested : 85;
        List<string> log = [];
        System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
        for (int frame = 0; clock.Elapsed.TotalSeconds < seconds; frame++)
        {
            string name = $"frame-{frame:00000}.png";
            double requestedAt = clock.Elapsed.TotalMilliseconds;
            await servo.SaveScreenshotAsync(Path.Combine(directory, name));
            log.Add(string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"{name} {requestedAt:F1} {clock.Elapsed.TotalMilliseconds:F1}"));
        }

        await File.WriteAllLinesAsync(Path.Combine(directory, "frames.txt"), log);
    }
}
