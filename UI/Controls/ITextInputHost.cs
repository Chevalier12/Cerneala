using Cerneala.Drawing;
using Cerneala.UI.Media;
using Cerneala.UI.Text;

namespace Cerneala.UI.Controls;

internal interface ITextInputHost
{
    Control Control { get; }

    string TextValue { get; }

    string DisplayText { get; }

    int GetDisplayIndex(int textPosition);

    int GetTextPosition(int displayIndex);

    Brush CaretBrush { get; }

    Color SelectionBackground { get; }

    bool AllowsEditing { get; }

    bool AcceptsReturn { get; }

    bool AcceptsTab { get; }

    // The most characters typed or pasted text can bring the text to; 0 is no limit.
    int MaxLength { get; }

    CharacterCasing CharacterCasing { get; }

    TextWrapping TextWrapping { get; }

    bool CentersSingleLineContentVertically { get; }

    string NormalizeInput(string text);

    void ApplyEditorText(string text);

    void RaiseSelectionChanged();
}
