using System.Globalization;

namespace Cerneala.StressTests;

// The stress tests take minutes and open native windows, so they never run by default (CI runs the whole solution):
// they skip unless CERNEALA_STRESS_TESTS=1, and the SDL_GPU window tests also need CERNEALA_SDL_NATIVE_TESTS=1.
internal static class StressSettings
{
    internal const string CollectionName = "RenderSurface2D stress";

    internal static string? SkipReason => IsSet("CERNEALA_STRESS_TESTS")
        ? null
        : "Set CERNEALA_STRESS_TESTS=1 to run the RenderSurface2D stress tests.";

    internal static string? SdlSkipReason => SkipReason ?? (IsSet("CERNEALA_SDL_NATIVE_TESTS")
        ? null
        : "Set CERNEALA_SDL_NATIVE_TESTS=1 (with CERNEALA_STRESS_TESTS=1) on a configured native runner.");

    // CERNEALA_STRESS_N ("1000,10000") overrides the default sizes of every scenario.
    internal static int[] Sizes(params int[] defaults) =>
        Environment.GetEnvironmentVariable("CERNEALA_STRESS_N") is { Length: > 0 } text
            ? text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(size => int.Parse(size, CultureInfo.InvariantCulture)).ToArray()
            : defaults;

    private static bool IsSet(string name) =>
        string.Equals(Environment.GetEnvironmentVariable(name), "1", StringComparison.Ordinal);
}

[AttributeUsage(AttributeTargets.Method)]
internal sealed class StressFactAttribute : FactAttribute
{
    public StressFactAttribute() => Skip = StressSettings.SkipReason;
}

[AttributeUsage(AttributeTargets.Method)]
internal sealed class StressSdlFactAttribute : FactAttribute
{
    public StressSdlFactAttribute() => Skip = StressSettings.SdlSkipReason;
}

// One collection for every stress test: they measure frame time, so they must not run beside each other.
[CollectionDefinition(StressSettings.CollectionName, DisableParallelization = true)]
public sealed class StressCollection { }
