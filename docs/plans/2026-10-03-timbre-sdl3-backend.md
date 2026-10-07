# Plan: Timbre — output SDL3 și lifetime nativ

> Data: 2026-10-07
> Status: finalizat
> Dependențe: [core/runtime](2026-10-03-timbre-core-runtime.md); [decodare/streaming](2026-10-03-timbre-decoding-streaming.md) pentru acceptarea integrată cu toate formatele
> Scop: output PCM prin SDL3 core din Graphix, fără SDL_mixer, separat de GPU și de lifetime-ul unei redări.

## 1. Puncte de integrare de verificat

- Identifică bootstrap-ul real în SdlGpuApplicationBackend și traseul NativeSdlApi/SdlWindowPlatform; nu presupune că un factory nefolosit este ownerul inițializării.
- Inspectează ISdlApi, NativeSdlApi și FakeSdlApi, init Video/Events și SdlPlatformLifetime. Device/callback audio se drenează înainte de SDL.Quit.
- Verifică pachetele Graphix-CS/Graphix.Native efective, pin-urile, payload-ul distribuit și ABI-ul API-urilor SDL3 audio; declarațiile managed nu certifică threading-ul sau teardown-ul.
- Protejează consumatorii direcți InitializeVideo/Quit, inclusiv SdlGpuShaderArtifactTests, fără inițializare audio implicită în teste GPU.
- Verifică native opt-in CERNEALA_SDL_NATIVE_TESTS=1, serializarea SdlNativeTestCollection, SmokeOptions și Invoke-SdlGpuSmoke înainte de integrarea modului Timbre.
- Păstrează granițele API verificate de SdlArchitectureTests; tipurile binding-ului SDL nu se expun din API-ul Timbre.

