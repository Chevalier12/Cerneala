# SDL3 + SDL_GPU Desktop Backend

The SDL desktop backend combines SDL3 windowing with SDL_GPU rendering and is the sole maintained desktop backend. The source generator stays backend-neutral, and each executable selects exactly one composition at assembly level.

## Select the backend

Reference the backend and source generator from a `net8.0` executable:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <RuntimeIdentifiers>win-x64;win-arm64;linux-x64;linux-arm64;osx-x64;osx-arm64</RuntimeIdentifiers>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Cerneala.csproj" />
    <ProjectReference Include="..\Cerneala.Backends.SdlGpu\Cerneala.Backends.SdlGpu.csproj" />
    <ProjectReference Include="..\Cerneala.SourceGen\Cerneala.SourceGen.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
    <AdditionalFiles Include="**\*.crn" Exclude="bin\**;obj\**" />
  </ItemGroup>
</Project>
```

Then add one assembly declaration:

```csharp
[assembly: Cerneala.UI.Hosting.Windowing.ApplicationBackend(
    typeof(Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend))]
```

Repeating the same SDL_GPU registration is harmless. A different windowing backend cannot be registered in the same process. The retired MonoGame and WindowsDX compositions are no longer available.

## Rendering contract

SDL3 owns windows, input, DPI, cursors, and the event pump. The renderer uses `SDL_GPU` directly: one GPU device is shared by the platform, while each window owns its swapchain and presentation session. There is no `SDL_Renderer`/`SDL_Render` path.

The SDL_GPU driver is D3D12 on Windows, Vulkan on Linux, and Metal on macOS. Shader artifacts for DXIL, SPIR-V, and MSL are compiled offline from the shared HLSL sources. The published application does not compile shaders and does not carry ShaderCross as a runtime dependency.

### Internal drawing flow

`SdlGpuDrawingBackend` translates Cerneala draw commands and state stacks into SDL_GPU geometry and state. It also owns text-atlas preparation, Prism and layer execution, render-target changes, and every barrier that requires pending geometry to be emitted. The internal `Cerberus` component owns the reusable CPU-side geometry storage, adjacent compatible-batch merging, index rebasing, geometry upload, and ordered SDL_GPU draw emission.

Cerberus is an implementation detail of `Cerneala.Backends.SdlGpu`, not a public extension point. It exposes no public `Begin`/`End` lifecycle or sorting options; `IDrawingBackend` remains the public renderer substitution contract. Each window graphics session has its own backend, geometry-upload arena, Cerberus instance, and mutable queue. Only the existing device-level drawing resources are shared across sessions.

The backend flushes through one coordination path. Pending resource uploads, including the text atlas, are completed before Cerberus uploads and emits queued geometry. Flush barriers cover copy passes, Prism execution, layer and `RenderSurface2D` target changes, child-target composition, clip/stencil transitions, and the end of a command range or frame. Cerberus preserves painter order and merges only immediately adjacent compatible triangle lists; it never sorts commands by texture or depth.

SDL_GPU state caching is local to one flush and its resumed render pass. The first draw binds complete state, and subsequent draws omit only documented-safe redundant binds. A copy pass, target change, geometry upload, or other render-pass restart discards that cache so the resumed pass binds complete state again.

## Runtime identifiers and native assets

### Graphix native dependency and temporary local feed

The native runtime comes from `Graphix.Native` version `3.4.16-graphix.2`,
an independently maintained SDL fork. The `SDL3-CS` managed binding remains
at `3.4.14.1`; the separate ShaderCross packages are build-tool dependencies,
not part of Graphix. Do not add the old `SDL3-CS.Windows`, `SDL3-CS.Linux` or
`SDL3-CS.MacOS` native providers alongside Graphix.

Publication is pending. The repository's `NuGet.Config` adds the temporary
`artifacts/graphix-packages` feed without changing user-wide NuGet settings or
removing existing sources. Before restoring a fresh checkout, copy the verified
package into that directory. While the CI artifact remains available, the
repository helper downloads it using authenticated GitHub CLI and verifies its
SHA256. The desktop and shader workflows call the same helper before restore.
An existing matching package is reused; mismatched bytes are rejected, not overwritten:

```powershell
.\Tools\scripts\Get-GraphixNativePackage.ps1
dotnet restore Cerneala.slnx
```

This package records Graphix source commit
`0c23f43af6884849165ebf21ba1d14fa2d6cdf51`. Its six native CTest suites passed
25/25 each. A separate real Windows-driver maximize regression passed 400/400
assertions on x64 and ARM64. These checks do not certify GPU behavior.
The artifact has a 30-day retention period and is not a permanent feed. The
package directory is ignored by Git, so other developers and CI must populate
it before restore. An existing copy can be supplied directly instead of using
GitHub CLI. Never replace a package's bytes under an already-used version.

This version corrects independent maximum dimensions in native Windows
maximization, including borderless client sizing. Repository verification and
remaining blockers are tracked in the
[migration audit](audits/2026-09-05-monogame-removal.md). Package CI is not a
substitute for Cerneala's rendering, input, allocation and native-platform gates.

### Published application assets

| Runtime identifier | Required SDL3 asset | Expected SDL_GPU driver |
| --- | --- | --- |
| `win-x64`, `win-arm64` | `SDL3.dll` | D3D12 |
| `linux-x64`, `linux-arm64` | `libSDL3.so`, with its versioned links/files | Vulkan |
| `osx-x64`, `osx-arm64` | `libSDL3.0.dylib` and `libSDL3.dylib` | Metal |

Use the repository publish helper to build and validate an output directory without deleting existing artifacts:

```powershell
.\Tools\scripts\Publish-SdlGpuSmoke.ps1 `
  -RuntimeIdentifier win-x64 `
  -OutputRoot artifacts\sdlgpu-publish
```

The script fails if the expected SDL3 asset is absent or an SDL native asset for another operating-system family leaked into the output.

Run the published application through the matching launch helper:

```powershell
.\Tools\scripts\Invoke-SdlGpuSmoke.ps1 `
  -RuntimeIdentifier win-x64 `
  -PublishedDirectory artifacts\sdlgpu-publish\win-x64 `
  -Mode multi-window `
  -ArtifactDirectory artifacts\sdlgpu-smoke
```

Available smoke modes cover single-window, multi-window, input, resize, Drawing, `RenderSurface2D`, Prism, and screenshot behavior. Screenshots are captured only through `Window.SaveScreenshot`; no operating-system screen-copy API is used.

## CI notes

The desktop workflow publishes both architectures for each operating-system family and executes native multi-window and Prism smoke tests on Windows, Linux, and macOS. Linux CI uses Xvfb and Mesa lavapipe when no physical display/GPU is available. This software Vulkan configuration is a CI fallback, not a runtime requirement for user applications.

## See also

- [Application markup and backend selection](application-markup.md)
- `Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend`
- `Cerneala.UI.Hosting.Windowing.ApplicationBackendAttribute`
