# Timbre Motion — etapa 0: target, lifecycle, API C# și RED-uri

Snapshot: `master` @ `26d8f96a` + planurile necomise `2026-10-03-timbre.md`, `2026-10-03-timbre-motion-parameters.md`.
Host: Windows 11 Pro 10.0.26200 x64, SDK .NET 10.0.400 (teste net8.0/net10.0).
Prerechizite: core/runtime, decodare/streaming, SDL3 și markup/Aspect au statusul `finalizat` (0 căsuțe nebifate fiecare).
Contractul aprobat (index §1.1, plan Motion §2–§3) nu este redeschis; documentul îl formalizează mecanic.

## 1. Source facts care decid integrarea

Citite complet: `Timbre/SoundPlayback.cs`, `SoundParameter.cs`, `SoundHandle.cs`, `SoundScope.cs`, `SoundClip.cs`,
`SoundStartOptions.cs`, `SoundRuntime.Mixer.cs`, `Engine/SoundVoice.cs`; `UI/Motion/Core/MotionSystem.cs`, `MotionGraph.cs`,
`MotionValue{T}.cs`, `MotionHandle.cs`, `MotionFrameCoordinator.cs`, `MotionNode.cs`; `UI/Motion/MotionExtensions.cs`,
`MotionAnimationBuilder.cs`, `ObjectMotionFacade.cs`, `ObjectMotionAnimationBuilder.cs`, `ObjectMotionRuntime.cs`,
`MotionElementFacade.cs`; `UI/Markup/GeneratedMarkupMotion.cs`, `GeneratedMarkupSound.cs`; `UI/Timbre/ElementSoundOwner.cs`;
`UI/Elements/UIElement.Sounds.cs`, `UIRoot.Sounds.cs`; `Cerneala.SourceGen/UiMarkupSoundEmitter.cs`,
`UiMarkupMotionActivationEmitter.cs` și fluxul `TryResolveMotionAspect/TryResolveMotionExecution/TryResolveMotionTarget`
din `UiMarkupMotionResolver.cs`; `Cerneala.Language/Semantics/CernealaSemanticModel.Sound.cs` (tiparea handle-urilor) și
`BindMotionAssignment` din `CernealaSemanticModel.MotionPrism.Motion.cs`.

- **Overload-ul actual.** `MotionExtensions.Motion<TTarget>(this TTarget, params object[])` (`where TTarget : class`) face ca
  `playback.Motion()` să compileze azi către `ObjectMotionFacade<SoundPlayback>` → `ObjectMotionRuntime.Current`
  (`[ThreadStatic]`, `SystemMotionClock` propriu, tick din `UiHost.Update`). Acesta este al doilea clock interzis și nu este
  owner-ul audio; RED-ul runtime de mai jos îl demonstrează.
- **Path-ul `.sound.` azi.** Language `BindMotionAssignment` și SourceGen `TryResolveMotionTarget` tratează
  `$self.sound.H.P` ca `$self` + proprietatea ultimului segment pe tipul elementului: `Volume` → CERNEALAUI021 „does not
  exist on 'Button'”, iar `$self.sound.H.Opacity` animează **accidental** `Button.Opacity` fără diagnostic.
- **Sampling-ul root.** `MotionSystem.Tick` calculează delta din clock-ul root-ului (zero la primul frame activ,
  plafonat la `MaxDelta` 100ms) și eșantionează `MotionGraph`; `UIRoot.ProcessFrame` rulează Motion numai cât
  `Motion.HasActiveMotion`; `WindowApplicationRuntime.PumpOnce` nu procesează ferestrele `!IsShown`/viewport zero și
  randează contextele vizibile cât timp `HasActiveMotion` este adevărat (politica existentă pentru orice Motion activ).
- **Sesiunea vizuală.** `MarkupMotionSession` cere `UIElementVisibility.IsEffectivelyVisible(owner)` pentru start
  (`CanStart`) și anulează execuțiile + binding-urile la `OnRenderabilityChanged(false)`; `PrismMotionBinding` se anulează
  la target ascuns. Acestea rămân neschimbate.
- **Sesiunea audio.** `MarkupSoundSession` (din planul markup) ignoră renderability, își retrage scope-ul la
  detach/înlocuirea Aspect-ului capturat/dispose și ordonează acțiunile corpului în ordinea sursei; acțiunile audio ale
  corpului rulează prin handler-ul Sound, iar execuțiile Motion vizuale ale aceluiași corp sunt păzite de
  `CanStartMotionExecution(motionSession)`.
