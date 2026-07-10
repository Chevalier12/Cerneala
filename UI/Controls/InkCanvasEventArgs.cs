using Cerneala.UI.Ink;

namespace Cerneala.UI.Controls;

public sealed class InkCanvasStrokeCollectedEventArgs : EventArgs
{
    public InkCanvasStrokeCollectedEventArgs(Stroke stroke)
    {
        Stroke = stroke ?? throw new ArgumentNullException(nameof(stroke));
    }

    public Stroke Stroke { get; }
}
