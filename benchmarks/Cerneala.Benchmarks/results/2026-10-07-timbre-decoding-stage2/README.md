# Timbre decoding stage 2: streaming bounded, transport și anulare

Data: 2026-10-07. Plan: `docs/plans/2026-10-03-timbre-decoding-streaming.md`, etapa 2.

## Integrare și RED

Readerele decodate din etapa 1 intră în calea de streaming a core-ului (`StreamingFeed`: pump pe thread pool, ring fix de 8192 frame-uri, backpressure prin spațiul liber al ring-ului), deci streaming-ul funcțional exista din momentul înregistrării decodoarelor. Testele noi `SoundStreaming*` au găsit totuși defecte reale de motor și de adaptor; fiecare a fost observat RED înaintea reparației la proprietarul invariantului:

| RED observat | Cauză | Reparație (owner) |
| --- | --- | --- |
| după un seek pe un stream cu ring plin, primul bloc era liniște (underrun contorizat), apoi PCM corect | mixer-ul prelua rezultatul seek-ului imediat, iar segmentele generației vechi ocupau ring-ul până la primul `Read`; `HasData` însemna „a avut vreodată date” | `StreamingFeed`: după un seek reușit aruncă segmentele vechi și raportează „fără date” până la primul segment nou — vocea așteaptă pregătirea fără padding (contract: redarea activă continuă după pregătire) |
| seek înapoi la 0 după alte seek-uri: canalul drept diferea la primele sample-uri (44.1/32/22.05 kHz; Opus 48 kHz neafectat) | `SpeexResampler.ResetMem` din Concentus 2.2.2 lasă istoric în al doilea canal (confirmat: resampler nou → exact) | `CanonicalConverter`: resampler nou la fiecare seek |
| `Duration` rămânea `null` pentru MP3 fără tag Info și după capătul sursei | durata se fixa doar la atașarea feed-ului | `StreamingFeed` reține lungimea la EOF/wrap; mixer-ul o publică (`SoundPlayback.PublishLength`) |
| reader care declară o lungime și se termină mai devreme: `Failed(InvalidData)` în preload, `Completed` în streaming (trunchiere tăcută) | pump-ul nu verifica lungimea declarată | `StreamingFeed`: sfârșit înaintea lungimii declarate sau frame-uri peste ea → `InvalidData` (`stage2-red-mp3-cut.trx` pentru MP3 tăiat la graniță de frame; `Mp3Source` verifică și el numărul din tag) |
| `stage2-mutation-red.trx` | mutație temporară: readerul decodat bufferează toată sursa înaintea primului output | detectată: cele 4 fișiere lungi cad (bariera din mijlocul fișierului e atinsă, redarea nu pornește); mutația a fost revertită (fișier identic) |

Semnalul intern `WhenSettledAsync` (numai observabilitate de test) raporta idle și în fereastra dintre eliberarea de ring de către mixer și reluarea pump-ului; acum „settled” = pump idle și nimic de făcut (ring sau descriptori plini, sursă terminată, niciun seek în așteptare). Fără acest lucru, testele care consumă mai repede decât timpul real înfometau intermitent un stream decodat.

## Teste (tests/Cerneala.Tests.Timbre/Decoding)

