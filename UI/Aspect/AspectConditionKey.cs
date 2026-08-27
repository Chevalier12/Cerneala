using System.Runtime.CompilerServices;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.UI.Aspect;

public sealed class AspectConditionKey
{
    private readonly ConditionalWeakTable<UIElement, State> states = new();

    public AspectConditionKey(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Aspect condition key name cannot be empty.", nameof(name));
        }

        Name = name;
    }

    public string Name { get; }

    public bool IsActive(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return states.TryGetValue(element, out State? state) && state.Active;
    }

    public bool SetActive(UIElement element, bool active)
    {
        ArgumentNullException.ThrowIfNull(element);
        State state = states.GetOrCreateValue(element);
        if (state.Active == active)
        {
            return false;
        }

        state.Active = active;
        element.Invalidate(InvalidationFlags.Aspect, $"Aspect condition '{Name}' changed");
        return true;
    }

    public override string ToString() => Name;

    private sealed class State
    {
        public bool Active { get; set; }
    }
}