- **Core.** `SoundPlayback.Volume`/`Set(...)` publică sub `runtime.Sync` prin `controlVersion`; mixerul copiază controalele
  în `SoundVoice` la începutul fiecărei iterații (`ApplyControlsLocked`), deci o publicație atinge numai PCM încă
  neprodus. `started` devine adevărat când mixerul selectează vocea pentru primul bloc (`MarkStartedLocked`); `paused`,
  `seekCompletion` (seek pending) și `IsTerminal` sunt stări sub lock. Terminal ⇒ setter-ele aruncă.
- **Motion core reutilizabil.** `MotionGraph` are constructor public (mixers + `ReducedMotionPolicy` proprii);
  `MotionValue<T>` deține sampler/conflict/retarget/handle; `MotionNode` registrat în graful root primește fiecare
  `MotionFrame`. Nu este nevoie de modificări în `UI/Motion/Core/`.

## 2. Target schema/path și interop cu handle-ul (înghețat)

```text
$self.sound.<Handle>.<Parametru>
```

- Exact 4 segmente; primul `$self`, al doilea literal `sound`. `$owner.sound.…`, `$Nume.sound.…`, `$self.sound.H` (3
  segmente) și orice alt număr de segmente → diagnostic Motion target (CERNEALAUI021). Fără fallback la proprietăți,
  `parts` sau Prism (`.prism.` rămâne neschimbat).
- `<Handle>` trebuie să fie un `@handle` declarat în același Aspect și tipat **Sound** de modelul legat al planului markup
  (folosit în `@sound … as H`). Handle Motion, nefolosit sau nedeclarat → CERNEALAUI031 (familia de referințe Sound).
- `<Parametru>`:
  - `Volume` — intrinsec, gain 0–1, întotdeauna disponibil (ca override-ul `@sound $C(Volume = …)`, care și el
    numește intrinsecul). Un `@parameter Volume` al clipului nu este țintibil prin markup.
  - altfel, un `@parameter` float prezent în **toate** clipurile pornite în handle-ul respectiv în acel Aspect; range-ul
    static = intersecția range-urilor sale din acele clipuri. Absent dintr-un clip posibil sau intersecție vidă → CERNEALAUI031.
  - `Source`, `Loop`, `Position`, numele modificatorilor (`LowPass`, `Delay`) și intrările lor nu sunt target-uri.
- Valorile literale din `@from`/`@to`/keyframes se validează static contra range-ului (CERNEALAUI032), ca override-urile.
- Contexte: `@animate` (cu `@from`/`@to`) și `@keyframes` din corpurile `@on`/`@when`/`@if` ale Aspect-ului, singure sau
  compuse prin `@parallel`/`@sequence` **numai cu alte execuții audio**. O execuție care amestecă target-uri `.sound.` cu
  target-uri de element/Prism → CERNEALAUI021 (lifecycle-urile rămân separate). `@set`, `@scroll`, `@stagger`, corpurile
  `MotionClip` (`@run`), binding-urile `{…}` ca valoare și `$owner`/named audio paths nu sunt cerute → diagnostic,
  nu feature suplimentar.

## 3. Identitate, clock, cancellation și sampling (înghețat)

- **Identitate.** Binding = (instanța `SoundPlayback` capturată, slot); slot 0 = Volume, slot i+1 = parametrul i al
  clipului capturat. Markup: ocupantul handle-ului se citește **o dată**, la activarea leaf-ului Motion (după `@sound`
  din același corp, în ordinea sursei); descriptorul se rezolvă tot atunci, după nume, în clipul capturat. Nu se face
  lookup în slot sau după nume la tick. Slot gol/ocupant terminal la activare → leaf no-op terminat imediat (fără autoplay).
- **Owner.** Un `SoundPlaybackMotion` per playback (adaptorul `UI/Timbre/`), `MotionNode` registrat în
  `UIRoot.Motion.Graph` cât are animații active. Deține un `MotionGraph` intern (mixers-ii root-ului,
  `ReducedMotionPolicy` propriu `NoPreference`) cu câte un `MotionValue<float>` per slot animat: specs, mixers, conflict,
  retarget, handle și completion sunt cele existente; nu există engine paralel, clock propriu sau ObjectMotion.
