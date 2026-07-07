using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Controls.Shapes;

public abstract class Shape : Control
{
    public static readonly UiProperty<Brush?> FillProperty = UiProperty<Brush?>.Register(
        nameof(Fill),
        typeof(Shape),
        new UiPropertyMetadata<Brush?>(null, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<Brush?> StrokeProperty = UiProperty<Brush?>.Register(
        nameof(Stroke),
        typeof(Shape),
        new UiPropertyMetadata<Brush?>(null, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<float> StrokeThicknessProperty = UiProperty<float>.Register(
        nameof(StrokeThickness),
        typeof(Shape),
        new UiPropertyMetadata<float>(1, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender, validateValue: IsValidStrokeThickness));

    public static readonly UiProperty<Geometry?> GeometryProperty = UiProperty<Geometry?>.Register(
        nameof(Geometry),
        typeof(Shape),
        new UiPropertyMetadata<Geometry?>(null, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    public new static readonly UiProperty<Transform> RenderTransformProperty = UiProperty<Transform>.Register(
        nameof(RenderTransform),
        typeof(Shape),
        new UiPropertyMetadata<Transform>(Transform.Identity, UiPropertyOptions.AffectsRender, validateValue: value => value is not null));

    public new static readonly UiProperty<float> OpacityProperty = UiProperty<float>.Register(
        nameof(Opacity),
        typeof(Shape),
        new UiPropertyMetadata<float>(1, UiPropertyOptions.AffectsRender, validateValue: value => float.IsFinite(value) && value >= 0 && value <= 1));

    public static readonly UiProperty<ShadowEffect?> ShadowProperty = UiProperty<ShadowEffect?>.Register(
        nameof(Shadow),
        typeof(Shape),
        new UiPropertyMetadata<ShadowEffect?>(null, UiPropertyOptions.AffectsRender));

    public Brush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public Brush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public float StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public Geometry? Geometry
    {
        get => GetValue(GeometryProperty);
        set => SetValue(GeometryProperty, value);
    }

    public new Transform RenderTransform
    {
        get => GetValue(RenderTransformProperty);
        set => SetValue(RenderTransformProperty, value);
    }

    public new float Opacity
    {
        get => GetValue(OpacityProperty);
        set => SetValue(OpacityProperty, value);
    }

    public ShadowEffect? Shadow
    {
        get => GetValue(ShadowProperty);
        set => SetValue(ShadowProperty, value);
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        Geometry? geometry = ResolveGeometry(LayoutRect.Empty);
        if (geometry is null)
        {
            return LayoutSize.Zero;
        }

        float strokePadding = Stroke?.SolidColor is null ? 0 : StrokeThickness;
        return new LayoutSize(
            geometry.Bounds.Width + strokePadding,
            geometry.Bounds.Height + strokePadding);
    }

    protected override void OnRender(RenderContext context)
    {
        if (Opacity <= 0)
        {
            return;
        }

        Geometry? geometry = ResolveGeometry(context.Bounds);
        if (geometry is null)
        {
            return;
        }

        RenderGeometry(context, geometry);
    }

    protected abstract Geometry? ResolveGeometry(LayoutRect arrangedBounds);

    protected virtual void RenderGeometry(RenderContext context, Geometry geometry)
    {
        DrawColor fill = Fill?.SolidColor ?? DrawColor.Transparent;
        DrawColor stroke = Stroke?.SolidColor ?? DrawColor.Transparent;
        float thickness = StrokeThickness;

        switch (geometry)
        {
            case RectangleGeometry rectangle:
                DrawRect rectangleBounds = TransformBounds(rectangle.Bounds);
                if (HasVisibleColor(fill) && rectangleBounds.Width > 0 && rectangleBounds.Height > 0)
                {
                    context.DrawingContext.FillRectangle(rectangleBounds, ApplyOpacity(fill));
                }

                if (HasVisibleColor(stroke) && thickness > 0 && rectangleBounds.Width > 0 && rectangleBounds.Height > 0)
                {
                    context.DrawingContext.DrawRectangle(rectangleBounds, ApplyOpacity(stroke), thickness);
                }

                break;

            case EllipseGeometry ellipse:
                DrawRect ellipseBounds = TransformBounds(ellipse.Bounds);
                if (HasVisibleColor(fill) && ellipseBounds.Width > 0 && ellipseBounds.Height > 0)
                {
                    context.DrawingContext.FillEllipse(ellipseBounds, ApplyOpacity(fill));
                }

                if (HasVisibleColor(stroke) && thickness > 0 && ellipseBounds.Width > 0 && ellipseBounds.Height > 0)
                {
                    context.DrawingContext.DrawEllipse(ellipseBounds, ApplyOpacity(stroke), thickness);
                }

                break;

            case PathGeometry path:
                if (HasVisibleColor(stroke) && thickness > 0)
                {
                    DrawPathStroke(context, path, ApplyOpacity(stroke), thickness);
                }

                break;
        }
    }

    private DrawRect TransformBounds(DrawRect bounds)
    {
        DrawPoint topLeft = RenderTransform.Apply(new DrawPoint(bounds.X, bounds.Y));
        DrawPoint topRight = RenderTransform.Apply(new DrawPoint(bounds.Right, bounds.Y));
        DrawPoint bottomLeft = RenderTransform.Apply(new DrawPoint(bounds.X, bounds.Bottom));
        DrawPoint bottomRight = RenderTransform.Apply(new DrawPoint(bounds.Right, bounds.Bottom));

        float minX = MathF.Min(MathF.Min(topLeft.X, topRight.X), MathF.Min(bottomLeft.X, bottomRight.X));
        float minY = MathF.Min(MathF.Min(topLeft.Y, topRight.Y), MathF.Min(bottomLeft.Y, bottomRight.Y));
        float maxX = MathF.Max(MathF.Max(topLeft.X, topRight.X), MathF.Max(bottomLeft.X, bottomRight.X));
        float maxY = MathF.Max(MathF.Max(topLeft.Y, topRight.Y), MathF.Max(bottomLeft.Y, bottomRight.Y));

        return new DrawRect(minX, minY, maxX - minX, maxY - minY);
    }

    protected static DrawRect ToDrawRect(LayoutRect rect)
    {
        return new DrawRect(rect.X, rect.Y, MathF.Max(0, rect.Width), MathF.Max(0, rect.Height));
    }

    private void DrawPathStroke(RenderContext context, PathGeometry path, DrawColor color, float thickness)
    {
        for (int i = 1; i < path.Points.Count; i++)
        {
            DrawPoint start = RenderTransform.Apply(path.Points[i - 1]);
            DrawPoint end = RenderTransform.Apply(path.Points[i]);
            context.DrawingContext.DrawLine(start, end, color, thickness);
        }
    }

    private DrawColor ApplyOpacity(DrawColor color)
    {
        return new DrawColor(color.R, color.G, color.B, (byte)Math.Clamp((int)MathF.Round(color.A * Opacity), 0, 255));
    }

    private static bool HasVisibleColor(DrawColor color)
    {
        return color.A > 0;
    }

    private static bool IsValidStrokeThickness(float value)
    {
        return float.IsFinite(value) && value >= 0;
    }
}
