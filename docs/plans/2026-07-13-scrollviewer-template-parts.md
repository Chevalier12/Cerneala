# Plan: ScrollViewer, ScrollBar si Track Template Parts

> Data: 2026-07-13
> Status: finalizat
> Dependenta: `docs/plans/2026-07-13-repeat-button.md`
> Scop: inlocuirea compozitiei hardcodate cu parti de template functionale si adaugarea butoanelor directionale ale scrollbar-ului

## 1. Rezumat

Implementarea actuala construieste direct `ScrollContentPresenter`, doua `ScrollBar`, un `Track` si un `Thumb`. Cand se aplica un `ComponentTemplate`, fallback-urile pot fi scoase din arbore, dar controalele continua sa sincronizeze instantele private vechi. Template-ul arata nou, iar logica vorbeste cu mobila din fostul apartament.

Planul introduce contracte explicite de parti, template-uri implicite functionale si un lifecycle comun pentru rezolvarea/dezabonarea partilor. `ScrollViewer` va opera pe presenterul si scrollbar-urile template-ului activ. `ScrollBar` va opera pe track-ul si cele doua `RepeatButton`-uri active. `Track` va opera pe thumb-ul activ.

Template-ul implicit al `ScrollBar` va afisa butoane directionale la capete. Acestea produc schimbari mici. Click-ul pe zona track-ului dinaintea sau de dupa thumb ramane schimbare mare.

## 2. Contractul de compozitie

Structura tinta:

```text
ScrollViewer
  PART_ScrollContentPresenter : ScrollContentPresenter
  PART_HorizontalScrollBar    : ScrollBar
  PART_VerticalScrollBar      : ScrollBar

ScrollBar
  PART_DecreaseButton         : RepeatButton
  PART_Track                  : Track
  PART_IncreaseButton         : RepeatButton

Track
  PART_Thumb                  : Thumb
```

Semantica partilor:

- `PART_ScrollContentPresenter`, `PART_HorizontalScrollBar` si `PART_VerticalScrollBar` sunt obligatorii pentru un `ScrollViewer` functional.
- `PART_Track` este obligatoriu pentru un `ScrollBar` functional.
- `PART_DecreaseButton` si `PART_IncreaseButton` sunt optionale pentru template-uri minimaliste, dar template-ul implicit le furnizeaza.
- `PART_Thumb` este obligatoriu pentru un `Track` dragabil.
- Sagetile nu sunt thumb-uri. Exista un singur thumb, adica manerul mobil.

## 3. Obiective

- [x] `ScrollViewer`, `ScrollBar` si `Track` declara `[TemplatePart]` pentru contractele de mai sus.
- [x] Toata logica foloseste partile template-ului activ, nu instante fallback ramase in campuri private.
- [x] Schimbarea sau eliminarea template-ului dezaboneaza partile vechi.
- [x] Template-urile invalide esueaza devreme cu mesaje care numesc partea lipsa sau tipul gresit.
- [x] Template-ul implicit `ScrollViewer` foloseste un layout root real si pastreaza convergenta scrollbar-urilor `Auto`.
- [x] Template-ul implicit `ScrollBar` functioneaza vertical si orizontal.
- [x] Butoanele directionale folosesc `SmallChange` si repeta cat timp sunt apasate.
- [x] Click-ul pe track foloseste `LargeChange`.
- [x] Drag-ul thumb-ului continua sa actualizeze valoarea si offset-ul viewer-ului.
- [x] `ScrollEventType` reflecta corect sursa schimbarii.
- [x] API-urile publice `Presenter`, `HorizontalScrollBar`, `VerticalScrollBar`, `Track` si `Thumb` expun partile active.
- [x] Layout-ul idle nu lasa measure/arrange work restant.

## 4. Non-obiective

