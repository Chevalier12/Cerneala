namespace Cerneala.UI.Input;

public sealed class ManipulationProcessor
{
    private readonly Dictionary<int, ManipulationPoint> points = [];
    private ManipulationSnapshot? previous;

    public ManipulationDelta Process(IReadOnlyList<ManipulationPoint> activePoints)
    {
        ArgumentNullException.ThrowIfNull(activePoints);
        points.Clear();
        foreach (ManipulationPoint point in activePoints)
        {
            points[point.Id] = point;
        }

        ManipulationSnapshot current = ManipulationSnapshot.From(points.Values);
        ManipulationDelta delta = previous is null
            ? new ManipulationDelta(0, 0, 1)
            : current.CreateDelta(previous.Value);
        previous = current;
        return delta;
    }

    public void Reset()
    {
        points.Clear();
        previous = null;
    }
}

public readonly record struct ManipulationPoint(int Id, float X, float Y);

public readonly record struct ManipulationDelta(float TranslationX, float TranslationY, float Scale);

internal readonly record struct ManipulationSnapshot(float CenterX, float CenterY, float Distance)
{
    public static ManipulationSnapshot From(IEnumerable<ManipulationPoint> points)
    {
        ManipulationPoint[] array = points.ToArray();
        if (array.Length == 0)
        {
            return new ManipulationSnapshot(0, 0, 0);
        }

        float centerX = array.Average(point => point.X);
        float centerY = array.Average(point => point.Y);
        float distance = array.Length < 2 ? 0 : MathF.Sqrt(MathF.Pow(array[1].X - array[0].X, 2) + MathF.Pow(array[1].Y - array[0].Y, 2));
        return new ManipulationSnapshot(centerX, centerY, distance);
    }

    public ManipulationDelta CreateDelta(ManipulationSnapshot previous)
    {
        float scale = previous.Distance <= 0 || Distance <= 0 ? 1 : Distance / previous.Distance;
        return new ManipulationDelta(CenterX - previous.CenterX, CenterY - previous.CenterY, scale);
    }
}
