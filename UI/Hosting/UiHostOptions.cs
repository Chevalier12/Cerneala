using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.UI.Hosting;

public sealed class UiHostOptions
{
    public UIRoot? Root { get; set; }

    public UiViewport Viewport { get; set; } = new(0, 0);

    public IInputSource? InputSource { get; set; }

    public IUiBackend? Backend { get; set; }

    public IUiClock? Clock { get; set; }

    public ElementInputBridge? InputBridge { get; set; }
}
