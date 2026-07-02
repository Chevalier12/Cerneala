using Microsoft.Xna.Framework.Input;

namespace Cerneala.UI.Input.MonoGame;

public sealed class MonoGameInputSource : IInputSource
{
    private PointerSnapshot previousPointer = PointerSnapshot.Empty;
    private PointerSnapshot currentPointer = PointerSnapshot.Empty;
    private KeyboardSnapshot previousKeyboard = KeyboardSnapshot.Empty;
    private KeyboardSnapshot currentKeyboard = KeyboardSnapshot.Empty;
    private readonly List<TextInputSnapshotEvent> queuedTextInputEvents = [];

    public void QueueTextInput(string text)
    {
        queuedTextInputEvents.Add(new TextInputSnapshotEvent(text));
    }

    public InputFrame GetFrame()
    {
        previousPointer = currentPointer;
        previousKeyboard = currentKeyboard;

        currentPointer = ReadPointer();
        currentKeyboard = ReadKeyboard();

        TextInputSnapshotEvent[] textInputEvents = queuedTextInputEvents.ToArray();
        queuedTextInputEvents.Clear();

        return new InputFrame(previousPointer, currentPointer, previousKeyboard, currentKeyboard, textInputEvents);
    }

    private static PointerSnapshot ReadPointer()
    {
        MouseState state = Mouse.GetState();

        return PointerSnapshot.Empty
            .WithPosition(state.X, state.Y)
            .WithWheelValue(state.ScrollWheelValue)
            .WithButton(InputMouseButton.Left, state.LeftButton == ButtonState.Pressed)
            .WithButton(InputMouseButton.Middle, state.MiddleButton == ButtonState.Pressed)
            .WithButton(InputMouseButton.Right, state.RightButton == ButtonState.Pressed)
            .WithButton(InputMouseButton.XButton1, state.XButton1 == ButtonState.Pressed)
            .WithButton(InputMouseButton.XButton2, state.XButton2 == ButtonState.Pressed);
    }

    private static KeyboardSnapshot ReadKeyboard()
    {
        IEnumerable<InputKey> downKeys = Keyboard.GetState()
            .GetPressedKeys()
            .Select(MonoGameInputMapper.MapKey)
            .Where(key => key is not InputKey.Unknown and not InputKey.None);

        return KeyboardSnapshot.FromDownKeys(downKeys);
    }
}
