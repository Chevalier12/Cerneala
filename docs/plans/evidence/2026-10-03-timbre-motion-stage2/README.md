# Timbre Motion — etapa 2: identitate, transport, cancel și hidden controls

Contract: `../2026-10-03-timbre-motion-stage0/README.md`; suprafața: `../2026-10-03-timbre-motion-stage1/README.md`.

## RED înaintea runtime-ului

Testele scrise pe API-ul aprobat, pe motorul Timbre real, graful `UIRoot.Motion` și scope-urile element/markup
(`SoundMotionLifecycleTests`: identitate capturată, înlocuire mid-animation prin consumer generat, strămoș Hidden/Collapsed;
`SoundMotionIntegrationTests`: control ascuns) au eșuat toate 5 cu
`System.NotImplementedException : The audio Motion binding is not implemented yet.` (`red.log`) — punctul de intrare comun
`SoundPlaybackMotion.Animate`/`MarkupSoundSession.StartMotion*` lăsat neimplementat în etapa 1. După implementare, primele
rulări au arătat două aspecte de harness, nu de binding: clock-ul root plafonează delta la `MaxDelta` = 100 ms (politica
existentă; testele pompează frame-uri ≤ 100 ms) și Servo rezolvă butoanele după conținut, nu `Name`.

Ordine: transportul (ceasul înghețat), setter-ul manual și respingerea sample-urilor au fost implementate în aceeași schimbare
cu binding-ul, deci RED-ul lor confirmat este cel comun de mai sus (punctul de intrare neimplementat), nu câte un RED separat
înaintea fiecărei punți; testele dedicate (SoundMotionTransportTests, SoundMotionContractTests) au fost scrise imediat după
și verifică fiecare invariantă pe motorul real.

## Implementare

- **Core** (`Timbre/SoundPlayback.cs`, `SoundClip.cs`, `SoundRuntime*.cs`): versiune de scriere manuală per slot (slot 0 Volume,
  i+1 parametrul i) incrementată de `Volume`/`Set`; `ReadMotionState` citește sub un singur lock terminal + versiuni +
  `clockRunning = started ∧ ¬paused ∧ seek-pending absent ∧ ¬terminal`; `TryPublishMotion` publică toate sloturile murdare
  ale unui frame cu un singur `controlVersion++`/`SignalMixer`, refuză pe terminal și nu atinge versiunile manuale;
  contoare `AnimatedPublications`/`MotionSamplesRejected` (runtime) + `MotionSamplesRejected` aditiv în Detective.
- **Adaptor** (`UI/Timbre/SoundPlaybackMotion.cs`): un `SoundPlaybackMotionTarget` per playback (ConditionalWeakTable), nod în
  `UIRoot.Motion.Graph` cât are animații active; deține un `MotionGraph` privat (mixers-ii root-ului, `ReducedMotionPolicy`
  propriu) cu un `MotionValue<float>` per slot. La fiecare frame root: stare sub lock → anularea sloturilor scrise manual →
  tick intern cu delta root sau zero (clock înghețat) → validare → publicare coerentă. Sample invalid: numai slotul se anulează,
  revine la ultima valoare publicată, contor + diagnostic. Terminal: toate handle-urile Canceled, nodul iese din graf.
  `ElementSoundOwner` înregistrează scope→root pentru `Motion()`; `Motion(root)` pentru scope-uri fără element.
- **Markup** (`UI/Markup/GeneratedMarkupSound.cs`): `StartSoundMotion` ține execuțiile audio în sesiunea Sound (fără
  renderability), `Retire` (detach / Aspect înlocuit / template retras / dispose) le anulează sincron înaintea scope-ului;
  `StartSoundMotionProperty` capturează o dată ocupantul și descriptorul (`Volume` sau parametrul clipului), slot gol sau
  ocupant terminal → handle no-op terminat, fără autoplay; spec absent → Tween 180 ms `Easings.Standard`.

## Teste (Cerneala.Tests.Timbre, filtrul `SoundMotion`, 29)

