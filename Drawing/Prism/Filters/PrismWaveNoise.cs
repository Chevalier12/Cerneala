using System.Collections.Immutable;
using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal enum PrismWaveSpectrum
{
    White,
    Blue,
    Pink,
    Brown
}

internal readonly record struct PrismWaveNoiseTable(
    ImmutableArray<Vector4> PackedSamples,
    float Normalization);

internal static class PrismWaveNoise
{
    public const int FrequencySampleCount = 32;
    public const int TableSampleCount = FrequencySampleCount * 2;
    public const int PackedTableSampleCount = TableSampleCount / 2;
    public const int MaximumDirectionCount = 32;

    private const float Tau = MathF.PI * 2;
    private const float OutputGain = 0.2f;

    public static PrismWaveNoiseTable Precompute(
        int seed,
        Vector4 frequencyRange,
        PrismWaveSpectrum spectrum)
    {
        float minimum = Math.Clamp(
            MathF.Min(frequencyRange.X, frequencyRange.Y),
            1f / FrequencySampleCount,
            1);
        float maximum = Math.Clamp(
            MathF.Max(frequencyRange.X, frequencyRange.Y),
            minimum,
            1);
        int minimumHarmonic = Math.Clamp(
            (int)MathF.Ceiling(
                minimum * FrequencySampleCount),
            1,
            FrequencySampleCount);
        int maximumHarmonic = Math.Clamp(
            (int)MathF.Floor(
                maximum * FrequencySampleCount),
            1,
            FrequencySampleCount);
        if (maximumHarmonic < minimumHarmonic)
        {
            int nearest = Math.Clamp(
                (int)MathF.Round(
                    ((minimum + maximum) * 0.5f) *
                    FrequencySampleCount),
                1,
                FrequencySampleCount);
            minimumHarmonic = nearest;
            maximumHarmonic = nearest;
        }
        Vector2[] samples = new Vector2[TableSampleCount];

        for (int harmonic = minimumHarmonic;
            harmonic <= maximumHarmonic;
            harmonic++)
        {
            float frequency =
                harmonic / (float)FrequencySampleCount;


            float amplitude =
                SpectrumAmplitude(frequency, spectrum) *
                frequency;
            float phase = Tau * Hash01(
                harmonic,
                0x51,
                seed);
            for (int sample = 0;
                sample < TableSampleCount;
                sample++)
            {
                float angle =
                    (Tau * harmonic * sample /
                        TableSampleCount) +
                    phase;
                samples[sample] += new Vector2(
                    MathF.Cos(angle),
                    MathF.Sin(angle)) *
                    amplitude;
            }
        }

        double meanSquare = 0;
        foreach (Vector2 sample in samples)
        {
            meanSquare += sample.LengthSquared();
        }
        float normalization = (float)(
            1 / Math.Sqrt(meanSquare / TableSampleCount));

        ImmutableArray<Vector4>.Builder packed =
            ImmutableArray.CreateBuilder<Vector4>(
                PackedTableSampleCount);
        for (int sample = 0;
            sample < TableSampleCount;
            sample += 2)
        {
            Vector2 first = samples[sample];
            Vector2 second = samples[sample + 1];
            packed.Add(
                new Vector4(
                    first.X,
                    first.Y,
                    second.X,
                    second.Y));
        }
        return new PrismWaveNoiseTable(
            packed.MoveToImmutable(),
            normalization);
    }

    public static float Sample(
        PrismWaveNoiseTable table,
        Vector2 position,
        uint seed,
        int directionCount,
        float sliceThickness,
        Vector4 anisotropy)
    {
        if (table.PackedSamples.Length != PackedTableSampleCount)
        {
            throw new InvalidOperationException(
                "Wave Noise received an invalid precomputed table.");
        }

        directionCount = Math.Clamp(
            directionCount,
            4,
            MaximumDirectionCount);
        sliceThickness = Math.Clamp(
            sliceThickness,
            0.25f,
            16);
        float axis = anisotropy.X * (MathF.PI / 180);
        float isotropy = Math.Clamp(anisotropy.Y, 0, 1);
        Vector2 scaledPosition = position * sliceThickness;
        Vector2 sum = Vector2.Zero;
        float weightSum = 0;
        float weightSquareSum = 0;

        for (int directionIndex = 0;
            directionIndex < directionCount;
            directionIndex++)
        {
            float sectorWidth = MathF.PI / directionCount;
            float sectorStart = directionIndex * sectorWidth;
            float baseAngle =
                sectorStart +
                (sectorWidth * Hash01(
                    directionIndex,
                    0,
                    seed + 0x19u));
            Vector2 baseDirection = Direction(baseAngle);
            float projection = Vector2.Dot(
                scaledPosition,
                baseDirection);
            int cell = (int)MathF.Floor(projection);
            float center = cell + SlicePosition(
                directionIndex,
                cell,
                seed);
            int leftSlice;
            int rightSlice;
            float leftPosition;
            float rightPosition;
            if (projection < center)
            {
                leftSlice = cell - 1;
                rightSlice = cell;
                leftPosition = leftSlice + SlicePosition(
                    directionIndex,
                    leftSlice,
                    seed);
                rightPosition = center;
            }
            else
            {
                leftSlice = cell;
                rightSlice = cell + 1;
                leftPosition = center;
                rightPosition = rightSlice + SlicePosition(
                    directionIndex,
                    rightSlice,
                    seed);
            }

            float blend = SmoothStep(
                (projection - leftPosition) /
                MathF.Max(
                    rightPosition - leftPosition,
                    0.0001f));
            Vector2 left = SampleSlice(
                table.PackedSamples,
                scaledPosition,
                directionIndex,
                leftSlice,
                directionCount,
                sectorStart,
                sliceThickness,
                seed);
            Vector2 right = SampleSlice(
                table.PackedSamples,
                scaledPosition,
                directionIndex,
                rightSlice,
                directionCount,
                sectorStart,
                sliceThickness,
                seed);
            Vector2 wave = Vector2.Lerp(left, right, blend);
            float weight = DirectionWeight(
                baseAngle,
                axis,
                isotropy);
            sum += wave * weight;
            weightSum += weight;
            weightSquareSum += weight * weight;
        }

        float effectiveDirectionCount =
            weightSum /
            MathF.Sqrt(MathF.Max(weightSquareSum, 0.0001f));
        float normalized =
            (sum.X / MathF.Max(weightSum, 0.0001f)) *
            effectiveDirectionCount *
            table.Normalization;
        return Math.Clamp(
            0.5f + (normalized * OutputGain),
            0,
            1);
    }

