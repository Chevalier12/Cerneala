using System.Globalization;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismCompletenessReportTests
{
    [Fact]
    public void PassRowsOfTheApprovedMatrixHaveEqualApprovedAndObservedCounts()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        string[] lines = File.ReadAllLines(
            Path.Combine(directory.FullName, "docs", "reference", "prism-completeness-report.generated.md"));
        int start = Array.FindIndex(lines, line => line.Trim() == "## Approved matrix");
        Assert.True(start >= 0, "The report has no '## Approved matrix' section.");

        int checkedRows = 0;
        List<string> offending = [];
        for (int index = start + 1; index < lines.Length && !lines[index].StartsWith("## ", StringComparison.Ordinal); index++)
        {
            string[] cells = lines[index].Trim().Trim('|').Split('|', StringSplitOptions.TrimEntries);
            if (cells.Length != 4 || cells[3] != "PASS" ||
                !int.TryParse(cells[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int approved) ||
                !int.TryParse(cells[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int observed))
            {
                continue;
            }

            checkedRows++;
            if (approved != observed)
            {
                offending.Add($"{cells[0]}: approved {approved}, observed {observed}, result {cells[3]}");
            }
        }

        Assert.True(checkedRows > 1, $"Only {checkedRows} integer PASS rows were found in the matrix.");
        Assert.True(
            offending.Count == 0,
            $"{offending.Count} of {checkedRows} integer PASS rows differ: " + string.Join("; ", offending));
    }
}
