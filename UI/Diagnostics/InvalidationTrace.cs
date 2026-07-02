using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.UI.Diagnostics;

public sealed class InvalidationTrace
{
    private readonly List<InvalidationTraceEntry> entries = [];

    public static InvalidationTrace Disabled { get; } = new(false);

    public InvalidationTrace(bool isEnabled = true)
    {
        IsEnabled = isEnabled;
    }

    public bool IsEnabled { get; }

    public IReadOnlyList<InvalidationTraceEntry> Entries => entries;

    public void RecordRequest(InvalidationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!IsEnabled)
        {
            return;
        }

        entries.Add(new InvalidationTraceEntry(
            InvalidationTraceEventKind.Request,
            request.Target,
            GetElementId(request.Target),
            request.Flags,
            null,
            request.Reason,
            request.SourceProperty));
    }

    public void RecordPropagation(UIElement element, InvalidationFlags flags, string reason)
    {
        Record(InvalidationTraceEventKind.Propagation, element, flags, null, reason);
    }

    public void RecordQueue(UIElement element, InvalidationFlags flags, string reason)
    {
        Record(InvalidationTraceEventKind.Queue, element, flags, null, reason);
    }

    public void RecordPhase(FramePhase phase, UIElement element, InvalidationFlags flags)
    {
        Record(InvalidationTraceEventKind.Phase, element, flags, phase, phase.ToString());
    }

    public void RecordPhaseSummary(FramePhase phase, int count)
    {
        if (!IsEnabled)
        {
            return;
        }

        entries.Add(new InvalidationTraceEntry(
            InvalidationTraceEventKind.PhaseSummary,
            null,
            null,
            InvalidationFlags.None,
            phase,
            count.ToString(),
            null));
    }

    public void RecordClear(UIElement element, InvalidationFlags flags)
    {
        Record(InvalidationTraceEventKind.Clear, element, flags, null, "Clear");
    }

    private void Record(
        InvalidationTraceEventKind kind,
        UIElement element,
        InvalidationFlags flags,
        FramePhase? phase,
        string reason)
    {
        if (!IsEnabled)
        {
            return;
        }

        entries.Add(new InvalidationTraceEntry(kind, element, GetElementId(element), flags, phase, reason, null));
    }

    private static string? GetElementId(UIElement? element)
    {
        return element?.ElementId?.ToString();
    }
}

public enum InvalidationTraceEventKind
{
    Request,
    Propagation,
    Queue,
    Phase,
    PhaseSummary,
    Clear
}

public sealed record InvalidationTraceEntry(
    InvalidationTraceEventKind Kind,
    UIElement? Element,
    string? ElementId,
    InvalidationFlags Flags,
    FramePhase? Phase,
    string Reason,
    UiProperty? SourceProperty)
{
    public string? SourcePropertyName => SourceProperty?.DiagnosticName;
}
