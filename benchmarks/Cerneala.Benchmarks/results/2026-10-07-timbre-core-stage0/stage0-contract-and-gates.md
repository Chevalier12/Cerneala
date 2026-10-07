# Timbre core stage 0: contract C#/backend, seam-uri și baseline

Data: 2026-10-07. Planul sursă: `docs/plans/2026-10-03-timbre-core-runtime.md`; contractele canonice: `docs/plans/2026-10-03-timbre.md` §1.1/§7. Documentul formalizează mecanic deciziile aprobate; nu redeschide produsul.

## 1. Snapshot, baseline și caracterizare

| Element | Valoare |
| --- | --- |
| Snapshot implementare | `master` @ `82a2386f`, worktree dirty numai cu `CLAUDE.md` și planurile Timbre (user-owned, neatinse) |
| Worktree baseline | `C:\Users\lauri\Desktop\Cerneala-baseline-timbre-82a2386` (detached `82a2386f`) |
| Baseline binar ApiCompat | `bin\Release\net8.0\Cerneala.dll`, 5,519,360 bytes, SHA-256 `41BB2C2681442DAAFBAF8A243501DB0A104D168EA1EF5B78B39389E6FE7D4219` |
| Caracterizare existentă | `stage0-baseline-characterization.trx` + `.log`: ServiceRegistration/UiHostPlatformServicesIntegration/ApplicationRuntime/ApplicationResourceIntegration/ElementLifecycle/WindowRuntime, **103/103 passed**. GREEN de baseline, nu RED audio. |
| Harness nou | `stage0-timbre-harness.trx`: observatori sink/reader + arhitectură + catalog, 15/15 passed. |

## 2. Suprafața publică înghețată (namespace `Cerneala.Timbre`)

Toate tipurile sunt `public sealed` (sau abstract cu constructor `private protected`, deci neextensibile din afara core-ului), cu excepția seam-urilor menite implementării externe (`SoundReader`, `ISoundOutput`). În etapa 0 corpurile aruncă `NotImplementedException`; tipurile de date pure (`SoundReadResult`, `SoundException`, `SoundRuntimeOptions`, enum-urile) sunt complete.

| Tip | Semnătură înghețată |
| --- | --- |
| `SoundSource` | `FromFile(string path)`, `FromStream(Func<Stream> openStream, string? name = null)`, `FromReader(Func<SoundReader> openReader, string? name = null)`, `implicit operator SoundSource(string path)`, `Name` |
| `SoundReader` | abstract: `long? LengthFrames`, `ValueTask<SoundReadResult> ReadAsync(Memory<float>, CancellationToken)`, `ValueTask SeekAsync(long frame, CancellationToken)`, `Dispose()` / `protected virtual Dispose(bool)` |
| `SoundReadResult` | `readonly struct (int Frames, bool EndOfSource)`, equality |
| `SoundLoading` | `Auto`, `Preload`, `Streaming` |
| `SoundParameter` / `SoundParameter<T> where T : struct` | `Name`, `ValueType`, `DefaultValue`; ctor `(string name, T defaultValue)`; prima livrare acceptă numai `T = float` (`NotSupportedException` altfel) |
| `SoundInput<T>` | `readonly struct`; implicit din `T` și din `SoundParameter<T>`; `IsParameter`, `Parameter`, `Value` |
| `SoundModifier`, `LowPass`, `Delay` | `LowPass(SoundInput<float>? cutoff = null)`; `Delay(SoundInput<float>? time = null, SoundInput<float>? feedback = null, SoundInput<float>? mix = null)`; `Time` în secunde |
| `SoundClip` | `(SoundSource source, float volume = 1f, bool loop = false, SoundLoading loading = Auto, IEnumerable<SoundParameter>? parameters = null, IEnumerable<SoundModifier>? modifiers = null)`; `Source`, `Volume`, `Loop`, `Loading`, `Parameters`, `Modifiers` (copii read-only) |
| `SoundRuntimeOptions` | `Output`, `MaxVoices=64`, `AutoPreloadMaxBytes=1 MiB`, `MaxPreloadBytes=16 MiB`, `MaxCacheBytes=64 MiB`, `StreamingMemoryLimit=long.MaxValue`, `DelayTailCap=30 s`, `BaseDirectory=null` (→ `AppContext.BaseDirectory`) |
| `SoundRuntime : IDisposable` | `const SampleRate=48000`, `const ChannelCount=2`, ctor `(SoundRuntimeOptions? options = null)`, `IsDisposed`, `CreateScope()`, `PrepareAsync(SoundClip, CancellationToken = default)`, `Dispose()` |
| `SoundScope : IDisposable` | `Runtime`, `IsDisposed`, `Play(SoundClip clip, Action<SoundStartOptions>? configure = null, SoundHandle? handle = null)`, `CreateHandle()`, `Dispose()` |
| `SoundStartOptions` | `Volume`, `Loop`, `Set<T>(SoundParameter<T>, T)`; valid numai în timpul delegate-ului |
| `SoundHandle` | `Scope`, `Current` (ocupant non-terminal sau `null`), `Cancel()` |
| `SoundPlayback` | `Clip`, `State`, `Volume {get;set;}`, `Loop` (snapshot), `Position`, `Duration` (`TimeSpan?`), `Completion` (`Task<SoundPlaybackResult>`), `Set<T>`, `Cancel()`, `Pause()`, `Resume()`, `SeekAsync(TimeSpan)` |
| `SoundPlaybackState` | `Pending`, `Playing`, `Paused`, `Completed`, `Canceled`, `Failed` |
| `SoundPlaybackResult` | `State` (terminal), `Error` (`SoundException?`), `TailTruncated` |
| `SoundException` / `SoundErrorKind` | `SourceUnavailable`, `UnsupportedFormat`, `InvalidData`, `ResourceLimitExceeded`, `VoiceLimitExceeded`, `DeviceUnavailable` |
| `ISoundOutput` / `ISoundOutputClient` | §4 |

