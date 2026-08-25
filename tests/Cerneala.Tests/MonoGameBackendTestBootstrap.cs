using System.Runtime.CompilerServices;
using Cerneala.UI.Hosting.Windows;

namespace Cerneala.Tests;

internal static class MonoGameBackendTestBootstrap
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        WindowsDxApplicationBackend.EnsureRegistered();
    }
}
