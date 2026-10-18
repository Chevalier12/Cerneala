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
// kinds does not need one frame per alternation and never falls further behind. A move
// never joins after a wheel notch, which is raised at the frame's final position.
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
    private bool hasWheel;
    private InputKey frameKey = InputKey.None;
    private InputKey lastKeyDown = InputKey.None;
    private InputKey textKey = InputKey.None;
    private bool hasPointerPosition;

    public SdlInputSource(Action? frameRequested = null)
    {
        this.frameRequested = frameRequested;
    }

    public float CoordinateScale { get; set; } = 1;

    // True while input is waiting to reach an app: deferred to a later frame, or already
    // moved into the frame under construction that no GetFrame has returned yet. GetFrame
    // applies the deferred input to the next frame as soon as it fits, so the deferred list
    // alone is empty while that frame still holds input.
    internal bool HasUndeliveredInput => deferred.Count > 0 || framePhase != FramePhase.Empty;

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
        hasWheel = false;
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

    // The window stops receiving key events (disabled, or the keyboard focus moved), so
    // the release of a key held now goes elsewhere. The held keys are dropped without a
    // release, like a window that gets no key-up message, so their next press is a press.
    public void ForgetKeys() => Accept(new PendingInput(PendingKind.ForgetKeys));

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

        if (input.Kind == PendingKind.ForgetKeys)
        {
            // A key transition or repeat in this frame happened before the window lost the
            // keys; it is published first. Without one, previousKeyboard holds exactly the
            // down keys, so dropping both leaves no release edge.
            if (frameKey != InputKey.None)
            {
                return false;
            }

            downKeys.Clear();
            previousKeyboard = KeyboardSnapshot.Empty;
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
                hasWheel = true;
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
    // holds no key or button transition. A move never joins after a wheel notch: the frame
    // raises the wheel at its final position, so the move would raise the notch elsewhere.
    // Example: Ctrl auto-repeats and wheel notches that alternate at one position share one
    // frame; a move after a button press or a wheel notch waits for the next frame.
    private bool CanJoinOutOfOrder(in PendingInput input, bool repeat)
    {
        if (hasTransition)
        {
            return false;
        }

        return input.Kind switch
        {
            PendingKind.Move => !hasWheel,
            PendingKind.Wheel => true,
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
        PendingKind.ForgetKeys => downKeys.Count > 0,
        _ => true
    };

    internal static InputKey MapScancode(int scancode)
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

    // The inverse of MapScancode: the scancode SDL reports for the key, or -1 when no
    // scancode that MapScancode maps back to this key exists.
    internal static int MapInputKey(InputKey key)
    {
        if (key is >= InputKey.A and <= InputKey.Z)
        {
            return 4 + (key - InputKey.A);
        }

        if (key is >= InputKey.D1 and <= InputKey.D9)
        {
            return 30 + (key - InputKey.D1);
        }

        if (key is >= InputKey.F1 and <= InputKey.F12)
        {
            return 58 + (key - InputKey.F1);
        }

        return key switch
        {
            InputKey.D0 => 39,
            InputKey.Enter => 40,
            InputKey.Escape => 41,
            InputKey.Back => 42,
            InputKey.Tab => 43,
            InputKey.Space => 44,
            InputKey.Insert => 73,
            InputKey.Home => 74,
            InputKey.PageUp => 75,
            InputKey.Delete => 76,
            InputKey.End => 77,
            InputKey.PageDown => 78,
            InputKey.Right => 79,
            InputKey.Left => 80,
            InputKey.Down => 81,
            InputKey.Up => 82,
            InputKey.LeftCtrl => 224,
            InputKey.LeftShift => 225,
            InputKey.LeftAlt => 226,
            InputKey.RightCtrl => 228,
            InputKey.RightShift => 229,
            InputKey.RightAlt => 230,
            _ => -1
        };
    }

    private enum PendingKind : byte
    {
        Move,
        Leave,
        Button,
        Wheel,
        Key,
        Text,
        ForgetKeys
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
