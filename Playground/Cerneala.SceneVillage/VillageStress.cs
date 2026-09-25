namespace Cerneala.SceneVillage;

internal enum StressPreset
{
    Static,
    Animated,
    Collision
}

internal readonly record struct StressItem(float X, float Y, StressPreset Preset);

internal static class VillageStress
{
    internal static readonly int[] CountOptions = [0, 100, 1_000, 10_000];

    internal static StressItem[] CreateItems(int count, StressPreset preset)
    {
        if (count < 0 || count > VillageLayout.MaximumStressCount)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        var items = new StressItem[count];
        for (int i = 0; i < count; i++)
        {
            var position = VillageLayout.StressPositions[i];
            items[i] = new StressItem(position.X, position.Y, preset);
        }

        return items;
    }

    internal static string Label(StressPreset preset) => preset switch
    {
        StressPreset.Static => "static decor",
        StressPreset.Animated => "animated decor",
        StressPreset.Collision => "collidable objects",
        _ => throw new ArgumentOutOfRangeException(nameof(preset))
    };
}
