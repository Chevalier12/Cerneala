using System.Globalization;

namespace Cerneala.UI.Controls;

internal static class ComboBoxTextMatcher
{
    public static IReadOnlyList<int> Rank(
        int itemCount,
        Func<int, string> getItemText,
        string query,
        CultureInfo culture,
        bool caseSensitive)
    {
        if (query.Length == 0)
        {
            return Enumerable.Range(0, itemCount).ToArray();
        }

        CompareInfo compareInfo = culture.CompareInfo;
        CompareOptions options = caseSensitive ? CompareOptions.None : CompareOptions.IgnoreCase;
        string normalizedQuery = Normalize(query, culture, caseSensitive);
        int maximumDistance = Math.Min(3, Math.Max(1, (query.Length + 2) / 3));
        List<Match> matches = [];

        for (int sourceIndex = 0; sourceIndex < itemCount; sourceIndex++)
        {
            string itemText = getItemText(sourceIndex);
            if (compareInfo.Compare(itemText, query, options) == 0)
            {
                matches.Add(new Match(sourceIndex, 0, 0, 0));
                continue;
            }

            if (compareInfo.IsPrefix(itemText, query, options))
            {
                matches.Add(new Match(sourceIndex, 1, 0, 0));
                continue;
            }

            int containsPosition = compareInfo.IndexOf(itemText, query, options);
            if (containsPosition >= 0)
            {
                matches.Add(new Match(sourceIndex, 2, 0, containsPosition));
                continue;
            }

            if (query.Length < 3)
            {
                continue;
            }

            string normalizedItemText = Normalize(itemText, culture, caseSensitive);
            int distance = MinimumWindowDistance(normalizedItemText, normalizedQuery, maximumDistance);
            if (distance <= maximumDistance)
            {
                matches.Add(new Match(sourceIndex, 3, distance, Math.Abs(itemText.Length - query.Length)));
            }
        }

        return matches
            .OrderBy(match => match.Tier)
            .ThenBy(match => match.Distance)
            .ThenBy(match => match.Position)
            .ThenBy(match => match.SourceIndex)
            .Select(match => match.SourceIndex)
            .ToArray();
    }

    private static string Normalize(string value, CultureInfo culture, bool caseSensitive)
    {
        return caseSensitive ? value : value.ToUpper(culture);
    }

    private static int MinimumWindowDistance(string candidate, string query, int maximumDistance)
    {
        if (candidate.Length == 0 || query.Length == 0)
        {
            return Math.Max(candidate.Length, query.Length);
        }

        int minimumLength = Math.Max(1, query.Length - maximumDistance);
        int maximumLength = Math.Min(candidate.Length, query.Length + maximumDistance);
        if (minimumLength > maximumLength)
        {
            return DamerauLevenshteinDistance(candidate, query);
        }

        int best = int.MaxValue;
        for (int length = minimumLength; length <= maximumLength; length++)
        {
            for (int start = 0; start + length <= candidate.Length; start++)
            {
                int distance = DamerauLevenshteinDistance(candidate.AsSpan(start, length), query.AsSpan());
                if (distance < best)
                {
                    best = distance;
                }
            }
        }

        return best;
    }

    private static int DamerauLevenshteinDistance(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
    {
        int rowLength = right.Length + 1;
        Span<int> rows = rowLength <= 128
            ? stackalloc int[rowLength * 3]
            : new int[rowLength * 3];
        Span<int> previousPrevious = rows[..rowLength];
        Span<int> previous = rows.Slice(rowLength, rowLength);
        Span<int> current = rows[(rowLength * 2)..];
        for (int index = 0; index <= right.Length; index++)
        {
            previous[index] = index;
        }

        for (int leftIndex = 1; leftIndex <= left.Length; leftIndex++)
        {
            current[0] = leftIndex;
            for (int rightIndex = 1; rightIndex <= right.Length; rightIndex++)
            {
                int substitutionCost = left[leftIndex - 1] == right[rightIndex - 1] ? 0 : 1;
                int distance = Math.Min(
                    Math.Min(previous[rightIndex] + 1, current[rightIndex - 1] + 1),
                    previous[rightIndex - 1] + substitutionCost);
                if (leftIndex > 1 && rightIndex > 1 &&
                    left[leftIndex - 1] == right[rightIndex - 2] &&
                    left[leftIndex - 2] == right[rightIndex - 1])
                {
                    distance = Math.Min(distance, previousPrevious[rightIndex - 2] + 1);
                }

                current[rightIndex] = distance;
            }

            Span<int> temporary = previousPrevious;
            previousPrevious = previous;
            previous = current;
            current = temporary;
        }

        return previous[right.Length];
    }

    private readonly record struct Match(int SourceIndex, int Tier, int Distance, int Position);
}
