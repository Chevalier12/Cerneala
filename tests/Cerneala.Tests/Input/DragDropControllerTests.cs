using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Input;

public sealed class DragDropControllerTests
{
    [Fact]
    public void DragMoveRoutesLeaveEnterAndOverWhenTargetChanges()
    {
        UIRoot root = new(100, 100);
        UIElement first = Arranged(0, 0, 20, 20);
        UIElement second = Arranged(40, 0, 20, 20);
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);
        List<string> calls = [];
        first.Handlers.AddHandler(InputEvents.DragLeaveEvent, (_, _) => calls.Add("leave-first"));
        second.Handlers.AddHandler(InputEvents.DragEnterEvent, (_, _) => calls.Add("enter-second"));
        second.Handlers.AddHandler(InputEvents.DragOverEvent, (_, _) => calls.Add("over-second"));
        DragDropController controller = new();

        controller.Begin(first, new DataTransfer().SetData("text/plain", "payload"));
        controller.Move(root, 5, 5);
        controller.Move(root, 45, 5);

        Assert.Equal(["leave-first", "enter-second", "over-second"], calls);
    }

    [Fact]
    public void DropDeliversDataTransfer()
    {
        UIRoot root = new(100, 100);
        UIElement source = Arranged(0, 0, 20, 20);
        UIElement target = Arranged(40, 0, 20, 20);
        root.VisualChildren.Add(source);
        root.VisualChildren.Add(target);
        DragEventArgs? received = null;
        target.Handlers.AddHandler(InputEvents.DropEvent, (_, args) => received = Assert.IsType<DragEventArgs>(args));
        DragDropController controller = new();
        DataTransfer data = new DataTransfer().SetData("text/plain", "payload");

        controller.Begin(source, data);
        controller.Drop(root, 45, 5);

        Assert.NotNull(received);
        Assert.Same(data, received.Data);
        Assert.True(received.Data.TryGetData("text/plain", out string? text));
        Assert.Equal("payload", text);
    }

    [Fact]
    public void DragMoveUsesRetainedInputCacheWhenDirty()
    {
        UIRoot root = new(100, 100);
        UIElement source = Arranged(0, 0, 20, 20);
        UIElement target = Arranged(40, 0, 20, 20);
        root.VisualChildren.Add(source);
        root.VisualChildren.Add(target);
        root.InputCache.EnsureCurrent(root);
        int rebuildsAfterInitialBuild = root.InputCache.RebuildCount;
        DragDropController controller = new();

        target.IsEnabled = false;
        controller.Begin(source, new DataTransfer().SetData("text/plain", "payload"));
        controller.Move(root, 45, 5);

        Assert.Equal(rebuildsAfterInitialBuild + 1, root.InputCache.RebuildCount);
        Assert.False(root.InputCache.RouteMap.TryGetId(target, out _));
    }

    [Fact]
    public void DataTransferTryGetDataReturnsTrueForStoredNullPayload()
    {
        DataTransfer data = new DataTransfer().SetData("application/x-null", null);

        Assert.True(data.TryGetData<object?>("application/x-null", out object? value));
        Assert.Null(value);
    }

    [Fact]
    public void CursorServiceResolvesCursorFromHitElement()
    {
        UIRoot root = new(100, 100);
        UIElement target = Arranged(0, 0, 20, 20);
        root.VisualChildren.Add(target);
        CursorService service = new();

        service.SetCursor(target, Cursor.Hand);

        Assert.Equal(Cursor.Hand, service.Resolve(root, 5, 5));
        Assert.Equal(Cursor.Arrow, service.Resolve(root, 50, 50));
    }

    [Fact]
    public void CursorServiceUsesRetainedInputCacheWhenDirty()
    {
        UIRoot root = new(100, 100);
        UIElement target = Arranged(0, 0, 20, 20);
        root.VisualChildren.Add(target);
        root.InputCache.EnsureCurrent(root);
        int rebuildsAfterInitialBuild = root.InputCache.RebuildCount;
        CursorService service = new();
        service.SetCursor(target, Cursor.Hand);

        target.IsEnabled = false;
        Cursor cursor = service.Resolve(root, 5, 5);

        Assert.Equal(Cursor.Arrow, cursor);
        Assert.Equal(rebuildsAfterInitialBuild + 1, root.InputCache.RebuildCount);
        Assert.False(root.InputCache.RouteMap.TryGetId(target, out _));
    }

    private static UIElement Arranged(float x, float y, float width, float height)
    {
        UIElement element = new();
        element.Arrange(new ArrangeContext(new LayoutRect(x, y, width, height)));
        return element;
    }
}
