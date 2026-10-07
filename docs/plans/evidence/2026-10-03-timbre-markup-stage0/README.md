# Timbre markup/Aspect — etapa 0: matrice syntax/activation și compatibilitate

Snapshot: `master` @ `a838fc35` + planuri necomise. Host: Windows 11 Pro 10.0.26200 x64, SDK .NET 10.0.400 (teste net8.0/net10.0).
Prerechizite: core/runtime, decodare/streaming și SDL3 au statusul `finalizat` (0 căsuțe nebifate fiecare).
Contractul aprobat (index §1/§2, plan markup §2) nu este redeschis; acest document îl formalizează mecanic.

## 1. Corpus înghețat

Fișier versionat: `tests/Cerneala.Tests.Language/Corpus/sound-corpus.json` (11 pozitive, 43 negative). Fiecare caz are
markup complet, familia (`SoundClip`, `Action`, `Reference`, `Value`, `Syntax`, `Context`) și, pentru negative, id-ul
de diagnostic plus fragmentul de mesaj care formează contractul. Etapa 1 îl consumă pentru paritatea Language/SourceGen.

- Pozitiv: clip simplu; exemplul aprobat (fără blocul `@animate … $self.sound.*`, livrat de planul Motion dependent);
  `Loop = true;` default; un parametru care alimentează două intrări; SoundClip în `Application.Resources`; shadowing;
  `@when/@if` cu `@cancel`; transport `@pause/@resume/@seek … to 30s`; body mixt `@sound` + Motion vizual; Aspect numit
  aplicat pe două butoane; `Sprite2D.Aspect` în Scene2D.
- Negativ: clip/parametru/handle necunoscut, resursă care nu e SoundClip, argument/parametru/handle/resursă duplicat,
  parametru folosit înaintea declarării, modifier/intrare/proprietate necunoscută, tip/range greșit (Volume, default,
  override, intrare modifier, Delay.Time), Loop non-boolean (clip și argument), seek fără unitate/negativ/fără `to`,
  `@sound/@pause/@resume/@seek` fără `;`, Source lipsă/URI, atribut/copil nepermis pe SoundClip, context greșit
  (top-level Aspect, `@default`, `@parallel`, MotionClip, SoundClip, conținut de element, `@modifier` în afara clipului),
  conflict de nume audio/Motion pentru același handle.

Coduri noi (categoria `Cerneala.UiMarkup.Sound`, erori la build și editor):

| Id | Titlu | Folosire |
| --- | --- | --- |
| CERNEALAUI030 | Invalid Sound markup syntax | `;` lipsă, formă `@seek`/`@parameter`, atribute/copii pe SoundClip |
| CERNEALAUI031 | Invalid Sound reference | clip/parametru/handle/modifier/intrare/proprietate necunoscută, duplicate, ordine, conflict audio/Motion |
| CERNEALAUI032 | Invalid Sound value | tip, range, unitate, Loop, Source |
| CERNEALAUI033 | Invalid Sound context | acțiuni în afara `@on/@when/@if` din Aspect, în compoziții, `@modifier` în afara SoundClip |

Duplicatele `@handle` și `Name` de resursă păstrează diagnosticele existente (CERNEALAUI020, CERNEALAUI005).
Cazurile `.sound.` ale exemplului complet aparțin planului Motion și nu blochează acest plan.

## 2. Gramatica și tiparea

SoundClip (resursă, nu control; numai atributul `Name`; fără copii XML; fără `TargetType`):

```text
Source = "relative/or/absolute.wav";     // obligatoriu, string nevid, fără "://"
Volume = 0.8;                            // 0–1, implicit 1
Loop = true;                             // true|false, implicit false
@parameter Nume: float = valoare;        // numai float, default obligatoriu, nume unic
@modifier LowPass { Cutoff = X; }        // X = literal sau parametru declarat înainte
@modifier Delay { Time = 120ms; Feedback = 0.2; Mix = X; }
```

