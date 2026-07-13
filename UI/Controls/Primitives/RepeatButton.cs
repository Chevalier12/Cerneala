using Cerneala.UI.Core;
using Cerneala.UI.Markup;

namespace Cerneala.UI.Controls.Primitives;

public class RepeatButton : Button
{
    public static readonly UiProperty<int> DelayProperty = UiProperty<int>.Register(
        nameof(Delay),
        typeof(RepeatButton),
        new UiPropertyMetadata<int>(500, validateValue: value => value >= 0));

    public static readonly UiProperty<int> IntervalProperty = UiProperty<int>.Register(
        nameof(Interval),
        typeof(RepeatButton),
        new UiPropertyMetadata<int>(100, validateValue: value => value > 0));

    [MarkupValueConstraint(MarkupValueConstraint.NonNegative)]
    public int Delay
    {
        get => GetValue(DelayProperty);
        set => SetValue(DelayProperty, value);
    }

    [MarkupValueConstraint(MarkupValueConstraint.Positive)]
    public int Interval
    {
        get => GetValue(IntervalProperty);
        set => SetValue(IntervalProperty, value);
    }

    protected override bool ShouldClickOnMouseUp => false;

    internal override TimeSpan? PointerRepeatDelay => TimeSpan.FromMilliseconds(Delay);

    internal override TimeSpan PointerRepeatInterval => TimeSpan.FromMilliseconds(Interval);
}