- [x] Nu introducem inertial scrolling sau overscroll.
- [x] Nu introducem overlay scrollbars ori auto-hide animat.
- [x] Nu schimbam politica existenta `Disabled`, `Auto`, `Hidden`, `Visible`.
- [x] Nu rescriem `IScrollInfo` sau virtualizarea.
- [x] Nu adaugam comenzi publice WPF precum `LineUpCommand` daca butoanele pot fi legate simplu si testabil la `ScrollBar`.
- [x] Nu facem un sistem general de template triggers in aceasta schimbare.
- [x] Nu mutam logica de scrolling in aspect system doar pentru ca putem face rahatul mai abstract.

## 5. Arhitectura propusa

### 5.1 Lifecycle comun pentru parti

Extinde `Control` cu un hook protected apelat dupa fiecare aplicare sau eliminare de template si cu un resolver tipizat pentru parti. Forma exacta poate varia, dar contractul trebuie sa permita:

```csharp
protected virtual void OnTemplateApplied(ComponentTemplateInstance? instance);
protected TElement GetRequiredTemplatePart<TElement>(string name)
    where TElement : UIElement;
protected TElement? GetOptionalTemplatePart<TElement>(string name)
    where TElement : UIElement;
```

Reguli:

- controlul derivat se dezaboneaza intai de la partile memorate anterior;
- rezolva si valideaza partile noii instante;
- abia apoi conecteaza evenimentele si sincronizeaza starea;
- daca rezolvarea esueaza, noua instanta este detasata/disposed si controlul nu ramane pe jumatate aplicat;
- `ApplyTemplate()` ramane idempotent pentru aceeasi instanta.

### 5.2 Template implicit ScrollViewer

Template-ul implicit foloseste un `Grid` cu doua randuri si doua coloane:

```text
*    | Auto
-----+-----
Auto | corner
```

Plasare:

- presenter in randul 0, coloana 0;
- scrollbar vertical in randul 0, coloana 1;
- scrollbar orizontal in randul 1, coloana 0;
- coltul poate ramane gol in MVP.

`ScrollViewer.MeasureCore` aplica template-ul si masoara root-ul in pana la trei treceri. Dupa fiecare trecere citeste extent/viewport din presenterul activ, recalculeaza vizibilitatea partilor si remasoara numai daca starea s-a schimbat. `ArrangeCore` repeta aceeasi convergenta pentru viewport-ul final. Astfel template-ul stabileste layout-ul, iar owner-ul stabileste politica de scrolling.

### 5.3 Template implicit ScrollBar

Template-ul implicit foloseste un panel intern mic, orientabil, care distribuie:

```text
DecreaseButton | Track flexibil | IncreaseButton
```

Pentru verticala ordinea este sus, centru, jos. Pentru orizontala este stanga, centru, dreapta. Panelul primeste `Orientation` prin template binding si nu necesita recrearea template-ului cand orientarea se schimba.

Butoanele contin glyph-uri directionale simple si clare. Glyph-ul este doar prezentare; semantica vine din faptul ca partile sunt decrease/increase.

### 5.4 Template implicit Track

Template-ul implicit foloseste un root intern care aranjeaza `PART_Thumb` dupa geometria calculata de `Track`. Template-urile custom pot inlocui prezentarea, dar trebuie sa furnizeze un `Thumb` valid si sa respecte axa/orientarea.

Geometria valorii, lungimea thumb-ului si conversia pointer-valoare raman intr-un singur loc in `Track`; nu se copiaza formulele in template si in control.

## 6. Fisiere estimate

Fisiere noi posibile:

- `UI/Controls/ScrollViewerTemplates.cs`;
- `UI/Controls/Primitives/ScrollBarTemplates.cs`;
- `UI/Controls/Primitives/ScrollBarLayoutPanel.cs`;
- `UI/Controls/Primitives/TrackTemplates.cs`;
- `UI/Controls/Primitives/TrackLayoutPanel.cs`;
- `UI/Controls/Primitives/TrackValueChangedEventArgs.cs`, numai daca este necesar pentru motivul schimbarii;
- teste dedicate pentru part lifecycle si template swap.

