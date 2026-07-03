namespace Cerneala.UI.Platform;

public interface ICursorService
{
    CursorShape Current { get; }

    void SetCursor(CursorShape shape);
}

public enum CursorShape
{
    Default,
    Arrow,
    Hand,
    IBeam,
    Crosshair,
    ResizeHorizontal,
    ResizeVertical,
    Hidden
}
