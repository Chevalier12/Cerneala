using Cerneala.UI.Elements;
using Cerneala.UI.Controls;
using Cerneala.UI.Input;

namespace Cerneala.UI.Aspect;

public sealed class AspectStateSet : IEquatable<AspectStateSet>
{
    private readonly HashSet<AspectState> states;
    private readonly AspectState[] orderedStates;

    public static AspectStateSet Empty { get; } = new([]);

    private AspectStateSet(IEnumerable<AspectState> states)
    {
        this.states = new HashSet<AspectState>(states);
        orderedStates = this.states.OrderBy(state => state.Name, StringComparer.Ordinal).ToArray();
    }

    public bool Contains(AspectState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return states.Contains(state);
    }

    public AspectStateSet Add(AspectState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (states.Contains(state))
        {
            return this;
        }

        return new AspectStateSet(states.Append(state));
    }

    public AspectStateSet Remove(AspectState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!states.Contains(state))
        {
            return this;
        }

        return new AspectStateSet(states.Where(existing => !existing.Equals(state)));
    }

    public static AspectStateSet FromElement(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        List<AspectState> activeStates = new(6);
        if (element.IsPointerOver)
        {
            activeStates.Add(AspectState.Hover);
        }

        if (element is IInputPressable { IsPressed: true })
        {
            activeStates.Add(AspectState.Pressed);
        }

        if (element.IsKeyboardFocused)
        {
            activeStates.Add(AspectState.Focus);
        }

        if (element.IsKeyboardFocusWithin)
        {
            activeStates.Add(AspectState.FocusWithin);
        }

        if (!element.IsEnabled)
        {
            activeStates.Add(AspectState.Disabled);
        }

        if (element is ISelectableItemContainer { IsSelected: true })
        {
            activeStates.Add(AspectState.Selected);
        }

        return activeStates.Count == 0 ? Empty : new AspectStateSet(activeStates);
    }

    public bool Equals(AspectStateSet? other)
    {
        return other is not null && states.SetEquals(other.states);
    }

    public override bool Equals(object? obj)
    {
        return obj is AspectStateSet other && Equals(other);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();
        foreach (AspectState state in orderedStates)
        {
            hash.Add(state);
        }

        return hash.ToHashCode();
    }

    public override string ToString()
    {
        return string.Join(", ", orderedStates.Select(state => state.Name));
    }
}
