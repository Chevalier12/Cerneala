using Cerneala.Drawing;
using Cerneala.UI.Input;

namespace Cerneala.UI.Hosting;

public interface IUiBackend
{
    IInputSource? InputSource { get; }

    IDrawingBackend? DrawingBackend { get; }
}