Fisiere modificate probabil:

- `UI/Controls/Control.cs`;
- `UI/Controls/ScrollViewer.cs`;
- `UI/Controls/Primitives/ScrollBar.cs`;
- `UI/Controls/Primitives/Track.cs`;
- `UI/Controls/Primitives/ScrollEventArgs.cs` numai daca trebuie clarificat contractul existent;
- `tests/Cerneala.Tests/Controls/ComponentTemplateLifecycleTests.cs`;
- `tests/Cerneala.Tests/Controls/ScrollViewerTests.cs`;
- `tests/Cerneala.Tests/Controls/ScrollBarTests.cs`;
- `tests/Cerneala.Tests/Controls/Primitives/TrackTests.cs`;
- paginile API corespunzatoare din `docs-site/documentation/classes/`;
- `docs-site/documentation/manifest.json` daca apar tipuri publice noi.

## 7. Etape de implementare

### Etapa 0 - Baseline si contracte de caracterizare

- [x] Finalizeaza planul `RepeatButton` si confirma ca testele lui sunt verzi.
- [x] Genereaza `FileTree.md` si actualizeaza indexul RoslynIndexer.
- [x] Ruleaza testele existente pentru `ScrollViewer`, `ScrollBar`, `Track`, `Thumb`, template lifecycle si layout scheduler.
- [x] Adauga teste de caracterizare pentru wheel, drag, track click, visibility policy si convergenta `Auto`.
- [x] Adauga teste de caracterizare pentru cadrul idle fara lucru restant.
- [x] Adauga un test care demonstreaza defectul actual: un track furnizat de template nu controleaza `ScrollBar`.
- [x] Adauga un test care demonstreaza defectul actual: partile unui template `ScrollViewer` nu devin partile active.
- [x] Adauga un test care demonstreaza defectul actual: un thumb de template nu este sursa drag-ului activ.

**Gate etapa 0**

- [x] Baseline-ul existent este verde.
- [x] Cele trei defecte de template sunt reproduse prin teste rosii.
- [x] Nu s-a schimbat inca API-ul public.

### Etapa 1 - Lifecycle-ul partilor in Control

- [x] Adauga resolverele tipizate pentru parti obligatorii si optionale.
- [x] Adauga hook-ul protected pentru template aplicat/eliminat.
- [x] Apeleaza hook-ul si cand `ComponentTemplate` devine `null`.
- [x] Defineste ordinea exacta: detach subscriptions vechi, dispose instanta veche, attach instanta noua, validate parts, publish instanta.
- [x] Daca hook-ul noii instante arunca, revino la o stare coerenta fara root partial atasat.
- [x] Pastreaza `ApplyTemplate()` idempotent.
- [x] Adauga teste pentru apply repetat, template swap, template null, parte lipsa si tip gresit.
- [x] Adauga teste care verifica lipsa handlerelor ramase pe partile vechi.
- [x] Verifica `CheckBox` si migreaza-l la helper numai daca reduce cod fara regresie.

**Gate etapa 1**

- [x] Lifecycle-ul este acoperit independent de scrolling.
- [x] O parte veche nu mai poate modifica owner-ul dupa template swap.
- [x] Erorile indica numele partii si tipul asteptat.
- [x] Toate controalele templated existente raman verzi.

### Etapa 2 - Track si PART_Thumb

- [x] Declara `[TemplatePart("PART_Thumb", typeof(Thumb))]` pe `Track`.
- [x] Introdu template-ul implicit si inregistreaza `PART_Thumb` prin `RequirePart`.
- [x] Inlocuieste campul readonly folosit ca fallback cu referinta la thumb-ul activ.
- [x] Conecteaza `DragDelta` numai la thumb-ul activ.
- [x] Deconecteaza `DragDelta` de la thumb-ul vechi la template swap/null/detach.
- [x] Pastreaza formulele actuale pentru range, ratio, viewport si lungimea thumb-ului.
- [x] Muta layout-ul implicit intr-un root/panel dedicat fara a duplica formulele.
- [x] Pastreaza click-ul inainte/dupa thumb ca `LargeDecrement`/`LargeIncrement`.
- [x] Introdu un motiv intern al schimbarii daca `ValueChanged` simplu nu poate informa corect `ScrollBar`.
- [x] Pastreaza `Thumb` ca proprietate publica ce returneaza partea activa.

