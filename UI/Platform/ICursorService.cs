namespace Cerneala.UI.Platform;

public interface ICursorService
{
    CursorShape Current { get; }

    void SetCursor(CursorShape shape);
}
