using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismIncrementalVoronoiSet
{
    private const int TileSize = 16;


    private static ReadOnlySpan<uint> PackedRanks =>
    [
        0x8a208000u, 0xc42e830cu, 0xb32d8f03u, 0xea3aee0au,
        0x79b55096u, 0x51f655e4u, 0x6b856efeu, 0x7fb941fdu,
        0xa3138c37u, 0xf91ca633u, 0x9410902fu, 0xd118863du,
        0x66b04f9cu, 0x68e0578bu, 0x60b667fbu, 0x5bef4aadu,
        0xa026840fu, 0xa125c605u, 0xac31aa08u, 0xa536bd06u,
        0x70e548edu, 0x569a7da2u, 0x71a959d7u, 0x4b8745c0u,
        0xb11ef539u, 0x981dca38u, 0xf21bb729u, 0xe617e732u,
        0x5faf7af7u, 0x78fa76fcu, 0x43cc53bfu, 0x479558e2u,
        0xd83c9e02u, 0xd5308d09u, 0xda2ab201u, 0x933b920bu,
        0x5ccb52cfu, 0x77ab4697u, 0x4dbb6ff4u, 0x619f63bcu,
        0xd916c13eu, 0xc2158924u, 0xd411de2bu, 0xd61af021u,
        0x75be4cc5u, 0x62d344cdu, 0x64d25ef1u, 0x72db7bd0u,
        0xeb22dc0eu, 0xba279b07u, 0xa42c820du, 0xff288e04u,
        0x4e885499u, 0x73df40ecu, 0x65e86cddu, 0x49f86a81u,
        0xc914b434u, 0xc319a723u, 0xe312b835u, 0xe11fce3fu,
        0x69a874c7u, 0x5ac842e9u, 0x7e915daeu, 0x6d9d7cf3u
    ];

    internal static int Rank(
        int cellX,
        int cellY,
        uint seed)
    {
        int x = (cellX + (int)(seed & 15u)) & 15;
        int y = (cellY + (int)((seed >> 4) & 15u)) & 15;
        uint transform = (seed >> 8) & 7u;
        if ((transform & 4u) != 0)
        {
            (x, y) = (y, x);
        }
        if ((transform & 1u) != 0)
        {
            x = TileSize - 1 - x;
        }
        if ((transform & 2u) != 0)
        {
            y = TileSize - 1 - y;
        }

        int index = (y * TileSize) + x;
        uint packed = PackedRanks[index >> 2];
        return (int)((packed >> ((index & 3) * 8)) & 0xffu);
    }

    internal static float Threshold(
        int cellX,
        int cellY,
        uint seed) =>
        (Rank(cellX, cellY, seed) + 0.5f) / 256f;

    internal static Vector2 Center(
        int cellX,
        int cellY,
        uint seed,
        float cellSize) =>
        new(
            (cellX + 0.15f +
                (0.7f * Hash(
                    cellX,
                    cellY,
                    seed ^ 0x13579bdfu))) *
                cellSize,
            (cellY + 0.15f +
                (0.7f * Hash(
                    cellX,
                    cellY,
                    seed ^ 0x2468ace0u))) *
                cellSize);

    private static float Hash(
        int x,
        int y,
        uint seed)
    {
        uint value = unchecked(
            ((uint)x * 0x9e3779b9u) ^
            ((uint)y * 0x85ebca6bu) ^
            seed);
        value ^= value >> 16;
        value = unchecked(value * 0x7feb352du);
        value ^= value >> 15;
        value = unchecked(value * 0x846ca68bu);
        value ^= value >> 16;
        return (value & 0x00ffffffu) /
            16777215f;
    }
}
