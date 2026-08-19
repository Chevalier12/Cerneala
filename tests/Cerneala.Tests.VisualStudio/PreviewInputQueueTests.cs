namespace Cerneala.Tests.VisualStudio;

using Cerneala.Preview;
using Cerneala.VisualStudio.Preview;

public sealed class PreviewInputQueueTests
{
    [Fact]
    public void ConsecutivePointerMovesCoalesceWithoutCrossingButtonTransitions()
    {
        CernealaPreviewInputQueue queue = new();
        queue.Enqueue(new PreviewRequest { Kind = PreviewRequestKind.PointerMove, X = 1, Y = 1 });
        queue.Enqueue(new PreviewRequest { Kind = PreviewRequestKind.PointerMove, X = 2, Y = 2 });
        queue.Enqueue(new PreviewRequest
        {
            Kind = PreviewRequestKind.PointerButton,
            X = 2,
            Y = 2,
            Button = "Left",
            IsDown = true
        });
        queue.Enqueue(new PreviewRequest { Kind = PreviewRequestKind.PointerMove, X = 3, Y = 3 });
        queue.Enqueue(new PreviewRequest { Kind = PreviewRequestKind.PointerMove, X = 4, Y = 4 });

        Assert.True(queue.TryDequeue(out PreviewRequest? first));
        Assert.Equal(2, Assert.IsType<PreviewRequest>(first).X);
        Assert.True(queue.TryDequeue(out PreviewRequest? second));
        Assert.Equal(PreviewRequestKind.PointerButton, Assert.IsType<PreviewRequest>(second).Kind);
        Assert.True(queue.TryDequeue(out PreviewRequest? third));
        Assert.Equal(4, Assert.IsType<PreviewRequest>(third).X);
        Assert.False(queue.TryDequeue(out _));
    }
}
