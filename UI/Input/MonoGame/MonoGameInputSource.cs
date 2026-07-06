using Cerneala.UI.Hosting;
using Microsoft.Xna.Framework.Input;

namespace Cerneala.UI.Input.MonoGame;

public sealed class MonoGameInputSource : IInputSource
{
    private readonly Func<MouseState> readMouseState;
    private readonly Func<KeyboardState> readKeyboardState;
    private PointerSnapshot previousPointer = PointerSnapshot.Empty;
    private PointerSnapshot currentPointer = PointerSnapshot.Empty;
    private KeyboardSnapshot previousKeyboard = KeyboardSnapshot.Empty;
    private KeyboardSnapshot currentKeyboard = KeyboardSnapshot.Empty;
    private readonly List<TextInputSnapshotEvent> queuedTextInputEvents = [];
    private float coordinateScale = 1;

    public MonoGameInputSource()
        : this(Mouse.GetState, Keyboard.GetState)
    {
    }

    internal MonoGameInputSource(Func<MouseState> readMouseState, Func<KeyboardState> readKeyboardState)
    {
        this.readMouseState = readMouseState ?? throw new ArgumentNullException(nameof(readMouseState));
        this.readKeyboardState = readKeyboardState ?? throw new ArgumentNullException(nameof(readKeyboardState));
    }

    public float CoordinateScale
    {
        get => coordinateScale;
        set
        {
            UiCoordinateMapper.ValidateScale(value);
            coordinateScale = value;
        }
    }

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

    private PointerSnapshot ReadPointer()
    {
        MouseState state = readMouseState();

        return PointerSnapshot.Empty
            .WithPosition(
                UiCoordinateMapper.PhysicalToLogical(state.X, coordinateScale),
                UiCoordinateMapper.PhysicalToLogical(state.Y, coordinateScale))
            .WithWheelValue(state.ScrollWheelValue)
            .WithButton(InputMouseButton.Left, state.LeftButton == ButtonState.Pressed)
            .WithButton(InputMouseButton.Middle, state.MiddleButton == ButtonState.Pressed)
            .WithButton(InputMouseButton.Right, state.RightButton == ButtonState.Pressed)
            .WithButton(InputMouseButton.XButton1, state.XButton1 == ButtonState.Pressed)
            .WithButton(InputMouseButton.XButton2, state.XButton2 == ButtonState.Pressed);
    }

    private KeyboardSnapshot ReadKeyboard()
    {
        IEnumerable<InputKey> downKeys = readKeyboardState()
            .GetPressedKeys()
            .Select(MonoGameInputMapper.MapKey)
            .Where(key => key is not InputKey.Unknown and not InputKey.None);

        return KeyboardSnapshot.FromDownKeys(downKeys);
    }
}
