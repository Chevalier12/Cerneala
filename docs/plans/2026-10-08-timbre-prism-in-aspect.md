# Plan: Timbre și Prism aduse de Aspect, `TimbreClip` ca set de sunete, `PrismComposition` → `PrismClip`

> Data: 2026-10-08
> Status: finalizat
> Baseline: commit `59c0e257` (redenumirea Sound → Timbre).
> Dependență: [programul Aspect per aplicare](2026-10-08-aspect-runtime-program.md) — gate-ul lui final este precondiție pentru Etapele 3–7. Etapele 1 și 2 (redenumiri) nu depind de el.
> Dependențe istorice: [markup/Aspect Timbre](2026-10-03-timbre-markup-aspect.md), [Motion parameters](2026-10-03-timbre-motion-parameters.md) — contractele lor de transport/Motion rămân valabile unde acest plan nu le înlocuiește.
> Scop: `@timbre` și `@prism` se scriu în Aspect și trăiesc cât aplicarea lui; `<TimbreClip>` devine un set de `@sound` numite, simetric cu `<PrismClip>` și `@layer`; sunetele se adresează mereu prin `$element.timbre.Nume`.

## 1. Rezumat

Regula aprobată: **„Dacă Aspect-ul este înlocuit, tot ce a adus cu el dispare; noul Aspect aplică din nou tot.”** Fundația face programul Aspect-ului să ruleze per aplicare. Acest plan pune Timbre și Prism în acel program și aliniază Timbre structural cu Prism: un container (`@timbre` / `<TimbreClip>`) cu noduri numite (`@sound`), adresate ca layer-ele Prism.

## 2. Decizii aprobate (2026-10-08)

| Decizie | Valoare |
|---|---|
| Unde se scriu | `@timbre` și `@prism` doar în corpul Aspect-ului (inline sau resursă); nu în conținutul elementului, nu în `@on`/`@when`/`@if`. |
| Cardinalitate | Cel mult un `@timbre` și cel mult un `@prism` pe Aspect. |
| Forme `@timbre` | `@timbre $Clip(args);` (resursă `<TimbreClip>`) sau `@timbre { … }` (inline, același corp ca `<TimbreClip>`). |
| Corpul unui `TimbreClip` | `@parameter Nume: float = valoare;` la nivel de clip (ca la `PrismClip`) și noduri `@sound Nume { Source; Volume; Loop; AutoPlay; @modifier … }`. Numele nodurilor sunt fără `$` și unice în clip. |
| Argumentele aplicării | Setează doar `@parameter`-ii clipului, ca `@prism $X(args)`. `Volume`, `Loop`, `AutoPlay` țin de fiecare `@sound`. |
| Adresare | Mereu path complet, fără formă scurtă: comenzi `@play $self.timbre.Click;`, `@stop`, `@pause`, `@resume`, `@seek $self.timbre.Click to 30s;`; Motion `$self.timbre.Hover.Volume`, `$self.timbre.Hover.<Parametru>`; alt element `$Speaker.timbre.Music` (`$Speaker` rezolvat la compilare în namescope-ul unde e scris Aspect-ul; în resursele din `App.crn` doar `$self`/`$owner`); `$owner.timbre.…`. Prism neschimbat: `$X.prism.<Nod>.<Proprietate>`. |
| Pornire | Sunetele stau oprite; un `@sound` cu `AutoPlay = true` pornește la fiecare aplicare a Aspect-ului. |
| `@play` pe redare activă | Restart. |
| Înlocuirea Aspect-ului | Dispar sunetele și efectul Prism aduse; noul Aspect le aplică pe ale lui (inclusiv AutoPlay). |
| Țintă indisponibilă | Element neatașat sau al cărui Aspect curent nu aduce acel sunet → `InvalidOperationException`. |
| Markup | Rămân `MotionClip`, `PrismClip`, `TimbreClip`. |
| C# | Tipul de azi `TimbreClip` (un sunet) devine `TimbreSound`; containerul este `TimbreClipDefinition` (nou), simetric cu `PrismClipDefinition`. |
| Forma C# a containerului | Numele stă pe nod, ca la Prism: `new TimbreClipDefinition("UiSounds", sounds: [new TimbreClipSound("Click", new TimbreSound(…)), …], parameters: […])`; `TimbreSound` rămâne fără nume; C# simplu: `element.Timbre.Play(ui.Sounds["Hover"].Sound)`. |
| Rezolvarea resursei `$UiSounds` | La aplicarea Aspect-ului; aplicările existente păstrează definiția până la următoarea aplicare (reattach sau înlocuire). |
| Aspect aplicat pe alt element | Permis (vezi planul-fundație); comenzile spre `$Speaker` rămân legate de Speaker-ul de la declarare. Dacă Aspect-ul lui Speaker se schimbă dar are tot `@sound Music`, comanda funcționează; altfel excepție. |
| `PrismComposition` → `PrismClip` | Complet: element markup, mesaje, tipuri publice/interne, pagini docs-site, manifest; „composition” generic rămâne. |
| Migrare | Fără alias: dispar `@timbre` ca acțiune, `as Handle`/`@handle`/`@cancel` pe sunet, `@prism` în conținutul elementului, `<PrismComposition>`, `<TimbreClip>` ca un singur sunet. `@handle`/`@cancel` rămân pentru Motion. |

