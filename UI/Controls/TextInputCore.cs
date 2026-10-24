using Cerneala.Drawing.Text;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Platform;
using Cerneala.UI.Resources;
using Cerneala.UI.Text;
using System.Globalization;
using System.Text;

namespace Cerneala.UI.Controls;

internal sealed class TextInputCore
{
    private enum MouseSelectionUnit
    {
        Character,
        Word,
        Line
    }

    private readonly ITextInputHost host;
    private readonly TextInputPolicy policy;
    private readonly TextEditor editor;
    private readonly TextEditingController controller;
    private readonly TextInputView view;
    private ScrollViewer? contentHost;
    private bool isMouseSelecting;
    private MouseSelectionUnit mouseSelectionUnit;
    // The press of a mouse selection: a caret position, or the word or line a double or triple click selected.
    private int mouseSelectionAnchor;
    private int mouseSelectionAnchorEnd;
    private TextSelection lastReportedSelection;
    // The X the caret keeps while Up, Down, PageUp and PageDown move it through shorter lines, as in WPF; every other
    // caret move forgets it.
    private float? verticalMoveX;

    public TextInputCore(ITextInputHost host, TextInputPolicy policy)
    {
        this.host = host ?? throw new ArgumentNullException(nameof(host));
        this.policy = policy;
        editor = new TextEditor(recordsHistory: policy.RecordsHistory);
        controller = new TextEditingController(editor);
        view = new TextInputView(host, editor);

        Control control = host.Control;
        control.Handlers.AddHandler(InputEvents.TextInputEvent, OnRoutedTextInput);
        control.Handlers.AddHandler(InputEvents.KeyDownEvent, OnRoutedKeyDown);
        control.Handlers.AddHandler(InputEvents.MouseDownEvent, OnRoutedMouseDown);
        control.Handlers.AddHandler(InputEvents.MouseUpEvent, OnRoutedMouseUp);
        control.Handlers.AddHandler(InputEvents.LostMouseCaptureEvent, (_, _) => CancelMouseSelection());
    }

    private Control Control => host.Control;

    public TextInputView View => view;

    public TextSelection Selection => editor.Selection;

    public TextCaret Caret => editor.Caret;

    internal int UndoHistoryCount => editor.UndoRedo.UndoCount;

    public TextMeasurer TextMeasurer
    {
        get => view.TextMeasurer;
        set => view.TextMeasurer = value;
    }

    public TextRenderer TextRenderer
    {
        get => view.TextRenderer;
        set => view.TextRenderer = value;
    }

    public ResourceId<FontResource>? FontResourceId
    {
        get => view.FontResourceId;
        set => view.FontResourceId = value;
    }

    public IResourceProvider? ResourceProvider
    {
        get => view.ResourceProvider;
        set => view.ResourceProvider = value;
    }

    public bool UpdateRenderTime(TimeSpan frameTime) => view.UpdateRenderTime(frameTime);

    // Puts the text view into the PART_ContentHost ScrollViewer of the owner's template, or takes it out of the old one
    // when the template goes away. The ScrollViewer only scrolls the text: it does not take focus, and its scroll bars
    // show the arrow cursor, as WPF's ScrollBar style does, so the owner's IBeam shows only over the text.
    public void AttachContentHost(ScrollViewer? nextContentHost)
    {
        if (!ReferenceEquals(contentHost, nextContentHost))
        {
            if (contentHost is not null)
            {
                contentHost.Content = null;
            }

            view.DetachFromPresenter();
        }

        contentHost = nextContentHost;
        if (nextContentHost is null)
        {
            return;
        }

        nextContentHost.Focusable = false;
        nextContentHost.IsTabStop = false;
        nextContentHost.HorizontalScrollBar.Cursor = Cursor.Arrow;
        nextContentHost.VerticalScrollBar.Cursor = Cursor.Arrow;
        nextContentHost.Content = view;
    }

