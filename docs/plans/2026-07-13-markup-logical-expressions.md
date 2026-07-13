# Plan: expresii logice in `@when` si `@if`

Data: 2026-07-13

## Obiectiv

Extindem limbajul directivelor din fisierele `.cui.xml` cu operatorii lowercase
`and`, `or` si paranteze de grupare, fara sa transformam markup-ul intr-un al
doilea C#. Expresiile trebuie sa ramana tipizate, source-generated, reactive si
compatibile cu sintaxa existenta.

Exemple tinta:

```xml
@when IsMouseOver and IsEnabled
{
    Background = $HoverBrush;
}
```

```xml
@when $DataContext.Temperature
{
    @if value >= 80 and value < 100
    {
        Foreground = $WarningBrush;
    }
}
```

```xml
@when $DataContext.HasTarget and
      ($DataContext.HasLineOfSight or $DataContext.CanShootThroughWalls)
{
    <Button Content="Fire" />
}
```

## Contract de limbaj

- [x] Documenteaza operatorii acceptati: exclusiv `and`, `or` si `(` `)`; nu
      adauga `not`, `&&`, `||` sau expresii C# arbitrare in acest change.
- [x] Defineste precedenta ca `comparatie` > `and` > `or`; parantezele au
      prioritate explicita.
- [x] Trateaza `and` si `or` drept keywords numai la granite de token, astfel
      incat membri precum `IsAndroidReady` sa ramana identificatori normali.
- [x] Permite whitespace si newline intre tokenuri, dar nu interpreteaza
      keywords aflate in string literals.
- [x] Pastreaza sintaxa existenta cu un singur source complet compatibila.
- [x] Pentru un `@when` compus, cere ca fiecare frunza sa fie o sursa Boolean;
      un `@when` simplu poate observa in continuare orice tip folosit ulterior
      prin `@if value ...`.
- [x] Defineste `value` dintr-un `@if` aflat intr-un `@when` compus drept
      rezultatul Boolean al intregii expresii `@when`.
- [x] Permite in `@if` comparatii legate logic, inclusiv comparatii repetate cu
      `value` si comparatii cu alte surse reactive tipizate.
- [x] Pastreaza regulile existente pentru comparatori, `Null`, stringuri,
      enum-uri, numere si compatibilitatea tipurilor.
- [x] Evalueaza predicatele cu short-circuit, dar descopera si observa toate
      dependentele sintactice. O ramura neexecutata nu trebuie sa devina stale.
- [x] Aplica guard-ul unei cai de date nullable/incomplete la frunza care o
      foloseste, nu peste intreaga expresie; intr-un `or`, cealalta ramura
      trebuie sa poata deveni `true`.

## Etapa 1: teste RED pentru gramatica

- [x] Adauga teste in
      `tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorTests.cs` pentru un
      `@when A and B` si un `@when A or B`.
- [x] Adauga teste pentru `@if value >= min and value <= max`.
- [x] Adauga un test care demonstreaza precedenta `and` inainte de `or`.
- [x] Adauga un test care demonstreaza ca parantezele suprascriu precedenta.
- [x] Adauga teste pentru expresii multiline si whitespace variabil.
- [x] Adauga teste de compatibilitate pentru `@when Source`, shorthand Boolean
      si multiple blocuri `@if` existente.
- [x] Adauga diagnostice testate pentru operator lipsa, operand lipsa,
      paranteza lipsa, paranteza in plus si expresie goala.
- [x] Adauga diagnostice testate pentru o frunza non-Boolean intr-un `@when`
      compus si comparatii intre tipuri incompatibile.
- [x] Adauga un test care confirma ca `and`/`or` dintr-un string sau nume de
      membru nu sunt tokenizate drept operatori.

## Etapa 2: AST si parser

- [x] Inlocuieste reprezentarea plata `SourceExpression` / `Comparator` /
      `Operand` cu un AST intern minimal pentru expresii: source, `value`,
      literal, comparatie, `and`, `or` si grupare.
- [x] Implementeaza un lexer mic, determinist, care pastreaza sursa `XObject`
      si offsetul tokenului pentru diagnostice precise.
- [x] Implementeaza parserul prin recursive descent: `ParseOr`, `ParseAnd`,
      `ParsePrimary` si `ParseComparison`.
- [x] Refoloseste acelasi parser logic pentru headerele `@when` si `@if`, cu
      validare contextuala diferita in loc sa dublezi gramatica.
- [x] Pastreaza `ReadHeaderUntilBrace` responsabil doar pentru delimitarea
      headerului si stringurilor; muta interpretarea expresiei in parserul nou.
- [x] Verifica protectia comparatorilor `<` si `<=` efectuata in
      `UiMarkupGenerator` inainte de parsarea XML si adauga un caz cu paranteze.
- [x] Emite diagnostice care indica tokenul problematic, nu doar inceputul
      intregului bloc.

## Etapa 3: binding semantic si dependente

- [x] Rezolva fiecare source leaf prin aceleasi cai folosite astazi de
      `EmitObservation`: proprietatea elementului, `$DataContext`, `$owner`,
      `$self` si template parts.