## 3. Baseline (fapte observate)

- Timbre: `CernealaSemanticModel.Timbre.BindTimbreAspect` leagă acțiuni cu `@handle`; `ReportTimbreDirectivesOutsideAspects` respinge `@timbre` în afara Aspect-ului; `UiMarkupTimbreEmitter.ReadTimbreClip` coboară `<TimbreClip>` la un singur `TimbreClip` C#, iar acțiunile la `AttachTimbreSession` + `PlayTimbre(session, ResourceId, configure, handleName)` + Cancel/Pause/Resume/Seek pe sloturi; `GeneratedMarkupTimbre.MarkupTimbreSession` deține scope-ul și sloturile.
- Core: `TimbreScope.Play(clip, configure, handle)` cu handle anulează la start reușit doar ocupantul handle-ului (mecanismul de restart); `TimbreHandle.Current`/`Cancel` există. Tipul `TimbreClip`: ~490 apariții în 92 de fișiere (fără `docs/plans`, rezultate benchmark, `.trx`), cu pagină docs-site; numele `TimbreSound`/`TimbreClipDefinition` nu există încă.
- Motion audio azi: `$self.timbre.Handle.Param` (4 segmente) în `UiMarkupDirectiveParser.MotionValues`, `UiMarkupMotionResolver`, `UiMarkupTimbreMotionEmitter`, `CernealaSemanticModel.TimbreMotion`; schema = intersecția parametrilor clipurilor din handle.
- Prism: `@prism` se parsează în conținutul elementului (`DirectiveContentKind.Prism` în `UiMarkupElementEmitter`, `UiMarkupGenerator.cs:~811`; Language `CernealaSemanticModel.MotionPrism.cs` → `BindPrismApplications`, PRISM2013); în directivele Aspect este azi „not allowed in this directive context” (`UiMarkupDirectiveParser.cs:94`). Compoziția are `@parameter` la nivel de compoziție și aplicare cu argumente (`@prism $CardFx(GlowRadius = 24);` în testele SourceGen).
- `UI/Prism/Runtime/PrismAttachment.cs`: o atașare per element; `Dispose` scoate boundary-ul (`SetPrismVisualBoundary(false)`) și invalidează elementul. Motion Prism cere o aplicare statică pe țintă (`PrismMotionResolver.TryResolvePrismMotionOwner`).
- `@play`/`@stop` nu sunt keyword-uri azi.
- `PrismComposition*`: ~470 apariții în 100 de fișiere; tipuri publice `PrismCompositionDefinition`, `PrismCompositionState`; raportul `docs/prism-completeness-report.generated.md` generat de `Tools/PrismAudit`.
- Enforcement API: ApiCompat strict după modelul `docs/plans/evidence/2026-10-03-timbre-motion-stage3/api-compat.proj`; `tests/Fixtures/TimbreConsumer`; testul de manifest din `Cerneala.Tests.VisualStudio`.

## 4. Contract țintă

