using System.Reflection;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlGpuSurfaceDamageAllocationTests
{
    [Fact]
    public void RetainedEntryComparisonDoesNotAllocatePerCommandIdentity()
    {
        DrawCommandList commands = new();
        commands.Add(DrawCommand.FillRectangle(new DrawRect(0, 0, 10, 10), Color.White));
        commands.Add(DrawCommand.FillRectangle(new DrawRect(0, 0, 10, 10), Color.Black));
        DrawCommandStateAnalysis analysis = new DrawCommandStateAnalyzer().Analyze(commands);
        DrawCommandStateEntry entry = analysis.Entries[0];
        var equals = typeof(SdlGpuDrawingBackend)
            .GetMethod("SurfaceEntryEquals", BindingFlags.Static | BindingFlags.NonPublic)!
            .CreateDelegate<Func<DrawCommandStateEntry, DrawCommandStateEntry, bool>>();
        for (int warmup = 0; warmup < 16; warmup++)
        {
            _ = equals(entry, entry);
        }

        const int comparisons = 1024;
        bool allEqual = true;
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < comparisons; index++)
        {
            allEqual &= equals(entry, entry);
        }
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(allEqual);
        Assert.True(allocatedBytes <= comparisons * 32,
            $"Retained entry comparison allocated {allocatedBytes:N0} bytes for {comparisons} command identities.");
        Assert.False(equals(entry, analysis.Entries[1]));
        Assert.False(equals(default, entry));
        Assert.False(equals(entry, default));
        Assert.False(equals(default, default));
    }
}
