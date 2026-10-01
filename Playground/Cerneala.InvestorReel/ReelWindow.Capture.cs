using Cerneala.UI.Controls;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.InvestorReel;

// Verification harness only: the film itself is authored entirely in markup.
// Everything here goes through the window-owned screenshot API.
//
// Stills:    CERNEALA_REEL_CAPTURE_DIR (+ optional CERNEALA_REEL_CAPTURE_TIMES, comma-separated ms).
// Recording: CERNEALA_REEL_RECORD_DIR (+ optional CERNEALA_REEL_RECORD_SECONDS, default 85) saves
//            frames back to back and writes frames.txt with each frame's capture time in ms.
// CERNEALA_REEL_CLOSE_AFTER_CAPTURE=1 closes the window when either mode finishes.
// CERNEALA_REEL_MARK_FILE receives the UTC ticks of the first rendered frame.
public partial class ReelWindow : Window
{
    private bool captureStarted;

    private void OnContentRendered(object? sender, EventArgs args)
    {
        if (captureStarted)
        {
            return;
        }

        // External recorders use this to find where the film starts in their footage.
        string? markFile = Environment.GetEnvironmentVariable("CERNEALA_REEL_MARK_FILE");
        if (!string.IsNullOrWhiteSpace(markFile))
        {
            File.WriteAllText(markFile, DateTime.UtcNow.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        string? recordDirectory = Environment.GetEnvironmentVariable("CERNEALA_REEL_RECORD_DIR");
        string? stillDirectory = Environment.GetEnvironmentVariable("CERNEALA_REEL_CAPTURE_DIR");
        if (!string.IsNullOrWhiteSpace(recordDirectory))
        {
            captureStarted = true;
            _ = RecordAsync(Path.GetFullPath(recordDirectory));
        }
        else if (!string.IsNullOrWhiteSpace(stillDirectory))
        {
            captureStarted = true;
            _ = CaptureAsync(Path.GetFullPath(stillDirectory));
        }
    }

    private async Task CaptureAsync(string directory)
    {
        Directory.CreateDirectory(directory);
        int[] times = (Environment.GetEnvironmentVariable("CERNEALA_REEL_CAPTURE_TIMES") ?? "2000")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse)
            .Order()
            .ToArray();
        ServoApi servo = new(this);
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

        CloseIfRequested();
    }

    private async Task RecordAsync(string directory)
    {
        Directory.CreateDirectory(directory);
        double seconds = double.TryParse(
            Environment.GetEnvironmentVariable("CERNEALA_REEL_RECORD_SECONDS"),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out double requested) ? requested : 85;
        ServoApi servo = new(this);
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
        CloseIfRequested();
    }

    private void CloseIfRequested()
    {
        if (Environment.GetEnvironmentVariable("CERNEALA_REEL_CLOSE_AFTER_CAPTURE") == "1")
        {
            Close();
        }
    }
}
