using System.Globalization;
using System.Text;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Diagnostics;

public sealed class DirtyTreeDumper
{
    public string Dump(UIElement root, InvalidationTrace? trace = null, ElementChildRole role = ElementChildRole.Visual)
    {
        ArgumentNullException.ThrowIfNull(root);

        StringBuilder builder = new();
        builder.AppendLine("Dirty tree");

        bool foundDirty = false;
        foreach (UIElement element in ElementTreeWalker.PreOrder(root, role))
        {
            if (!element.DirtyState.IsDirty)
            {
                continue;
            }

            foundDirty = true;
            DirtyTraceInfo? latest = FindLatestEntry(trace, element);
            builder.Append(CultureInfo.InvariantCulture, $"- {element.GetType().Name}#{element.ElementId?.ToString() ?? "unattached"}");
            builder.Append(CultureInfo.InvariantCulture, $" flags={element.DirtyState.Flags}");
            builder.Append(CultureInfo.InvariantCulture, $" version={element.DirtyState.Version}");
            if (latest is not null)
            {
                builder.Append(CultureInfo.InvariantCulture, $" reason={latest.Reason}");
                if (latest.SourcePropertyName is not null)
                {
                    builder.Append(CultureInfo.InvariantCulture, $" source={latest.SourcePropertyName}");
                }
            }

            builder.AppendLine();
        }

        if (!foundDirty)
        {
            builder.AppendLine("- none");
        }

        return builder.ToString().TrimEnd();
    }

    private static DirtyTraceInfo? FindLatestEntry(InvalidationTrace? trace, UIElement element)
    {
        if (trace is null)
        {
            return null;
        }

        for (int index = trace.Entries.Count - 1; index >= 0; index--)
        {
            InvalidationTraceEntry entry = trace.Entries[index];
            if (ReferenceEquals(entry.Element, element))
            {
                return new DirtyTraceInfo(entry.Reason, FindSourcePropertyName(trace, element, index));
            }
        }

        return null;
    }

    private static string? FindSourcePropertyName(InvalidationTrace trace, UIElement element, int startIndex)
    {
        for (int index = startIndex; index >= 0; index--)
        {
            InvalidationTraceEntry entry = trace.Entries[index];
            if (!ReferenceEquals(entry.Element, element))
            {
                continue;
            }

            if (entry.SourcePropertyName is not null)
            {
                return entry.SourcePropertyName;
            }

            if (entry.Kind == InvalidationTraceEventKind.Request)
            {
                return null;
            }
        }

        return null;
    }

    private sealed record DirtyTraceInfo(string Reason, string? SourcePropertyName);
}
