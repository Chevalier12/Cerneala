# Timbre core stage 2: LowPass, Delay și procesare pe blocuri

Data: 2026-10-07. Plan: `docs/plans/2026-10-03-timbre-core-runtime.md`, etapa 2. Algoritmi și oracle-uri fixate în `../2026-10-07-timbre-core-stage0/stage0-contract-and-gates.md` §7.

## RED → GREEN

| Artefact | Conținut | Rezultat |
| --- | --- | --- |
| `stage2-red.trx` | corpusul DSP (`Cerneala.Tests.Timbre.Dsp`) contra kernel-elor/lanțului staged și a restricției din etapa 1 | 32/32 failed: 19 `NotImplementedException` din `LowPassKernel`/`DelayKernel`/`SoundDspChain` staged, 13 `NotSupportedException` „Playing a sound clip with modifiers requires the Timbre DSP chain” (restricția explicită a etapei 1). Fără erori de compilare sau fixture. |
| `stage2-green.trx` | întregul proiect Timbre după implementare | 117/117 passed |

RED-ul acoperă și reset-ul DSP la seek manual, păstrarea la pause/resume și loop natural, tail/cap/freeze și seek respins: aceste teste există în `DspChainEngineTests` și au eșuat în `stage2-red.trx`. Testul `PlainAndModifiedPlaybacksShareOneMix` a fost adăugat după GREEN ca acoperire suplimentară a mixajului plain + lanț; sub codul etapei 1 ar fi eșuat pe aceeași restricție `NotSupportedException`.

Testul de etapa 1 `ModifiedClipIsRejectedBeforeReplacementUntilDspIsAvailable` documenta restricția temporară; etapa 2 o elimină, iar testul este înlocuit de `ModifiedClipStartsAndReplacesThroughTheSamePathAsAPlainClip`.

## Implementare

- `Timbre/Dsp/LowPassKernel.cs`: SVF TPT, `k = √2`, `g = tan(π·fc/48000)`, coeficienți recalculați numai la schimbarea cutoff-ului (per bloc), stare per canal.
- `Timbre/Dsp/DelayKernel.cs`: linie circulară per canal, `D = round(Time·48000)`, citire înaintea scrierii, schimbare de Time cu salt imediat al poziției de citire.
- `Timbre/Dsp/SoundDspChain.cs`: etape în ordinea declarației, intrări constante sau index de parametru citit din valorile publicate ale redării; capacitate Delay = D pentru Time constant, 2 s pentru Time parametric. Clipul nu conține niciun buffer runtime.
- `SoundRuntime.RenderVoice`: sursă → lanț → (în mixer) × Volume; underrun procesează numai frame-urile primite (DSP înghețat pentru padding); după EOF non-loop lanțul procesează zero până la criteriul tail sau cap; cap-ul șterge PCM-ul de după limită și marchează `TailTruncated`.
- Seek reușit → `SoundVoice.ResetDsp()`; seek respins nu atinge starea; Pause nu procesează (tail înghețat); Loop nu produce EOF.
- `DspStateBytes` contabilizează liniile Delay de la start până la eliberarea redării.
- Starea recursivă sub `1e-20` este anulată la sfârșitul blocului (LowPass) sau la scriere (Delay) pentru a evita aritmetica subnormală în tăceri lungi; pragul este cu 14 ordine de mărime sub criteriul tail `1e-6` și nu limitează valorile de ieșire.

## Oracle-uri și toleranțe

- LowPass: biquad RBJ (Q = 1/√2) DF-I în double — altă structură decât SVF-ul motorului, aceeași funcție de transfer. Răspuns la impuls la 200/1200/6000/20000 Hz pe 9600 eșantioane, toleranță `2e-5` (float32, ε = 1.19e-7, acumulare recursivă). Magnitudine `|H|² = 1/(1+(tan(ω/2)/tan(ωc/2))⁴)` verificată prin bin DFT pe perioade întregi la 200/1000/4000/12000 Hz, toleranță `1e-4`; −3.01 dB la cutoff; DC unitar.
- Delay: tren de impulsuri exact `(1−Mix)` și `Mix·Feedback^(k−1)`; referință double pe semnal broadband cu Feedback 0.95, toleranță `2e-5`; capetele Mix 0/1 exacte (toleranță 0).
- Partiții neregulate (1, 2, 7, 13, 33, 37, 480, 999, 1000, 1024) identice bit cu bit cu procesarea într-un singur bloc, pentru ambii kernel-i.
- Canale independente: canal drept zero rămâne exact zero; procesarea stereo egală cu procesarea separată.
- Modulație Cutoff 20 Hz ↔ 20 kHz la fiecare bloc pe 2000 de blocuri: ieșire finită, vârf < 2.
- Tail: capătul calculat din oracle-ul double pe aceeași regulă (`max(D, 480)` frame-uri sub `1e-6` pe ieșirea pre-Volume); Volume = 0 produce aceeași lungime de tail; cap implicit 30 s cu Feedback 0.95 / Time 2 s produce exact `480 + 30·48000` frame-uri și `TailTruncated`.

## Stabilitate

Corpusul complet rulat de 50 de ori consecutiv: 0 rulări eșuate (după corectarea unei ordonări de test în `LatestStreamingSeekWinsAndTheSupersededRequestIsCanceled`: seek-urile se emit acum numai după ce coada sink-ului s-a umplut, altfel mixer-ul putea produce legitim încă un bloc înaintea aserției `Position`).
