using Cerneala.UI.Core;

namespace Cerneala.Tests.UI.Core;

public sealed class UiPropertyInvalidationTests
{
    [Fact]
    public void MetadataOptionsTriggerOwnerInvalidationHook()
    {
        UiProperty<int> property = UiProperty<int>.Register(
            UniqueName(),
            typeof(UiPropertyInvalidationTests),
            new UiPropertyMetadata<int>(
                0,
                UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsHitTest));
        InvalidatingObject owner = new();

        owner.SetValue(property, 1);

        Assert.Single(owner.Invalidations);
        Assert.Same(property, owner.Invalidations[0].Property);
        Assert.True(owner.Invalidations[0].Options.HasFlag(UiPropertyOptions.AffectsMeasure));
        Assert.True(owner.Invalidations[0].Options.HasFlag(UiPropertyOptions.AffectsRender));
        Assert.True(owner.Invalidations[0].Options.HasFlag(UiPropertyOptions.AffectsHitTest));
    }

    [Fact]
    public void EqualEffectiveValueDoesNotNotifyOrInvalidate()
    {
        UiProperty<int> property = UiProperty<int>.Register(
            UniqueName(),
            typeof(UiPropertyInvalidationTests),
            new UiPropertyMetadata<int>(5, UiPropertyOptions.AffectsRender));
        InvalidatingObject owner = new();
        int changes = 0;
        owner.PropertyChanged += (_, _) => changes++;

        int previous = owner.SetValue(property, 5);

        Assert.Equal(5, previous);
        Assert.Equal(0, changes);
        Assert.Empty(owner.Invalidations);
    }

    [Fact]
    public void LowerPrecedenceChangeDoesNotInvalidateWhenEffectiveValueIsUnchanged()
    {
        UiProperty<string> property = UiProperty<string>.Register(
            UniqueName(),
            typeof(UiPropertyInvalidationTests),
            new UiPropertyMetadata<string>("default", UiPropertyOptions.AffectsRender));
        InvalidatingObject owner = new();
        owner.SetValue(property, "local");
        owner.Invalidations.Clear();

        owner.SetValue(property, "style", UiPropertyValueSource.StyleBase);

        Assert.Equal("local", owner.GetValue(property));
        Assert.Empty(owner.Invalidations);
    }

    [Fact]
    public void NonInvalidationOptionsDoNotTriggerOwnerInvalidationHook()
    {
        UiProperty<string> property = UiProperty<string>.Register(
            UniqueName(),
            typeof(UiPropertyInvalidationTests),
            new UiPropertyMetadata<string>("default", UiPropertyOptions.Inherits));
        InvalidatingObject owner = new();

        owner.SetValue(property, "inherited", UiPropertyValueSource.Inherited);

        Assert.Equal("inherited", owner.GetValue(property));
        Assert.Empty(owner.Invalidations);
    }

    private static string UniqueName()
    {
        return $"{nameof(UiPropertyInvalidationTests)}_{Guid.NewGuid():N}";
    }

    private sealed class InvalidatingObject : UiObject, IUiPropertyOwner
    {
        public List<(UiProperty Property, UiPropertyOptions Options)> Invalidations { get; } = new();

        public void OnPropertyInvalidated(UiPropertyChangedEventArgs args, UiPropertyOptions options)
        {
            Invalidations.Add((args.Property, options));
        }
    }
}
