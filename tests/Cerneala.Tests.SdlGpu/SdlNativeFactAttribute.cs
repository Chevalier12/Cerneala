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

// Physical audio delivery needs a host with a default playback endpoint, which
// hosted CI runners lack; the skip is reported, never counted as a pass.
[AttributeUsage(AttributeTargets.Method)]
internal sealed class TimbreAudioDeviceFactAttribute : FactAttribute
{
    public TimbreAudioDeviceFactAttribute()
    {
        Skip = SdlNativeFactAttribute.NativeSkipReason ?? (string.Equals(
            Environment.GetEnvironmentVariable("CERNEALA_TIMBRE_AUDIO_DEVICE"),
            "1",
            StringComparison.Ordinal)
            ? null
            : "Set CERNEALA_TIMBRE_AUDIO_DEVICE=1 on a Windows host with a default playback device.");
    }
}
