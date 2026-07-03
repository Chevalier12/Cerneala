namespace Cerneala.UI.Media;

public readonly record struct OpacityLayer
{
    public OpacityLayer(float opacity)
    {
        if (!float.IsFinite(opacity) || opacity < 0 || opacity > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(opacity), "Opacity must be between 0 and 1.");
        }

        Opacity = opacity;
    }

    public float Opacity { get; }
}
