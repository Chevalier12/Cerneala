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
        string[] declaredMethodsAndProperties = typeof(Storyboard)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(member => member.MemberType is MemberTypes.Method or MemberTypes.Property)
            .Select(member => member.Name)
            .Order()
            .ToArray();

        Assert.Equal(["Add", "get_Handles", "Handles", "Stop"], declaredMethodsAndProperties);
        Assert.All(
            typeof(Storyboard).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
            method => Assert.DoesNotContain(method.GetParameters(), parameter => parameter.ParameterType == typeof(TimeSpan)));
    }

    [Fact]
    public void AnimationSchedulerHasNoRootOrFrameIntegration()
    {
        Assert.All(
            typeof(AnimationScheduler).GetConstructors(),
            constructor => Assert.Empty(constructor.GetParameters()));

        MemberInfo[] declaredMembers = typeof(AnimationScheduler)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        Assert.DoesNotContain(declaredMembers, MemberMentionsRootOrFrame);
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
        Assert.DoesNotContain(
            typeof(AnimationScheduler).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
            member => member.Name.Contains("Diagnostic", StringComparison.Ordinal)
                || member.Name.Contains("Trace", StringComparison.Ordinal)
                || member.Name.Contains("Snapshot", StringComparison.Ordinal));
    }

    private static bool MemberMentionsRootOrFrame(MemberInfo member)
    {
        return member switch
        {
            MethodInfo method => TypeMentionsRootOrFrame(method.ReturnType)
                || method.GetParameters().Any(parameter => TypeMentionsRootOrFrame(parameter.ParameterType)),
            PropertyInfo property => TypeMentionsRootOrFrame(property.PropertyType),
            ConstructorInfo constructor => constructor.GetParameters().Any(parameter => TypeMentionsRootOrFrame(parameter.ParameterType)),
            _ => false
        };
    }

    private static bool TypeMentionsRootOrFrame(Type type)
    {
        return type.Name.Contains("Root", StringComparison.Ordinal)
            || type.Name.Contains("Frame", StringComparison.Ordinal);
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
