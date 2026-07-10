# Plan: template-uri de componenta declarate direct in markup

**Data:** 2026-07-10  
**Status:** Implementat si verificat  
**Scop:** Extinderea markup-ului Cerneala astfel incat orice element derivat din `Control` sa poata declara un `@template` local, iar un `Aspect` sa poata furniza acelasi tip modern de `ComponentTemplate`.

## Rezumat

Vrem sa permitem forma locala:

```xml
<Button Content="Close">
    @template
    {
        <Border Name="Bd"
                Background="$owner.Background"
                CornerRadius="6">
            @when $owner.IsMouseOver
            {
                Background = "#252B36";
            }

            <ContentPresenter Content="$owner.Content"
                              HorizontalAlignment="Center"
                              VerticalAlignment="Center"/>
        </Border>
    }
</Button>
```

si aceeasi capacitate intr-un Aspect:

```xml
<Aspect Name="TitleBarButton" Target="Button">
    @default
    {
        Width = 28;
        Height = 28;
        Background = "Transparent";
        Foreground = $InkDim;
    }

    @template
    {
        <Border Name="Bd"
                Background="$owner.Background"
                CornerRadius="6">
            @when $owner.IsMouseOver
            {
                Background = "#252B36";
            }

            <ContentPresenter Content="$owner.Content"
                              HorizontalAlignment="Center"
                              VerticalAlignment="Center"/>
        </Border>
    }
</Aspect>
```

Generatorul va transforma declaratia intr-un `ComponentTemplate<TControl>` modern. Nu introducem o a doua infrastructura de template si nu reinviem API-urile legacy.

## Decizii care trebuie confirmate la review

1. **API-ul Aspect ramane neschimbat: `Name` + `Target`.** Implementarea adauga `@template` peste contractul existent si nu redenumeste atribute ori membri runtime.
2. **`@template` este disponibil numai pe `Control`.** Un `StackPanel`, de exemplu, deriva din `UIElement`, nu din `Control`, deci primeste diagnostic.
3. **In interiorul template-ului, `$owner` este controlul templated, iar `$self` este elementul vizual curent.** O proprietate necalificata dintr-un `@when`, precum `IsMouseOver`, este prescurtare pentru `$owner.IsMouseOver` numai in acest context.
4. **Continutul nu este proiectat implicit.** Daca template-ul trebuie sa afiseze `Button.Content`, autorul scrie explicit un `ContentPresenter Content="$owner.Content"`.
5. **Template-urile nu se combina.** Castiga sursa cu precedenta cea mai mare; `@default` si conditiile Aspectului continua sa se aplice.
6. **Un `@template` direct pe radacina unui `UserControl` paired defineste corpul template-ului generat deja.** Nu asignam un al doilea `ComponentTemplate` peste cel folosit de generator pentru acel `UserControl`.

## Comportament semantic

### Eligibilitate

- `@template` poate aparea direct in continutul unui element al carui tip rezolvat deriva din `Cerneala.UI.Controls.Control`.
- Regula este determinata semantic prin Roslyn, nu printr-un catalog hardcodat de controale.
- Controalele custom sunt acceptate automat daca deriva din `Control`.
- Un element poate avea cel mult un `@template` direct.
- Un template contine exact un element radacina care deriva din `UIElement`.
- Textul brut, asignarile si mai multe elemente radacina direct sub `@template` sunt invalide.
- `@template` nu poate aparea direct intr-un `@when` sau `@if` in prima versiune. Schimbarea dinamica a template-ului ramane in afara scopului.
- Un Control aflat in interiorul altui template poate declara propriul sau `@template`; acest caz trebuie sa functioneze recursiv.

### Continutul controlului

- Directiva `@template` este consumata de compilator si nu devine `Content` sau copil vizual.
- Un `Button` poate avea simultan un `@template` si continut normal.
- Continutul normal continua sa fie atribuit proprietatii `Content` conform regulilor existente.
- Template-ul decide daca si unde proiecteaza acel continut printr-un `ContentPresenter` explicit.
- Nu introducem proiectare implicita, slot implicit sau un `ContentPresenter` generat pe ascuns.

### Domeniul expresiilor

