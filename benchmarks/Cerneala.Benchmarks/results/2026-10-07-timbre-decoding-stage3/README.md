# Timbre decoding stage 3: distribuție și integrare

Data: 2026-10-07. Plan: `docs/plans/2026-10-03-timbre-decoding-streaming.md`, etapa 3. Host: Intel Core i5-9300H, Windows 10.0.26200 (Windows 11 Pro), .NET 8.0.30, SDK 10.0.400.

## Distribuție (host-side, șase RID-uri)

`asset-check-six-rids.json`: `Tools/scripts/Test-TimbreDecoderAssets.ps1` publică framework-dependent consumer-ul extern `tests/Fixtures/TimbreConsumer` (referă `Cerneala.csproj`, fără IVT) pentru win-x64, win-arm64, linux-x64, linux-arm64, osx-x64, osx-arm64 și verifică: `Cerneala.dll`, `NVorbis.dll` (0.10.5, SHA-256 `d4fd7a6a…`), `Concentus.dll` (2.2.2, `3a1dcfcf…`) prezente cu hash identic pe toate RID-urile; `Cerneala.Timbre.THIRD-PARTY-NOTICES.txt` prezent; nicio bibliotecă nativă de codec (opus/vorbis/ogg/mpg123/lame/speex/SDL_mixer). NLayer e cod vendorizat în `Cerneala.dll`. Numărul diferit de fișiere între RID-uri vine din asset-urile native SkiaSharp/HarfBuzz existente, nu din decodoare.

Execuția runtime/codec pe Linux și macOS: **N/A conform politicii de platformă** (index §7) — nu a fost lansată și nu e raportată ca PASS. `.github/workflows/desktop-backends.yml` adaugă: trigger pe `Timbre/**`, `tests/Cerneala.Tests.Timbre/**`, consumer și script; corpusul Timbre numai pe runner-ul Windows; verificarea de asset-uri/notice pe RID-urile fiecărui job (packaging, nu execuție). Workflow-ul nu a fost declanșat (fără push).

## Integrare cu sink-ul core

`SoundDecoderIntegrationTests`: MP3, Vorbis, Opus și WAV, redate în streaming în buclă prin `LowPass` + `Delay` cu volum 0.7, egale (toleranța DSP a core-ului, 2e-5) cu oracle-ul DSP independent (biquad RBJ în formă directă I + delay de referință, dublă precizie) aplicat pe PCM-ul decodat repetat — peste segmentele de stream și cusătura buclei; zero underrun; un singur output deschis. `TimbreArchitectureTests.DecoderAdaptersOnlyProducePcmAndOwnNoOutputMixerOrEffects`: niciun tip din `Cerneala.Timbre.Decoding` (inclusiv NLayer vendorizat) nu referă output, client de output, runtime, scope, playback, modificatori, feed sau lanț DSP, și toate sunt interne. Acceptarea cu dispozitiv SDL3 aparține planului backend.

## Cost (protocolul din index §7)

`Invoke-TimbreDecodingCost.ps1` rulează 5 procese secvențiale; fiecare execută (1) gate-ul de cost aprobat al core-ului (`TimbreCoreBenchmarkRunner`: 32 de voci în buclă, LowPass + Delay, blocuri de 480 de frame-uri, 1000 warmup + 10 000 măsurate, dispozitiv cu ritm de ceas real) cu cele 4 voci streaming decodând fișiere reale (`mp3-long-22050-mono-cbr64`, `vorbis-long-22050-mono-q2`, `opus-long-mono-32k`, `mp3-mpeg1-44100-stereo-cbr128`), apoi (2) `TimbreDecodingBenchmarkRunner`. Pragurile sunt cele aprobate, fixate înaintea măsurării; nimic nu a fost relaxat.

### Gate (`campaign/`)

| Proces | Mix P50 | P95 | P99 | Max (ms) | Alocări în calea realtime | Coadă max | Underrun dispozitiv | Underrun voci | Play pregătit → primul PCM P95 (ms) |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 0.29 | 0.36 | 0.43 | 1.53 | 0 | 1920 | 0 | 0 | 16.02 |
| 2 | 0.29 | 0.35 | 0.43 | 1.95 | 0 | 1920 | 0 | 0 | 16.17 |
| 3 | 0.26 | 0.30 | 0.33 | 1.50 | 0 | 1920 | 0 | 0 | 16.21 |
| 4 | 0.26 | 0.30 | 0.33 | 1.83 | 0 | 1920 | 0 | 0 | 16.02 |
| 5 | 0.26 | 0.29 | 0.33 | 1.93 | 0 | 1920 | 0 | 0 | 16.02 |

Praguri: P99 < 2.5 ms (25% din blocul de 10 ms), 0 alocări steady-state, coadă ≤ 1920 frame-uri (40 ms), 0 underrun, P95 < 50 ms. Verdict: **PASS** pe toate procesele; variația P99 max/min 1.33.

`campaign-disturbed/`: o rulare anterioară a aceluiași protocol a dat FAIL pe procesul 5 cu 919 frame-uri de underrun la dispozitiv (underrun la voci 0, mix max 2.34 ms), în timp ce bucla mea de așteptare lansa `powershell` cu interogări CIM la fiecare 20 s pe aceeași mașină. Atribuirea la această perturbare e o inferență, nu e demonstrată; rularea a doua, fără nicio sarcină paralelă, e cea raportată. Măsurătoarea de pornire streaming din acea rulare era și greșită (citea timestamp-ul primului PCM înainte să existe) și a fost corectată.

### Decodare (worker), memorie și pornire (`campaign/summary.json`)

