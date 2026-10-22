using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Media;

namespace Cerneala.UI.Documents;

// The base of the inline content of a TextBlock, as in WPF. A property that is not set takes the effective value of the
// parent Span, or of the TextBlock that owns the inline; TextBlock resolves that chain when it builds its text spans.
public abstract class TextElement : UiObject, IUiPropertyOwner
{
    public static readonly UiProperty<string> FontFamilyProperty = UiProperty<string>.Register(
        nameof(FontFamily),
        typeof(TextElement),
        new UiPropertyMetadata<string>(
            "Default",
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender,
            validateValue: value => !string.IsNullOrWhiteSpace(value)));

    public static readonly UiProperty<float> FontSizeProperty = UiProperty<float>.Register(
        nameof(FontSize),
        typeof(TextElement),
        new UiPropertyMetadata<float>(
            16,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender,
            validateValue: value => value > 0 && float.IsFinite(value)));

    public static readonly UiProperty<FontWeight> FontWeightProperty = UiProperty<FontWeight>.Register(
        nameof(FontWeight),
        typeof(TextElement),
        new UiPropertyMetadata<FontWeight>(
            FontWeight.Normal,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender,
            validateValue: value => Enum.IsDefined(value)));

    public static readonly UiProperty<FontStyle> FontStyleProperty = UiProperty<FontStyle>.Register(
        nameof(FontStyle),
        typeof(TextElement),
        new UiPropertyMetadata<FontStyle>(
            FontStyle.Normal,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender,
            validateValue: value => Enum.IsDefined(value)));

    public static readonly UiProperty<FontStretch> FontStretchProperty = UiProperty<FontStretch>.Register(
        nameof(FontStretch),
        typeof(TextElement),
        new UiPropertyMetadata<FontStretch>(
            FontStretch.Normal,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender,
            validateValue: value => Enum.IsDefined(value)));

    public static readonly UiProperty<Brush?> ForegroundProperty = UiProperty<Brush?>.Register(
        nameof(Foreground),
        typeof(TextElement),
        new UiPropertyMetadata<Brush?>(null, UiPropertyOptions.AffectsRender));

    public string FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public float FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontWeight FontWeight
    {
        get => GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    public FontStyle FontStyle
    {
        get => GetValue(FontStyleProperty);
        set => SetValue(FontStyleProperty, value);
    }

    public FontStretch FontStretch
    {
        get => GetValue(FontStretchProperty);
        set => SetValue(FontStretchProperty, value);
    }

    public Brush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    private UiPropertyMutationObserver? mutationObserver;

    // The TextBlock that this element is part of, or null while it is not in a TextBlock's inline tree.
    internal virtual TextBlock? OwnerTextBlock => null;

    protected override UiPropertyMutationObserver? MutationObserver => mutationObserver ??= new SourceChangeObserver(this);

    // The style of this element on top of the style that it inherits from its parent: each property that this element sets
    // (a local value, a style or trigger value, or a framework default such as the weight of Bold) replaces the
    // inherited one.
    internal virtual InlineStyle Resolve(InlineStyle inherited)
    {
        return inherited with
        {
            FontFamily = IsSet(FontFamilyProperty) ? FontFamily : inherited.FontFamily,
            FontSize = IsSet(FontSizeProperty) ? FontSize : inherited.FontSize,
            FontWeight = IsSet(FontWeightProperty) ? FontWeight : inherited.FontWeight,
            FontStyle = IsSet(FontStyleProperty) ? FontStyle : inherited.FontStyle,
            FontStretch = IsSet(FontStretchProperty) ? FontStretch : inherited.FontStretch,
            Foreground = IsSet(ForegroundProperty) ? Foreground : inherited.Foreground
        };
    }

    public void OnPropertyInvalidated(UiPropertyChangedEventArgs args, UiPropertyOptions options)
    {
        ArgumentNullException.ThrowIfNull(args);
        OwnerTextBlock?.OnInlinePropertyChanged(args.Property, options);
    }

    private void OnPropertyMutated(UiPropertyMutation mutation)
    {
        // A write or a clear that changes the effective value is reported through OnPropertyInvalidated. One that keeps the
        // value but changes its source still changes whether this element sets the property (IsSet), and so what the
        // element takes from its parent: a local value equal to the default stops the inheritance, and clearing it
        // restores the inheritance.
        if (mutation.OldEffectiveSource == mutation.NewEffectiveSource ||
            !mutation.Property.AreEqualUntyped(mutation.OldEffectiveValue, mutation.NewEffectiveValue))
        {
            return;
        }

        UiPropertyOptions options = mutation.Property.Options & (UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender);
        if (options != UiPropertyOptions.None)
        {
            OwnerTextBlock?.OnInlinePropertyChanged(mutation.Property, options);
        }
    }

    private sealed class SourceChangeObserver(TextElement owner) : UiPropertyMutationObserver
    {
        internal override void OnPropertyMutated(UiPropertyMutation mutation) => owner.OnPropertyMutated(mutation);
    }
}
