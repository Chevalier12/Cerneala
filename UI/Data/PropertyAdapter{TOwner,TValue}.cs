using Cerneala.UI.Core;

namespace Cerneala.UI.Data;

public sealed class PropertyAdapter<TOwner, TValue>
{
    private readonly Func<TOwner, TValue> getter;
    private readonly Action<TOwner, TValue>? setter;

    public PropertyAdapter(Func<TOwner, TValue> getter, Action<TOwner, TValue>? setter = null)
    {
        this.getter = getter ?? throw new ArgumentNullException(nameof(getter));
        this.setter = setter;
    }

    public bool CanWrite => setter is not null;

    public TValue Read(TOwner owner)
    {
        return getter(owner);
    }

    public void Write(TOwner owner, TValue value)
    {
        if (setter is null)
        {
            throw new InvalidOperationException("Property adapter is read-only.");
        }

        setter(owner, value);
    }

    public static PropertyAdapter<TUiOwner, TValue> ForUiProperty<TUiOwner>(UiProperty<TValue> property)
        where TUiOwner : UiObject
    {
        ArgumentNullException.ThrowIfNull(property);
        return new PropertyAdapter<TUiOwner, TValue>(
            owner => owner.GetValue(property),
            (owner, value) => owner.SetValue(property, value));
    }
}