- `$owner.Property` se refera la instanta de Control pentru care este aplicat template-ul.
- `$self.Property` se refera la elementul vizual curent in care apare directiva reactiva.
- In interiorul unui template, `@when IsMouseOver` este echivalent cu `@when $owner.IsMouseOver`.
- In afara unui template, expresiile necalificate isi pastreaza semantica actuala: elementul curent.
- `$ResourceName` isi pastreaza semantica de resource lexical.
- Numele `$owner` si `$self` devin rezervate si nu pot fi folosite ca identitati de resource sau element.
- Accesul arbitrar `$ElementName.Property` in interiorul template-ului nu intra in prima etapa; partile se acceseaza prin `ComponentTemplateInstance.Parts` la runtime.

### Template binding

Un atribut precum:

```xml
<Border Background="$owner.Background"/>
```

va fi emis ca un binding realizat prin `ComponentTemplateContext.Bind`, nu ca o valoare copiata o singura data:

```csharp
templateContext.Bind(
    Control.BackgroundProperty,
    border,
    Control.BackgroundProperty);
```

Generatorul trebuie sa valideze semantic:

- existenta proprietatii pe owner;
- existenta proprietatii tinta pe element;
- compatibilitatea tipurilor;
- posibilitatea de a scrie proprietatea tinta;
- folosirea unei proprietati reactive compatibile cu `Bind`.

In prima versiune acceptam binding direct proprietate-la-proprietate. Nu adaugam convertoare, expresii aritmetice, cai imbricate sau binding bidirectional.

### Conditii reactive in template

Forma booleana prescurtata devine valida:

```xml
@when $owner.IsMouseOver
{
    Background = "#252B36";
}
```

Ea este echivalenta semantic cu vechea forma explicita pentru cazul boolean adevarat. Parserul nu trebuie sa inventeze un `@if` fals in AST; emitter-ul valideaza ca sursa observata este booleana.

Forma existenta ramane valida pentru valori cu mai multe ramuri:

```xml
@when Status
{
    @if value == "Ready"
    {
        Foreground = "Green";
    }

    @if value == "Failed"
    {
        Foreground = "Red";
    }
}
```

Valorile conditionale au precedenta peste template binding cat timp conditia este activa. Cand conditia devine falsa, valoarea conditionala este eliminata si binding-ul catre `$owner.Background` redevine vizibil.

### Nume si parti de template

- `Name="Bd"` dintr-un template inline nu genereaza un camp pe code-behind-ul exterior.
- Fiecare astfel de nume este inregistrat prin `ComponentTemplateContext.RequirePart`.
- Partile sunt disponibile pe `ComponentTemplateInstance.Parts`.
- Spatiul de nume al partilor template-ului este separat de spatiul de nume al documentului exterior.
- Numele duplicate in acelasi template produc diagnostic.
- Acelasi template poate fi instantiat de mai multe ori fara ca partile unei instante sa se calce intre ele.

Exceptie intentionata: continutul generat pentru radacina unui `UserControl` paired pastreaza comportamentul actual pentru membrii numiti ai acelui UserControl. Controalele cu template inline imbricate in acel continut folosesc insa spatiul de parti izolat.

## Aspecte si precedenta

Sintaxa Aspect ramane cea existenta:

```xml
<Aspect Target="TextBlock">
    ...
</Aspect>

<Aspect Name="TitleBarButton" Target="Button">
    ...
</Aspect>
```

- `Target` descrie tipul tinta.
- `Name` identifica un Aspect referentiabil.
- Un Aspect fara `Name` ramane Aspectul implicit pentru tip.
- Referinta pe element ramane `Aspect="$TitleBarButton"`.
- `MarkupAspectResource.Name` si modelul intern existent raman neschimbate.
- Implementarea `@template` nu produce nicio schimbare incompatibila in API-ul Aspect.

Ordinea de precedenta pentru `ComponentTemplateProperty` este:

1. Aspect implicit pentru tip;
2. Aspect referentiat prin `Aspect="$Name"`;
3. Aspect local declarat prin `<Button.Aspect>`;
4. `@template` declarat direct pe Control.

