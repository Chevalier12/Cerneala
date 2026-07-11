using Cerneala.Drawing;

namespace Cerneala.UI.Media;

public sealed record DrawingBrush : TileBrush
{
    private readonly DrawCommand[] commands;

    public DrawingBrush(
        IEnumerable<DrawCommand> commands,
        DrawRect contentBounds,
        DrawBrushStretch stretch = DrawBrushStretch.Fill,
        DrawBrushAlignmentX alignmentX = DrawBrushAlignmentX.Center,
        DrawBrushAlignmentY alignmentY = DrawBrushAlignmentY.Center,
        DrawRect? viewport = null,
        DrawRect? viewbox = null,
        DrawTileMode tileMode = DrawTileMode.None,
        float opacity = 1)
        : base(stretch, alignmentX, alignmentY, viewport, viewbox, tileMode, opacity)
    {
        ArgumentNullException.ThrowIfNull(commands);
        if (!float.IsFinite(contentBounds.X) || !float.IsFinite(contentBounds.Y) ||
            !float.IsFinite(contentBounds.Width) || !float.IsFinite(contentBounds.Height) ||
            contentBounds.Width <= 0 || contentBounds.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(contentBounds));
        }

        this.commands = commands.ToArray();
        ContentBounds = contentBounds;
    }

    public IReadOnlyList<DrawCommand> Commands => Array.AsReadOnly(commands);

    public DrawRect ContentBounds { get; }

    public override DrawBrushKind Kind => DrawBrushKind.Drawing;

    public bool Equals(DrawingBrush? other)
    {
        return ReferenceEquals(this, other) ||
            other is not null &&
            base.Equals(other) &&
            ContentBounds == other.ContentBounds &&
            commands.SequenceEqual(other.commands);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(base.GetHashCode());
        hash.Add(ContentBounds);
        foreach (DrawCommand command in commands)
        {
            hash.Add(command);
        }

        return hash.ToHashCode();
    }

    protected override DrawBrushDescriptor CreateDescriptor()
    {
        return new DrawingDrawBrushDescriptor(
            Commands, ContentBounds, Stretch, AlignmentX, AlignmentY, Viewport, Viewbox, TileMode, Opacity);
    }
}