    public void SynchronizeTextFromHost(string text)
    {
        string next = text ?? string.Empty;
        if (editor.Document.Text == next)
        {
            return;
        }

        editor.SetText(next);
        editor.UndoRedo.Clear();
        ForgetCaretPlacement();
        view.InvalidateTextMetrics("TextBox text changed");
        view.RevealCaret();
    }

    // Typed text: consecutive typing with no other edit or caret move in between is one undo unit, as in WPF.
    public void ReceiveTextInput(string text)
    {
        if (!host.AllowsEditing)
        {
            return;
        }

        string input = FilterInput(text);
        if (input.Length == 0)
        {
            return;
        }

        string before = editor.Document.Text;
        TextSelection selection = editor.Selection;
        editor.InsertTypedText(input);
        if (before == editor.Document.Text && selection == editor.Selection)
        {
            return;
        }

        CommitEditorText("TextBox text input");
    }

    public string SelectedText => editor.Document.Text.Substring(Selection.Start, Selection.Length);

    // Replaces the selection with text and selects the new text, as WPF's TextRange.Text, in one undo unit that later
    // typing does not join; undo restores the old text and the old selection. As a programmatic change, it ignores
    // IsReadOnly, MaxLength and CharacterCasing.
    public void ReplaceSelectedText(string text)
    {
        string value = text ?? string.Empty;
        if (value == SelectedText)
        {
            return;
        }

        int start = Selection.Start;
        editor.ReplaceSelection(value);
        editor.Select(start, start + value.Length);
        CommitEditorText("TextBox selected text replaced");
    }

    public bool CanUndo => editor.UndoRedo.CanUndo;

    public bool CanRedo => editor.UndoRedo.CanRedo;

    public bool IsUndoEnabled
    {
        get => editor.UndoRedo.IsEnabled;
        set => editor.UndoRedo.IsEnabled = value;
    }

    public int UndoLimit
    {
        get => editor.UndoRedo.Limit;
        set => editor.UndoRedo.Limit = value;
    }

    public bool Copy() => CanCopy && CopySelection();

    public bool Cut() => CanCut && CutSelection();

    public bool Paste() => CanPaste && PasteClipboardText();

    public void Select(int anchor, int active)
    {
        editor.Select(anchor, active);
        ForgetCaretPlacement();
        ReportSelectionChanged();
        view.RevealCaret();
        view.InvalidateTextRender("TextBox selection changed");
    }

    public void MoveCaret(int position, bool extendSelection = false)
    {
        editor.MoveCaret(position, extendSelection);
        ForgetCaretPlacement();
        ReportSelectionChanged();
        view.RevealCaret();
        view.ResetCaretBlink();
        view.InvalidateTextRender("TextBox caret changed");
    }

    public bool Undo()
    {
        if (!host.AllowsEditing || !policy.RecordsHistory || !editor.Undo())
        {
            return false;
        }

        CommitEditorText("TextBox undo");
        return true;
    }

    public bool Redo()
    {
        if (!host.AllowsEditing || !policy.RecordsHistory || !editor.Redo())
        {
            return false;
        }

        CommitEditorText("TextBox redo");
        return true;
    }

    public void OnKeyboardFocusChanged(bool isFocused)
    {
        if (isFocused)
        {
            view.ResetCaretBlink();
        }

        // The caret shows only while the owner has the keyboard focus.
        view.InvalidateTextRender("TextBox keyboard focus changed");
    }

    public void ReportSelectionChanged()
    {
        if (lastReportedSelection == Selection)
        {
            return;
        }

        lastReportedSelection = Selection;
        host.RaiseSelectionChanged();
    }

