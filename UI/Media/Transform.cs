using Cerneala.Drawing;

namespace Cerneala.UI.Media;

public sealed record Transform(Matrix3x2 Matrix)
{
    public static Transform Identity { get; } = new(Matrix3x2.Identity);

    public DrawPoint Apply(DrawPoint point)
    {
        return Matrix.Transform(point);
    }

    public Transform Compose(Transform next)
    {
        ArgumentNullException.ThrowIfNull(next);
        return new Transform(Matrix3x2.Multiply(Matrix, next.Matrix));
    }
}
