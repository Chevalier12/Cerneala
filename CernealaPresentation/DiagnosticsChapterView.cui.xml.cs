using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;

namespace Cerneala.Presentation;

public partial class DiagnosticsChapterView : UserControl
{
    private bool skipNextRefresh;

    internal void UpdateDiagnostics(UiFrame frame)
    {
        if (skipNextRefresh)
        {
            skipNextRefresh = false;
            return;
        }

        DiagFrameTime.Text = $"{frame.ProcessingTime.TotalMilliseconds:0.00} ms";
        DiagLayout.Text = $"{frame.Stats.MeasuredElements} / {frame.Stats.ArrangedElements}";
        DiagRender.Text = $"{frame.Stats.RenderedElements} / {frame.Stats.HitTestElements}";
        DiagSummary.Text = frame.Stats.HasWork ? "dirty work committed" : "idle fast path";
        skipNextRefresh = true;
    }
}
