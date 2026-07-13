using Cerneala.UI.Input;

namespace Cerneala.UI.Hosting.Windows;

internal sealed class Win32InputSource : IInputSource
{
    private PointerSnapshot previousPointer = PointerSnapshot.Empty;
    private PointerSnapshot currentPointer = PointerSnapshot.Empty;
    private KeyboardSnapshot previousKeyboard = KeyboardSnapshot.Empty;
    private readonly HashSet<InputKey> downKeys = [];
    private readonly List<TextInputSnapshotEvent> textInput = [];
    private bool hasPointerPosition;

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

    public bool MovePointer(int x, int y)
    {
        float logicalX = x / CoordinateScale;
        float logicalY = y / CoordinateScale;
        if (hasPointerPosition && currentPointer.X == logicalX && currentPointer.Y == logicalY)
        {
            return false;
        }

        currentPointer = currentPointer.WithPosition(logicalX, logicalY);
        hasPointerPosition = true;
        return true;
    }

    public void SetButton(InputMouseButton button, bool down)
    {
        currentPointer = currentPointer.WithButton(button, down);
    }

    public void AddWheelDelta(int delta)
    {
        currentPointer = currentPointer.WithWheelValue(currentPointer.WheelValue + delta);
    }

    public void SetKey(uint virtualKey, bool down)
    {
        InputKey key = MapKey(virtualKey);
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

    public void AddText(char character)
    {
        if (!char.IsControl(character))
        {
            textInput.Add(new TextInputSnapshotEvent(character.ToString()));
        }
    }

    private static InputKey MapKey(uint key)
    {
        if (key is >= 0x30 and <= 0x39)
        {
            return InputKey.D0 + (int)(key - 0x30);
        }

        if (key is >= 0x41 and <= 0x5A)
        {
            return InputKey.A + (int)(key - 0x41);
        }

        if (key is >= 0x70 and <= 0x7B)
        {
            return InputKey.F1 + (int)(key - 0x70);
        }

        return key switch
        {
            0x08 => InputKey.Back,
            0x09 => InputKey.Tab,
            0x0D => InputKey.Enter,
            0x1B => InputKey.Escape,
            0x20 => InputKey.Space,
            0x21 => InputKey.PageUp,
            0x22 => InputKey.PageDown,
            0x23 => InputKey.End,
            0x24 => InputKey.Home,
            0x25 => InputKey.Left,
            0x26 => InputKey.Up,
            0x27 => InputKey.Right,
            0x28 => InputKey.Down,
            0x2D => InputKey.Insert,
            0x2E => InputKey.Delete,
            0xA0 => InputKey.LeftShift,
            0xA1 => InputKey.RightShift,
            0xA2 => InputKey.LeftCtrl,
            0xA3 => InputKey.RightCtrl,
            0xA4 => InputKey.LeftAlt,
            0xA5 => InputKey.RightAlt,
            _ => InputKey.Unknown
        };
    }
}
