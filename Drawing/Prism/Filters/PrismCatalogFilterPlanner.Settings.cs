using System.Collections.Immutable;
using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism.Filters;

internal static partial class PrismCatalogFilterPlanner
{
    private static Vector4 AccentedEdgesGaussianSettings(
        PrismFilterParameterReader values,
        float deviceScale) =>
        XDogGaussianSettings(
            MathF.Max(values.Number("EdgeWidth"), 0) *
            deviceScale *
                0.5f);

    private static Vector4 GlowingEdgesSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float edgeRadius = Math.Clamp(
            MathF.Round(
                MathF.Max(values.Number("EdgeWidth"), 1) *
                deviceScale),
            1,
            8);
        float smoothness = Math.Clamp(
            values.Number("Smoothness"),
            0,
            15);
        float sigma = Math.Clamp(
            (0.65f + (smoothness * 0.18f)) * deviceScale,
            0.5f,
            4);
        float gaussianRadius = Math.Clamp(
            MathF.Ceiling(sigma * 2),
            1,
            8);
        float haloMix = 0.35f + (0.65f * (smoothness / 15));
        return new Vector4(
            edgeRadius,
            sigma,
            gaussianRadius,
            haloMix);
    }

    private static Vector4 ChromeSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float detail = Math.Clamp(values.Number("Detail"), 0, 10);
        float smoothness = Math.Clamp(
            values.Number("Smoothness"),
            0,
            15);
        float sigma = Math.Clamp(
            (0.55f + (smoothness * 0.18f)) * deviceScale,
            0.5f,
            4);
        float radius = Math.Clamp(
            MathF.Ceiling(sigma * 2),
            1,
            8);
        float detailGain = 1 + (detail * 0.75f);
        float reflectionWidth =
            0.035f + ((smoothness / 15) * 0.08f);
        return new Vector4(
            sigma,
            radius,
            detailGain,
            reflectionWidth);
    }

    private static Vector4 NotePaperSettings(
        PrismFilterParameterReader values) =>
        new(
            Math.Clamp(values.Number("ImageBalance"), 0, 50) / 50,
            Math.Clamp(values.Number("Graininess"), 0, 20) / 20,
            Math.Clamp(values.Number("Relief"), 0, 50) / 50,
            0);

    private static Vector4 PlasterSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float balance = Math.Clamp(
            values.Number("ImageBalance"),
            0,
            50) / 50;
        float smoothness = Math.Clamp(
            values.Number("Smoothness"),
            0,
            15) / 15;
        float radius = Math.Clamp(
            MathF.Ceiling(
                (1 + (smoothness * 7)) *
                MathF.Sqrt(deviceScale)),
            1,
            12);
        float epsilon = float.Lerp(
            0.0004f,
            0.014f,
            smoothness * smoothness);
        float normalStrength = float.Lerp(7, 3.5f, smoothness);
        return new Vector4(
            balance,
            radius,
            epsilon,
            normalStrength);
    }

    private static Vector4 PhotocopyXDogSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float detail = Math.Clamp(values.Number("Detail"), 0, 24);
        float sigma = Math.Clamp(
            (1.2f / MathF.Sqrt(1 + (detail * 0.22f))) * deviceScale,
            0.5f,
            3.75f);
        float extendedSigma = Math.Clamp(
            sigma * 1.8f,
            sigma + 0.25f,
            4);
        float radius = Math.Clamp(
            MathF.Ceiling(extendedSigma * 3),
            1,
            12);
        float epsilon = Math.Clamp(
            values.Number("Darkness") / 40,
            0,
            1);
        return new Vector4(sigma, extendedSigma, radius, epsilon);
    }

    private static Vector4 StampXDogSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float smoothness = Math.Clamp(
            values.Number("Smoothness"),
            1,
            50);
        float normalizedSmoothness = (smoothness - 1) / 49;
        float sigma = Math.Clamp(
            float.Lerp(0.5f, 3.75f, normalizedSmoothness) * deviceScale,
            0.5f,
            3.75f);
        float extendedSigma = Math.Clamp(
            sigma * 1.8f,
            sigma + 0.25f,
            4);
        float radius = Math.Clamp(
            MathF.Ceiling(extendedSigma * 3),
            1,
            12);
        float epsilon = Math.Clamp(
            values.Number("LightDarkBalance") / 50,
            0,
            1);
        return new Vector4(sigma, extendedSigma, radius, epsilon);
    }

    private static Vector4 TornEdgesXDogSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float smoothness = TornEdgesNormalizedSmoothness(values);
        float sigma = Math.Clamp(
            float.Lerp(0.65f, 2.6f, smoothness) * deviceScale,
            0.5f,
            3.75f);
        float extendedSigma = Math.Clamp(
            sigma * 1.6f,
            sigma + 0.25f,
            4);
        float radius = Math.Clamp(
            MathF.Ceiling(extendedSigma * 3),
            1,
            12);
        float threshold = Math.Clamp(
            values.Number("ImageBalance") / 50,
            0,
            1);
        return new Vector4(sigma, extendedSigma, radius, threshold);
    }

    private static Vector4 TornEdgesNoiseSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float smoothness = TornEdgesNormalizedSmoothness(values);
        float contrast = Math.Clamp(
            (values.Number("Contrast") - 1) / 24,
            0,
            1);
        float sharpen = float.Lerp(8, 48, contrast);
        float amplitude = float.Lerp(0.16f, 0.035f, smoothness);
        float frequency = Math.Clamp(
            float.Lerp(0.18f, 0.055f, smoothness) / deviceScale,
            0.01f,
            0.5f);
        float transitionWidth = float.Lerp(0.2f, 0.07f, contrast);
        return new Vector4(
            sharpen,
            amplitude,
            frequency,
            transitionWidth);
    }

    private static float TornEdgesNormalizedSmoothness(
        PrismFilterParameterReader values) =>
        Math.Clamp(
            (values.Number("Smoothness") - 1) / 14,
            0,
            1);

    private static Vector4 ReticulationSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float density = Math.Clamp(
            values.Number("Density"),
            0,
            50) / 50;
        float cellSize = Math.Clamp(
            float.Lerp(18, 3, density) * deviceScale,
            2,
            256);
        float foregroundLevel = Math.Clamp(
            values.Number("ForegroundLevel"),
            0,
            50) / 50;
        float backgroundLevel = Math.Clamp(
            values.Number("BackgroundLevel"),
            0,
            50) / 50;
        return new Vector4(
            cellSize,
            foregroundLevel,
            backgroundLevel,
            0);
    }

    private static Vector4 CraquelureSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float cellSize = Math.Clamp(
            MathF.Abs(values.Number("CrackSpacing")) * deviceScale,
            2,
            256);
        float depth = Math.Clamp(values.Number("CrackDepth"), 0, 10) / 10;
        float brightness = Math.Clamp(
            values.Number("CrackBrightness"),
            0,
            10) / 10;
        float crackWidth = float.Lerp(0.035f, 0.16f, depth);
        return new Vector4(cellSize, crackWidth, depth, brightness);
    }

    private static void GrainSettings(
        PrismFilterParameterReader values,
        float deviceScale,
        out Vector4 model,
        out Vector4 shape)
    {
        int type = values.SymbolCode(
            "Type",
            ("Regular", 0),
            ("Soft", 1),
            ("Sprinkles", 2),
            ("Clumped", 3),
            ("Contrasty", 4),
            ("Enlarged", 5),
            ("Stippled", 6),
            ("Horizontal", 7),
            ("Vertical", 8),
            ("Speckle", 9));
        (float radiusX, float radiusY, float softness, float gain) =
            type switch
            {
                0 => (1.15f, 1.15f, 0.2f, 1f),
                1 => (1.4f, 1.4f, 0.45f, 0.75f),
                2 => (0.65f, 0.65f, 0.1f, 1.2f),
                3 => (1.8f, 1.8f, 0.18f, 1.1f),
                4 => (1.05f, 1.05f, 0.08f, 1.35f),
                5 => (2.4f, 2.4f, 0.22f, 1f),
                6 => (0.75f, 0.75f, 0.05f, 1.1f),
                7 => (1.8f, 0.7f, 0.12f, 1f),
                8 => (0.7f, 1.8f, 0.12f, 1f),
                _ => (0.45f, 0.45f, 0.04f, 1.25f)
            };
        radiusX *= deviceScale;
        radiusY *= deviceScale;
        float cellSize = Math.Clamp(
            MathF.Max(2 * deviceScale, 2.5f * MathF.Max(radiusX, radiusY)),
            1,
            256);
        model = new Vector4(
            Math.Clamp(values.Number("Intensity"), 0, 100) / 100,
            Math.Clamp(values.Number("Contrast"), 0, 100) / 100,
            type,
            cellSize);
        shape = new Vector4(radiusX, radiusY, softness, gain);
    }

    private static Vector4 ChalkCharcoalGaussianSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float charcoalSigma = Math.Clamp(
            (0.5f +
                (MathF.Max(values.Number("CharcoalArea"), 0) * 0.25f)) *
            deviceScale,
            0.5f,
            4);
        float requestedChalkSigma = Math.Clamp(
            (0.5f +
                (MathF.Max(values.Number("ChalkArea"), 0) * 0.25f)) *
            deviceScale,
            0.5f,
            6.4f);
        float chalkSigma = Math.Clamp(
            MathF.Max(requestedChalkSigma, charcoalSigma + 0.25f),
            charcoalSigma,
            6.4f);
        float charcoalRadius = Math.Clamp(
            MathF.Ceiling(charcoalSigma * 2),
            1,
            8);
        float chalkRadius = Math.Clamp(
            MathF.Ceiling(chalkSigma * 2),
            charcoalRadius,
            8);
        return new Vector4(
            charcoalSigma,
            chalkSigma,
            charcoalRadius,
            chalkRadius);
    }

    private static Vector4 CharcoalFdogSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float thickness = Math.Clamp(
            MathF.Abs(values.Number("CharcoalThickness")),
            0.25f,
            4);
        float detail = Math.Clamp(values.Number("Detail"), 0, 10);
        float sigma = Math.Clamp(
            (0.55f + (thickness * 0.45f)) * deviceScale,
            0.5f,
            4);
        float extendedSigma = Math.Clamp(
            sigma * 1.6f,
            sigma + 0.25f,
            6.4f);
        float normalRadius = Math.Clamp(
            MathF.Ceiling(extendedSigma * 2),
            2,
            8);
        float flowRadius = Math.Clamp(
            MathF.Round((3 + (detail * 0.6f)) * deviceScale),
            3,
            8);
        return new Vector4(
            sigma,
            extendedSigma,
            normalRadius,
            flowRadius);
    }

    private static Vector4 CharcoalEtfSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float detail = Math.Clamp(values.Number("Detail"), 0, 10);
        float radius = Math.Clamp(
            MathF.Round((2 + (detail * 0.2f)) * deviceScale),
            2,
            4);
        float refinementCount = 1 + MathF.Round(detail / 5);
        float thresholdSlope = 18 + (detail * 2.4f);
        return new Vector4(radius, refinementCount, 0.98f, thresholdSlope);
    }

    private static Vector4 ConteCrayonXDogSettings(float deviceScale)
    {
        float sigma = Math.Clamp(0.85f * deviceScale, 0.5f, 4);
        float extendedSigma = Math.Clamp(
            sigma * 1.6f,
            sigma + 0.25f,
            6.4f);
        float normalRadius = Math.Clamp(
            MathF.Ceiling(extendedSigma * 2),
            2,
            8);
        float flowRadius = Math.Clamp(
            MathF.Round(5 * MathF.Sqrt(deviceScale)),
            3,
            8);
        return new Vector4(
            sigma,
            extendedSigma,
            normalRadius,
            flowRadius);
    }

    private static Vector4 GraphicPenXDogSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float strokeLength = Math.Clamp(
            MathF.Abs(values.Number("StrokeLength")) * deviceScale,
            1,
            96);
        float sigma = Math.Clamp(
            0.65f + (MathF.Min(strokeLength, 32) * 0.025f),
            0.5f,
            4);
        float extendedSigma = Math.Clamp(
            sigma * 1.6f,
            sigma + 0.25f,
            6.4f);
        float normalRadius = Math.Clamp(
            MathF.Ceiling(extendedSigma * 2),
            2,
            8);
        float flowRadius = Math.Clamp(
            MathF.Round(4 + MathF.Sqrt(strokeLength) * 0.45f),
            3,
            8);
        return new Vector4(
            sigma,
            extendedSigma,
            normalRadius,
            flowRadius);
    }

    private static Vector4 GraphicPenEtfSettings() =>
        new(3, 1, 0.98f, 0);

    private static Vector3 MosaicTilesSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float tileSize = Math.Clamp(
            values.Number("TileSize") * deviceScale,
            1,
            16384);
        float groutWidth = Math.Clamp(
            values.Number("GroutWidth") * deviceScale,
            0,
            tileSize);
        float lightenGrout = Math.Clamp(
            values.Number("LightenGrout") / 10,
            0,
            1);
        return new Vector3(tileSize, groutWidth, lightenGrout);
    }

    private static Vector2 PatchworkSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float squareSize = Math.Clamp(
            values.Number("SquareSize") * deviceScale,
            1,
            16384);
        float relief = Math.Clamp(
            values.Number("Relief") / 50,
            0,
            1);
        return new Vector2(squareSize, relief);
    }

    private static Vector3 StainedGlassSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float cellSize = Math.Clamp(
            MathF.Abs(values.Number("CellSize")) * deviceScale,
            2,
            16384);
        float borderThickness = Math.Clamp(
            MathF.Abs(values.Number("BorderThickness")) * deviceScale,
            0,
            1024);
        float lightIntensity = Math.Clamp(
            values.Number("LightIntensity"),
            0,
            10);
        return new Vector3(
            cellSize,
            borderThickness,
            lightIntensity);
    }

    private static Vector4 InkOutlinesGaussianSettings(
        PrismFilterParameterReader values,
        float deviceScale) =>
        XDogGaussianSettings(
            MathF.Max(values.Number("StrokeLength"), 0) *
                deviceScale *
                0.25f);

    private static Vector4 SumiEGaussianSettings(
        PrismFilterParameterReader values,
        float deviceScale) =>
        XDogGaussianSettings(
            (0.65f +
                (MathF.Max(values.Number("StrokeWidth"), 0) * 0.08f)) *
            deviceScale);

    private static Vector4 XDogGaussianSettings(float requestedSigma)
    {
        float sigma = Math.Clamp(requestedSigma, 0.5f, 4);
        float extendedSigma = MathF.Min(sigma * 1.6f, 6.4f);
        float radius = Math.Clamp(
            MathF.Ceiling(extendedSigma * 2),
            1,
            8);
        return new Vector4(
            sigma,
            extendedSigma,
            Math.Clamp(MathF.Ceiling(sigma * 2), 1, 8),
            radius);
    }
}