    public bool BeginPointerDrag(
        PointerCaptureManager captureManager,
        ElementInputRouteMap routeMap,
        MouseButtonEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(captureManager);
        ArgumentNullException.ThrowIfNull(routeMap);
        ArgumentNullException.ThrowIfNull(args);
        if (!Control.IsEnabled ||
            args.ChangedButton != InputMouseButton.Left ||
            !IsTextPress(args.OriginalSource, routeMap))
        {
            return false;
        }

        // As WPF's text editor: the second press of a double click selects the word under the pointer and the third the
        // text line, and the drag that follows extends the selection by whole words or whole lines.
        isMouseSelecting = true;
        mouseSelectionUnit = args.ClickCount switch
        {
            2 => MouseSelectionUnit.Word,
            >= 3 => MouseSelectionUnit.Line,
            _ => MouseSelectionUnit.Character
        };
        if (mouseSelectionUnit == MouseSelectionUnit.Character)
        {
            int index = host.GetTextPosition(GetDisplayIndexAtPointer(args, out bool atLineEnd));
            mouseSelectionAnchor = index;
            mouseSelectionAnchorEnd = index;
            MoveCaret(index);
            view.CaretAtLineEnd = atLineEnd;
        }
        else
        {
            (mouseSelectionAnchor, mouseSelectionAnchorEnd) = GetMouseSelectionUnitRange(args);
            Select(mouseSelectionAnchor, mouseSelectionAnchorEnd);
            view.ResetCaretBlink();
        }

        captureManager.Capture(Control, routeMap);
        args.Handled = true;
        return true;
    }

    public bool UpdatePointerDrag(MouseEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (!isMouseSelecting)
        {
            return false;
        }

        ExtendMouseSelection(args);
        args.Handled = true;
        return true;
    }

    public bool CompletePointerDrag(
        PointerCaptureManager captureManager,
        ElementInputRouteMap routeMap,
        MouseButtonEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(captureManager);
        ArgumentNullException.ThrowIfNull(routeMap);
        ArgumentNullException.ThrowIfNull(args);
        if (!isMouseSelecting || args.ChangedButton != InputMouseButton.Left)
        {
            return false;
        }

        ExtendMouseSelection(args);
        isMouseSelecting = false;
        captureManager.Release(routeMap);
        args.Handled = true;
        return true;
    }

    public static string NormalizeSingleLineInput(string? text) =>
        NormalizeInput(text, keepsLineBreaks: false, keepsTabs: false);

    // As NormalizeSingleLineInput, but "\r\n", "\r" and "\n" stay, for a TextBox that accepts returns.
    public static string NormalizeMultiLineInput(string? text) =>
        NormalizeInput(text, keepsLineBreaks: true, keepsTabs: false);

    // Removes the control characters from input, except line breaks when keepsLineBreaks and '\t' when keepsTabs.
    public static string NormalizeInput(string? text, bool keepsLineBreaks, bool keepsTabs)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        StringBuilder? builder = null;
        for (int i = 0; i < text.Length; i++)
        {
            char character = text[i];
            if (char.IsControl(character) && !KeepsControlCharacter(character, keepsLineBreaks, keepsTabs))
            {
                builder ??= new StringBuilder(text.Length).Append(text, 0, i);
                continue;
            }

            builder?.Append(character);
        }

