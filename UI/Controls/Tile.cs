using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Resources;
using static Cerneala.UI.Controls.Scene2DModelValidator;

namespace Cerneala.UI.Controls;

/// <summary>An immutable image placement in a tilemap, not a UI element.</summary>
public sealed class Tile
{
    public Tile(ImageReference image, float x = 0, float y = 0, float width = float.NaN, float height = float.NaN)
        : this(image, null, x, y, width, height)
    {
    }

    public Tile(ImageReference image, TileColliderDescriptor2D? collider,
        float x = 0, float y = 0, float width = float.NaN, float height = float.NaN)
    {
        ArgumentNullException.ThrowIfNull(image);
        DrawArgument.ThrowIfNotValidPixelCoordinate(x, nameof(x));
        DrawArgument.ThrowIfNotValidPixelCoordinate(y, nameof(y));
        if (!float.IsNaN(width)) { DrawArgument.ThrowIfNegativeOrNotValidPixelSize(width, nameof(width)); }
        if (!float.IsNaN(height)) { DrawArgument.ThrowIfNegativeOrNotValidPixelSize(height, nameof(height)); }
        Image = image;
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Matrix3x2 placement = Matrix3x2.CreateTranslation(x, y);
        collider?.ValidateGeometry(placement);
        Collider = collider;
        _ = GetDestination(default);
        if (image.DirectImage is IDrawImage direct) { _ = GetDestination(new DrawSize(direct.Width, direct.Height)); }
    }

    public ImageReference Image { get; }
    public float X { get; }
    public float Y { get; }
    public float Width { get; }
    public float Height { get; }
    public TileColliderDescriptor2D? Collider { get; }

    internal DrawRect GetDestination(DrawSize imageSize)
    {
        float width = float.IsNaN(Width) ? imageSize.Width : Width;
        float height = float.IsNaN(Height) ? imageSize.Height : Height;
        DrawArgument.ThrowIfNotValidPixelCoordinate(X + width, nameof(Width));
        DrawArgument.ThrowIfNotValidPixelCoordinate(Y + height, nameof(Height));
        return new DrawRect(X, Y, width, height);
    }
}
