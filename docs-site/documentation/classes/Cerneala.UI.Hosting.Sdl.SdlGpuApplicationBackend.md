# SdlGpuApplicationBackend Class

## Definition

Namespace: `Cerneala.UI.Hosting.Sdl`

Assembly/Project: `Cerneala.Backends.SdlGpu`

Source: `Cerneala.Backends.SdlGpu/Hosting/SdlGpuApplicationBackend.cs`

Registers the SDL3 window platform and SDL_GPU graphics backend used by Cerneala desktop applications.

```csharp
public static class SdlGpuApplicationBackend
```

## Examples

Select SDL_GPU once for the executable assembly:

```csharp
[assembly: Cerneala.UI.Hosting.Windowing.ApplicationBackend(
    typeof(Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend))]
```

The executable must reference `Cerneala.Backends.SdlGpu`; its published output must also contain the SDL3 native asset for the selected runtime identifier.

## Remarks

Generated Cerneala startup calls `EnsureRegistered` when this type is selected through `ApplicationBackendAttribute`. The method composes `Cerneala.Platforms.Sdl3` windowing with the SDL_GPU graphics-session factory and registers the composition atomically in the shared windowing registry.

Registration is idempotent for SDL_GPU. A later attempt to register WindowsDX, or to register SDL_GPU after WindowsDX, throws a deterministic conflict exception because one process may own only one windowing backend.

The adapter uses SDL3 for windowing and `SDL_GPU` for rendering over D3D12, Vulkan, or Metal. It does not use `SDL_Renderer`, and no SDL binding type is exposed by the public API. Shaders are precompiled and packaged; ShaderCross is not required at application runtime.

## Methods

| Name | Description |
| --- | --- |
| `EnsureRegistered()` | Ensures that the composed SDL3 and SDL_GPU backend is available to the window host. |

## Applies to

.NET 8 desktop applications published for `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, or `osx-arm64` with the matching SDL3 native asset.

## See also

- `Cerneala.UI.Hosting.Windowing.ApplicationBackendAttribute`
- `Cerneala.UI.Hosting.Windows.WindowsDxApplicationBackend`
- `docs/sdl-desktop-backend.md`
- `docs/application-markup.md`