Exemplele §2.1 se compilează în `tests/Fixtures/TimbreConsumer` (assembly fără `InternalsVisibleTo`, fără markup/Aspect/SDL); compilarea dovedește suprafața, nu comportamentul.

### Owner access (aditiv, compilat/executat în etapa 1)

| Membru nou | Contract |
| --- | --- |
| `UIElement.Sounds : SoundScope` | owner-thread (`Root.Relay.VerifyAccess`); cere element atașat și `Root.SoundRuntime`; altfel `InvalidOperationException`. Scope lazy per lifecycle; detach îl dispune (anulează redările lui), reattach creează scope nou la următorul acces. Renderability/hide nu îl atinge. Include `Scene2D`/`SceneNode2D` (derivă din `UIElement`). |
| `UIRoot.SoundRuntime` / `UIRoot.SetSoundRuntime(SoundRuntime?)` | analog `SetImageLoader`; schimbarea runtime-ului retrage scope-urile elementelor din root. Root-ul nu deține runtime-ul. |
| `UiHostOptions.SoundRuntime` | hosted fără `Application`: `UiHost` îl aplică root-ului la construire/`SetRoot` numai dacă este non-null; runtime caller-owned. |
| `Application.SoundRuntime {get;set;}` | lazy la primul get; setter numai înainte de materializare; runtime creat de Application este dispus la exit, unul atribuit rămâne caller-owned. |
| `Application.Sounds : SoundScope` | scope application-scoped din `SoundRuntime`, dispus la exit. |
| `WindowApplicationRuntime` | la crearea contextului unei ferestre, dacă există `Application`, `root.SetSoundRuntime(application.SoundRuntime)`; output-ul rămâne lazy, nu se deschide device. |

Inventar al seam-urilor schimbate: `UIRoot` (sealed), `UiHostOptions` (sealed), `UiHost` (sealed), `Application` și `UIElement` (nesigilate; grep pe `*.cs` nu găsește niciun membru `Sound`/`Sounds` care ar fi ascuns). Callerii existenți ai constructorilor nu se schimbă; toți membrii sunt aditivi. **`IPlatformServices`, `PlatformServices` (constructor/deconstruct/equality) și `IResourceProvider` rămân neschimbate**; implementatorii existenți (fake-uri PasswordBox/TextBox/cursor/host) nu sunt atinși. Audio nu intră în service registry și nu în resource lookup.

## 3. Catalog și maparea build-time

Ownerul unic este `Timbre/Catalog/TimbreCatalog.cs` (intern, fără dependențe, numai API netstandard2.0). Core îl compilează; `Cerneala.Language` (netstandard2.0, IVT către SourceGen/LSP) îl compilează prin `<Compile Include Link>`. Maparea este identitate de cod: aceleași constante servesc validarea runtime și diagnosticele build-time ale planului markup. Generatorul nu depinde de SDL sau decoder. Notă: un assembly cu IVT atât de la core cât și de la Language ar vedea două tipuri interne omonime; niciun consumator nu le folosește împreună acum.