Teste etapa 2:

- [x] Thumb-ul implicit se pozitioneaza identic cu baseline-ul.
- [x] Thumb-ul proportional foloseste `ViewportSize`.
- [x] Drag-ul thumb-ului custom modifica valoarea.
- [x] Drag-ul thumb-ului vechi nu mai modifica valoarea dupa template swap.
- [x] Track click inaintea thumb-ului produce large decrement.
- [x] Track click dupa thumb produce large increment.
- [x] Orientarea orizontala si verticala folosesc axa corecta.
- [x] Range zero si track mai scurt decat minimul thumb-ului raman stabile.

**Gate etapa 2**

- [x] `Track` nu mai sincronizeaza un thumb invizibil.
- [x] Geometria nu este duplicata intre control si template root.
- [x] Testele vechi si noi pentru drag/layout sunt verzi.

### Etapa 3 - ScrollBar, PART_Track si butoanele directionale

- [x] Declara partile `PART_Track`, `PART_DecreaseButton` si `PART_IncreaseButton`.
- [x] Creeaza template-ul implicit orientabil cu `RepeatButton` la capete.
- [x] Rezolva `PART_Track` ca parte obligatorie.
- [x] Rezolva cele doua butoane ca parti optionale pentru a permite scrollbars fara sageti.
- [x] Sincronizeaza `Minimum`, `Maximum`, `Value`, `SmallChange`, `LargeChange`, `ViewportSize` si `Orientation` catre track-ul activ.
- [x] Sincronizeaza modificarile track-ului inapoi in `ScrollBar.Value`.
- [x] Leaga decrease la `Track.DecreaseSmall()` si increase la `Track.IncreaseSmall()`.
- [x] Nu executa schimbarea mica si prin `Command`, si prin handler; alege un singur drum intern.
- [x] Ridica `Scroll` cu `SmallDecrement` sau `SmallIncrement` pentru sageti.
- [x] Ridica `LargeDecrement` sau `LargeIncrement` pentru click-urile pe track.
- [x] Ridica `ThumbTrack` numai pentru drag, nu pentru orice schimbare de valoare.
- [x] Decide si testeaza daca `EndScroll` se ridica la release; nu-l adauga doar ca decor daca nu exista consumator.
- [x] Pastreaza `Track` ca proprietate publica ce returneaza partea activa.
- [x] Deconecteaza toate evenimentele partilor vechi la template swap.

Teste etapa 3:

- [x] Scrollbar vertical afiseaza butoanele sus/jos si track-ul intre ele.
- [x] Scrollbar orizontal afiseaza butoanele stanga/dreapta.
- [x] Schimbarea `Orientation` rearanjeaza template-ul fara recreare manuala.
- [x] Click-ul initial pe decrease modifica valoarea cu `SmallChange`.
- [x] Tinerea apasata repeta schimbarea conform `RepeatButton`.
- [x] Valoarea se opreste la `Minimum`/`Maximum` fara evenimente false suplimentare.
- [x] Un template fara butoane ramane dragabil si page-scrollabil.
- [x] Un track custom devine sursa reala a valorii.
- [x] Track-ul vechi nu mai modifica scrollbar-ul dupa swap.
- [x] `ScrollEventType` corespunde fiecarei interactiuni.

**Gate etapa 3**

- [x] Scrollbar-ul implicit are sageti functionale.
- [x] Template-urile minimaliste fara sageti sunt permise.
- [x] Nu exista sincronizare cu track-uri detasate.
- [x] Evenimentele nu mai eticheteaza page click-ul drept `ThumbTrack`.

