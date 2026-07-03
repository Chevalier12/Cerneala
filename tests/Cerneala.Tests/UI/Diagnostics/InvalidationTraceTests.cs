using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.Tests.UI.Diagnostics;

public sealed class InvalidationTraceTests
{
    [Fact]
    public void EnabledTraceRecordsInvalidationRequestAndPhases()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);

        child.Invalidate(InvalidationFlags.Render, "render");
        root.ProcessFrame();

        Assert.Contains(root.Trace.Entries, entry => entry.Kind == InvalidationTraceEventKind.Request);
        Assert.Contains(root.Trace.Entries, entry => entry.Kind == InvalidationTraceEventKind.Phase && entry.Phase == FramePhase.RenderCache);
        Assert.Contains(root.Trace.Entries, entry => entry.Kind == InvalidationTraceEventKind.Clear);
    }

    [Fact]
    public void DisabledTraceDoesNotRetainEntries()
    {
        InvalidationTrace trace = InvalidationTrace.Disabled;
        UIRoot root = new();
        UIElement child = new();

        trace.RecordRequest(new InvalidationRequest(child, InvalidationFlags.Render, "render"));

        Assert.Empty(trace.Entries);
        Assert.True(root.Trace.IsEnabled);
    }

    [Fact]
    public void TraceEntryKeepsElementIdRecordedAtInvalidationTime()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        string elementId = child.ElementId!.Value.ToString();

        child.Invalidate(InvalidationFlags.Render, "render");
        InvalidationTraceEntry requestEntry = Assert.Single(
            root.Trace.Entries,
            entry => entry.Kind == InvalidationTraceEventKind.Request && entry.Reason == "render");

        root.VisualChildren.Remove(child);

        Assert.Equal(elementId, requestEntry.ElementId);
    }
}
