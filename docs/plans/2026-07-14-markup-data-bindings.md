# Plan: binding-uri declarative in markup si surse reactive comune

> Data: 2026-07-14
> Status: finalizat
> Dependenta: `docs/plans/2026-07-13-markup-logical-expressions.md` (implementat)
> Scop: adaugam binding-uri source-generated `OneWay` si `TwoWay` in atributele si assignment-urile conditionale `.cui.xml` si formalizam sursele din `@when` ca binding-uri read-only compuse, fara reflection paths sau un al doilea motor reactiv.

## 1. Rezumat

Markup-ul trebuie sa poata lega o proprietate UI de o cale tipizata:

```xml
<TextBlock Text="$DataContext.Name" />
<TextBlock Text="$DataContext.Name:OneWay" />
<TextBlock Text="$DataContext.Type.Name:OneWay" />
<TextBlock Text="$DataContext.Count" />
<TextBox Text="$DataContext.Name:TwoWay" />
```

Cand tinta este `string`, un binding `OneWay` accepta automat orice sursa si
proiecteaza valoarea prin conversia standard la text. De exemplu, un `int`
din `Count` devine text folosind cultura curenta; un `null` devine string gol.
Un literal string poate incorpora una sau mai multe cai reactive:

```xml
<TextBlock Text="Salut, $DataContext.Name" />
<TextBlock Text="Comenzi: $DataContext.Count, utilizator: $DataContext.Name" />
<TextBlock Text="Literal: \$DataContext.Name" />
```

Interpolarea este intotdeauna `OneWay`: toate caile sunt observate, valorile
sunt convertite automat la text, iar schimbarea oricarei surse recompune
string-ul complet. `:OneWay` si `:TwoWay` sunt interzise in interiorul unei
interpolari. Secventa `\$` afiseaza un `$` literal si nu porneste nici binding,
nici interpolare.

Caile catre template parts pastreaza gramatica existenta si trebuie sa se
termine obligatoriu intr-o proprietate:

```xml
<TextBlock
    Text="$MyScrollViewer.parts.$PART_VerticalScrollBar.Name:OneWay" />
```

Proprietatile elementelor denumite pot fi surse directe, fara traversarea unui
template part. `OneWay` este modul implicit atunci cand sufixul lipseste:

```xml
<Slider Name="VolumeSlider" Value="40" />
<ProgressBar Value="$VolumeSlider.Value" />
<ProgressBar Value="$VolumeSlider.Value:OneWay" />
<ProgressBar Value="$VolumeSlider.Value:TwoWay" />
```

`OneWay|TwoWay` sau `OneWay/TwoWay` sunt doar notatii scurte in documentatie
pentru cele doua alternative. In markup se scrie exact un singur mod;
`:OneWay/TwoWay` nu este o valoare legala.

Sursele din `@when` nu accepta moduri. Ele sunt binding-uri read-only, iar
operatorii si parantezele construiesc un binding Boolean derivat:

```xml
@when ($DataContext.IsEnabled and $DataContext.User.IsAdmin)
    or $DataContext.IsDebug
{
    Background = "Green";
}
```

Toate frunzele expresiei raman observate. Evaluarea respecta short-circuit-ul,
dar abonamentele nu sunt create si distruse dupa ramura activa.

Assignment-urile dintr-o ramura conditionala pot selecta si un binding, nu
doar o valoare statica. Binding-urile integrale sunt expresii unquoted;
string-urile quoted fara cai raman literale, iar cele cu text literal plus cai
devin interpolari:

```xml
@when $DataContext.UseShortName
{
    Text = $DataContext.ShortName;
}

@when $DataContext.UseLongName
{
    Text = $DataContext.LongName:TwoWay;
}
```

`Text = "MyText";` este un literal legal. O expresie care arata ca binding, dar
este pusa intre ghilimele, de exemplu
`Text = "$DataContext.ShortName:OneWay";`, este ilegala si primeste diagnostic
in loc sa fie tratata silentios ca text.

Un string quoted care are si continut literal este interpolare legala:

```text
Text = "Salut, $DataContext.ShortName";
```

Ghilimelele XML raman obligatorii pentru valorile atributelor si sunt doar
delimitatori XML. Regula unquoted se aplica expresiilor din partea dreapta a
assignment-urilor din directive.

## 2. Decizii stabilite

- Toate caile de binding folosesc `OneWay` implicit cand sufixul lipseste.
  `:OneWay` ramane forma explicita echivalenta, iar `:TwoWay` trebuie cerut
  explicit.
- Expresiile conditie din `@when` si `@if` nu accepta `:OneWay`, `:TwoWay` sau
  alt mod; sursele lor sunt intotdeauna read-only/one-way. Aceasta regula nu
  interzice modul din valoarea unui assignment aflat in corpul ramurii.
- `$owner.Property` ramane binding-ul implicit one-way existent in interiorul
  unui `@template`; `$owner.Property:OneWay` este aliasul explicit legal, iar
  `:TwoWay` ramane in afara acestui plan si primeste diagnostic.
- `$control.parts.$part.Property` ramane forma canonica pentru template parts:
  `parts` este lowercase, numele sunt case-sensitive, exista un singur nivel
  de template si proprietatea terminala este obligatorie.
- `$element.Property[:Mode]` leaga direct o proprietate a unui element denumit
  vizibil in scope; elementul poate fi declarat inainte sau dupa tinta, iar
  generatorul amana atasarea pana dupa construirea elementelor denumite.
- `$self.Property[:Mode]` este permis numai cand proprietatea sursa este
  diferita de proprietatea tinta. Un self-binding direct, precum
  `IsEnabled="$self.IsEnabled"`, este respins cu diagnostic dedicat.
- Assignment-urile din `@when` / `@if` accepta binding-uri numai ca expresii
  unquoted. Un string quoted obisnuit ramane literal, iar un string quoted care
  are forma unei cai de binding este respins cu diagnostic. Modul apartine
  valorii assignment-ului, nu expresiei Boolean care controleaza ramura.
- Un binding conditional este activ numai cat timp assignment-ul sau este
  castigator pentru proprietatea tinta. La dezactivare se opreste, iar la o
  activare ulterioara reciteste imediat sursa curenta.
- `and`, `or` si parantezele nu produc abonamente dinamice. Toate sursele
  sintactice sunt abonate, iar predicatul generat pastreaza short-circuit-ul C#.
- Tipurile sursei si tintei trebuie sa fie compatibile la compilare, cu o
  singura conversie incorporata: pentru un binding `OneWay` catre `string`,
  orice valoare sursa este transformata cu semantica
  `Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty`.
- Conversia automata la `string` este numai source-to-target. Un binding
  `TwoWay` catre `string` cere sursa `string`; nu inventam parsing invers pentru
  numere, enum-uri, date sau obiecte arbitrare.
- O valoare string cu text literal si una sau mai multe cai incorporate este un
  binding interpolat derivat, intotdeauna `OneWay`. Toate caile sunt observate,
  fiecare valoare foloseste aceeasi conversie la text, iar un segment
  nerezolvat sau terminal `null` contribuie cu `string.Empty` pana la refresh.
- Modurile `:OneWay` si `:TwoWay` sunt ilegale in interiorul interpolarii.
  Modul se poate pune numai pe o valoare care este integral o singura cale de
  binding.
- `\$` este escape-ul canonic pentru un `$` literal in atribute si in
  string-urile quoted din directive. Scannerul consuma backslash-ul, emite `$`
  si nu incearca sa rezolve secventa urmatoare ca resursa, binding sau
  interpolare. Celelalte secvente cu backslash isi pastreaza contractul
  existent.
- O cale `$DataContext` necesita `DataType` pe radacina, conform contractului
  reactiv existent.
