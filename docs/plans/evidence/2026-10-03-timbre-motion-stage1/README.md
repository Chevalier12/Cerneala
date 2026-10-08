# Timbre Motion — etapa 1: binding semantic și emission tipat

Contractul: `../2026-10-03-timbre-motion-stage0/README.md`. Build `dotnet build .\Cerneala.slnx -c Release -m:1` → 0 erori.

## Implementare

- **Language (owner unic al validității).** `ValidateMotionAssignments` deviază orice path `X.sound.…` (≥3 segmente)
  înaintea `BindMotionAssignment`, deci fără fallback la proprietăți/parts/Prism. Ținta este legată în
  `CernealaSemanticModel.SoundMotion.cs` după tiparea handle-urilor din `BindSoundAspect`: formă `$self.sound.H.P`
  (CERNEALAUI021), context `@from/@to` din `@animate`/`@keyframes`, fără `@set/@scroll/@stagger/@run`/MotionClip (CERNEALAUI033),
  execuție fără target-uri de element/Prism/MotionClip (CERNEALAUI021), handle tipat Sound (CERNEALAUI031), `Volume` sau parametru
  din schema handle-ului (CERNEALAUI031), valoare `current` sau număr în range (CERNEALAUI032). Schema
  `BoundSoundAspect.HandleParameters` = parametrii float prezenți în toate clipurile pornite în handle, cu intersecția range-urilor;
  este publicată în `SoundMarkupModel` și consumată de SourceGen. Simboluri: segmentul handle → `@handle`, segmentul parametru →
  `@parameter` din clip (`Volume` = proprietate intrinsecă). Completion lexical pentru `$self.sound.` și `$self.sound.H.`.
- **SourceGen (lowering, fără re-validare).** Sintaxa acceptă `$x.sound.H.P`; `TryResolveSoundMotionTarget` produce
  `ResolvedSoundMotionTarget(H, P)` din schema legată (lipsa ei = eroare internă, nu fallback). Execuțiile audio
  (`IsAudioMotionNode`) nu sunt emise pe sesiunea vizuală: `EmitAudioMotionExecutions` le emite pe sesiunea Sound cu
  `GeneratedMarkup.StartSoundMotion(soundSession, factory)` și leaf-uri
  `GeneratedMarkup.StartSoundMotionProperty(soundSession, "H", "P", hasFrom, from, toCurrent, to, spec, options)`; în handler-ele
  `@on` rulează în ordinea sursei fără gardă `CanStartMotionExecution`, iar în `@when/@if` intră în `SoundBody` (sidecar audio),
  nu în `Activations` vizuale. Emisia `@animate`/compoziții a fost extrasă în `EmitMotionAnimationActivation`/
  `EmitMotionCompositionActivation`, parametrizate numai prin sesiune și metoda de start; calea vizuală emite identic.
- **Suprafața C#.** `SoundPlayback.VolumeParameter` (Set/start options echivalente cu `Volume`, interzis ca parametru de clip),
  `MotionExtensions.Motion(this SoundPlayback)` și `Motion(this SoundPlayback, UIRoot)`, `SoundMotionFacade`,
  `SoundMotionAnimationBuilder`, helpers `GeneratedMarkup.StartSoundMotion*`. Toate converg în
  `SoundPlaybackMotion.Animate(...)`; comportamentul binding-ului este, conform planului, implementat după RED-urile etapei 2
  (aici aruncă `NotImplementedException`).
- **Docs.** Pagini noi `Cerneala.UI.Timbre.SoundMotionFacade.md`, `Cerneala.UI.Timbre.SoundMotionAnimationBuilder.md` + manifest;
  actualizate `SoundPlayback`, `SoundStartOptions`, `SoundClip`, `MotionExtensions`, `GeneratedMarkup`. Exemplele paginilor noi
  sunt compilate în `tests/Fixtures/TimbreConsumer/DocumentationExamples.cs` (consumer fără InternalsVisibleTo).

## Verificare

| Suită | Rezultat |
| --- | --- |
| SourceGen `~SoundMotion` (aprobat, fallback element, lowering tipat, paired UserControl + `@when/@if`) | 4 passed |
| Language `~SoundMotion` (corpus 22 cazuri cu paritate Language/SourceGen + compilare generată, schema, completion, navigation) | 31 passed |
| Timbre `SoundMotionApiBindingTests` (consumer Roslyn: `playback.Motion()` leagă overload-ul non-generic `SoundPlayback`, nu `ObjectMotionFacade`) | passed |
| Cerneala.Tests.SourceGen complet | 641 passed (`SourceGen.trx`) |
| Cerneala.Tests.Language complet | 371 passed, 1 skipped preexistent (`Language.trx`) |
| LanguageServer / VisualStudio / PreviewHost complet | 45 / 48 / 22 passed |
| Manifest `ApiDocumentationManifestIsValidAndReferencesExistingFiles` (după editarea docs) | passed |
| Timbre `ExternalConsumer|SoundMotionApiBinding|Architecture`, SourceGen `~Documented` | 9 / 2 passed |
| Cerneala.Tests.Timbre complet | rulare 1: 336 passed, 2 failed; rulare 2: 337 passed, 1 failed (numai RED-ul de etapa 2) |

Corpusul (`tests/Cerneala.Tests.Language/Corpus/sound-motion-corpus.json`) acoperă: intrinsic Volume și custom float, Spring/
`@from`/`current`, parametru care alimentează două intrări (range intersectat), schema comună a două clipuri, keyframes + `@sequence`
audio, ramuri `@when/@if`; negative: handle nedeclarat/gol/Motion, parametru nedeclarat/proprietate de element/Loop, schema
incompatibilă, tip/range Volume/range parametru/binding, formă (3 segmente, `$Name.sound`), context `@set`/MotionClip/mixed.
`.prism.`, `parts` și path-urile UI rămân pe ramurile existente (suitele complete SourceGen/Language trec neschimbate).

`SoundMotionIntegrationTests.SoundMotionIsSampledByTheRootClockWhileTheControlIsHidden` rămâne RED (binding neimplementat),
clasificat ca RED de etapa 2.

**Allocation intermitent.** În rularea 1 a suitei Timbre complete `RealtimeAllocationTests.SteadyStateMixerBlocksDoNotAllocate(Streaming)`
a raportat 1 din 2000 blocuri măsurate cu 6248 B pe firul mixerului (`Timbre.trx`). Izolat: 3/3 passed; suita completă reluată
(`Timbre-rerun.trx`): passed; rulările complete anterioare (markup etapa 4, SDL3 etapa 2): passed. Etapa nu modifică
`Timbre/Engine`, `Timbre/Dsp`, `SoundRuntime.Mixer.cs` sau `StreamingFeed`; ramurile noi din core (`Set`, `SoundStartOptions.Set`,
constructorul `SoundClip`) nu sunt apelate de test. Clasificare: intermitent sub încărcarea suitei, necauzat de această etapă,
cauză nedeterminată; raportat separat, nu corectat aici.