using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls;

public sealed class ItemContainerRecyclePool
{
    private readonly Dictionary<Type, Stack<UIElement>> containers = [];

    public int Count => containers.Values.Sum(stack => stack.Count);

    public void Push(UIElement container)
    {
        ArgumentNullException.ThrowIfNull(container);
        Type key = container.GetType();
        if (!containers.TryGetValue(key, out Stack<UIElement>? stack))
        {
            stack = new Stack<UIElement>();
            containers.Add(key, stack);
        }

        stack.Push(container);
    }

    public UIElement? Pop(Type containerType)
    {
        ArgumentNullException.ThrowIfNull(containerType);
        return containers.TryGetValue(containerType, out Stack<UIElement>? stack) && stack.Count > 0
            ? stack.Pop()
            : null;
    }

    public void Clear()
    {
        containers.Clear();
    }
}
