# Timbre core API compatibility

Data: 2026-10-07.

## Baseline

- Baseline: worktree detached `C:\Users\lauri\Desktop\Cerneala-baseline-timbre-82a2386` la `82a2386f`, `bin\Release\net8.0\Cerneala.dll`, 5,519,360 bytes, SHA-256 `41BB2C2681442DAAFBAF8A243501DB0A104D168EA1EF5B78B39389E6FE7D4219` (înghețat în etapa 0).
- Proiect: `../2026-10-07-timbre-core-stage0/api-compat.proj` (`ValidateAssembliesTask`, `EnableStrictMode=true`, `EnableRuleCannotChangeParameterName=true`, `PermitUnnecessarySuppressions=false`), fără suppression-uri din alte inițiative.

## Adaosuri suprimate explicit (`api-compat.suppressions.xml`)

- 23 de tipuri noi în `Cerneala.Timbre`: `ISoundOutput`, `ISoundOutputClient`, `SoundClip`, `SoundErrorKind`, `SoundException`, `SoundHandle`, `SoundInput<T>`, `SoundLoading`, `SoundModifier`, `LowPass`, `Delay`, `SoundParameter`, `SoundParameter<T>`, `SoundPlayback`, `SoundPlaybackResult`, `SoundPlaybackState`, `SoundReader`, `SoundReadResult`, `SoundRuntime`, `SoundRuntimeOptions`, `SoundScope`, `SoundSource`, `SoundStartOptions` (CP0001).
- `Cerneala.UI.Detective.SoundDiagnosticsSnapshot` (CP0001).
- Membri noi pe tipuri existente (CP0002): `Application.SoundRuntime` (get/set), `Application.Sounds`, `UIElement.Sounds`, `UIRoot.SoundRuntime`, `UIRoot.SetSoundRuntime(SoundRuntime)`, `UiHostOptions.SoundRuntime` (get/set), `Detective.CaptureSound()`.

Nicio eliminare, schimbare de semnătură sau redenumire de parametru nu este suprimată. `IPlatformServices`, `PlatformServices` și `IResourceProvider` sunt neschimbate.

## Rezultat

```powershell
dotnet msbuild .\benchmarks\Cerneala.Benchmarks\results\2026-10-07-timbre-core-stage0\api-compat.proj -t:Compare -v:minimal
```

Exit 0, nicio diferență nesuprimată sau suppression inutilă (`api-compat.log`).
