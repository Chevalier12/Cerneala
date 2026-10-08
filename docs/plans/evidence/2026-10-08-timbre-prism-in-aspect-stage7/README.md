# Etapa 7 — Tooling, documentație, smoke, verificare completă

Plan: [2026-10-08-timbre-prism-in-aspect.md](../../2026-10-08-timbre-prism-in-aspect.md), Etapa 7.

## Tooling

- Completion: `@timbre`/`@prism` la începutul corpului Aspect, `@sound`/`@parameter` în `<TimbreClip>`, `@play/@stop/@pause/@resume/@seek` în `@on/@when/@if`, `$X.timbre.` (sunete, apoi `Volume` și parametri) — `TimbreToolingTests`, `CompletionTests` (Etapa 4, verdi în suita de mai jos).
- Gramatica VS: `@sound Nume` (`entity.name.section.sound.cerneala`), cuvintele cheie `@timbre|@sound|@play|@stop|@pause|@resume|@seek|@modifier`, `to` înaintea duratei; golden `cerneala-tokenization.crn`/`.golden.json` regenerat; `Cerneala.Tests.VisualStudio` 48/48.
- LanguageServer fără proiect (`DiagnosticService.CreateStandaloneEmbeddedDiagnostics`): în corpul unui Aspect, `@timbre`/`@prism` sunt găsite cu `AspectAttachmentScanner` (același scanner ca modelul semantic, mutat în `Cerneala.Language/Syntax/Embedded/AspectAttachmentScanner.cs`), verificate pe formă (`;` lipsă, corpul inline `@timbre { … }` prin `TimbreMarkupBinder.BindClip`, `@prism` prin `PrismSyntaxParser.ParseApplications`) și golite înainte de parsarea Motion. `TimbreDiagnosticsAreTheSameForStandaloneAndProjectDocuments` (cazul `@timbre $Tone` fără `;`) trece; `Cerneala.Tests.LanguageServer` 45/45. Decizia 15.
- PreviewHost: `PreviewAudioIsDisabledByDefaultAndARecompiledSessionRetiresTheOldScopes` rulează acum pentru `@when … { @play $self.timbre.Tone; }` și pentru `AutoPlay = true` fără comenzi: prima sesiune pornește sunetul o dată (blocat, audio oprit implicit), recompilarea o retrage, sesiunea nouă îl pornește din nou o singură dată. `Cerneala.Tests.PreviewHost` 23/23. Decizia 16.

## Documentație

`docs/timbre-guide.md` (§1 model cu `TimbreSound`/`TimbreClipDefinition`, §3–7), `docs/CernealaMarkupGuide.md` §14–15, `docs/prism-guide.md`, docs-site (`TimbreClipDefinition`, `TimbreClipSound`, `GeneratedMarkup`, `MarkupConditionRule`, paginile Prism redenumite, exemplele `RenderSurface2D`/`Scene2DDebugOverlay`/`Sprite2D`) și `manifest.json` — actualizate în Etapele 1–6. `@handle` rămas în ghidul de markup e cel al Motion (`@run $Clip as Playback`), nu al sunetului. Fișierul gol rămas din editare `GeneratedMarkup.md.new` a fost șters.

## Smoke Windows (`native/`)

`dotnet run --project tests/Cerneala.SdlGpuSmoke/Cerneala.SdlGpuSmoke.csproj -c Release --no-build --no-restore -- --mode <mod> --artifacts <stage7>/native --no-screenshot`:

| Mod | Ieșire | Rezultat |
| --- | --- | --- |
| `timbre-markup` | exit 0, 20 s | `SDL_GPU_SMOKE_OK mode=timbre-markup scenarios=10 … opens=1` (același număr de scenarii ca în 2026-10-03; `overlap` → `click/restart`, decizia 12) |
| `timbre-motion` | exit 0, 16 s | `SDL_GPU_SMOKE_OK mode=timbre-motion scenarios=8 opens=1` |
| `timbre` | exit 0, 26 s | `SDL_GPU_SMOKE_OK mode=timbre scenarios=18 … opens=2` |

