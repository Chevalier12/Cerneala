namespace Cerneala.UI.Prism.Definitions;

public readonly record struct PrismSourceSpan
{
    public PrismSourceSpan(int start, int length, string? sourceName = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        if (sourceName is not null && string.IsNullOrWhiteSpace(sourceName))
        {
            throw new ArgumentException(
                "A Prism source name cannot be empty or whitespace.",
                nameof(sourceName));
        }

        Start = start;
        Length = length;
        SourceName = sourceName;
    }

    public int Start { get; }

    public int Length { get; }

    public string? SourceName { get; }

    public override string ToString()
    {
        return $"{SourceName ?? "<source>"}@{Start}+{Length}";
    }
}
