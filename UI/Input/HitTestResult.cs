using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class HitTestResult
{
    public HitTestResult(UIElement element, float x, float y)
    {
        Element = element ?? throw new ArgumentNullException(nameof(element));
        X = x;
        Y = y;
    }

    public UIElement Element { get; }

    public float X { get; }

    public float Y { get; }
}