| Intrare | Unitate | Interval | Default |
| --- | --- | --- | --- |
| Volume (post-chain) | gain liniar | 0–1 | 1 |
| LowPass.Cutoff | Hz | 20–20000 | 1200 |
| Delay.Time | s | 0.001–2 | 0.12 |
| Delay.Feedback | ratio | 0–0.95 | 0.20 |
| Delay.Mix | ratio wet/dry liniar | 0–1 | 0.15 |

Nu există Q/Resonance/Bypass/pantă publice.

## 4. Seam-uri interne/externe înghețate

### Output (`ISoundOutput`, push cu backpressure)

- Format: float32 interleaved stereo, 48 kHz. Unități: sample = 4 bytes, frame = 2 samples = 8 bytes; `Submit` primește numai frame-uri complete.
- Runtime-ul este singurul producător: un fir dedicat „Timbre mixer” deținut de `SoundRuntime`, pornit lazy la primul start acceptat. `Open(client)` este apelat lazy pe acel fir; o excepție = device open failure.
- Coada software aparține output-ului; runtime-ul produce un bloc de 480 frame-uri (10 ms) numai când `QueuedFrames + 480 ≤ 1920` (40 ms). `QueuedFrames` = frame-uri trimise și încă neconsumate.
- Output-ul (inclusiv un callback nativ) apelează numai `NotifyCapacityAvailable()` / `NotifyDeviceLost(error)`; ambele doar semnalizează firul mixer, fără I/O, decoder, DSP, UI sau excepții aruncate înapoi.
- Clock-ul de procesare este numărul de frame-uri trimise; `consumed = submitted − QueuedFrames`. Transportul nu depinde de redraw, pump UI sau `Window.Hide`.
- Output-ul se închide numai la `SoundRuntime.Dispose` sau după device loss/open failure; niciodată la cancel/pause/seek/replacement local. Nu există clear/pause pe coada comună.

### Source/reader (`SoundReader`)

- Readerul livrează PCM canonic (stereo interleaved float32, 48 kHz); conversia formatelor sursă aparține planului decoding. `ReadAsync` scrie cel mult `destination.Length / 2` frame-uri; sample-urile non-finite sunt `InvalidData`.
- După un rezultat `(0, EndOfSource=false)`, următorul `ReadAsync` trebuie să aștepte asincron date, EOF, eroare sau anulare. Un al doilea rezultat consecutiv `(0,false)` completat sincron este violare de contract → `Failed(InvalidData)`; nu există polling/delay.
- `SeekAsync(frame)` este precis la frame decodat. Un reader per playback, creat pe worker; disposal pe worker după oprire.
- `SoundSource.FromFile`: fără I/O la declarare; calea relativă se rezolvă față de `SoundRuntimeOptions.BaseDirectory ?? AppContext.BaseDirectory` (nu working directory/fișier `.crn`); URI-uri (`://`) respinse. `FromStream`: factory per playback, stream cerut `CanRead && CanSeek`. Ambele trec printr-un seam intern de decodare detectat după conținut, **gol în core** → `Failed(UnsupportedFormat)`; planul decoding îl populează. `FromReader`: reader canonic custom, ocolește decodarea.

### Cozi, thread-uri, ownership

| Coadă / buffer | Producător | Consumator | Owner |
| --- | --- | --- | --- |
| ring PCM per playback streaming (8192 frame-uri = 64 KiB, ≤ 256 descriptori de segment; revizuit în etapa 1 din pool-ul inițial de chunk-uri) | pump-ul readerului (thread pool) | firul mixer | playback-ul |
| payload preload immutable | worker de încărcare | firul mixer (citire directă, pinned) | cache-ul runtime |
| bloc de mix (480 frame-uri, prealocat) | firul mixer | `ISoundOutput.Submit` (copiază) | runtime-ul |
| coada software a output-ului | runtime | device/sink | output-ul |

DSP și mixajul rulează numai pe firul mixer; trezirea și anularea pump-urilor streaming trec printr-un fir dispatcher al runtime-ului (etapa 3), astfel încât mixer-ul nu pune în coadă work items și nu rulează callback-uri de cancellation; comenzile API se publică sub lock-ul runtime-ului în câmpuri per voice, aplicate la începutul fiecărui bloc (granularitate 10 ms; fără alocări per comandă).

## 5. PCM produs, queued, consumat

