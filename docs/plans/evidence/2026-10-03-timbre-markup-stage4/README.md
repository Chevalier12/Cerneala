# Timbre markup/Aspect — etapa 4: docs și dogfood

Snapshot: `master` @ `a838fc35` + modificările necomise ale planului. Host: Windows 11 Pro 10.0.26200 x64, .NET SDK
10.0.400, runtime 8.0.30.

## Documentație

- Ghid conceptual nou [`docs/timbre-guide.md`](../../../timbre-guide.md): modelul (runtime/scope/clip/playback/handle),
  loading și device, redarea din C#, overlap/replace/cancel, Pause/Resume/SeekAsync/Loop (latest seek, DSP
  păstrat/resetat, terminal), `SoundClip` în markup cu tabelul instrucțiunilor și intervalelor din `TimbreCatalog`,
  acțiunile din Aspect, scope/lifetime, evenimente, reguli reactive (initial true, reentry, false fără stop, hidden),
  erori sincrone/asincrone, tooling (030–033, completion) și politica Live Preview. Fără autoplay; numai API/grammar
  implementate.
- [`docs/CernealaMarkupGuide.md`](../../../CernealaMarkupGuide.md): `SoundClip` în resursele build-time și secțiunea nouă
  „14. Sound (Timbre)” (secțiunile următoare renumerotate 15–22; ghidul nu avea trimiteri numerice interne).
- API canonic (`writing-api-documentation`, actualizat în etapa 2 odată cu API-ul): `Cerneala.UI.Markup.GeneratedMarkup.md`
  (cei 10 helperi noi) și `Cerneala.UI.Markup.MarkupConditionRule.md` (constructorul cu `soundActivated`). Nicio pagină
  nouă sau redenumită → `docs-site/documentation/manifest.json` neschimbat; testul de manifest din
  `Cerneala.Tests.VisualStudio` trece.
- Exemple compilate: `DocumentedSoundExamplesCompile` (SourceGen) extrage blocurile `xml`/`csharp` cu sunet din ambele
  ghiduri, rulează generatorul real și compilează C#-ul contra API-ului core — 2/2 passed.

## Dogfood nativ Windows (`tests/Cerneala.SdlGpuSmoke --mode timbre-markup`)

`TimbreMarkupPanel.crn` declară `SoundClip` WAV/MP3/Vorbis/Opus, un clip cu `LowPass`+`Delay` și Aspect-uri `@on Click`
și `@when $Box.IsChecked`; `TimbreMarkupSmoke` le acționează numai prin Servo (input rutat prin fereastra SDL reală) și
nu apelează API-ul C# de redare. Tap-ul PCM existent din fața output-ului SDL3 (WASAPI, 0x8120/2/48000) este comparat
bit cu bit cu același clip definit în C# și randat într-un sink determinist. Rezultat
([`native/timbre-markup-diagnostics.json`](native/timbre-markup-diagnostics.json)): `SDL_GPU_SMOKE_OK mode=timbre-markup
scenarios=10 opens=1`, exit 0.

| Scenariu | Probe |
| --- | --- |
| declarare | 0 redări, 0 deschideri de output, 0 intrări în cache după construirea ferestrei |
| click WAV / MP3 / Vorbis / Opus | 24000 / 96000 / 95040 / 96000 cadre, PCM identic cu clipul C#, o singură redare per click, 0 underrun |
| click `Filtered(ToneCutoff = 800)` | 70080 cadre (coadă Delay), identic cu C# `LowPass`+`Delay` + `Set(cutoff, 800)` |
| overlap | două click-uri fără handle → 2 redări completate, 0 anulări |
| transport reactiv (MP3 2,4 MB, Auto → streaming) | `StreamingBufferBytes` 65536; pause (tap oprit), resume, seek 60s, cancel = 1/1/1/1, fără redări suplimentare |
| loop reactiv | ≥2 wrap-uri; true→false nu oprește/reia; cancel; false→true repornește; exact 2 porniri |
| multiwindow | închiderea ferestrei secundare anulează numai redarea ei; cea din fereastra principală se termină; output deschis o dată, 0 închideri |
| final | `ActiveVoices`, `LiveReaders`, `LiveScopes` = 0; `PlaybacksFailed` = 0 |

Baseline pe același host: smoke-ul C# existent `--mode timbre` → `scenarios=18`, exit 0
([`native/csharp/timbre-diagnostics.json`](native/csharp/timbre-diagnostics.json)). Tap-ul certifică pipeline-ul până la
output, nu ce a auzit cineva; validarea auditivă umană rămâne gate separat al inițiativei (index §7) și nu este dedusă
de aici.

## Compatibilitate API (strict ApiCompat)

