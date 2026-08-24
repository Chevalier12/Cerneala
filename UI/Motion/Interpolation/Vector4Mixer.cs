using System.Numerics;

namespace Cerneala.UI.Motion.Interpolation;

public sealed class Vector4Mixer : ValueMixer<Vector4>
{
    public override bool SupportsVectorOperations => true;

    public override Vector4 Mix(Vector4 from, Vector4 to, float progress) =>
        Vector4.Lerp(from, to, Math.Clamp(progress, 0, 1));

    public override bool EqualsWithinTolerance(
        Vector4 left,
        Vector4 right,
        float tolerance)
    {
        ThrowIfNegativeTolerance(tolerance);
        Vector4 difference = Vector4.Abs(left - right);
        return difference.X <= tolerance &&
            difference.Y <= tolerance &&
            difference.Z <= tolerance &&
            difference.W <= tolerance;
    }

    public override Vector4 Add(Vector4 left, Vector4 right) => left + right;

    public override Vector4 Subtract(Vector4 left, Vector4 right) => left - right;

    public override Vector4 Scale(Vector4 value, float scalar) => value * scalar;

    public override float Magnitude(Vector4 value) => value.Length();
}
