using Cerneala.UI.Input;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Servo;

internal interface IServoInputDriver
{
    Task HoverAsync(Func<UIRoot, ServoActionTarget> resolveTarget, CancellationToken cancellationToken);

    Task ClickAsync(Func<UIRoot, ServoActionTarget> resolveTarget, CancellationToken cancellationToken);

    Task DragAsync(
        Func<UIRoot, ServoActionTarget> resolveTarget,
        float endX,
        float endY,
        int steps,
        CancellationToken cancellationToken);

    Task ScrollAsync(Func<UIRoot, ServoActionTarget> resolveTarget, int wheelDelta, CancellationToken cancellationToken);

    Task PressKeyAsync(InputKey key, ServoModifiers modifiers, CancellationToken cancellationToken);

    Task SendTextAsync(string text, CancellationToken cancellationToken);
}
