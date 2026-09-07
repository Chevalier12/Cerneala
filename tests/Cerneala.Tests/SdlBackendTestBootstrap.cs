using System.Runtime.CompilerServices;
using Cerneala.UI.Hosting.Sdl;

namespace Cerneala.Tests;

internal static class SdlBackendTestBootstrap
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        SdlGpuApplicationBackend.EnsureRegistered();
    }
}
