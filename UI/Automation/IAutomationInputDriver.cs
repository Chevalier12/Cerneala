using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.UI.Automation;

public interface IAutomationInputDriver
{
    void Click(UIElement target);

    Task DragAsync(
        UIElement target,
        float startXRatio,
        float startYRatio,
        float endXRatio,
        float endYRatio,
        int steps = 12,
        CancellationToken cancellationToken = default) =>
        Task.FromException(new NotSupportedException(
            $"{GetType().Name} does not support pointer dragging."));

    void PressKey(InputKey key, AutomationModifiers modifiers = AutomationModifiers.None);

    void SendText(string text);
}
