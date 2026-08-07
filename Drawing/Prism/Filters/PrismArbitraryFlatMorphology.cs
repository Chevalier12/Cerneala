using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismArbitraryFlatMorphology
{
    public static Vector4[] DilateRound(
        ReadOnlySpan<Vector4> source,
        int width,
        int height,
        float radiusX,
        float radiusY) =>
        Apply(
            source,
            width,
            height,
            radiusX,
            radiusY,
            round: true,
            dilate: true);

    public static Vector4[] ErodeRound(
        ReadOnlySpan<Vector4> source,
        int width,
        int height,
        float radiusX,
        float radiusY) =>
        Apply(
            source,
            width,
            height,
            radiusX,
            radiusY,
            round: true,
            dilate: false);

    public static Vector4[] ErodeSquare(
        ReadOnlySpan<Vector4> source,
        int width,
        int height,
        float radiusX,
        float radiusY) =>
        Apply(
            source,
            width,
            height,
            radiusX,
            radiusY,
            round: false,
            dilate: false);

    private static Vector4[] Apply(
        ReadOnlySpan<Vector4> source,
        int width,
        int height,
        float radiusX,
        float radiusY,
        bool round,
        bool dilate)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }
        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }
        if (source.Length != checked(width * height))
        {
            throw new ArgumentException(
                "The source pixel count does not match its dimensions.",
                nameof(source));
        }
        if (!float.IsFinite(radiusX) || radiusX < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radiusX));
        }
        if (!float.IsFinite(radiusY) || radiusY < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radiusY));
        }
        if (radiusX == 0 && radiusY == 0)
        {
            return source.ToArray();
        }

        Chord[] horizontal = round
            ? BuildRoundChords(radiusX, radiusY)
            : BuildSquareChords(radiusX, radiusY);
        Chord[] vertical = round
            ? BuildRoundChords(radiusY, radiusX)
            : BuildSquareChords(radiusY, radiusX);
        bool useVertical = vertical.Length < horizontal.Length;
        return ApplyByChords(
            source,
            width,
            height,
            useVertical ? vertical : horizontal,
            useVertical,
            dilate);
    }

    private static Vector4[] ApplyByChords(
        ReadOnlySpan<Vector4> source,
        int width,
        int height,
        Chord[] chords,
        bool vertical,
        bool dilate)
    {
        int lineLength = vertical ? height : width;
        int lineCount = vertical ? width : height;
        int minimumStart = chords.Min(chord => chord.Start);
        int maximumEnd = chords.Max(
            chord => chord.Start + chord.Length - 1);
        int leftPadding = Math.Max(0, -minimumStart);
        int rightPadding = Math.Max(0, maximumEnd);
        int[] lengths = RequiredLengths(chords);
        Dictionary<int, LineLookup> lookupByLine = [];
        Vector4[] output = new Vector4[source.Length];

        for (int cross = 0; cross < lineCount; cross++)
        {
            foreach (Chord chord in chords)
            {
                int sourceLine = Math.Clamp(
                    cross + chord.CrossOffset,
                    0,
                    lineCount - 1);
                if (!lookupByLine.ContainsKey(sourceLine))
                {
                    lookupByLine.Add(
                        sourceLine,
                        BuildLineLookup(
                            source,
                            width,
                            lineLength,
                            sourceLine,
                            vertical,
                            leftPadding,
                            rightPadding,
                            lengths,
                            dilate));
                }
            }

            for (int along = 0; along < lineLength; along++)
            {
                Vector4 value = dilate
                    ? Vector4.Zero
                    : Vector4.One;
                foreach (Chord chord in chords)
                {
                    int sourceLine = Math.Clamp(
                        cross + chord.CrossOffset,
                        0,
                        lineCount - 1);
                    Vector4 candidate = lookupByLine[sourceLine].Get(
                        chord.Length,
                        along + chord.Start + leftPadding);
                    value = dilate
                        ? Vector4.Max(value, candidate)
                        : Vector4.Min(value, candidate);
                }

                int outputIndex = vertical
                    ? (along * width) + cross
                    : (cross * width) + along;
                output[outputIndex] = value;
            }

            HashSet<int> nextLines = [];
            if (cross + 1 < lineCount)
            {
                foreach (Chord chord in chords)
                {
                    nextLines.Add(
                        Math.Clamp(
                            cross + 1 + chord.CrossOffset,
                            0,
                            lineCount - 1));
                }
            }
            foreach (int sourceLine in lookupByLine.Keys.ToArray())
            {
                if (!nextLines.Contains(sourceLine))
                {
                    lookupByLine.Remove(sourceLine);
                }
            }
        }

        return output;
    }

    private static LineLookup BuildLineLookup(
        ReadOnlySpan<Vector4> source,
        int width,
        int lineLength,
        int line,
        bool vertical,
        int leftPadding,
        int rightPadding,
        int[] lengths,
        bool dilate)
    {
        int paddedLength = checked(
            lineLength + leftPadding + rightPadding);
        Dictionary<int, Vector4[]> valuesByLength = [];
        Vector4[] single = new Vector4[paddedLength];
        for (int index = 0; index < paddedLength; index++)
        {
            int coordinate = Math.Clamp(
                index - leftPadding,
                0,
                lineLength - 1);
            int sourceIndex = vertical
                ? (coordinate * width) + line
                : (line * width) + coordinate;
            single[index] = source[sourceIndex];
        }
        valuesByLength.Add(1, single);

        foreach (int length in lengths)
        {
            if (length == 1)
            {
                continue;
            }

            int halfLength = (length + 1) / 2;
            int secondOffset = length - halfLength;
            Vector4[] half = valuesByLength[halfLength];
            Vector4[] values = new Vector4[paddedLength];
            int lastStart = paddedLength - length;
            for (int start = 0; start <= lastStart; start++)
            {
                values[start] = dilate
                    ? Vector4.Max(
                        half[start],
                        half[start + secondOffset])
                    : Vector4.Min(
                        half[start],
                        half[start + secondOffset]);
            }
            valuesByLength.Add(length, values);
        }

        return new LineLookup(valuesByLength);
    }

    private static Chord[] BuildRoundChords(
        float alongRadius,
        float crossRadius)
    {
        int crossExtent = (int)MathF.Ceiling(crossRadius);
        List<Chord> chords = [];
        for (int cross = -crossExtent; cross <= crossExtent; cross++)
        {
            if (crossRadius == 0 && cross != 0)
            {
                continue;
            }

            float normalizedCross = crossRadius == 0
                ? 0
                : cross / crossRadius;
            float remaining =
                1 - (normalizedCross * normalizedCross);
            if (remaining < 0)
            {
                continue;
            }

            int alongExtent = alongRadius == 0
                ? 0
                : (int)MathF.Floor(
                    (alongRadius * MathF.Sqrt(remaining)) +
                    0.000001f);
            chords.Add(
                new Chord(
                    cross,
                    -alongExtent,
                    checked((alongExtent * 2) + 1)));
        }

        return [.. chords];
    }

    private static Chord[] BuildSquareChords(
        float alongRadius,
        float crossRadius)
    {
        int alongExtent = (int)MathF.Floor(
            alongRadius + 0.000001f);
        int crossExtent = (int)MathF.Floor(
            crossRadius + 0.000001f);
        int length = checked((alongExtent * 2) + 1);
        Chord[] chords = new Chord[(crossExtent * 2) + 1];
        for (int cross = -crossExtent;
             cross <= crossExtent;
             cross++)
        {
            chords[cross + crossExtent] = new Chord(
                cross,
                -alongExtent,
                length);
        }
        return chords;
    }

    private static int[] RequiredLengths(Chord[] chords)
    {
        HashSet<int> lengths = [1];
        foreach (Chord chord in chords)
        {
            int length = chord.Length;
            while (length > 1)
            {
                lengths.Add(length);
                length = (length + 1) / 2;
            }
        }
        return [.. lengths.Order()];
    }

    private readonly record struct Chord(
        int CrossOffset,
        int Start,
        int Length);

    private sealed class LineLookup(
        Dictionary<int, Vector4[]> valuesByLength)
    {
        public Vector4 Get(int length, int start) =>
            valuesByLength[length][start];
    }
}
