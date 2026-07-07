using System.Reflection;
using Cerneala.UI.Animation;
using Cerneala.UI.Core;

namespace Cerneala.Tests.UI.Animation;

[Trait("TestType", "Characterization")]
public sealed class LegacyAnimationCompatibilityTests
{
    [Fact]
    public void AnimationSourceOutranksStyleSources()
    {
        UiProperty<float> property = RegisterFloat();
        UiObject target = new();
        target.SetValue(property, 1, UiPropertyValueSource.StyleBase);
        target.SetValue(property, 2, UiPropertyValueSource.StyleVisualState);
        AnimationScheduler scheduler = new();

        scheduler.Schedule(target, property, new Animation<float>(0, 10, TimeSpan.FromSeconds(1), Lerp));
        scheduler.Tick(TimeSpan.FromMilliseconds(500));

        Assert.Equal(5, target.GetValue(property));
        Assert.Equal(UiPropertyValueSource.Animation, target.GetValueSource(property));
    }

    [Fact]
    public void LocalSourceMasksAnimationSource()
    {
        UiProperty<float> property = RegisterFloat();
        UiObject target = new();
        target.SetValue(property, 42);
        AnimationScheduler scheduler = new();

        scheduler.Schedule(target, property, new Animation<float>(0, 10, TimeSpan.FromSeconds(1), Lerp));
        scheduler.Tick(TimeSpan.FromMilliseconds(500));

        Assert.Equal(42, target.GetValue(property));
        Assert.Equal(UiPropertyValueSource.Local, target.GetValueSource(property));

        target.ClearValue(property);

        Assert.Equal(5, target.GetValue(property));
        Assert.Equal(UiPropertyValueSource.Animation, target.GetValueSource(property));
    }

    [Fact]
    public void CompletingOldAnimationClearsAnimationSource()
    {
        UiProperty<float> property = RegisterFloat();
        UiObject target = new();
        target.SetValue(property, 3, UiPropertyValueSource.StyleBase);
        AnimationScheduler scheduler = new();

        scheduler.Schedule(target, property, new Animation<float>(0, 10, TimeSpan.FromSeconds(1), Lerp));
        AnimationTickResult result = scheduler.Tick(TimeSpan.FromSeconds(1));

        Assert.Equal(3, target.GetValue(property));
        Assert.Equal(UiPropertyValueSource.StyleBase, target.GetValueSource(property));
        Assert.Equal(1, result.Completed);
        Assert.False(result.HasPendingWork);
    }

    [Fact]
    public void ReplacingSameTargetPropertyStopsAndClearsOldEntry()
    {
        UiProperty<float> property = RegisterFloat();
        UiObject target = new();
        target.SetValue(property, 3, UiPropertyValueSource.StyleBase);
        AnimationScheduler scheduler = new();
        AnimationScheduler.AnimationHandle oldHandle = scheduler.Schedule(
            target,
            property,
            new Animation<float>(0, 10, TimeSpan.FromSeconds(1), Lerp));
        scheduler.Tick(TimeSpan.FromMilliseconds(500));

        scheduler.Schedule(target, property, new Animation<float>(100, 200, TimeSpan.FromSeconds(1), Lerp));

        Assert.Equal(3, target.GetValue(property));
        Assert.Equal(UiPropertyValueSource.StyleBase, target.GetValueSource(property));

        oldHandle.Stop();
        AnimationTickResult result = scheduler.Tick(TimeSpan.FromMilliseconds(100));

        Assert.Equal(110, target.GetValue(property));
        Assert.Equal(UiPropertyValueSource.Animation, target.GetValueSource(property));
        Assert.Equal(1, result.Ticked);
        Assert.Equal(0, result.Completed);
        Assert.True(result.HasPendingWork);
    }

    [Fact]
    public void StoppingHandleIsIdempotent()
    {
        UiProperty<float> property = RegisterFloat();
        UiObject target = new();
        AnimationScheduler scheduler = new();
        AnimationScheduler.AnimationHandle handle = scheduler.Schedule(
            target,
            property,
            new Animation<float>(0, 10, TimeSpan.FromSeconds(1), Lerp));
        scheduler.Tick(TimeSpan.FromMilliseconds(500));

        handle.Stop();
        handle.Stop();
        AnimationTickResult stoppedResult = scheduler.Tick(TimeSpan.FromMilliseconds(1));
        handle.Stop();
        AnimationTickResult idleResult = scheduler.Tick(TimeSpan.FromMilliseconds(1));

        Assert.Equal(1, stoppedResult.Completed);
        Assert.False(stoppedResult.HasPendingWork);
        Assert.Equal(0, idleResult.Ticked);
        Assert.Equal(0, idleResult.Completed);
        Assert.False(idleResult.HasPendingWork);
        Assert.False(scheduler.HasActiveAnimations);
    }

