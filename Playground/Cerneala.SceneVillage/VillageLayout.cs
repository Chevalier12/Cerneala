using System.Numerics;

namespace Cerneala.SceneVillage;

internal static class VillageLayout
{
    internal const float WorldSize = 4096f;
    internal const float TileSize = 16f;
    internal const float CharacterSize = 32f;
    internal const float StressPitch = 32f;
    internal const float PlayerSpeed = 200f;
    internal const int MaximumStressCount = 10_000;

    internal static readonly Vector2 PlayerSpawn = new(2048f, 2048f);
    internal static readonly Vector2 StressFieldWaypoint = new(2600f, 2056f);

    internal static readonly HouseSite[] Houses =
    [
        new(1744f, 1752f, false),
        new(2216f, 1752f, true),
        new(1864f, 1992f, true),
        new(2280f, 1992f, false),
        new(1744f, 2240f, false),
        new(2216f, 2240f, true)
    ];

    // Tree site 16 marks the lower cell of the paired 4+16 green tree.
    // Flower 2, rounded foliage 5, and small trees 27/28 use one cell each.
    internal static readonly DecorationSite[] Decorations =
    [
        new(1680f, 1680f, 5), new(1712f, 1680f, 28),
        new(2384f, 1680f, 27), new(2416f, 1680f, 28),
        new(1680f, 2384f, 27), new(1712f, 2384f, 5),
        new(2384f, 2384f, 28), new(2416f, 2384f, 27),
        new(1888f, 1784f, 5), new(2176f, 1784f, 5),
        new(1888f, 2272f, 5), new(2176f, 2272f, 5),
        new(1824f, 1960f, 16), new(2224f, 1960f, 16),
        new(1824f, 2192f, 16), new(2224f, 2192f, 16),
        new(1952f, 1888f, 2), new(2144f, 1888f, 2),
        new(1952f, 2208f, 2), new(2144f, 2208f, 2)
    ];

    private static readonly Vector2[] stressPositions = CreateStressPositions();

    internal static IReadOnlyList<Vector2> StressPositions => stressPositions;

    private static Vector2[] CreateStressPositions()
    {
        var positions = new List<Vector2>(14_400);
        for (int row = 0; row < 120; row++)
        {
            for (int column = 0; column < 120; column++)
            {
                float x = 128f + column * StressPitch;
                float y = 128f + row * StressPitch;
                // Keep the authored village, player spawn and approaches playable
                // in every stress preset, including the collidable one.
                if ((x >= 1664f && x < 2432f && y >= 1664f && y < 2432f) ||
                    (x >= 2480f && x < 2640f && y >= 1968f && y < 2128f))
                {
                    continue;
                }

                positions.Add(new Vector2(x, y));
            }
        }

        // An explicit PRNG keeps count prefixes distributed over the same
        // world rather than filling one corner before the next count tier.
        uint state = 0x5EED2026u;
        for (int i = positions.Count - 1; i > 0; i--)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            int other = (int)(state % (uint)(i + 1));
            (positions[i], positions[other]) = (positions[other], positions[i]);
        }

        return positions.Take(MaximumStressCount).ToArray();
    }
}

internal readonly record struct HouseSite(float X, float Y, bool RedRoof);

internal readonly record struct DecorationSite(float X, float Y, int TileIndex);