- Cancel/pause/resume/seek/replacement/parametri/Motion afectează numai PCM produs după blocul în care comanda a fost aplicată; PCM deja trimis rămâne în bugetul ≤40 ms.
- Cancel trece instanța imediat în `Canceled` (rezultat livrat), oprește producerea sursei și a tail-ului; pump-ul/readerul sunt opriți și dispuși asincron, contorizat (`LiveReaders`, `LiveSourcePumps`).
- `Completed` cere EOF al sursei (non-loop) + tail terminat + drain observat: `consumed ≥` indexul ultimului frame trimis care conține redarea. Ultimul bloc produs nu este completion.
- Loop: EOF reia sursa fără completion; un loop peste o sursă de lungime 0 se termină `Completed` (nimic de repetat).

## 6. Configurația de start și tranzacția

Ordinea sincronă în `Play`: (1) scope nedispus, clip non-null, handle aparținând acestui scope; (2) delegate-ul rulează o singură dată pe firul apelantului, pe un `SoundStartOptions` proaspăt; (3) validare: descriptor declarat în clip, `T` corespunzător, valoare finită în intervalul efectiv (intersecția intrărilor alimentate), Volume 0–1; (4) admitere voice (activ − ocupantul înlocuit + 1 ≤ `MaxVoices`, inclusiv paused/pending/draining) → `SoundException(VoiceLimitExceeded)`; (5) abia apoi ocupantul vechi este anulat și noua identitate devine ocupant. Orice excepție la (1)–(4), inclusiv excepția aruncată de delegate (propagată nemodificat), nu creează identitate și păstrează ocupantul. Opțiunile sunt sigilate după delegate; utilizarea ulterioară aruncă `InvalidOperationException`. Failure asincron al noului ocupant nu îl restaurează pe cel vechi. Override-urile nu modifică clipul sau alte redări.

## 7. DSP, mix și valori invalide

- **LowPass:** filtru 2-pol Butterworth implementat ca SVF topology-preserving transform (Zavalishin, *The Art of VA Filter Design* rev. 2.1.2, cap. 3–4; Simper, „Linear Trapezoidal Integrated SVF”, Cytomic 2013), `k = √2`, `g = tan(π·fc/48000)`. Ales față de biquad-ul RBJ DF-II pentru stabilitate la modulație (Cutoff animat de Motion); răspunsul în magnitudine este identic cu biquad-ul bilinear prewarped. Oracle independent: `|H(e^{jω})|² = 1 / (1 + (tan(ω/2)/tan(ωc/2))⁴)` și răspunsul la impuls al biquad-ului RBJ Audio EQ Cookbook (Q = 1/√2) calculat DF-I în test. Stare per canal, per playback.
- **Delay:** linie circulară per canal, întârziere întreagă `D = round(Time·48000)` frame-uri (48–96000), `w[n] = x[n] + Feedback·w[n−D]`, `y[n] = (1−Mix)·x[n] + Mix·w[n−D]`. Schimbarea Time mută instant offsetul de citire (fără interpolare, fără promisiune click-free). Buffer dimensionat la `D` pentru Time constant și la 2 s când Time este parametru. Oracle: tren de impulsuri `(1−Mix)` la 0 și `Mix·Feedback^(k−1)` la `k·D`. Feedback ≤ 0.95 garantează stabilitatea.
- **Ordine:** sursă → modificatori în ordinea declarată → `Volume` (gain liniar post-chain) → sumă peste redări → hard clip la ±1 pe output cu contor `ClippedSamples`. Fără normalizare, compressor sau limiter.
- **Tail:** după EOF non-loop, lanțul procesează zero; tail-ul se termină când vârful ieșirii pre-Volume pe o fereastră de `max(D, 480)` frame-uri este < `1e-6`; cap `DelayTailCap` (30 s) după EOF → `Completed` cu `TailTruncated = true`. Clip fără lanț: fără tail. Pause îngheață tail-ul; cancel îl oprește.
- **Valori invalide:** setterii și startul resping NaN/Infinity/out-of-range cu `ArgumentOutOfRangeException`, fără clamp/ramp. Callback-ul nativ nu aruncă niciodată. Puntea Motion (planul Motion) prinde respingerea, oprește numai animația acelui parametru, păstrează ultima valoare validă și raportează diagnostic.

## 8. Mașina de stări și transport

