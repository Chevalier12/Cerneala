# Local platform availability, read-only inventory

Observed 2026-09-24 before the Stage 3 native opt-in run. This is **availability**, not a platform conformance result or waiver. No CI was dispatched and no build, test, native process, or stale WindowsDX binary was executed for this inventory.

`dotnet --info` exited 0. Relevant raw lines:

```text
.NET SDK:
 Version:           10.0.400
Runtime Environment:
 OS Name:     Windows
 OS Version:  10.0.26200
 OS Platform: Windows
 RID:         win-x64
Host:
  Version:      10.0.11
  Architecture: x64
.NET SDKs installed:
  9.0.305 [C:\Program Files\dotnet\sdk]
  10.0.303 [C:\Program Files\dotnet\sdk]
  10.0.400 [C:\Program Files\dotnet\sdk]
.NET runtimes installed:
  Microsoft.NETCore.App 8.0.30 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
  Microsoft.NETCore.App 9.0.9 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
  Microsoft.NETCore.App 10.0.11 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
  Microsoft.WindowsDesktop.App 8.0.30 [C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App]
  Microsoft.WindowsDesktop.App 9.0.9 [C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App]
  Microsoft.WindowsDesktop.App 10.0.11 [C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App]
global.json file:
  C:\Users\lauri\Desktop\Cerneala\global.json
```

Read-only host query reported Windows 11 Pro x64, PowerShell 7.6.5, Intel UHD Graphics 630, and NVIDIA GeForce RTX 2060. This supports attempting Windows SDL native tests; it is not evidence that they pass.

`wsl.exe --list --verbose` exited 1. The merged output, with NUL characters removed for readability, was:

```text
The Windows Subsystem for Linux is not installed. You can install by running 'wsl.exe --install'.

For more information please visit https://aka.ms/wslinstall
```

The read-only PowerShell `Get-Command -ErrorAction SilentlyContinue` loop returned `NOT FOUND` for each of `docker.exe`, `podman.exe`, `qemu-system-x86_64.exe`, `xvfb-run.exe`, `sw_vers`, `xcodebuild`, `xcrun`, `vmrun`, `VBoxManage`, `qemu-system-aarch64.exe`, and `Get-VM`. Thus no local Linux/Xvfb or macOS execution route was found. That is a local inventory, not proof no remote runner exists.

The read-only `Test-Path Env:CERNEALA_SDL_NATIVE_TESTS` check returned `CERNEALA_SDL_NATIVE_TESTS=<unset>` before Stage 3. Native test commands must explicitly set it to `1`.

`Test-Path -LiteralPath` returned `False` for all four current-source probes:

```text
Cerneala.Backends.MonoGame
Cerneala.Platforms.Win32
tests/Cerneala.WindowsDxSmoke/Cerneala.WindowsDxSmoke.csproj
tests/Cerneala.WindowsDxSmoke/Program.cs
```

`git ls-files -- 'Cerneala.Backends.MonoGame' 'Cerneala.Platforms.Win32' 'tests/Cerneala.WindowsDxSmoke'` exited 0 with empty stdout. `rg -n 'WindowsDx|WindowsDX|MonoGame' '.github/workflows/desktop-backends.yml'` exited 1 with empty stdout. Ignored old `tests/Cerneala.WindowsDxSmoke/bin` and `obj` artifacts from 2026-09-05 are not a reproducible tracked backend or harness.

The workflow's existing configured jobs, located by read-only `Select-String` in `.github/workflows/desktop-backends.yml`, are at lines 81–89 (Windows/Ubuntu/macOS SDL matrix), 102 (Linux virtual display/software Vulkan), 161/164 (native pipeline with `CERNEALA_SDL_NATIVE_TESTS: 1`), and 314/316/343/358 (Windows full regression/native opt-in). A configured lane is not an executed run. At the time of this inventory, the Stage 0 contract/platform matrix required Linux/macOS SDL_GPU and WindowsDX or a **new explicit waiver**; none had yet been given. The later explicit user scope decision below supersedes those requirements for this change.

## Subsequent user scope decision — 2026-09-24

The user said: “Nu avem cum sa testam Linux si MacOs pe statia asta. Toate verificarile/gateurile raman doar la nivel de Windows”, followed by “Scoate WindowsDX; gate-urile sunt Windows SDL”. Accordingly, only Windows SDL_GPU native verification remains a platform gate **for this change**. Linux, macOS and WindowsDX were not executed or validated. This is an explicit scope revision, not a retrospective claim that any of those lanes passed, and it does not convert disabled Windows test cases into passes. The native Windows execution evidence is in `stage3-verification.md` and `integration/full-slnx-final-isolation/`.
