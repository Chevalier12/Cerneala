namespace Cerneala.UI.Motion.Layout;

public readonly record struct LayoutMotionId(string Value)
{
    public static implicit operator LayoutMotionId(string value)
    {
        return new LayoutMotionId(value);
    }

    public static implicit operator string(LayoutMotionId id)
    {
        return id.Value;
    }
}
