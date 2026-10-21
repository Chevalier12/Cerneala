using System.Text;
using Cerneala.UI.Core;

namespace Cerneala.UI.Documents;

public sealed class Run : Inline
{
    public static readonly UiProperty<string> TextProperty = UiProperty<string>.Register(
        nameof(Text),
        typeof(Run),
        new UiPropertyMetadata<string>(
            string.Empty,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender,
            coerceValue: (_, value) => value ?? string.Empty));

    public Run()
    {
    }

    public Run(string text)
    {
        Text = text;
    }

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value ?? string.Empty);
    }

    internal override void AppendText(StringBuilder builder)
    {
        builder.Append(Text);
    }

    internal override void CollectPieces(InlineStyle inherited, List<(string Text, InlineStyle Style)> pieces)
    {
        pieces.Add((Text, Resolve(inherited)));
    }
}
