using TimbreConsumer;

namespace Cerneala.Tests.Timbre.Contracts;

// Executes the external consumer assembly, which compiles against public API
// only (no InternalsVisibleTo, markup, Aspect, generator, or SDL).
public sealed class ExternalConsumerTests
{
    [Fact]
    public async Task StandaloneConsumerRunsThePlanExampleFlow()
    {
        IReadOnlyList<string> log = await ConsumerScenarios.RunStandaloneAsync();

        Assert.Equal(
            [
                "overlap firstCanceled=True secondActive=True",
                "paused=Paused",
                "seek position=1.000 duration=2.000 loop=True state=Paused",
                "slot old=Canceled current=replacement",
                "stale cancel replacementActive=True",
                "slot cancel current=Canceled empty=True",
                "result=Canceled error=True"
            ],
            log);
    }

    [Fact]
    public void ElementAndSceneOwnersUseTheSamePath()
    {
        IReadOnlyList<string> log = ConsumerScenarios.RunElementAndSceneOwners();

        Assert.Equal(
            [
                "detached button=Canceled sceneActive=True",
                "reattached scope fresh=True",
                "runtime disposed scene=Canceled"
            ],
            log);
    }

    [Fact]
    public void PlanExampleCompilesInTheConsumerAssembly()
    {
        Func<Cerneala.Timbre.TimbreScope, Task> example = StandaloneUsage.RunPlanExampleAsync;
        Assert.NotNull(example);
        Assert.NotNull(StandaloneUsage.CreateConstantModifierClip());
    }
}