O sursa cu precedenta mai mare inlocuieste complet template-ul anterior. Proprietatile `@default` si valorile `@when` din Aspect raman active; numai valoarea `ComponentTemplate` este suprascrisa.

Un template declarat intr-un Aspect este compilat o singura data in scope-ul resource-ului si este stocat ca valoare pentru `Control.ComponentTemplateProperty`. Pentru un Aspect inline, valoarea este aplicata prin `ElementAspect` la nivelul `LocalAspectBase`. Pentru un `@template` direct, generatorul foloseste setter-ul local normal.

## Arhitectura propusa

### 1. AST-ul directivelor

Extindem `UiMarkupDirectiveParser` cu:

```csharp
internal sealed record DirectiveTemplateNode(
    XElement Root,
    SourceLocation Source) : DirectiveNode;
```

`DirectiveWhenNode` trebuie sa poata reprezenta separat:

- ramuri explicite `@if`;
- un corp boolean direct.

Parserul primeste un context explicit de capabilitati, nu o succesiune de bool-uri greu de urmarit. De exemplu:

```csharp
[Flags]
internal enum DirectiveContentKind
{
    Elements = 1,
    Assignments = 2,
    Templates = 4
}
```

Regulile de nesting sunt validate in parser acolo unde sunt strict gramaticale. Eligibilitatea tipului si compatibilitatea proprietatilor raman in faza semantica.

### 2. Modelul semantic al template-ului

Introducem un model intern mic, separat de XML-ul brut:

```csharp
internal sealed record TemplateDeclaration(
    XElement Root,
    ITypeSymbol OwnerType,
    string GeneratedName,
    TemplateOrigin Origin,
    SourceLocation Source);
```

`TemplateOrigin` distinge doar cazurile necesare emiterii si precedentei:

- `AspectResource`;
- `InlineAspect`;
- `DirectElement`;
- `PairedUserControlRoot`.

Nu construim o ierarhie extensibila inutila si nu mutam logica runtime in generator.

### 3. Contextul de emitere

`GenerationScope` primeste un stack de contexte pentru template-uri. Contextul curent contine:

- variabila `ComponentTemplateContext<TControl>`;
- expresia owner-ului;
- simbolul tipului owner;
- namespace-ul partilor;
- indicatorul ca emiterea curenta este intr-un template;
- elementul vizual curent folosit de `$self`.

Este necesar un helper reutilizabil pentru schimbarea temporara a bufferelor `currentLines` si `currentPostLines`. Codul existent din emitter-ul conditional face deja aceasta manevra; il extragem intr-un scope sigur si il refolosim pentru template-uri, inclusiv template-uri imbricate.

### 4. Emiterea unui template direct

Pentru un Button normal, forma generata va fi echivalenta cu:

```csharp
button.ComponentTemplate = new ComponentTemplate<Button>(
    "Inline.Button.<locatie-determinista>",
    templateContext0 =>
    {
        Border border0 = new();
        templateContext0.RequirePart("Bd", border0);
        templateContext0.Bind(
            Control.BackgroundProperty,
            border0,
            Control.BackgroundProperty);
        return border0;
    });
```

Numele template-ului este determinist, bazat pe tip, origine si pozitia stabila in document. Nu folosim GUID-uri, ca generated source-ul si snapshot-urile sa ramana curate.

Lambda nu este fortata `static`: event handler-ele si resursele generate pot avea nevoie de instanta code-behind. Generatorul poate emite `static` numai cand demonstreaza ca nu exista capturi; aceasta optimizare nu este necesara in prima implementare.

### 5. Integrarea cu emitter-ul de proprietati

`EmitProperty` ramane responsabil pentru valorile literale existente. Adaugam o ramura template-aware pentru expresii `$owner.Property` si, unde este relevant, `$self.Property`.

Rezolvarea trebuie sa foloseasca infrastructura semantica existenta:

- `ResolveElementTypeSymbol`;
- `ResolvePropertyOwnerType`;
- `FindPropertySpec`;
- `IsOrDerivesFrom`.

Nu adaugam liste de tipuri sau proprietati cunoscute manual.

### 6. Integrarea cu emitter-ul reactiv

