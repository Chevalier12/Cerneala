namespace Cerneala.UI.Core;

[Flags]
public enum UiPropertyOptions
{
    None = 0,
    AffectsMeasure = 1 << 0,
    AffectsArrange = 1 << 1,
    AffectsRender = 1 << 2,
    AffectsHitTest = 1 << 3,
    AffectsAspect = 1 << 4,
    AffectsInputVisual = 1 << 5,
    Inherits = 1 << 6,
    ReadOnly = 1 << 7,
    AffectsSemantics = 1 << 8
}
