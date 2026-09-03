using Cerneala.UI.Input;

namespace Cerneala.UI.Servo;

internal interface IServoInputDriver
{
    Task HoverAsync(float x, float y, CancellationToken cancellationToken);

    Task ClickAsync(float x, float y, CancellationToken cancellationToken);

    Task DragAsync(
        float startX,
        float startY,
        float endX,
        float endY,
        int steps,
        CancellationToken cancellationToken);

    Task ScrollAsync(float x, float y, int wheelDelta, CancellationToken cancellationToken);

    Task PressKeyAsync(InputKey key, ServoModifiers modifiers, CancellationToken cancellationToken);

    Task SendTextAsync(string text, CancellationToken cancellationToken);
}
