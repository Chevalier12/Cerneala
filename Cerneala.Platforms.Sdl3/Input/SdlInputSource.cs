using Cerneala.UI.Input;

namespace Cerneala.Platforms.Sdl3;

// Turns the ordered SDL event stream into level-sampled input frames. An InputFrame
// reports a press or release as the difference between two snapshots, so a frame may
// hold at most one transition of each button, and only events whose arrival order
// matches the order ElementInputBridge dispatches them in (position, wheel and buttons,
// keys, text). Key events read Ctrl, Shift and Alt from the frame's final snapshot and
// are dispatched in InputKey order, so a frame also holds the events of one key only:
// at most one transition, as the frame's first key event, then repeats of that key.
// An event that breaks a rule closes the frame; it and every later event wait in
// arrival order for the next frames, and the source asks for them.
// Input that is not a transition (a move, a wheel notch, an auto-repeat of a held key)
// may join a frame without transitions out of order, so input that alternates between
// kinds does not need one frame per alternation and never falls further behind.
internal sealed class SdlInputSource : IInputSource
{
    private readonly Action? frameRequested;
    private PointerSnapshot previousPointer = PointerSnapshot.Empty;
    private PointerSnapshot currentPointer = PointerSnapshot.Empty;
    private KeyboardSnapshot previousKeyboard = KeyboardSnapshot.Empty;
    private readonly HashSet<InputKey> downKeys = [];
    private readonly List<TextInputSnapshotEvent> textInput = [];
    private readonly List<InputKey> keyRepeats = [];
    private readonly HashSet<InputMouseButton> changedButtons = [];
    private readonly List<PendingInput> deferred = [];
    private FramePhase framePhase;
    private bool hasTransition;
    private InputKey frameKey = InputKey.None;
    private InputKey lastKeyDown = InputKey.None;
    private InputKey textKey = InputKey.None;
    private bool hasPointerPosition;

    public SdlInputSource(Action? frameRequested = null)
    {
        this.frameRequested = frameRequested;
    }

    public float CoordinateScale { get; set; } = 1;

    public InputFrame GetFrame()
    {
        KeyboardSnapshot currentKeyboard = KeyboardSnapshot.FromDownKeys(downKeys);
        InputFrame frame = new(previousPointer, currentPointer, previousKeyboard, currentKeyboard, textInput.ToArray(), keyRepeats.ToArray());
        previousPointer = currentPointer;
        previousKeyboard = currentKeyboard;
        textInput.Clear();
        keyRepeats.Clear();
        frameKey = InputKey.None;
        changedButtons.Clear();
        framePhase = FramePhase.Empty;
        hasTransition = false;
        lastKeyDown = InputKey.None;
        textKey = InputKey.None;
        if (deferred.Count > 0)
        {
            int applied = 0;
            while (applied < deferred.Count && TryApply(deferred[applied], out _))
            {
                applied++;
            }

            deferred.RemoveRange(0, applied);
            frameRequested?.Invoke();
        }

        return frame;
    }

    public bool MovePointer(float x, float y) =>
        Accept(new PendingInput(PendingKind.Move, X: x / CoordinateScale, Y: y / CoordinateScale));

