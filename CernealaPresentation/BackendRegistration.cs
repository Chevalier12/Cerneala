#if CERNEALA_MONOGAME
[assembly: Cerneala.UI.Hosting.Windowing.ApplicationBackend(
    typeof(Cerneala.UI.Hosting.Windows.WindowsDxApplicationBackend))]
#elif CERNEALA_SDL3
[assembly: Cerneala.UI.Hosting.Windowing.ApplicationBackend(
    typeof(Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend))]
#endif