Unități (catalogul comun `Timbre/Catalog/TimbreCatalog.cs`): un număr simplu este în unitatea catalogului (Hz, s, ratio,
gain); un literal cu sufix `ms`/`s` este acceptat numai unde unitatea așteptată este `s` și se convertește în secunde.
Range-ul unui parametru este intersecția intrărilor pe care le alimentează (identic cu constructorul `SoundClip`);
parametru nefolosit: orice valoare finită. Modificatorii se execută în ordinea sursei; intrările omise primesc default-ul
catalogului; același modifier poate apărea de mai multe ori (ca în C#).

Acțiuni (numai în corpul `@on`, în corpul boolean `@when` sau `@if`, la orice adâncime de `@when/@if` imbricate):

```text
@sound $Clip;                     @sound $Clip(Volume = 0.2, Loop = true, Param = 800) as Handle;
@cancel Handle;   @pause Handle;  @resume Handle;   @seek Handle to 30s;    // toate cu ';'
```

Handle typing: `@handle H;` rămâne declarația top-level comună cu Motion, înaintea folosirii. Un handle este **audio**
dacă apare în `@sound … as H`, `@pause/@resume/@seek H`; **Motion** dacă apare în `@run … as H`; ambele în același Aspect →
CERNEALAUI031. `@cancel H` urmează tipul handle-ului; un handle fără nicio folosire tipată păstrează comportamentul Motion
existent. Un handle audio poate primi clipuri diferite; `Volume` este comun; schema parametrilor custom comună tuturor
clipurilor posibile se calculează în modelul legat și este consumată de planul Motion (`$self.sound.H.Param`).
Numele rezervate `self/owner/root` și diagnosticele `@handle` existente rămân neschimbate.

## 3. AST, binding, emission și granițe

Owner build-time unic: **`Cerneala.Language/Timbre/`** (nou), `netstandard2.0`, fără dependență de UI core:

- `SoundMarkupParser` — parser al corpului SoundClip și al instrucțiunilor de acțiune, cu spans absolute și recovery
  pe `;`; nu deschide fișiere.
- `SoundMarkupBinder` — leagă corpul clipului la `BoundSoundClip` (Source, Volume, Loop, parametri cu min/max/default,
  modifiers cu intrări constante sau parametru) și acțiunile la `BoundSoundAction` (kind, handle, clip, override-uri,
  seek), validând exclusiv prin `TimbreCatalog` (fișier deja linkat în Language).
- `CernealaSemanticModel.Sound.cs` — partial nou: clasifică resursa `SoundClip`, contextul fiecărui keyword audio,
  tiparea handle-urilor per Aspect și publică `SoundMarkupModel` (clipuri după începutul elementului, acțiuni pe Aspect în
  ordinea documentului, handle kinds).
- `SourceGeneratorSemanticModel` exportă `SoundMarkupModel`; **SourceGen nu re-validează audio**: cursorul de directive
  recunoaște numai granițele instrucțiunilor (`@sound/@pause/@resume/@seek` până la `;`) și ia semantica din modelul legat
  (aspect după `Span.Start` al elementului, acțiunea după indexul ordinal în documentul Aspect-ului; nepotrivirea de kind
  sau handle este eroare internă, nu fallback). SoundClip din Application vine din modelul semantic al fișierului App.

Nu se introduc un al doilea parser Sound în SourceGen sau un resolver Aspect secundar; `AspectEngine` rămâne resolverul
valorilor. `@sound` nu devine `MotionExecutionNode`: nod nou `SoundActionNode`; un corp mixt conține zero sau mai multe
acțiuni audio și cel mult un root Motion (regula existentă de compoziție explicită rămâne pentru Motion). Pentru un
corp care conține acțiuni audio, verificarea „sibling Motion executions” se face după clasificarea handle-urilor
(un `@cancel` audio nu este execuție Motion); corpurile fără audio păstrează verificarea legacy neschimbată.

Inventar callers (snapshot curent, `rg` pe Language/SourceGen/LanguageServer/PreviewHost/VisualStudio/UI):

| Funcție comună | Callers | Tratament |
| --- | --- | --- |
| `DirectiveCursor.ParseNodes` / `ParseDirectiveContent` | 4 (AspectReader, MotionResolver/MotionClip, ReactiveEmitter, parser) | content kind nou `SoundActions` permis numai în `@on/@when/@if` din Aspect |
| `ValidateExplicitMotionComposition` | 3 (`@on`, `@when`, `@if`) | neschimbat pentru corpuri fără audio |
| `TryParseAspectBody` | 2 (resource + inline) | colectează handle kinds din model |
| `ResolveMotionAspect`, `EmitMotionActivations` | 2+2 (AspectEmitter) | neschimbate; `EmitSoundActivations` nou alături |
| `HasMotionBehavior`, `SupportsPackageBehavior` | 1+1 | audio ⇒ comportament per element (nu package), ca Motion |
| `CollectRuleBody` → `ReactiveRule` | 2 | listă separată de acțiuni audio |
| `EmitReactivePlan` | 9 (Aspect, Window, UserControl, reactive) | emite `soundActivated` numai când există |
| `MarkupConditionRule` ctor / `AttachConditions` | 2 / 1 | ctor nou aditiv; ctor-urile existente neschimbate |
| `ReadResources` / `EmitRuntimeResources` | 1 / 5 (Element, Window, UserControl, App) | caz `SoundClip` aditiv |
| Language `CreateResourceDefinition`, `IsSpecialElement`, `BindMotionProgram` | 3, 2, 3 | `ResourceKind.SoundClip`; keyword-urile audio nu mai sunt „unknown” |
| `CernealaLanguageFacts.MotionDirectiveKeywords` | 7 (facts, completion, semantic) | listă separată `SoundDirectiveKeywords`; lista Motion neschimbată |
| `SourceGeneratorSemanticModel.Create` | 1 | exportă modelul audio |

Formele de emisie inventariate: factory (`UiMarkupGenerator.GenerateFile`), paired UserControl/Window/Application/Scene
component, inline Aspect, Aspect numit (resursă element), Aspect implicit (package), `@template` și `ContentTemplate`/
ItemsControl (sesiunea se înregistrează în `RegisterLifetime` al contextului de template, ca Motion). Contractele lor de
construcție nu se schimbă.

## 4. Lowering către API-ul core și helpers

| Markup | C# generat |
| --- | --- |
| `<SoundClip>` | `new global::Cerneala.Timbre.SoundClip(SoundSource.FromFile(path), volume, loop, SoundLoading.Auto, parameters, modifiers)` cu `new SoundParameter<float>(name, default)`, `new LowPass(cutoff)`, `new Delay(time, feedback, mix)`; înregistrat cu `Resources.SetResource(new ResourceId<SoundClip>(name), clip)`. Fără I/O la construcție. |
| `@sound $C(args) [as H]` | `GeneratedMarkup.PlaySound(session, new ResourceId<SoundClip>("C"), (clip, start) => { start.Volume = …; start.Loop = …; start.Set(GeneratedMarkup.GetSoundParameter(clip, "P"), …); }, "H"|null)` → `SoundScope.Play(clip, configure, handle)` |
| `@cancel/@pause/@resume H` | `GeneratedMarkup.CancelSound/PauseSound/ResumeSound(session, "H")` → ocupantul curent al `SoundHandle`; slot gol sau ocupant terminal = no-op |
| `@seek H to 30s` | `_ = GeneratedMarkup.SeekSound(session, "H", TimeSpan.FromTicks(…))` → `SoundPlayback.SeekAsync`, nonblocking |

- Clipul se rezolvă **la fiecare start** prin lookup tipat `UIElement.FindResource<SoundClip>(ResourceId)` din elementul
  instanței (regulile existente de scope/shadowing + `UIRoot.ResourceProvider`/Application): înlocuirea resursei afectează
  numai pornirile viitoare; redările păstrează definiția capturată de core. Override-urile se leagă după numele
  parametrului în clipul rezolvat; o schemă incompatibilă după înlocuire aruncă sincron (start respins).
- Sesiunea audio (`GeneratedMarkup.AttachSoundSession(owner, ElementAspect?)`, `IDisposable`) este scope-ul per instanță
  concretă: un `ElementSoundOwner` (scope Timbre creat lazy din `UIRoot.SoundRuntime` la primul start) + sloturi
  nume→`SoundHandle`. Detach, înlocuirea Aspect-ului capturat și retragerea template-ului fac `Dispose` scope-ului:
  anulează redările cu și fără handle; reattach creează lifecycle nou. Renderability nu are efect.
- Triggere `@on` cu audio: `GeneratedMarkup.AddSoundTrigger(session, attach, detach)` — abonare pe durata attach (nu a
  renderability). Un corp mixt rulează în ordinea sursei; partea Motion se execută numai dacă
  `GeneratedMarkup.CanStartMotionExecution(motionSession)` (echivalent cu abonarea legacy doar cât e renderable).
  Corpurile numai-Motion păstrează emisia existentă.
- Helpers publici noi (aditivi, documentați în `Cerneala.UI.Markup.GeneratedMarkup.md`): `AttachSoundSession`,
  `AddSoundTrigger`, `PlaySound`, `GetSoundParameter`, `CancelSound`, `PauseSound`, `ResumeSound`, `SeekSound`,
  `CanStartMotionExecution`; ctor aditiv `MarkupConditionRule(…, Action<Action?>? soundActivated)`. Nu se generează apeluri
  SDL/decoder; numele `@handle` nu este o instanță `SoundPlayback`; nu se cere `InternalsVisibleTo` consumerului.

## 5. Observarea condițiilor ascunse și identitatea activării

`MarkupConditionController` rămâne observatorul unic (aceleași `MarkupObservation`, același `Evaluate`). Sidecar audio:
`soundActiveRules` separat de `activeRules` vizual.

- Initial true: la attach, activarea audio este amânată în același `Relay.Post` ca cea vizuală (ordinea corpului mixt
  rămâne a sursei), dar **nu** depinde de renderability: un control ascuns (sau strămoș ascuns) pornește sunetul.
- Reevaluarea aceleiași reguli active nu repetă (`soundActiveRules` conține regula). false → true pornește din nou,
  chiar fără render. true → false nu oprește (nicio deactivare audio).
- Hide → show cu regula stabil adevărată: visual Motion se reactivează (comportament existent, documentat); audio nu.
- Când ambele părți sunt datorate în aceeași trecere, controllerul apelează `soundActivated(rule.Activated)`, iar
  lambda generată rulează `s1(); motion?.Invoke(); s2();` — ordinea sursei. Doar audio: `soundActivated(null)`;
  doar vizual: `Activated()` ca înainte. Regula intră în `soundActiveRules` înaintea invocării (o eroare sincronă nu
  provoacă reîncercări la fiecare reevaluare).
- Detach (`Stop`) golește `soundActiveRules`; reattach = activare nouă. Versiunea de attach audio este separată de cea
  vizuală, deci schimbările de renderability nu anulează activarea inițială audio.
- `Window.Hide` poate suspenda pump-ul UI (deci `Relay.Post` și Motion sampling), fără a afecta transportul audio deja
  pornit; un control ascuns într-un root activ continuă să fie observat. Nu se schimbă politica pump-ului.

## 6. Pending, ordine și erori

- `PlaySound` întoarce identitatea la acceptare (Pending inclus) înaintea acțiunii următoare; nu se așteaptă pregătire/EOF.
- Eroare sincronă (fără `SoundRuntime`, element detașat, override invalid, schemă incompatibilă, seek peste durată
  cunoscută) se propagă și oprește restul corpului; acțiunile deja pornite rămân. Failure asincron (I/O/decoder/device)
  devine `Failed` în core, fără rollback și fără catch-and-ignore în helpers.
- `SeekSound` capturează ocupantul la momentul acțiunii, apelează `SeekAsync` și întoarce `Task`-ul fără să-l aștepte;
  ultima cerere înlocuiește pe cea precedentă (core); un ocupant înlocuit ulterior nu este retargetat.
- Pause/Resume/Seek/Cancel pe un ocupant care a devenit terminal concurent sunt tratate atomic (sub `runtime.Sync`,
  helper intern) ca slot gol — fără excepție de cursă în UI.

## 7. Baseline GREEN și RED

Baseline Release (`dotnet build .\Cerneala.slnx -c Release` 0 erori), apoi `--no-build`:

| Suită | Rezultat |
| --- | --- |
| Cerneala.Tests.SourceGen | 625 passed, 0 failed |
| Cerneala.Tests.Language | 259 passed, 1 skipped (preexistent), 0 failed |
| Cerneala.Tests.LanguageServer | 40 passed |
| Cerneala.Tests.VisualStudio | 47 passed |
| Cerneala.Tests.PreviewHost | 17 passed |
| Cerneala.Tests (filtru `Markup\|Aspect\|Motion\|Sound`, runtime conditions/Motion/Aspect/Timbre UI) | 573 passed |

RED, după adăugarea celor două teste minimale (build Release al proiectelor de test reușit, deci nu compilare/fixture):

```text
UiMarkupGeneratorTests.SoundClipAndEventSoundActionGenerateWithoutDiagnostics [FAIL]
  SoundMinimal.crn(3,6): error CERNEALAUI002: Markup element 'SoundClip' is not supported by the source generator
  SoundMinimal.crn(8,17): error CERNEALAUI020: Motion syntax in 'SoundMinimal.crn' is invalid: Unknown Motion directive '@sound'.
SoundSemanticTests.SoundClipAndEventSoundActionBindWithoutDiagnostics [FAIL]
  CERNEALAUI002 (2,5)-(2,14) Markup element 'SoundClip' is not supported …
  CERNEALAUI020 (7,16)-(7,22) Unknown Motion directive '@sound'.
```

Ambele eșecuri sunt exact capabilitatea lipsă (resursă și directivă nesuportate); nu implică SDL, device sau runtime.
Așteptările testelor existente nu au fost modificate.
