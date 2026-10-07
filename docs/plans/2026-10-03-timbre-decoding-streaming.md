# Plan: Timbre — WAV, MP3, Ogg/Vorbis, Ogg/Opus și streaming

> Data: 2026-10-07
> Status: finalizat
> Dependență: [core/runtime](2026-10-03-timbre-core-runtime.md), cu source-reader/state machine înghețate
> Scop: surse SoundClip preload/streaming bounded, seek precis și loop întreg, cu anulare reală; selecție decoder autonomă condiționată de dovezi.

## 1. Contract tehnic

Contractul canonic este [index §1.1](2026-10-03-timbre.md#11-contracte-și-limitele-autonomiei). Cerințele de mai jos sunt de implementat și verificat, nu rezultate de teste.

### Formate, surse și ownership

Se livrează WAV/PCM, MP3, **Ogg/Vorbis și Ogg/Opus**, preload/streaming și **Seek/Loop** pe toate aceste formate. Ogg nu este un codec; suportul numai pentru Vorbis nu satisface cerința Ogg/Opus.

Matricea este mono/stereo 8–192 kHz unde codec-ul permite, WAV RIFF little-endian PCM 8/16/24/32-bit și float32, MP3 CBR/VBR, un singur elementary stream Ogg Vorbis sau Opus. RIFX/RF64/chained/multiplexed/surround/non-seekable/HTTP sunt excluse. Opus pre-skip/end trimming și metadata MP3 gapless, când există, sunt respectate.

Source este fișier local sau factory de stream seekable. Căile relative se rezolvă față de directorul aplicației, iar formatul este detectat după conținut, nu după extensie.

Pregătirea/decodarea rulează în afara callback-ului audio și nu blochează UI pentru citirea întregului fișier. Readerul, poziția și cancellation sunt per playback. Reutilizarea unui SoundClip nu partajează decoderul mutabil între voices. Preload poate partaja payload immutable cu lifetime decis în core; streaming nu poate fi implementat prin cache al întregului fișier comprimat.

Se integrează **decodere existente**; nu se scriu MP3/Vorbis/Opus sau drivere audio de la zero. Adaptoarele livrează PCM readerului core, nu pornesc propriul device/mixer. SoundClip/API-ul public și codul generat nu expun tipurile bibliotecii decoder. Timbre deține DSP/mix, iar SDL3 core consumă mixul PCM final, nu fișierul comprimat.

### Loading, memorie și admitere

Loading public: Auto/Preload/Streaming și pregătire anticipată asincronă. Auto folosește preload până la **1 MiB PCM decodat**, streaming peste prag sau când dimensiunea este necunoscută. Preload are limita **16 MiB/clip**, iar cache-ul immutable **64 MiB/runtime**, configurabile conform core/runtime. În modul Streaming, primul output apare înaintea citirii întregului fișier.

Reader/decoder/buffere streaming nu au plafon numeric obligatoriu implicit și memoria lor nu crește proporțional cu durata audio. Nu se introduce un plafon implicit arbitrar. Calificarea cere streaming incremental, storage neproporțional cu durata, consum observat cu scope declarat, cleanup, codec/PCM/transport și distribuție/licențe. Justificarea exhaustivă CLR/CRT/OS/TLS/stack nu este prerequisite pentru selecție.

Se raportează separat memoria live observată a readerului/decoderului/bufferelor, inclusiv temporare/indexuri/caches/shared atribuibile, DSP/preload/cache, heap-ul global .NET, garbage-ul încă necolectat și costurile neobservabile. Măsurătorile parțiale nu sunt promisiune de RAM/RSS total sau bound fizic.

Pentru limite **configurate explicit**, o sursă validă care depășește bugetul este refuzată înaintea alocării excesive: Play acceptat → Failed cu diagnostic ResourceLimitExceeded, PrepareAsync → eșec explicit. Nu se raportează corrupt/unsupported/EOF, nu se reduce arbitrar matricea și nu se introduce fallback. Admiterea se bazează pe contabilizarea resurselor efective sau conservator justificate ale implementării.

Pentru surse custom, declararea și rezervarea memoriei live preced alocarea, inclusiv pentru Source I/O. La eliberare, storage-ul este retras înaintea rezervării asociate. Garanția se aplică adaptoarelor Timbre și implementărilor cooperative, nu codului arbitrar interceptat automat. Compatibilitatea factory-urilor fără buget nu certifică automat contabilizarea; suprafața aditivă și lifetime-ul rezervării cer implementare, regresii și API docs înainte de gate.

### Transport

SeekAsync este absolut în timpul sursei și precis prin decoded-frame discard, nu doar byte/frame comprimat; Duration poate fi necunoscută temporar. Pause păstrează reader/poziție și permite numai pregătire bounded; seek păstrează paused/active, latest-request wins, iar cancel/replacement retrage și cererile stale. Loop întreg repetă sursa cu trimming, fără cache al întregului encoded file, fără creștere a indexurilor proporțională cu durata și fără completion per EOF. Semantica transportului și a stării DSP rămâne cea din contractul canonic core/runtime.

## 2. Selecția decoderelor și limitele verificării

Selecția/adăugarea decoderelor existente este autorizată condiționat de dovezi: toate cele patru formate, streaming real pe blocuri, .NET 8, cancellation, API source-reader compatibil, licențe permisive/notices/transitive dependencies, asset coverage/RIDs, mentenanță, seek/loop real și resurse măsurate. Se preferă managed când satisface cerințele; native este acceptabil când este justificat. Poate fi aleasă o bibliotecă sau o combinație de adaptoare.

Versiunea efectivă, sursa și conținutul pachetului se verifică și se pinnează înainte de selecție. Niciun nume/pin nu este aprobat implicit. Source/package screening sau README-ul nu înlocuiesc probele codec/PCM/streaming/distribuție. Dovezile precedă PackageReference și redistribuția nativă. Copyleft/comercial, reducerea cerințelor ori schimbarea produsului/arhitecturii cer decizie separată.

**SDL_mixer este exclus din calea de livrare**, atât ca mixer, cât și ca decoder implicit. Reintroducerea sa cere aprobarea unei revizii arhitecturale. Nu se transferă cleanup-ul harness-ului în producție ca workaround și nu se condiționează livrarea decoderelor de repararea SDL_mixer/Graphix. Alegerea nu introduce un al doilea device/mixer care ocolește motorul Timbre.

Adaptarea nativă întreținută Xiph/Ogg și modificările vendor strict necesare sunt autorizate, cu calificarea separată a variantei exacte și păstrarea verificărilor aplicabile. Aceasta nu autorizează un codec propriu, restrângerea matricei sau selecția shipping fără dovezi.

### Platforme și siguranța probelor

Execuția codec/streaming se verifică pe Windows-ul disponibil. Execuțiile runtime native/codec non-Windows sunt **N/A conform politicii de platformă**, nu PASS/parity. Rămân obligatorii verificările host-side restore/build/publish/asset/notice pentru win-x64/win-arm64/linux-x64/linux-arm64/osx-x64/osx-arm64. Un gate Windows aplicabil fără probă sau cu eșec este blocked/fail, nu N/A.

Probe native autorizate: decodare/PCM/streaming obișnuite Windows pe corpusul de test și refuz controlat într-un pool local mic și fix, pe fișiere valide din corpus, înaintea depășirii și cu eroare explicită de resurse din wrapper. Fără attach la procese, crash intenționat/stack-frontier, OOM al sistemului ori alocare până la epuizarea resurselor. Un eșec neașteptat al procesului se păstrează și oprește proba, fără retry automat sau ocolirea protecțiilor.

## 3. Fișiere estimate

- **Noi:** adaptoare decoder în `Timbre/Decoding/` sau alt owner aprobat în etapa 0; source-reader comun definit de core, fără expunerea tipurilor bibliotecii externe în SoundClip.
- **Existente:** proiectul care primește dependențele după justificare; `Cerneala.csproj` doar dacă aceasta este compoziția aprobată; `Cerneala.slnx` dacă un proiect separat este justificat, nu decorativ.
- **Noi:** fixture corpus cu licență/proveniență/hash-uri și teste în proiectul planificat `tests/Cerneala.Tests.Timbre/`; instrumentare de source reads/buffer high-water/worker lifetime.
- **Distribuție:** `.github/workflows/desktop-backends.yml` și artifact/notice manifest după selecția dependenței; adaptoarele native/interop numai dacă este ales un decoder nativ.
- **Docs:** API source-loading/streaming aprobate și manifest; ghidul conceptual Timbre creat de planul markup nu se duplică aici.

## 4. Etapele de implementare

### Etapa 0 — selecție, fișiere și corpus

- [x] Formalizează matricea aprobată de mai sus și corpusul pozitiv/negativ: bit depths/rates mono-stereo, conținut versus extensie, truncated/corrupt/EOF, trimming/gapless, variante excluse și source seekable. Nu cere iar acord pentru limite aprobate; restrângerea materială ulterioară cere oprire.
- [x] Aplică politica preload/streaming aprobată în core și public API aferent. Formalizează membrii Auto/Preload/Streaming și pregătire anticipată async conform ergonomiei core, fără a inventa altă politică de loading.
- [x] Compară candidații prin probe izolate reproducibile în afara globs production: inițializare, primele blocuri, întregul PCM, EOF, pause/resume, seek/discard/supersession/loop, cancellation și lungime mare. Înregistrează input/hash, versiuni, comenzi și raw rezultatele; nu selecta biblioteca doar după textul API-ului.
- [x] Compară numai selecții compatibile cu granița decoder → PCM → Timbre DSP/mix → SDL3 core: verifică fiecare codec inclusiv Opus/Ogg, iar API-ul adaptorului nu deschide device și nu deține playback/effects. Nu considera WAV-only drept livrare a cerinței și nu implementa un codec propriu când candidații nu satisfac matricea; oprește și cere decizie.
- [x] Verifică codec execution pe Windows și pachetul/asset-urile host-side pentru șase RIDs, licențe permisive/notices/transitive deps/pins și mentenanță. Aprobarea selecției/adăugării este condiționată în index §1.1; dovezile precedă PackageReference/native redistribution. Copyleft/comercial sau schimbarea scope-ului opresc selecția. Runtime non-Windows N/A conform politicii de platformă, nu verificat.
- [x] Creează corpus synthetic/licensed versionat, cu instrument/versiune/comandă de codare reproductibile dacă este generat. Instrumentul encoder nu este presupus instalat și nu devine dependență runtime. Oracle-ul PCM/duration/channel și toleranțele lossy nu provin exclusiv din decoderul candidat.
- [x] Instrumentează reads/seek/open/close și buffers high-water, inclusiv în biblioteca aleasă; demonstrează streaming incremental și storage neproporțional cu durata, cu consum observat și limitele atribuirii raportate. Fără plafon numeric implicit ori justificarea exhaustivă CRT/OS ca prerequisite; packet reads singure nu dovedesc absența indexării/citirii integrale.
- [x] Probează forwarding-ul și refuzul cooperativ pentru limite configurate explicit, înaintea rezervării/alocării declarate peste allowance; ResourceLimitExceeded rămâne distinct de corrupt/unsupported. Implicitul nu impune plafon numeric. Nu pretinde interceptarea alocărilor arbitrare sau cap fizic din counter-ul rezervărilor.

**Gate etapa 0**

- [x] Decoderul/pin/licențele satisfac aprobarea condiționată și matricea pe dovezi Windows/asset checks; codec missing nativ nu este tratat ca fișier audio invalid.
- [x] Corpusul și observatorii pot distinge preload de streaming bounded; niciun test nou nu poate trece doar verificând extensia sau existența header-ului.

### Etapa 1 — adaptoare și PCM conformance

- [x] Adaugă teste de contract pentru readerul real și confirmă RED unde există comportament de integrat/modificat, fără a numi tipurile lipsă sau lipsa bibliotecii native un RED de decodare.
- [x] Implementează adaptoarele selectate și conversia spre formatul intern aprobat, fără byte/frame/sample confusion, downmix implicit sau state partajat între voices.
- [x] Verifică preload pentru toate formatele, mono/stereo din matrice, convertite în float32/stereo/48kHz: durata și frame count, ordinea canalelor, nivel/DC, samples finite și oracle lossy/toleranțe explicite.
- [x] Compară decodarea pe partiții neregulate cu decodarea completă, inclusiv Opus preskip/end trimming și MP3 VBR metadata conform matricei; EOF nu dublează sau pierde arbitrar ultimul bloc.
- [x] Verifică detectarea conținutului, invalid/corrupt/truncated, unavailable decoder și unsupported variant: erori structurate, fără auto-fallback la alt codec sau playback reușit fals.

**Gate etapa 1**

- [x] Fiecare format trece corpusul PCM/duration/channel și erorile pe adaptoarele reale; pachetele/transitive notices sunt păstrate. Nu se pretinde output fizic dintr-un test decoder.

### Etapa 2 — streaming bounded, transport și anulare

- [x] Înainte de integrarea streaming funcțională, adaugă și confirmă RED faithful pentru citirea cu contor de bytes/frames și bariere deterministic reader→queue→consumer; lipsa fișierului sau timeout-ul fixture-ului nu sunt RED.
- [x] Implementează decodare worker-side și buffers bounded cu backpressure; nici callback audio și nici UI nu citesc/decodează fișierul. Delay/DSP păstrează propria stare la frontierele blocurilor.
- [x] Compară fixtures scurte și lungi (de exemplu 10 s / 10 min, input/hash înghețate) în aceeași configurație: primul output înainte de citirea întregii surse; buffer high-water în limita aprobată, nu proporțional cu durata. Include costul indexurilor/caches ale decoderului în concluzie.
- [x] Adaugă și confirmă RED faithful pentru transport pe adaptoarele reale apoi implementează: Pause/Resume preserve reader+position, pause Pending, SeekAsync pe început/mijloc/aproape EOF și în paused, negative/over-duration și Duration încă necunoscută, latest-request supersession/cancel/replacement. Oracle independent verifică primul PCM la ținta precisă, inclusiv Opus preskip/end trim și MP3 CBR/VBR.
- [x] Confirmă RED/implementează rewind/reopen bounded pentru Loop întreg: mai multe repetări cu frame-count/PCM oracle, fără pauze introduse sau retained indexes/cache crescătoare; streaming nu devine preload ca workaround de seek/loop. Probează read/worker lifetime și consumul de memorie inclusiv seek și loop, fără cap numeric obligatoriu.
- [x] Testează cancel în open/read/decode/wait-for-space/EOF/seek/loop, înlocuire imediată și două playback-uri ale aceluiași fișier; închiderea unui reader nu închide readerul altei redări și nu livrează samples târzii vechi.
- [x] Testează underrun și I/O error injectate, padding contorizat la underrun temporar fără salt în sursă, Failed la I/O/corrupt/truncated; nu ascunde starvation sau retry.
- [x] Rulează 100 cicluri start/pause/resume/seek/loop/cancel/replace și final shutdown cu bariere/seed deterministic; worker/resource/file handles revin la baseline după drenare, fără loop de sleep/retry introdus pentru a masca race-uri.

**Gate etapa 2**

- [x] Streaming-ul tuturor formatelor este incremental și bounded în scope-ul măsurat; cancel/failure termină workerii și readerii și nu publică în playback înlocuit. Nu se substituie buffering-ul întregului fișier comprimat.

### Etapa 3 — distribuție și integrare

- [x] Rulează corpusul net8.0/decoder pe Windows și host-side restore/build/publish/asset/notice pentru win-x64/win-arm64/linux-x64/linux-arm64/osx-x64/osx-arm64. Execuții runtime native/codec pe non-Windows N/A conform politicii din index §7; nu le lansează, nu afirmă GREEN desktop/parity din validarea hostului.
- [x] Integrează aceleași surse/reader cu sink-ul core și testează streaming→modificatori→output, fără un mixer paralel în adaptorul decoder. Acceptarea cu device SDL3 aparține planului backend dependent; finalizarea acestui plan nu așteaptă acel backend.
- [x] Măsoară startup/first-frame, decode throughput, allocation și retained buffers conform protocolului din index; validează bugetele aprobate, cu rezultate inconclusive separate.
- [x] Documentează capabilitățile și limitele reale, loading/cancellation și distribuția/licențele; sincronizează API docs/manifest și rulează ApiCompat dacă au apărut membri publici.
- [x] Rulează proiectul Timbre integral, proiectele afectate de PackageReference/native assets și full solution; verify diff și cleanup, inclusiv procesele decoder/probe.

**Gate etapa 3**

- [x] Cele patru formate și streaming-ul sunt verificate în distribuția țintă; Windows aplicabil trece, execuțiile non-Windows sunt N/A conform politicii de platformă și nu prezentate ca pass. Output audio real se închide și în planul backend.

## 5. Comenzi și definiția de gata

După crearea proiectului/corpusului din core (numele de teste de aici vor fi artefacte noi):

```powershell
dotnet test .\tests\Cerneala.Tests.Timbre\Cerneala.Tests.Timbre.csproj -c Release --filter "FullyQualifiedName~SoundDecoder|FullyQualifiedName~SoundStreaming"
dotnet test .\tests\Cerneala.Tests.Timbre\Cerneala.Tests.Timbre.csproj -c Release
```

- [x] WAV/MP3/Vorbis/Opus trec preload/streaming/seek/loop pe Windows cu oracle independent și șase RID asset checks; runtime non-Windows N/A conform politicii de platformă.
- [x] Startup/buffers/throughput/cancel și failure satisfac contractul aprobat, fără I/O pe callback și fără stare mutabilă partajată.
- [x] Dependențele sunt pinneate, justificate și documentate; tests/API/docs/manifest/full-suite gates aplicabile sunt închise.
