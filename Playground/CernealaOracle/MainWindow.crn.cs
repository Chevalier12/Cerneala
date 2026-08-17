using Cerneala.UI.Controls;
using Cerneala.UI.Layout;
using System.Globalization;

namespace CernealaOracle;

public partial class MainWindow : Window
{
    private bool captured;
    private bool configured;

    private void OnFrameRendered(object? sender, EventArgs args)
    {
        if (!configured)
        {
            configured = true;
            bool changed = false;
            bool motionLabHeader = string.Equals(
                Environment.GetEnvironmentVariable("CERNEALA_ORACLE_SCENARIO"),
                "motion-lab-header",
                StringComparison.OrdinalIgnoreCase);
            if (motionLabHeader)
            {
                Width = 900;
                Height = 68;
                DefaultCanvas.Visibility = Visibility.Collapsed;
                MotionLabCanvas.Visibility = Visibility.Visible;
                changed = true;
            }

            string? value = Environment.GetEnvironmentVariable("CERNEALA_ORACLE_FONT_SIZE");
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float fontSize) &&
                float.IsFinite(fontSize) &&
                fontSize > 0 &&
                OracleText.FontSize != fontSize)
            {
                OracleText.FontSize = fontSize;
                changed = true;
            }

            string? fontFamily = Environment.GetEnvironmentVariable("CERNEALA_ORACLE_FONT_FAMILY");
            if (!string.IsNullOrWhiteSpace(fontFamily) && OracleText.FontFamily != fontFamily)
            {
                OracleText.FontFamily = fontFamily;
                changed = true;
            }

            string? text = Environment.GetEnvironmentVariable("CERNEALA_ORACLE_TEXT");
            if (text is not null && OracleText.Text != text)
            {
                OracleText.Text = text;
                changed = true;
            }

            if (changed)
            {
                return;
            }
        }

        string? path = Environment.GetEnvironmentVariable("CERNEALA_ORACLE_SCREENSHOT");
        if (captured || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        captured = true;
        string fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        SaveScreenshot(fullPath);

        bool motionLabScenario = MotionLabCanvas.Visibility == Visibility.Visible;
        TextBlock measuredText = motionLabScenario ? MotionLabText : OracleText;
        File.WriteAllLines(Path.ChangeExtension(fullPath, ".metrics.txt"),
        [
            $"Scenario={(motionLabScenario ? "motion-lab-header" : "text")}",
            $"Text={measuredText.Text}",
            $"FontFamily={measuredText.FontFamily}",
            $"FontSize={measuredText.FontSize:R}",
            $"DesiredSize={measuredText.DesiredSize}",
            $"ArrangedBounds={measuredText.ArrangedBounds}"
        ]);
        Close();
    }
}
