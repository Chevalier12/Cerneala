using Cerneala.UI.Core;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

namespace Cerneala.UI.Controls.Primitives;

internal sealed class ScrollBarLayoutPanel : Cerneala.UI.Layout.Panels.Panel
{
    private const float ButtonLength = 12;
    private const float CrossLength = 10;
    private readonly RepeatButton decreaseButton;
    private readonly Track track;
    private readonly RepeatButton increaseButton;

    public ScrollBarLayoutPanel(RepeatButton decreaseButton, Track track, RepeatButton increaseButton)
    {
        this.decreaseButton = decreaseButton;
        this.track = track;
        this.increaseButton = increaseButton;
        VisualChildren.Add(decreaseButton);
        VisualChildren.Add(track);
        VisualChildren.Add(increaseButton);
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
        decreaseButton.Content = Orientation == Orientation.Horizontal ? "<" : "^";
        increaseButton.Content = Orientation == Orientation.Horizontal ? ">" : "v";
    }
}
