namespace Cerneala.UI.Core;

// A changed effective value waiting for its notification: the PropertyChanged arguments and the mutation details.
internal readonly record struct UiPropertyChange(
    UiPropertyChangedEventArgs Args,
    UiPropertyValueSource MutatingSource,
    UiPropertyValueSource OldEffectiveSource,
    object? OldSourceValue,
    object? NewSourceValue,
    bool WasCoerced);