`UiMarkupReactiveEmitter` primeste rezolvarea explicita a sursei observate:

- `$owner` -> owner-ul template-ului;
- `$self` -> elementul curent;
- necalificat in template -> owner;
- necalificat in afara template-ului -> elementul curent;
- `$DataContext` si resursele existente -> comportamentul actual.

Abonamentele create pentru `@when` in template trebuie sa fie detinute de instanta template-ului si eliminate la inlocuirea sau detasarea acesteia. Nu acceptam o implementare care functioneaza vizual, dar lasa abonamente agatate dupa ea.

### 7. Paired UserControl si Window

Pentru o radacina paired `UserControl`:

- `@template` furnizeaza corpul lui `__CernealaGeneratedTemplate` deja creat de generator;
- nu se emite o asignare suplimentara la `ComponentTemplate`;
- `$owner` este instanta acelui UserControl;
- nu poate exista simultan si un copil vizual direct pe wrapper, deoarece ambele ar defini radacina template-ului;
- controalele imbricate se comporta normal.

Pentru o radacina `Window`:

- `Window` poate primi un `ComponentTemplate` local ca orice alt Control;
- copilul vizual direct continua sa fie `Window.Content`;
- cele doua pot coexista;
- template-ul trebuie sa proiecteze explicit continutul daca vrea sa-l afiseze.

## Fisiere vizate

### Generator

- `Cerneala.SourceGen/UiMarkupDirectiveParser.cs`
  - AST pentru `@template`;
  - corp boolean direct pentru `@when`;
  - reguli gramaticale si nesting.
- `Cerneala.SourceGen/UiMarkupGenerator.cs`
  - pastrarea contractului Aspect `Name`/`Target`;
  - model semantic de template;
  - validare `Control`;
  - emitere `ComponentTemplate<TControl>`;
  - template binding;
  - precedenta Aspect/direct;
  - parti numite si scope-uri imbricate.
- `Cerneala.SourceGen/UiMarkupReactiveEmitter.cs`
  - `$owner`, `$self` si prescurtarea booleana;
  - lifecycle corect pentru conditiile din template.
- `Cerneala.SourceGen/UiMarkupUserControlGenerator.cs`
  - semantica speciala pentru radacina paired.
- `Cerneala.SourceGen/UiMarkupWindowGenerator.cs`
  - template local pe Window si coexistenta cu Content.

### Runtime

- `Cerneala/UI/Markup/MarkupAspectResource.cs`
  - fara redenumiri de API;
  - ajustari numai daca sunt necesare pentru stocarea template-ului modern in Aspect.
- `Cerneala/UI/Styling/ElementAspect.cs`
  - numai ajustarile strict necesare pentru valoarea `ComponentTemplateProperty`; nu se introduce o noua abstractie de template.
- Infrastructura `ComponentTemplate*`
  - schimbari numai daca testele de lifecycle dovedesc ca abonamentele sau partile nu sunt curatate corect.

### Playground si documentatie

- `Playground/Cerneala.Playground/MainWindow.cui.xml`
  - pastrarea Aspectelor cu `Name`/`Target`;
  - un exemplu real de `Button` cu `@template`, hover si `ContentPresenter`.
- `docs/aspect-system.md`
  - noua sintaxa si precedenta.
- `docs/getting-started.md`
  - exemplu minimal de template direct.
- `docs/developer-preview-scope.md`
  - capabilitati si limitari explicite.
- Documentatia API pentru `MarkupAspectResource`, numai daca suportul template necesita clarificari; `Name` ramane neschimbat.

## Etape de implementare

### Etapa 1: teste RED pentru gramatica si contract

- [ ] Adauga teste pentru parsarea unui `@template` valid.
- [ ] Adauga teste pentru `@when` cu corp boolean direct.
- [ ] Adauga test pentru un `@when` din template cu mai multe ramuri `@if`.
- [ ] Adauga teste pentru zero, una si mai multe radacini.
- [ ] Adauga teste pentru template duplicat si template in conditional.
- [ ] Adauga test care demonstreaza ca `StackPanel` este respins semantic.
- [ ] Adauga test care demonstreaza ca un Control custom descoperit prin Roslyn este acceptat.
- [ ] Confirma ca testele esueaza din motivul asteptat inainte de codul de productie.