- O cale incompleta din cauza unui segment intermediar `null` este
  indisponibila temporar; binding-ul elimina valoarea sa de markup si se
  reconecteaza cand segmentul devine disponibil. Un `null` returnat de
  proprietatea terminala ramane o valoare valida pentru o tinta nullable.
- Binding-ul de atribut ocupa `UiPropertyValueSource.MarkupBase`, astfel incat
  `MarkupConditional` sa-l poata suprascrie si apoi restaura. Binding-ul dintr-un
  assignment castigator ocupa `MarkupConditional` numai cat timp ramura sa este
  activa.
- Pentru `TwoWay`, doar o schimbare relevanta a tintei din sursa `Local` este
  impinsa inapoi. Valorile conditionale, animatiile si restaurarile de cascada
  nu trebuie sa murdareasca ViewModel-ul.
- Binding-urile si interpolarile trebuie sa reactioneze la schimbarile sursei;
  nu acceptam degradarea silentioasa la o simpla citire initiala. Sursele UI se
  observa prin `UiObject.PropertyChanged`, iar fiecare owner CLR dintr-o cale
  `$DataContext` trebuie sa implementeze `INotifyPropertyChanged`; altfel
  generatorul emite diagnostic actionabil.
- Contractul de threading al acestui plan este strict: binding-ul se activeaza
  pe thread-ul `Update`/UI, iar orice `PropertyChanged` consumat de el trebuie
  ridicat pe acelasi thread. O notificare off-thread este detectata inainte de
  citirea sau scrierea tintei si produce o eroare fail-fast clara, cu informatii
  despre binding si thread-uri; nu este ignorata, nu este executata off-thread
  si nu este pusa automat intr-o coada. Auto-marshaling-ul apartine unui plan de
  infrastructura separat: `docs/plans/2026-07-14-relay-auto-marshaling.md`.

## 3. Baseline si problema actuala

- `BindingOperations` si `UiPropertyBinding<T>` leaga astazi numai un
  `ObservableValue<T>` de un `UiProperty<T>`; generatorul de markup nu le
  foloseste.
- `StringPropertyPath` este intentionat dezactivat. Caile noi trebuie validate
  de Roslyn si emise ca accesori tipizati, nu evaluate prin reflection.
- `UiMarkupGenerator.EmitProperty` trateaza `$owner.Property` drept
  `TemplateBinding` doar in `@template`; celelalte valori `$Name` din atribute
  sunt referinte la resurse.
- `UiMarkupReactiveEmitter` poate observa deja proprietati UI,
  `$DataContext.Path`, `$owner.Property`, `$self.Property` si
  `$control.parts.$part.Property`.
- `GeneratedMarkup`, `MarkupObservation` si `MarkupDataPathSegment` contin deja
  reconectarea pentru `DataContext`, `INotifyPropertyChanged`, `UiObject` si
  inlocuirea template-ului.
- `ReactivePlan` deduplica observatiile si pastreaza toate dependentele unei
  expresii cu `and`/`or`, inclusiv ramurile short-circuited.
- `UIElement.Bindings` se goleste la detach, in timp ce conditiile generate
  folosesc `IElementLifecycleBehavior` pentru stop/restart. Noul binding de
  markup trebuie sa aiba acelasi contract de attach/detach ca markup-ul
  reactiv, nu sa piarda definitiv abonamentele la primul detach.

Problema de arhitectura este ca sursele reactive sunt rezolvate numai pentru
directive, iar binding-urile runtime accepta numai `ObservableValue<T>`. Daca
adaugam inca un parser si inca o retea de subscriptions direct in
`EmitProperty`, obtinem doua motoare care se cearta ca dracu pe aceleasi
proprietati.

## 4. Obiective

- Binding-uri de atribut complet source-generated pentru cai simple si nested
  din `$DataContext`.
- Binding-uri de atribut catre proprietati UI ale elementelor denumite si catre
  o alta proprietate a lui `$self`.
- Binding-uri de atribut catre proprietati de template part folosind gramatica
  existenta.
- Binding-uri `OneWay` si `TwoWay` ca valori ale assignment-urilor
  conditionale, exprimate unquoted si activate per ramura castigatoare.
- Conversie automata source-to-string pentru binding-uri `OneWay` care au o
  proprietate tinta `string`, inclusiv in assignment-uri conditionale.
- Interpolari string reactive cu una sau mai multe cai, conversie automata a
  fiecarei valori si recompozitie la schimbarea oricarei surse.
- Moduri `OneWay` si `TwoWay`, cu initializare imediata, reentrancy guard,
  reconectare si cleanup determinist.
- O singura rezolvare semantica pentru sursele folosite de atribute si de
  `@when`.
- Pastrarea cascadei `MarkupBase` / `MarkupConditional` si a contractului de
  template lifecycle.
- Diagnostice source-generator precise pentru sintaxa, tipuri, accesibilitate
  si moduri invalide.

## 5. Non-obiective

- Sintaxa WPF `{Binding ...}`.
- Activarea `StringPropertyPath` sau evaluarea prin reflection.
- Convertere, `FallbackValue`, `TargetNullValue`, validare declarativa ori
  `UpdateSourceTrigger`.
- `OneTime`, `OneWayToSource`, multi-binding sau expresii C# arbitrare.
- Auto-binding-ul unei proprietati la ea insasi prin `$self`; generatorul il
  respinge in loc sa construiasca o bucla fara sens.
- Binding la obiectul brut `$control.parts.$part`; calea trebuie sa se termine
  intr-o proprietate.
- Binding la obiectul brut `$element`; sursa directa denumita trebuie sa se
  termine intr-o proprietate UI.
- Navigare recursiva prin mai multe template-uri, de exemplu
  `ScrollViewer -> ScrollBar -> Track -> Thumb` intr-o singura cale.
- Schimbarea comportamentului public al `BindingOperations` pentru
  `ObservableValue<T>`.
- Extinderea `$owner.Property` la `TwoWay` in acest plan; forma fara mod si
  `:OneWay` raman `TemplateBinding` one-way.
- Moduri de binding in interiorul interpolarilor, de exemplu
  `Text="Salut, $DataContext.Name:OneWay"`; interpolarea este deja un binding
  derivat `OneWay`, deci modurile pe fragmente sunt respinse.

## 6. Arhitectura propusa

### 6.1 Rezolvare semantica unica in source generator

Se extrage din `UiMarkupReactiveEmitter` un descriptor intern minimal al unei
surse tipizate, reutilizat de directive si binding-urile de atribut. Descriptorul
trebuie sa contina:

- tipul valorii terminale;
- codul pentru construirea unui `MarkupObservation`;
- disponibilitatea unui setter terminal pentru `TwoWay`;
- proiectia source-to-target necesara, limitata in acest plan la identitate sau
  conversia incorporata la `string`;
- locatia si forma canonica a expresiei pentru diagnostice;
- informatia de scope pentru `$DataContext`, elemente denumite, `$owner`,
  `$self` si template parts;
- identitatea proprietatii UI sursa si tinta, pentru detectarea self-binding-ului
  direct inainte de emiterea C#.

Elementele denumite folosesc acelasi name scope ca directivele reactive. O
referinta forward este valida in acelasi scope, dar o referinta care incearca sa
iasa dintr-un template name scope primeste diagnostic. Generatorul construieste
mai intai elementele denumite si ataseaza binding-urile dependente dupa ce toate
referintele din scope sunt disponibile.

Nu se expune AST-ul generatorului ca API public si nu se muta parsing-ul
`.cui.xml` in runtime.

### 6.2 Endpoint runtime reutilizabil

`MarkupObservation` ramane endpoint-ul comun de citire si notificare. El va
distinge intern intre:

