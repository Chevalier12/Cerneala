namespace Cerneala.Tests.SdlGpu;

[AttributeUsage(AttributeTargets.Method)]
internal sealed class SdlNativeFactAttribute : FactAttribute
{
    public SdlNativeFactAttribute()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("CERNEALA_SDL_NATIVE_TESTS"),
                "1",
                StringComparison.Ordinal))
        {
            Skip = "Set CERNEALA_SDL_NATIVE_TESTS=1 on a configured native matrix runner.";
        }
    }
}