- **Clock.** Timpul animației = `frame.Delta` al root-ului (aceeași politică clock/`MaxDelta`), trecut la graful intern
  numai cât `ClockRunning = started ∧ ¬paused ∧ ¬seekPending ∧ ¬terminal`, altfel delta 0:
  - Pending: identitatea și valorile `From` sunt aplicate imediat; timpul începe la primul bloc PCM (`started`).
  - Pause (inclusiv Pending și tail) îngheață; Resume continuă din același progres, fără restart.
  - Seek îngheață până la rezultatul ultimei cereri (succes sau eșec), fără retiming/restart; supersession păstrează
    înghețul până la ultima cerere.
  - Loop/EOF nu au efect asupra animației.
  - Visual Motion nu citește niciuna dintre aceste stări.
- **Sampling și publicare.** La fiecare frame root: (1) citire sub un singur lock a stării playback-ului (terminal,
  ClockRunning, versiunile scrierilor manuale per slot); (2) anularea sloturilor scrise manual; (3) tick-ul grafului
  intern; (4) validarea sample-urilor; (5) o singură publicare coerentă a tuturor sloturilor schimbate (`controlVersion`
  incrementat o dată, `SignalMixer` o dată). Publicarea nu incrementează versiunea „manual”. Fără reflection, dynamic sau
  string lookup per frame.
- **Cancellation order.** Înlocuirea în handle: `@sound … as H` anulează vechiul ocupant în core înaintea leaf-urilor
  Motion următoare; vechea țintă nu mai primește publicații (core respinge sub lock orice publicare pe terminal) și
  handle-urile ei Motion se termină Canceled la următorul sample root sau sincron la retragerea sesiunii; noile leaf-uri
  capturează noul ocupant. `@cancel H`/`playback.Cancel()`/failure/completion: aceeași regulă. Detach, înlocuirea
  Aspect-ului capturat și retragerea template-ului anulează sincron execuțiile audio ale sesiunii, apoi scope-ul.
  **Bariera de teardown** = tranziția terminală a playback-ului (publicațiile ulterioare sunt imposibile) și, pentru
  binding-uri, retragerea sesiunii sau primul sample root după terminal.
- **Setter manual.** `playback.Volume = x` / `playback.Set(p, x)` incrementează versiunea manuală a slotului; la următorul
  sample animația acelui slot se anulează (handle Canceled, valoarea manuală rămâne), înaintea oricărei publicări.
  Celelalte sloturi continuă.
- **Sample invalid.** NaN/∞/în afara range-ului slotului (ex. overshoot Spring) → numai animația slotului se anulează,
  ultima valoare validă publicată rămâne, `MotionSamplesRejected` crește și diagnosticul este observabil; redarea continuă.
  Fără clamp.
- **Hidden/Window.** Nodul trăiește în graful root, independent de renderability: Hidden/Collapsed direct sau pe strămoș nu
  oprește sampling-ul cât root-ul este pompat. `Window.Hide`: `PumpOnce` nu procesează fereastra ascunsă → sampling
  suspendat, transportul continuă; la revenire se aplică politica existentă de delta (`MaxDelta`). Fără al doilea clock și
  fără randare în contextul ascuns. Într-o fereastră vizibilă, animația audio activă ține `HasActiveMotion` adevărat,
  deci pump-ul existent produce frame-uri ca pentru orice Motion; nodul audio raportează zero invalidări layout/render.
- **Reduced Motion** al root-ului nu atinge graful intern; animațiile audio rulează normal.

## 4. API C# (înghețat) și lowering comun

```csharp
namespace Cerneala.Timbre;
public sealed class SoundPlayback
{
    // Descriptor intrinsec "Volume" (default 1, range 0–1). Set(VolumeParameter, v) ≡ Volume = v;
    // SoundStartOptions.Set(VolumeParameter, v) ≡ start.Volume = v.
    public static SoundParameter<float> VolumeParameter { get; }
}

namespace Cerneala.UI.Motion;
public static class MotionExtensions
{
    public static SoundMotionFacade Motion(this SoundPlayback playback);            // root-ul scope-ului elementului
    public static SoundMotionFacade Motion(this SoundPlayback playback, UIRoot root); // scope fără element (application/standalone)
}

namespace Cerneala.UI.Timbre;
public sealed class SoundMotionFacade
{
    public SoundPlayback Playback { get; }
    public SoundMotionAnimationBuilder Animate(SoundParameter<float> parameter);
}
public sealed class SoundMotionAnimationBuilder
{
    public SoundMotionAnimationBuilder From(float value);
    public SoundMotionAnimationBuilder To(float value);
    public MotionHandle With(MotionSpec<float> spec);                                    // HoldOnComplete = true
    public MotionHandle With(MotionSpec<float> spec, MotionPropertyStartOptions options);
}
```

