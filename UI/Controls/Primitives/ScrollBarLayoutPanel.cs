using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Media;
using DirectionPath = Cerneala.UI.Controls.Shapes.Path;
using DirectionShape = Cerneala.UI.Controls.Shapes.Shape;

namespace Cerneala.UI.Controls.Primitives;

internal sealed class ScrollBarLayoutPanel : Cerneala.UI.Layout.Panels.Panel
{
    private const float ButtonLength = 12;
    private const float CrossLength = 10;
    private const string UpGlyphData = "M 384,871.92523 346.53984,908.36226 223.99999,789.16924 101.46016,908.36226 64,871.92523 l 37.42566,-36.40349 0.0344,0.0335 L 224,716.36226 l 122.53983,119.19302 0.0344,-0.0335 37.42568,36.4035 z";
    private const string DownGlyphData = "M 64,784.79925 L 101.46016,748.36222 L 224.00001,867.55524 L 346.53984,748.36222 L 384,784.79925 L 346.57434,821.20274 L 346.53994,821.16924 L 224,940.36222 L 101.46017,821.1692 L 101.42577,821.2027 L 64.00009,784.7992 z";
    private const string LeftGlyphData = "M 267.56299,668.36224 L 304.00002,705.8224 L 184.807,828.36225 L 304.00002,950.90208 L 267.56299,988.36224 L 231.1595,950.93658 L 231.193,950.90218 L 112.00002,828.36224 L 231.19304,705.82241 L 231.15954,705.78801 L 267.56304,668.36233 z";
    private const string RightGlyphData = "M 180.43701,988.36224 L 143.99998,950.90208 L 263.193,828.36223 L 143.99998,705.8224 L 180.43701,668.36224 L 216.8405,705.7879 L 216.807,705.8223 L 335.99998,828.36224 L 216.80696,950.90207 L 216.84046,950.93647 L 180.43696,988.36215 z";
    private static readonly DrawRect GlyphViewBox = new(0, 604.36224f, 448, 448);
    private static readonly SvgGeometry UpGlyphGeometry = new(UpGlyphData, GlyphViewBox);
    private static readonly SvgGeometry DownGlyphGeometry = new(DownGlyphData, GlyphViewBox);
    private static readonly SvgGeometry LeftGlyphGeometry = new(LeftGlyphData, GlyphViewBox);
    private static readonly SvgGeometry RightGlyphGeometry = new(RightGlyphData, GlyphViewBox);
    private readonly RepeatButton decreaseButton;
    private readonly Track track;
    private readonly RepeatButton increaseButton;
    private readonly DirectionPath decreaseGlyph = CreateDirectionGlyph();
    private readonly DirectionPath increaseGlyph = CreateDirectionGlyph();

    public ScrollBarLayoutPanel(RepeatButton decreaseButton, Track track, RepeatButton increaseButton)
    {
        this.decreaseButton = decreaseButton;
        this.track = track;
        this.increaseButton = increaseButton;
        decreaseButton.Content = decreaseGlyph;
        increaseButton.Content = increaseGlyph;
        decreaseButton.PropertyChanged += OnDirectionButtonPropertyChanged;
        increaseButton.PropertyChanged += OnDirectionButtonPropertyChanged;
        VisualChildren.Add(decreaseButton);
        VisualChildren.Add(track);
        VisualChildren.Add(increaseButton);
        SyncGlyphFill(decreaseButton, decreaseGlyph);
        SyncGlyphFill(increaseButton, increaseGlyph);
        UpdateGlyphs();
    }

    public static readonly UiProperty<Orientation> OrientationProperty = UiProperty<Orientation>.Register(
        nameof(Orientation),
        typeof(ScrollBarLayoutPanel),
        new UiPropertyMetadata<Orientation>(Orientation.Horizontal, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange));

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        LayoutSize buttonAvailable = Orientation == Orientation.Horizontal
            ? new LayoutSize(ButtonLength, CrossLength)
            : new LayoutSize(CrossLength, ButtonLength);
        decreaseButton.Measure(new MeasureContext(buttonAvailable, context.Rounding));
        increaseButton.Measure(new MeasureContext(buttonAvailable, context.Rounding));

