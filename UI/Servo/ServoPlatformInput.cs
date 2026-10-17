using System.Globalization;
using Cerneala.UI.Input;

namespace Cerneala.UI.Servo;

public sealed class ServoPlatformInput
{
    private ServoPlatformInput(
        ServoPlatformInputKind kind,
        InputKey key = InputKey.None,
        bool repeat = false,
        string? text = null,
        ServoPoint position = default,
        InputMouseButton button = InputMouseButton.None,
        int wheelDelta = 0)
    {
        Kind = kind;
        Key = key;
        Repeat = repeat;
        CommittedText = text;
        Position = position;
        Button = button;
        WheelDelta = wheelDelta;
    }

    internal ServoPlatformInputKind Kind { get; }

    internal InputKey Key { get; }

    internal bool Repeat { get; }

    internal string? CommittedText { get; }

    internal ServoPoint Position { get; }

    internal InputMouseButton Button { get; }

    internal int WheelDelta { get; }

    public static ServoPlatformInput KeyDown(InputKey key, bool repeat = false)
    {
        ValidateKey(key);
        return new ServoPlatformInput(ServoPlatformInputKind.KeyDown, key: key, repeat: repeat);
    }

    public static ServoPlatformInput KeyUp(InputKey key)
    {
        ValidateKey(key);
        return new ServoPlatformInput(ServoPlatformInputKind.KeyUp, key: key);
    }

    public static ServoPlatformInput Text(string text)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
        return new ServoPlatformInput(ServoPlatformInputKind.Text, text: text);
    }

    public static ServoPlatformInput PointerMove(ServoPoint position) =>
        new(ServoPlatformInputKind.PointerMove, position: position);

    public static ServoPlatformInput PointerDown(ServoPoint position, InputMouseButton button)
    {
        ValidateButton(button);
        return new ServoPlatformInput(ServoPlatformInputKind.PointerDown, position: position, button: button);
    }

    public static ServoPlatformInput PointerUp(ServoPoint position, InputMouseButton button)
    {
        ValidateButton(button);
        return new ServoPlatformInput(ServoPlatformInputKind.PointerUp, position: position, button: button);
    }

    public static ServoPlatformInput Wheel(ServoPoint position, int wheelDelta)
    {
        if (wheelDelta == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(wheelDelta),
                wheelDelta,
                "A platform wheel event needs a non-zero delta.");
        }

        return new ServoPlatformInput(ServoPlatformInputKind.Wheel, position: position, wheelDelta: wheelDelta);
    }

    public override string ToString() => Kind switch
    {
        ServoPlatformInputKind.KeyDown => Repeat ? $"KeyDown({Key}, repeat)" : $"KeyDown({Key})",
        ServoPlatformInputKind.KeyUp => $"KeyUp({Key})",
        ServoPlatformInputKind.Text => $"Text(\"{CommittedText}\")",
        ServoPlatformInputKind.PointerMove => $"PointerMove({Format(Position)})",
        ServoPlatformInputKind.PointerDown => $"PointerDown({Format(Position)}, {Button})",
        ServoPlatformInputKind.PointerUp => $"PointerUp({Format(Position)}, {Button})",
        _ => $"Wheel({Format(Position)}, {WheelDelta.ToString(CultureInfo.InvariantCulture)})"
    };

    private static string Format(ServoPoint point) =>
        string.Create(CultureInfo.InvariantCulture, $"{point.X}, {point.Y}");

    private static void ValidateKey(InputKey key)
    {
        if (!Enum.IsDefined(key) || key is InputKey.None or InputKey.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(key), key, "A platform key event needs a real key.");
        }
    }

    private static void ValidateButton(InputMouseButton button)
    {
        if (!Enum.IsDefined(button) || button == InputMouseButton.None)
        {
            throw new ArgumentOutOfRangeException(nameof(button), button, "A platform pointer button event needs a real button.");
        }
    }
}

internal enum ServoPlatformInputKind
{
    KeyDown,
    KeyUp,
    Text,
    PointerMove,
    PointerDown,
    PointerUp,
    Wheel
}
