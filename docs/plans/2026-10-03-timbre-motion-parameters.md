# Plan: Timbre — parametri animați prin Motion

> Data: 2026-10-07
> Status: finalizat
> Dependențe: [core/runtime](2026-10-03-timbre-core-runtime.md), [markup/Aspect](2026-10-03-timbre-markup-aspect.md); [codecs](2026-10-03-timbre-decoding-streaming.md) și [SDL3](2026-10-03-timbre-sdl3-backend.md) pentru native integrare
> Scop: Motion existent controlează proprietățile unei redări capturate, nu definiția SoundClip și nu noua instanță care ocupă ulterior același handle.

## 1. Puncte de integrare și compatibilitate

- Verifică target binding în UiMarkupMotionResolver și CernealaSemanticModel.MotionPrism.Motion: `.sound.` cere binding tipat înaintea fallback-ului property/template parts.
- Inspectează punțile PrismMotionResolver/PrismMotionEmitter ca precedent de getter/setter tipat, nu ca owner audio.
- Protejează lifecycle-ul vizual din GeneratedMarkupMotion, UIElement și MotionPropertyBinding; audio nu este o proprietate UI fictivă și nu invalidează render.
- Verifică ObjectMotionRuntime și ObjectMotionTests; runtime-ul object-target nu este owner implicit pentru instanțele/handle-urile audio din markup.
- Inspectează UIRoot.ProcessFrameCore, UiHost.UpdateCore, MotionFrameCoordinator și WindowApplicationRuntime.PumpOnce. Window.Hide poate suspenda sampling-ul Motion audio fără a opri transportul.

## 2. Contract țintă

```text
@handle Playback;

@on Click
{
    @sound $ConfirmSound(Volume = 0.2, ToneCutoff = 800) as Playback;
    @animate with Tween(300ms, EaseOut)
    {
        @to
        {
            $self.sound.Playback.Volume = 0.8;
            $self.sound.Playback.ToneCutoff = 6000;
        }
    }
}
```

Căile aprobate sunt intrinsic Volume și parametrii expuși ai clipului prin handle. Source, Loop, recipe-ul modifier și playhead nu sunt target-uri Motion; Seek este acțiune de transport explicită, nu animare Position. Nu există `$Playback.Volume` doar pentru că s-a declarat @handle; nu introducem late string/reflection traversal.

La activare se capturează playback identity + typed parameter descriptor. Animarea handle-ului gol este no-op fără autoplay; cancel/replace/failure/terminal release anulează binding-ul vechi conform contractului core. State-ul unei instanțe noi nu primește sample-uri vechi. Parametrul poate alimenta mai mulți modificatori fără ca Motion să cunoască structura DSP internă.

Control Hidden/Collapsed și ancestor visibility nu opresc audio Motion în root activ. Detach/Aspect replacement/template retirement opresc numai binding-urile și redările scope-ului retras. Window.Hide poate suspenda root sampling și transportul audio rămâne domeniu separat. Nu se rescrie pump-ul nativ și nu se promite sample-accurate automation.

### Pending, transport, valori și clock aprobate

- Binding identity se capturează imediat; timpul animației pending începe la primul PCM, nu în loading. Motion vizual următor nu așteaptă.
- Pause suspendă timpul animațiilor audio ale instanței, inclusiv în tail; Resume continuă fără restart. Seek suspendă temporar timpul, dar nu mută/repornește timeline-ul; Loop nu repetă animația per EOF. Visual Motion nu este suspendat de transport audio.
- Setter manual anulează numai animația parametrului scris. Current implicit, From/To/With și specs numerice existente păstrează politicile aplicabile de conflict/hold; valoarea finală rămâne.
- Sample NaN/Infinity/out-of-range anulează numai animația acelui parametru, cu ultima valoare validă/diagnostic și redare continuată; fără clamp ascuns. Start/setter invalid este respins de core.
- Reduced Motion vizual nu dezactivează implicit audio Motion. SoundPlayback terminal respinge mutații/start animație/transport nou; slot markup gol rămâne no-op. Nu confundă cele două.
- Window.Hide poate suspenda root sampling și păstrează clock/delta policy existentă la revenire; nu este Pause al redării și nu cere al doilea clock/generic ObjectMotion fallback.

## 3. Owner și compatibilitate

Binding-ul audio propus trăiește în scope-ul audio, pe identitatea playback și descriptorul de parametru; folosește valorile/specs/mixers/cancellation/composition Motion existente și contractul de publication către Timbre. Nu creează engine de animație paralel. Sampling-ul este distinct de procesarea PCM și nu declanșează render invalidation pentru o schimbare strict audio.

