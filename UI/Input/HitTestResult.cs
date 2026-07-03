using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class HitTestResult
{
    public HitTestResult(UIElement element, UiElementId elementId, float x, float y)
    {
        Element = element ?? throw new ArgumentNullException(nameof(element));
        ElementId = elementId;
        X = x;
        Y = y;
    }

    public UIElement Element { get; }

    public UiElementId ElementId { get; }

    public float X { get; }

    public float Y { get; }
}
