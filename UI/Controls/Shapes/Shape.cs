using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
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

    public static readonly UiProperty<DrawFillRule> FillRuleProperty = UiProperty<DrawFillRule>.Register(
        nameof(FillRule),
        typeof(Shape),
        new UiPropertyMetadata<DrawFillRule>(DrawFillRule.NonZero, UiPropertyOptions.AffectsRender,
            validateValue: value => Enum.IsDefined(value)));

    public new static readonly UiProperty<Transform> RenderTransformProperty = UIElement.RenderTransformProperty;

    public new static readonly UiProperty<float> OpacityProperty = UIElement.OpacityProperty;

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

    public DrawFillRule FillRule
    {
        get => GetValue(FillRuleProperty);
        set => SetValue(FillRuleProperty, value);
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

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        Geometry? geometry = ResolveGeometry(LayoutRect.Empty);
        if (geometry is null)
        {
            return LayoutSize.Zero;
        }

        float strokePadding = Stroke is null ? 0 : StrokeThickness;
        return new LayoutSize(
            geometry.Bounds.Width + strokePadding,
            geometry.Bounds.Height + strokePadding);
    }

    protected override void OnRender(RenderContext context)
    {
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
        Brush? fill = Fill;
        Brush? stroke = Stroke;
        float thickness = StrokeThickness;

        switch (geometry)
        {
            case SvgGeometry svgPath:
                DrawRect destination = ToDrawRect(context.Bounds);
                if (HasVisibleBrush(fill) && destination.Width > 0 && destination.Height > 0)
                {
                    context.DrawingContext.FillPath(
                        svgPath.Path,
                        svgPath.Bounds,
                        destination,
                        fill!,
                        FillRule);
                }
                if (HasVisibleBrush(stroke) && thickness > 0 && destination.Width > 0 && destination.Height > 0)
                {
                    context.DrawingContext.DrawPath(
                        svgPath.Path,
                        svgPath.Bounds,
                        destination,
                        new DrawPen(stroke!, thickness));
                }
                break;

            case RectangleGeometry rectangle:
                DrawRect rectangleBounds = rectangle.Bounds;
                if (HasVisibleBrush(fill) && rectangleBounds.Width > 0 && rectangleBounds.Height > 0)
                {
                    context.DrawingContext.FillRectangle(rectangleBounds, fill!);
                }

                if (HasVisibleBrush(stroke) && thickness > 0 && rectangleBounds.Width > 0 && rectangleBounds.Height > 0)
                {
                    context.DrawingContext.DrawRectangle(
                        rectangleBounds,
                        new DrawPen(stroke!, thickness));
                }

                break;

            case EllipseGeometry ellipse:
                DrawRect ellipseBounds = ellipse.Bounds;
                if (HasVisibleBrush(fill) && ellipseBounds.Width > 0 && ellipseBounds.Height > 0)
                {
                    context.DrawingContext.FillEllipse(ellipseBounds, fill!);
                }

                if (HasVisibleBrush(stroke) && thickness > 0 && ellipseBounds.Width > 0 && ellipseBounds.Height > 0)
                {
                    context.DrawingContext.DrawEllipse(
                        ellipseBounds,
                        new DrawPen(stroke!, thickness));
                }

                break;

            case PathGeometry path:
                bool hasFill = HasVisibleBrush(fill) && path.Bounds.Width > 0 && path.Bounds.Height > 0;
                bool hasStroke = HasVisibleBrush(stroke) && thickness > 0;
                if (path.Path is not null && (hasFill || hasStroke))
                {
                    bool hasOffset = context.Bounds.X != 0 || context.Bounds.Y != 0;
                    if (hasOffset)
                    {
                        context.DrawingContext.PushTransform(System.Numerics.Matrix3x2.CreateTranslation(
                            context.Bounds.X, context.Bounds.Y));
                    }
                    if (hasFill)
                    {
                        context.DrawingContext.FillPath(path.Path, fill!, FillRule);
                    }
                    if (hasStroke)
                    {
                        context.DrawingContext.DrawPath(path.Path, new DrawPen(stroke!, thickness));
                    }
                    if (hasOffset)
                    {
                        context.DrawingContext.PopTransform();
                    }
                }

                break;
        }
    }

    protected static DrawRect ToDrawRect(LayoutRect rect)
    {
        return new DrawRect(rect.X, rect.Y, MathF.Max(0, rect.Width), MathF.Max(0, rect.Height));
    }

    private static bool HasVisibleBrush(Brush? brush)
    {
        return brush is not null && brush.Opacity > 0 && (brush.SolidColor?.A ?? 255) > 0;
    }

    private static bool IsValidStrokeThickness(float value)
    {
        return float.IsFinite(value) && value >= 0;
    }
}
