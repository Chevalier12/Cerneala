namespace Cerneala.Drawing.Text;

public readonly record struct TextCaretVerticalMetrics
{
    public TextCaretVerticalMetrics(float offsetY, float height)
    {
        if (!float.IsFinite(offsetY))
        {
            throw new ArgumentOutOfRangeException(nameof(offsetY), "Caret vertical offset must be finite.");
        }

        if (height <= 0 || !float.IsFinite(height))
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Caret height must be positive and finite.");
        }

        OffsetY = offsetY;
        Height = height;
    }

    public float OffsetY { get; }

    public float Height { get; }
}
