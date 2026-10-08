# Etapa 0 — inventar, RED și decizii rămase

Plan: [2026-10-08-aspect-runtime-program.md](../../2026-10-08-aspect-runtime-program.md). Starea codului: după Etapele 1–2 din planul Timbre/Prism (fără schimbări de producție în această etapă).

## 1. Emisia per element și migrarea ei în behavior

| Caller (azi) | Ce emite per element | Migrare |
|---|---|---|
| `UiMarkupAspectEmitter.ApplyAspects`, ramura Aspect default (`Name is null && !IsInline`) neprins de package (`!BehaviorOwnedByPackage`) — ~438–477 | `ResolveMotionAspect`, `EmitMotionPresence`, `EmitMotionLayout`, `EmitMotionActivations(bindToElementAspect: false)`, `EmitTimbreActivations(false)`, plus `EmitReactivePlan` pentru semnale și activări | În `AspectPackage.AddBehavior` prin `PrepareAspectBehavior(includeMotion: true)` pentru orice Aspect default (fără condiția `SupportsPackageBehavior`) |
| `ApplyAspects`, Aspect numit/inline cu `HasMotionBehavior` — ~482–494 și ~520–528 | aceleași, cu `bindToElementAspect: true` (sesiuni care observă `AspectProperty`) și `motionPlan` reactiv | În `ElementAspect.behaviorFactory` prin `PrepareAspectBehavior(includeMotion: true)` (azi `false` în `EmitNamedElementAspectResource` și `EmitLocalAspect`) |
| `UiMarkupMotionActivationEmitter.EmitMotionActivations` | `AttachMotionSession(variable[, variable.Aspect!])` în post-lines, `RegisterLifetime` pe contextul de template, `AddMotionTrigger`, execuții, `@cancel`, `EmitMotionScrolls/Drag/GesturePress` | Sesiunea devine lifetime returnat de behavior (`CombineLifetimes`); `RegisterLifetime` devine inutil (behavior-ul se dispune la detach) |
| `UiMarkupMotionActivationEmitter.EmitMotionPresence/EmitMotionLayout` (~390–418) | `Presence`/`LayoutMotion` + protecția „must be applied before the element is attached” | În behavior, fără protecție (decizia 1A); `UiMarkupGeneratorMotionPresenceTests`/`…LayoutTests` care așteaptă textul protecției se actualizează |
| `UiMarkupTimbreEmitter.EmitTimbreActivations` | `AttachTimbreSession(variable[, aspect])`, `AddTimbreTrigger`, acțiuni, execuții Motion audio | Idem Motion: lifetime al behavior-ului |
| `EmitMotionStaggerActivation` | variabila colecției numite (`CreateIdentifier(StaggerTarget.Name)`) | Captură din namescope-ul de declarare |
| `ResolveAspects` → `CloneAspectForApplication` (resurse fără variabilă runtime, de ex. din alt document) și `IsApplicationNamedAspect` → `AttachResource` | aplicarea Aspect-urilor de aplicație | Behavior-ul vine cu obiectul `ElementAspect`/`AspectPackage` al resursei; aplicarea rămâne atribuire |
| Runtime: `GeneratedMarkup.AttachMotionSession(owner, ElementAspect)`, `AttachTimbreSession(owner, ElementAspect)` (`aspectScoped`, observă `UIElement.AspectProperty`) | retragere la înlocuire | Varianta aspect-scoped devine inutilă; se simplifică în Etapa 3 |

Runtime-ul suportă deja: `UIElement.OnPropertyChanged(AspectProperty)` dispune behavior-ul vechi și atașează noul; `AttachToRoot` / `DetachFromRoot` (~679–712) fac același lucru la attach/detach.

## 2. `$Name` în Aspect-uri resursă (inventar)

Căutare în `.crn`, `.cs`, `.md`, `.json` (fără `docs/plans`, rezultate, `FileTree.md`) a țintelor `$X.Proprietate =` și `@stagger $X` în `<Aspect Name="…">`:

- `$self.parts.$X` (relativ la țintă, nu e problemă): `CernealaPresentation/PresentationWindow.crn` (`NavButton`), `UiMarkupGeneratorMotionTests` (`TemplatedMotion`).
- `$owner.parts.$Chrome` într-o resursă aplicată în template: `UiMarkupGeneratorMotionTests` (`TemplateChildMotion`) → decizia 2A.
- `$Name` real: `UiMarkupGeneratorMotionTests` `ForwardNamedMotion` → `$Child` (declarat după resursă, descendent al elementului de aplicare), `HostMotion` → `$Target.parts.$Chrome`; `StructureTests` `Interactive` → `$Action`; `docs/motion-markup-syntax-proposal.md` (exemple istorice de propunere).
- Nicio resursă din `App.crn` cu `$Name`.