- Ergonomie identică cu `MotionAnimationBuilder<T>`: `From` opțional (implicit valoarea curentă), `To`, `With` pornește și
  întoarce `MotionHandle` (Cancel KeepCurrent/Revert/Complete, `Completed`, `Completion`). `HoldOnComplete = false`
  republică la finalizarea naturală valoarea de dinaintea animației; implicit valoarea finală rămâne.
- `Motion()` rezolvă root-ul din scope-ul înregistrat de adaptor (`element.Sounds` și sesiunile markup). Scope fără element
  (`application.Sounds`, runtime standalone) → `InvalidOperationException` cu indicația `Motion(root)`; `Motion(root)`
  verifică thread-ul root-ului. Un playback animat deja de alt root → `InvalidOperationException`.
- Erori sincrone, înaintea oricărei schimbări: argument null; descriptor care nu este `VolumeParameter` și nu este declarat de
  `playback.Clip` (`ArgumentException`, mesajul core); playback terminal (`InvalidOperationException`, ca setter-ele);
  `From`/`To` non-finite/în afara range-ului (`ArgumentOutOfRangeException`); thread greșit. Fără Aspect/.crn necesar.
- **Rezolvarea overload-ului.** Pentru receiver `SoundPlayback`, `Motion(this SoundPlayback)` este aplicabil în formă
  normală, non-generic și cu conversie identitate; `Motion<TTarget>(…, params object[])` numai în formă expandată și
  generic; `Motion(this object)` cere conversie la `object`. Cele trei sunt în aceeași clasă statică, deci
  `using Cerneala.UI.Motion;` aduce mereu și overload-ul audio. Calea generică existentă nu este migrată.
- **Lowering comun.** `.sound.H.P` → `GeneratedMarkup.StartSoundMotion(soundSession, factory)` (execuție deținută de sesiunea
  audio, fără verificare de renderability) cu leaf-uri
  `GeneratedMarkup.StartSoundMotionProperty(soundSession, "H", "P", hasFrom, from, toCurrent, to, spec, options)` →
  capturează ocupantul și descriptorul → aceeași operație internă ca `SoundMotionAnimationBuilder.With`. Helper-ii nu
  adaugă politică de parametri; nu există sampling/interpolare duplicate.

## 5. Inventar callers (snapshot curent)

| Funcție comună | Callers | Tratament |
| --- | --- | --- |
| `MotionExtensions.Motion(...)` | 2 în `UI/Motion/Input/GestureMotionController.cs` (UIElement) + apeluri generate `MotionExtensions.Motion(element)` | overload-uri noi aditive pentru `SoundPlayback`; cele existente neschimbate |
| `UiMarkupMotionResolver.TryResolveMotionTarget` | 5 (animate, set, keyframes, stagger, scroll) | ramură `.sound.` înaintea fallback-ului; `.prism.`/parts/UI identice |
| `IsSoundNode` | 7 (resolver, sound emitter, reactive) | include execuțiile audio; corpurile fără audio neschimbate |
| `EmitSoundActivations` | 3 (Aspect, Window, UserControl) | emite și execuțiile audio |
| Language `BindMotionAssignment` | 3 | ramură `sound` înaintea fallback-ului |
| `ElementSoundOwner.GetScope` | 2 (`UIElement.Sounds`, `MarkupSoundSession`) | înregistrează scope→root pentru `Motion()` |
| `GetSoundSession` | 7 (helpers GeneratedMarkup) | helpers noi `StartSoundMotion*` |
| `MarkupMotionExecution.From` | 4 | reutilizat pentru leaf-uri audio |
| `SoundPlayback.Volume`/`Set`, `CountParameterPublication`, `ValidateParameterValue` | 3/3 în core | versiune manuală per slot și publicare animată internă, fără schimbarea semanticii publice |

`UI/Motion/Core/`, `GeneratedMarkupMotion.cs` și `GeneratedMarkupConditions.cs` nu sunt modificate de acest contract.

## 6. Compatibilitate specs/range (decisă înaintea implementării)

- Orice `MotionSpec<float>` existent: Tween (Easings/CubicBezier/Step), Spring, Decay, `@keyframes`, Repeat/PingPong;
  `RetargetMode` Restart/PreserveProgress și `MotionPriority` prin `MotionPropertyStartOptions`, cu `MotionValue`-ul slotului.
- `current` implicit (`From` omis) = ultima valoare publicată a slotului (sau valoarea curentă a playback-ului dacă slotul
  nu este animat). O nouă animație pe același slot urmează conflict policy-ul existent (anulează/retarget-ează).
