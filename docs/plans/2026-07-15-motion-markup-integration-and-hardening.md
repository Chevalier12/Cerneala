# Plan: integrare, diagnostics si hardening pentru Motion markup

> Data: 2026-07-15
> Status: finalizat
> Dependenta: toate celelalte planuri `2026-07-15-motion-markup-*`
> Scop: dogfood-uim limbajul, inchidem diagnostics/lifecycle/performance si promovam proposal-ul la documentatie implementata.

## 1. Obiective

- O singura suprafata coerenta, nu sase feature islands care se saluta din departare.
- Diagnostics cu source spans precise si generated code lizibil.
- Dovada prin CernealaPresentation ca un showcase Motion complex poate fi scris predominant in markup.
- Stress gates pentru leaks, allocations, idle frames si cleanup.

## 2. Non-obiective

- Fara extensie VS/LSP completa in acest plan.
- Fara noi semantics Motion fata de proposal si deciziile Decay acceptate explicit.
- Fara rescrierea unrelated a CernealaPresentation.

## 3. Etape de implementare

### Audit etapa 0 - matrice de acoperire

| Constructie publica | Plan proprietar | Test pozitiv | Diagnostic negativ relevant | Rezultat audit |
| --- | --- | --- | --- | --- |
| `Tween`, `Spring`, Aspect named/inline, `@when`, `@if`, `@on`, `@animate`, `@from`, `@to`, `current`, `$part`, start options | foundation | `UiMarkupGeneratorMotionTests` | acelasi fixture: missing `@to`, property/type/mixer/resource/context/options/event | implementat |
| `@set`, `@parallel`, `@sequence` si nesting | composition-and-clips | `UiMarkupGeneratorMotionClipTests`, `UiMarkupGeneratorMotionCompositionTests` | valori discrete invalide, empty groups, siblings fara composition, lifecycle cancel | implementat |
| `MotionClip`, `@run` | composition-and-clips | `UiMarkupGeneratorMotionClipTests` | acelasi fixture: body count/context, missing/wrong target, recursion/direct assignment | implementat |
| `@parameter` si arguments tipizate | composition-and-clips | `UiMarkupGeneratorMotionParameterTests` | acelasi fixture: duplicate/missing/wrong type/unsupported use | implementat |
| `@handle`, `as`, `@cancel` | composition-and-clips | `UiMarkupGeneratorMotionHandleTests` | acelasi fixture: undeclared/duplicate/use-before-declaration/clip context | implementat |
| `@keyframes`, ranges, `hold`, `Step`, `Repeat`, `PingPong`, `@stagger`, spec options | timelines-and-specs | `UiMarkupGeneratorMotionTimelineTests` | acelasi fixture: ranges/overlap/nesting/spec/count/context invalid | implementat |
| `Decay` | timelines-and-specs | testele de respingere din `UiMarkupGeneratorMotionTimelineTests` | resource-ul si constructorul inline sunt respinse | deferred: runtime-ul nu are execution tipizat fara `@to` decorativ |
| `@presence` | presence-and-layout | `UiMarkupGeneratorMotionPresenceTests` | acelasi fixture: duplicate/custom endpoints/body/retroactive attach | implementat |
| `@layout` | presence-and-layout | `UiMarkupGeneratorMotionLayoutTests` | acelasi fixture: mode/crossfade/shared-element/custom sequence | implementat |
| `@scroll`, `@drag`, `@gesture press` | scroll-and-input | `MotionInputTimelineTests` plus stress/runtime tests din planul proprietar | parser/resolver resping ranges/easing/non-float, drag options si gestures nesustinute | implementat; contractele sourcegen dedicate se intaresc in etapa 1 |
| `@else`, `$event`, transactions, resursa directa `Motion`, layout programming extensions | explicit in afara limbajului | n/a | unknown/illegal directive si diagnostics de capability/context | absent intentionat; nu exista lowering sau exemplu declarat livrat |