- valoare rezolvata, inclusiv valoare terminala `null`;
- cale temporar nerezolvata din cauza unui owner sau segment intermediar lipsa;
- endpoint writable sau read-only.

`MarkupDataPathSegment` primeste un setter tipizat emis de generator numai
pentru segmentul terminal writable. Observatiile de `UiProperty` si template
part pot scrie prin `SetValue` cand proprietatea nu este read-only. Aceeasi
observatie de `UiProperty` acopera `$element.Property` si
`$self.OtherProperty`; nu se introduce un endpoint runtime separat doar pentru
ca sursa are alta ortografie in markup.

`DataPathObservation` continua sa se aboneze la `UiObject.PropertyChanged` si
`INotifyPropertyChanged` pentru fiecare owner din cale si reconstruieste toate
segmentele descendente cand unul se schimba. Resolverul semantic refuza un
owner CLR fara contract de notificare, astfel incat un binding declarat reactiv
nu poate functiona doar la initializare fara sa spuna nimic.

### 6.3 Controller pentru binding-ul unei proprietati

`GeneratedMarkup` primeste o fabrica publica generica pentru atasarea unui
binding intre un `MarkupObservation` si un `UiProperty<T>`. Implementarea
interna, estimata ca `MarkupPropertyBindingController<T>`, va:

- implementa contractul `Binding`/`IDisposable` si
  `IElementLifecycleBehavior`;
- porni observatia si scrie valoarea initiala in slotul configurat;
- actualiza tinta la fiecare schimbare `OneWay`;
- pentru `TwoWay`, observa tinta, scrie prin setter-ul terminal si blocheaza
  buclele recursive;
- ignora propagarea target-to-source pentru schimbari din alte value sources;
- opri subscriptions la detach si le reconstrui la reattach;
- elimina numai contributia slotului pe care il detine la disposal definitiv;
- putea fi inregistrat prin `TemplateEmissionContext.RegisterLifetime` cand
  binding-ul este creat intr-un template.

Controllerul accepta intern slotul tinta (`MarkupBase` sau `MarkupConditional`)
si un contract de activare. Pentru binding-ul de atribut este activ pe intreaga
durata de viata a elementului. Pentru un binding aflat in assignment-ul unei
ramuri, `MarkupConditionController` il activeaza numai cat timp assignment-ul
castiga proprietatea tinta.

Controllerul aplica proiectia source-to-target inainte de scrierea slotului.
Pentru tinta `string`, generatorul emite conversia standard cu
`CultureInfo.CurrentCulture`; `null` produce `string.Empty`. Proiectia nu este
un `IValueConverter` configurabil si nu are cale inversa. De aceea un
`TwoWay` cu sursa non-string si tinta `string` este respins la compilare.

Nu se forteaza `UiPropertyBinding<T>` sa accepte reflection paths. Acesta
ramane binding-ul direct pentru `ObservableValue<T>`, iar controllerul de
markup refoloseste `BindingMode`, regulile de writable target si conventia de
disposal.

### 6.4 Binding Boolean derivat pentru `@when`

`@when` continua sa foloseasca `ReactivePlan`, `MarkupObservation` si
`MarkupConditionRule`; nu cream un `UiPropertyBinding<T>` inutil pentru fiecare
frunza. Contractul documentat devine:

```text
source bindings -> expresie tipizata derivata -> bool -> regula conditionala
```

Frunzele identice sunt deduplicate. Parantezele definesc AST-ul, `and` si `or`
se emit ca `&&` si `||`, iar toate observatiile raman active indiferent de
short-circuit.

### 6.5 Valori de binding in assignment-uri conditionale

`MarkupConditionController` ramane unicul arbitru al slotului
`MarkupConditional`; controller-ele de binding nu scriu concurent in acelasi
slot. Modelul de assignment conditional va distinge intre:

- valoare statica deja suportata;
- fabrica de valoare reactiva care poate fi activata si dezactivata.

Cand o regula devine castigatoare pentru o proprietate, controllerul:

1. dezactiveaza furnizorul anterior pentru acea proprietate;
2. activeaza binding-ul noii ramuri si citeste imediat sursa curenta;
3. publica actualizarile numai cat timp token-ul de activare este curent;
4. pentru `TwoWay`, scrie inapoi numai schimbarile `Local` produse cat timp
   assignment-ul este activ;
5. la pierderea ramurii, opreste observatia si elimina contributia
   `MarkupConditional`, lasand sa reapara binding-ul sau valoarea `MarkupBase`.

Daca sursa binding-ului conditional devine temporar nerezolvata, contributia
`MarkupConditional` este eliminata pana la reconectare, iar `MarkupBase` devine
din nou vizibil fara a schimba ramura Boolean castigatoare.

Observatiile expresiei Boolean din `@when` raman permanent active conform
contractului existent. Numai observatia valorii de binding din assignment poate
fi oprita cat timp ramura nu castiga; la reactivare face refresh complet, deci
nu poate afisa o valoare veche.

Formele legale sunt expresii unquoted; lipsa modului inseamna `OneWay`:

```text
Text = $DataContext.ShortName;
Text = $DataContext.ShortName:OneWay;
Text = $DataContext.ShortName:TwoWay;
```

Forma unquoted este tokenizata numai in gramatica assignment-urilor si trebuie
sa consume intreaga valoare pana la `;`. Nu relaxam regulile XML pentru
atribute. `Text = "MyText";` ramane literal legal, iar
`Text = "Salut, $DataContext.ShortName";` este interpolare. Un string quoted
format exclusiv dintr-o cale, precum `Text = "$DataContext.ShortName";`, sau
care pune un mod intr-un fragment, precum
`Text = "$DataContext.ShortName:OneWay";`, primeste diagnostic.

### 6.6 Gramatica binding-urilor de atribut si assignment

Forma generala, cu mod optional:

```text
<source-path>[:<mode>]
```

Surse acceptate in acest plan:

```text
$DataContext.Property
$DataContext.Property:OneWay
$DataContext.Parent.Child.Property:TwoWay
$owner.Property
$owner.Property:OneWay
$element.Property
$element.Property:OneWay
$element.Property:TwoWay
$self.OtherProperty
$self.OtherProperty:OneWay
$self.OtherProperty:TwoWay
$control.parts.$part.Property
$control.parts.$part.Property:OneWay
$control.parts.$part.Property:TwoWay
```

`$element.Property` cere o proprietate UI terminala accesibila. `$self` se
rezolva la elementul pe care se afla proprietatea tinta si este legal numai
daca proprietatea terminala difera de tinta. Comparatia se face semantic dupa
simbolul/identitatea `UiProperty`, nu dupa text, astfel incat o proprietate
mostenita nu poate ocoli diagnosticul prin alta calificare.

Parserul separa sufixul numai daca ultimul segment dupa `:` este exact
`OneWay` sau `TwoWay`; daca sufixul lipseste, descriptorul foloseste
`BindingMode.OneWay`. `$Accent` continua sa fie resursa, iar o valoare care
incepe cu `$` dar nu respecta o forma de binding ramane pe traseul existent de
rezolvare a resurselor sau primeste diagnosticul existent. In assignment-uri,
parserul citeste expresia unquoted completa si respinge explicit o cale de
binding scrisa ca string quoted. Pentru radacina speciala `$owner`, modul
implicit si `:OneWay` sunt legale, iar `:TwoWay` este diagnosticat explicit.

Dupa rezolvarea caii, verificarea de tip accepta asignarea directa sau, numai
pentru `OneWay` cu tinta `string`, proiectia incorporata la text. Alte
incompatibilitati raman diagnostice; in particular, `string` nu este folosit ca
pod magic pentru `TwoWay`.

### 6.7 Interpolari string reactive

