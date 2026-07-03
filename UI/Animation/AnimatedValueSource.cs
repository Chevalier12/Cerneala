using Cerneala.UI.Core;

namespace Cerneala.UI.Animation;

public static class AnimatedValueSource
{
    public static void Apply<T>(UiObject target, UiProperty<T> property, T value)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(property);

        target.SetValue(property, value, UiPropertyValueSource.Animation);
    }

    public static void Clear<T>(UiObject target, UiProperty<T> property)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(property);

        target.ClearValue(property, UiPropertyValueSource.Animation);
    }
}