```xml
<UserControl.Resources>
    <TimbreClip Name="UiSounds">
        @parameter Brightness: float = 1200;

        @sound Click
        {
            Source = "audio/click.wav";
        }

        @sound Hover
        {
            Source = "audio/hover.wav";
            Volume = 0.3;
            @modifier LowPass { Cutoff = Brightness; }
        }
    </TimbreClip>

    <PrismClip Name="CardFx">
        @parameter GlowRadius: float = 18;
        @layer Foreground { @filter Blur { Radius = GlowRadius; } }
    </PrismClip>

    <Aspect Name="MenuButton" TargetType="Button">
        @timbre $UiSounds(Brightness = 800);
        @prism $CardFx(GlowRadius = 24);

        @on Click { @play $self.timbre.Click; }

        @on MouseEnter
        {
            @play $self.timbre.Hover;
            @animate with Tween(300ms, EaseOut)
            {
                @to
                {
                    $self.timbre.Hover.Volume = 0.8;
                    $self.prism.Foreground.Opacity = 0.5;
                }
            }
        }

        @on MouseLeave { @stop $self.timbre.Hover; }
    </Aspect>
</UserControl.Resources>

<Border Name="Speaker">
    <Border.Aspect>
        @timbre
        {
            @sound Music
            {
                Source = "audio/music.ogg";
                Loop = true;
                Volume = 0.4;
                AutoPlay = true;
            }
        }
    </Border.Aspect>
</Border>

<Button Content="Start" Aspect="$MenuButton" />

<Button Content="Music">
    <Button.Aspect>
        @on Click { @pause $Speaker.timbre.Music; }
    </Button.Aspect>
</Button>
```

- Comenzile sunt permise în `@on`/`@when`/`@if`, interzise în `@parallel`/`@sequence`; fără argumente per comandă.
- Ținta statică: într-un Aspect inline, `$Speaker.timbre.Music` cere ca Aspect-ul static al lui Speaker să aducă sunetul `Music` (altfel eroare de compilare). La runtime, element neatașat sau Aspect înlocuit fără `Music` → `InvalidOperationException`.
- Pe o țintă validă fără redare activă, `@stop`/`@pause`/`@resume`/`@seek` și Motion sunt no-op.
- Motion pe parametri: `$self.timbre.Hover.Brightness` animă valoarea parametrului pe redarea curentă a lui Hover; parametrul trebuie folosit de acel sunet.
- `<TimbreClip>` este resursă runtime rezolvată la aplicarea Aspect-ului; înlocuirea resursei nu schimbă aplicările existente și nu mută redările existente.

## 5. Arhitectură și ownership

- **C# core (`Timbre/`):** `TimbreSound` (fostul `TimbreClip`, aceeași semantică) rămâne unitatea redată de `TimbreScope.Play`. `TimbreClipDefinition` (nou; locul exact — `Timbre/` sau `UI/Timbre/` — se fixează în Etapa 0, fără dependență core → UI) = parametri comuni + sunete numite. Semnăturile se fixează în Etapa 0.
- **Behavior-ul Aspect-ului (fundația) deține tot:** pentru `@timbre`, un set de atașări (`UI/Timbre/TimbreAttachment.cs`, intern) — câte un `TimbreHandle` per `@sound` într-un scope al aplicării; pentru `@prism`, apelul `AttachPrism` existent. Dispunerea behavior-ului oprește sunetele și scoate efectul Prism.
- **Registru per element** (`ConditionalWeakTable<UIElement, …>`) cu sunetele curente după nume, scris și curățat de behavior; comenzile cross-element îl consultă. Elementul țintă este capturat la compilare din namescope-ul de declarare al Aspect-ului; nu există lookup după nume de element la runtime.
- **API public estimat (`GeneratedMarkup`, fixat în Etapa 0 cu diff ApiCompat):** `AttachTimbre(UIElement target, ResourceId<TimbreClipDefinition> clip, Action<TimbreClipDefinition, …>? parameters)` (+ overload inline) → `IDisposable`; `PlayTimbre/StopTimbre/PauseTimbre/ResumeTimbre(UIElement target, string soundName)`, `SeekTimbre(UIElement target, string soundName, TimeSpan)`; `StartTimbreMotionProperty(..., UIElement target, string soundName, string parameterName, ...)`. Se elimină variantele pe sesiune + nume de handle.
- Prism din Aspect: la detach/reattach behavior-ul este recreat; azi `PrismAttachment.Attach` recreează deja instanța la fiecare attach, deci comportamentul nu se schimbă.

## 6. Non-obiective