Pentru o proprietate tinta `string`, o valoare cu text literal si cel putin o
cale incorporata produce un descriptor de interpolare. Scannerul recunoaste
aceleasi radacini si aceleasi name scopes ca binding-urile integrale, consuma
cea mai lunga cale tipizata valida si opreste calea inaintea caracterelor
literale precum spatiu, virgula sau semn de punctuatie.

```text
"Salut, $DataContext.Name"
"Comenzi: $DataContext.Count, utilizator: $DataContext.Name"
"Valoare: $VolumeSlider.Value"
"Literal: \$DataContext.Name"
```

Reguli:

- o cale interpolata trebuie sa se termine intr-o proprietate;
- `$Accent` fara proprietate terminala nu devine interpolare si isi pastreaza
  contractul de resursa/literal existent;
- un `$` care nu incepe o cale valida ramane caracter literal;
- perechea exacta `\$` este consumata inainte de recunoasterea cailor, produce
  un singur `$` literal si nu creeaza nicio observatie; regula se aplica identic
  atributelor si string-urilor quoted din directive, fara sa schimbe celelalte
  escape-uri existente;
- `:OneWay`, `:TwoWay` si orice alt sufix de mod dintr-un fragment sunt
  diagnostice;
- fragmentele identice sunt deduplicate semantic, dar apar in toate pozitiile
  din rezultatul final;
- toate observatiile interpolarii active raman abonate si orice schimbare
  recompune intregul string;
- un fragment `null` sau temporar nerezolvat produce `string.Empty` pana cand
  sursa redevine disponibila;
- o interpolare de atribut scrie in `MarkupBase`; una dintr-un assignment
  conditional foloseste acelasi contract activate/deactivate ca binding-ul
  conditional si scrie in `MarkupConditional` numai cat timp ramura castiga.

In assignment-uri, un string quoted format exclusiv dintr-o singura cale ramane
eroare de autor: trebuie scris ca binding unquoted. Prezenta unui fragment
literal real transforma string-ul quoted intr-o interpolare legala. In
atributele XML, ghilimelele sunt delimitatori obligatorii, deci valoarea exacta
`$DataContext.Name` ramane binding integral, nu interpolare.

## 7. Fisiere estimate

Fisiere modificate:

- `Cerneala.SourceGen/UiMarkupGenerator.cs`
- `Cerneala.SourceGen/UiMarkupReactiveEmitter.cs`
- `Cerneala.SourceGen/UiMarkupDirectiveParser.cs`, pentru expresiile de binding
  unquoted si diagnosticul binding-urilor scrise ca string quoted
- `UI/Markup/GeneratedMarkupConditions.cs`
- `tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorTests.cs`
- `docs/getting-started.md` sau pagina conceptuala curenta pentru `.cui.xml`
- `docs-site/documentation/classes/Cerneala.UI.Markup.GeneratedMarkup.md`
- `docs-site/documentation/classes/Cerneala.UI.Markup.MarkupObservation.md`
- `docs-site/documentation/classes/Cerneala.UI.Markup.MarkupDataPathSegment.md`

Fisiere noi posibile, daca separarea reduce clar complexitatea fisierelor
existente:

- `Cerneala.SourceGen/UiMarkupBindingEmitter.cs`
- `UI/Markup/GeneratedMarkupBindings.cs`
- `tests/Cerneala.Tests/UI/Markup/GeneratedMarkupBindingTests.cs`

Nu se adauga fisiere doar ca sa plimbam trei metode dintr-un rahat de colt in
altul; separarea este justificata numai daca delimiteaza parserul/emitterul de
binding si controllerul runtime.

## 8. Etape de implementare

### Etapa 0 - Baseline si teste RED

- [x] Adauga teste source-generator RED pentru
  `Text="$DataContext.Name:OneWay"` si verifica valoarea initiala plus
  actualizarea dupa `INotifyPropertyChanged`.
- [x] Adauga un test RED pentru o cale nested
  `$DataContext.Type.Name:OneWay`, inclusiv inlocuirea obiectului `Type` si
  dezabonarea de la vechiul segment terminal.
- [x] Adauga teste RED pentru `Text="$DataContext.Count"`: valoarea numerica
  initiala si actualizarile devin text cu `CurrentCulture`, iar o sursa
  nullable `null` produce `string.Empty`.
- [x] Adauga teste RED pentru interpolarea
  `Text="Salut, $DataContext.Name"`: valoarea initiala este compusa, schimbarea
  lui `Name` actualizeaza textul si nu este necesar un mod explicit.
- [x] Adauga teste RED pentru `Text="Literal: \$DataContext.Name"` si pentru
  echivalentul quoted dintr-un assignment conditional: rezultatul contine
  `$DataContext.Name` literal, backslash-ul este consumat si nu exista abonare
  reactiva pentru secventa escaped.
- [x] Adauga un test RED cu mai multe fragmente, inclusiv un `Count` non-string,
  un fragment repetat, un terminal `null` si o cale nested care isi inlocuieste
  un segment intermediar.
- [x] Adauga teste RED pentru interpolari cu `$element.Property`,
  `$self.OtherProperty`, `$owner.Property` in template si
  `$control.parts.$part.Property`, respectand aceleasi name scopes.
- [x] Adauga un test RED care confirma ca un owner `$DataContext` fara
  `INotifyPropertyChanged` primeste diagnostic in locul unui binding care ar
  actualiza doar valoarea initiala.
- [x] Adauga un test RED care ridica `INotifyPropertyChanged` de pe un worker
  thread si confirma ca binding-ul da fail-fast inainte sa citeasca sursa sau sa
  scrie proprietatea tinta; mesajul identifica binding-ul, thread-ul UI capturat
  si thread-ul emitent.
- [x] Adauga un test RED pentru mostenirea si inlocuirea `DataContext` pe un
  element descendent.
- [x] Adauga un test RED `TextBox.Text` cu
  `$DataContext.Name:TwoWay`: sursa initializeaza tinta, input-ul modifica
  sursa, iar o schimbare ulterioara a sursei revine in tinta fara bucla.
- [x] Adauga un test RED pentru
  `$Host.parts.$Chrome.IsEnabled:OneWay` si reconectarea dupa inlocuirea
  `ComponentTemplate`.
- [x] Adauga teste RED pentru binding direct la element denumit,
  `Value="$VolumeSlider.Value"`, `:OneWay` si `:TwoWay`, cu sursa declarata
  atat inainte, cat si dupa tinta in acelasi name scope.
- [x] Adauga un test RED pentru `$self.IsVisible:OneWay` folosit pe alta
  proprietate tinta compatibila si un test de diagnostic pentru
  `IsEnabled="$self.IsEnabled"`.
- [x] Adauga teste RED pentru un binding conditional
  `Text = $DataContext.ShortName;` si forma explicita `:OneWay`; confirma ca
  sunt echivalente, apoi verifica activarea, actualizarile si restaurarea
  valorii de baza.
- [x] Adauga un test RED care confirma ca `Text = "MyText";` ramane literal
  legal si ca `Text = "$DataContext.ShortName:OneWay";` primeste diagnostic.
- [x] Adauga un test RED pentru binding conditional `TwoWay`: write-back-ul
  functioneaza numai cat timp assignment-ul castiga proprietatea si se opreste
  dupa schimbarea ramurii.
- [x] Adauga teste de caracterizare GREEN pentru `$owner.Content` one-way si
  echivalenta lui cu `$owner.Content:OneWay`, plus resursele `$Accent` si
  expresiile `@when` existente, ca noul parser sa nu le confunde cu binding-uri
  de atribut.
- [x] Adauga un test de caracterizare pentru `(A and B) or C` care confirma ca
  toate cele trei surse raman observate, inclusiv ramura short-circuited.
