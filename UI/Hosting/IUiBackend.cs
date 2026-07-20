using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.UI.Input;

namespace Cerneala.UI.Hosting;

public interface IUiBackend
{
    IInputSource? InputSource { get; }

    IDrawingBackend? DrawingBackend { get; }

    IBackdropFrameSource? BackdropFrameSource => null;
}