- Lookup de element după nume la runtime.
- Formă scurtă de adresare (`@play Click;`, `@play $Click;`).
- Mai multe `@timbre`/`@prism` pe Aspect, compoziții stivuite.
- Overlap pe același `@sound`, argumente per `@play`, binding-uri în argumentele `@timbre`.
- Orchestrare în `@parallel`/`@sequence`; alias pentru sintaxa veche.
- Schimbarea modelului `MotionClip` (`@run … as Handle`), notat pentru discuția despre unificarea în Aspect.
- Schimbări în decodare, DSP, mixer, SDL, renderer Prism.

## 7. Callerii de migrat

| Caller | Dependență actuală | Migrare | Verificare |
|---|---|---|---|
| `Timbre/TimbreClip.cs` și toți consumatorii C# (`Timbre/**`, `UI/**`, `tests/**`, `benchmarks/**` sursă, `tests/Fixtures/TimbreConsumer`, docs-site) | tipul `TimbreClip` | `TimbreSound` | build + `Cerneala.Tests.Timbre`, `ExternalConsumerTests` |
| `Cerneala.SourceGen/UiMarkupTimbreEmitter.cs` | `<TimbreClip>` = un sunet; sesiune + sloturi | `<TimbreClip>` → `TimbreClipDefinition`; `AttachTimbre` în behavior; comenzi pe (target, sound) | `UiMarkupGeneratorTimbreTests` |
| `UiMarkupTimbreMotionEmitter.cs`, `UiMarkupMotionActivationEmitter.cs`, `UiMarkupDirectiveParser.MotionValues.cs`, `UiMarkupMotionResolver.cs` | `$self.timbre.Handle.Param` | `$X.timbre.Sound.Param` | `UiMarkupGeneratorTimbreMotionTests` |
| `Cerneala.SourceGen/Prism/Emission/PrismMarkupEmitter.cs`, `UiMarkupElementEmitter.cs`, `UiMarkupGenerator.cs`, `UiMarkupDirectiveParser.cs`, `Prism/Binding/PrismMotionResolver.cs`, `PrismMarkupBinder.cs` | `@prism` în conținutul elementului | `@prism` în Aspect, emis în behavior | teste Prism SourceGen |
| `Cerneala.Language/Semantics/CernealaSemanticModel.Timbre.cs`, `.TimbreMotion.cs`, `.MotionPrism*.cs`, `Timbre/TimbreMarkupSyntax.cs`, `TimbreMarkupBinder.cs` | clip = un sunet; acțiuni cu handle; Prism în conținut | clip = set de `@sound`; `@timbre`/`@prism` în Aspect; comenzi și scheme Motion | corpus Language |
| `Cerneala.Language/Features/CernealaCompletionService.Timbre.cs`, `.Directives.cs`, `CernealaLanguageFacts.cs`, `Syntax/Embedded/DirectiveSyntaxParser.cs` | keyword-uri și completion vechi | `@sound`, `@play`/`@stop`, `$X.timbre.` | `TimbreToolingTests` |
| `Cerneala.VisualStudio/Grammars/cerneala.tmLanguage.json` + golden | `@(?:timbre|pause|resume|seek|modifier)`, tag `PrismComposition` | `@sound`, `@play`, `@stop`, `PrismClip` | `CernealaGrammarTests` |
| `UI/Markup/GeneratedMarkupTimbre.cs`, `UI/Markup/GeneratedMarkupPrism.cs` | sesiune cu sloturi; `AttachPrism` din element | `TimbreAttachment` + registru; `AttachPrism` din behavior | `Cerneala.Tests.Timbre`, `PrismAttachmentTests` |
| Markup cu `@prism` în conținut: `tests/Fixtures/VisualStudioConsumer/*.crn`, `Playground/Cerneala.Playground/*.crn`, corpus/teste Prism, `docs/prism-guide.md`, `docs/CernealaMarkupGuide.md` §15 | sintaxa veche | mutat în Aspect | build + teste |
| `tests/Cerneala.SdlGpuSmoke/TimbreMarkupPanel.crn`, `TimbreMotionPanel.crn`, `tests/Cerneala.Tests.PreviewHost/PreviewHostTests.cs`, `docs/timbre-guide.md`, docs-site `GeneratedMarkup.md`, `MarkupConditionRule.md`, `Cerneala.Timbre.TimbreClip.md` | sintaxa/tipul vechi | sintaxa/tipurile noi | smoke, PreviewHost, `DocumentedTimbreExamplesCompile` |

