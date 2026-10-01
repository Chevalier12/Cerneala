using Cerneala.UI.Controls;
using Cerneala.UI.Servo;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.SolarSystem;

// Verification harness only. With CERNEALA_SOLAR_TOUR_DIR set, a scripted tour drives the app
// through real input (Servo click and wheel) and saves frames through the window-owned screenshot
// API. CERNEALA_SOLAR_TOUR_CLOSE=1 closes the window afterwards.
public partial class MainWindow
{
    private bool tourStarted;

    private void OnContentRendered(object? sender, EventArgs args)
    {
        string? directory = Environment.GetEnvironmentVariable("CERNEALA_SOLAR_TOUR_DIR");
        if (tourStarted || string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        tourStarted = true;
        _ = RunTourAsync(Path.GetFullPath(directory));
    }

    private async Task RunTourAsync(string directory)
    {
        Directory.CreateDirectory(directory);
        ServoApi.SetId(SunHit, "sun");
        ServoApi.SetId(EarthHit, "earth");
        ServoApi.SetId(JupiterHit, "jupiter");
        ServoApi.SetId(SaturnHit, "saturn");
        ServoApi.SetId(SpeedMonth, "speed-month");
        ServoApi.SetId(SpeedPause, "speed-pause");
        ServoApi.SetId(OverviewButton, "overview");
        ServoApi.SetId(Viewport, "viewport");
        ServoApi servo = new(this, new ServoOptions { DefaultTimeout = TimeSpan.FromSeconds(20) });
        string log = Path.Combine(directory, "tour-log.txt");

        async Task Shot(string name, int delayMs)
        {
            await Task.Delay(delayMs);
            await servo.SaveScreenshotAsync(Path.Combine(directory, name + ".png"));
            await File.AppendAllTextAsync(log, name + Environment.NewLine);
        }

        async Task Focus(string id, string name)
        {
            await servo.ClickAsync(ServoTarget.ById("overview"));
            await Task.Delay(2600);
            await servo.ClickAsync(ServoTarget.ById(id));
            await Shot(name, 3200);
        }

        try
        {
            await Shot("01-intro", 900);
            await Shot("02-overview", 3600);

            // Servo acts on stable targets, so the orbits are paused (through the real button) while it aims.
            await servo.ClickAsync(ServoTarget.ById("speed-pause"));
            await Task.Delay(400);
            await Focus("jupiter", "03-jupiter");
            await Focus("earth", "04-earth");
            await File.WriteAllTextAsync(Path.Combine(directory, "stop.txt"), "");
            await Focus("saturn", "05-saturn");
            await Focus("sun", "06-sun");
            await servo.ClickAsync(ServoTarget.ById("overview"));
            await servo.ClickAsync(ServoTarget.ById("speed-month"));
            await Shot("07-overview-month", 3200);
            await servo.ScrollAsync(ServoTarget.ById("viewport"), 480);
            await Shot("08-zoomed", 2200);
        }
        catch (Exception exception)
        {
            await File.WriteAllTextAsync(Path.Combine(directory, "tour-error.txt"), exception.ToString());
        }

        if (Environment.GetEnvironmentVariable("CERNEALA_SOLAR_TOUR_CLOSE") == "1")
        {
            Close();
        }
    }
}
