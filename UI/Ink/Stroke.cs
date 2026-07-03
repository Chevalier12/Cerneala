using Cerneala.Drawing;

namespace Cerneala.UI.Ink;

public sealed class Stroke
{
    private readonly List<DrawPoint> points = [];

    public Stroke(IEnumerable<DrawPoint>? points = null)
    {
        if (points is not null)
        {
            this.points.AddRange(points);
        }
    }

    public IReadOnlyList<DrawPoint> Points => points;

    public void AddPoint(DrawPoint point)
    {
        points.Add(point);
    }
}
