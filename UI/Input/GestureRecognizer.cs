namespace Cerneala.UI.Input;

public sealed class GestureRecognizer
{
    private readonly float dragThreshold;
    private GestureSample? downSample;
    private GestureSample? lastSample;
    private bool dragging;

    public GestureRecognizer(float dragThreshold = 4)
    {
        if (dragThreshold < 0 || !float.IsFinite(dragThreshold))
        {
            throw new ArgumentOutOfRangeException(nameof(dragThreshold));
        }

        this.dragThreshold = dragThreshold;
    }

    public IReadOnlyList<GestureEvent> Process(GestureSample sample)
    {
        List<GestureEvent> events = [];
        if (sample.IsPressed && downSample is null)
        {
            downSample = sample;
            lastSample = sample;
            return events;
        }

        if (sample.IsPressed && downSample is GestureSample start)
        {
            float totalDx = sample.X - start.X;
            float totalDy = sample.Y - start.Y;
            float lastDx = sample.X - (lastSample?.X ?? start.X);
            float lastDy = sample.Y - (lastSample?.Y ?? start.Y);
            if (!dragging && Distance(totalDx, totalDy) >= dragThreshold)
            {
                dragging = true;
                events.Add(new GestureEvent(GestureKind.DragStarted, sample.X, sample.Y, totalDx, totalDy));
            }
            else if (dragging)
            {
                events.Add(new GestureEvent(GestureKind.DragDelta, sample.X, sample.Y, lastDx, lastDy));
            }

            lastSample = sample;
            return events;
        }

        if (!sample.IsPressed && downSample is not null)
        {
            if (dragging)
            {
                events.Add(new GestureEvent(GestureKind.DragCompleted, sample.X, sample.Y));
            }
            else if (Distance(sample.X - downSample.Value.X, sample.Y - downSample.Value.Y) < dragThreshold)
            {
                events.Add(new GestureEvent(GestureKind.Tap, sample.X, sample.Y));
            }
        }

        downSample = null;
        lastSample = null;
        dragging = false;
        return events;
    }

    private static float Distance(float x, float y)
    {
        return MathF.Sqrt((x * x) + (y * y));
    }
}

public readonly record struct GestureSample(float X, float Y, bool IsPressed);

public readonly record struct GestureEvent(GestureKind Kind, float X, float Y, float DeltaX = 0, float DeltaY = 0);

public enum GestureKind
{
    Tap,
    DragStarted,
    DragDelta,
    DragCompleted
}
