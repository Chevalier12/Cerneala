using System.Globalization;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Automation;

public sealed class RetainedAutomationInputDriver : IAutomationInputDriver
{
    private readonly UiHost host;
    private PointerSnapshot pointer = PointerSnapshot.Empty;
    private KeyboardSnapshot keyboard = KeyboardSnapshot.Empty;

    public RetainedAutomationInputDriver(UiHost host)
    {
        this.host = host ?? throw new ArgumentNullException(nameof(host));
    }

    public void Click(UIElement target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (!ReferenceEquals(target.Root, host.Root))
        {
            throw new InvalidOperationException("The automation target does not belong to this driver's UI root.");
        }

        LayoutRect bounds = target.ArrangedBounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new InvalidOperationException("The automation target has no arranged hit-test area.");
        }

        MovePointer(bounds.X + (bounds.Width / 2), bounds.Y + (bounds.Height / 2));
        SetPointerButton(InputMouseButton.Left, true);
        SetPointerButton(InputMouseButton.Left, false);
    }

    public void PressKey(InputKey key, AutomationModifiers modifiers = AutomationModifiers.None)
    {
        if (key is InputKey.None or InputKey.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(key));
        }

        InputKey[] modifierKeys = ModifierKeys(modifiers);
        SetKeyboard([.. modifierKeys]);
        SetKeyboard([.. modifierKeys, key]);
        SetKeyboard([.. modifierKeys]);
        SetKeyboard([]);
    }

    public void SendText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            Dispatch([new TextInputSnapshotEvent(enumerator.GetTextElement())]);
        }
    }

    private void MovePointer(float x, float y)
    {
        PointerSnapshot next = pointer.WithPosition(x, y);
        Dispatch(next, keyboard, []);
        pointer = next;
    }

    private void SetPointerButton(InputMouseButton button, bool isDown)
    {
        PointerSnapshot next = pointer.WithButton(button, isDown);
        Dispatch(next, keyboard, []);
        pointer = next;
    }

    private void SetKeyboard(IReadOnlyCollection<InputKey> downKeys)
    {
        KeyboardSnapshot next = KeyboardSnapshot.FromDownKeys(downKeys);
        Dispatch(pointer, next, []);
        keyboard = next;
    }

    private void Dispatch(IReadOnlyList<TextInputSnapshotEvent> textInputEvents)
    {
        Dispatch(pointer, keyboard, textInputEvents);
    }

    private void Dispatch(
        PointerSnapshot nextPointer,
        KeyboardSnapshot nextKeyboard,
        IReadOnlyList<TextInputSnapshotEvent> textInputEvents)
    {
        InputFrame frame = new(pointer, nextPointer, keyboard, nextKeyboard, textInputEvents);
        host.Update(frame, host.Viewport, TimeSpan.Zero);
    }

    private static InputKey[] ModifierKeys(AutomationModifiers modifiers)
    {
        List<InputKey> keys = [];
        if ((modifiers & AutomationModifiers.Shift) != 0)
        {
            keys.Add(InputKey.LeftShift);
        }

        if ((modifiers & AutomationModifiers.Control) != 0)
        {
            keys.Add(InputKey.LeftCtrl);
        }

        if ((modifiers & AutomationModifiers.Alt) != 0)
        {
            keys.Add(InputKey.LeftAlt);
        }

        return keys.ToArray();
    }
}
