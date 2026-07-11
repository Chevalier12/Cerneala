using Cerneala.Drawing;
using System.Runtime.CompilerServices;

namespace Cerneala.UI.Media;

public sealed record ImageBrush : TileBrush
{
    public ImageBrush(
        IDrawImage? image,
        DrawBrushStretch stretch = DrawBrushStretch.Fill,
        DrawBrushAlignmentX alignmentX = DrawBrushAlignmentX.Center,
        DrawBrushAlignmentY alignmentY = DrawBrushAlignmentY.Center,
        DrawRect? viewport = null,
        DrawRect? viewbox = null,
        DrawTileMode tileMode = DrawTileMode.None,
        float opacity = 1)
        : base(stretch, alignmentX, alignmentY, viewport, viewbox, tileMode, opacity)
    {
        Image = image;
    }

    public ImageBrush(
        string sourceIdentity,
        DrawBrushStretch stretch = DrawBrushStretch.Fill,
        DrawBrushAlignmentX alignmentX = DrawBrushAlignmentX.Center,
        DrawBrushAlignmentY alignmentY = DrawBrushAlignmentY.Center,
        DrawRect? viewport = null,
        DrawRect? viewbox = null,
        DrawTileMode tileMode = DrawTileMode.None,
        float opacity = 1)
        : this((IDrawImage?)null, stretch, alignmentX, alignmentY, viewport, viewbox, tileMode, opacity)
    {
        if (string.IsNullOrWhiteSpace(sourceIdentity))
        {
            throw new ArgumentException("ImageBrush source identity cannot be empty.", nameof(sourceIdentity));
        }

        SourceIdentity = sourceIdentity;
    }

    public ImageBrush(
        ImageSource source,
        DrawBrushStretch stretch = DrawBrushStretch.Fill,
        DrawBrushAlignmentX alignmentX = DrawBrushAlignmentX.Center,
        DrawBrushAlignmentY alignmentY = DrawBrushAlignmentY.Center,
        DrawRect? viewport = null,
        DrawRect? viewbox = null,
        DrawTileMode tileMode = DrawTileMode.None,
        float opacity = 1)
        : this(source?.ResolveDrawImage(), stretch, alignmentX, alignmentY, viewport, viewbox, tileMode, opacity)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public IDrawImage? Image { get; }

    public ImageSource? Source { get; }

    public string? SourceIdentity { get; }

    public override DrawBrushKind Kind => DrawBrushKind.Image;

    public bool Equals(ImageBrush? other)
    {
        return ReferenceEquals(this, other) ||
            other is not null &&
            base.Equals(other) &&
            ReferenceEquals(Image, other.Image) &&
            Equals(Source, other.Source) &&
            string.Equals(SourceIdentity, other.SourceIdentity, StringComparison.Ordinal);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), Image is null ? 0 : RuntimeHelpers.GetHashCode(Image), Source, SourceIdentity);
    }

    protected override DrawBrushDescriptor CreateDescriptor()
    {
        return new ImageDrawBrushDescriptor(
            Source?.ResolveDrawImage() ?? Image,
            Source?.Identity ?? SourceIdentity,
            Stretch,
            AlignmentX,
            AlignmentY,
            Viewport,
            Viewbox,
            TileMode,
            Opacity);
    }
}
