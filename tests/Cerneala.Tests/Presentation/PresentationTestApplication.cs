using Cerneala.Presentation;
using Cerneala.UI;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.Presentation;

// The presentation views need the installed App (Application.Current.Resources), not a native window.
// The SDL platform must be disposed on the thread that created it, and xUnit disposes a test that
// resumed after an await on another thread (issue #738), so these tests install a windowless platform.
internal static class PresentationTestApplication
{
    public static void Install()
    {
        Application.ResetForTesting();
        WindowApplicationRuntime.ResetForTesting();
        WindowApplicationRuntime runtime = new(new WindowlessPlatform());
        WindowApplicationRuntime.Install(runtime);
        App app = new();
        app.Install(runtime);
    }

    private sealed class WindowlessPlatform : IWindowPlatform
    {
        public IPlatformWindow CreateWindow(Window window, IWindowPlatformCallbacks callbacks) =>
            throw new InvalidOperationException("Presentation view tests do not open windows.");

        public void PumpEvents()
        {
        }

        public void Dispose()
        {
        }
    }
}