### Etapa 4 - ScrollViewer si partile active

- [x] Declara cele trei parti obligatorii ale `ScrollViewer`.
- [x] Creeaza template-ul implicit cu `Grid`, presenter si doua scrollbars.
- [x] Seteaza template-ul implicit prin aceeasi sursa de valoare folosita de controalele tematizate existente.
- [x] Elimina constructia si ownership-ul hardcodat al celor trei copii din constructor.
- [x] Rezolva si memoreaza partile active la aplicarea template-ului.
- [x] Leaga `Content` la presenterul activ si actualizeaza-l la schimbarea continutului.
- [x] Leaga schimbarile offset-ului presenterului activ la sincronizarea scrollbar-urilor.
- [x] Leaga valorile scrollbar-urilor active inapoi la presenter.
- [x] Pastreaza proprietatile publice existente, dar fa-le sa returneze partile active.
- [x] Defineste comportamentul accesarii proprietatilor inainte de measure: constructorul trebuie sa aplice template-ul implicit sau getter-ul trebuie sa apeleze `ApplyTemplate()`.
- [x] Nu returna `null` din proprietatile publice non-null existente.
- [x] Deconecteaza presenterul si scrollbar-urile vechi la template swap.

**Gate etapa 4**

- [x] Viewer-ul nu mai detine copii hardcodati in paralel cu template-ul.
- [x] Content, offset si values sunt sincronizate numai cu partile active.
- [x] API-ul public existent ramane compatibil la nivel de nullability si utilizare.

### Etapa 5 - Convergenta layout-ului prin template root

- [x] Extrage calculul `ShowsScrollBar`, `ReservesSpace` si `ToVisibility` fara duplicare intre measure si arrange.
- [x] Masoara root-ul template-ului, nu cele trei parti ca frati hardcodati ai owner-ului.
- [x] Dupa fiecare measure, recalculeaza nevoia de bare din extent-ul si viewport-ul presenterului activ.
- [x] Repeta cel mult trei treceri si pastreaza fallback-ul conservator cand starea oscileaza.
- [x] In arrange, reevalueaza impotriva dimensiunii finale si rearanjeaza root-ul numai cand vizibilitatea s-a schimbat.
- [x] Pastreaza semantica `Hidden`: scrolling activ, spatiu rezervat, vizual ascuns.
- [x] Pastreaza semantica `Disabled`: scrolling oprit, offset zero, bara collapsed.
- [x] Elimina workaround-urile `ConsumeOwnedScrollBarLayoutWork` numai dupa ce testele demonstreaza ca template root nu lasa lucru tarziu. (Nu a fost eliminat complet: invalidarea de vizibilitate mai programeaza ancestorii, iar consumarea ramasa este limitata la ierarhia partilor active si la measure/arrange deja executat sincron.)
- [x] Daca workaround-urile raman necesare, documenteaza cauza si limiteaza-le la partile active.
- [x] Verifica unbounded measure, continut care creste/scade si interactiunea dintre barele `Auto`.

Teste etapa 5:

- [x] O bara `Auto` poate forta aparitia celeilalte.
- [x] Barele dispar cand continutul se micsoreaza.
- [x] Unbounded measure produce desired size corect.
- [x] Arrange mai mic decat measure reevalueaza barele.
- [x] `Hidden` rezerva spatiu prin layout-ul template-ului.
- [x] `Visible` ramane vizibil fara overflow.
- [x] Cadrul urmator nemodificat raporteaza zero measure/arrange/render work.
- [x] Un template custom cu aceleasi parti pastreaza scrolling-ul functional.

**Gate etapa 5**

- [x] Layout-ul template-ului inlocuieste geometria hardcodata fara regresii.
- [x] Convergenta este limitata si determinista.
- [x] Nu exista invalidari perpetue sau work restant pe frame idle.

### Etapa 6 - Template swap, erori si robustete

