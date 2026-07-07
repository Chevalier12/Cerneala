using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Input;

namespace Cerneala.UI.Motion;

public sealed class MotionElementFacade
{
    private readonly UIElement element;

    internal MotionElementFacade(UIElement element)
    {
        this.element = element ?? throw new ArgumentNullException(nameof(element));
    }

    public MotionPropertyShortcut<float> Opacity => new(this, UIElement.OpacityProperty);

    public MotionPropertyShortcut<float> TranslateX => new(this, UIElement.TranslateXProperty);

    public MotionPropertyShortcut<float> TranslateY => new(this, UIElement.TranslateYProperty);

    public MotionPropertyShortcut<float> Scale => new(this, UIElement.ScaleProperty);

    public MotionAnimationBuilder<T> Animate<T>(UiProperty<T> property)
    {
        ArgumentNullException.ThrowIfNull(property);
        return new MotionAnimationBuilder<T>(this, property);
    }

    public MotionStateBuilder States()
    {
        return new MotionStateBuilder(this);
    }

    public GestureMotionController Gestures()
    {
        return new GestureMotionController(element);
    }

    public DragMotionController Drag()
    {
        return new DragMotionController(element);
    }

    public ScrollTimeline ScrollTimeline()
    {
        return element is ScrollViewer scrollViewer
            ? new ScrollTimeline(scrollViewer)
            : throw new InvalidOperationException("ScrollTimeline can only be created for ScrollViewer elements.");
    }

    internal UIElement Element => element;

    internal Core.MotionSystem ResolveMotion()
    {
        if (element.Root is not null)
        {
            return element.Root.Motion;
        }

        if (element is UIRoot root)
        {
            return root.Motion;
        }

        throw new InvalidOperationException($"Element '{element.GetType().Name}' must be attached to a UIRoot before motion can animate its properties.");
    }
}