- [x] Ruleaza testele RED si consemneaza exact diagnosticele sau comportamentul
  lipsa inainte de implementare.

  Dovada RED din 2026-07-14: filtrul `MarkupBindingStageZero` are 14 cazuri
  esuate exclusiv pe capabilitatile planificate si un caz de compatibilitate
  GREEN. Binding-urile de atribut/named/`$self`/template-part/conditionale emit
  momentan `CERNEALAUI004`, aliasul `$owner.Content:OneWay` emite
  `CERNEALAUI007`, interpolarile raman text literal, iar caile quoted din
  assignment nu emit inca diagnosticul cerut. Baseline-ul separat este
  `123/123` GREEN, fara skipped.

**Gate etapa 0**

- [x] Testele noi esueaza exclusiv pentru ca binding-urile de atribut,
  named/`$self` si assignment conditional nu sunt implementate; baseline-ul
  existent ramane GREEN.
- [x] Semantica existenta pentru `@when`, template binding si resurse este
  acoperita inainte de refactor.

### Etapa 1 - Contract runtime pentru endpoint-uri observabile

- [x] Extinde `MarkupObservation` cu stare interna de rezolvare si cu un
  contract intern de scriere terminala, fara a expune setterele arbitrare ca
  API general pentru utilizatori.
- [x] Extinde `MarkupDataPathSegment` cu un overload public pentru setter-ul
  terminal emis de generator; pastreaza constructorul existent compatibil.
- [x] Modifica `DataPathObservation` sa pastreze owner-ul terminal curent, sa
  diferentieze terminal `null` de cale incompleta si sa reconecteze getterul si
  setterul dupa schimbarea oricarui segment.
- [x] Permite `UiPropertyObservation` si `TemplatePartPropertyObservation` sa
  raporteze writable numai pentru proprietati care nu sunt read-only.
- [x] Adauga teste runtime focalizate pentru cale simpla, cale nested,
  intermediar `null`, terminal nullable, inlocuire de `DataContext` si template
  swap.
- [x] Adauga teste runtime pentru endpoint-ul unei proprietati UI directe,
  inclusiv setter writable, proprietate read-only si schimbarea valorii pe un
  element denumit sau pe `$self`.
- [x] Adauga teste runtime care confirma ca schimbarea oricarui owner
  `INotifyPropertyChanged` dintr-o cale reconstruieste segmentele descendente si
  ca vechii owneri sunt dezabonati.
- [x] Dupa fiecare modificare C# reindexeaza cu
  `dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- index .\Cerneala.slnx --json`.

  Dovada etapa 1 din 2026-07-14: `GeneratedMarkupObservationTests` este
  `4/4` GREEN, inclusiv contoare explicite de subscriptions pentru ownerii
  vechi, iar regresia source-generator existenta este `123/123` GREEN. Indexul
  Roslyn raporteaza zero warnings. Overload-ul public nou si semantica interna
  de rezolvare sunt sincronizate in paginile API existente.

**Gate etapa 1**

- [x] Endpoint-urile citesc si se reconecteaza identic cu observatiile actuale,
  iar setterul exista numai cand sursa terminala este writable.
- [x] Niciun test existent pentru `GeneratedMarkup` sau directive reactive nu
  regreseaza.

### Etapa 2 - Controllerul de binding generat

- [x] Adauga fabrica generica in `GeneratedMarkup` si controllerul intern care
  leaga un `MarkupObservation` de un `UiProperty<T>` cu `BindingMode`.
- [x] Scrie valorile source-to-target folosind `MarkupBase` pentru binding-ul
  de atribut si `MarkupConditional` pentru assignment-ul activ, niciodata
  `Local`, si elimina numai contributia detinuta la disposal/dezactivare.
- [x] Implementeaza `OneWay` cu initializare imediata, schimbari ulterioare,
  guard de reentrancy si cale temporar indisponibila.
- [x] Permite controllerului o proiectie source-to-target tipizata si aplica
  proiectia incorporata la `string` inainte de scrierea in `MarkupBase` sau
  `MarkupConditional`, fara a introduce un converter public configurabil.
- [x] Adauga un controller/compozitor de interpolare care detine lista
  deduplicata de `MarkupObservation`, recompune string-ul complet la schimbarea
  oricarei surse si respecta lifecycle-ul binding-ului simplu.
- [x] Pentru interpolarea conditionala, activeaza si dezactiveaza toate
  observatiile impreuna cu assignment-ul castigator si face refresh complet la
  reactivare.
- [x] Adauga teste runtime pentru conversia la `string` a numerelor, enum-urilor,
  unui obiect cu `ToString()` suprascris si a lui `null`, inclusiv un test cu
  cultura setata si restaurata determinist.
- [x] Implementeaza `TwoWay` numai pentru endpoint writable; propaga inapoi
  schimbarile tintei din `UiPropertyValueSource.Local` si ignora
  `MarkupConditional`, animatie, aspect si actualizarile proprii.
- [x] Dupa un write `TwoWay` reusit, normalizeaza valoarea efectiva inapoi in
  slotul binding-ului astfel incat un `Local` tranzitoriu sa nu blocheze
  actualizarile viitoare ale sursei.
- [x] Daca o cale `TwoWay` este temporar nerezolvata, ignora write-back-ul fara
  exceptie si restaureaza tinta cand sursa redevine disponibila.
- [x] Daca sursa unui binding conditional activ devine temporar nerezolvata,
  elimina contributia `MarkupConditional`, expune `MarkupBase` si reaplica
  valoarea conditionala numai dupa reconectare.
- [x] Inregistreaza controllerul ca `IElementLifecycleBehavior`: stop la
  detach, refresh la reattach si disposal definitiv pentru lifetime-ul unui
  template inlocuit.
- [x] Adauga activarea/dezactivarea idempotenta a controllerului, cu refresh
  imediat la fiecare reactivare si ignorarea callback-urilor intarziate de la
  o activare veche.
- [x] Captureaza/verifica thread-ul `Update`/UI la activarea controllerului si
  respinge sincron orice callback `PropertyChanged` primit de pe alt thread,
  inainte de evaluarea caii ori mutarea tintei; nu introduce coada sau marshal
  implicit in acest plan.
- [x] Extinde assignment-ul conditional cu un furnizor reactiv, pastrand
  `MarkupConditionController` drept unicul owner al slotului
  `MarkupConditional` si activand cel mult un furnizor per proprietate tinta.
- [x] Adauga teste runtime pentru attach/detach/reattach, disposal idempotent,
  template swap, reentrancy si lipsa actualizarilor dupa disposal.
- [x] Adauga un test de cascada in care `@when` suprascrie binding-ul
  `MarkupBase`, iar dezactivarea regulii restaureaza valoarea curenta a sursei.
- [x] Adauga teste de cascada cu doua ramuri care ofera binding-uri diferite
  aceleiasi proprietati; numai binding-ul ramurii castigatoare poate actualiza
  tinta sau sursa `TwoWay`.
- [x] Reindexeaza solutia dupa fiecare modificare C# sau project-file.

  Dovada etapa 2 din 2026-07-14: `GeneratedMarkupBindingTests` este `11/11`
  GREEN; matricea combinata cu observatiile si binding-urile existente este
  `34/34` GREEN; regresia source-generator existenta este `123/123` GREEN.
  Sunt acoperite sloturile, conversia cu cultura, interpolarea deduplicata,
  TwoWay si normalizarea `Local`, fallback-ul conditional, lifecycle-ul,
  template swap-ul si fail-fast off-thread inainte de getter. Indexul Roslyn
  are zero warnings, iar noile fabrici publice sunt documentate in paginile API
  existente.

**Gate etapa 2**

