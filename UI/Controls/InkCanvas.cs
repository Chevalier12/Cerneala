using Cerneala.Drawing;
using Cerneala.UI.Ink;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;

namespace Cerneala.UI.Controls;

public sealed class InkCanvas : Layout.Panels.Canvas
{
    private readonly Dictionary<InkInputKey, Stroke> activeStrokes = [];

    public InkCanvas()
    {
        Strokes.Changed += (_, _) => Invalidate(InvalidationFlags.Measure | InvalidationFlags.Render, "Ink strokes changed");
    }

    public StrokeCollection Strokes { get; } = new();

    public event EventHandler<InkCanvasStrokeCollectedEventArgs>? StrokeCollected;

    public void ApplyStylus(StylusInputPoint point)
    {
        ApplyPoint(point.Action switch
        {
            StylusInputAction.Down => InkInputAction.Down,
            StylusInputAction.Move => InkInputAction.Move,
            StylusInputAction.Up => InkInputAction.Up,
            _ => InkInputAction.Move
        }, InkInputKind.Stylus, point.Id, point.X, point.Y);
    }

    public void ApplyTouch(TouchInputPoint point)
    {
        ApplyPoint(point.Action switch
        {
            TouchInputAction.Down => InkInputAction.Down,
            TouchInputAction.Move => InkInputAction.Move,
            TouchInputAction.Up => InkInputAction.Up,
            _ => InkInputAction.Move
        }, InkInputKind.Touch, point.Id, point.X, point.Y);
    }

    private void ApplyPoint(InkInputAction action, InkInputKind kind, int id, float x, float y)
    {
        InkInputKey key = new(kind, id);
        if (action == InkInputAction.Down)
        {
            Stroke stroke = new();
            stroke.AddPoint(new DrawPoint(x, y));
            activeStrokes[key] = stroke;
            Strokes.Add(stroke);
            return;
        }

        if (!activeStrokes.TryGetValue(key, out Stroke? activeStroke))
        {
            return;
        }

        activeStroke.AddPoint(new DrawPoint(x, y));
        Invalidate(InvalidationFlags.Measure | InvalidationFlags.Render, "Ink stroke point added");
        if (action == InkInputAction.Up)
        {
            activeStrokes.Remove(key);
            StrokeCollected?.Invoke(this, new InkCanvasStrokeCollectedEventArgs(activeStroke));
        }
    }

    private readonly record struct InkInputKey(InkInputKind Kind, int Id);

    private enum InkInputKind
    {
        Stylus,
        Touch
    }

    private enum InkInputAction
    {
        Down,
        Move,
        Up
    }
}
