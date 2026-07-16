# Plan: composition, MotionClip si execution handles

> Data: 2026-07-15
> Status: finalizat
> Dependenta: `docs/plans/2026-07-15-motion-markup-foundation.md`
> Scop: adaugam composition explicita, retete reutilizabile si cancellation per Aspect fara sa inventam un runtime `MotionClip`.

## 1. Baseline si constrangere reala

Runtime-ul are `MotionGroup.Parallel(MotionHandle[])`, `MotionSequence.Start(Func<MotionHandle>[])` si `MotionGroupHandle`. Aceste API-uri nu pot reprezenta direct un arbore arbitrar in care group handles sunt copii ai altor groups. Generatorul trebuie sa aiba un adaptor de execution propriu, cu un contract unificat peste leaf si group handles, fara sa pretinda ca `MotionGroupHandle` are `Complete()` public sau cancellation modes.

## 2. Arhitectura propusa

- `MarkupMotionExecution` din bridge-ul `GeneratedMarkup` unifica `Completion`, `Cancel` si terminal state pentru codul generat, adaptand `MotionHandle` si `MotionGroupHandle`.
- `@parallel` si `@sequence` compun acest adaptor; nu schimba API-ul general Motion daca adaptorul generator-owned este suficient.
- `MotionClip` este compilat ca factory/recipe tipizata in generated code. Declaratia resource este imutabila si nu detine subscriptions sau handles.
- Fiecare `@run` creeaza o execution noua. `@handle` este un slot per sesiune de Aspect, nu un nume global.

## 3. Etape de implementare

### Etapa 0 - RED pentru composition

- [x] Adauga teste RED pentru `@parallel`, `@sequence` si nesting in ambele directii.
- [x] Adauga teste RED pentru completion ordering, cancel in mijlocul sequence si zero-step/one-step edge cases.
- [x] Adauga un test care demonstreaza ca runtime API-urile actuale nu pot fi fortate prin casts sau polling; foloseste adaptorul explicit.
- [x] Adauga teste de lifecycle: detach anuleaza copiii activi si nu porneste pasii viitori ai unei sequence.
- [x] Reindexeaza solutia.

**Gate etapa 0**

- [x] Modelul de execution are o singura semantica pentru leaf/group si nu expune operatii inexistente precum generic `Complete`.

### Etapa 1 - Execution tree

- [x] Extinde AST-ul cu un `execution-body` recursiv pentru `@animate`, `@parallel` si `@sequence`.
- [x] Impune cel putin un copil pentru groups si diagnostics precise pentru siblings plasati fara composition explicita.
- [x] Implementeaza adaptorul runtime cu cancel idempotent, completion exact o data si fara continuations care tin sesiunea vie dupa detach.
- [x] Emite parallel astfel incat completion asteapta toti copiii; emite sequence astfel incat urmatorul copil porneste numai dupa completare naturala.
- [x] Propaga cancellation fara a inventa selectable cancel behavior pentru `MotionGroupHandle`.
- [x] Reindexeaza solutia.

**Gate etapa 1**

- [x] Arbori nested functioneaza si se curata determinist.
- [x] Existing `MotionGroupTests` si toate testele Motion core raman GREEN.

### Etapa 2 - MotionClip resources

- [x] Parseaza `<MotionClip Name TargetType>` in resource scopes si cere exact un top-level execution body.
- [x] Respinge `@when`, `@on`, `@run` si al doilea body in interiorul clipului.
- [x] Rezolva target properties si `$part` la fiecare application/run site, cu assignability fata de `TargetType`.
- [x] Emite reteta ca factory fara runtime class `MotionClip`, fara subscriptions si fara stare partajata.
- [x] Implementeaza `@run $Clip` ca execution leaf numai in Aspect.
- [x] Adauga diagnostics pentru clip lipsa, wrong target, recursive invocation si direct assignment pe control.
- [x] Reindexeaza solutia.

**Gate etapa 2**

- [x] Doua instante si doua rulari simultane ale aceluiasi clip nu impart handles sau valori mutable.
- [x] Resource lookup este rezolvat la build, nu prin dictionary lookup pe fiecare run.

### Etapa 3 - Parametri tipizati

- [x] Parseaza `@parameter Name: Type = default` numai la inceputul unui MotionClip.
- [x] Limiteaza tipurile la valori/specs pe care resolverul le poate valida static; respinge duplicate, defaults incompatibile si parametri nefolosibili.
- [x] Emite parametri immutable per execution si valideaza named arguments, required arguments si duplicate arguments.
- [x] Permite parametri in values, specs, counts, ranges si options numai unde tipul rezultat ramane cunoscut.
- [x] Adauga teste pentru spec parameter XML-safe `MotionSpec[float]`, numeric parameter, default si diagnostics.
- [x] Reindexeaza solutia.

**Gate etapa 3**

- [x] Niciun parametru nu devine `object` sau dynamic in codul generat.

### Etapa 4 - Handles si cancellation

- [x] Parseaza `@handle Name`, `@run $Clip as Name` si `@cancel Name` numai in Aspect.
- [x] Creeaza sloturile per sesiune; un nou `@run ... as` anuleaza execution-ul anterior inainte de replacement.
- [x] Anuleaza toate sloturile la detach si elimina referintele la execution terminate.
- [x] Emite diagnostics pentru handle nedeclarat, duplicate, use-before-declaration si `@cancel` in MotionClip.
- [x] Adauga stress test cu restart/cancel repetat si verifica Motion graph + memorie stabilizata dupa GC.
- [x] Reindexeaza solutia.

**Gate etapa 4**

- [x] Handles nu ies din instanta Aspectului si nu exista generic `@complete`.

## 4. Verificare si definitia de gata

- [x] Ruleaza suitele sourcegen si runtime Motion targetate.
- [x] Inspecteaza generated code pentru un clip parametrizat nested si confirma factory nou per run.
- [x] Ruleaza `dotnet test .\Cerneala.slnx`, `git diff --check` si reindexarea finala.
- [x] Composition nested, MotionClip single-body, parameters si handles respecta exact grammar-ul proposal-ului.
- [x] API docs sunt actualizate pentru orice bridge public nou.
