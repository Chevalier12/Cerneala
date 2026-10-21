using Cerneala.Drawing;
using Cerneala.UI.Media;
using Cerneala.UI.Text;

namespace Cerneala.UI.Documents;

// The effective text style of one inline: the style of the TextBlock with the properties of the inlines down to this one
// applied on top.
internal readonly record struct InlineStyle(
    string FontFamily,
    float FontSize,
    FontWeight FontWeight,
    FontStyle FontStyle,
    FontStretch FontStretch,
    Brush? Foreground,
    TextDecorations Decorations);