- [x] Controllerul trece testele one-way, two-way, activare conditionala,
  interpolare, cascada si lifecycle fara subscription leaks sau bucle
  recursive.
- [x] `BindingOperations` si `UiPropertyBinding<T>` isi pastreaza comportamentul
  public existent.

### Etapa 3 - Parser si rezolvare semantica comuna

- [x] Adauga un parser minimal pentru sufixul final optional `:OneWay` /
  `:TwoWay`; foloseste `OneWay` cand lipseste si pastreaza locatia exacta pentru
  un mod prezent, dar invalid.
- [x] Extinde parserul de assignment-uri sa recunoasca expresia de binding ca
  token unquoted terminat de `;`, fara continut literal sau resturi dupa mod.
- [x] Parseaza string-urile quoted ca fragmente literale plus interpolari
  optionale, dar diagnosticheaza un string format exclusiv dintr-o singura cale
  care trebuia scrisa ca binding unquoted.
- [x] Adauga scannerul de interpolare pentru tinte `string`: separa fragmentele
  literale de cele mai lungi cai valide, refoloseste resolverul semantic comun
  si deduplica observatiile identice. Proceseaza `\$` inaintea recunoasterii
  cailor, eliminand backslash-ul si pastrand `$` ca text.
- [x] Respinge orice `:Mode` intr-un fragment interpolat si pastreaza ca literal
  un `$` care nu incepe o cale valida.
- [x] Introdu descriptorul intern comun de sursa si muta in el rezolvarea
  folosita astazi de `EmitObservation`, fara sa schimbi AST-ul logic deja
  implementat.
- [x] Refoloseste descriptorul in `UiMarkupReactiveEmitter` pentru frunzele din
  `@when` si `@if`; pastreaza deduplicarea existenta dupa expresia canonica.
- [x] Rezolva `$DataContext.Path` la compile time, emite getter pentru fiecare
  segment si setter numai pentru proprietatea terminala writable.
- [x] Rezolva `$element.Property` la o proprietate UI a unui element denumit
  din acelasi name scope, inclusiv forward references, si emite setter numai
  pentru `TwoWay` pe o proprietate care nu este read-only.
- [x] Rezolva `$self.Property` la elementul tinta si compara identitatea
  proprietatii sursa cu proprietatea tinta; emite diagnostic dedicat pentru
  self-binding direct si permite o alta proprietate compatibila.
- [x] Rezolva `$control.parts.$part.Property` cu exact cele patru segmente
  existente inaintea sufixului de mod si emite endpoint-ul de template part.
- [x] Pastreaza `$owner.Property` si `$owner.Property:OneWay` pe traseul
  `TemplateBinding` existent si diagnosticheaza `$owner.Property:TwoWay`.
- [x] Valideaza tipul sursei fata de `PropertySpec.ValueType`, target-ul
  read-only si writable source pentru `TwoWay` inainte de emiterea C#.
- [x] Accepta incompatibilitatea source/target numai cand tinta este `string`
  si modul este `OneWay`; ataseaza descriptorului proiectia standard cu
  `CurrentCulture` si respinge aceeasi pereche de tipuri pentru `TwoWay`.
- [x] Valideaza ca fiecare owner CLR dintr-o cale `$DataContext` implementa
  `INotifyPropertyChanged`; sursele `UiObject` continua sa foloseasca
  `PropertyChanged`, iar tipurile fara notificare primesc diagnostic.
- [x] Confirma ca documentele `Window<TViewModel>` si
  `UserControl<TViewModel>` folosesc tipul generic existent cand `DataType` nu
  este repetat, conform rezolvarii actuale.
- [x] Reindexeaza solutia dupa fiecare modificare C# sau project-file.

**Gate etapa 3**

- [x] Acelasi resolver semantic alimenteaza binding-urile de atribut si
  assignment, interpolarile si frunzele reactive, fara doua implementari de
  path walking.
- [x] Codul generat nu contine reflection, string property paths sau lookup de
  membri la runtime.

Dovada etapa 3 din 2026-07-14: parserul si descriptorul comun sunt acoperite de
`BindingStageThree` (`6/6`), inclusiv offset-ul exact al modului invalid,
assignment unquoted, quoted/interpolare, `\$`, deduplicare, toate endpoint-urile,
validarea de tip/writability/observabilitate si inferenta generica pentru
`Window<TViewModel>` / `UserControl<TViewModel>`. Suita source-gen fara probele
RED din etapa 0 este verde (`129/129`), indexarea are zero warnings, iar
RoslynIndexer nu mai gaseste simbolurile vechi `EmitDataObservation` sau
`EmitTemplatePartObservation`; inspectia codului generat confirma acces tipizat,
fara reflection ori property paths evaluate la runtime.

### Etapa 4 - Emiterea binding-urilor de markup

- [x] Extinde `EmitProperty` sa detecteze binding-ul inaintea referintelor la
  resurse, dar numai pentru formele cu sursa si mod valide.
- [x] Emite observatia, controllerul si inregistrarea lifetime pentru
  `$DataContext.Path:OneWay` si `:TwoWay`.
- [x] Emite proiectia source-to-string pentru atribute si assignment-uri
  conditionale `OneWay`, fara reflection sau string property paths.
- [x] Emite interpolarile ca fragmente literale plus observatii tipizate si o
  functie de compozitie, fara evaluarea runtime a caii scrise ca string.
- [x] Emite binding-uri directe pentru `$element.Property[:Mode]` dupa
  construirea tuturor elementelor denumite din scope, inclusiv cand sursa este
  declarata dupa tinta si cand modul implicit este folosit.
- [x] Emite `$self.OtherProperty[:Mode]` folosind aceeasi observatie de
  proprietate UI ca sursele denumite, fara un controller runtime duplicat si
  cu `OneWay` implicit.
- [x] Emite binding-uri catre template parts si confirma reconectarea la
  `ComponentTemplate` nou.
- [x] Emite pentru assignment-urile conditionale unquoted fabrica de binding
  activabil, aplica `OneWay` implicit si o inregistreaza in
  regula/proprietatea corespunzatoare din `MarkupConditionController`.
- [x] Confirma ca un binding conditional inactiv nu observa sursa sa, nu face
  write-back si reciteste valoarea curenta imediat cand ramura redevine
  castigatoare.
- [x] Pentru elemente din `@template`, inregistreaza controllerul prin
  `TemplateEmissionContext.RegisterLifetime`; pentru elemente obisnuite,
  foloseste lifecycle owner-ul tintei.
- [x] Pastreaza ordinea de emitere astfel incat toate elementele denumite si
  template-urile necesare sursei sa existe inainte de atasarea binding-ului.
- [x] Inspecteaza codul generat pentru exemple simple cu mod implicit/explicit,
  nested, direct named, `$self`, conditional unquoted, two-way si part: toate
  accesarile trebuie sa fie tipizate si complet calificate.
- [x] Reindexeaza solutia dupa fiecare modificare C# sau project-file.

**Gate etapa 4**

- [x] Exemplele tinta, inclusiv binding direct intre elemente si binding
  conditional unquoted cu `OneWay` implicit plus interpolari multi-source,
  compileaza si functioneaza end-to-end in factory, `Window<TViewModel>` si
  `UserControl<TViewModel>`.
- [x] Niciun binding generat nu ramane activ dupa template disposal sau detach.

Dovada etapa 4 din 2026-07-14: scenariile Stage 0 plus Stage 4 sunt GREEN
(`19/19`), intreaga suita source-generator este GREEN (`148/148`), iar matricea
runtime pentru binding-uri si observatii este GREEN (`15/15`), fara teste
skipped. Inspectia sursei generate confirma fabrici tipizate si complet
calificate pentru binding direct, nested, named forward, `$self`, conditional,
interpolare si template lifetime, fara reflection. Testele end-to-end acopera
factory, `Window<TViewModel>`, `UserControl<TViewModel>`, template swap si
detach/reattach; RoslynIndexer raporteaza zero warnings, iar `git diff --check`
nu raporteaza erori.

