using Cerneala.Drawing;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Media;

public abstract record ImageSource
{
    protected ImageSource(string identity, LayoutSize intrinsicSize)
    {
        if (string.IsNullOrWhiteSpace(identity))
        {
            throw new ArgumentException("Image source identity cannot be empty.", nameof(identity));
        }

        if (!IsValidIntrinsicSize(intrinsicSize))
        {
            throw new ArgumentOutOfRangeException(nameof(intrinsicSize), "Intrinsic image size must be finite and non-negative.");
        }

        Identity = identity;
        IntrinsicSize = intrinsicSize;
    }

    public string Identity { get; }

    public LayoutSize IntrinsicSize { get; }

    public abstract IDrawImage? ResolveDrawImage();

    private static bool IsValidIntrinsicSize(LayoutSize size)
    {
        return float.IsFinite(size.Width) &&
            float.IsFinite(size.Height) &&
            size.Width >= 0 &&
            size.Height >= 0;
    }
}
