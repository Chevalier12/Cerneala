# Timbre core cost gate: protocol fixat înaintea măsurării

Data: 2026-10-07. Scris înaintea campaniei; nu se modifică după rezultate.

## Fixture

- Runner: `benchmarks/Cerneala.Benchmarks` mod `--timbre-core <report.json>` (`TimbreCoreBenchmarkRunner.cs`), Release, `net8.0-windows`, procese separate.
- Matrice: 32 voices looping = 28 preload (4 clipuri distincte de 2 s, pregătite cu `PrepareAsync`) + 4 streaming (sursă 60 s generată la cerere, reader propriu per voice); fiecare voice are `LowPass(2000 Hz)` + `Delay(0.25 s, 0.4, 0.3)`; Volume 0.5; blocuri de 480 frame-uri; coada software 40 ms.
- Consumator: emulator de device pe fir propriu, independent de mixer, care consumă pe ceasul de perete la 48 kHz (`Stopwatch`), contorizează frame-urile datorate și indisponibile ca underrun de device și notifică runtime-ul după consum. Nu procesează și nu mixează PCM. Underrun-ul, restanța și padding-ul nu se resetează și nu se clamp-ează.
- Instrumentare: `ISoundBlockObserver` intern, apelat pe firul mixer după fiecare bloc trimis; timpul acoperă selecția voices, render (sursă + DSP), mix, clipping și `Submit`; alocările sunt `GC.GetAllocatedBytesForCurrentThread()` pe firul mixer pe același interval. Recorder-ul folosește array-uri prealocate.
- Rulare: 1000 blocuri warmup (aruncate) + 10000 blocuri măsurate per proces, 5 procese secvențiale cu `TIMBRE_BENCH_PROCESS=1..5`. După fereastra măsurată: 200 de porniri ale unui clip plain pregătit (0.1 s), măsurând `Play` → primul bloc trimis care conține redarea (`FirstQueuedTimestamp`), cu cele 32 voices active.

## Praguri (index §7, aprobate)

| Metrică | Prag per proces |
| --- | --- |
| P99 cost mixer per bloc | < 2.5 ms (25% din 10 ms) |
| Alocări managed pe firul mixer în fereastra măsurată | 0 bytes |
| Coada software maximă | ≤ 1920 frame-uri (40 ms) |
| Underrun device în fereastra măsurată | 0 frame-uri |
| Underrun voice (padding de sursă) în fereastra măsurată | 0 frame-uri |
| P95 `Play` clip pregătit → primul PCM queued | < 50 ms |

## Regula de decizie și variație

- Gate PASS numai dacă **fiecare** din cele 5 procese satisface **toate** pragurile. Nu se fac reluări pentru a înlocui un proces eșuat; un eșec este FAIL.
- Variația raportată: raportul max/min între procese pentru P50/P95/P99 al costului mixer și pentru P95 latență. Dacă un proces are P99 sub prag dar campania nu poate fi executată complet (crash, timeout, mediu indisponibil), rezultatul este **inconclusive**, nu PASS.
- Un pilot scurt (`TIMBRE_BENCH_PILOT=1`: 100 warmup + 500 măsurate) validează numai că fixture-ul rulează și raportează toate câmpurile; rezultatele pilotului nu intră în verdict și nu ajustează pragurile.
- Nu există gate GPU și nu există prag DAC fizic; costurile decoder-worker/start/UI nu fac parte din fereastra realtime măsurată.