### Etapa 2: parser si AST

- [ ] Adauga `DirectiveTemplateNode`.
- [ ] Inlocuieste combinatia actuala de flag-uri booleene cu un context lizibil de continut permis.
- [ ] Extinde `DirectiveWhenNode` pentru corp boolean direct.
- [ ] Pastreaza forma cu `@if` compatibila.
- [ ] Emite erori gramaticale cu pozitia XML corecta.
- [ ] Ruleaza testele parserului si reindexeaza solutia.

### Etapa 3: conservarea contractului Aspect

- [ ] Pastreaza citirea atributelor `Name` si `Target` fara modificari de sintaxa.
- [ ] Pastreaza `MarkupAspectResource.Name` si modelele interne existente.
- [ ] Adauga teste de regresie pentru Aspect implicit si Aspect numit cu sintaxa actuala.
- [ ] Confirma ca adaugarea unui `@template` nu schimba rezolvarea `Aspect="$Name"`.
- [ ] Reindexeaza solutia dupa fiecare grup coerent de schimbari.

### Etapa 4: emiterea template-ului local minimal

- [ ] Detecteaza si extrage `@template` inaintea emiterii continutului normal.
- [ ] Valideaza semantic ca owner-ul deriva din `Control`.
- [ ] Introdu `TemplateDeclaration` si contextul de emitere.
- [ ] Extrage helper-ul sigur pentru buffer-ele de cod generate.
- [ ] Emite `ComponentTemplate<TControl>` cu nume determinist si o radacina `UIElement`.
- [ ] Permite Content normal langa directiva fara ca directiva sa devina Content.
- [ ] Sustine template-uri locale imbricate.
- [ ] Inspecteaza generated source-ul pentru cod stabil si lizibil.

### Etapa 5: `$owner`, `$self` si template bindings

- [ ] Rezerva identificatorii `$owner` si `$self`.
- [ ] Rezolva semantic proprietatile owner si target.
- [ ] Emite `ComponentTemplateContext.Bind` pentru atributele `$owner.Property`.
- [ ] Adauga suportul `$self.Property` acolo unde expresia este permisa.
- [ ] Raporteaza diagnostic pentru proprietate inexistenta, tip incompatibil sau tinta nescriptibila.
- [ ] Pastreaza lookup-ul lexical pentru `$ResourceName`.
- [ ] Nu adauga conversii implicite ori cai imbricate.

### Etapa 6: reactive state in template

- [ ] Extinde planul reactiv cu owner-ul si elementul curent.
- [ ] Implementeaza `@when $owner.BoolProperty`.
- [ ] Implementeaza shorthand-ul `@when BoolProperty` in template.
- [ ] Implementeaza `@when $self.BoolProperty`.
- [ ] Verifica revenirea de la valoarea conditionala la template binding.
- [ ] Verifica detasarea abonamentelor cand template-ul este inlocuit sau reaplicat.

### Etapa 7: template-uri in Aspect

- [ ] Permite un singur `@template` in corpul unui Aspect.
- [ ] Compileaza template-ul in scope-ul lexical al resource-ului.
- [ ] Stocheaza valoarea in `Control.ComponentTemplateProperty` prin mecanismul Aspect existent.
- [ ] Aplica ordinea de precedenta documentata.
- [ ] Confirma ca override-ul template-ului nu elimina `@default` sau `@when` din Aspect.
- [ ] Permite template si in `<Control.Aspect>` inline.

### Etapa 8: nume si parti

- [ ] Inregistreaza `Name` prin `RequirePart` pentru template-urile inline normale.
- [ ] Detecteaza duplicatele in acelasi template.
- [ ] Nu emite campuri code-behind exterioare pentru partile template-ului.
- [ ] Verifica izolarea partilor intre doua instante ale aceluiasi template.
- [ ] Pastreaza contractul actual pentru membrii paired UserControl.

### Etapa 9: radacini speciale