### Etapa 5 - Diagnostice si compatibilitate

- [x] Adauga diagnostice testate pentru mod necunoscut, cale goala, proprietate
  terminala lipsa si `parts` scris cu alta capitalizare; lipsa modului este
  legala si selecteaza `OneWay`.
- [x] Adauga diagnostice pentru `DataType` lipsa, membru inexistent,
  getter inaccesibil, incompatibilitate de tip si target read-only.
- [x] Adauga diagnostic pentru `TwoWay` pe o proprietate sursa fara setter
  accesibil sau pe o proprietate de template part read-only.
- [x] Adauga diagnostic pentru `TwoWay` intre o sursa non-string si o tinta
  `string`, explicand ca proiectia automata la text este numai `OneWay` si ca
  parsing-ul invers necesita un converter viitor.
- [x] Adauga diagnostice pentru element denumit inexistent, referinta in afara
  name scope-ului, proprietate UI terminala inexistenta/read-only pentru
  `TwoWay` si self-binding direct al proprietatii tinta la ea insasi.
- [x] Adauga diagnostice pentru `:OneWay` si `:TwoWay` folosite in `@when` sau
  `@if`, explicand ca directivele sunt intotdeauna read-only.
- [x] Diferentiaza in teste modul interzis in expresia conditiei de modul legal
  din partea dreapta a unui assignment aflat in corpul aceleiasi directive.
- [x] Adauga diagnostice pentru binding unquoted fara `;`, cu text ramas dupa
  mod, cu mod necunoscut sau folosit ca fragment intr-o valoare mai lunga.
- [x] Adauga diagnostic pentru o cale de binding quoted in assignment, inclusiv
  cu mod implicit, `:OneWay` sau `:TwoWay`, fara sa respinga string-uri literale
  obisnuite precum `"MyText"` sau interpolari cu text literal real.
- [x] Adauga diagnostice pentru `:OneWay`, `:TwoWay` sau mod necunoscut in orice
  fragment interpolat si pentru self-referinta aceleiasi proprietati tinta,
  inclusiv cand elementul este referit prin nume in loc de `$self`.
- [x] Adauga diagnostic pentru orice owner CLR dintr-o cale reactiva care nu
  implementeaza `INotifyPropertyChanged`, explicand ca binding-ul nu poate
  observa o proprietate CLR fara un semnal de schimbare.
- [x] Testeaza ca `$Accent`, `$NamedAspect`, brush resources si valorile
  literale cu `:` nu sunt interpretate accidental ca binding-uri.
- [x] Testeaza ca un string cu o cale in interior, precum
  `"Salut, $DataContext.Name"`, este interpolat si reactiv, in timp ce
  `"Salut, lume"` ramane literal, iar intreaga valoare `$DataContext.Count`
  foloseste binding-ul simplu cu proiectie la `string`.
- [x] Testeaza punctuatia si delimitarea mai multor cai interpolate, fragmentele
  repetate, `$` literal care nu formeaza o cale si resursa intreaga `$Accent`.
- [x] Testeaza contrastul dintre `$DataContext.Name` reactiv si
  `\$DataContext.Name` literal in atribute si directive, inclusiv o secventa
  escaped care seamana cu un fragment cu `:OneWay` / `:TwoWay` si nu trebuie
  interpretata sau diagnosticata drept mod de binding.
- [x] Testeaza ca `$owner.Content` continua sa emita `context.Bind(...)` si ca
  regulile conditionale din template ii restaureaza valoarea.
- [x] Testeaza ca o cale catre template part fara proprietate terminala este
  respinsa atat in atribut, cat si in `@when`.
- [x] Testeaza ca `$VolumeSlider.Value` si
  `$VolumeSlider.Value:OneWay` emit acelasi binding, iar `$VolumeSlider` fara
  proprietate terminala continua sa urmeze contractul de resursa/diagnostic
  existent.
- [x] Testeaza ca assignment-ul unquoted fara mod si cel cu `:OneWay` emit
  acelasi descriptor semantic, in timp ce forma quoted primeste diagnostic.
- [x] Reindexeaza solutia dupa fiecare modificare C# sau project-file.

**Gate etapa 5**

- [x] Toate formele invalide esueaza in source generator cu diagnostic
  actionabil, nu cu exceptie runtime sau eroare C# obscura in codul generat.
- [x] Sintaxa existenta pentru resurse, aspects, templates si directive ramane
  compatibila, iar `:Mode` din conditia `@when` ramane ilegal chiar daca este
  legal in assignment-ul ramurii.

Dovada etapa 5 din 2026-07-14: matricea dedicata de diagnostice si
compatibilitate este GREEN (`10/10`), intreaga suita source-generator este
GREEN (`158/158`), iar testele runtime targetate pentru binding-uri, observatii
si template binding sunt GREEN (`23/23`), fara teste skipped. Sunt acoperite
sintaxa si modurile invalide, accesibilitatea, tipurile, writability, name
scope-ul, self-binding-ul direct si numit, resursele/aspects, interpolarea,
escape-ul `\$`, `$owner` cu restaurare conditionala si template parts in
atribute si conditii. RoslynIndexer raporteaza zero warnings, iar
`git diff --check` nu raporteaza erori.

### Etapa 6 - Documentatie si API public

- [x] Actualizeaza documentatia conceptuala `.cui.xml` cu gramatica
  `source-path[:mode]`, `OneWay` implicit, exemple OneWay/TwoWay, cai nested,
  elemente denumite, `$self`, template parts si cerinta `DataType`.
- [x] Documenteaza binding-urile unquoted din assignment-uri conditionale,
  diagnosticul pentru o cale quoted, faptul ca ghilimelele raman obligatorii
  in atribute si ciclul activate/deactivate/refresh al ramurii castigatoare.
- [x] Documenteaza conversia automata `OneWay` catre `string`, cultura curenta,
  rezultatul gol pentru `null` si lipsa conversiei inverse `TwoWay`.
- [x] Documenteaza interpolarea reactiva, caile multiple, delimitarea,
  conversia fiecarui fragment, lipsa modurilor in fragmente si diferenta dintre
  binding integral, interpolare si literal simplu, inclusiv `\$` pentru un `$`
  literal.
- [x] Documenteaza obligatia `INotifyPropertyChanged` pentru ownerii CLR din
  `$DataContext` si faptul ca `UiObject`/template parts folosesc sistemul lor de
  proprietati; nu promite observarea magica a unei auto-properties fara event.
- [x] Documenteaza faptul ca `PropertyChanged` pentru o sursa legata trebuie
  ridicat pe thread-ul `Update`/UI, comportamentul fail-fast off-thread si faptul
  ca acest plan nu ofera auto-marshaling.
- [x] Documenteaza diagnosticul pentru self-binding direct si arata separat un
  exemplu legal in care `$self` citeste alta proprietate a aceluiasi element.
- [x] Documenteaza explicit ca sursele din `@when` sunt binding-uri read-only
  compuse, ca toate frunzele sunt observate si ca short-circuit-ul afecteaza
  numai evaluarea.
- [x] Documenteaza semantica pentru intermediar `null`, terminal nullable,
  source/target read-only, cascada si lifecycle.
- [x] Foloseste skill-ul `writing-api-documentation` pentru orice membru public
  nou sau modificat din `GeneratedMarkup`, `MarkupObservation` si
  `MarkupDataPathSegment`.
