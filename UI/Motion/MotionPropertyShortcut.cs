using Cerneala.UI.Core;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Input;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Motion;

public sealed class MotionPropertyShortcut<T>
{
    private readonly MotionElementFacade facade;
    private readonly UiProperty<T> property;

    internal MotionPropertyShortcut(MotionElementFacade facade, UiProperty<T> property)
    {
        this.facade = facade ?? throw new ArgumentNullException(nameof(facade));
        this.property = property ?? throw new ArgumentNullException(nameof(property));
    }

    public MotionHandle To(T value, MotionSpec<T> spec)
    {
        return facade.Animate(property).To(value).With(spec);
    }

    public void Bind(ScrollMotionBinding<T> binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        binding.Bind(facade.Element, property);
    }
}
