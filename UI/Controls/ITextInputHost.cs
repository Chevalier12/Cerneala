using Cerneala.Drawing;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Controls;

internal interface ITextInputHost
{
    Control Control { get; }

    string TextValue { get; }

    string DisplayText { get; }

    Color CaretColor { get; }

    Color SelectionBackground { get; }

    Thickness Insets { get; }

    string NormalizeInput(string text);

    void ApplyEditorText(string text);

    void RaiseSelectionChanged();
}
