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

### Public Graphix managed/native dependencies

The native runtime comes from [Graphix.Native 3.4.16-graphix.4](https://www.nuget.org/packages/Graphix.Native/3.4.16-graphix.4),
an independently maintained SDL fork. The managed binding is [Graphix-CS 3.4.16.1](https://www.nuget.org/packages/Graphix-CS/3.4.16.1),
a packaging fork of upstream `SDL3-CS v3.4.16.0`. It preserves namespace `SDL3`,
public class `SDL`, assembly name `SDL3-CS.dll`, and the upstream binding/generator source.
Do not reference both `Graphix-CS` and `SDL3-CS` in the same application.
The separate `SDL3-CS.*.Shadercross` packages remain build-tool dependencies,
not part of Graphix. Do not add the old `SDL3-CS.Windows`, `SDL3-CS.Linux` or
`SDL3-CS.MacOS` native providers alongside Graphix.

Both packages are public on NuGet.org. The repository's `NuGet.Config` declares
the public NuGet source without clearing user-wide package sources. A fresh
checkout restores directly; no GitHub authentication, temporary Actions artifact
or local package feed is required by developers or CI:

```powershell
dotnet restore Cerneala.slnx
```

The managed package comes from verified [Graphix-CS main CI run 34129320508](https://github.com/Chevalier12/Graphix-CS/actions/runs/34129320508),
commit `79f3958a88aad3ba354aeee8fece42ff33782d56`. The downloaded NuGet.org `.nupkg` SHA256 is
`C11770CC11D194E22D17C8CD90DCD93D0346B6ED78DDD72475F5F2CFD5C1CA1D`.
The package records this commit in its repository metadata and contains no native runtime assets.

The inherited callback generator references Roslyn 5.9. Cerneala pins SDK `10.0.400`
in `global.json` so the compiler can load it; application target frameworks remain unchanged.
Older Roslyn 5.6 compilers reject the generator with `CS9057`.

The native package records Graphix source commit
`7dcfac5a73007e72bd8fb060861e46fe07f55c46`, built by
[Graphix CI run 34256929848](https://github.com/Chevalier12/Graphix/actions/runs/34256929848).
Its downloaded NuGet.org `.nupkg` SHA256 is
`FEEBE8900AAA2CFDFDE48F2941E9CD2DDAA899D36A90375D11DBB4D38BD0C376`.
Its six native CTest suites passed
25/25 each. A separate real Windows-driver maximize regression passed 400/400
assertions on x64 and ARM64. These checks do not certify GPU behavior.
The public packages are independent of Actions artifact retention. NuGet.org adds
a repository signature, so the public ZIP hashes differ from the unsigned CI
artifacts. Every managed/native payload entry was compared byte-for-byte against
the verified CI package, and both NuGet repository signatures were validated.
Never replace a package's payload under an already-used version.

This version retains the independent Windows maximum-dimension correction and
adds D3D12 descriptor-heap and GPU renderer texture corrections. The exact packaged
Windows x64 DLL also passed 21 descriptor assertions and 162 texture assertions
on each of D3D12 and Vulkan; this does not certify GPU execution on other RIDs.
Windows process argument quoting and shell validation also changed: the stricter
batch/cmd argument-list policy is intentionally restrictive, while an explicit
raw command line remains the caller's responsibility. The native header documents
that policy. The independent NVIDIA alpha-occlusion failure is not fixed by this
release. Repository verification and
remaining blockers are tracked in the
[migration audit](audits/2026-09-05-monogame-removal.md). Package CI is not a
substitute for Cerneala's rendering, input, allocation and native-platform gates.

The September 8, 2026 graphix.4 consumer verification ran with native tests enabled:
the SDL project passed 534/538 tests, and the full solution passed 4,802/4,806
executed tests. Both runs retained the same four known NVIDIA alpha-occlusion
failures (content 1, 7, 6, 3; differences 47, 25, 27, 46/255). Native input passed.
The solution also hit the documented Visual Studio nested-restore `NETSDK1047`;
that unchanged test project passed 47/47 in a separate invocation. Neither
recheck nor package publication makes the original full-solution run green.
Windows x64/ARM64 smoke publishing, x64 multi-window and Prism execution, six
shader artifact checks and the Prism public-surface audit passed. Consumer
execution on Linux, macOS and Windows ARM64, and human validation, were not run.
Local commands, logs and TRX results are retained under `.artifacts/graphix-native-4/`.

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