- `SoundStreamingIncrementalTests`: MP3/Vorbis/Opus/WAV, 2 s vs 300 s aceeași configurație — primul PCM (identic cu decodarea) apare cu o barieră care blochează orice citire între 5% și finalul fișierului (coada mărginită — pagina Ogg finală, tag-urile MP3 de la final — rămâne permisă); octeți citiți < 5% din fișier; ring (64 KiB) și rezervarea decodorului identice între scurt și lung; citirile rulează pe pump, nu pe firul mixer-ului sau al apelantului.
- `SoundStreamingTransportTests` (MP3 CBR, MP3 VBR, Vorbis, Opus cu pre-skip/end-trim, WAV): seek la mijloc/început/aproape de final/țintă impară — primul bloc egal bit cu bit cu decodarea la ținta exactă (Opus: decodor repornit cu pre-roll 80 ms, toleranță 0.05 și SNR ≥ 15 dB față de oracle), același stream; seek în pauză rămâne în pauză și reia la țintă; Pause/Resume păstrează readerul și poziția, fără restart; Pause în Pending ține primul PCM; seek negativ/peste durată respins sincron; durată necunoscută: seek peste final eșuează și redarea continuă contiguu, iar durata se învață la wrap; latest-seek-wins cu readerul blocat în I/O; cancel și înlocuire prin handle retrag seek-ul pendinte și numai noua redare ajunge în output.
- `SoundStreamingSeekTests`: secvențe de seek înainte/înapoi/la 0 la nivel de reader, exacte pentru toate codec-urile, cu și fără re-eșantionare.
- `SoundStreamingLoopTests`: bucla repetă întreaga sursă trimată fără goluri (PCM egal cu decodarea concatenată pe 2.5 repetări), zero underrun, rezervare constantă, același reader, sursa recitită (fără copie în cache).
- `SoundStreamingCancelTests`: cancel cu readerul blocat la deschidere, în citire, în așteptare de spațiu, după EOF, în rewind-ul de buclă; redarea devine `Canceled` imediat, iar readerul, stream-ul, pump-ul, rezervările și ring-ul revin la 0 după ce I/O blocată se întoarce; două redări ale aceluiași fișier au readere independente.
- `SoundStreamingFailureTests`: underrun injectat (citire blocată) — padding contorizat, poziția nu avansează, reluarea continuă exact de unde s-a oprit; eroare I/O → `Failed(SourceUnavailable)` cu `IOException`; CRC Vorbis, MP3 trunchiat, sync pierdut, MP3 tăiat la graniță de frame, reader terminat înaintea lungimii (preload și streaming) → `Failed(InvalidData)`.
- `SoundStreamingStressTests`: 100 de cicluri seed-uite start/pause/resume/seek/buclă/cancel/înlocuire pe MP3/Vorbis/Opus/WAV, fiecare pas sincronizat pe semnale ale motorului (sink-ul consumă numai când pasul îl conduce), apoi shutdown: readere, pump-uri, rezervări, ring-uri, voci 0 și toate stream-urile închise.

Anularea nu poate întrerupe o citire sincronă blocată a unui stream; redarea devine `Canceled` imediat, iar resursele se eliberează când citirea se întoarce (documentat; pentru fișiere locale citirea e scurtă).

## Memorie pe codul de producție

`decoding-memory.json` (`dotnet run --project benchmarks/Cerneala.Benchmarks -c Release -- --timbre-decoding <raport> tests/Cerneala.Tests.Timbre/Corpus`): heap live atribuibil unui reader (stream cu buffer de 64 KiB, demux, decodor, convertor), măsurat cu GC complet, cu bufferele măsurătorii alocate înaintea baseline-ului; peak peste 11 000 de blocuri decodate în buclă, o decodare completă și patru seek-uri.

| Fișier | Rezervare | Live după deschidere | Peak (bucle + seek-uri) | După dispose |
| --- | ---: | ---: | ---: | ---: |
| WAV 22.05 kHz mono 2 s / 300 s | 256 KiB | 202 064 / 199 512 | 202 304 / 199 528 | ~0 |
| MP3 22.05 kHz mono VBR 2 s / CBR 300 s | 512 KiB | 310 576 / 240 808 | 325 632 / 258 624 | tabele statice NLayer la primul MP3 |
| Vorbis 22.05 kHz mono 2 s / 300 s | 2 MiB | 709 104 / 706 512 | 709 120 / 706 528 | ~0 |
| Opus mono 2 s / 300 s | 512 KiB | 201 152 / 183 688 | 201 168 / 183 704 | ~0 |

Memoria nu crește cu durata și nici cu buclele sau seek-urile; nu există index de frame-uri/pagini. Scope: heap managed; nu include tabele statice, garbage necolectat, memorie CLR/OS/nativă sau stive și nu e un bound RAM/RSS.

## Rulări

`stage2-timbre-full-1.trx`…`-3.trx`: `Cerneala.Tests.Timbre` 299/299 de trei ori consecutiv. O rulare anterioară a prins o cursă preexistentă în fixture-ul consumer al core-ului (`ManualOutput` consumă instantaneu și clipul de înlocuire nebuclat se termina înaintea verificării slotului); înlocuirea pornește acum în buclă, ca celelalte redări din scenariu.
