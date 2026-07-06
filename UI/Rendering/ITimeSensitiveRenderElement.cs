namespace Cerneala.UI.Rendering;

public interface ITimeSensitiveRenderElement
{
    bool UpdateRenderTime(TimeSpan frameTime);
}