Sesiunea Motion vizuală și publicul GeneratedMarkup existent păstrează hidden-owner cancellation. De asemenea, root UI-property motion, Prism motion, arbitrary-object Motion și event bindings vizuale nu sunt făcute renderability-independent global. Dacă integrarea reală nu poate păstra aceste limite, oprește și cere aprobare pentru extinderea architecturală, nu copia un workaround thread-static.

### API C# și lowering-ul aceleiași animații

Utilizatorul C# trebuie să poată anima SoundPlayback direct, fără Aspect sau `.crn`, cu aceleași specs și politici de captură ca markup-ul. Forma aprobată ca ergonomie este o fațadă audio dedicată, cu descriptori tipați:

```csharp
// Ergonomie aprobată; tipurile/membrii audio nu există încă.
void AnimatePlayback(
    SoundPlayback playback,
    SoundParameter<float> toneCutoff,
    MotionSpec<float> transition)
{
    playback.Motion()
        .Animate(SoundPlayback.VolumeParameter)
        .To(0.8f)
        .With(transition);

    playback.Motion()
        .Animate(toneCutoff)
        .To(6000f)
        .With(transition);
}
```

`UI/Motion/MotionAnimationBuilder.cs` oferă deja From/To/With pentru UiProperty; `MotionExtensions.cs` și `ObjectMotionAnimationBuilder.cs` au și o cale object-target cu runtime thread-static și Start. Acestea sunt precedente de ergonomie, nu bridge audio gata făcut. Overload-ul specific SoundPlayback și bridge/clock-ul se formalizează și probează în etapa 0 în limitele contractului aprobat. Binding-ul audio nu falsifică un UiProperty vizual și nu cade implicit pe generic Object Motion.

`.sound.Handle.Parametru` se lower-uiește către aceeași captură playback+descriptor și aceeași operație Motion audio; helpers pot lega execuția de scope-ul markup, fără a dubla sampling/interpolation. SoundHandle este slot, SoundPlayback este instanță: nici fațada C#, nici codul generat nu urmăresc ocupantul slotului la fiecare tick. Engine-ul core Timbre rămâne independent de UI/Motion; fațada și integrarea scheduling/lifecycle aparțin adaptorului `UI/Timbre/`.

## 4. Fișiere estimate și callerii de protejat

- **Noi:** binding/target runtime audio tipat în `UI/Timbre/` și helpers GeneratedMarkup audio, plus target semantic comun în Language și lowering în SourceGen.
- **Existente:** `UiMarkupMotionResolver.cs`, modelul/emitter-ul Motion target și `UiMarkupMotionActivationEmitter.cs`; `CernealaSemanticModel.MotionPrism.Motion.cs`; completion/navigation pentru path-uri noi.
- **Runtime existente numai dacă integrarea stabilită o cere:** `UI/Motion/Core/` graph/frames/timeline și `GeneratedMarkupMotion.cs`, GeneratedMarkupConditions; modificările sunt aditive și callerii legacy sunt listați explicit înainte de editare.
- **Teste:** noi SoundMotionIntegrationTests și sound-target SourceGen/Language corpus; existente GeneratedMarkupMotionTests, MarkupMotionExecutionTests, MotionPropertyBindingTests, PrismMotionIntegrationTests, MotionSystemTests, ObjectMotionTests și generator handle/parameter/composition.
- **Consumer/docs:** exemplu real în Timbre smoke/ghid și paginile API Motion/GeneratedMarkup schimbate, numai sub canonical classes + manifest.

## 5. Etapele de implementare

### Etapa 0 — target/lifecycle și RED-uri de integrare