[`api-compat.proj`](api-compat.proj): `ValidateAssembliesTask` din SDK 10.0.400, baseline binar propriu `a838fc35`
(worktree `C:\Users\lauri\Desktop\Cerneala-baseline-markup-a838fc3`, `Cerneala.dll` Release), strict mode + nume de
parametri, fără suppression-uri.

- Mod compatibil (`-p:Strict=false`): exit 0 — nicio schimbare incompatibilă ([`api-compat-compatible.log`](api-compat-compatible.log)).
- Strict: exit 1 cu exact 11 `CP0002` „exists on right but not on left” ([`api-compat-strict.log`](api-compat-strict.log)),
  toate adăugiri aprobate și documentate: `GeneratedMarkup.AttachSoundSession(UIElement)`,
  `AttachSoundSession(UIElement, ElementAspect?)`, `AddSoundTrigger`, `PlaySound`, `GetSoundParameter`, `CancelSound`,
  `PauseSound`, `ResumeSound`, `SeekSound`, `CanStartMotionExecution` și constructorul
  `MarkupConditionRule(…, Action<Action?>? soundActivated)`. Nicio eliminare sau modificare.

## Suita completă

`dotnet build .\Cerneala.slnx -c Release` → 0 erori ([`full-solution-build.log`](full-solution-build.log)).
`dotnet test .\Cerneala.slnx -c Release --no-build --no-restore -m:1` cu `CERNEALA_SDL_NATIVE_TESTS=1` și
`CERNEALA_TIMBRE_AUDIO_DEVICE=1` ([`full-solution.log`](full-solution.log), `full-solution/*.trx`):

| Proiect | Rezultat |
| --- | --- |
| Cerneala.Tests | 4326 passed, 2 failed (vezi mai jos) → după corecție, rerulat cu aceleași variabile de mediu: **4328 passed, 0 failed** |
| Cerneala.Tests.SdlGpu | 1009 passed, 5 skipped (preexistente) |
| Cerneala.Tests.SourceGen | 636 passed |
| Cerneala.Tests.Language | 340 passed, 1 skipped (preexistent) |
| Cerneala.Tests.Timbre | 336 passed |
| Cerneala.Tests.Scene2DImporters | 173 passed |
| Cerneala.Tests.Scene2DPackages | 104 passed |
| Cerneala.Tests.VisualStudio | 48 passed |
| Cerneala.Tests.LanguageServer | 45 passed |
| Cerneala.Tests.SceneVillage | 43 passed, 1 skipped (preexistent) |
| Cerneala.Tetris.Tests | 31 passed |
| Cerneala.Tests.PreviewHost | 22 passed |

Cele 2 eșecuri din `Cerneala.Tests` (`DesktopBackendDependencyBoundaryTests.NativeWindowImplementationTermsStayInThePlatformProject`,
`ArchitectureBoundaryTests.FirstPartyCernealaApplicationsUseServoAndDetectiveOwnershipBoundaries`) proveneau exclusiv
din fișierele worktree-ului altei sesiuni Claude, `.claude\worktrees\dreamy-villani-c4a2d0\…` (ignorat de git prin
`.git/info/exclude`), pe care scanările de arhitectură le tratau ca proiecte ale repository-ului. Ca la excluderea
`.claude\**` din `Cerneala.csproj` (etapa 3), scanările sar acum peste segmentul `.claude` la fel ca peste
`bin`/`obj`/`artifacts`; cele două clase trec (27/27), iar `Cerneala.Tests` a fost rerulat integral
([`cerneala-tests-rerun.log`](cerneala-tests-rerun.log)). Codul de producție nu s-a schimbat după rularea suitei.

## Review și cleanup

Diff-ul final a fost revizuit: fără cod de debug/probe, fără fișiere generate în sursă; probe-ul temporar folosit la
diagnosticarea activării duble din etapa 3 a fost șters. Worktree-ul de baseline `Cerneala-baseline-markup-a838fc3`
rămâne lângă celelalte baseline-uri ale inițiativei, ca intrare reproductibilă pentru ApiCompat.

## Publish pe șase RID-uri

`dotnet publish tests/Cerneala.SdlGpuSmoke -c Release -r <rid> --self-contained false` pentru win-x64, win-arm64,
linux-x64, linux-arm64, osx-x64, osx-arm64: exit 0 pe toate; asamblarea publicată conține tipul generat
`TimbreMarkupPanel`, iar fixture-urile audio (`timbre/*.mp3|ogg|opus`) sunt copiate
([`six-rid-publish.log`](six-rid-publish.log)). WAV-ul markup e scris la runtime de smoke. Execuția nativă non-Windows
este N/A conform index §7.
