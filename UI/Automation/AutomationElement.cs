using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.UI.Automation;

public sealed class AutomationElement
{
    private readonly AutomationSession session;

    internal AutomationElement(AutomationSession session, UIElement element)
    {
        this.session = session;
        Element = element;
    }

    public UIElement Element { get; }

    public string? AutomationId => AutomationProperties.GetAutomationId(Element);

    public string TypeName => Element.GetType().Name;

    public AutomationElement Click()
    {
        session.Input.Click(Element);
        return this;
    }

    public Task DragAsync(
        float startXRatio,
        float startYRatio,
        float endXRatio,
        float endYRatio,
        int steps = 12,
        CancellationToken cancellationToken = default)
    {
        return session.Input.DragAsync(
            Element,
            startXRatio,
            startYRatio,
            endXRatio,
            endYRatio,
            steps,
            cancellationToken);
    }

    public AutomationElement PressKey(InputKey key, AutomationModifiers modifiers = AutomationModifiers.None)
    {
        session.Input.PressKey(key, modifiers);
        return this;
    }

    public AutomationElement SendText(string text)
    {
        session.Input.SendText(text);
        return this;
    }
}
