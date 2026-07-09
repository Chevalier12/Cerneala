namespace Cerneala.UI.Input;

public sealed class StylusEventArgs : RoutedEventArgs
{
    public StylusEventArgs(RoutedEvent routedEvent, object originalSource, StylusInputPoint point)
        : base(routedEvent, originalSource)
    {
        Point = point ?? throw new ArgumentNullException(nameof(point));
    }

    public StylusInputPoint Point { get; }

    public int StylusId => Point.Id;

    public float X => Point.X;

    public float Y => Point.Y;

    public float Pressure => Point.Pressure;

    public bool IsInRange => Point.IsInRange;

    public string? Button => Point.Button;
}
