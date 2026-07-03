using Cerneala.Drawing;
using Cerneala.UI.Layout;
using System.Runtime.CompilerServices;

namespace Cerneala.UI.Media;

public sealed record RenderTargetImage : ImageSource
{
    public RenderTargetImage(string identity, LayoutSize intrinsicSize, IDrawImage image)
        : base(identity, intrinsicSize)
    {
        Image = image ?? throw new ArgumentNullException(nameof(image));
    }

    public IDrawImage Image { get; }

    public override IDrawImage ResolveDrawImage()
    {
        return Image;
    }

    public bool Equals(RenderTargetImage? other)
    {
        return ReferenceEquals(this, other) ||
            other is not null &&
            base.Equals(other) &&
            ReferenceEquals(Image, other.Image);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), RuntimeHelpers.GetHashCode(Image));
    }
}
