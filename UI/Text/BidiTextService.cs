using Cerneala.Drawing.Text;

namespace Cerneala.UI.Text;

public sealed class BidiTextService
{
    public static BidiTextService Default { get; } = new();

    public TextDirection GetBaseDirection(string text) =>
        Map(UnicodeBidiEngine.GetBaseDirection(text));

    public IReadOnlyList<BidiTextRun> GetDirectionalRuns(string text) =>
        UnicodeBidiEngine.GetDirectionalRuns(text)
            .Select(static run => new BidiTextRun(run.Start, run.Length, Map(run.Direction)))
            .ToArray();

    public bool ContainsRightToLeft(string text) =>
        UnicodeBidiEngine.GetDirectionalRuns(text)
            .Any(static run => run.Direction == UnicodeTextDirection.RightToLeft);

    private static TextDirection Map(UnicodeTextDirection direction) =>
        direction switch
        {
            UnicodeTextDirection.RightToLeft => TextDirection.RightToLeft,
            UnicodeTextDirection.LeftToRight => TextDirection.LeftToRight,
            _ => TextDirection.Neutral
        };
}