Acoperire: căutări text pentru helper-ele publice, `@handle`+`@timbre`, `.timbre.`, `@prism`, `PrismComposition`, `TimbreClip`; potrivirile inspectate în context. Inventarele complete se refac în Etapa 0.

## 8. Etape de implementare

### Etapa 0 — Baseline, inventar, contract C#, RED

- [x] Confirmă gate-ul final al planului-fundație (precondiție pentru Etapele 3–7).
- [x] Fixează semnăturile `TimbreSound` (fost `TimbreClip`, fără schimbare de semantică), `TimbreClipDefinition` (parametri + sunete numite, locul în assembly fără dependență core → UI) și helper-ele `GeneratedMarkup`; scrie diff-ul ApiCompat așteptat față de `59c0e257`.
- [x] Inventariază toate `@prism` din conținutul elementelor, toate `<TimbreClip>` și toate `@timbre`/`@handle`/`@cancel` pe sunet; tabel de migrare.
- [x] Test pentru rezolvarea resursei la aplicare: înlocuirea resursei `<TimbreClip>` nu schimbă aplicările existente; următoarea aplicare folosește definiția nouă. (Prism la reattach: `PrismAttachment.Attach` recreează deja instanța azi — fără schimbare de comportament.)
- [x] Îngheață corpusul pozitiv al §4 și negativ: `@timbre`/`@prism` în conținutul elementului, două `@timbre` sau două `@prism`, `@sound` cu nume duplicat, `@parameter` în `@sound`, argument care nu e `@parameter` al clipului, adresare scurtă (`@play Click;`, `@play $Click;`), `@play $Speaker;`, `$Name` într-o resursă din `App.crn`, `$Speaker.timbre.Lipsa`, Motion pe parametru nefolosit de sunet, comandă în `@parallel`, sintaxa veche cu handle.
- [x] RED Language/SourceGen pentru `@timbre`/`@prism` în corpul Aspect-ului și `@sound` în `<TimbreClip>`; RED runtime (după fundație): AutoPlay la aplicare, swap A→B (sunetele și Prism-ul lui A dispar, ale lui B apar, AutoPlay repornește), comandă cross-element pe țintă neatașată aruncă.
- [x] Baseline GREEN salvat în `docs/plans/evidence/2026-10-08-timbre-prism-in-aspect-stage0/`.

**Gate etapa 0**

- [x] Semnături C# și diff ApiCompat fixate; inventare complete; RED-uri eșuând pe comportament. Evidence: `docs/plans/evidence/2026-10-08-timbre-prism-in-aspect-stage0/README.md`.

### Etapa 1 — `PrismComposition` → `PrismClip` (independentă)

- [x] Redenumește elementul markup, `ResourceKind`, listele de elemente speciale, completion, LanguageServer, gramatica VS.
- [x] Redenumește `PrismCompositionDefinition` → `PrismClipDefinition`, `PrismCompositionState` → `PrismClipState`, identificatorii interni `*PrismComposition*` și fișierele (`git mv`); nu atinge `PrismClipToBelowDiagnostic`, `PrismClippingStyle`, `ClipToBelow`, „composition”.
- [x] Mesajele PRISM (de ex. PRISM2002 „Unknown PrismClip resource”) și testele lor.
- [x] `Tools/PrismAudit/Program.cs`; `dotnet run --project Tools/PrismAudit/PrismAudit.csproj -c Release -- --write`, apoi `-- --check`.
- [x] Markup, golden VS, docs (`docs/prism-*.md`, `docs/CernealaMarkupGuide.md`, `DOCUMENTATION_CHECKLIST.md`, `PrismChecklist.md`), pagini docs-site + `manifest.json` + link-uri, cu `writing-api-documentation`.

**Gate etapa 1**

- [x] `git grep -w -E 'PrismComposition[A-Za-z]*'` gol în afara `docs/plans`, rezultatelor benchmark, `.trx`; build fără erori; `Cerneala.Tests`, `.Language`, `.SourceGen`, `.LanguageServer`, `.VisualStudio`, `.SdlGpu` GREEN; `PrismAudit -- --check` trece; ApiCompat arată doar redenumirile. Evidence: `docs/plans/evidence/2026-10-08-timbre-prism-in-aspect-stage1/README.md`.

