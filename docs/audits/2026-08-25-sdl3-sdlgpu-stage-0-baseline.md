# SDL3 + SDL_GPU — jurnal etapa 0

Data: 2026-08-25

## Versiuni și proveniență

| Componentă | Versiune | Sursă |
|---|---:|---|
| SDL | `3.4.14` | [release upstream](https://github.com/libsdl-org/SDL/releases/tag/release-3.4.14) |
| SDL3-CS | `3.4.14.1` | [NuGet](https://www.nuget.org/packages/SDL3-CS/3.4.14.1), [lista oficială de bindings](https://wiki.libsdl.org/SDL3/LanguageBindings) |
| SDL3-CS.Windows | `3.4.14.1` | [NuGet](https://www.nuget.org/packages/SDL3-CS.Windows/3.4.14.1) |
| SDL3-CS.Linux | `3.4.14.1` | [NuGet](https://www.nuget.org/packages/SDL3-CS.Linux/3.4.14.1) |
| SDL3-CS.MacOS | `3.4.14.1` | [NuGet](https://www.nuget.org/packages/SDL3-CS.MacOS/3.4.14.1) |
| SDL3-CS desktop ShaderCross | `3.0.0.9` | [SDL_shadercross](https://github.com/libsdl-org/SDL_shadercross), pachetele NuGet `SDL3-CS.Windows.Shadercross`, `SDL3-CS.Linux.Shadercross`, `SDL3-CS.MacOS.Shadercross` |

Hash-urile SHA-512 salvate de NuGet în cache-ul restaurat sunt:

| Pachet | SHA-512 (`.nupkg.sha512`, Base64) |
|---|---|
| `SDL3-CS/3.4.14.1` | `TNfy2UvuP6PsHjdPKXR5KbFvo3anYChLGzlO6poXhdfFmcwSmaXkupZUsboV5AtZBBLPpx2tEFl/C2rzTj+FUg==` |
| `SDL3-CS.Windows/3.4.14.1` | `P6HGlk2QLRGXg+yx17SRRc9PEtLJZnDZvtmwO2dQXVMM7CNNdb9GWrwfGFaXtzOkGJqqmKm4YrFdEgaWtyt8Ew==` |
| `SDL3-CS.Linux/3.4.14.1` | `2qRJ2mLoO5FA4lNR+p8bVsWCdIxtKqMOaxTJuvlcUTzF9Nb2mzH/RtR5MHsAkRqsUuEfop0y4xcdYjo2dwBOCQ==` |
| `SDL3-CS.MacOS/3.4.14.1` | `HtdiJ2arZ13C+8px2TGJg+JmlCwQ6wMU4kOGbNydLhZbB0fJW7qHc+i9MAM2ZK31iv3yczSjf0AGL4tSgXn+/A==` |
| `SDL3-CS.Windows.Shadercross/3.0.0.9` | `rLQ5tqs8VgsfPUODb6ezSqpbyYcyQXpUhj9Jr7KTdkpQyNWxu7vaW0xzNVqbVxMg09af4H7Q4XakcsbkKzW5uQ==` |
| `SDL3-CS.Linux.Shadercross/3.0.0.9` | `klANZgrOjwRk7peSn/w7AiXmpw4MxOZUd29NDvi4AjGGIO7yrDaV4meRSywF9N4/K+/FEDk3SIpGlY28DGk46A==` |
| `SDL3-CS.MacOS.Shadercross/3.0.0.9` | `wPXPqC0SsUd4eSvFCk+FUx/JcFxQcslgyNom7wFXZsxdoZYNrmpJ3mwIajQT0EjR14sHsupi1Z1DiGGClXjcqQ==` |

## Restore și publish RID

Un proiect temporar controlat a referit simultan bindingul, toate cele trei familii native și toate cele trei familii ShaderCross. Restore-ul a reușit, iar publish-ul framework-dependent Release a produs următoarele asset-uri SDL native:

| RID | Asset-uri native SDL publicate |
|---|---|
| `win-x64` | `SDL3.dll` |
| `win-arm64` | `SDL3.dll` |
| `linux-x64` | `libSDL3.so`, `libSDL3.so.0`, `libSDL3.so.0.4.14` |
| `linux-arm64` | `libSDL3.so`, `libSDL3.so.0`, `libSDL3.so.0.4.14` |
| `osx-x64` | `libSDL3.0.dylib`, `libSDL3.dylib` |
| `osx-arm64` | `libSDL3.0.dylib`, `libSDL3.dylib` |

Niciun output nu a conținut DLL, SO sau dylib pentru alt sistem de operare. Directorul temporar `C:\Users\lauri\AppData\Local\Temp\cerneala-sdl3-stage0-spike` a fost șters după experiment; verificarea finală a întors `False` pentru `Test-Path`.

## API spike

Reflection asupra `SDL3-CS 3.4.14.1` și a bindingului ShaderCross a confirmat API-urile necesare:

| Capacitate | Membri găsiți |
|---|---|
| window ID și event routing | `SDL.GetWindowID` (1), `SDL.PollEvent` (2) |
| device și window claim | `SDL.CreateGPUDevice` (1), `SDL.ClaimWindowForGPUDevice` (1), `SDL.ReleaseWindowFromGPUDevice` (1) |
| swapchain și command buffer | `SDL.WaitAndAcquireGPUSwapchainTexture` (1), `SDL.AcquireGPUCommandBuffer` (1) |
| render/copy pass | `SDL.BeginGPURenderPass` (6), `SDL.BeginGPUCopyPass` (1) |
| transfer și readback | `SDL.CreateGPUTransferBuffer` (1), `SDL.MapGPUTransferBuffer` (1), `SDL.DownloadFromGPUTexture` (1) |
| fences | `SDL.SubmitGPUCommandBufferAndAcquireFence` (1), `SDL.WaitForGPUFences` (3), `SDL.QueryGPUFence` (1), `SDL.ReleaseGPUFence` (1) |
| capability/error | `SDL.GetGPUShaderFormats` (1), `SDL.GetError` (1) |
| ShaderCross | `ShaderCross.Init` (1), `CompileSPIRVFromHLSL` (2), `CompileDXILFromHLSL` (1), `TranspileMSLFromSPIRV` (1), `ReflectGraphicsSPIRV` (1) |

Nu este necesară nicio completare P/Invoke pentru suprafața minimă investigată. Bindingul va rămâne totuși încapsulat intern, conform planului.

## Testele RED ale generatorului

`UiMarkupGeneratorBackendSelectionTests` adaugă 18 cazuri care compilează fixture-uri pentru:

- selecție absentă și duplicată;
- tip inaccesibil sau generic;
- cinci forme invalide ale `EnsureRegistered`;
- două backenduri fake valide, pentru `<Application>` și legacy `MainWindow`, cu `Main` generat și module initializer;
- interzicerea namespace-ului Windows, a assembly-ului backend și a artefactelor WindowsDX în proiectul/outputul SourceGen.

Rularea RED focalizată a avut `18/18` eșecuri, exclusiv pentru comportamentul încă neimplementat: lipsește `CERNEALAUI015`, outputul cheamă în continuare backendul Windows hardcodat, iar sursa generatorului conține namespace-ul Windows. Fixture-urile compilează și nu există eșecuri de infrastructură sau ipoteze SDL în aceste teste.

## Scene canonice și capturi WindowsDX

Contractul machine-readable este în `tests/Baselines/Conformance/scenes.json`. Toate imaginile au fost produse exclusiv prin `Window.SaveScreenshot`, cu stare animată dezactivată și telemetry fixă. Fiecare captură a fost repetată, iar a doua execuție a produs același SHA-256 byte-for-byte.

| Scenă | Contract | Captură WindowsDX | SHA-256 |
|---|---|---|---|
| Drawing API | 800×600 logical, 1000×750 RGBA sRGB, DPI 1.25, frame 8, Segoe UI + Segoe UI Emoji, mascot local fix | `WindowsDx/drawing-api.png` | `57B11D62651660AE85684D8B8458CFA9D0B5E4EE97FC3551FDCFB7773C9D8ABE` |
| Multi-window A | 320×200 logical/physical, scale 1, frame 8, culoare solidă distinctă | `WindowsDx/multi-window-a.png` | `CD9270713A577EC2899D1141B2CA68F6EB8B924D8E1C351ACEB793C573AEAC63` |
| Multi-window B | 320×200 logical/physical, scale 1, frame 8, culoare solidă distinctă | `WindowsDx/multi-window-b.png` | `555BC6AFA9139109622B1F78BEC7F432D77407D1868738AA84D6753C258EB64C` |
| Prism | client 1320×844 logical, 1650×1055 RGBA sRGB, DPI 1.25, telemetry fixă | `WindowsDx/prism.png` | `B838ECFF3563D79FE3F2A172FA50DE82774D80ABF2EC45549D7A9180698359A1` |

Scena Drawing acoperă primitive, path fill/stroke, text, imagini, clip/transform/Screen blend și `RenderSurface2D`. Scena multi-window captează două ferestre simultane cu output distinct. Presetul Prism selectează automat prima operație fără resurse, în ordine lexicală, din fiecare grup `Kind:Category`; lista exactă a celor șapte reprezentanți este salvată în `prism.metrics.txt` și în manifest.

## Baseline existent

Înaintea schimbărilor etapei:

- `Cerneala.Tests.SourceGen`, Release: `448/448` teste trecute;
- `Cerneala.Tests`, Release: `3098/3098` teste trecute;
- `dotnet build Cerneala.slnx -c Release`: reușit cu `0` warnings și `0` errors.

Nu au existat eșecuri preexistente. Verificarea finală a etapei trebuie să păstreze cele 448 de teste SourceGen existente GREEN și să accepte numai cele 18 eșecuri RED noi până la implementarea etapei 1.

Verificarea finală a etapei, după toate modificările de baseline, a confirmat:

- `dotnet build Cerneala.slnx -c Release --no-restore`: reușit, `0` warnings, `0` errors;
- `Cerneala.Tests.SourceGen`: `448` passed, exact `18` failed (toate cazurile RED de mai sus), `0` skipped;
- `Cerneala.Tests`: `3098/3098` passed;
- smoke-ul WindowsDX obișnuit: exit code `0`;
- capturile Drawing, multi-window și Prism: hash identic la două execuții consecutive;
- proiectul temporar RID/API: absent.
