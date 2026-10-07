# Timbre SDL3 backend — etapa 2: smoke audio observabil și codecuri

Host: Windows 11 Pro 10.0.26200 x64, .NET 8.0.30, SDL `3.4.16-graphix.6`, driver real `wasapi` (device F32/2/48000, 480 sample frames).

## Smoke `timbre`

- Parser (`SmokeOptions` — mod `timbre`), dispatcher (`MainWindow.OnContentRendered` → `TimbreSmoke.RunAsync`; `OnFrameRendered` ignoră modul), launcher (`Invoke-SdlGpuSmoke.ps1` ValidateSet). `SmokeOptionsTests` (fișierul compilat în proiectul de teste SDL): mod acceptat case-insensitive cu `--artifacts` relativ → cale absolută și `--no-screenshot`, mod necunoscut respins cu lista care conține `timbre`, `--mode`/`--artifacts` fără valoare și argument necunoscut respinse, ValidateSet-ul launcher-ului conține `timbre`.
- `App.OnStartup` (numai în modul timbre) atribuie `Application.SoundRuntime` un runtime cu **PCM tap application-owned** în fața output-ului platformei (`TimbreSmoke.PcmTapOutput`, înregistrează exact PCM-ul acceptat de output). Restul scenariului folosește numai API public C# (`Application.Sounds`, `Window.Sounds`, `SoundClip`, `SoundHandle`, transport). Acces intern (IVT pentru aplicația de test) numai pentru diagnostice și pentru a găsi output-ul platformei.
- Oracle: același clip, același motor, randat în `RecordingSink` (coadă mereu goală, `Preload`). Comparația e **bit-exactă** pe segmentul tap al fiecărui clip izolat; underrun-urile motorului trebuie să fie 0.
- Fixture-uri: WAV sintetic 16-bit 44.1 kHz generat de aplicație; MP3/Vorbis/Opus și MP3 lung copiate din corpusul decoderelor (`tests/Cerneala.Tests.Timbre/Corpus`, proveniență în `corpus-manifest.json`) în `timbre/` din output.

Comenzi: `dotnet run --project .\tests\Cerneala.SdlGpuSmoke\Cerneala.SdlGpuSmoke.csproj -c Release --no-build --no-restore -- --mode timbre --artifacts artifacts/timbre/native --no-screenshot` (exit 0, ~27 s, `run-timbre-diagnostics.json`); `Publish-SdlGpuSmoke.ps1 -RuntimeIdentifier win-x64` + `Invoke-SdlGpuSmoke.ps1 -Mode timbre` (exit 0, `launcher-*.{log,json}`).

## Rezultate (18 scenarii, toate PASS)

| Scenariu | Rezultat |
| --- | --- |
| device absent (`SDL_AUDIO_DRIVER=cerneala-absent-driver`) | `Failed`/`DeviceUnavailable` („Audio target … not available”), 0 deschideri; reset hint + `Play` explicit → `Completed`, 1 deschidere (fără retry automat) |
| WAV 44.1k, MP3 CBR128 44.1k, Vorbis 48k, Opus 48k × Auto/Streaming | toate `Completed`, PCM tap = sink determinist bit-exact (SHA-256 identic Auto vs Streaming per format), 0 underrun |
| LowPass+Delay (Cutoff override 800, Volume 0.8) | `Completed` cu tail, bit-exact vs sink |
| overlap, replacement (slot), cancel dublu | `Completed`×2; `Canceled`/`Completed`; `Canceled`, output rămâne deschis |
| pause Pending (reader cu barieră) | 0 PCM în 30 de request-uri de device, `Paused`, apoi `Completed` după `Resume` |
| pause/resume | poziția ținută, 0 PCM în pauză, `Resume` dublu no-op, `Completed` |
| seek streaming MP3 lung (300 s) | cererea veche anulată (superseded), poziție 60.21 s după cea mai nouă; buffer streaming 64 KiB, reader 576 KiB |
| loop | 2 wrap-uri, `Canceled` la cancel, fără completion per EOF |
| două ferestre + voci queued | închiderea ferestrei secundare anulează numai redarea ei; cealaltă `Completed`; output neînchis; cancel pe una din două voci queued nu o taie pe cealaltă |
| device eliminat (`SDL_EVENT_AUDIO_DEVICE_REMOVED` publicat real) | `Failed`/`DeviceUnavailable`, output închis fără redeschidere; `Play` explicit → `Completed`, a doua deschidere |

Output: 2 deschideri, high-water 1920 frames (≤40 ms), 0 put failures, 0 late callbacks, 0 handler failures. Motor: 0 underrun frames, 0 clipping.
Starved requests raportate de SDL (132) includ perioadele idle dintre scenarii (coadă goală, device activ); nu sunt underrun-uri ale motorului. Schimbarea formatului device-ului este probată nativ în etapa 1 (`SdlConvertsTheMixFormat…`, device S16/44.1 kHz) — pe WASAPI shared formatul este al engine-ului OS și nu poate fi forțat din proces.

## Defecte reale găsite de comparația tap vs sink și reparate la owner (core Timbre)

1. **Start de stream cu pachet parțial.** Observație: Vorbis Streaming → 352 frames de padding (`480 − 128`). Cauză (sursă): `StreamingFeed.HasData` devenea true după primul segment de orice mărime. RED: `StreamingPrimingTests` (1792 / 2048 frames de liniște la start/după seek, `red-streaming-priming.trx`). Fix: vocea pornește și repornește după seek doar cu o coadă software completă (1920 frames) în buffer sau cu restul sursei publicat; `sourceEnded` se publică după segmentul final.
2. **Padding prematur cu coadă încă plină.** Observație după fix 1: MP3/Opus Streaming → 352 frames la blocul 5 (frame 1920), intermitent la rece; proba temporară cu device ritmat (10 ms/bloc) a localizat underrun-ul imediat după umplerea inițială. Cauză: mixerul umplea locul liber din coadă cu liniște dacă vocea n-avea un bloc întreg, deși device-ul mai avea ≥10 ms. Fix: `SoundFeed.HasBlock` + `DeferForSource` — blocul se amână cât coada mai are cel puțin un bloc; padding-ul (contorizat, fără salt de conținut) apare numai când output-ul ar seca. Proba ritmată după fix: 0 underrun în 9/9 rulări (3 codecuri × 3); proba a fost ștearsă după experiment.
3. Teste core adaptate la contractul neschimbat (padding numai la starvation reală, fără salt): `LifecycleTests.StreamingUnderrunPadsCountedSilenceWithoutSkippingContent` (bariera mutată după prima coadă completă; padding provocat prin golirea cozii) și `SoundStreamingFailureTests.UnderrunPadsCountedSilenceAndResumesWithoutSkippingSource` (drenare bloc cu bloc; snapshot înainte de reluarea sursei). Ambele verifică în continuare padding contorizat, poziția nemodificată și reluarea exactă din frame-ul blocat.

Docs sincronizate: `Cerneala.Timbre.SoundPlayback.md` (seek: o coadă completă înainte de mixare), `Cerneala.Timbre.SoundLoading.md` (pornire streaming și politica de underrun).

## Verificare (starea finală a codului)

- `timbre-full-after-deferral.trx`: `Cerneala.Tests.Timbre` 309/309 (inclusiv proba temporară, eliminată apoi; fără ea 306 teste + 3 noi priming/adaptate).
- `sdl-default.trx` 43 pass / 8 native skip; `sdl-native.trx` 52/52 (audio, parser, arhitectură, lifetime).
- `core-hosting.trx` 87/87.
- Smoke nativ direct și prin launcher publicat: PASS.