**Regula de azi** (`UiMarkupMotionResolver.FindMotionNamedElement`): `$Name` se caută numai în `applicationElement.DescendantsAndSelf()`. Decizia din plan (namescope-ul unde e scris Aspect-ul) o extinde: un frate ca `$Speaker` devine valid; cazurile existente (descendent cu nume unic în namescope) rezolvă același element.

## 3. Resurse declarate în template-uri

`UiMarkupElementEmitter` emite `EmitRuntimeResources(element, variable)` imediat după crearea elementului, în contextul de emisie curent; pentru elementele din `@template`, acel context este factory-ul template-ului, deci resursele (inclusiv Aspect-urile) se creează per instanță de template și capturează elementele acelei instanțe. Nu e nevoie de lookup la runtime.

## 4. Ordinea la primul attach

`UIElement.AttachToRoot` (~679): `AttachElementAspectBehavior()` → `Initialized` → `OnAttached()` → lifecycle behaviors → `Motion.Layout.MarkAttached` → `Motion.Presence.MarkAttached` → `Loaded`. Un `@presence`/`@layout` setat de behavior la primul attach este deci văzut de coordonatori înainte de decizia de intrare, iar handler-ele `@on Loaded` sunt abonate înaintea evenimentului. `PresenceCoordinator` citește `element.Presence` la intrare/ieșire; `LayoutMotionCoordinator.OnPropertyMutated` tratează schimbarea pe element atașat. Protecția „must be applied before the element is attached” există numai în codul generat (din `e96d8a68`).

## 5. Punctele de creare a elementelor de template

`ComponentTemplate<TControl>.CreateInstanceCore` (`UI/Controls/Templates/ComponentTemplate.cs`) — singurul punct: `factory(typed)` construiește root-ul cu `ComponentTemplateContext.Owner` cunoscut. Template-urile imbricate ale controalelor copil trec prin propriul `CreateInstance`. `UiMarkupUserControlGenerator` construiește un `ComponentTemplateContext<>` pentru UserControl-urile pereche. Legătura element → owner se înregistrează acolo.

## 6. Teste (`tests/Cerneala.Tests.Timbre/Markup/AspectRuntimeProgramTests.cs`, `red.trx`)

Singurul harness care compilează și rulează markup generat este `MarkupTimbreFixture` (`Cerneala.Tests.Timbre`); `Cerneala.Tests` nu are unul, iar testele SourceGen verifică textul generat. Testul Motion după swap stă deci aici (planul a fost corectat).

| Test | Tip | Rezultat |
|---|---|---|
| `StaticAspectRunsItsProgram` | caracterizare | GREEN (`audio/other.wav`, `Opacity` 0.25) |
| `DetachAndReattachKeepExactlyOneProgram` | caracterizare | GREEN (o singură redare) |
| `ReplacementAtRuntimeAppliesTheNewAspectProgram` | RED | eșuează: nicio redare după swap A→B (`[]`) |
| `ReplacingBackAndForthKeepsExactlyOneProgram` | RED | eșuează: `[]` după A→B→A→B |
| `OneResourceAppliedAtRuntimeGivesEachElementItsOwnProgram` | RED | eșuează: `[]` |
| `DefaultAspectBringsItsProgramToAnElementCreatedAtRuntime` | RED | eșuează: `[]` pentru butonul creat la runtime |
| `InlineAspectAssignedToAnotherElementKeepsItsNamedReferences` | RED (Etapa 1 + runtime) | generatorul respinge `$Speaker` (frate): CERNEALAUI021 „not available at this Aspect application site” — exact regula pe care o schimbă Etapa 1; partea de runtime se verifică după Etapa 1 |

## 7. Baseline GREEN

Codul de producție este identic cu starea verificată în `../2026-10-08-timbre-prism-in-aspect-stage2/` (TRX acolo): `Cerneala.Tests` 4198/2 skip, `Cerneala.Tests.SourceGen` 641, `Cerneala.Tests.Language` 371/1 skip, `Cerneala.Tests.Timbre` 368, `Cerneala.Tests.PreviewHost` 22. Singura schimbare ulterioară este fișierul de teste nou de mai sus.