        return builder?.ToString() ?? text;
    }

    // The control characters that input keeps.
    private static bool KeepsControlCharacter(char character, bool keepsLineBreaks, bool keepsTabs)
    {
        return (keepsLineBreaks && character is '\r' or '\n') || (keepsTabs && character == '\t');
    }

    // The root point goes back through the render transforms of the presenter and every ancestor, so the caret lands at
    // the character drawn under the pointer. A point that cannot be converted keeps the caret.
    private int GetDisplayIndexAtPointer(MouseEventArgs args, out bool atLineEnd)
    {
        return view.GetDisplayIndexAtRootPoint(
            args.RootX,
            args.RootY,
            host.GetDisplayIndex(Caret.Position),
            out atLineEnd);
    }

    // A drag selects from the press to the pointer. After a double or triple click it selects whole words or whole text
    // lines: from the start of the pressed unit to the end of the unit under the pointer, or, when the pointer is before
    // the pressed unit, from the end of the pressed unit to the start of the unit under the pointer.
    private void ExtendMouseSelection(MouseEventArgs args)
    {
        if (mouseSelectionUnit == MouseSelectionUnit.Character)
        {
            int displayIndex = GetDisplayIndexAtPointer(args, out bool atLineEnd);
            Select(mouseSelectionAnchor, host.GetTextPosition(displayIndex));
            view.CaretAtLineEnd = atLineEnd;
        }
        else
        {
            (int start, int end) = GetMouseSelectionUnitRange(args);
            if (start >= mouseSelectionAnchor)
            {
                Select(mouseSelectionAnchor, Math.Max(end, mouseSelectionAnchorEnd));
            }
            else
            {
                Select(mouseSelectionAnchorEnd, start);
            }
        }

        view.ResetCaretBlink();
    }

    // The word (with the whitespace after it) or the text line (without its line break) of the character under the
    // pointer, as text positions. A PasswordBox finds them in its mask, so a double click selects the whole password and
    // shows nothing of the words in it.
    private (int Start, int End) GetMouseSelectionUnitRange(MouseEventArgs args)
    {
        int displayIndex = view.GetCharacterIndexAtRootPoint(
            args.RootX,
            args.RootY,
            host.GetDisplayIndex(Caret.Position));
        (int start, int end) = mouseSelectionUnit == MouseSelectionUnit.Line
            ? TextWordBoundaries.GetLineRange(host.DisplayText, displayIndex)
            : TextWordBoundaries.GetWordRange(host.DisplayText, displayIndex);
        return (host.GetTextPosition(start), host.GetTextPosition(end));
    }

    // As WPF's TextBox: the input loses its other control characters (NormalizeInput), Upper and Lower change its case
    // (WPF uses the culture of the current input language; Cerneala has none, so the invariant culture), and MaxLength
    // cuts it to the room left once the selection it replaces is gone, without splitting a text element.
    private string FilterInput(string text)
    {
        string input = host.NormalizeInput(text);
        input = host.CharacterCasing switch
        {
            CharacterCasing.Upper => input.ToUpperInvariant(),
            CharacterCasing.Lower => input.ToLowerInvariant(),
            _ => input
        };

        int maxLength = host.MaxLength;
        if (maxLength <= 0 || input.Length == 0)
        {
            return input;
        }

        int room = maxLength - (editor.Document.Length - Selection.Length);
        if (room <= 0)
        {
            return string.Empty;
        }

        if (input.Length <= room)
        {
            return input;
        }

        // The input is longer than room, so the kept part ends at the last text element start at or before room.
        int length = 0;
        foreach (int start in StringInfo.ParseCombiningCharacters(input))
        {
            if (start > room)
            {
                break;
            }

            length = start;
        }

        return input[..length];
    }

    // A press on the scroll bars of the content host scrolls; it does not place the caret or select.
    private bool IsTextPress(object? originalSource, ElementInputRouteMap? routeMap)
    {
        UIElement? source = originalSource as UIElement;
        for (UIElement? current = source;
            current is not null && !ReferenceEquals(current, Control);
            current = current.VisualParent)
        {
            if (current is ScrollBar)
            {
                return false;
            }
        }

        return true;
    }

    private void OnRoutedTextInput(object sender, RoutedEventArgs args)
    {
        if (args is not TextCompositionEventArgs textArgs || textArgs.Handled)
        {
            return;
        }

        ReceiveTextInput(textArgs.Text);
        textArgs.Handled = true;
    }

    private void OnRoutedKeyDown(object sender, RoutedEventArgs args)
    {
        if (args is not KeyEventArgs keyArgs || keyArgs.Handled)
        {
            return;
        }

        if (keyArgs.IsControlDown &&
            !keyArgs.IsAltDown &&
            HandleControlShortcut(keyArgs.Key, keyArgs.IsShiftDown))
        {
            keyArgs.Handled = true;
            return;
        }

        if (keyArgs.Key == InputKey.Enter)
        {
            keyArgs.Handled = HandleEnter(keyArgs);
            return;
        }

        if (keyArgs.Key == InputKey.Tab)
        {
            keyArgs.Handled = HandleTab(keyArgs);
            return;
        }

        if (HandleLineNavigationKey(keyArgs))
        {
            keyArgs.Handled = true;
            return;
        }

        bool handled = HandleEditingKey(keyArgs.Key, keyArgs.IsShiftDown, byWord: keyArgs.IsControlDown);
        keyArgs.Handled = handled;
    }

    private void OnRoutedMouseDown(object sender, RoutedEventArgs args)
    {
        if (args is MouseButtonEventArgs mouseArgs &&
            mouseArgs.ChangedButton == InputMouseButton.Left &&
            IsTextPress(mouseArgs.OriginalSource, mouseArgs.RouteMap))
        {
            mouseArgs.Handled = true;
        }
    }

    // The release that ends the mouse selection begun by a press on this control is the control's, as WPF's text
    // editor handles MouseLeftButtonUp while it holds the capture: an enclosing Button does not click or run its
    // command (#586). The input bridge completes the selection after the MouseUp, so it is still active here.
    private void OnRoutedMouseUp(object sender, RoutedEventArgs args)
    {
        if (isMouseSelecting &&
            args is MouseButtonEventArgs mouseArgs &&
            mouseArgs.ChangedButton == InputMouseButton.Left)
        {
            mouseArgs.Handled = true;
        }
    }

    // As WPF's TextBox: with AcceptsReturn, Enter without Ctrl or Alt types a line break; otherwise Enter stays
    // unhandled, so a default button or an enclosing handler gets it.
    private bool HandleEnter(KeyEventArgs keyArgs)
    {
        if (keyArgs.IsControlDown || keyArgs.IsAltDown || !host.AcceptsReturn || !host.AllowsEditing)
        {
            return false;
        }

        ReceiveTextInput(Environment.NewLine);
        return true;
    }

    // As WPF's TextBox: with AcceptsTab, Tab and Shift+Tab without Ctrl or Alt type a tab, so the focus stays; Ctrl+Tab
    // and a TextBox without AcceptsTab leave Tab unhandled, so it moves the focus.
    private bool HandleTab(KeyEventArgs keyArgs)
    {
        if (keyArgs.IsControlDown || keyArgs.IsAltDown || !host.AcceptsTab || !host.AllowsEditing)
        {
            return false;
        }

        ReceiveTextInput("\t");
        return true;
    }

    // Home and End go to the start and end of the drawn line, and with Ctrl to the start and end of the text. Up, Down,
    // PageUp and PageDown move between drawn lines; a single line leaves them unhandled, so an enclosing control, such as
    // a ComboBox, still gets them.
    private bool HandleLineNavigationKey(KeyEventArgs keyArgs)
    {
        InputKey key = keyArgs.Key;
        bool extendSelection = keyArgs.IsShiftDown;
        int caretIndex = host.GetDisplayIndex(Caret.Position);
        switch (key)
        {
            case InputKey.Home:
                if (keyArgs.IsControlDown)
                {
                    MoveCaretToDisplayIndex(0, extendSelection, atLineEnd: false);
                    return true;
                }

                MoveCaretToDisplayIndex(
                    view.GetLineStart(view.GetLineIndex(caretIndex, view.CaretAtLineEnd)),
                    extendSelection,
                    atLineEnd: false);
                return true;
            case InputKey.End:
                if (keyArgs.IsControlDown)
                {
                    MoveCaretToDisplayIndex(host.DisplayText.Length, extendSelection, atLineEnd: false);
                    return true;
                }

                int lineEnd = view.GetLineEnd(view.GetLineIndex(caretIndex, view.CaretAtLineEnd), out bool atLineEnd);
                MoveCaretToDisplayIndex(lineEnd, extendSelection, atLineEnd);
                return true;
            case InputKey.Up:
            case InputKey.Down:
            case InputKey.PageUp:
            case InputKey.PageDown:
                return MoveCaretVertically(key, keyArgs, caretIndex);
            default:
                return false;
        }
    }

    private bool MoveCaretVertically(InputKey key, KeyEventArgs keyArgs, int caretIndex)
    {
        int lineCount = view.LineCount;
        if (keyArgs.IsControlDown || keyArgs.IsAltDown || lineCount <= 1)
        {
            return false;
        }

        bool extendSelection = keyArgs.IsShiftDown;
        int line = view.GetLineIndex(caretIndex, view.CaretAtLineEnd);
        int distance = key is InputKey.PageUp or InputKey.PageDown ? view.GetLinesPerPage() : 1;
        bool down = key is InputKey.Down or InputKey.PageDown;
        if (!down && line == 0)
        {
            MoveCaretToDisplayIndex(0, extendSelection, atLineEnd: false);
            return true;
        }

        if (down && line == lineCount - 1)
        {
            MoveCaretToDisplayIndex(host.DisplayText.Length, extendSelection, atLineEnd: false);
            return true;
        }

        int targetLine = Math.Clamp(down ? line + distance : line - distance, 0, lineCount - 1);
        float x = verticalMoveX ?? view.GetCaretX(caretIndex, view.CaretAtLineEnd);
        int targetIndex = view.GetDisplayIndexAtLineX(targetLine, x, out bool atLineEnd);
        MoveCaretToDisplayIndex(targetIndex, extendSelection, atLineEnd);
        verticalMoveX = x;
        return true;
    }

    private void MoveCaretToDisplayIndex(int displayIndex, bool extendSelection, bool atLineEnd)
    {
        editor.MoveCaret(host.GetTextPosition(displayIndex), extendSelection);
        ForgetCaretPlacement();
        view.CaretAtLineEnd = atLineEnd;
        ReportSelectionChanged();
        view.RevealCaret();
        view.ResetCaretBlink();
        view.InvalidateTextRender("TextBox caret changed");
    }

    // Ctrl+Left and Ctrl+Right find the word over the drawn text, as the double-click word selection does. A TextBox draws
    // its own text, so the caret moves by its words. A PasswordBox draws one mask character per text element, which is a
    // single word, so the caret goes to the start or the end of the password, as in WPF, and never shows where its words are.
    private bool MoveCaretByWord(bool backward, bool extendSelection)
    {
        TextSelection beforeSelection = Selection;
        TextCaret beforeCaret = Caret;
        editor.MoveCaret(GetWordTargetPosition(backward), extendSelection);
        return beforeSelection != Selection || beforeCaret != Caret;
    }

    // The text position Ctrl+Left (backward) or Ctrl+Right moves the caret to, and where Ctrl+Backspace or Ctrl+Delete
    // deletes up to.
    private int GetWordTargetPosition(bool backward)
    {
        int displayIndex = host.GetDisplayIndex(Caret.Position);
        int target = backward
            ? TextWordBoundaries.GetPreviousWordStart(host.DisplayText, displayIndex)
            : TextWordBoundaries.GetNextWordStart(host.DisplayText, displayIndex);
        return host.GetTextPosition(target);
    }

    // Ctrl+Backspace and Ctrl+Delete delete up to the place Ctrl+Left and Ctrl+Right move to; a selection is deleted alone.
    private bool DeleteByWord(bool backward)
    {
        string before = editor.Document.Text;
        TextSelection beforeSelection = Selection;
        TextCaret beforeCaret = Caret;
        editor.DeleteTo(GetWordTargetPosition(backward));
        return before != editor.Document.Text || beforeSelection != Selection || beforeCaret != Caret;
    }

    // Left and Right move by text element, and with Ctrl by word, as WPF's Ctrl+Left and Ctrl+Right. Backspace and Delete
    // delete a text element, and with Ctrl a word, as WPF's Ctrl+Backspace and Ctrl+Delete.
    private bool HandleEditingKey(InputKey key, bool extendSelection, bool byWord)
    {
        if (!host.AllowsEditing && key is InputKey.Back or InputKey.Delete)
        {
            return true;
        }

        bool changed = byWord && key is InputKey.Left or InputKey.Right
            ? MoveCaretByWord(backward: key == InputKey.Left, extendSelection)
            : byWord && key is InputKey.Back or InputKey.Delete
                ? DeleteByWord(backward: key == InputKey.Back)
                : controller.HandleKey(key, extendSelection);
        switch (key)
        {
            case InputKey.Back:
            case InputKey.Delete:
                if (!changed)
                {
                    return false;
                }

                CommitEditorText(key == InputKey.Back ? "TextBox backspace" : "TextBox delete");
                return true;
            case InputKey.Left:
            case InputKey.Right:
                ForgetCaretPlacement();
                ReportSelectionChanged();
                view.RevealCaret();
                view.ResetCaretBlink();
                view.InvalidateTextRender("TextBox caret changed");
                return true;
            default:
                return false;
        }
    }

    // A blocked copy, cut or paste still handles its shortcut, so a PasswordBox's Ctrl+C reaches no enclosing handler.
    // Ctrl+Z undoes, and Ctrl+Y and Ctrl+Shift+Z redo, as WPF's Undo and Redo key gestures; with nothing to undo or redo
    // the key stays unhandled.
    private bool HandleControlShortcut(InputKey key, bool isShiftDown)
    {
        return key switch
        {
            InputKey.A => SelectAllText(),
            InputKey.C => !CanCopy || CopySelection(),
            InputKey.X => !CanCut || CutSelection(),
            InputKey.V => !CanPaste || PasteClipboardText(),
            InputKey.Z => isShiftDown ? Redo() : Undo(),
            InputKey.Y => !isShiftDown && Redo(),
            _ => false
        };
    }

    private bool CanCopy => policy.AllowsCopy;

    private bool CanCut => host.AllowsEditing && policy.AllowsCut;

    private bool CanPaste => host.AllowsEditing && policy.AllowsPaste;

    private bool SelectAllText()
    {
        Select(0, editor.Document.Length);
        return true;
    }

    private bool CopySelection()
    {
        IClipboard? clipboard = ResolveClipboard();
        if (clipboard is null || Selection.IsEmpty)
        {
            return false;
        }

        clipboard.SetText(editor.Document.Text.Substring(Selection.Start, Selection.Length));
        return true;
    }

    private bool CutSelection()
    {
        IClipboard? clipboard = ResolveClipboard();
        if (clipboard is null || Selection.IsEmpty)
        {
            return false;
        }

        clipboard.SetText(editor.Document.Text.Substring(Selection.Start, Selection.Length));
        editor.ReplaceSelection(string.Empty);
        CommitEditorText("TextBox cut");
        return true;
    }

    private bool PasteClipboardText()
    {
        IClipboard? clipboard = ResolveClipboard();
        if (clipboard?.HasText != true)
        {
            return false;
        }

        string input = FilterInput(clipboard.GetText() ?? string.Empty);
        if (input.Length == 0 || !controller.InsertText(input))
        {
            return false;
        }

        CommitEditorText("TextBox paste");
        return true;
    }

    private IClipboard? ResolveClipboard()
    {
        return Control.Root?.PlatformServices.Clipboard ??
            Control.Root?.PlatformServices.TextInput?.Clipboard;
    }

    private void CommitEditorText(string reason)
    {
        host.ApplyEditorText(editor.Document.Text);
        ForgetCaretPlacement();
        view.InvalidateTextMetrics(reason);
        view.RevealCaret();
        view.ResetCaretBlink();
        ReportSelectionChanged();
    }

    // A caret move that is not Up, Down, PageUp or PageDown forgets the X those keys keep and shows a caret at the end of
    // a wrapped line at the start of the next line.
    private void ForgetCaretPlacement()
    {
        verticalMoveX = null;
        view.CaretAtLineEnd = false;
    }

    private void CancelMouseSelection()
    {
        isMouseSelecting = false;
    }
}
