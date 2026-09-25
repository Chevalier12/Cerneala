namespace Cerneala.Tests.SceneVillage;

internal sealed class VillageNativeFactAttribute : FactAttribute
{
    public VillageNativeFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("CERNEALA_SDL_NATIVE_TESTS") != "1")
        {
            Skip = "Set CERNEALA_SDL_NATIVE_TESTS=1 on a configured Windows SDL runner.";
        }
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class VillageNativeTestCollection
{
    public const string Name = "SceneVillage SDL Native";
}