        float availableLength = Orientation == Orientation.Horizontal
            ? context.AvailableSize.Width
            : context.AvailableSize.Height;
        float trackLength = float.IsPositiveInfinity(availableLength)
            ? float.PositiveInfinity
            : MathF.Max(0, availableLength - (ButtonLength * 2));
        LayoutSize trackAvailable = Orientation == Orientation.Horizontal
            ? new LayoutSize(trackLength, CrossLength)
            : new LayoutSize(CrossLength, trackLength);
        LayoutSize trackSize = track.Measure(new MeasureContext(trackAvailable, context.Rounding));

        return Orientation == Orientation.Horizontal
            ? new LayoutSize((ButtonLength * 2) + trackSize.Width, CrossLength)
            : new LayoutSize(CrossLength, (ButtonLength * 2) + trackSize.Height);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        float length = Orientation == Orientation.Horizontal ? context.FinalRect.Width : context.FinalRect.Height;
        float buttonLength = MathF.Min(ButtonLength, length / 2);
        float trackLength = MathF.Max(0, length - (buttonLength * 2));

        if (Orientation == Orientation.Horizontal)
        {
            decreaseButton.Arrange(new ArrangeContext(
                new LayoutRect(context.FinalRect.X, context.FinalRect.Y, buttonLength, context.FinalRect.Height),
                context.Rounding));
            track.Arrange(new ArrangeContext(
                new LayoutRect(context.FinalRect.X + buttonLength, context.FinalRect.Y, trackLength, context.FinalRect.Height),
                context.Rounding));
            increaseButton.Arrange(new ArrangeContext(
                new LayoutRect(context.FinalRect.X + buttonLength + trackLength, context.FinalRect.Y, buttonLength, context.FinalRect.Height),
                context.Rounding));
        }
        else
        {
            decreaseButton.Arrange(new ArrangeContext(
                new LayoutRect(context.FinalRect.X, context.FinalRect.Y, context.FinalRect.Width, buttonLength),
                context.Rounding));
            track.Arrange(new ArrangeContext(
                new LayoutRect(context.FinalRect.X, context.FinalRect.Y + buttonLength, context.FinalRect.Width, trackLength),
                context.Rounding));
            increaseButton.Arrange(new ArrangeContext(
                new LayoutRect(context.FinalRect.X, context.FinalRect.Y + buttonLength + trackLength, context.FinalRect.Width, buttonLength),
                context.Rounding));
        }

        return context.FinalRect;
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, OrientationProperty))
        {
            UpdateGlyphs();
        }
    }

    private void UpdateGlyphs()
    {
        decreaseGlyph.Geometry = Orientation == Orientation.Horizontal ? LeftGlyphGeometry : UpGlyphGeometry;
        increaseGlyph.Geometry = Orientation == Orientation.Horizontal ? RightGlyphGeometry : DownGlyphGeometry;
    }

    private void OnDirectionButtonPropertyChanged(object? sender, UiPropertyChangedEventArgs args)
    {
        if (!ReferenceEquals(args.Property, Control.ForegroundProperty))
        {
            return;
        }

        if (ReferenceEquals(sender, decreaseButton))
        {
            SyncGlyphFill(decreaseButton, decreaseGlyph);
        }
        else if (ReferenceEquals(sender, increaseButton))
        {
            SyncGlyphFill(increaseButton, increaseGlyph);
        }
    }

    private static void SyncGlyphFill(RepeatButton button, DirectionPath glyph)
    {
        glyph.SetValue(
            DirectionShape.FillProperty,
            button.Foreground,
            UiPropertyValueSource.TemplateBinding);
    }

    private static DirectionPath CreateDirectionGlyph()
    {
        return new DirectionPath
        {
            Geometry = UpGlyphGeometry
        };
    }
}
