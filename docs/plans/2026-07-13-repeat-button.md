# Plan: RepeatButton

> Data: 2026-07-13
> Status: finalizat
> Scop: introducerea unui buton care activeaza imediat la apasare si repeta activarea la intervale determinate cat timp ramane apasat

## 1. Rezumat

`RepeatButton` va extinde comportamentul existent de buton fara sa introduca thread-uri, `Task.Delay`, timere globale sau comenzi executate direct din control. Timpul de repetare va veni din frame-ul host-ului, iar rutarea evenimentului `Click` si executia comenzilor vor folosi aceleasi contracte ca restul inputului Cerneala.

Prima activare are loc la apasarea butonului stang. Dupa `Delay`, controlul produce cel mult o activare pe frame la fiecare `Interval`. Repetarea se opreste la release, anularea starii pressed, detach, disable, pierderea rutei sau schimbarea root-ului.

Presupunere de proiectare: `RepeatButton` va deriva din `Button`, nu direct din `ButtonBase`, pentru a reutiliza compozitia de continut si fallback-ul vizual actual. Daca inaintea implementarii apare un template implicit comun la nivel de `ButtonBase`, aceasta decizie trebuie reevaluata, nu copiata orbeste ca o reteta de sarmale.

## 2. Obiective

- [x] Exista `Cerneala.UI.Controls.Primitives.RepeatButton` ca API public.
- [x] `RepeatButton` expune `Delay` si `Interval` ca `UiProperty<int>` exprimate in milisecunde.
- [x] `Delay` accepta valori finite intregi mai mari sau egale cu zero.
- [x] `Interval` accepta numai valori intregi strict pozitive.
- [x] Apasarea valida produce imediat un singur `Click` si o singura executie de comanda.
- [x] Prima repetare are loc numai dupa expirarea `Delay`.
- [x] Repetarile urmatoare respecta `Interval` si produc cel mult o activare pe frame.
- [x] Un frame intarziat nu produce o rafala necontrolata de activari restante.
- [x] Release-ul nu mai produce inca un click pentru `RepeatButton`.
- [x] `Button`, `ToggleButton`, activarea din tastatura si comenzile existente isi pastreaza comportamentul.
- [x] Implementarea este determinista in teste prin `TimeSpan` furnizat explicit host-ului.

## 3. Non-obiective

- [x] Nu introducem un scheduler general de timere UI.
- [x] Nu folosim `System.Threading.Timer`, `DispatcherTimer`, `Task.Delay` sau lucru pe thread secundar.
- [x] Nu adaugam accelerare progresiva a repetarii.
- [x] Nu adaugam proprietati WPF care nu sunt necesare acestui slice.
- [x] Nu modificam inca template-urile `ScrollBar`; acestea sunt tratate de planul dependent pentru partile de scrolling.
- [x] Nu schimbam semantica generala a `Click` pentru butoanele obisnuite.

## 4. Contract propus

API public estimat:

```csharp
public class RepeatButton : Button
{
    public static readonly UiProperty<int> DelayProperty;
    public static readonly UiProperty<int> IntervalProperty;

    public int Delay { get; set; }
    public int Interval { get; set; }
}
```

Valori implicite propuse:

- `Delay = 500` ms;
- `Interval = 100` ms.

Contract intern propus:

```text
UiHost frameTime
    -> ElementInputBridge.Dispatch(..., frameTime)
        -> RepeatButtonController
            -> IInputActivatable.Activate()
            -> IInputCommandSource.ExecuteCommand(...)
```

`RepeatButtonController` trebuie sa detina sesiunea temporara de repetare. Controlul expune configuratia si identitatea de input, dar nu primeste dependinte globale precum `CommandRouter`.

## 5. Fisiere estimate

Fisiere noi:

- `UI/Controls/Primitives/RepeatButton.cs`;
- `UI/Input/RepeatButtonController.cs`;
- `UI/Input/IInputRepeatSource.cs`, numai daca marker-ul intern simplifica integrarea fara verificari concrete de tip;
- `tests/Cerneala.Tests/Controls/Primitives/RepeatButtonTests.cs`;
- `docs-site/documentation/classes/Cerneala.UI.Controls.Primitives.RepeatButton.md`.

Fisiere modificate probabil:

- `UI/Controls/Primitives/ButtonBase.cs`;
- `UI/Input/ElementInputBridge.cs`;
- `UI/Hosting/UiHost.cs`;
- `UI/Hosting/MonoGame/MonoGameUiHost.cs`, numai daca semnatura wrapper-ului o cere;
- `tests/Cerneala.Tests/UI/Hosting/UiHostTests.cs`;
- `tests/Cerneala.Tests/UI/Input/ElementInputBridgeTests.cs`;
- `docs-site/documentation/classes/Cerneala.UI.Controls.Primitives.ButtonBase.md` daca apare un hook protected nou;
- `docs-site/documentation/manifest.json`.

