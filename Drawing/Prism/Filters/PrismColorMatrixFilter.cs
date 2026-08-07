using System.Numerics;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismColorMatrixFilter
{
    public const float MaximumHalfValue = 65504;

    public static Vector4 Apply(
        Vector4 straightRgba,
        PrismColorMatrixResource? resource,
        bool clamp)
    {
        Matrix4x4 matrix = resource?.Matrix ?? Matrix4x4.Identity;
        Vector4 offset = resource?.Offset ?? Vector4.Zero;
        Vector4 transformed = new(
            DotRow(straightRgba, matrix.M11, matrix.M12, matrix.M13, matrix.M14) + offset.X,
            DotRow(straightRgba, matrix.M21, matrix.M22, matrix.M23, matrix.M24) + offset.Y,
            DotRow(straightRgba, matrix.M31, matrix.M32, matrix.M33, matrix.M34) + offset.Z,
            DotRow(straightRgba, matrix.M41, matrix.M42, matrix.M43, matrix.M44) + offset.W);
        if (clamp)
        {
            return Vector4.Clamp(
                transformed,
                Vector4.Zero,
                Vector4.One);
        }

        return new Vector4(
            Math.Clamp(transformed.X, -MaximumHalfValue, MaximumHalfValue),
            Math.Clamp(transformed.Y, -MaximumHalfValue, MaximumHalfValue),
            Math.Clamp(transformed.Z, -MaximumHalfValue, MaximumHalfValue),
            Math.Clamp(transformed.W, 0, 1));
    }

    public static void Pack(
        PrismColorMatrixResource? resource,
        out Vector4 rowRed,
        out Vector4 rowGreen,
        out Vector4 rowBlue,
        out Vector4 rowAlpha,
        out Vector4 offset)
    {
        Matrix4x4 matrix = resource?.Matrix ?? Matrix4x4.Identity;
        rowRed = new Vector4(
            matrix.M11,
            matrix.M12,
            matrix.M13,
            matrix.M14);
        rowGreen = new Vector4(
            matrix.M21,
            matrix.M22,
            matrix.M23,
            matrix.M24);
        rowBlue = new Vector4(
            matrix.M31,
            matrix.M32,
            matrix.M33,
            matrix.M34);
        rowAlpha = new Vector4(
            matrix.M41,
            matrix.M42,
            matrix.M43,
            matrix.M44);
        offset = resource?.Offset ?? Vector4.Zero;
    }

    private static float DotRow(
        Vector4 value,
        float red,
        float green,
        float blue,
        float alpha) =>
        (value.X * red) +
        (value.Y * green) +
        (value.Z * blue) +
        (value.W * alpha);
}
