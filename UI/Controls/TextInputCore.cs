using Cerneala.Drawing.Text;
using Cerneala.UI.Core;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Platform;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;
using Cerneala.UI.Text;
using System.Text;

namespace Cerneala.UI.Controls;

internal sealed class TextInputCore
{
    private readonly ITextInputHost host;
    private readonly TextInputPolicy policy;
    private readonly TextEditor editor;
    private readonly TextEditingController controller;
    private readonly TextInputViewport viewport;
    private bool isMouseSelecting;
    private int mouseSelectionAnchor;
    private TextSelection lastReportedSelection;

    public TextInputCore(ITextInputHost host, TextInputPolicy policy)
    {
        this.host = host ?? throw new ArgumentNullException(nameof(host));
        this.policy = policy;
        editor = new TextEditor(recordsHistory: policy.RecordsHistory);
        controller = new TextEditingController(editor);
        viewport = new TextInputViewport(host, editor);

        Control control = host.Control;
        control.Handlers.AddHandler(InputEvents.TextInputEvent, OnRoutedTextInput);
        control.Handlers.AddHandler(InputEvents.KeyDownEvent, OnRoutedKeyDown);
        control.Handlers.AddHandler(InputEvents.MouseDownEvent, OnRoutedMouseDown);
        control.Handlers.AddHandler(InputEvents.LostMouseCaptureEvent, (_, _) => CancelMouseSelection());
    }

    private Control Control => host.Control;

    public TextSelection Selection => editor.Selection;

    public TextCaret Caret => editor.Caret;

    internal int UndoHistoryCount => editor.UndoRedo.UndoCount;

    public TextMeasurer TextMeasurer
    {
        get => viewport.TextMeasurer;
        set => viewport.TextMeasurer = value;
    }

    public TextRenderer TextRenderer
    {
        get => viewport.TextRenderer;
        set => viewport.TextRenderer = value;
    }

    public ResourceId<FontResource>? FontResourceId
    {
        get => viewport.FontResourceId;
        set => viewport.FontResourceId = value;
    }

    public IResourceProvider? ResourceProvider
    {
        get => viewport.ResourceProvider;
        set => viewport.ResourceProvider = value;
    }

    public LayoutSize Measure(MeasureContext context) => viewport.Measure(context);

    public void Render(RenderContext context) => viewport.Render(context);

    public bool UpdateRenderTime(TimeSpan frameTime) => viewport.UpdateRenderTime(frameTime);

    public void SynchronizeTextFromHost(string text)
    {
        string next = text ?? string.Empty;
        if (editor.Document.Text == next)
        {
            return;
        }

        editor.SetText(next);
        editor.UndoRedo.Clear();
        viewport.EnsureCaretVisible();
        viewport.InvalidateTextMetrics("TextBox text changed");
    }

    public void ReceiveTextInput(string text)
    {
        string input = host.NormalizeInput(text);
        if (input.Length == 0 || !controller.InsertText(input))
        {
            return;
        }

        CommitEditorText("TextBox text input");
    }

    public void Select(int anchor, int active)
    {
        editor.Select(anchor, active);
        ReportSelectionChanged();
        viewport.EnsureCaretVisible();
        Control.Invalidate(InvalidationFlags.Render, "TextBox selection changed");
    }

    public void MoveCaret(int position, bool extendSelection = false)
    {
        editor.MoveCaret(position, extendSelection);
        ReportSelectionChanged();
        viewport.EnsureCaretVisible();
        viewport.ResetCaretBlink();
        Control.Invalidate(InvalidationFlags.Render, "TextBox caret changed");
    }

    public bool Undo()
    {
        if (!policy.RecordsHistory || !editor.Undo())
        {
            return false;
        }

        CommitEditorText("TextBox undo");
        return true;
    }

    public bool Redo()
    {
        if (!policy.RecordsHistory || !editor.Redo())
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
            viewport.ResetCaretBlink();
        }
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
        if (!Control.IsEnabled || args.ChangedButton != InputMouseButton.Left)
        {
            return false;
        }

        LayoutRect content = ContentControl.Deflate(Control.ArrangedBounds, host.Insets);
        int index = viewport.GetCaretIndexAtMouseX(args.X, content);
        isMouseSelecting = true;
        mouseSelectionAnchor = index;
        MoveCaret(index);
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

        LayoutRect content = ContentControl.Deflate(Control.ArrangedBounds, host.Insets);
        int index = viewport.GetCaretIndexAtMouseX(args.X, content);
        Select(mouseSelectionAnchor, index);
        viewport.ResetCaretBlink();
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

        LayoutRect content = ContentControl.Deflate(Control.ArrangedBounds, host.Insets);
        int index = viewport.GetCaretIndexAtMouseX(args.X, content);
        Select(mouseSelectionAnchor, index);
        viewport.ResetCaretBlink();
        isMouseSelecting = false;
        captureManager.Release(routeMap);
        args.Handled = true;
        return true;
    }

    public static string NormalizeSingleLineInput(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        StringBuilder? builder = null;
        for (int i = 0; i < text.Length; i++)
        {
            char character = text[i];
            if (char.IsControl(character))
            {
                builder ??= new StringBuilder(text.Length).Append(text, 0, i);
                continue;
            }

            builder?.Append(character);
        }

        return builder?.ToString() ?? text;
    }

    private void OnRoutedTextInput(UiElementId sender, RoutedEventArgs args)
    {
        if (args is not TextCompositionEventArgs textArgs || textArgs.Handled)
        {
            return;
        }

        ReceiveTextInput(textArgs.Text);
        textArgs.Handled = true;
    }

    private void OnRoutedKeyDown(UiElementId sender, RoutedEventArgs args)
    {
        if (args is not KeyEventArgs keyArgs || keyArgs.Handled)
        {
            return;
        }

        if (keyArgs.IsControlDown &&
            !keyArgs.IsAltDown &&
            HandleClipboardShortcut(keyArgs.Key))
        {
            keyArgs.Handled = true;
            return;
        }

        bool handled = HandleEditingKey(keyArgs.Key, keyArgs.IsShiftDown);
        keyArgs.Handled = handled;
    }

    private void OnRoutedMouseDown(UiElementId sender, RoutedEventArgs args)
    {
        if (args is MouseButtonEventArgs mouseArgs &&
            mouseArgs.ChangedButton == InputMouseButton.Left)
        {
            mouseArgs.Handled = true;
        }
    }

    private bool HandleEditingKey(InputKey key, bool extendSelection)
    {
        bool changed = controller.HandleKey(key, extendSelection);
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
            case InputKey.Home:
            case InputKey.End:
            case InputKey.Left:
            case InputKey.Right:
                ReportSelectionChanged();
                viewport.EnsureCaretVisible();
                viewport.ResetCaretBlink();
                Control.Invalidate(InvalidationFlags.Render, "TextBox caret changed");
                return true;
            default:
                return false;
        }
    }

    private bool HandleClipboardShortcut(InputKey key)
    {
        return key switch
        {
            InputKey.A => SelectAllText(),
            InputKey.C => policy.AllowsCopy ? CopySelection() : true,
            InputKey.X => policy.AllowsCut ? CutSelection() : true,
            InputKey.V => policy.AllowsPaste ? PasteClipboardText() : true,
            _ => false
        };
    }

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

        string input = host.NormalizeInput(clipboard.GetText() ?? string.Empty);
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
        viewport.EnsureCaretVisible();
        viewport.ResetCaretBlink();
        viewport.InvalidateTextMetrics(reason);
        ReportSelectionChanged();
    }

    private void CancelMouseSelection()
    {
        isMouseSelecting = false;
    }
}
