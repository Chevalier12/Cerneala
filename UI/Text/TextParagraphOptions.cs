namespace Cerneala.UI.Text;

// The paragraph-level layout options of a TextBlock. The wrapping, trimming and scale of the paragraph come from the
// paragraph TextAspect that is passed to the measurer and the renderer next to these options.
public sealed record TextParagraphOptions
{
    public TextParagraphOptions(
        TextAlignment Alignment = TextAlignment.Left,
        int MaxLines = 0,
        float LineHeight = float.NaN,
        LineStackingStrategy LineStackingStrategy = LineStackingStrategy.MaxHeight)
    {
        if (!Enum.IsDefined(Alignment))
        {
            throw new ArgumentOutOfRangeException(nameof(Alignment));
        }

        if (MaxLines < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxLines), "MaxLines cannot be negative.");
        }

        if (!float.IsNaN(LineHeight) && (!float.IsFinite(LineHeight) || LineHeight <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(LineHeight), "LineHeight must be NaN (automatic) or positive and finite.");
        }

        if (!Enum.IsDefined(LineStackingStrategy))
        {
            throw new ArgumentOutOfRangeException(nameof(LineStackingStrategy));
        }

        this.Alignment = Alignment;
        this.MaxLines = MaxLines;
        this.LineHeight = LineHeight;
        this.LineStackingStrategy = LineStackingStrategy;
    }

    public TextAlignment Alignment { get; }

    // 0 means no limit.
    public int MaxLines { get; }

    // NaN means automatic: the height of the content of the line.
    public float LineHeight { get; }

    public LineStackingStrategy LineStackingStrategy { get; }
}