- [ ] Integreaza corpul `@template` al unui paired UserControl in template-ul generat existent.
- [ ] Raporteaza conflictul dintre acel corp si un copil vizual direct al wrapper-ului.
- [ ] Permite `@template` pe Window fara a elimina Content-ul normal.
- [ ] Verifica `$owner`, event handler-ele si resursele in ambele cazuri.

### Etapa 10: Playground si documentatie

- [ ] Pastreaza si verifica sintaxa Playground `Name`/`Target`.
- [ ] Adauga un Button demonstrativ cu template direct, continut si hover.
- [ ] Documenteaza sintaxa, scope-ul, precedenta si limitarile.
- [ ] Include exemple pentru template direct, Aspect implicit, Aspect cu Name si Aspect inline.
- [ ] Spune explicit ca `StackPanel` nu este templatable deoarece nu deriva din `Control`.
- [ ] Regenereaza `FileTree.md` daca structura documentatiei s-a schimbat.

## Matrice de teste

### Source generator

- [ ] Button cu `@template` emite `ComponentTemplate<Button>` si radacina corecta.
- [ ] Directiva nu este tratata drept Content.
- [ ] Template si atributul `Content` pot coexista.
- [ ] Template si copilul Content normal pot coexista.
- [ ] `$owner.Background` emite binding si urmareste schimbarea owner-ului.
- [ ] `$owner.Content` alimenteaza explicit `ContentPresenter`.
- [ ] `@when $owner.IsMouseOver` aplica si elimina valoarea conditionala.
- [ ] Un singur `@when` accepta mai multe ramuri `@if`, fiecare cu propriile asignari si elemente conditionale.
- [ ] `@when IsMouseOver` foloseste owner-ul in template.
- [ ] `@when $self.IsMouseOver` foloseste partea curenta.
- [ ] `$InkDim` continua sa rezolve resource-ul lexical.
- [ ] `Name="Bd"` devine part, nu camp exterior.
- [ ] Un Control imbricat poate avea propriul `@template`.
- [ ] Un Aspect implicit poate furniza template.
- [ ] Un Aspect cu `Name` poate furniza template.
- [ ] `<Button.Aspect>` poate furniza template.
- [ ] Template-ul direct castiga peste template-ul din Aspect.
- [ ] `@default` si `@when` din Aspect raman aplicate dupa override.
- [ ] Paired UserControl foloseste un singur template generat.
- [ ] Window pastreaza Content-ul normal cu template local.
- [ ] Generated code compileaza fara warnings.

### Diagnostice

- [ ] `@template` pe `StackPanel`.
- [ ] Doua `@template` pe acelasi Control.
- [ ] Template fara radacina.
- [ ] Template cu doua radacini.
- [ ] Text sau asignare direct la radacina template-ului.
- [ ] Template declarat intr-un conditional.
- [ ] `$owner.UnknownProperty`.
- [ ] `$self.UnknownProperty`.
- [ ] Binding cu tipuri incompatibile.
- [ ] Nume de parte duplicat.
- [ ] Conflict pe radacina paired UserControl.
- [ ] Un Aspect cu `Name`/`Target` continua sa compileze si sa se rezolve corect.

Erorile gramaticale pot continua sa foloseasca diagnosticul existent pentru directive invalide. Pentru contractul semantic de template introducem un diagnostic dedicat, de exemplu `CERNEALAUI012`, cu mesaj specific si locatie exacta. Nu inghesuim toate cazurile intr-un mesaj generic de tipul "template invalid".

### Runtime si lifecycle

- [ ] Schimbarea proprietatii owner actualizeaza partea legata.
- [ ] Inlocuirea template-ului detaseaza binding-urile vechii instante.
- [ ] Reaplicarea template-ului nu dubleaza abonamentele.
- [ ] Partile vechii instante nu raman accesibile dupa inlocuire.
- [ ] Doua controale cu acelasi template au dictionare de parti independente.
- [ ] Conditia reactiva revine la valoarea de binding dupa dezactivare.
- [ ] Sursa locala `ComponentTemplate` castiga peste sursele Aspect conform precedentei.

## Verificare finala