- [x] Schimba template-ul `Track` in timpul vietii si verifica thumb-ul nou.
- [x] Schimba template-ul `ScrollBar` si verifica track-ul/butoanele noi.
- [x] Schimba template-ul `ScrollViewer` si verifica presenterul/barele noi.
- [x] Verifica template null si revenirea la template-ul implicit conform politicii stabilite.
- [x] Verifica parti lipsa, duplicate sau cu tip gresit.
- [x] Verifica detasarea intregului viewer in timpul drag-ului sau repetarii.
- [x] Verifica schimbarea continutului in timpul unei repetari de scroll.
- [x] Verifica faptul ca partile vechi nu retin owner-ul prin handleri.
- [x] Verifica reatasarea aceluiasi viewer la alt root.
- [x] Adauga teste cu weak references numai daca testele directe de lifecycle nu pot demonstra absenta retentiei. (Nu a fost necesar: testele directe exercita partile vechi dupa swap/detach si demonstreaza ca owner-ul nu mai este modificat.)

**Gate etapa 6**

- [x] Template swap-ul nu lasa stari sau subscriptions stale.
- [x] Erorile de autor sunt clare si apar la aplicarea template-ului.
- [x] Detach-ul anuleaza drag-ul si repetarea fara activari ulterioare.

### Etapa 7 - Markup, documentatie si verificare finala

- [x] Adauga teste source generator pentru numele `PART_*` in template-uri markup.
- [x] Verifica validarea tipurilor partilor declarate prin `[TemplatePart]`.
- [x] Adauga un sample Playground care arata scrollbar vertical si orizontal cu sageti.
- [x] Verifica vizual glyph-urile, hit targets si orientarea la scale diferite.
- [x] Actualizeaza API docs pentru `Control`, `ScrollViewer`, `ScrollBar`, `Track` si orice tip public nou folosind skill-ul `writing-api-documentation`.
- [x] Corecteaza in aceeasi schimbare sectiunile stale care afirma ca `ScrollBar` sau `ScrollViewer` nu declara evenimente.
- [x] Actualizeaza `docs-site/documentation/manifest.json` pentru pagini noi sau redenumite. (Nu a fost necesar: nu au aparut tipuri publice, pagini API noi sau pagini redenumite.)
- [x] Reindexeaza dupa fiecare modificare de cod sau proiect.
- [x] Ruleaza testele tintite pentru controale, template lifecycle, markup si layout scheduler.
- [x] Ruleaza `dotnet test Cerneala.slnx`.
- [x] Verifica API diff-ul public si compara-l cu contractul acestui plan.

**Gate etapa 7**

- [x] Toata suita este verde.
- [x] Playground demonstreaza sagetile, drag-ul, page click-ul si wheel-ul.
- [x] Documentatia descrie partile active si lifecycle-ul real.
- [x] Nu exista instante private invizibile care continua sa primeasca sincronizare.

## 8. Ordinea recomandata

- [x] Finalizeaza `RepeatButton`.
- [x] Finalizeaza lifecycle-ul comun al template-urilor.
- [x] Migreaza `Track`.
- [x] Migreaza `ScrollBar` si adauga sagetile.
- [x] Migreaza `ScrollViewer`.
- [x] Abia apoi sterge fallback-urile si workaround-urile vechi.

Ordinea este importanta. Daca incepem cu `ScrollViewer` si lasam `ScrollBar` sa sincronizeze track-ul fantoma, obtinem o interfata frumoasa care misca fix pula.

## 9. Definitia de gata

Implementarea este gata cand template-ul implicit afiseaza si opereaza butoanele directionale, track-ul si thumb-ul; toate controalele folosesc partile active ale template-ului; schimbarea template-ului muta complet logica si subscripțiile pe noile parti; politicile de vizibilitate si convergenta raman corecte; iar un frame idle nu pastreaza lucru de layout. `PART_*` trebuie sa fie contracte functionale, nu etichete lipite pe cutii goale.
