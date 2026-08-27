using Cerneala.UI;

namespace Cerneala.SdlGpuSmoke;

public partial class App : Application
{
    protected override void OnStartup(ApplicationStartupEventArgs args)
    {
        SmokeOptions.Initialize(args.Args);
        base.OnStartup(args);
    }
}