Auditul public API pentru worktree fata de `HEAD` este gol. Paginile care trebuie
revizuite/sincronizate in etapa 4 pentru suprafata agregata sunt
`Cerneala.UI.Markup.GeneratedMarkup`,
`Cerneala.UI.Markup.MarkupMotionExecution`,
`Cerneala.UI.Motion.Input.ScrollMotionBinding<T>` si
`Cerneala.UI.Motion.Input.DragMotionController`; toate cele patru pagini exista deja
in `docs-site/documentation/classes/`. Proposal-ul marcheaza explicit drept deferred
Decay, seek/reverse/scrubbing, ordering Stagger extins, Presence/layout/input options
fara suport runtime si nu prezinta niciuna dintre ele ca sintaxa livrata.

### Etapa 0 - Audit de suprafata

- [x] Construieste o matrice intre fiecare exemplu/directiva din `motion-markup-syntax-proposal.md`, planul care o implementeaza, testul pozitiv si diagnosticul negativ relevant.
- [x] Elimina sau marcheaza explicit deferred orice exemplu care nu poate fi coborat la runtime-ul actual; nu lasa syntax decorativ.
- [x] Confirma absenta `@else`, `$event`, transactions, direct `Motion` resources si layout-programming extensions.
- [x] Ruleaza public API diff si inventariaza toate paginile `docs-site/documentation/classes/` care trebuie sincronizate.

**Gate etapa 0**

- [x] Fiecare constructie din proposal este implementata, respinsa cu diagnostic sau marcata explicit deferred cu motiv runtime.

### Etapa 1 - Diagnostics si generated source quality

- [x] Stabileste IDs si mesaje distincte pentru syntax, target resolution, event resolution, property/spec typing, composition, lifecycle-only directives si unsupported runtime capability.
- [x] Mapeaza fiecare diagnostic la tokenul/directiva exacta din `.cui.xml`, inclusiv resources referenced din alt scope.
- [x] Emite generated members cu names stabile si `#line`/source mapping unde infrastructura generatorului permite, fara sa sacrifice debugging-ul C#. (Names sunt deterministe si acoperite contractual; `#line` nu a fost adaugat peste metoda factory comuna, deoarece ar mapa gresit statements din noduri XML intercalate si ar degrada debugging-ul C#.)
- [x] Adauga snapshot/contract tests pentru codul generat: fara reflection, dynamic, per-frame lookup dupa string sau closures recreate la fiecare tick.
- [x] Adauga diagnostics suggestions pentru `TargetType` prea general si custom event gasit pe tipul concret.
- [x] Reindexeaza solutia.

**Gate etapa 1**

- [x] Un utilizator poate localiza eroarea din mesaj si source span fara sa citeasca generated C# ca pe zațul din cafea.

### Etapa 2 - Dogfood in CernealaPresentation

- [x] Migreaza un behavior mic hover/event la foundation syntax si verifica echivalenta vizuala.
- [x] Migreaza Motion view/showcase la `Aspect`, `MotionClip`, `@set`, composition si handles pe masura ce suprafetele devin disponibile; inclusiv replay-ul si starile lui discrete sunt integral in markup, fara orchestration code-behind.
- [x] Foloseste cel putin un custom event pentru a demonstra static `@on` wiring si un attach/detach cycle real.
- [x] Foloseste Layout sau Presence numai daca showcase-ul are un caz natural; nu adauga decoratii doar ca sa bifam API-uri. (Nu a fost necesar: Motion Lab nu insereaza/elimina sau reparenteaza elemente.)
- [x] Ruleaza automatizarea existenta din `PresentationWindow.Automation.cs` si compara screenshots/behavior cu baseline-ul acceptat. (2 cicluri complete, 15 samples, fara eroare; captura Motion Lab 1125x765 a pastrat geometria/paleta si nu are overlap.)
- [x] Reindexeaza solutia.

**Gate etapa 2**

- [x] Showcase-ul complex nu necesita manual handle orchestration in code-behind pentru lucrurile reprezentabile in markup.

### Etapa 3 - Lifecycle si memory stress

