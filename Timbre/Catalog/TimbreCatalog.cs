// Single owner of the Timbre value catalog. The file is dependency-free and
// uses only netstandard2.0 APIs, so build-time consumers link it unchanged
// (Cerneala.Language compiles it today; SourceGen consumes Language).
namespace Cerneala.Timbre.Catalog
{
    internal sealed class TimbreCatalogInput
    {
        internal TimbreCatalogInput(string owner, string name, string unit, float minimum, float maximum, float defaultValue)
        {
            Owner = owner;
            Name = name;
            Unit = unit;
            Minimum = minimum;
            Maximum = maximum;
            DefaultValue = defaultValue;
        }

        internal string Owner { get; }

        internal string Name { get; }

        internal string Unit { get; }

        internal float Minimum { get; }

        internal float Maximum { get; }

        internal float DefaultValue { get; }

        internal bool Contains(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= Minimum && value <= Maximum;
        }
    }

    internal static class TimbreCatalog
    {
        internal const string LowPassName = "LowPass";
        internal const string DelayName = "Delay";

        internal const int SampleRate = 48000;
        internal const int ChannelCount = 2;
        internal const int BytesPerSample = 4;
        internal const int BytesPerFrame = ChannelCount * BytesPerSample;

        // 10 ms processing blocks; the software output queue holds at most 40 ms.
        internal const int BlockFrames = 480;
        internal const int OutputQueueBudgetFrames = 1920;

        internal const long AutoPreloadMaxBytes = 1L * 1024 * 1024;
        internal const long MaxPreloadBytesPerClip = 16L * 1024 * 1024;
        internal const long MaxCacheBytes = 64L * 1024 * 1024;
        internal const int MaxVoices = 64;
        internal const long StreamingMemoryLimit = long.MaxValue;
        internal const double DelayTailCapSeconds = 30.0;

        // A Delay/LowPass tail ends once one full echo period (at least one
        // block) of the pre-volume chain output stays below this peak.
        internal const float TailSilenceThreshold = 1e-6f;

        internal static readonly TimbreCatalogInput Volume =
            new TimbreCatalogInput("TimbreClip", "Volume", "gain", 0f, 1f, 1f);

        internal static readonly TimbreCatalogInput LowPassCutoff =
            new TimbreCatalogInput(LowPassName, "Cutoff", "Hz", 20f, 20000f, 1200f);

        internal static readonly TimbreCatalogInput DelayTime =
            new TimbreCatalogInput(DelayName, "Time", "s", 0.001f, 2f, 0.12f);

        internal static readonly TimbreCatalogInput DelayFeedback =
            new TimbreCatalogInput(DelayName, "Feedback", "ratio", 0f, 0.95f, 0.20f);

        internal static readonly TimbreCatalogInput DelayMix =
            new TimbreCatalogInput(DelayName, "Mix", "ratio", 0f, 1f, 0.15f);

        internal static readonly TimbreCatalogInput[] LowPassInputs = { LowPassCutoff };

        internal static readonly TimbreCatalogInput[] DelayInputs = { DelayTime, DelayFeedback, DelayMix };

        internal static readonly string[] ModifierNames = { LowPassName, DelayName };

        internal static TimbreCatalogInput[]? GetModifierInputs(string modifierName)
        {
            switch (modifierName)
            {
                case LowPassName:
                    return LowPassInputs;
                case DelayName:
                    return DelayInputs;
                default:
                    return null;
            }
        }
    }
}
