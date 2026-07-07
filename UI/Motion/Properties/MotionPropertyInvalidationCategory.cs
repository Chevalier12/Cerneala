namespace Cerneala.UI.Motion.Properties;

[Flags]
public enum MotionPropertyInvalidationCategory
{
    None = 0,
    Render = 1 << 0,
    Layout = 1 << 1,
    HitTest = 1 << 2,
    Semantics = 1 << 3
}
