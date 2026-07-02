namespace Cerneala.UI.Core;

[Flags]
public enum UiPropertyOptions
{
    None = 0,
    AffectsMeasure = 1 << 0,
    AffectsArrange = 1 << 1,
    AffectsRender = 1 << 2,
    AffectsHitTest = 1 << 3,
    AffectsStyle = 1 << 4,
    Inherits = 1 << 5,
    ReadOnly = 1 << 6
}
