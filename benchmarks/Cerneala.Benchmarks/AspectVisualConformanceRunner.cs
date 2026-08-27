using System.Text.Json;
using SkiaSharp;

namespace Cerneala.Benchmarks;

internal static class AspectVisualConformanceRunner
{
    private const double MaximumMeanAbsoluteError = 1.0;
    private const int MaximumPercentile99 = 10;
    private const int MaximumAbsoluteDelta = 49;

    public static void Run(string baselinePath, string actualPath, string reportPath)
    {
        string baselineFullPath = Path.GetFullPath(baselinePath);
        string actualFullPath = Path.GetFullPath(actualPath);
        string reportFullPath = Path.GetFullPath(reportPath);
        using SKBitmap baseline = SKBitmap.Decode(baselineFullPath) ??
            throw new InvalidOperationException($"Could not decode baseline image '{baselineFullPath}'.");
        using SKBitmap actual = SKBitmap.Decode(actualFullPath) ??
            throw new InvalidOperationException($"Could not decode actual image '{actualFullPath}'.");
        if (baseline.Width != actual.Width || baseline.Height != actual.Height)
        {
            throw new InvalidOperationException(
                $"Visual dimensions differ: baseline={baseline.Width}x{baseline.Height}, " +
                $"actual={actual.Width}x{actual.Height}.");
        }

        int[] deltas = new int[baseline.Width * baseline.Height * 4];
        int deltaIndex = 0;
        int changedPixels = 0;
        int minimumChangedX = baseline.Width;
        int minimumChangedY = baseline.Height;
        int maximumChangedX = -1;
        int maximumChangedY = -1;
        long totalDelta = 0;
        int maximumDelta = 0;
        Dictionary<ulong, int> colorChanges = [];
        using SKBitmap difference = new(baseline.Width, baseline.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        for (int y = 0; y < baseline.Height; y++)
        {
            for (int x = 0; x < baseline.Width; x++)
            {
                SKColor expected = baseline.GetPixel(x, y);
                SKColor observed = actual.GetPixel(x, y);
                int red = Math.Abs(expected.Red - observed.Red);
                int green = Math.Abs(expected.Green - observed.Green);
                int blue = Math.Abs(expected.Blue - observed.Blue);
                int alpha = Math.Abs(expected.Alpha - observed.Alpha);
                int pixelMaximum = Math.Max(Math.Max(red, green), Math.Max(blue, alpha));
                if (pixelMaximum > 0)
                {
                    changedPixels++;
                    minimumChangedX = Math.Min(minimumChangedX, x);
                    minimumChangedY = Math.Min(minimumChangedY, y);
                    maximumChangedX = Math.Max(maximumChangedX, x);
                    maximumChangedY = Math.Max(maximumChangedY, y);
                    ulong colorKey = Pack(expected) | ((ulong)Pack(observed) << 32);
                    colorChanges[colorKey] = colorChanges.GetValueOrDefault(colorKey) + 1;
                }

                deltas[deltaIndex++] = red;
                deltas[deltaIndex++] = green;
                deltas[deltaIndex++] = blue;
                deltas[deltaIndex++] = alpha;
                totalDelta += red + green + blue + alpha;
                maximumDelta = Math.Max(maximumDelta, pixelMaximum);
                difference.SetPixel(x, y, new SKColor((byte)pixelMaximum, 0, 0, 255));
            }
        }

        Array.Sort(deltas);
        int percentileIndex = Math.Clamp(
            (int)Math.Ceiling(0.99 * deltas.Length) - 1,
            0,
            deltas.Length - 1);
        double meanAbsoluteError = (double)totalDelta / deltas.Length;
        int percentile99 = deltas[percentileIndex];
        bool passes = meanAbsoluteError <= MaximumMeanAbsoluteError &&
            percentile99 <= MaximumPercentile99 &&
            maximumDelta <= MaximumAbsoluteDelta;
        AspectVisualDiffReport report = new(
            SchemaVersion: 1,
            Baseline: baselineFullPath,
            Actual: actualFullPath,
            Width: baseline.Width,
            Height: baseline.Height,
            ChangedPixels: changedPixels,
            ChangedPixelRatio: (double)changedPixels / (baseline.Width * baseline.Height),
            ChangedBounds: changedPixels == 0
                ? null
                : new AspectVisualBounds(minimumChangedX, minimumChangedY, maximumChangedX, maximumChangedY),
            MostFrequentColorChanges: colorChanges
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key)
                .Take(16)
                .Select(pair => new AspectVisualColorChange(
                    Unpack((uint)pair.Key),
                    Unpack((uint)(pair.Key >> 32)),
                    pair.Value))
                .ToArray(),
            MeanAbsoluteError: meanAbsoluteError,
            Percentile99: percentile99,
            MaximumAbsoluteDelta: maximumDelta,
            MaximumMeanAbsoluteError,
            MaximumPercentile99,
            MaximumAbsoluteDeltaThreshold: MaximumAbsoluteDelta,
            Passes: passes);
        Directory.CreateDirectory(Path.GetDirectoryName(reportFullPath)!);
        File.WriteAllText(
            reportFullPath,
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
        string differencePath = Path.ChangeExtension(reportFullPath, ".diff.png");
        using SKImage differenceImage = SKImage.FromBitmap(difference);
        using SKData encoded = differenceImage.Encode(SKEncodedImageFormat.Png, 100);
        using FileStream output = File.Create(differencePath);
        encoded.SaveTo(output);

        Console.WriteLine(
            $"Aspect visual diff: MAE={meanAbsoluteError:F4}, P99={percentile99}, " +
            $"max={maximumDelta}, changed={changedPixels}/{baseline.Width * baseline.Height}, " +
            $"passes={passes}");
        Console.WriteLine($"Report: {reportFullPath}");
        Console.WriteLine($"Difference image: {differencePath}");
        if (!passes)
        {
            throw new InvalidOperationException(
                "Aspect visual diff exceeded the canonical RGBA thresholds: " +
                $"MAE<={MaximumMeanAbsoluteError:F1}, P99<={MaximumPercentile99}, max<={MaximumAbsoluteDelta}.");
        }
    }

    private static uint Pack(SKColor color)
    {
        return (uint)(color.Red | (color.Green << 8) | (color.Blue << 16) | (color.Alpha << 24));
    }

    private static string Unpack(uint value)
    {
        return $"#{(byte)value:X2}{(byte)(value >> 8):X2}{(byte)(value >> 16):X2}{(byte)(value >> 24):X2}";
    }

    private sealed record AspectVisualDiffReport(
        int SchemaVersion,
        string Baseline,
        string Actual,
        int Width,
        int Height,
        int ChangedPixels,
        double ChangedPixelRatio,
        AspectVisualBounds? ChangedBounds,
        IReadOnlyList<AspectVisualColorChange> MostFrequentColorChanges,
        double MeanAbsoluteError,
        int Percentile99,
        int MaximumAbsoluteDelta,
        double MaximumMeanAbsoluteError,
        int MaximumPercentile99,
        int MaximumAbsoluteDeltaThreshold,
        bool Passes);

    private sealed record AspectVisualBounds(int Left, int Top, int Right, int Bottom);

    private sealed record AspectVisualColorChange(string Baseline, string Actual, int Count);
}
