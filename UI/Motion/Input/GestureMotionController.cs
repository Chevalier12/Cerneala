using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Motion.Input;

public sealed class GestureMotionController
{
    private readonly UIElement element;

    internal GestureMotionController(UIElement element)
    {
        this.element = element ?? throw new ArgumentNullException(nameof(element));
    }

    public PointerMotionState State { get; private set; }

    public void PointerPressed(MotionSpec<float> spec)
    {
        State = PointerMotionState.Pressed;
        element.Motion().Scale.To(0.97f, spec);
    }

    public void PointerReleased(MotionSpec<float> spec)
    {
        State = PointerMotionState.Idle;
        element.Motion().Scale.To(1, spec);
    }
}
