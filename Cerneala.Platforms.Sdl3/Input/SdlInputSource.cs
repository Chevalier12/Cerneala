using Cerneala.UI.Input;

namespace Cerneala.Platforms.Sdl3;

internal sealed class SdlInputSource : IInputSource
{
    private PointerSnapshot previousPointer = PointerSnapshot.Empty;
    private PointerSnapshot currentPointer = PointerSnapshot.Empty;
    private KeyboardSnapshot previousKeyboard = KeyboardSnapshot.Empty;
    private readonly HashSet<InputKey> downKeys = [];
    private readonly List<TextInputSnapshotEvent> textInput = [];

    public float CoordinateScale { get; set; } = 1;

    public InputFrame GetFrame()
    {
        KeyboardSnapshot currentKeyboard = KeyboardSnapshot.FromDownKeys(downKeys);
        InputFrame frame = new(previousPointer, currentPointer, previousKeyboard, currentKeyboard, textInput.ToArray());
        previousPointer = currentPointer;
        previousKeyboard = currentKeyboard;
        textInput.Clear();
        return frame;
    }

    public void MovePointer(float x, float y) => currentPointer =
        currentPointer.WithPosition(
            x / CoordinateScale,
            y / CoordinateScale);

    public void LeavePointer() => currentPointer = currentPointer.WithPosition(-1, -1);

    public void SetButton(byte button, bool down)
    {
        InputMouseButton mapped = button switch
        {
            1 => InputMouseButton.Left,
            2 => InputMouseButton.Middle,
            3 => InputMouseButton.Right,
            4 => InputMouseButton.XButton1,
            5 => InputMouseButton.XButton2,
            _ => InputMouseButton.None
        };
        currentPointer = currentPointer.WithButton(mapped, down);
    }

    public void AddWheel(float y, bool flipped)
    {
        int delta = checked((int)MathF.Round(y * 120));
        currentPointer = currentPointer.WithWheelValue(
            checked(currentPointer.WheelValue + (flipped ? -delta : delta)));
    }

    public void SetKey(int scancode, bool down)
    {
        InputKey key = MapScancode(scancode);
        if (key is InputKey.None or InputKey.Unknown)
        {
            return;
        }

        if (down)
        {
            downKeys.Add(key);
        }
        else
        {
            downKeys.Remove(key);
        }
    }

    public void AddText(string? text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            textInput.Add(new TextInputSnapshotEvent(text));
        }
    }

    private static InputKey MapScancode(int scancode)
    {
        if (scancode is >= 4 and <= 29)
        {
            return InputKey.A + (scancode - 4);
        }

        if (scancode is >= 30 and <= 38)
        {
            return InputKey.D1 + (scancode - 30);
        }

        if (scancode is >= 58 and <= 69)
        {
            return InputKey.F1 + (scancode - 58);
        }

        return scancode switch
        {
            39 => InputKey.D0,
            40 => InputKey.Enter,
            41 => InputKey.Escape,
            42 => InputKey.Back,
            43 => InputKey.Tab,
            44 => InputKey.Space,
            73 => InputKey.Insert,
            74 => InputKey.Home,
            75 => InputKey.PageUp,
            76 => InputKey.Delete,
            77 => InputKey.End,
            78 => InputKey.PageDown,
            79 => InputKey.Right,
            80 => InputKey.Left,
            81 => InputKey.Down,
            82 => InputKey.Up,
            224 => InputKey.LeftCtrl,
            225 => InputKey.LeftShift,
            226 => InputKey.LeftAlt,
            228 => InputKey.RightCtrl,
            229 => InputKey.RightShift,
            230 => InputKey.RightAlt,
            _ => InputKey.Unknown
        };
    }
}