## 6. Etape de implementare

### Etapa 0 - Baseline si teste de caracterizare

- [x] Genereaza `FileTree.md` si verifica indexul RoslynIndexer.
- [x] Ruleaza testele existente pentru `Button`, `ButtonBase`, activare din tastatura, comenzi si `ElementInputBridge`.
- [x] Adauga teste de caracterizare pentru click-ul normal pe release al unui `Button`.
- [x] Adauga teste de caracterizare pentru executia unei comenzi exact o data la click.
- [x] Adauga teste de caracterizare pentru anularea starii pressed la release si detach.
- [x] Noteaza explicit ca aceste teste trebuie sa ramana neschimbate dupa introducerea repetarii. (Testele de caracterizare din `ElementInputBridgeTests` sunt contract de regresie si nu se modifica pentru a acomoda `RepeatButton`.)

**Gate etapa 0**

- [x] Baseline-ul este verde.
- [x] Contractul butoanelor normale este acoperit inainte de schimbarea inputului.
- [x] Nicio modificare functionala nu a fost introdusa.

### Etapa 1 - API-ul RepeatButton

- [x] Creeaza `RepeatButton` in namespace-ul `Cerneala.UI.Controls.Primitives`.
- [x] Deriva temporar din `Button` conform presupunerii documentate.
- [x] Inregistreaza `DelayProperty` cu valoarea implicita `500` si validare `>= 0`.
- [x] Inregistreaza `IntervalProperty` cu valoarea implicita `100` si validare `> 0`.
- [x] Expune proprietatile CLR `Delay` si `Interval`.
- [x] Marcheaza proprietatile cu constrangerile de markup disponibile sau extinde validarea de markup numai daca este necesar. (Nu a fost necesara extinderea: constrangerile existente acopera proprietatile, iar validarea `UiProperty` ramane autoritara.)
- [x] Adauga teste pentru valori implicite, valori valide si respingerea valorilor invalide.
- [x] Verifica generarea/parsarea proprietatilor din markup cu valori intregi.

**Gate etapa 1**

- [x] API-ul compileaza si validarea este acoperita.
- [x] Controlul nu porneste singur thread-uri sau timere.
- [x] Testele existente pentru butoane raman verzi.

### Etapa 2 - Un singur drum de activare

- [x] Extrage in `ButtonBase` un hook protected minimal care decide daca mouse-up produce click.
- [x] Pastreaza valoarea implicita a hook-ului astfel incat `Button` si `ToggleButton` sa continue sa activeze pe release.
- [x] Suprascrie hook-ul in `RepeatButton` pentru a evita click-ul suplimentar la release.
- [x] Pastreaza `IInputActivatable.Activate()` ca drum unic pentru ridicarea evenimentului `Click`.
- [x] Nu executa comanda direct din `RepeatButton`.
- [x] Adauga teste care demonstreaza ca activarea programatica ridica un singur `Click`.
- [x] Adauga teste care demonstreaza ca release-ul unui `RepeatButton` nu ridica un click suplimentar.

**Gate etapa 2**

- [x] Exista o separare clara intre ridicarea `Click` si executia comenzii.
- [x] Butoanele normale isi pastreaza semantica.
- [x] `RepeatButton` nu dubleaza activarea la release.

### Etapa 3 - Timpul de input determinist

- [x] Adauga un overload `ElementInputBridge.Dispatch(UIRoot, InputFrame, TimeSpan frameTime)`.
- [x] Pastreaza overload-ul existent pentru compatibilitate si delega explicit cu o valoare neutra documentata. (`TimeSpan.Zero`.)
- [x] Modifica `UiHost.UpdateCore` sa transmita acelasi `frameTime` folosit de frame catre input bridge.
- [x] Nu reutiliza `ITimeSensitiveRenderElement`; repetarea este comportament de input, nu invalidare de render cu mustata falsa.
- [x] Stabileste daca `frameTime` reprezinta timestamp absolut sau delta si pastreaza aceeasi semantica in host, controller si teste. (`frameTime` este delta scursa in frame-ul curent.)
- [x] Adauga teste host care confirma propagarea timpului furnizat explicit.
- [x] Verifica wrapper-ele MonoGame si Windows pentru semnaturi sau cai alternative de update. (Nu au fost necesare modificari: ambele cai ajung deja in `UiHost.UpdateCore` cu acelasi delta.)

**Gate etapa 3**

- [x] Inputul primeste timp determinist fara acces la ceas global.
- [x] Nicio cale de host nu avanseaza timpul de doua ori.
- [x] Testele fara timp explicit raman compatibile.

### Etapa 4 - RepeatButtonController