| Din | Eveniment | În |
| --- | --- | --- |
| — | `Play` acceptat | `Pending` (identitate imediată) |
| `Pending` | primul bloc care conține PCM al redării | `Playing` |
| `Pending`/`Playing` | `Pause()` | `Paused` (Pending: pregătirea continuă bounded, primul PCM blocat) |
| `Paused` | `Resume()` | `Playing` dacă a pornit, altfel `Pending`; fără restart |
| non-terminal | `Cancel()` / dispose scope / replacement | `Canceled` |
| non-terminal | I/O, decoder, `InvalidData`, resurse, device | `Failed` |
| `Playing` | EOF + tail + drain | `Completed` |

`Pause` repetat și `Resume` pe `Playing`/`Pending` sunt no-op. Pe terminal: `Cancel` no-op idempotent; `Pause`/`Resume`/`SeekAsync`/`Volume`/`Set` aruncă `InvalidOperationException`. `SeekAsync(t)`: `t < 0` sau `t > Duration` cunoscută → `ArgumentOutOfRangeException` sincron; altfel cerere asincronă, ultima câștigă (cererea anterioară → task anulat), cancel/terminal anulează cererea pendinte; paused rămâne paused, activ continuă după pregătire; resetare DSP numai la seek reușit; Loop natural păstrează DSP. `Position` = poziția sursei procesate (ținta seek-ului după completare), nu DAC. `Duration` = `LengthFrames/48000`, `null` cât timp lungimea este necunoscută. `Completion` nu se termină niciodată faulted, completează pe thread pool (`RunContinuationsAsynchronously`), independent de UI pump; un `await` pe contextul UI revine prin Relay.

## 9. Loading, resurse, device

- `Auto`: preload dacă `LengthFrames·8 ≤ AutoPreloadMaxBytes`, altfel sau la lungime necunoscută streaming. `Preload`: lungime cunoscută peste `MaxPreloadBytes` → refuz înainte de alocare; lungime necunoscută → citire incrementală până la limită, apoi refuz. Refuzul = `Failed(ResourceLimitExceeded)` pentru Play, excepție `SoundException` pentru `PrepareAsync`.
- Cache immutable keyed pe identitatea sursei (cale completă pentru fișiere, instanța factory-ului altfel), ≤ `MaxCacheBytes`; payload pinned de redările active; eviction LRU numai neprinned; admiterea care nu încape după eviction → `ResourceLimitExceeded`. Fără I/O la declararea clipului; `PrepareAsync` este pregătirea anticipată explicită.
- Streaming: pool fix per playback, memorie neproporțională cu durata, fără plafon numeric implicit; rezervările pe `StreamingMemoryLimit` (implicit `long.MaxValue`, fără alocarea capacității) se aplică numai limitei configurate explicit. Raportare separată: cache/preload, buffere streaming, stare DSP.
- `MaxVoices` = 64 non-terminale inclusiv paused/pending/draining; overflow respinge sincron, fără voice stealing.
- Underrun: silence padding contorizat (`UnderrunFrames`) pentru acea redare, poziția și DSP rămân pe loc, fără salt de conținut.
- Device: numai output-ul configurat, deschis lazy; fără output configurat sau open failure/device loss → redările afectate `Failed(DeviceUnavailable)`, fără retry automat; un `Play` explicit ulterior redeschide. Core nu are autoplay și nu are concept de preview; „Preview disabled implicit cu enable explicit” aparține PreviewHost/planului markup.

## 10. Test project, observatori și clasificare

`tests/Cerneala.Tests.Timbre` (net8.0, xunit) conține `DeterministicSoundOutput` (sink care nu mixează: înregistrează PCM, expune queued/consumed/max queued, consumă numai la cererea testului și notifică clientul), `DeterministicSoundReader`/`DeterministicSoundSourceFactory` (semnal închis-formă, partiții neregulate, gate de I/O, failure injectat, contoare reads/seeks/live readers). `SoundRuntimeDiagnostics` (intern) fixează contoarele planificate pentru motor: start/pause/resume/seek/supersede/loop/cancel/completion/failure, frame-uri trimise/consumate, underrun, clipping, publicații de parametri, voices, readers/pumps, output, cache, buffere streaming, stare DSP. Observatorii sunt verificați separat (`HarnessObserverTests`); dovada harness → motor real se închide în etapa 1.

Clasificare: staging-ul compilabil (consumer + teste de arhitectură/catalog) este distinct de RED runtime. Lipsa unui tip, a unui codec sau a unui fișier nu este raportată ca eșec audio; RED-urile de comportament se confirmă în etapa 1 pe motorul concret.