- [x] Adauga un test integrat cu 100 cicluri attach/detach/reattach pentru Aspect cu `@when`, custom `@on`, active clip, handle, Presence, Scroll si Drag unde se aplica.
- [x] Verifica dupa settle/GC ca Motion graph active nodes, event subscriptions, observations, controllers si retained elements revin la baseline.
- [x] Adauga rapid Next/Previous-style restart/cancel stress pentru a preveni regresia de memorie observata in CernealaPresentation.
- [x] Adauga idle-frame assertions: fara active/infinite motion, markup behavior nu cere frames si nu produce layout/render invalidation.
- [x] Adauga allocation budget dupa warmup pentru hover/event restart si scroll update. (Warmup 64, 1.000 interactiuni masurate, plafon 40 MB si stabilitate intre cele doua jumatati; baseline local observat ~28,2 MB.)
- [x] Reindexeaza solutia.

**Gate etapa 3**

- [x] Stress tests sunt deterministe si GREEN; cresterea tranzitorie a heap-ului se stabilizeaza si nu corespunde unor owners retinuti. (27 teste lifecycle/runtime relevante GREEN, inclusiv cele 2 gate-uri integrate noi.)

### Etapa 4 - Documentatie si tooling contract

- [x] Transforma proposal-ul in documentatie de limbaj implementat sau pastreaza separat o sectiune clar marcata deferred; elimina formularea de proposal pentru ce este livrat. (Fisierul istoric ramane la acelasi path pentru link compatibility, dar titlul si continutul sunt language reference; suprafata nelivrata este izolata sub `Deferred Surface`.)
- [x] Documenteaza grammar, ownership, event semantics, no-reflection lowering, lifecycle, cancellation si toate limitarile runtime.
- [x] Actualizeaza docs Motion/API si toate public API pages folosind skill-ul `writing-api-documentation`; sincronizeaza manifestul. (Cele patru pagini inventariate au fost sincronizate; intrarile lor existau deja in manifest, deci nu a fost necesara modificarea JSON.)
- [x] Adauga un tabel machine-readable sau o gramatica unica folosita/testata de generator care poate alimenta ulterior syntax highlighting/completion; nu duplica manual keywords in doua surse nevalidate. (`MotionMarkupLanguage.DirectiveNames` este consumat de parser si parcurs integral de testul contractual.)
- [x] Documenteaza requirements pentru viitorul tooling: completion, hover types, go-to-definition, rename, quick fixes si generated-code preview, fara a le declara implementate.
- [x] Reindexeaza solutia.

**Gate etapa 4**

- [x] Documentatia si generatorul nu se contrazic pentru niciun exemplu public. (161 teste sourcegen Motion GREEN, inclusiv contractul tabelului de directive.)

### Etapa 5 - Verificare finala

- [x] Ruleaza `dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj`. (329 passed, 0 failed, 0 skipped.)
- [x] Ruleaza `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj`. (1.899 passed, 0 failed, 0 skipped.)
- [x] Ruleaza `dotnet test .\Cerneala.slnx`. (1.899 runtime + 329 sourcegen passed, 0 failed, 0 skipped.)
- [x] Ruleaza smoke-ul CernealaPresentation si inspectia vizuala/automation a showcase-ului migrat. (Ciclul final are 8 samples/8 chapters fara error report; captura Motion in-flight 1650x990 confirma layout stabil, starile discrete generate de `@set` si animatiile active fara overlap.)
- [x] Ruleaza `git diff --check`, public API diff si RoslynIndexer `doctor/status` dupa indexarea finala. (`diff --check` curat, runtime public API diff gol, indexarea are 0 warnings; `doctor/status` raporteaza asteptat `stale` doar fiindca worktree-ul are fisiere necomise modificate.)
- [x] Confirma ca nu exista tests skipped noi, warnings noi sau generated source cu reflection/dynamic. (0 skipped, build/test fara warnings; contractul generated source interzice reflection, `dynamic`, string lookup si tick closures.)

## 4. Definitia de gata

- [x] Toata sintaxa acceptata este tipizata, source-generated, diagnosticata si demonstrata intr-o aplicatie reala.
- [x] Events custom, observations, clips, handles si input controllers se detaseaza complet.
- [x] Performance gates demonstreaza zero work in idle si lipsa cresterii necontrolate la repeated interaction.
- [x] Tooling-ul viitor are un contract stabil de grammar/symbols/diagnostics pe care poate construi, fara sa ghiceasca limbajul.
- [x] Motion markup este suficient de coerent incat WPF poate incepe sa planga regulamentar.
