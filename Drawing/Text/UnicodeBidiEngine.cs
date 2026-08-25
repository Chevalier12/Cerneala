using System.Globalization;

namespace Cerneala.Drawing.Text;

internal enum UnicodeTextDirection
{
    Neutral,
    LeftToRight,
    RightToLeft
}

internal readonly record struct UnicodeDirectionalRun(
    int Start,
    int Length,
    UnicodeTextDirection Direction);

internal static class UnicodeBidiEngine
{
    public static UnicodeTextDirection GetBaseDirection(string? text)
    {
        foreach (UnicodeTextElement element in UnicodeLineBreakEngine.CreateTextElements(text ?? string.Empty))
        {
            UnicodeTextDirection direction = GetDirection(element.Text);
            if (direction != UnicodeTextDirection.Neutral)
            {
                return direction;
            }
        }
        return UnicodeTextDirection.LeftToRight;
    }

    public static IReadOnlyList<UnicodeDirectionalRun> GetDirectionalRuns(string? text)
    {
        string value = text ?? string.Empty;
        UnicodeTextElement[] elements = UnicodeLineBreakEngine.CreateTextElements(value);
        if (elements.Length == 0)
        {
            return [];
        }

        List<UnicodeDirectionalRun> runs = [];
        UnicodeTextDirection current = NormalizeNeutral(
            GetDirection(elements[0].Text),
            UnicodeTextDirection.LeftToRight);
        int start = elements[0].Start;
        for (int index = 1; index < elements.Length; index++)
        {
            UnicodeTextDirection direction = NormalizeNeutral(GetDirection(elements[index].Text), current);
            if (direction == current)
            {
                continue;
            }

            runs.Add(new UnicodeDirectionalRun(start, elements[index].Start - start, current));
            start = elements[index].Start;
            current = direction;
        }

        runs.Add(new UnicodeDirectionalRun(start, value.Length - start, current));
        return runs;
    }

    public static UnicodeTextDirection GetDirection(string textElement)
    {
        if (string.IsNullOrEmpty(textElement))
        {
            return UnicodeTextDirection.Neutral;
        }

        int scalar = char.ConvertToUtf32(textElement, 0);
        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(textElement, 0);
        if (IsRightToLeft(scalar) && category is
            UnicodeCategory.OtherLetter or
            UnicodeCategory.LetterNumber or
            UnicodeCategory.DecimalDigitNumber)
        {
            return UnicodeTextDirection.RightToLeft;
        }

        return category is
            UnicodeCategory.LowercaseLetter or
            UnicodeCategory.UppercaseLetter or
            UnicodeCategory.TitlecaseLetter or
            UnicodeCategory.ModifierLetter or
            UnicodeCategory.OtherLetter or
            UnicodeCategory.DecimalDigitNumber
            ? UnicodeTextDirection.LeftToRight
            : UnicodeTextDirection.Neutral;
    }

    private static UnicodeTextDirection NormalizeNeutral(
        UnicodeTextDirection direction,
        UnicodeTextDirection fallback) =>
        direction == UnicodeTextDirection.Neutral ? fallback : direction;

    private static bool IsRightToLeft(int scalar) =>
        (scalar >= 0x0590 && scalar <= 0x08FF) ||
        (scalar >= 0xFB1D && scalar <= 0xFDFF) ||
        (scalar >= 0xFE70 && scalar <= 0xFEFF) ||
        (scalar >= 0x10800 && scalar <= 0x10FFF) ||
        (scalar >= 0x1E800 && scalar <= 0x1EEFF);
}