- [x] Creeaza un controller intern detinut de `ElementInputBridge`.
- [x] La mouse-down valid, rezolva cel mai apropiat repeat source din ruta vizuala.
- [x] Porneste o singura sesiune pentru butonul stang si memoreaza sursa, root-ul si urmatorul termen.
- [x] Produce imediat activarea initiala prin `IInputActivatable`.
- [x] Executa comanda initiala prin `IInputCommandSource` si `CommandRouter`.
- [x] Dupa `Delay`, produce cel mult o activare pe frame.
- [x] Dupa prima repetare, programeaza urmatorul termen folosind `Interval`.
- [x] La un frame foarte intarziat, sare peste intervalele vechi fara bucla de catch-up.
- [x] Opreste sesiunea la mouse-up, detach, disable, hidden/collapsed, schimbare de root sau ruta invalida.
- [x] Opreste sesiunea daca sursa nu mai este pressable ori command source valid.
- [x] Decide si testeaza explicit comportamentul cand pointerul paraseste butonul cat timp butonul ramane apasat. (Sesiunea este anulata.)
- [x] Pentru MVP, prefera anularea repetarii la iesirea din hit target in locul unei capturi implicite noi.
- [x] Evita referinte stale dupa anulare.

**Gate etapa 4**

- [x] Secventa click/command este `1 initial + N repetari`, fara click final accidental.
- [x] Nu exista mai mult de o activare pe frame.
- [x] Controllerul nu retine controale detasate.
- [x] Comenzile routed si comenzile simple folosesc acelasi drum existent.

### Etapa 5 - Interactiuni si cazuri limita

- [x] Testeaza `Delay = 0` fara dublarea activarii initiale.
- [x] Testeaza schimbarea `Delay` in timpul unei sesiuni si fixeaza regula: afecteaza numai sesiunea urmatoare.
- [x] Testeaza schimbarea `Interval` in timpul unei sesiuni si fixeaza regula: se aplica urmatorului termen calculat.
- [x] Testeaza un frame care sare peste mai multe intervale.
- [x] Testeaza release exact la termenul unei repetari; release-ul castiga si nu mai produce repeat.
- [x] Testeaza disable si detach intre doua frame-uri.
- [x] Testeaza comanda care devine `CanExecute == false` in timpul repetarii.
- [x] Testeaza handler `Click` care modifica arborele sau elimina butonul.
- [x] Testeaza doua `RepeatButton` apasate succesiv; sesiunea veche trebuie anulata.
- [x] Testeaza ca butonul drept si wheel-ul nu pornesc repetarea.
- [x] Testeaza activarea din tastatura si documenteaza ca MVP-ul repeta numai pointerul, daca nu este implementata repetarea pentru Space. (Space activeaza o singura data la release; repetarea MVP este numai pentru pointerul stang.)

**Gate etapa 5**

- [x] Cazurile limita au rezultate deterministe.
- [x] Nu exista activari dupa release sau detach.
- [x] Nu exista diferente neintentionate pentru `Button` si `ToggleButton`.

### Etapa 6 - Integrare, documentatie si verificare

- [x] Inregistreaza `RepeatButton` in schema/factory-ul de markup daca tipurile nu sunt descoperite automat.
- [x] Adauga un exemplu minimal in Playground numai daca exista deja o suprafata potrivita pentru controale primitive. (Nu a fost necesar: Playground-ul curent este un shell de navigare, fara o suprafata de showcase pentru controale primitive.)
- [x] Creeaza documentatia API in `docs-site/documentation/classes/` folosind skill-ul `writing-api-documentation`.
- [x] Actualizeaza `docs-site/documentation/manifest.json` pentru pagina noua.
- [x] Actualizeaza documentatia `ButtonBase` daca hook-ul protected devine API public/protected.
- [x] Reindexeaza dupa fiecare modificare de cod sau proiect.
- [x] Ruleaza testele tintite pentru input, butoane, comenzi si host.
- [x] Ruleaza `dotnet test Cerneala.slnx`.
- [x] Verifica diff-ul API public si confirma ca include numai suprafata intentionata. (`RepeatButton`, proprietatile sale, hook-ul protected din `ButtonBase` si overload-ul temporizat din `ElementInputBridge`; mecanismul de repetare ramane intern.)

**Gate etapa 6**

- [x] Toata suita este verde.
- [x] Documentatia publica este sincronizata.
- [x] RepeatButton poate fi folosit din C# si markup.
- [x] Planul dependent pentru scrollbar poate consuma controlul fara workaround-uri.

## 7. Definitia de gata

`RepeatButton` este gata cand o apasare valida produce imediat un click si o executie de comanda, repetarea incepe numai dupa `Delay`, continua cel mult o data pe frame conform `Interval`, se opreste sigur in toate caile de anulare si nu schimba comportamentul butoanelor obisnuite. Niciun timer ascuns nu trebuie sa ramana sa bata in pereti dupa ce controlul a disparut.