    [Fact]
    public void StoryboardCannotExpressSequencing()
    {
        MemberInfo[] publicContract = PublicInstanceContractMembers(typeof(Storyboard));
        string[] sequencingConcepts =
        [
            "Sequence",
            "Timeline",
            "Delay",
            "Keyframe",
            "Step",
            "Begin",
            "After",
            "Before",
            "Duration",
            "TimeOffset",
            "Offset",
            "StartTime",
            "EndTime",
            "Then",
            "Wait"
        ];

        Assert.DoesNotContain(publicContract, member => MemberMentionsAnyConcept(member, sequencingConcepts));
    }

    [Fact]
    public void AnimationSchedulerHasNoRootOrFrameIntegration()
    {
        Assert.All(
            typeof(AnimationScheduler).GetConstructors(),
            constructor => Assert.Empty(constructor.GetParameters()));

        MemberInfo[] publicContract = PublicInstanceContractMembers(typeof(AnimationScheduler));

        Assert.DoesNotContain(publicContract, member => MemberMentionsAnyConcept(member, ["Root", "Frame"]));
    }

    [Fact]
    public void AnimationSchedulerHasNoDiagnosticsBeyondTickCounts()
    {
        string[] resultProperties = typeof(AnimationTickResult)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .Order()
            .ToArray();

        Assert.Equal(["Completed", "HasPendingWork", "Ticked"], resultProperties);
        MemberInfo[] publicContract = PublicInstanceContractMembers(typeof(AnimationScheduler));
        string[] diagnosticsConcepts =
        [
            "Diagnostic",
            "Diagnostics",
            "Trace",
            "Telemetry",
            "Event",
            "Metric",
            "Metrics",
            "Stat",
            "Stats",
            "Report",
            "Snapshot",
            "Observer"
        ];

        Assert.DoesNotContain(
            publicContract,
            member => MemberMentionsAnyConcept(member, diagnosticsConcepts));
    }

    private static MemberInfo[] PublicInstanceContractMembers(Type type)
    {
        return type.GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Where(member => member.DeclaringType != typeof(object))
            .ToArray();
    }

    private static bool MemberMentionsAnyConcept(MemberInfo member, IReadOnlyCollection<string> concepts)
    {
        return TextMentionsAnyConcept(member.Name, concepts)
            || member switch
            {
                MethodInfo method => TypeMentionsAnyConcept(method.ReturnType, concepts)
                    || method.GetParameters().Any(parameter =>
                        TextMentionsAnyConcept(parameter.Name ?? string.Empty, concepts)
                        || TypeMentionsAnyConcept(parameter.ParameterType, concepts)),
                PropertyInfo property => TypeMentionsAnyConcept(property.PropertyType, concepts),
                ConstructorInfo constructor => constructor.GetParameters().Any(parameter =>
                    TextMentionsAnyConcept(parameter.Name ?? string.Empty, concepts)
                    || TypeMentionsAnyConcept(parameter.ParameterType, concepts)),
                _ => false
            };
    }

    private static bool TypeMentionsAnyConcept(Type type, IReadOnlyCollection<string> concepts)
    {
        return TextMentionsAnyConcept(type.Name, concepts)
            || (type.FullName is not null && TextMentionsAnyConcept(type.FullName, concepts))
            || type.GetGenericArguments().Any(argument => TypeMentionsAnyConcept(argument, concepts));
    }

    private static bool TextMentionsAnyConcept(string text, IReadOnlyCollection<string> concepts)
    {
        return concepts.Any(concept => text.Contains(concept, StringComparison.OrdinalIgnoreCase));
    }

    private static UiProperty<float> RegisterFloat()
    {
        return UiProperty<float>.Register(
            $"{nameof(LegacyAnimationCompatibilityTests)}_{Guid.NewGuid():N}",
            typeof(LegacyAnimationCompatibilityTests),
            new UiPropertyMetadata<float>(0));
    }

    private static float Lerp(float from, float to, float progress)
    {
        return from + ((to - from) * progress);
    }
}