1. Ruleaza testele tintite ale parserului si generatorului.
2. Ruleaza testele runtime pentru `ComponentTemplate`, binding si lifecycle.
3. Ruleaza intreaga suita cu `dotnet test Cerneala.slnx --no-restore`.
4. Ruleaza build-ul complet fara warnings sau errors.
5. Ruleaza formatter-ul in mod de verificare.
6. Inspecteaza generated source pentru exemplele Playground.
7. Porneste Playground-ul si verifica manual:
   - continutul Button-ului;
   - hover-ul;
   - schimbarea proprietatilor owner;
   - deschiderea mai multor ferestre;
   - reaplicarea/inlocuirea template-ului.
8. Ruleaza `git diff --check`.
9. Regenereaza `FileTree.md`.
10. Reindexeaza `Cerneala.slnx` cu RoslynIndexer si confirma ca indexul este sanatos.

## Non-obiective pentru prima versiune

- Template-uri pe orice `UIElement` care nu deriva din `Control`.
- Template switching din `@when` sau `@if`.
- Combinarea structurala a doua template-uri.
- `ContentPresenter` sau slot generat implicit.
- Convertoare de binding.
- Binding bidirectional.
- Cai precum `$owner.User.Profile.Name`.
- Expresii arbitrare in atribute.
- Acces direct `$PartName.Property` intre partile template-ului.
- Triggers, animations sau visual states noi.
- O noua clasa runtime paralela cu `ComponentTemplate`.

## Riscuri si masuri

### Scope gresit pentru expresii

Cel mai mare risc semantic este ca o expresie necalificata sa observe accidental partea vizuala in locul controlului. Contextul de template trebuie sa faca aceasta alegere explicit, iar testele pentru `$owner`, `$self` si forma necalificata trebuie sa existe separat.

### Abonamente reactive ramase dupa template

Un template se poate reinstanta de multe ori. Toate binding-urile si conditiile create de factory trebuie detinute si eliminate impreuna cu `ComponentTemplateInstance`. Testele de inlocuire si reaplicare sunt blocante pentru merge.

### Nume transformate in campuri globale

Daca numele din template folosesc mecanismul code-behind obisnuit, doua instante se vor suprascrie. Template-urile normale trebuie sa foloseasca exclusiv `Parts`; exceptia paired UserControl ramane limitata si testata explicit.

### Regresii in Content

Directiva trebuie eliminata din fluxul de copii inainte de regulile existente pentru Button, Border, Panel si Window. Testele trebuie sa acopere atat Content atribut, cat si copil vizual direct.

### Regresii in contractul Aspect

`@template` trebuie adaugat fara sa schimbe `Name`, `Target`, `Aspect="$Name"` sau metadatele runtime existente. Testele de regresie pentru Aspect sunt blocante, fiindca aici nu facem renovare cu barosul intr-un API care deja functioneaza.

## Criterii de acceptare

Implementarea este gata numai cand:

- sintaxa directa si cea din Aspect produc acelasi `ComponentTemplate` modern;
- orice Control custom descoperit semantic poate folosi `@template`;
- un non-Control primeste diagnostic clar;
- `$owner`, `$self`, resource-urile si conditiile au scope determinist;
- Content-ul este pastrat si proiectat numai explicit;
- precedenta Aspect/direct este dovedita prin teste;
- partile sunt izolate per instanta;
- inlocuirea template-ului nu lasa binding-uri sau abonamente vechi;
- Playground-ul demonstreaza fluxul end-to-end;
- documentatia si toate exemplele pastreaza contractul `Name`/`Target`;
- build-ul si intreaga suita de teste sunt curate.

## Dovezi de implementare

- `dotnet build Cerneala.slnx --no-restore`: 0 warnings, 0 errors.
- `dotnet test Cerneala.slnx --no-restore`: 1570 teste runtime/documentatie si 92 teste source-generator, toate verzi.
- `dotnet format Cerneala.slnx --no-restore --verify-no-changes`: curat.
- `git diff --check`: curat.
- Generated source-ul Playground contine `ComponentTemplate<Button>`, `RequirePart`, owner bindings si `RegisterLifetime` pentru conditiile reactive.
- Playground-ul compilat porneste, creeaza fereastra nativa `Cerneala generator playground` si ramane responsiv.
- RoslynIndexer raporteaza index valid, fara fisiere dirty sau warnings.
