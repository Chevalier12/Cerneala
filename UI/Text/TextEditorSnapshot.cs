namespace Cerneala.UI.Text;

public readonly record struct TextEditorSnapshot(string Text, TextCaret Caret, TextSelection Selection);
