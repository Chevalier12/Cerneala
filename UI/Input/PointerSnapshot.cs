namespace Cerneala.UI.Input;

public sealed class PointerSnapshot
{
    private readonly IReadOnlyDictionary<InputMouseButton, bool> buttons;

    private PointerSnapshot(float x, float y, int wheelValue, IReadOnlyDictionary<InputMouseButton, bool> buttons)
    {
        X = x;
        Y = y;
        WheelValue = wheelValue;
        this.buttons = buttons;
    }

    public static PointerSnapshot Empty { get; } = new(0, 0, 0, new Dictionary<InputMouseButton, bool>());

    public float X { get; }

    public float Y { get; }

    public int WheelValue { get; }

    public bool IsDown(InputMouseButton button)
    {
        return buttons.TryGetValue(button, out bool isDown) && isDown;
    }

    public PointerSnapshot WithPosition(float x, float y)
    {
        return new PointerSnapshot(x, y, WheelValue, buttons);
    }

    public PointerSnapshot WithWheelValue(int wheelValue)
    {
        return new PointerSnapshot(X, Y, wheelValue, buttons);
    }

    public PointerSnapshot WithButton(InputMouseButton button, bool isDown)
    {
        Dictionary<InputMouseButton, bool> nextButtons = new(buttons)
        {
            [button] = isDown
        };

        return new PointerSnapshot(X, Y, WheelValue, nextButtons);
    }
}
