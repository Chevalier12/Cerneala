using Cerneala.UI;

namespace Cerneala.SdlGpuSmoke;

public partial class App : Application
{
    protected override void OnStartup(ApplicationStartupEventArgs args)
    {
        SmokeOptions.Initialize(args.Args);
        if (SmokeOptions.Current.Mode is "timbre" or "timbre-markup")
        {
            TimbreSmoke.Install(this);
        }

        base.OnStartup(args);
    }
}
