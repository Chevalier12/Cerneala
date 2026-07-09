using System.Globalization;

namespace Cerneala.UI.Text;

public sealed class BidiTextService
{
    public static BidiTextService Default { get; } = new();

    public TextDirection GetBaseDirection(string text)
    {
        foreach (char value in text ?? string.Empty)
        {
            TextDirection direction = GetDirection(value);
            if (direction is TextDirection.LeftToRight or TextDirection.RightToLeft)
            {
                return direction;
            }
        }

        return TextDirection.LeftToRight;
    }

    public IReadOnlyList<BidiTextRun> GetDirectionalRuns(string text)
    {
        string value = text ?? string.Empty;
        if (value.Length == 0)
        {
            return [];
        }

        List<BidiTextRun> runs = [];
        TextDirection currentDirection = NormalizeNeutral(GetDirection(value[0]), TextDirection.LeftToRight);
        int start = 0;

        for (int i = 1; i < value.Length; i++)
        {
            TextDirection direction = NormalizeNeutral(GetDirection(value[i]), currentDirection);
            if (direction == currentDirection)
            {
                continue;
            }

            runs.Add(new BidiTextRun(start, i - start, currentDirection));
            start = i;
            currentDirection = direction;
        }

        runs.Add(new BidiTextRun(start, value.Length - start, currentDirection));
        return runs;
    }

    public bool ContainsRightToLeft(string text)
    {
        return GetDirectionalRuns(text).Any(run => run.Direction == TextDirection.RightToLeft);
    }

    private static TextDirection NormalizeNeutral(TextDirection direction, TextDirection fallback)
    {
        return direction == TextDirection.Neutral ? fallback : direction;
    }

    private static TextDirection GetDirection(char value)
    {
        return CharUnicodeInfo.GetUnicodeCategory(value) switch
        {
            UnicodeCategory.OtherLetter when IsRightToLeft(value) => TextDirection.RightToLeft,
            UnicodeCategory.LowercaseLetter or
                UnicodeCategory.UppercaseLetter or
                UnicodeCategory.TitlecaseLetter or
                UnicodeCategory.ModifierLetter or
                UnicodeCategory.OtherLetter or
                UnicodeCategory.DecimalDigitNumber => TextDirection.LeftToRight,
            _ => TextDirection.Neutral
        };
    }

    private static bool IsRightToLeft(char value)
    {
        return (value >= '\u0590' && value <= '\u08FF') ||
            (value >= '\uFB1D' && value <= '\uFDFF') ||
            (value >= '\uFE70' && value <= '\uFEFF');
    }
}
