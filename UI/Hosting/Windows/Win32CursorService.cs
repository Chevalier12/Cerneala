using System.ComponentModel;
using System.Runtime.InteropServices;
using Cerneala.UI.Platform;

namespace Cerneala.UI.Hosting.Windows;

internal sealed class Win32CursorService : ICursorService
{
    private readonly Func<int, nint> loadCursor;
    private readonly Action<nint> applyCursor;
    private readonly Dictionary<int, nint> handles = [];

    public Win32CursorService()
        : this(
            resourceId => Win32.LoadCursor(0, resourceId),
            cursor => Win32.SetCursor(cursor))
    {
    }

    internal Win32CursorService(Func<int, nint> loadCursor, Action<nint> applyCursor)
    {
        this.loadCursor = loadCursor ?? throw new ArgumentNullException(nameof(loadCursor));
        this.applyCursor = applyCursor ?? throw new ArgumentNullException(nameof(applyCursor));
    }

    public CursorShape Current { get; private set; } = CursorShape.Arrow;

    public void SetCursor(CursorShape shape)
    {
        nint handle = shape == CursorShape.Hidden
            ? 0
            : GetHandle(ResourceIdFor(shape));
        applyCursor(handle);
        Current = shape;
    }

    private nint GetHandle(int resourceId)
    {
        if (handles.TryGetValue(resourceId, out nint handle))
        {
            return handle;
        }

        handle = loadCursor(resourceId);
        if (handle == 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not load a system cursor.");
        }

        handles.Add(resourceId, handle);
        return handle;
    }

    private static int ResourceIdFor(CursorShape shape)
    {
        return shape switch
        {
            CursorShape.Hand => Win32.IDC_HAND,
            CursorShape.IBeam => Win32.IDC_IBEAM,
            CursorShape.Crosshair => Win32.IDC_CROSS,
            CursorShape.ResizeHorizontal => Win32.IDC_SIZEWE,
            CursorShape.ResizeVertical => Win32.IDC_SIZENS,
            _ => Win32.IDC_ARROW
        };
    }
}
