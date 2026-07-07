namespace Cerneala.UI.Core;

public sealed record UiPropertyMutation(
    UiObject Target,
    UiProperty Property,
    UiPropertyValueSource MutatingSource,
    object? OldEffectiveValue,
    UiPropertyValueSource OldEffectiveSource,
    object? NewEffectiveValue,
    UiPropertyValueSource NewEffectiveSource,
    object? OldSourceValue,
    object? NewSourceValue,
    bool WasCoerced);