- [x] Separa rezolvarea semantica de emiterea C#: AST-ul validat trebuie sa
      contina tipul si observatia asociata fiecarei frunze.
- [x] Deduplica sursele identice din aceeasi expresie, astfel incat
      `value >= 80 and value < 100` sa nu creeze doua observations pentru
      aceeasi proprietate.
- [x] Valideaza Boolean pentru frunzele unui `@when` compus.
- [x] Valideaza comparatorii si tipurile fiecarei comparatii din `@if` folosind
      regulile existente din `EmitComparison`.
- [x] Pastreaza toate observations in `ReactivePlan`, indiferent de ramurile
      care vor fi short-circuited la evaluare.
- [x] Confirma ca observations create in template sunt inregistrate in
      lifetime-ul `TemplateEmissionContext` existent.

## Etapa 4: emitere reactiva

- [x] Emite predicate C# complet parantezate cu `&&` si `||`, ca rezultatul sa
      nu depinda accidental de precedenta generatorului de stringuri.
- [x] Pastreaza short-circuit-ul C# la evaluare.
- [x] Compune predicatul expresiei cu `inheritedPredicate` folosit de Aspect si
      de blocurile `@when` nested.
- [x] Pastreaza ordinea si cascada existente pentru assignments conditionale.
- [x] Pastreaza lifecycle-ul existent pentru conditional content: creare la
      activare, detasare si disposal la dezactivare.
- [x] Nu introduce alocari per reevaluare; AST-ul si dependency discovery sunt
      exclusiv responsabilitatea source generatorului.
- [x] Nu modifica API-ul runtime public daca infrastructura curenta
      `MarkupObservation` + `MarkupConditionRule` poate reprezenta expresiile.

## Etapa 5: teste runtime si lifecycle

- [x] Demonstreaza ca modificarea oricarei dependente reevalueaza un `and`.
- [x] Demonstreaza ca modificarea oricarei dependente reevalueaza un `or`,
      inclusiv dependenta aflata initial intr-o ramura short-circuited.
- [x] Testeaza expresii care combina proprietati de element si cai
      `$DataContext`.
- [x] Testeaza `$owner` si `$self` intr-un template compus.
- [x] Testeaza o proprietate de template part intr-o expresie compusa.
- [x] Testeaza restore-ul valorii de baza cand expresia trece din `true` in
      `false`.
- [x] Testeaza conditional children la activare, dezactivare si reactivare.
- [x] Testeaza detasarea elementului/template-ului si confirma ca subscriptions
      nu mai primesc notificari.
- [x] Adauga un caz cu data path intermediar `null` intr-un `or` pentru a
      valida guard-urile per frunza.
- [x] Confirma ca doua aparitii ale aceleiasi surse produc o singura
      observation/subscription.

## Etapa 6: documentatie si exemplu real

- [x] Actualizeaza documentatia conceptuala pentru directivele `.cui.xml` cu
      gramatica, precedenta si exemple pentru `and`, `or` si paranteze.
- [x] Explica diferenta dintre short-circuit-ul evaluarii si observarea tuturor
      dependentelor.
- [x] Documenteaza explicit limitarile: fara `not`, fara C# arbitrar si fara
      frunze non-Boolean intr-un `@when` compus.
- [x] Actualizeaza exemplele stale de pe site care descriu sintaxa reactiva.
- [x] Adauga in Playground un exemplu mic, lizibil, care combina doua stari si
      afiseaza efectul fara sa transforme `MainWindow.cui.xml` intr-o ciorba.
- [x] Daca implementarea modifica vreun API public, actualizeaza in acelasi
      change paginile din `docs-site/documentation/classes/` si manifestul;
      altfel consemneaza explicit ca schimbarea este doar de limbaj/sourcegen.

## Etapa 7: verificare finala

- [x] Ruleaza testele targetate din `Cerneala.Tests.SourceGen` dupa fiecare
      etapa RED/GREEN.
- [x] Ruleaza full test suite si nu accepta niciun test failed sau skipped nou.
- [x] Porneste Playground-ul si verifica manual `and`, `or`, precedenta si un
      caz cu paranteze, urmarind si frame stats pentru regresii evidente.
- [x] Inspecteaza codul generat pentru un exemplu complex: observations
      deduplicate, predicate complet parantezate si lifetime registration.
- [x] Ruleaza un smoke test de attach/detach repetat pentru conditional content
      si verifica lipsa subscription leaks.
- [x] Reindexeaza solutia si confirma zero warnings in RoslynIndexer.
- [x] Bifeaza checklist-ul numai pe masura ce fiecare pas este demonstrat.

## Definitia de gata

- [x] Sintaxa veche compileaza si se comporta identic.
- [x] `and`, `or` si parantezele functioneaza in `@when` si `@if` conform
      precedentei documentate.
- [x] Toate dependentele sunt reactive chiar daca evaluarea face short-circuit.
- [x] Diagnosticele pentru expresii invalide sunt precise si actionabile.
- [x] Conditional properties, Aspect templates si conditional children isi
      pastreaza cascada si lifecycle-ul.
- [x] Playground-ul a fost testat manual, documentatia este actualizata si
      intreaga suita este GREEN.