- [x] Îngheață target schema/path și interop handle contract aprobate în core/markup; stabilește binding identity, cancellation order și param sampling pentru pending-start cu timp pornit la primul PCM, pause/resume/seek freeze și loop fără restart. Nu face lookup în slot la fiecare tick care ar retargeta implicit.
- [x] Îngheață API-ul C# audio Motion de mai sus și lowering-ul comun: overload specific SoundPlayback, descriptori Volume/custom float, From/current/To/With și handle Motion rezultat, ownership/clock și comportament fără Aspect. Probează rezolvarea overload-ului ca să nu selecteze ObjectMotionRuntime.Current; nu migra calea generică existentă.
- [x] Citește complet ownerii/runtime bridge aleși și reenumeră callerii din generator/runtime/template/Prism/object paths; identifică source facts, nu presupune API-uri speciale pentru nonvisual targets.
- [x] Rulează GREEN-urile existente: ConditionalMotionWaitsForEffectiveVisibilityBeforeStarting, DetachCancelsOnlyTheOwningMotionSession, same-session handle replacement, hidden-target UI binding, Prism nonrenderable cancellation și ObjectMotion nedesenat.
- [x] Adaugă RED generator/semantic pentru .sound. care eșuează pentru target-ul unsupported actual; după suprafața core/markup compilabilă, adaugă RED runtime determinist cu manual clock pentru hidden-control continuation/captured instance/cancel. O eroare de path care accesează accidental template parts nu este o implementare audio validă.
- [x] Formalizează compatibilitatea specs/ranges aprobate: Tween/Spring/keyframes/mixers/From/current/To/hold/conflicts existente, setter manual anulează binding-ul parametrului, Reduced Motion nu dezactivează implicit audio. Samples invalid opresc numai animația cu ultima valoare validă și diagnostic, fără clamp cutoff/gain pentru Spring.
- [x] Fixează observatori pentru identity/param samples/owned Motion nodes/publications și layout/render counters; clock/domain transfer și eventualele ramp-uri audio sunt contracte de core, nu magie de emitter.

**Gate etapa 0**

- [x] Target/runtime/lifecycle sunt stabilite cu caller coverage și RED-uri pentru invarianta dorită; politica vizuală și Window.Hide sunt explicit separate. Compatibilitatea tip/range/spec este decisă înaintea implementării binding-ului.

### Etapa 1 — semantic binding și emission tipat

- [x] Adaugă rezolvarea .sound.Handle.Parametru înainte de ordinary property/parts fallback, cu schema statică validată a handle-ului; getter/setter sunt tipați și generated code nu folosește reflection/dynamic/string lookup per frame.
- [x] Lower @animate/@from/@to și compositions Motion existente către binding-ul audio; binding capturează playback instance/descriptor la activare, după @sound din același body.
- [x] Adaugă suprafața/fațada C# audio și lowering-ul către contractul aceluiași binding; verifică consumer compilation pentru apeluri directe și generated partial/factory. Compară binding-ul semantic al apelurilor; niciun helper markup nu poate expune altă politică de parametri decât API-ul aprobat. Comportamentul/lifecycle-ul binding-ului se implementează numai după RED-urile etapei 2.
- [x] Testează intrinsic Volume/custom float, parameter→multiple modifiers, override initial/current, clip schema incompatibilă, handle necunoscut/gol, wrong type/undeclared parameter și context scope invalid.
- [x] Rulează corpusul pozitiv și negativ Language/SourceGen, generated C# compilation și completion/navigation path tests. Nu extinde `$owner`/named-element audio paths ca feature suplimentar dacă nu există cerință aprobată.

**Gate etapa 1**

- [x] .sound. este target semantic audio real; diagnostics/typing corespund între hosts, iar .prism./parts/UI paths existente rămân identice.

### Etapa 2 — identitate, transport, cancel și hidden controls

- [x] Înainte de runtime changes, confirmă RED pentru captured identity, înlocuire mid-animation și hidden-ancestor continuation pe motor/graph/scope reale.
- [x] Implementează binding-ul și teardown-ul: cancel audio oprește animațiile lui, replacement eliberează binding-uri vechi fără mutarea lor pe noua instanță; completion/failure/pending-cancel respectă core state machine.
- [x] Rulează timeline determinist cu Tween și specs aprobate: valori inițiale/între/finale, `current`, interruption/conflict policy și completion hold. Nu deduce performanță audio din timing-ul unui test manual clock.
- [x] Folosește harness-ul generated-consumer din planul markup pentru paritate C#/.sound.: aceleași samples pe identitatea capturată, Volume/custom float, pending start, pause/resume/seek/loop și înlocuire mid-animation. Slotul gol din markup rămâne no-op; API-ul C# pentru referință null/terminală respectă contractul propriu înghețat, fără a inventa o echivalență cu slotul gol.
- [x] Confirmă RED înainte de bridge transport, apoi implementează/testează cu manual clock pause/resume freeze exact pe identitatea capturată, Pause Pending înainte de primul PCM, freeze temporar seek fără retiming/restart, latest-seek supersession și loop fără repetarea animației. Visual Motion continuă după propria politică; queued audio nu schimbă timeline-ul unei alte instanțe.
- [x] Confirmă RED/testează setter manual → cancel doar același parametru, Spring overshoot/nonfinite → binding canceled/ultima valoare validă/diagnostic/sunet continuat, terminal mutation throws și Reduced Motion distinct. Compară C# cu generated-consumer, nu numai bridge direct.
- [x] Testează direct și ancestor Hidden/Collapsed: samples audio continuă în root activ; UI/Prism Motion se anulează conform contractului vechi. Hide→show nu repornește clipul sau animația.
- [x] Testează detach/reattach, template/Aspect swap, două scopes și 100 cicluri replacement/cancel: zero publicații în identitatea veche după bariera de teardown, active audio bindings/resources revin la baseline.
- [x] Caracterizează Window.Hide/pump: nu cere sampling în hidden native context și nu introduce al doilea clock sau GPU render doar pentru sunet. Revenirea ferestrei (nu SoundPlayback.Resume) folosește politica de clock/delta Motion actuală; nu pretinde că orice minimized window are viewport zero.

