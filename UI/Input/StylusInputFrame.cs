namespace Cerneala.UI.Input;

public sealed record StylusInputFrame(IReadOnlyList<StylusInputPoint> Points)
{
    public StylusInputFrame(params StylusInputPoint[] points)
        : this((IReadOnlyList<StylusInputPoint>)points)
    {
    }
}