SDL3 documentează [streams/device mixing/conversion](https://wiki.libsdl.org/SDL3/CategoryAudio), [callback on-demand](https://wiki.libsdl.org/SDL3/SDL_SetAudioStreamGetCallback) și [callback ABI/thread behavior](https://wiki.libsdl.org/SDL3/SDL_AudioStreamCallback). Aceste surse justifică probele de interop; nu dovedesc că un callback managed al nostru este rooted/disposed corect.

## 2. Owner și graniță

Backend-ul implementează contractul output înghețat în core. Deține init audio, device/stream, callback lifetime, negocierea/conversia formatului și erorile native. Timbre deține redările/modificatorii; backend-ul nu evaluează Aspect, nu citește arborele UI și nu folosește BeginPrism/EndPrism sau sesiuni de drawing.

Compoziția este **mix PCM final din Timbre → SDL3 core AudioStream → dispozitiv**. Nu se deschide un stream/device per clip pentru a delega mixajul voices SDL și nu se folosește SDL_mixer pentru redare sau decodare. Formatul intern float32/stereo/48kHz și limitele sunt aprobate în index; push/pull se alege pe probe în core în autoritatea delegată; acest adaptor livrează blocurile, fără o a doua implementare DSP/playback.

Cale nativă candidată documentată: [SDL_OpenAudioDeviceStream](https://wiki.libsdl.org/SDL3/SDL_OpenAudioDeviceStream) deschide device + stream bound și pornește pauzat; alimentarea folosește [SDL_PutAudioStreamData](https://wiki.libsdl.org/SDL3/SDL_PutAudioStreamData), iar pornirea cere SDL_ResumeAudioStreamDevice. DestroyAudioStream închide și device-ul pe această cale. Alternativa open/create/bind explicit este permisă numai după fixarea ownerilor în etapa 0; nu se amestecă cele două politici de close. Callback-ul poate rula de pe orice fir, cu stream lock ținut, și nu poate distruge propriul stream. Acestea sunt contracte native de verificat prin binding/probe, nu API C# fictiv sau output Timbre deja executat.

API-ul utilizatorului C# și codul generat în factory/partial folosesc core-ul Timbre, nu acest adaptor. SDL3 consumă output-ul aceluiași motor indiferent dacă pornirea vine din C# sau markup; SoundClip/SoundPlayback/SoundHandle nu expun device handles sau tipuri native. Smoke-ul C# din acest plan consumă API-ul public înghețat în core, iar paritatea declarativă se verifică ulterior în planurile markup/Motion, fără dependență circulară.

Device implicit, output lazy la prima nevoie reală; SoundClip declaration/ref nu îl deschide. Device absent/open failure/loss produce failure explicit al redărilor afectate; fără reconectare/migrare/retry automat, Play ulterior explicit poate încerca din nou. Se verifică software queue≤40ms și drain, nu se pretinde măsurare DAC.

Pause/Resume/Seek/Loop sunt transport per playback în motorul Timbre. Nu apelează pauza/clear pe output-ul comun pentru un singur sunet și nu creează device per handle; queued PCM poate rămâne audibil în bugetul aprobat.

Device/output este comun ferestrelor runtime-ului. Scope-uri audio per comportament/element sunt separate; închiderea unei ferestre sau anularea unui clip nu închide dispozitivul altora. Shutdown terminal oprește publicațiile, anulează/drain workers, desprinde și drenează callback-urile, distruge streams/device, apoi permite SDL.Quit. Ordinea și excepțiile sunt observate prin fake și native probes.

## 3. Fișiere estimate

- **Noi:** `Cerneala.Platforms.Sdl3/Audio/` (output/device/callback/interop), forma exactă a seam-ului audio decisă în etapa 0; nu se umple ISdlApi cu API-uri inutile pentru GPU.
- **Existente:** `Interop/ISdlApi.cs`, `Interop/NativeSdlApi.cs`, `Hosting/SdlPlatformLifetime.cs`, `Hosting/SdlWindowPlatform.cs`, bootstrap-ul real și fake-ul dacă seam-ul aprobat le modifică.
- **Teste:** SdlPlatformLifetimeTests/NativeSdlLifetimeTests, SdlArchitectureTests și SdlGpuShaderArtifactTests ca GREEN de compatibilitate; noi SoundOutput/NativeSoundOutput teste și instrumentare callback/resource în `tests/Cerneala.Tests.SdlGpu/`.
- **Smoke:** `tests/Cerneala.SdlGpuSmoke/SmokeOptions.cs`, program/scenarii, `Tools/scripts/Invoke-SdlGpuSmoke.ps1`, `SdlGpuSmoke.Common.ps1`, `.github/workflows/desktop-backends.yml`.
- **API/docs:** paginile public/protected afectate și manifest; asset/notice contract după decoder selection.

## 4. Etapele de implementare

### Etapa 0 — interop și matrice de runner

- [x] Formalizează default-device/lazy output, format48k/stereo/float32 și init/subsystem shutdown contract aprobate; inspectează semnăturile și calling convention ale binding-ului pinneat, dimensiuni AudioSpec, channel layout și byte/frame arithmetic. Nu migra la alt binding doar pentru comoditate.
- [x] Validează importurile SDL3 core efectiv alese (AudioSpec pointer/struct, callback cu user/stream/additional_amount/total_amount, bool și calling convention). Stabilește init Audio și reference balancing față de Video/Events, open/bind/resume/close și calea unică de ownership; nu introduce MIX_Init/MIX_Quit.
- [x] Fixează implementarea output-ului comun: push sau pull, bounded queued PCM conform contractului core, variable byte requests și conversie/resampling SDL. Callback-ul se limitează la operațiile aprobate de output/procesare core; nici citire de fișier, nici decoder, nici sampling Motion/arbore UI pe firul audio. Independența de redraw/Window.Hide nu creează un clock Motion nou.
- [x] Probează callback rooting/delegate ABI și stream conversion în harness izolat cu timeout și cleanup, fără permanent callback lăsat în background. Arhivează source/input/conditions/raw results.
- [x] Inventariază toți callerii ISdlApi/lifetime/factory care chiar vor fi modificați și consumers GPU/fakes/native test; fixează migrarea sau contractul neschimbat pentru fiecare. Nu modifica inițializarea video/global quit fără această acoperire.
- [x] Rulează GREEN SdlPlatformLifetimeTests/NativeSdlLifetimeTests aplicabile și îngheață init/quit/thread counts; opt-in/skip se raportează separat.
- [x] Consemnează host/device/format Windows disponibil pentru real output și dummy separat. Politica din index §7: non-Windows native/runtime audio probes N/A, nu se lansează pe Linux/macOS/alte RIDs sau runners remote. Șase RID host-side publish/asset checks rămân required; Windows device unavailable rămâne blocked, nu primește waiver implicit.
- [x] Definește observatori pentru streams/devices/callbacks, frames/queue depth/underruns, teardown ordering și late callbacks. O metrică unavailable se instrumentează înainte de gate, nu se fabrică retrospectiv.
- [x] Documentează nivelul real al progresului observabil: produced/queued/dequeued software nu certifică momentul auzit la DAC. Aplică completion/drain și buget≤40ms din core/index, cu instrumentele disponibile; nu există prag fizic DAC aprobat. Validarea auditivă umană Windows este gate final separat obligatoriu, nu dedusă din consumed counters.

**Gate etapa 0**

- [x] Interop SDL3 core/pins/format și ownerii init/quit sunt validați în scope-ul probei; Windows și observatorii sunt executabili sau blocked explicit; non-Windows N/A conform politicii de platformă, iar assets sunt verificabile pe host. Output-ul primește mixul final din core; SDL_mixer nu este dependență implicită și probele lui nu substituie aceste gates.

### Etapa 1 — output și shutdown

- [x] Adaugă RED-uri faithful pentru output frame ordering, exact lifecycle și fake concurrent callback/drain înainte de implementarea behavior; păstrează GREEN video/GPU lifetime.
- [x] Implementează output-ul, inițializarea audio și integrarea runtime conform contractului core, cu buffers/callback rooted și fără read/decode/Aspect/UI access pe callback.
- [x] Testează variable request sizes în bytes, negotiated formats/resampling, callback cu additional_amount zero și cereri diferite, Put failure și error la fiecare pas init/open/bind/resume, dispose repetat și callbacks de pe fire diferite conforme ABI. PutAudioStreamData returnează bool, nu un număr de bytes/short write; observerul frames/queue nu inventează alt contract.
- [x] Testează cu output queue observabilă cancel/pause/resume/seek/loop/replacement/param updates și EOF/completion conform contractului core, inclusiv două voices deja mixate. Audio rămas queued respectă latența aprobată; cancel/pause/seek local nu execută ClearAudioStream/pause-device pe mixul comun și nu taie cealaltă redare; latest-seek completion nu publică din generația veche. Verifică final drain separat de ultimul bloc trimis.
- [x] Testează cancel/dispose în timp ce callback-ul rulează, callback după cererea de shutdown și două ferestre/redări: drenează înainte de free/SDL.Quit, fără deadlock, double free sau închiderea output-ului la cancel local.
- [x] Confirmă no-op frame/param updates nu produc layout/render invalidations prin service wiring; audio nu introduce work GPU doar pentru a fi procesat.
- [x] Probează alimentarea/consumul audio în timp ce UI nu face redraw și o fereastră este ascunsă sau închisă, dar runtime-ul și alt scope sunt active. Window.Hide poate suspenda Motion sampling conform indexului, nu anulează automat output-ul comun. Folosește bariere/counters, nu sleep ca dovadă de progres.

**Gate etapa 1**

- [x] Fake lifecycle/concurrency și native output probe trec; late callbacks nu ating memorie/instanțe eliberate, iar init/quit GPU existente rămân compatibile.

### Etapa 2 — smoke audio observabil și codecuri

- [x] Adaugă explicit modul smoke planificat `timbre` în parser, dispatcher și launcher ValidateSet; testează unknown/missing args și paths. Numele nu este switch existent înainte de această etapă.
- [x] Adaugă scenariu native C# pentru clip simplu/modificat, overlap/replacement/cancel/Pause/Resume/Seek/Loop și două ferestre pe Windows, inclusiv pause Pending și două voices cu PCM deja queued. Înregistrează structured diagnostics și PCM tap de test application-owned înainte de output, fără OS audio/screen capture substitut. Fixture-ul declarativ este integrarea ulterioară din planul markup, nu prerequisite pentru backend.
- [x] Integrează toate formatele aprobate și streaming-ul cu device real Windows; PCM tap compară pipeline-ul după decoder/DSP cu sink-ul deterministic, nu certifică singur ieșirea fizică.
- [x] Probează device absent/open failure și schimbarea formatului/dispozitivului conform politicii aprobate; nu introduce retry/delay/fallback ascuns pentru a face smoke-ul verde.

**Gate etapa 2**

- [x] Noul smoke mode/parser/launcher și PCM/diagnostics trec; native device delivery este separat de dummy interop. Codec pipeline și multiwindow lifetime sunt observabile prin C#; input-ul și event routing declarativ se verifică în planul markup și matricea integrată din index.

### Etapa 3 — distribuție, cost și verificare completă

- [x] Integrează corpusul Timbre și smoke/audio environment Windows explicit în workflow-ul existent fără a rula/publisha remote în această sarcină; nu adaugă noi joburi audio runtime non-Windows și nu dezactivează testele legacy. Nu interpretează Xvfb/Vulkan ca setup audio.
- [x] Rulează probe native pe Windows-ul disponibil; Linux/macOS/alte runtime RIDs N/A conform politicii de platformă, fără execuție. Verifică host-side restore/build/publish/assets/notices pentru toate cele șase RIDs: win-x64/win-arm64/linux-x64/linux-arm64/osx-x64/osx-arm64; nu pretinde native parity din cross-publish.
- [x] Măsoară underruns, queue high-water, CPU/allocations și resource churn pentru streaming/modifiers/overlap conform protocolului indexului; load matrix32voices/4streams și pragurile sunt deja aprobate în index §7; consemnează rezultatele fără relaxare post-hoc.
- [x] Documentează API/lifetime/threading/capabilities/errors și actualizează canonical docs/manifest; verifică strict ApiCompat pentru suprafața aditivă.
- [x] Rulează proiectul SDL complet cu native opt-in, corpusul Timbre, sourcegen consumer builds, full solution/manifest și smokes vizuale existente relevante; inspectează diff și cleanup.
- [x] Rulează SdlArchitectureTests împreună cu testele lifecycle și direct NativeSdlApi GPU consumers inventariate: API-ul audio nu expune binding types și nu lărgește accidental bootstrap-ul GPU. Extinde filtrele native Windows pentru noile teste audio, fără a cere probe non-Windows în afara scope-ului; rularea suitei fără opt-in nu dovedește execuția lor.

**Gate etapa 3**

- [x] Distribuția și output-ul sunt verificate pe platformele cerute, compatibility/GPU nu sunt degradate, gates cost/API/docs sunt închise. Windows missing/skip required nu este pass; native/runtime non-Windows N/A conform politicii de platformă, assets non-Windows rămân distincte.

## 5. Comenzi și definiția de gata

Existente:

```powershell
dotnet test .\tests\Cerneala.Tests.SdlGpu\Cerneala.Tests.SdlGpu.csproj -c Release --filter "FullyQualifiedName~SdlPlatformLifetimeTests|FullyQualifiedName~NativeSdlLifetimeTests|FullyQualifiedName~SdlArchitectureTests|FullyQualifiedName~SdlGpuShaderArtifactTests"
```

După etapa 2, cu build/restore făcute și native environment configurat:

```powershell
dotnet run --project .\tests\Cerneala.SdlGpuSmoke\Cerneala.SdlGpuSmoke.csproj -c Release --no-build --no-restore -- --mode timbre --artifacts artifacts/timbre/native --no-screenshot
```

Opt-in nativ și restaurarea environment-ului sunt job-scoped. Native tests rulează serializat. Audio smoke fără screenshot nu cere captură vizuală; orice captură Cerneala cerută ulterior este numai Window.SaveScreenshot.

- [x] Device/stream/callback/init/quit respectă lifetime-ul comun și nu sunt cuplate la redraw sau cancel local.
- [x] Output-ul SDL3 core livrează PCM mixat de Timbre fără SDL_mixer și verifică format/queue/latency/drain separat de DSP/decoder conformance; niciun defect de output nu este mascat prin clear global sau ownership duplicat.
- [x] Output real, streaming și format corpus, multiwindow/failure/cancel/pause/resume/seek/loop și bugetele aprobate sunt verificate cu instrumentare reală.
- [x] API/docs/assets/notice/full-suite gates și politica de platformă sunt închise prin pass sau waiver explicit autorizat. Un gate obligatoriu blocked păstrează planul incomplet; raportarea lui nu este acceptare și nu produce parity fictivă.