- Range-uri: Volume 0–1; parametru = range-ul clipului (intersecția intrărilor). Start invalid respins sincron de core.
  Sample invalid → §3. Hold → §4. Reduced Motion → §3.

## 7. Observatori

- Core (`SoundRuntimeDiagnostics`, intern): `ParameterPublications` existent, plus `AnimatedPublications` și
  `MotionSamplesRejected`; Detective `SoundDiagnosticsSnapshot` primește `MotionSamplesRejected` aditiv.
- Adaptor: număr de ținte audio active per root și jurnal de sample-uri publicate (hook intern pentru teste).
- Identitate: `MarkupSoundFixture.Started` (ordinea playback-urilor acceptate) și `SoundPlayback.Volume`/`Set`.
- Layout/render: `MotionFrameResult`/`FrameStats` (`MotionRenderInvalidations`, `MotionLayoutInvalidations`, measure/arrange)
  și `InvalidationTrace` existente. PCM: sink-ul determinist (`TimbreRig`/`DeterministicSoundOutput`).

## 8. Baseline GREEN și RED

Build: `dotnet build .\Cerneala.slnx -c Release -m:1` → 0 erori (`baseline-build.log`). Prima încercare paralelă a eșuat
tranzitoriu cu CS2012 (DLL-ul `Cerneala.Backends.SdlGpu` blocat de VBCSCompiler între două instanțe de proiect); reluarea
serializată a trecut, fără modificări de cod.

GREEN legacy (`results/green-legacy.trx`, `--no-build`): filtrul din planul §6 pe `Cerneala.Tests` → **76 passed, 0 failed**,
inclusiv: `ConditionalMotionWaitsForEffectiveVisibilityBeforeStarting`, `DetachCancelsOnlyTheOwningMotionSession`,
`StartingIntoTheSameSessionHandleCancelsAndReplacesThePreviousExecution` (handle replacement în aceeași sesiune),
`NonVisibleTargetCancelsForeverBinding`/`NonVisibleAncestorCancelsDescendantForeverBinding` (Hidden/Collapsed),
`NonRenderableOwnerCancelsOnceWithoutAnotherPrismWrite` (3 variante) și `UiFrameLoopAdvancesAnObjectThatIsNotDrawnOrAttached`.

RED (build Release al proiectelor de test reușit; deci nu compilare/fixture):

```text
SourceGen  UiMarkupGeneratorTests.SoundMotionApprovedExampleGeneratesWithoutDiagnostics [FAIL]
  SoundMotionApproved.crn(22,38): error CERNEALAUI021: … Motion property 'Volume' does not exist on target type 'Button'.
  SoundMotionApproved.crn(23,38): error CERNEALAUI021: … Motion property 'ToneCutoff' does not exist on target type 'Button'.
SourceGen  UiMarkupGeneratorTests.SoundMotionTargetNeverFallsBackToAnElementProperty [FAIL]
  numai CERNEALAUI020 „Motion target must be Property, $self.Property, …” — nicio diagnoză despre handle-ul Playback
Language   SoundMotionSemanticTests.SoundMotionApprovedExampleBindsWithoutDiagnostics [FAIL]
  CERNEALAUI021 'Volume' / 'ToneCutoff' does not exist on target type 'Button'
Language   SoundMotionSemanticTests.SoundMotionTargetNeverFallsBackToAnElementProperty [FAIL]
  zero diagnostice: `$self.sound.Playback.Opacity` este legat accidental de Button.Opacity
Runtime    SoundMotionIntegrationTests.SoundMotionIsSampledByTheRootClockWhileTheControlIsHidden [FAIL]
  Assert.InRange: Range (0.499 – 0.501), Actual 0.2121398
```

Clasificare: generatorul/Language nu au target audio (fallback la proprietăți de element, cu dezacord Language 0 diagnostice
vs SourceGen 020 pentru `.Opacity`); runtime-ul leagă `playback.Motion()` de `ObjectMotionFacade<SoundPlayback>`, al cărui
runtime thread-static folosește `SystemMotionClock`, nu clock-ul manual al root-ului (0.2121 = 0.2 + câteva ms reale).
Toate trei sunt exact invarianta dorită, nu mediu. RED-ul runtime compilează azi prin calea generică; în etapa 1 apelul
devine `Animate(SoundPlayback.VolumeParameter).To(…).With(…)` pe fațada audio, iar RED-urile comportamentale
(identitate capturată, înlocuire mid-animation, hidden ancestor) se reconfirmă în etapa 2 înaintea runtime-ului.
Așteptările testelor existente nu au fost modificate.