    private static Vector2 SampleSlice(
        ImmutableArray<Vector4> table,
        Vector2 scaledPosition,
        int directionIndex,
        int slice,
        int directionCount,
        float sectorStart,
        float sliceThickness,
        uint seed)
    {
        float sectorWidth = MathF.PI / directionCount;
        float angle =
            sectorStart +
            (sectorWidth * Hash01(
                directionIndex,
                slice,
                seed + 0x6du));
        Vector2 direction = Direction(angle);
        float offset = Hash01(
            directionIndex,
            slice,
            seed + 0xb7u);
        float coordinate =
            (Vector2.Dot(scaledPosition, direction) + offset) /
            (sliceThickness * FrequencySampleCount);
        return SampleTable(table, coordinate);
    }

    private static Vector2 SampleTable(
        ImmutableArray<Vector4> table,
        float coordinate)
    {
        float wrapped = Fraction(coordinate) * TableSampleCount;
        int first = (int)MathF.Floor(wrapped);
        int second = (first + 1) % TableSampleCount;
        float blend = wrapped - first;
        return Vector2.Lerp(
            Unpack(table, first),
            Unpack(table, second),
            blend);
    }

    private static Vector2 Unpack(
        ImmutableArray<Vector4> table,
        int sample)
    {
        Vector4 packed = table[sample / 2];
        return (sample & 1) == 0
            ? new Vector2(packed.X, packed.Y)
            : new Vector2(packed.Z, packed.W);
    }

    private static float DirectionWeight(
        float angle,
        float axis,
        float isotropy)
    {
        if (isotropy >= 0.999f)
        {
            return 1;
        }

        float delta = MathF.Abs(
            Fraction((angle - axis) / MathF.PI + 0.5f) *
            MathF.PI -
            (MathF.PI * 0.5f));
        float sigma = 0.04f +
            (isotropy * ((MathF.PI * 0.5f) - 0.04f));
        float ratio = delta / sigma;
        return MathF.Exp(-0.5f * ratio * ratio) + 0.001f;
    }

    private static float SlicePosition(
        int direction,
        int slice,
        uint seed) =>
        0.3f +
        (0.4f * Hash01(direction, slice, seed + 0x31u));

    private static float SpectrumAmplitude(
        float frequency,
        PrismWaveSpectrum spectrum) =>
        spectrum switch
        {
            PrismWaveSpectrum.White => 1,
            PrismWaveSpectrum.Blue => MathF.Sqrt(frequency),
            PrismWaveSpectrum.Pink => 1 / MathF.Sqrt(frequency),
            PrismWaveSpectrum.Brown => 1 / frequency,
            _ => throw new ArgumentOutOfRangeException(nameof(spectrum))
        };

    private static Vector2 Direction(float angle) =>
        new(MathF.Cos(angle), MathF.Sin(angle));

    private static float SmoothStep(float value)
    {
        value = Math.Clamp(value, 0, 1);
        return value * value * (3 - (2 * value));
    }

    private static float Fraction(float value) =>
        value - MathF.Floor(value);

    private static float Hash01(int x, int y, int seed) =>
        Hash01(x, y, unchecked((uint)seed));

    private static float Hash01(int x, int y, uint seed)
    {
        uint hash = unchecked(
            ((uint)x * 0x9e3779b9u) ^
            ((uint)y * 0x85ebca6bu) ^
            (seed * 0xc2b2ae35u));
        hash ^= hash >> 16;
        hash *= 0x7feb352du;
        hash ^= hash >> 15;
        hash *= 0x846ca68bu;
        hash ^= hash >> 16;
        return (hash & 0x00ffffffu) / 16777216f;
    }
}
