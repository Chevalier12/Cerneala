using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismSurfaceTexture
{
    public static float Height(
        float x,
        float y,
        float scale,
        int texture)
    {
        scale = MathF.Max(scale, 0.125f);
        x /= scale;
        y /= scale;
        float fine = ValueNoise(x * 0.45f, y * 0.45f, 0x51ed270bu);
        float coarse = ValueNoise(x * 0.09f, y * 0.09f, 0x8321ca5du);
        return texture switch
        {
            1 => BrickHeight(x, y, fine, coarse),
            2 => BurlapHeight(x, y, fine, coarse),
            3 => Math.Clamp(
                (0.35f * fine) +
                (0.5f * coarse) +
                (0.15f * ValueNoise(x * 0.9f, y * 0.18f, 0x31a42f19u)),
                0,
                1),
            _ => Math.Clamp(
                (0.45f * fine) +
                (0.25f * coarse) +
                (0.15f * Wave(x * 1.7f)) +
                (0.15f * Wave(y * 1.9f)),
                0,
                1)
        };
    }

    public static float ValueNoise(float x, float y, uint seed)
    {
        int cellX = (int)MathF.Floor(x);
        int cellY = (int)MathF.Floor(y);
        float horizontal = x - cellX;
        float vertical = y - cellY;
        horizontal = horizontal * horizontal * (3 - (2 * horizontal));
        vertical = vertical * vertical * (3 - (2 * vertical));
        float top = float.Lerp(
            Hash(cellX, cellY, seed),
            Hash(cellX + 1, cellY, seed),
            horizontal);
        float bottom = float.Lerp(
            Hash(cellX, cellY + 1, seed),
            Hash(cellX + 1, cellY + 1, seed),
            horizontal);
        return float.Lerp(top, bottom, vertical);
    }

    public static Vector3 LightVector(int direction)
    {
        const float diagonal = 0.7071068f;
        Vector2 planar = direction switch
        {
            1 => new Vector2(diagonal, -diagonal),
            2 => Vector2.UnitX,
            3 => new Vector2(diagonal, diagonal),
            4 => Vector2.UnitY,
            5 => new Vector2(-diagonal, diagonal),
            6 => -Vector2.UnitX,
            7 => new Vector2(-diagonal, -diagonal),
            _ => -Vector2.UnitY
        };
        return Vector3.Normalize(new Vector3(planar, 1.25f));
    }

    private static float BrickHeight(
        float x,
        float y,
        float fine,
        float coarse)
    {
        float row = MathF.Floor(y / 5);
        float shiftedX = x + (((int)row & 1) == 0 ? 0 : 4);
        float verticalMortar = MathF.Abs(
            ((shiftedX / 8) - MathF.Floor(shiftedX / 8)) - 0.5f) * 2;
        float horizontalMortar = MathF.Abs(
            ((y / 5) - MathF.Floor(y / 5)) - 0.5f) * 2;
        float mortar = 1 - MathF.Min(verticalMortar, horizontalMortar);
        return Math.Clamp(
            (0.5f * fine) +
            (0.35f * coarse) +
            (0.15f * mortar),
            0,
            1);
    }

    private static float BurlapHeight(
        float x,
        float y,
        float fine,
        float coarse)
    {
        float horizontal = Wave((y * 2.2f) + (coarse * 1.4f));
        float vertical = Wave((x * 2.05f) - (fine * 1.2f));
        return Math.Clamp(
            (0.28f * fine) +
            (0.18f * coarse) +
            (0.27f * horizontal) +
            (0.27f * vertical),
            0,
            1);
    }

    private static float Wave(float value) =>
        0.5f + (0.5f * MathF.Cos(value));

    private static float Hash(int x, int y, uint seed)
    {
        uint value =
            unchecked((uint)x * 0x9e3779b9u) ^
            unchecked((uint)y * 0x85ebca6bu) ^
            seed;
        value ^= value >> 16;
        value *= 0x7feb352du;
        value ^= value >> 15;
        value *= 0x846ca68bu;
        value ^= value >> 16;
        return (value & 0x00ffffffu) / 16777215f;
    }
}