    public void LeavePointer() => Accept(new PendingInput(PendingKind.Leave));

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
        Accept(new PendingInput(PendingKind.Button, Value: (int)mapped, Down: down));
    }

    public void AddWheel(float y, bool flipped)
    {
        int delta = checked((int)MathF.Round(y * 120));
        Accept(new PendingInput(PendingKind.Wheel, Value: flipped ? -delta : delta));
    }

    public void SetKey(int scancode, bool down, bool repeat = false)
    {
        InputKey key = MapScancode(scancode);
        if (key is InputKey.None or InputKey.Unknown)
        {
            return;
        }

        Accept(new PendingInput(PendingKind.Key, Value: (int)key, Down: down, Repeat: repeat));
    }

    public void AddText(string? text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            Accept(new PendingInput(PendingKind.Text, Text: text));
        }
    }

    // Returns whether the input changed state the next frame must show.
    private bool Accept(PendingInput input)
    {
        if (deferred.Count == 0 && TryApply(input, out bool changed))
        {
            return changed;
        }

        if (input.Kind == PendingKind.Move && deferred.Count > 0 && deferred[^1].Kind == PendingKind.Move)
        {
            // Only the last of consecutive moves matters; keep a stalled queue small.
            deferred[^1] = input;
        }
        else
        {
            deferred.Add(input);
        }

        return true;
    }

    // Applies the input to the frame under construction, or returns false when it
    // must wait for the next frame. Input that changes nothing never closes a frame.
    private bool TryApply(in PendingInput input, out bool changed)
    {
        changed = Changes(input);
        if (!changed)
        {
            return true;
        }

        FramePhase phase = input.Kind switch
        {
            PendingKind.Move or PendingKind.Leave => FramePhase.Position,
            PendingKind.Wheel or PendingKind.Button => FramePhase.WheelAndButtons,
            PendingKind.Key => FramePhase.Key,
            _ => FramePhase.Text
        };
        // An auto-repeat of a held key is an event, not a transition.
        bool repeat = input.Kind == PendingKind.Key && input.Repeat && input.Down && downKeys.Contains((InputKey)input.Value);
        if ((phase < framePhase && !CanJoinOutOfOrder(input, repeat)) ||
            (input.Kind == PendingKind.Button && changedButtons.Contains((InputMouseButton)input.Value)) ||
            (input.Kind == PendingKind.Key && !CanJoinFrameKey(input)))
        {
            return false;
        }

        if (phase > framePhase)
        {
            framePhase = phase;
        }

        hasTransition |= input.Kind == PendingKind.Button || (input.Kind == PendingKind.Key && !repeat);
        switch (input.Kind)
        {
            case PendingKind.Move:
                hasPointerPosition = true;
                currentPointer = currentPointer.WithPosition(input.X, input.Y);
                break;
            case PendingKind.Leave:
                hasPointerPosition = false;
                currentPointer = currentPointer.WithPosition(-1, -1);
                break;
            case PendingKind.Button:
                changedButtons.Add((InputMouseButton)input.Value);
                currentPointer = currentPointer.WithButton((InputMouseButton)input.Value, input.Down);
                break;
            case PendingKind.Wheel:
                currentPointer = currentPointer.WithWheelValue(checked(currentPointer.WheelValue + input.Value));
                break;
            case PendingKind.Key:
                frameKey = (InputKey)input.Value;
                lastKeyDown = input.Down ? (InputKey)input.Value : InputKey.None;
                if (repeat)
                {
                    keyRepeats.Add((InputKey)input.Value);
                    break;
                }

                if (input.Down)
                {
                    downKeys.Add((InputKey)input.Value);
                }
                else
                {
                    downKeys.Remove((InputKey)input.Value);
                }
                break;
            case PendingKind.Text:
                // SDL sends the text a key types right after that key's key-down.
                textKey = lastKeyDown != InputKey.None && (textKey == InputKey.None || textKey == lastKeyDown)
                    ? lastKeyDown
                    : InputKey.Unknown;
                lastKeyDown = InputKey.None;
                textInput.Add(new TextInputSnapshotEvent(input.Text!));
                break;
        }

        return true;
    }

    // A frame dispatches its input by kind, so input that joins it out of order is raised
    // before the input of later kinds that arrived first. That is allowed for a move or a
    // wheel notch, and for an auto-repeat after the text its own key typed, while the frame
    // holds no key or button transition. Example: Ctrl auto-repeats and wheel notches that
    // alternate share one frame; a move after a button press waits for the next frame.
    private bool CanJoinOutOfOrder(in PendingInput input, bool repeat)
    {
        if (hasTransition)
        {
            return false;
        }

        return input.Kind switch
        {
            PendingKind.Move or PendingKind.Wheel => true,
            PendingKind.Key => repeat && textKey == (InputKey)input.Value,
            _ => false
        };
    }

    // A transition must be the frame's first key event: a later one would change the
    // modifier state that the frame's earlier key events are raised with.
    private bool CanJoinFrameKey(in PendingInput input) =>
        frameKey == InputKey.None ||
        (frameKey == (InputKey)input.Value && IsRepeatOfHeldKey(input));

    private bool IsRepeatOfHeldKey(in PendingInput input) =>
        input.Repeat && input.Down && downKeys.Contains((InputKey)input.Value);

    private bool Changes(in PendingInput input) => input.Kind switch
    {
        PendingKind.Move => !hasPointerPosition || currentPointer.X != input.X || currentPointer.Y != input.Y,
        PendingKind.Leave => hasPointerPosition || currentPointer.X != -1 || currentPointer.Y != -1,
        PendingKind.Button => (InputMouseButton)input.Value != InputMouseButton.None &&
            currentPointer.IsDown((InputMouseButton)input.Value) != input.Down,
        PendingKind.Wheel => input.Value != 0,
        PendingKind.Key => (input.Repeat && input.Down) || downKeys.Contains((InputKey)input.Value) != input.Down,
        _ => true
    };

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

    private enum PendingKind : byte
    {
        Move,
        Leave,
        Button,
        Wheel,
        Key,
        Text
    }

    // The order ElementInputBridge dispatches one frame's input in. The wheel shares the
    // buttons' rank: it is not a transition, and one frame routes both to the same target.
    private enum FramePhase : byte
    {
        Empty,
        Position,
        WheelAndButtons,
        Key,
        Text
    }

    private readonly record struct PendingInput(
        PendingKind Kind,
        float X = 0,
        float Y = 0,
        int Value = 0,
        bool Down = false,
        bool Repeat = false,
        string? Text = null);
}
