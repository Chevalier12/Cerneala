using Cerneala.UI.Controls;
using Cerneala.UI.Ink;
using Cerneala.UI.Input;

namespace Cerneala.Tests.Controls;

public sealed class InkCanvasTests
{
    [Fact]
    public void InkCanvasRecordsStylusStrokeInOrder()
    {
        InkCanvas canvas = new();

        canvas.ApplyStylus(new StylusInputPoint(1, 1, 2, StylusInputAction.Down));
        canvas.ApplyStylus(new StylusInputPoint(1, 3, 4, StylusInputAction.Move));
        canvas.ApplyStylus(new StylusInputPoint(1, 5, 6, StylusInputAction.Up));

        Stroke stroke = Assert.Single(canvas.Strokes);
        Assert.Equal(3, stroke.Points.Count);
        Assert.Equal(1, stroke.Points[0].X);
        Assert.Equal(6, stroke.Points[2].Y);
    }

    [Fact]
    public void StrokeCollectionNotifiesOnMutation()
    {
        StrokeCollection strokes = new();
        Stroke stroke = new();
        List<StrokeCollectionChangeKind> changes = [];
        strokes.Changed += (_, args) => changes.Add(args.Kind);

        strokes.Add(stroke);
        strokes.Remove(stroke);

        Assert.Equal([StrokeCollectionChangeKind.Added, StrokeCollectionChangeKind.Removed], changes);
    }

    [Fact]
    public void InkCanvasKeepsConcurrentTouchStrokesSeparatedById()
    {
        InkCanvas canvas = new();

        canvas.ApplyTouch(new TouchInputPoint(1, 0, 0, TouchInputAction.Down));
        canvas.ApplyTouch(new TouchInputPoint(2, 10, 10, TouchInputAction.Down));
        canvas.ApplyTouch(new TouchInputPoint(1, 1, 1, TouchInputAction.Move));
        canvas.ApplyTouch(new TouchInputPoint(2, 11, 11, TouchInputAction.Move));
        canvas.ApplyTouch(new TouchInputPoint(1, 2, 2, TouchInputAction.Up));
        canvas.ApplyTouch(new TouchInputPoint(2, 12, 12, TouchInputAction.Up));

        Assert.Equal(2, canvas.Strokes.Count);
        Assert.Equal([0, 1, 2], canvas.Strokes[0].Points.Select(point => point.X));
        Assert.Equal([10, 11, 12], canvas.Strokes[1].Points.Select(point => point.X));
    }
}
