# Timbre core stage 1: redări, transport, parametri și anulare

Data: 2026-10-07. Plan: `docs/plans/2026-10-03-timbre-core-runtime.md`, etapa 1. Contract: `../2026-10-07-timbre-core-stage0/stage0-contract-and-gates.md`.

## RED → GREEN

| Artefact | Conținut | Rezultat |
| --- | --- | --- |
| `stage1-red.trx` | corpusul nou compilat contra suprafeței staged (owner access staged și el) | 67 failed / 16 passed. 65 eșecuri `NotImplementedException` din membrii staged (comportament absent), 2 `Assert.Throws` care au primit același `NotImplementedException` în locul excepției contractate. Cele 16 passed sunt observatorii harness-ului și testele de arhitectură/catalog din etapa 0. Nicio eroare de compilare, fixture sau mediu. |
| `stage1-green.trx` | același corpus + testul de publicare târzie după replacement | 84/84 passed |
| `stage1-compatibility.trx` | caracterizarea din etapa 0 + `ApplicationSoundsIntegrationTests` | 104/104 passed (103 baseline + 1 nou) |
| `stage1-cerneala-tests-full.trx` | întregul `Cerneala.Tests` după modificarea UIElement/UIRoot/Application/UiHost/WindowApplicationRuntime | vezi secțiunea „Regresie largă” |

RED-ul de transport (Pause/Resume/Pending/terminal, SeekAsync, Loop) face parte din același RED: testele există și eșuează înaintea oricărei implementări de transport.

## Defecte găsite de corpus în prima implementare

1. **Buffer streaming limitat de numărul de citiri, nu de frame-uri.** Pool-ul inițial avea 8 chunk-uri, câte o citire per chunk; cu citiri de 37 de frame-uri buffer-ul ținea 296 de frame-uri și mixer-ul intra în underrun (`StreamingWithIrregularReadsMatchesTheClosedFormSignal`, `StreamingLoopRepeatsWithoutGapsUnderIrregularReads`). Ownerul invariantului „memorie neproporțională cu durata, buffer suficient” este feed-ul: înlocuit cu un ring fix de 8192 frame-uri (64 KiB) și descriptori de segment (≤256), o citire per segment direct în spațiul liber contiguu, max 2048 frame-uri per citire. Datele unei citiri sunt publicate imediat (nu așteaptă umplerea unui chunk în spatele unui I/O blocat).
2. **Voice completat în drain check eliberat abia la următorul wake.** `CompletionRequiresEndOfSourceAndAnObservedDrain` aștepta `WhenReleased`. Mixer-ul reia acum iterația după orice completion, ca resursele să fie eliberate imediat.

Două așteptări de test au fost corectate ca rase reale ale testului, nu ale motorului (dovedite prin semantică): runtime-ul implicit al `Application` nu are output, deci redarea poate ajunge `Failed(DeviceUnavailable)` înainte de exit; după `Resume` cu capacitate disponibilă, mixer-ul poate ajunge `Playing` înaintea aserției `Pending` (sink-ul este ținut acum în timpul observației).

## Stabilitate

Corpusul complet rulat de 40 de ori consecutiv după corecții: 0 rulări eșuate. Sincronizarea testelor folosește numai semnale ale motorului (`WhenReady`, `WhenSettledAsync`, `WhenReleased`, `SyncAsync`) și ale sink-ului/readerului (submitted frames, read count); fără sleep/polling.

## Ce dovedește corpusul

- Sink-ul și readerii observă motorul concret (`SoundRuntime` real, thread mixer real, feed-uri reale), nu un mixer fake; testele de PCM compară bloc cu bloc cu semnalul închis-formă.
- Identitate/izolare: două redări ale aceluiași clip au playhead, volum, stare și (la streaming) reader proprii; override-urile nu mută definiția; preload partajează un singur payload immutable.
- Tranzacția de start: delegate care aruncă, descriptor străin, valori NaN/out-of-range, handle din alt scope, clip cu modificatori (refuzat explicit cu `NotSupportedException` până la etapa 2) și limita de voices resping sincron și păstrează ocupantul; replacement eliberează slotul de voice în aceeași tranzacție.
- Transport: Pause/Resume fără restart, Pause în Pending blochează primul PCM, no-op idempotente, terminal respinge transport/mutații, Seek precis (preload + streaming), latest-seek wins cu task anulat, cancel anulează seek-ul pendinte, seek peste durată necunoscută eșuează task-ul și redarea continuă, seek la final completează, Loop fără gol (preload + streaming, citiri neregulate), Loop snapshot la start, loop pe sursă vidă completează.
- Coadă/consum: cancel/replacement/volum aplicate numai blocurilor produse după comandă; PCM queued rămâne; output-ul comun nu este golit sau închis; `Completed` cere EOF + drain observat (nu ultimul bloc produs).
- Eșecuri: I/O (`SourceUnavailable`, inner păstrat), NaN (`InvalidData`), violarea contractului readiness (`InvalidData`), fișier fără decoder (`UnsupportedFormat`), fișier lipsă / stream non-seekable (`SourceUnavailable`), fără output / open failure / device loss (`DeviceUnavailable`, fără retry automat, `Play` ulterior redeschide).
- Resurse: cancel/failure/replacement eliberează readerul și pump-ul (`LiveReaders`, `LiveSourcePumps` = 0) fără a afecta alte redări; worker-ul unei redări înlocuite nu publică în noul ocupant.
- Loading: `PrepareAsync` preîncarcă și `Play` devine gata sincron din cache; Auto respectă pragul; Preload refuză înaintea citirii la depășire; cache-ul evacuează numai payload-uri nepinned și refuză când datele pinned îl umplu.
- Owner access: `UIElement.Sounds` (inclusiv `Scene2D`), `UIRoot.SetSoundRuntime`, `UiHostOptions.SoundRuntime` fără `Application`, `Application.SoundRuntime/Sounds`, ferestre care partajează runtime-ul aplicației; detach/close retrage numai scope-urile proprii; ascunderea nu oprește audio; acces off-thread respins.

## Regresie largă

`stage1-cerneala-tests-full.trx`: întregul `Cerneala.Tests` (net8.0-windows) — 4195 passed, 0 failed, 2 skipped (skip-uri preexistente, independente de Timbre). Proiectul `Cerneala.Tests.Timbre` rulează separat (84/84).
