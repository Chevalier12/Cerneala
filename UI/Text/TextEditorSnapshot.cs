namespace Cerneala.UI.Text;

internal readonly record struct TextEditorSnapshot(string Text, TextCaret Caret, TextSelection Selection);
