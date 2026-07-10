using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls.Templates;

public sealed class TemplateRecyclePool
{
    private readonly Dictionary<TemplateRecycleKey, Stack<UIElement>> pool = [];

    public void Release(TemplateRecycleKey key, UIElement element)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(element);
        Reset(element);
        if (!pool.TryGetValue(key, out Stack<UIElement>? stack))
        {
            stack = new Stack<UIElement>();
            pool[key] = stack;
        }

        stack.Push(element);
    }

    public UIElement? Rent(TemplateRecycleKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return pool.TryGetValue(key, out Stack<UIElement>? stack) && stack.Count > 0
            ? stack.Pop()
            : null;
    }

    private static void Reset(UIElement element)
    {
        if (element is ContentPresenter presenter)
        {
            presenter.Content = null;
            presenter.ContentTemplate = null;
            presenter.ContentTemplateKey = null;
            presenter.ContentIndex = -1;
        }
    }
}