| Fișier | Acoperire |
| --- | --- |
| `SoundMotionLifecycleTests` | identitate capturată vs slot; înlocuire mid-animation (consumer generat); strămoș Hidden/Collapsed continuă, UI Motion se anulează, hide→show nu repornește |
| `SoundMotionIntegrationTests` | control ascuns eșantionat de clock-ul root |
| `SoundMotionTimelineTests` | Tween 0/75/150/225/300 ms pe Volume + Cut, hold, `ActiveTargets` → 0; `From` publicat în Pending, `HoldOnComplete=false`; retarget din `current`, prioritate/respingere; Spring, keyframes |
| `SoundMotionTransportTests` | Pause îngheață/Resume continuă (Motion vizual nesuspendat); timp de la primul PCM + Pause în Pending; seek pending + supersession (sursă streaming cu gate) fără restart; Loop fără repetare |
| `SoundMotionContractTests` | setter manual anulează numai slotul; overshoot Spring → Canceled, ultima valoare validă, contor runtime + Detective, sunet continuă; NaN fără clamp; erori sincrone (null, descriptor străin, range, terminal, scope fără root, alt root); Reduced Motion vizual nu afectează audio |
| `SoundMotionMarkupTests` | paritate consumer generat ↔ C# (Pending, Playing, Pause/Resume, înlocuire); slot gol no-op; Spring invalid identic C#/markup; owner ascuns + hide/show; detach/reattach, Aspect înlocuit, două scope-uri, 100 cicluri de înlocuire, template retras: zero publicații pe identitatea retrasă după barieră, `ActiveTargets`/voices la baseline |
| `SoundMotionApiBindingTests` | overload `SoundPlayback` în consumer (din etapa 1) |

## Verificare

| Comandă | Rezultat |
| --- | --- |
| build `Cerneala.slnx -c Release -m:1` | 0 erori |
| Timbre `~SoundMotion` × 3 | 29/29 de fiecare dată (`soundmotion-repeat.txt`) |
| Cerneala.Tests, filtrul legacy din plan §6 | 76 passed (`legacy-motion.trx`) |
| SourceGen `~SoundMotion|~UiMarkupGeneratorTests|~PrismMarkupContractTests` | 609 passed |
| Cerneala.Tests.Timbre complet × 2 (`--blame-hang`) | 365/365 de două ori (`timbre-1.trx`, `timbre-2.trx`) |

**Blocaj găsit și rezolvat în teste.** Primele două rulări complete ale proiectului Timbre s-au blocat (173 și 178 teste
terminate); `--blame-hang` a indicat `PlaybackIdentityTests.StartOverridesApplyToTheFirstBlockWithoutChangingTheDefinition`
(async), care trece izolat. Ipoteză: testele Motion sincrone (UI thread-affine) blocau firele `MaxConcurrencySyncContext` ale
xUnit cu `.GetAwaiter().GetResult()` pe metode async ale harness-ului ale căror continuări erau postate înapoi în același
context, deci la încărcare contextul se epuiza și testul async nu mai era reluat. Experiment: așteptările harness-ului din
testele Motion rulează prin `SoundMotionTestKit.Wait` (`Task.Run`, fără context capturat); interacțiunile Servo/UI rămân pe
firul testului. Rezultat: 2/2 rulări complete fără blocaj (înainte 2/2 blocate). Clasificare: susținut în condițiile testate;
cod de producție neatins.

## Window.Hide / pump (caracterizare)

Fapte de sursă: `WindowApplicationRuntime.PumpOnce` sare contextele cu `!Window.IsShown` sau viewport zero (nu procesează
root-ul, deci nici Motion); pentru contextele vizibile randează cât timp `Root.Motion.HasActiveMotion`. Testul
`SoundMotionUnpumpedRootHoldsSamplesWhileTransportContinues` reproduce root-ul nepompat: 10 blocuri PCM consumate, poziția
crește, volumul rămâne 0.35; după 1 s de clock, primul frame folosește delta plafonată (100 ms) → 0.55. Nu există al doilea
clock, nu se cere sampling sau randare în contextul ascuns, transportul nu este pauzat. O fereastră minimizată care nu este
ascunsă și nu are viewport zero rămâne pompată conform politicii existente; nu pretindem altceva. Într-o fereastră vizibilă,
o animație audio activă ține `HasActiveMotion` adevărat, deci pump-ul existent produce frame-uri ca pentru orice Motion;
costul acestora este măsurat în etapa 3.

## Observații separate (nereparate aici)

- `SpringSpec` cu valori mari (ex. 900→5000 Hz) rămânea la câțiva pași float de țintă cu viteza blocată peste `RestSpeed`
  și nu se termina. **Corectat ulterior** (fix separat): un punct fix exact al integrării (valoare și viteză neschimbate
  într-un avans) încheie arcul pe țintă; testul folosește acum arcul implicit, fără praguri scalate.
- `RealtimeAllocationTests` intermitent (vezi etapa 1).
