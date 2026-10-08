using System.Reflection;
using System.Runtime.CompilerServices;
using Cerneala.Timbre;
using Cerneala.Timbre.Catalog;

namespace Cerneala.Tests.Timbre.Contracts;

public sealed class TimbreArchitectureTests
{
    private static readonly Assembly Core = typeof(TimbreRuntime).Assembly;

    [Fact]
    public void CoreTimbreTypesDoNotReferenceUiMarkupOrPlatformTypes()
    {
        Type[] timbreTypes = Core.GetTypes()
            .Where(type => type.Namespace?.StartsWith("Cerneala.Timbre", StringComparison.Ordinal) == true)
            .ToArray();
        Assert.NotEmpty(timbreTypes);

        List<string> violations = [];
        foreach (Type type in timbreTypes)
        {
            foreach (Type referenced in ReferencedTypes(type))
            {
                string? ns = referenced.Namespace;
                if (ns is not null &&
                    (ns.StartsWith("Cerneala.UI", StringComparison.Ordinal) ||
                     ns.StartsWith("Cerneala.Drawing", StringComparison.Ordinal) ||
                     ns.StartsWith("Cerneala.Language", StringComparison.Ordinal) ||
                     ns.StartsWith("Cerneala.SourceGen", StringComparison.Ordinal) ||
                     ns.Contains("Sdl", StringComparison.Ordinal)))
                {
                    violations.Add($"{type.FullName} -> {referenced.FullName}");
                }
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void DecoderAdaptersOnlyProducePcmAndOwnNoOutputMixerOrEffects()
    {
        // Decoder → PCM (TimbreReader) → Timbre DSP/mix → output: the adapters
        // never hold the output, the runtime, a playback, a feed or the DSP.
        Type[] decoding = Core.GetTypes()
            .Where(type => type.Namespace?.StartsWith("Cerneala.Timbre.Decoding", StringComparison.Ordinal) == true)
            .ToArray();
        Assert.NotEmpty(decoding);
        Type[] forbidden =
        [
            typeof(ITimbreOutput), typeof(ITimbreOutputClient), typeof(TimbreRuntime), typeof(TimbreScope), typeof(TimbrePlayback),
            typeof(TimbreModifier), Core.GetType("Cerneala.Timbre.Engine.TimbreFeed")!, Core.GetType("Cerneala.Timbre.Dsp.TimbreDspChain")!,
        ];

        List<string> violations = [];
        foreach (Type type in decoding)
        {
            foreach (Type referenced in ReferencedTypes(type))
            {
                if (forbidden.Any(candidate => candidate.IsAssignableFrom(referenced)))
                {
                    violations.Add($"{type.FullName} -> {referenced.FullName}");
                }
            }
        }

        Assert.Empty(violations);
        Assert.All(decoding, type => Assert.False(type.IsPublic || type.IsNestedPublic, $"{type.FullName} is public."));
    }

    [Fact]
    public void ExternalConsumerHasNoInternalsAccess()
    {
        string[] friends = Core.GetCustomAttributes<InternalsVisibleToAttribute>()
            .Select(attribute => attribute.AssemblyName)
            .ToArray();
        Assert.DoesNotContain("TimbreConsumer", friends);
        Assert.Equal("TimbreConsumer", typeof(TimbreConsumer.StandaloneUsage).Assembly.GetName().Name);
    }

    [Fact]
    public void CatalogMatchesApprovedRangesAndFormat()
    {
        Assert.Equal(48000, TimbreRuntime.SampleRate);
        Assert.Equal(2, TimbreRuntime.ChannelCount);
        Assert.Equal(8, TimbreCatalog.BytesPerFrame);
        Assert.Equal(TimbreCatalog.SampleRate / 100, TimbreCatalog.BlockFrames);
        Assert.Equal(TimbreCatalog.SampleRate * 40 / 1000, TimbreCatalog.OutputQueueBudgetFrames);

        AssertInput(TimbreCatalog.Volume, 0f, 1f, 1f);
        AssertInput(TimbreCatalog.LowPassCutoff, 20f, 20000f, 1200f);
        AssertInput(TimbreCatalog.DelayTime, 0.001f, 2f, 0.12f);
        AssertInput(TimbreCatalog.DelayFeedback, 0f, 0.95f, 0.20f);
        AssertInput(TimbreCatalog.DelayMix, 0f, 1f, 0.15f);

        Assert.Equal(["LowPass", "Delay"], TimbreCatalog.ModifierNames);
        Assert.Equal(["Cutoff"], TimbreCatalog.GetModifierInputs("LowPass")!.Select(input => input.Name));
        Assert.Equal(["Time", "Feedback", "Mix"], TimbreCatalog.GetModifierInputs("Delay")!.Select(input => input.Name));
        Assert.Null(TimbreCatalog.GetModifierInputs("Reverb"));

        TimbreRuntimeOptions defaults = new();
        Assert.Equal(64, defaults.MaxVoices);
        Assert.Equal(1L << 20, defaults.AutoPreloadMaxBytes);
        Assert.Equal(16L << 20, defaults.MaxPreloadBytes);
        Assert.Equal(64L << 20, defaults.MaxCacheBytes);
        Assert.Equal(long.MaxValue, defaults.StreamingMemoryLimit);
        Assert.Equal(TimeSpan.FromSeconds(30), defaults.DelayTailCap);
        Assert.Null(defaults.Output);
    }

    [Fact]
    public void CatalogRangeCheckRejectsNonFiniteAndOutOfRangeValues()
    {
        Assert.True(TimbreCatalog.DelayFeedback.Contains(0.95f));
        Assert.False(TimbreCatalog.DelayFeedback.Contains(0.951f));
        Assert.False(TimbreCatalog.Volume.Contains(float.NaN));
        Assert.False(TimbreCatalog.LowPassCutoff.Contains(float.PositiveInfinity));
        Assert.False(TimbreCatalog.LowPassCutoff.Contains(19.99f));
    }

    private static void AssertInput(TimbreCatalogInput input, float minimum, float maximum, float defaultValue)
    {
        Assert.Equal(minimum, input.Minimum);
        Assert.Equal(maximum, input.Maximum);
        Assert.Equal(defaultValue, input.DefaultValue);
        Assert.True(input.Contains(defaultValue));
    }

    private static IEnumerable<Type> ReferencedTypes(Type type)
    {
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        if (type.BaseType is not null) { yield return type.BaseType; }
        foreach (Type implemented in type.GetInterfaces()) { yield return implemented; }
        foreach (FieldInfo field in type.GetFields(all)) { yield return field.FieldType; }
        foreach (PropertyInfo property in type.GetProperties(all)) { yield return property.PropertyType; }
        foreach (MethodBase method in type.GetMethods(all).Cast<MethodBase>().Concat(type.GetConstructors(all)))
        {
            if (method is MethodInfo info) { yield return info.ReturnType; }
            foreach (ParameterInfo parameter in method.GetParameters()) { yield return parameter.ParameterType; }
        }
    }
}