### Etapa 2 — `TimbreClip` → `TimbreSound` în C# (independentă)

- [x] Redenumește tipul public `TimbreClip` → `TimbreSound` și identificatorii C# legați de el (fișier, pagină docs-site, manifest, exemple, `tests/Fixtures/TimbreConsumer`, benchmark-uri sursă); markup-ul `<TimbreClip>` rămâne neschimbat în această etapă.
- [x] Nu schimba semantica: aceleași constructori, parametri, modificatori, loading.

**Gate etapa 2**

- [x] `git grep -w TimbreClip` în cod C# găsește doar referințe la elementul markup (string-uri din Language/SourceGen); build fără erori; `Cerneala.Tests.Timbre`, `.SourceGen`, `Cerneala.Tests`, `.SdlGpu` GREEN; ApiCompat arată doar redenumirea. Evidence: `docs/plans/evidence/2026-10-08-timbre-prism-in-aspect-stage2/README.md` (include proprietatea `TimbrePlayback.Clip` → `Sound` și parametrii `clip` → `sound`).

### Etapa 3 — Core: `TimbreClipDefinition`

- [x] Adaugă `TimbreClipDefinition` cu semnăturile din Etapa 0 (parametri comuni, sunete numite unice, validarea parametrilor folosiți de modificatori), cu teste de definiție după modelul `tests/Cerneala.Tests.Timbre/Definitions/TimbreDefinitionTests.cs`.

**Gate etapa 3**

- [x] Testele de definiție GREEN; `TimbreArchitectureTests` GREEN. Evidence: `docs/plans/evidence/2026-10-08-timbre-prism-in-aspect-stage3/README.md`.

### Etapa 4 — Language

- [x] `<TimbreClip>` cu `@parameter` la nivel de clip și noduri `@sound`; `@timbre $Clip(args);` și `@timbre { … }` în corpul Aspect-ului; `@prism` în corpul Aspect-ului; cel mult unul din fiecare; erori pentru formele vechi.
- [x] Comenzi `@play/@stop/@pause/@resume/@seek` numai pe path complet `$self|$owner|$Name.timbre.Sound` (`$Name` rezolvat în namescope-ul de declarare); validarea statică a țintei din Aspect-ul static al elementului numit.
- [x] Scheme Motion `$X.timbre.Sound.Volume|Parametru` și Prism `$X.prism.…` pe Aspect-ul țintei.
- [x] Simboluri pentru navigare/hover pe clip, `@sound`, parametri, comenzi și ținte.

**Gate etapa 4**

- [x] Corpusul din Etapa 0 trece în `Cerneala.Tests.Language`; testele Motion/Prism existente (migrate sintactic) GREEN. Evidence: `docs/plans/evidence/2026-10-08-timbre-prism-in-aspect-stage4/README.md`.

### Etapa 5 — Runtime

- [x] `TimbreAttachment` + registrul per element; RED-uri deterministe (`DeterministicTimbreOutput`, manual clock) înaintea corpurilor: AutoPlay la aplicare, restart, stop/pause/resume/seek, detach oprește, swap oprește și noul Aspect aplică, hidden nu oprește, comandă pe țintă neatașată sau fără sunet aruncă, comenzi fără redare activă = no-op.
- [x] Helper-ele `GeneratedMarkup` din §5; `StartTimbreMotionProperty` capturează redarea curentă a sunetului (regulile planului Motion: timp de la primul PCM, freeze la pause/seek, înlocuirea redării încheie animația).
- [x] `TimbreArchitectureTests`, `ExternalConsumerTests`.

**Gate etapa 5**

- [x] `Cerneala.Tests.Timbre` GREEN; testele Motion audio existente (migrate) GREEN. Evidence: `docs/plans/evidence/2026-10-08-timbre-prism-in-aspect-stage5/README.md`.

### Etapa 6 — SourceGen

