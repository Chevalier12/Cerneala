using System.Numerics;

namespace Cerneala.UI.Prism.Definitions;


public sealed class PrismColorMatrixResource
{








    public PrismColorMatrixResource(
        Matrix4x4 matrix,
        Vector4 offset)
    {
        ValidateFinite(matrix, nameof(matrix));
        ValidateFinite(offset, nameof(offset));
        Matrix = matrix;
        Offset = offset;
    }




    public Matrix4x4 Matrix { get; }


    public Vector4 Offset { get; }

    private static void ValidateFinite(
        Matrix4x4 matrix,
        string parameterName)
    {
        if (!float.IsFinite(matrix.M11) ||
            !float.IsFinite(matrix.M12) ||
            !float.IsFinite(matrix.M13) ||
            !float.IsFinite(matrix.M14) ||
            !float.IsFinite(matrix.M21) ||
            !float.IsFinite(matrix.M22) ||
            !float.IsFinite(matrix.M23) ||
            !float.IsFinite(matrix.M24) ||
            !float.IsFinite(matrix.M31) ||
            !float.IsFinite(matrix.M32) ||
            !float.IsFinite(matrix.M33) ||
            !float.IsFinite(matrix.M34) ||
            !float.IsFinite(matrix.M41) ||
            !float.IsFinite(matrix.M42) ||
            !float.IsFinite(matrix.M43) ||
            !float.IsFinite(matrix.M44))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Color-matrix coefficients must be finite.");
        }
    }

    private static void ValidateFinite(
        Vector4 value,
        string parameterName)
    {
        if (!float.IsFinite(value.X) ||
            !float.IsFinite(value.Y) ||
            !float.IsFinite(value.Z) ||
            !float.IsFinite(value.W))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Color-matrix offsets must be finite.");
        }
    }
}
