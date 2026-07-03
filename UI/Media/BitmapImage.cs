using Cerneala.Drawing;
using Cerneala.UI.Layout;
using System.Runtime.CompilerServices;

namespace Cerneala.UI.Media;

public sealed record BitmapImage : ImageSource
{
    public BitmapImage(string identity, LayoutSize intrinsicSize, IDrawImage? image = null)
        : base(identity, intrinsicSize)
    {
        Image = image;
    }

    public IDrawImage? Image { get; }

    public override IDrawImage? ResolveDrawImage()
    {
        return Image;
    }

    public bool Equals(BitmapImage? other)
    {
        return ReferenceEquals(this, other) ||
            other is not null &&
            base.Equals(other) &&
            ReferenceEquals(Image, other.Image);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), Image is null ? 0 : RuntimeHelpers.GetHashCode(Image));
    }
}
