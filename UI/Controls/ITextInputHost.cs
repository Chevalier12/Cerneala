using Cerneala.Drawing;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.UI.Controls;

internal interface ITextInputHost
{
    Control Control { get; }

    string TextValue { get; }

    string DisplayText { get; }

    Brush CaretBrush { get; }

    Color SelectionBackground { get; }

    Thickness Insets { get; }

    string NormalizeInput(string text);

    void ApplyEditorText(string text);

    void RaiseSelectionChanged();
}
