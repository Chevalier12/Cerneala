using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;

namespace Cerneala.Drawing.Prism.Filters;

internal static partial class PrismResamplingMath
{
    internal static Vector2 MapWave(
        PrismResamplingPlan plan,
        Vector2 uv,
        int width,
        int height)
    {
        WaveJacobian(
            plan,
            uv,
            width,
            height,
            out Vector2 mapped,
            out _,
            out _);
        return mapped;
    }

    internal static Vector4 SampleWaveFeline(
        PrismResamplingPlan plan,
        Vector4[] source,
        int width,
        int height,
        Vector2 uv,
        int edgeMode,
        Vector4 fill)
    {
        WaveJacobian(
            plan,
            uv,
            width,
            height,
            out Vector2 mapped,
            out Vector2 derivativeX,
            out Vector2 derivativeY);
        return SampleFeline(
            source,
            width,
            height,
            mapped,
            derivativeX,
            derivativeY,
            edgeMode,
            fill);
    }

    private static void WaveJacobian(
        PrismResamplingPlan plan,
        Vector2 uv,
        int width,
        int height,
        out Vector2 mapped,
        out Vector2 derivativeX,
        out Vector2 derivativeY)
    {
        uint seed = Seed(
            plan.Options2.Y,
            plan.Options2.Z);
        int generators = Math.Clamp(
            (int)MathF.Round(plan.Options0.X),
            1,
            PrismResamplingPlanner.MaximumWaveGenerators);
        int kind = (int)plan.Options0.W;
        Vector2 displacement = Vector2.Zero;
        Vector2 displacementDerivativeX = Vector2.Zero;
        Vector2 displacementDerivativeY = Vector2.Zero;
        Vector2 pixelPosition = uv * new Vector2(
            width,
            height);
        for (int generator = 0;
            generator < generators;
            generator++)
        {
            float directionAngle =
                WaveHash(seed, generator, 0) *
                MathF.Tau;
            Vector2 direction = new(
                MathF.Cos(directionAngle),
                MathF.Sin(directionAngle));
            float wavelength = float.Lerp(
                plan.Options0.Y,
                plan.Options0.Z,
                WaveHash(seed, generator, 1));
            float amplitude = float.Lerp(
                plan.Options1.X,
                plan.Options1.Y,
                WaveHash(seed, generator, 2));
            WaveSample wave = BandLimitedWave(
                (Vector2.Dot(
                    pixelPosition,
                    direction) /
                    wavelength) +
                    WaveHash(seed, generator, 3),
                direction / wavelength,
                kind);
            Vector2 displacementDirection =
                direction * amplitude;
            displacement +=
                displacementDirection *
                wave.Value;
            displacementDerivativeX +=
                displacementDirection *
                wave.Derivative *
                direction.X /
                wavelength;
            displacementDerivativeY +=
                displacementDirection *
                wave.Derivative *
                direction.Y /
                wavelength;
        }

        float normalization =
            1 / MathF.Sqrt(generators);
        float scaleX = plan.Options1.Z * normalization;
        float scaleY = plan.Options1.W * normalization;
        mapped = uv + new Vector2(
            displacement.X * scaleX / width,
            displacement.Y * scaleY / height);
        derivativeX = new Vector2(
            1 + (displacementDerivativeX.X * scaleX),
            displacementDerivativeX.Y * scaleY);
        derivativeY = new Vector2(
            displacementDerivativeY.X * scaleX,
            1 + (displacementDerivativeY.Y * scaleY));
    }

    private static WaveSample BandLimitedWave(
        float phase,
        Vector2 phaseWidth,
        int kind)
    {
        float wrapped = phase - MathF.Floor(phase);
        float maximumWidth = MathF.Max(
            MathF.Abs(phaseWidth.X),
            MathF.Abs(phaseWidth.Y));
        if (kind == 0)
        {
            if (maximumWidth > 0.5f)
            {
                return default;
            }

            float attenuation =
                Sinc(MathF.PI * phaseWidth.X) *
                Sinc(MathF.PI * phaseWidth.Y);
            float angle = wrapped * MathF.Tau;
            return new WaveSample(
                MathF.Sin(angle) * attenuation,
                MathF.Tau *
                    MathF.Cos(angle) *
                    attenuation);
        }

        float value = 0;
        float derivative = 0;
        for (int term = 0; term < 8; term++)
        {
            int harmonic = (term * 2) + 1;
            float harmonicWidth =
                harmonic * maximumWidth;
            if (harmonicWidth > 0.5f)
            {
                break;
            }

            float attenuation =
                Sinc(
                    MathF.PI *
                    harmonic *
                    phaseWidth.X) *
                Sinc(
                    MathF.PI *
                    harmonic *
                    phaseWidth.Y);
            float angle =
                wrapped *
                harmonic *
                MathF.Tau;
            if (kind == 1)
            {
                float coefficient =
                    8 /
                    (MathF.PI *
                        MathF.PI *
                        harmonic *
                        harmonic);
                value +=
                    coefficient *
                    MathF.Cos(angle) *
                    attenuation;
                derivative -=
                    coefficient *
                    MathF.Tau *
                    harmonic *
                    MathF.Sin(angle) *
                    attenuation;
            }
            else
            {
                float coefficient =
                    -4 /
                    (MathF.PI * harmonic);
                value +=
                    coefficient *
                    MathF.Sin(angle) *
                    attenuation;
                derivative +=
                    coefficient *
                    MathF.Tau *
                    harmonic *
                    MathF.Cos(angle) *
                    attenuation;
            }
        }
        return new WaveSample(value, derivative);
    }

    private static float Sinc(float value) =>
        MathF.Abs(value) < 0.0001f
            ? 1
            : MathF.Sin(value) / value;

    private static float WaveHash(
        uint seed,
        int generator,
        int channel)
    {
        uint value =
            seed ^
            unchecked(
                ((uint)generator + 1) *
                0x9e3779b9u) ^
            unchecked(
                ((uint)channel + 1) *
                0x85ebca6bu);
        value ^= value >> 16;
        value = unchecked(value * 0x7feb352du);
        value ^= value >> 15;
        value = unchecked(value * 0x846ca68bu);
        value ^= value >> 16;
        return (value & 0x00ffffffu) /
            16777216f;
    }

    private readonly record struct WaveSample(
        float Value,
        float Derivative);
}