- [x] Actualizeaza paginile corespunzatoare exclusiv sub
  `docs-site/documentation/classes/` si sincronizeaza
  `docs-site/documentation/manifest.json` numai daca se adauga sau se redenumesc
  pagini.
- [x] Ruleaza un public API diff si confirma ca schimbarea este limitata la
  helper-ele necesare codului source-generated; nu expune controllerul intern.

**Gate etapa 6**

- [x] Documentatia descrie exact sintaxa acceptata, limitele si comportamentul
  testat, fara exemple WPF care nu compileaza in Cerneala.
- [x] Toate schimbarile publice au pagini API sincronizate si manifest valid.

Dovada etapa 6 din 2026-07-14: ghidul conceptual `docs/markup-data-bindings.md`
si pagina de getting started descriu gramatica, modurile, sursele, conversia,
interpolarea, conditiile, nulurile, cascada, lifecycle-ul si contractul de
threading. Paginile API pentru `GeneratedMarkup`, `MarkupObservation`,
`MarkupDataPathSegment`, `MarkupConditionalValue` si `UiMarkupGenerator` sunt
sincronizate sub `docs-site/documentation/classes/`. Auditul Roslyn/public diff
limiteaza suprafata noua la constructorul writable al segmentului si cele cinci
factory/helper methods necesare codului generat; controllerul si endpoint-urile
raman interne. Manifestul JSON are `858` intrari, cate una valida pentru fiecare
pagina afectata, si a ramas neschimbat deoarece nu s-a adaugat sau redenumit
nicio pagina. Verificarea automata a continutului, linkurilor si placeholderelor
este GREEN, iar `git diff --check` nu raporteaza erori.

### Etapa 7 - Verificare finala

- [x] Ruleaza testele source-generator targetate:
  `dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj --filter FullyQualifiedName~UiMarkupGeneratorTests`.
- [x] Ruleaza testele runtime targetate:
  `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter FullyQualifiedName~GeneratedMarkupBindingTests`.
- [x] Ruleaza testele existente de binding si template:
  `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~UiPropertyBindingTests|FullyQualifiedName~TextBoxTwoWayBindingTests|FullyQualifiedName~TemplateBindingTests"`.
- [x] Ruleaza intreaga suita cu `dotnet test .\Cerneala.slnx` si nu accepta
  teste failed sau skipped noi.
- [x] Inspecteaza manual codul generat pentru lipsa reflection-ului,
  subscriptions duplicate si setter emis pe segmente intermediare.
- [x] Executa un smoke test cu detach/reattach repetat, schimbare de
  `DataContext`, template swap, forward reference la element denumit, schimbare
  intre doua binding-uri conditionale, interpolare cu mai multe surse si o
  expresie `(A and B) or C`.
- [x] Reindexeaza final cu
  `dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- index .\Cerneala.slnx --json` si confirma zero warnings.
- [x] Revizuieste `git diff` pentru a confirma ca nu au intrat convertere,
  reflection paths sau alte extensii din non-obiective.

**Gate etapa 7**

- [x] Testele targetate si suita completa sunt GREEN.
- [x] Codul generat, public API diff, documentatia si RoslynIndexer sunt curate.

Dovada etapa 7 din 2026-07-14: testele source-generator targetate sunt GREEN
(`158/158`), testele runtime targetate sunt GREEN (`11/11`), iar regresiile
existente de binding/template sunt GREEN (`20/20`). Suita completa este GREEN:
`1769/1769` runtime plus `158/158` source-generator, in total `1927` teste, cu
zero failed si zero skipped. Smoke-ul compus a trecut separat (`1` runtime si
`6` source-generator) pentru detach/reattach, schimbare de `DataContext`,
template swap, forward named source, doi furnizori conditionali, interpolare
multi-sursa si `(A and B) or C`. Inspectia emitterului si a testului de cod
generat confirma accesori tipizati fara reflection, deduplicare dupa expresia
canonica si setter numai pe segmentul terminal; auditul productiei raporteaza
zero termeni interzisi. Reindexarea finala acopera `1936` documente, `27843`
simboluri si `114062` referinte cu zero warnings, iar `git diff --check` este
curat.

## 9. Ordinea recomandata

1. Ingheata comportamentul existent si adauga testele RED.
2. Extinde endpoint-urile runtime fara sa atingi inca sintaxa markup.
3. Implementeaza si verifica controllerul one-way/two-way plus lifecycle.
4. Extrage resolverul semantic comun si conecteaza directivele existente.
5. Emite binding-urile de atribut, sursele named/`$self` si furnizorii
   conditionali unquoted plus interpolarile cu `OneWay` implicit, apoi inchide
   diagnosticele de tip, scope si observabilitate.
6. Actualizeaza documentatia si ruleaza verificarile complete.

Nu continua la emitterul de atribut daca endpoint-ul runtime nu trece testele
de reconectare si disposal. Nu adauga convertere sau multi-binding ca sa
"pregatim viitorul"; viitorul se descurca si singur, fir-ar sa fie.

## 10. Definitia de gata

- [x] `Text="$DataContext.Name:OneWay"` initializeaza si actualizeaza tinta.
- [x] `Text="$DataContext.Name"` este echivalent cu forma explicita
  `:OneWay`, iar `:TwoWay` ramane opt-in.
- [x] Caile nested se reconecteaza corect cand se schimba orice segment.
- [x] `TwoWay` propaga input-ul tintei inapoi fara bucle si continua sa accepte
  schimbari ulterioare din sursa.
- [x] `$control.parts.$part.Property[:Mode]` functioneaza cu `OneWay` implicit
  si se reconecteaza dupa template swap.
- [x] `$element.Property[:Mode]` functioneaza pentru referinte backward si
  forward din acelasi name scope, cu `OneWay` implicit si diagnostic pentru
  surse inaccesibile.
- [x] `$self.OtherProperty[:Mode]` functioneaza, iar legarea
  proprietatii tinta la ea insasi este respinsa cu diagnostic precis.
- [x] `@when` foloseste aceleasi surse tipizate, observa toate frunzele si
  respecta `and`, `or`, parantezele si short-circuit-ul.
- [x] Assignment-urile conditionale accepta binding-uri unquoted cu `OneWay`
  implicit, resping ca diagnostic o cale quoted, activeaza numai binding-ul
  castigator, opresc write-back-ul cand acesta pierde si fac refresh la
  reactivare.
- [x] `Text="Salut, $DataContext.Name"` si interpolarile cu mai multe cai se
  recompun la orice schimbare, convertesc valorile non-string, trateaza `null`
  ca gol si resping modurile pe fragmente.
- [x] `\$` produce un `$` literal atat in atribute, cat si in string-urile quoted
  din directive, fara observatii sau diagnostice false de binding.
- [x] Nicio cale `$DataContext` cu owner CLR neobservabil nu compileaza ca un
  binding aparent reactiv; diagnosticul cere `INotifyPropertyChanged`.
- [x] Orice notificare `PropertyChanged` off-thread este respinsa determinist
  inainte de accesarea UI-ului, cu eroare actionabila; nicio actualizare nu este
  executata sau pusa implicit in coada de binding.
- [x] Cai incomplete, terminale nullable, detach/reattach si disposal au
  comportament determinist si testat.
- [x] Binding-ul de markup este suprascris/restaurat corect de
  `MarkupConditional`.
- [x] `$owner.Property` si `$owner.Property:OneWay` sunt echivalente,
  `$owner.Property:TwoWay` este diagnosticat, iar referintele la resurse raman
  compatibile.
- [x] Nu exista reflection paths, convertere sau moduri suplimentare.
- [x] Diagnosticele sunt precise, documentatia publica este sincronizata,
  testele sunt GREEN si indexul Roslyn este curat.