| Fișier | Bloc 480 fr. P99 (µs, max din 5) | xRT min | Deschidere max (ms) | Alocări worker / bloc (B) | Heap live peak (B) | Rezervare (B) |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| WAV 22.05 kHz mono 2 s | 526 | 172 | 28.4 | 591 | 202 304 | 262 144 |
| WAV 22.05 kHz mono 300 s | 196 | 392 | 1.5 | 72 | 199 528 | 262 144 |
| MP3 22.05 kHz mono CBR 300 s | 796 | 127 | 1.6 | 75 | 258 408 | 524 288 |
| MP3 44.1 kHz stereo CBR 128 | 1054 | 80 | 0.9 | 427 | 210 056 | 524 288 |
| MP3 22.05 kHz mono VBR | 935 | 136 | 8.5 | 635 | 325 416 | 524 288 |
| Opus mono 32 kbps 300 s | 216 | 283 | 0.4 | 11 204 | 183 496 | 524 288 |
| Opus mono 24 kbps | 458 | 232 | 9.0 | 11 212 | 209 168 | 524 288 |
| Opus stereo 96 kbps | 379 | 149 | 0.3 | 18 484 | 219 512 | 524 288 |
| Vorbis 22.05 kHz mono 2 s | 612 | 165 | 26.5 | 2 010 | 717 136 | 2 097 152 |
| Vorbis 44.1 kHz stereo q4 | 473 | 109 | 10.9 | 4 950 | 999 760 | 2 097 152 |
| Vorbis 22.05 kHz mono 300 s | 391 | 215 | 5.4 | 1 406 | 706 312 | 2 097 152 |

Pornire streaming (Play → primul PCM la un output gol, 20 de eșantioane/proces), P95 max din 5: MP3 300 s 3.18 ms, Opus 300 s 2.08 ms, Vorbis 300 s 5.64 ms. Fără prag aprobat (gate-ul index acoperă numai clipuri pregătite).

Interpretare: decodarea unui stream costă cel mult ~1 ms la P99 pe bloc de 10 ms, pe worker, nu în calea realtime; heap-ul live nu crește cu durata (2 s ≈ 300 s per codec) și rămâne sub rezervări. Alocările de worker sunt raportate, nu gate (index §7 exclude decoder-worker): Opus alocă ~11–18 KB pe bloc în interiorul Concentus (garbage gen0; gate-ul cu 4 stream-uri decodate, inclusiv Opus, a rămas fără underrun), Vorbis 1.4–5 KB (pachete NVorbis), WAV/MP3 scurte mai mult din cauza resampler-ului recreat la fiecare buclă. Deschiderile cele mai lungi (8–28 ms) sunt prima utilizare a unui codec în proces (JIT și tabele statice); după aceea WAV, MP3 și Opus se deschid sub 2 ms, iar Vorbis 5–11 ms, pentru că își construiește codebook-urile din antetul fiecărui fișier.

## API, documentație, compatibilitate

- `api-compat.proj` (strict, cu verificarea numelor de parametri) contra baseline-ului propriu al inițiativei: worktree detașat `C:\Users\lauri\Desktop\Cerneala-baseline-decoding-56755a5` la `56755a50` (planul core finalizat), `Cerneala.dll` SHA-256 `4fd8349f…37850`. `api-compat.suppressions.xml` suprimă explicit numai adaosurile aprobate: tipurile `SoundMemoryBudget`, `SoundMemoryReservation` și overload-urile `SoundSource.FromStream/FromReader(Func<SoundMemoryBudget, …>)`. Exit 0 (`api-compat.log`), fără eliminări, schimbări de semnătură sau suppression-uri inutile.
- Pagini canonice actualizate în etapele 0–3: `SoundMemoryBudget`, `SoundMemoryReservation` (noi, în manifest), `SoundSource` (matricea, erorile, gapless, notice), `SoundErrorKind`, `SoundReader` (lungime declarată), `SoundPlayback` (seek fără underrun, durată învățată, anulare cu I/O blocată), `SoundRuntime` (`PrepareAsync` pentru streaming), `SoundRuntimeOptions` (`StreamingMemoryLimit`); exemplele noi compilate în `tests/Fixtures/TimbreConsumer/DocumentationExamples.cs`.

## Suite

- `stage3-timbre.trx`: `Cerneala.Tests.Timbre` 304/304.
- `full-solution.log`: `dotnet tool restore`, `dotnet restore Cerneala.slnx`, `dotnet build Cerneala.slnx -c Release --no-restore`, `dotnet test Cerneala.slnx -c Release --no-build --no-restore -m:1` — rezultat în secțiunea de mai jos.

Rezultat `full-solution.log`: build Release fără erori (avertismentele existente nu provin din codul acestui plan), toate proiectele de test trec — Cerneala.Tests 4195 (2 skip), SourceGen 625, SdlGpu 563 (235 skip native opt-in), Timbre 304, Language 259 (1 skip), Scene2DImporters 173, Scene2DPackages 104, VisualStudio 47, LanguageServer 40, SceneVillage 36 (8 skip), Tetris 31, PreviewHost 17; `FULL-SOLUTION-EXIT 0`. Testul `Stage6ReleaseHarnessTests.ApiDocumentationManifestIsValidAndReferencesExistingFiles` trece. Filtrul planului (`SoundDecoder|SoundStreaming`): 170/170.

Curățenie: niciun proces de probă/benchmark/testhost rămas; testele temporare de diagnostic (`ScratchSeekProbe`, `ScratchShortReader`, instrumentarea `TIMBRE_STRESS_ONLY`) au fost eliminate; worktree-ul baseline rămâne pentru reproducerea ApiCompat. Spațiile de final moștenite din sursa NLayer și din licența Concentus au fost eliminate (`git diff --check` curat).

Validarea auditivă umană (index §7) rămâne o poartă separată, neefectuată aici.