Log-uri: `run-timbre-markup.log`, `run-timbre-motion.log`, `run-timbre.log`; diagnostice `*-diagnostics.json`. Fișierele WAV generate de smoke ca intrări nu sunt păstrate.

## ApiCompat strict față de `59c0e257` (`api-compat-strict.log`)

`dotnet msbuild ../2026-10-08-timbre-prism-in-aspect-stage1/api-compat.proj -t:Compare` (strict, fără suppression-uri): exit 1, 46 de diferențe. Comparate cu diff-ul deja clasificat al fundației (`../2026-10-08-aspect-runtime-program-stage3/api-compat-strict.log`, care include Etapele 1–2), diferențele noi sunt exact cele fixate în `../2026-10-08-timbre-prism-in-aspect-stage0/README.md`:

- tipuri adăugate: `Cerneala.Timbre.TimbreClipDefinition`, `Cerneala.Timbre.TimbreClipSound`;
- adăugate în `GeneratedMarkup`: `AttachTimbre(UIElement, ResourceId<TimbreClipDefinition>, IReadOnlyDictionary<string, float>?)`, `AttachTimbre(UIElement, TimbreClipDefinition, IReadOnlyDictionary<string, float>?)`, `PlayTimbre(UIElement, string)`, `StopTimbre(UIElement, string)`, `PauseTimbre(UIElement, string)`, `ResumeTimbre(UIElement, string)`, `SeekTimbre(UIElement, string, TimeSpan)`, `StartTimbreMotionProperty(UIElement, string, string, …)`;
- eliminate din `GeneratedMarkup`: `PlayTimbre(IDisposable, ResourceId<TimbreSound>, …, string?)` (care în fundație apărea ca redenumire din `TimbreClip`), `CancelTimbre(IDisposable, string)`, `PauseTimbre(IDisposable, string)`, `ResumeTimbre(IDisposable, string)`, `SeekTimbre(IDisposable, string, TimeSpan)`, `StartTimbreMotionProperty(IDisposable, string, string, …)`.

Nicio altă diferență.

## Verificare completă (`full/`)

- `dotnet build .\Cerneala.slnx -c Release -m:1`: 0 erori (`full/build.log`).
- `CERNEALA_SDL_NATIVE_TESTS=1 CERNEALA_TIMBRE_AUDIO_DEVICE=1 dotnet test .\Cerneala.slnx -c Release --no-build --no-restore -m:1`: exit 0 (`full/test.log`, TRX):

| Proiect | Passed | Skipped |
| --- | --- | --- |
| Cerneala.Tests | 4333 | 0 |
| Cerneala.Tests.SdlGpu | 1009 | 5 |
| Cerneala.Tests.SourceGen | 647 | 0 |
| Cerneala.Tests.Language | 408 | 1 |
| Cerneala.Tests.Timbre | 391 | 0 |
| Cerneala.Tests.Scene2DImporters | 173 | 0 |
| Cerneala.Tests.Scene2DPackages | 104 | 0 |
| Cerneala.Tests.VisualStudio | 48 | 0 |
| Cerneala.Tests.LanguageServer | 45 | 0 |
| Cerneala.Tests.SceneVillage | 43 | 1 |
| Cerneala.Tetris.Tests | 31 | 0 |
| Cerneala.Tests.PreviewHost | 23 | 0 |

Exemplele din ghiduri compilează: `DocumentedTimbreExamplesCompile` (`timbre-guide.md`, `CernealaMarkupGuide.md`) Passed.

Cele 7 skip-uri sunt aceleași ca în rularea completă a fundației (`../2026-10-08-aspect-runtime-program-stage4/full/test.log`): `WarmCompletionP95…`, `NativeVillageFrameRateGate…`, `NativeOwnershipInputAndGraphicsLifetimes…`, patru `OpaqueStrokeOccludesEarlierStroke…`.

- `git diff --check`: curat. Fără `NotImplementedException`, `Debugger.Break` sau log-uri de debug în fișierele modificate/noi (`Console.WriteLine` rămase sunt ieșirea existentă a uneltelor/benchmark-urilor/smoke-ului).
- Platformă: Windows nativ executat; Linux/macOS N/A (politica §9).