**Gate etapa 2**

- [x] Contractele aprobate de identitate/hidden-control/cancel/detach sunt demonstrate, compatibilitatea legacy trece și handle gol nu pornește audio.

### Etapa 3 — PCM/native cost și documentație

- [x] Aplică samples Motion prin publicații tipate/coerente către DSP; testează cu PCM tap că Volume/cutoff/echo changes afectează doar playback-ul capturat, inclusiv streaming și două redări simultane.
- [x] Măsoară warm CPU/allocations/publication counts și invalidări pentru animație audio în root vizibil și control ascuns; no-op param changes nu măsoară/aranjează/invalidează desenul. Folosește protocoalele din index, nu promisiuni zero-work.
- [x] Rulează native fixture Windows cu events/reactive/Pause/Resume/Seek/Loop și animation pe WAV/MP3/Vorbis/Opus; click/hover prin input user-like, cu raw PCM/diagnostics și cancel mid-flight.
- [x] Documentează .sound. target schema, specs/range/hold/conflict, pending/terminal/transport freeze/loop și hidden/window distinction în ghid și canonical API pages folosind writing-api-documentation; actualizează manifestul și compilă exemplele.
- [x] Rulează proiectele Timbre/Core/SourceGen/Language/LSP/VS/Preview afectate, strict ApiCompat/manifest/full solution și Windows native matrix și N/A explicit non-Windows din index; review actual source/diff și cleanup înaintea checkpoint-ului.

**Gate etapa 3**

- [x] Motion audio este acceptat prin probe typed/runtime/PCM/native/cost și documentație, fără schimbări collaterale ale Motion vizual sau output transport.

## 6. Comenzi și definiția de gata

```powershell
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj -c Release --filter "FullyQualifiedName~GeneratedMarkupMotionTests|FullyQualifiedName~MarkupMotionExecutionTests|FullyQualifiedName~MotionPropertyBindingTests|FullyQualifiedName~PrismMotionIntegrationTests|FullyQualifiedName~MotionSystemTests|FullyQualifiedName~ObjectMotionTests"
dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj -c Release --filter "FullyQualifiedName~SoundMotion|FullyQualifiedName~UiMarkupGeneratorTests|FullyQualifiedName~PrismMarkupContractTests"
dotnet test .\tests\Cerneala.Tests.Language\Cerneala.Tests.Language.csproj -c Release --filter "FullyQualifiedName~SoundMotion|FullyQualifiedName~MotionPrismSemanticTests"
```

Probele native/runtime non-Windows sunt N/A conform politicii din index §7; Windows și asset checks rămân separate, fără parity pretinsă.

Noile filtre SoundMotion se folosesc după crearea testelor și se verifică numărul selectat. Proiectul Timbre și full suites se rulează conform indexului după filtre.

- [x] Motion controlează parametri tipați ai playback-ului, cu identitate capturată și fără state partajat/retarget accidental.
- [x] Fațada audio C# și animația lower-uită din .crn folosesc același binding/clock/publishing și trec probele de paritate pe consumer generat executat, fără fallback către Object Motion sau target vizual fictiv.
- [x] Pause/Resume/Seek freeze/continuation și Loop fără retiming/restart sunt probate pe C# și markup; setter/ranges/terminal/Reduced Motion respectă contractul aprobat.
- [x] Hidden-control continuation și detach/cancel/replacement coexistă cu lifecycle-ul vizual vechi și cu suspendarea permisă prin Window.Hide.
- [x] SourceGen/Language/editor, runtime/PCM/native/cost, API/docs/manifest și full-suite gates sunt închise; niciun rezultat netestat nu este pretins.