- [x] Coboară `<TimbreClip>`/`@timbre { … }` la `TimbreClipDefinition`; emite `@timbre` și `@prism` în behavior-ul Aspect-ului; comenzi pe (target, sound); Motion `.timbre.Sound.` și `.prism.`.
- [x] Elimină emisia veche (sloturi, `timbreCancels`, `@prism` din conținut).
- [x] Migrează `UiMarkupGeneratorTimbreTests`, `UiMarkupGeneratorTimbreMotionTests`, testele Prism SourceGen și harness-ul de paritate (`GeneratedTimbreConsumer`, `TimbreMarkupParityTests`): markup și C# (`TimbreClipDefinition`/`TimbreSound`) produc aceleași redări; fixture-uri factory, partial-paired, template, Scene2D/Sprite2D; simbolurile apelurilor emise verificate.

**Gate etapa 6**

- [x] `Cerneala.Tests.SourceGen`, `Cerneala.Tests.Timbre`, `Cerneala.Tests` (Prism) GREEN; diagnosticele Language și SourceGen coincid pe corpus. Evidence: `docs/plans/evidence/2026-10-08-timbre-prism-in-aspect-stage6/README.md`.

### Etapa 7 — Tooling, documentație, smoke, verificare completă

- [x] Completion (`@timbre`, `@sound`, `@prism` în Aspect, `@play`…, `$X.timbre.`), gramatica VS + golden, LanguageServer, PreviewHost (recompilarea retrage atașările; AutoPlay în preview-ul nou).
- [x] `docs/timbre-guide.md` (inclusiv C#: `TimbreSound`, `TimbreClipDefinition`), `docs/CernealaMarkupGuide.md` §14–15, `docs/prism-guide.md`; docs-site cu `writing-api-documentation` (pagini noi/redenumite + manifest).
- [x] Panourile smoke migrate cu același număr de scenarii; Windows `tests/Cerneala.SdlGpuSmoke --mode timbre-markup`, `--mode timbre-motion`, `--mode timbre`; `run.log` salvat.
- [x] ApiCompat strict față de `59c0e257`: exact diff-ul fixat (fundație + Etapele 0–2), fără suppression-uri.
- [x] `dotnet build .\Cerneala.slnx -c Release -m:1`; `dotnet test .\Cerneala.slnx -c Release --no-build --no-restore -m:1` cu `CERNEALA_SDL_NATIVE_TESTS=1`, `CERNEALA_TIMBRE_AUDIO_DEVICE=1`; `git diff --check`; fără cod de debug sau `NotImplementedException`.

**Gate etapa 7**

- [x] Suita completă GREEN (skip-uri doar preexistente), smoke-uri exit 0, ApiCompat clasificat, documentația compilată.

## 9. Politica de platformă

- Windows: runtime nativ obligatoriu și executat.
- Linux/macOS: runtime N/A, ca în indexul Timbre; nu se raportează ca GREEN.
- Validarea auditivă umană nu face parte din plan.

## 10. Ordinea recomandată

Etapele 1 și 2 se pot face oricând, inclusiv înaintea fundației. Etapa 3 după Etapa 2. Etapele 4–7 după gate-ul final al [planului-fundație](2026-10-08-aspect-runtime-program.md), în ordine.

## 11. Condiții de oprire

- O comandă cross-element care ar cere găsirea elementului după nume la runtime.
- `TimbreClipDefinition` nu poate sta în core fără dependență de UI: oprește-te și decide locul cu utilizatorul.
- Nu se extinde planul cu forme scurte de adresare, overlap sau compoziții stivuite.

## 12. Definiția de gata

- `PrismClip`/`PrismClipDefinition`/`PrismClipState` înlocuiesc `PrismComposition*`; `TimbreSound` înlocuiește tipul C# `TimbreClip`; `TimbreClipDefinition` există.
- `<TimbreClip>` este un set de `@sound` cu `@parameter` comuni; `@timbre` și `@prism` se scriu doar în Aspect, cel mult câte unul, și dispar/reapar odată cu aplicarea lui; AutoPlay pornește la fiecare aplicare; `@play` repornește.
- Sunetele se adresează doar ca `$element.timbre.Nume`, în comenzi și Motion; sintaxa veche dă diagnostice clare.
- Language, SourceGen, LSP, gramatica VS și PreviewHost sunt consecvente; ghidurile și paginile API descriu doar ce e implementat.
- Suita completă, smoke-urile Windows și ApiCompat sunt verificate cu evidence în `docs/plans/evidence/2026-10-08-timbre-prism-in-aspect-stage*/`.
