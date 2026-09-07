namespace Cerneala.Tests.SdlGpu;

[AttributeUsage(AttributeTargets.Method)]
internal sealed class SdlNativeFactAttribute : FactAttribute
{
    public SdlNativeFactAttribute()
    {
        Skip = NativeSkipReason;
    }

    internal static string? NativeSkipReason => string.Equals(
        Environment.GetEnvironmentVariable("CERNEALA_SDL_NATIVE_TESTS"),
        "1",
        StringComparison.Ordinal)
        ? null
        : "Set CERNEALA_SDL_NATIVE_TESTS=1 on a configured native matrix runner.";
}

[AttributeUsage(AttributeTargets.Method)]
internal sealed class SdlNativeTheoryAttribute : TheoryAttribute
{
    public SdlNativeTheoryAttribute() => Skip = SdlNativeFactAttribute.NativeSkipReason;
}
