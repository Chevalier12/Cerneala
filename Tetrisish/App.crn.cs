using Cerneala.UI;

[assembly: Cerneala.UI.Hosting.Windowing.ApplicationBackend(
    typeof(Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend))]

namespace Cerneala.Tetris;

public partial class App : Application
{
}
