namespace Cerneala.UI.Text;

// A run of paragraph text with one style. Decorations draw over the whole span.
public readonly record struct TextSpan(string Text, TextAspect Aspect, TextDecorations Decorations = TextDecorations.None);
