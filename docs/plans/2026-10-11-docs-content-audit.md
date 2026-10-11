# Plan: Audit de conținut pentru ghiduri, referințe, pagini API și site

> Data: 2026-10-11
> Status: finalizat
> Baseline: commit `f1dbdcb3` (branch `session/repo-docs-reorganization`).
> Start implementare: commit `f1dbdcb3` (branch `session/repo-docs-reorganization`), 2026-10-11.
> Dependență: [reorganizarea documentației](2026-10-10-repo-docs-reorganization.md) și [documentația de arhitectură](2026-10-10-architecture-docs.md), ambele `finalizat`.
> Scop: fiecare afirmație din `docs/guides/`, `docs/reference/`, `docs-site/documentation/classes/` și paginile HTML din `docs-site/` este verificată față de codul de azi, iar ce e fals sau învechit este corectat.

## 1. Rezumat

Planurile 1 și 2 au mutat documentele, au reparat linkurile și au rescris arhitectura. Conținutul ghidurilor, al referințelor și al celor 1.174 de pagini API a fost corectat doar punctual. Acest plan le verifică pe toate, rând cu rând: părțile structurate (declarații, tabele de membri, exemple de cod) cu scripturi temporare, iar proza cu citire față de sursă. Paginile HTML ale site-ului sunt aduse la zi complet.

Exemplu concret de ce prinde planul: pagina `Cerneala.UI.Input.KeyboardNavigationController.md` descrie o clasă declarată `internal sealed class KeyboardNavigationController` (`UI/Input/KeyboardNavigationController.cs:5`). Utilizatorul nu o poate folosi, deci pagina se șterge, cu tot cu intrarea ei din `manifest.json`.

## 2. Baseline (fapte observate, 2026-10-11)

Sursele: 4 exploratori Haiku și sonda temporară `.artifacts/docs-audit/coverage.csx`, rulată pe `bin/Release/net8.0/Cerneala.dll` (2026-10-10 18:35) și pe ieșirile Debug ale celorlalte 5 ansambluri (2026-10-10, între 17:43 și 19:05). Toate sunt mai noi decât ultimul commit cu `.cs` din cod de producție (`bd7b81b1`, 2026-10-10 15:14).

| Zonă | Mărime | Ce o verifică azi automat |
|---|---|---|
| `docs/guides/` | 10 fișiere, 3.269 de linii, 110 blocuri de cod | `DocumentedTimbreExamplesCompile` compilează doar blocurile Timbre din `timbre-guide.md` și `markup-guide.md` (`tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorTimbreTests.cs:254-293`). |
| `docs/reference/` | 12 fișiere, 8.182 de linii (4.340 generate) | PrismAudit: hash pentru `prism-markup-syntax.md` și `prism-public-api-baseline.md`, marker pentru `prism-filter-reference.generated.md`, egalitate de text pentru `prism-completeness-report.generated.md` (`Tools/PrismAudit/Program.cs:221-238, 276-280, 321-323`). Corpusul Language verifică doar că 4 documente există (`tests/Cerneala.Tests.Language/CorpusCoverageTests.cs:25-37`). |
| `docs-site/documentation/classes/` | 1.174 de pagini `.md`, 80.319 linii | `ApiDocumentationManifestIsValidAndReferencesExistingFiles` verifică doar că fiecare `file` din manifest există și că numele sunt unice (`tests/Cerneala.Tests.VisualStudio/Stage6ReleaseHarnessTests.cs:106-126`). Nimic nu compară paginile cu codul. |
| `docs-site/*.html` + `*.js` | 7 pagini HTML, 4 fișiere JS | Nimic. `pages.yml` publică folderul la push pe `master`, fără verificări. |

**Acoperirea paginilor API pe tipuri** (sonda, potrivire după nume fără aritate generică):

| Ansamblu | Tipuri publice | Fără pagină |
|---|---|---|
| `Cerneala` | 1.060 | 0 |
| `Cerneala.Scene2D.Packages` | 9 | 0 |
| `Cerneala.Scene2D.Importers` | 4 | 0 |
| `Cerneala.SourceGen` | 1 | 0 |
| `Cerneala.Backends.SdlGpu` | 1 | 0 |
| `Cerneala.VisualStudio` | 1 | 0 |

Invers, **70 de pagini descriu tipuri pe care utilizatorul nu le poate folosi**: 67 de tipuri `internal` sau imbricate `private` în `Cerneala.dll`, 2 tipuri imbricate `private` în `UiMarkupGenerator` (`Cerneala.SourceGen/UiMarkupGenerator*.cs`: `private sealed partial class GenerationScope`, `private readonly struct MarkupSource`) și 1 tip care nu mai există (`ResourceDependencyTracker.ReferenceEqualityComparer`). Lista completă e în Anexa A. Sonda nu deosebește un tip generic de unul negeneric cu același nume, deci lista este candidată până la Etapa 2.

Cele 3 pagini `Control.Background`, `Control.BorderBrush`, `Control.Foreground` au tipul „Property” și documentează membri, nu tipuri. Rămân.

**Forma unei pagini API:** `# <Nume> <Tip>`, apoi `## Definition` cu `Namespace:`, `Assembly/Project:`, `Source:` și un bloc `csharp` cu declarația. Urmează tabele `| Name | Type | Description |` sau `| Name | Type | Declared by | Description |` în secțiunile Constructors, Properties, Methods, Events, Fields și „Important Inherited …”. Variații notate de explorator: 32 de pagini au `Source` ca link GitHub, 3 pagini au mai multe fișiere sursă, 6 pagini Detective scriu `Assembly:`, titlurile apar și ca „Applies to”/„Applies To”.

**Contradicții deja găsite în site** (de corectat în Etapa 11):
- `wpf-compendium.html:384` spune că `ComboBox` are „opt-in filtering with fuzzy ranking”, iar `wpf-compendium.js:87` spune „Filtering and autocomplete remain out of scope”.
- `wpf-compendium.html:380` trece `Menu` la „absent”, iar `wpf-compendium.js:92` îl marchează „partial”.
- `benchmarks.html:310`: „Last archive refresh: August 27, 2026”. Pagina nu pomenește niciun rezultat din 2026-09 sau 2026-10, deși există, de exemplu, `benchmarks/results/2026-09-03-servo/` și `benchmarks/Cerneala.Benchmarks/results/2026-10-07-timbre-core-stage3/`.
- Datele „snapshot”: `wpf-compendium.html:66` 7/14/2026, `roadmap.html:48` 8/28/2026, `benchmarks.html:54` 8/27/2026.
- `index.html:800`: cifra „0 B allocated” nu are sursă pe pagină.

## 3. Decizii ale utilizatorului (2026-10-11)

1. **Paginile pentru tipuri ne-publice se șterg**, cu intrarea lor din `manifest.json`.
2. **Uneltele de verificare sunt temporare.** Scripturile stau în `.artifacts/docs-audit/` (ignorat de git). Harness-ul de compilare a exemplelor stă în afara repo-ului. Totul se șterge la final. Nu se adaugă cod sau teste în repo.
3. **Paginile HTML se aduc la zi complet**: se corectează ce e fals și se actualizează conținutul la starea de azi (de exemplu, familiile de benchmark din septembrie și octombrie în `benchmarks.html`).
4. **Paginile API aproape goale se scriu complet** (decizie din 2026-10-11, după Etapa 1). Etapa 1 a găsit 85 de pagini (80 după Etapa 2) cu fraza falsă „This page is generated from the repository API index…” și fără tabele de membri (de exemplu `Cerneala.UI.Data.BindingMode.md` nu listează valorile enum-ului). În lotul lor (Etapele 3-8), fiecare primește conținut real din sursă după skill-ul `writing-api-documentation`: valori sau membri, tipuri, descriere verificată. `members.csx` verifică tabelele noi. Lista: `.artifacts/docs-audit/members-stubs.txt`.

## 4. Obiective

- Fiecare pagină API descrie un tip public care există. Declarația, numele membrilor, tipurile lor și clasa care îi declară se potrivesc cu ansamblul compilat.
- Fiecare bloc `csharp` din secțiunile Examples ale paginilor API, din ghiduri și din referințele scrise de mână compilează pe codul de azi. Blocurile care nu sunt programe întregi (de exemplu, o singură semnătură) sunt marcate în dovezi ca „fragment”, cu motivul.
- Fiecare afirmație în proză care pomenește o valoare, o excepție, o invalidare, o cale sau un comportament este verificată în sursă. Afirmațiile false sunt corectate.
- Documentele generate sunt regenerate cu generatorul lor, nu editate de mână.
- Paginile HTML spun starea de azi, iar fiecare cifră are sursa ei în repo.

## 5. Non-obiective

- Schimbări de cod de producție, chiar dacă auditul găsește defecte. Defectele merg prin skill-ul `github-issue-raise`.
- Teste sau unelte permanente (decizia 2).
- `docs/architecture/` (planul 2), `docs/archive/`, `docs/plans/`, `docs/audits/`.
- Pagini API noi: azi nu lipsește niciuna. Dacă Etapa 1 găsește tipuri publice fără pagină, acestea se raportează și se oprește etapa pentru decizia utilizatorului.
- Rularea din nou a benchmark-urilor. Cifrele din site se copiază din fișierele de rezultat existente, cu calea lor.
- Conținutul personal și politicile mentenantului din `about.html` și `contributors.html` (păreri, glume, politica de modele). Se verifică doar faptele despre repo: numărul de skill-uri, uneltele permise, căile, comenzile.
- Refactorizarea CSS/JS. Selectorul mort `[data-control-filter]` din `roadmap.js:8` se notează ca datorie, nu se repară.
- Capturi de ecran ale site-ului (regula din `CLAUDE.md`).
- `ROADMAP.md`. Dacă `roadmap.html` și `ROADMAP.md` se contrazic după corectarea site-ului, se oprește Etapa 11 și se întreabă utilizatorul.

## 6. Metoda de verificare

**Unelte temporare** (Etapa 1), toate în `.artifacts/docs-audit/`:
- `coverage.csx` (există): tipurile publice din metadatele ansamblului față de numele din manifest. Se extinde ca să potrivească după aritatea generică (`MotionValue_T_` ↔ `MotionValue`1`).
- `members.csx` (nou): pentru fiecare pagină compară titlul (Class/Struct/Enum/Interface/Record/Delegate), declarația din blocul `csharp` (modificatori, tip de bază, interfețe) și fiecare rând din tabelele de membri cu membrii publici și protejați ai tipului, inclusiv cei moșteniți (coloana „Declared by”). Verifică și căile din `Source:` și „See also”.
- Harness-ul de exemple (nou): un proiect consolă temporar în `%TEMP%\cerneala-docs-audit\`, cu referință la `Cerneala.csproj` și la celelalte proiecte documentate. Fiecare bloc `csharp` devine o metodă într-un fișier `.cs` propriu, numit după pagină și numărul blocului. Fiecare eroare de compilare se raportează ca `<pagină>#<bloc>: <eroare>`.

**Proză:** pentru fiecare lot, exploratori Haiku citesc pagina și sursa ei și raportează afirmațiile care nu se potrivesc, cu `fișier:linie` pe ambele părți. Exploratorii nu editează. Parent-ul verifică fiecare afirmație raportată direct în sursă (Roslyn `hover`/`goto_definition` sau citire), apoi corectează pagina.

**Dovezi:** fiecare lot are `.artifacts/docs-audit/<lot>.txt` cu rezultatul uneltelor, afirmațiile verificate și corecturile făcute. Nu se comite.

**Ansambluri proaspete:** înainte de fiecare rulare a uneltelor se compilează `dotnet build Cerneala.slnx -c Debug`. Uneltele citesc doar ieșirile Debug, ca să nu amestece configurații.

## 7. Fișiere estimate

- **Șterse:** cele 70 de pagini din Anexa A (sau lista confirmată în Etapa 2).
- **Editate:** `docs-site/documentation/manifest.json`; pagini din `docs-site/documentation/classes/` (numărul se află din Etapa 1); cele 10 ghiduri; cele 10 referințe scrise de mână din `docs/reference/`; `docs-site/index.html`, `documentation.html`, `wpf-compendium.html`, `wpf-compendium.js`, `roadmap.html`, `benchmarks.html`, `contributors.html`, `about.html`; `docs/plans/README.md`.
- **Regenerate:** `docs/reference/prism-completeness-report.generated.md` (PrismAudit `--write`, dacă se editează un document hash-uit) și `docs/reference/prism-filter-reference.generated.md` (doar dacă generatorul produce alt text).
- **Temporare, șterse la final:** `.artifacts/docs-audit/**`, `%TEMP%\cerneala-docs-audit\`.

## 8. Etape de implementare

Fiecare etapă de conținut: întâi uneltele, apoi exploratorii pentru proză, apoi verificarea parent-ului, apoi editarea, apoi uneltele din nou pe lotul editat.

### Etapa 0 — Baseline

- [x] Confirmă că planurile 1 și 2 au `Status: finalizat` și notează commit-ul de start în acest plan. (Ambele `finalizat`; start la `f1dbdcb3`, notat în antet.)
- [x] `dotnet build Cerneala.slnx -c Debug` trece. (0 erori, 1 min 45 s.)
- [x] `pwsh -NoProfile -File Tools/scripts/Test-MarkdownLinks.ps1 -Baseline .artifacts/docs-reorg/links-baseline.txt` iese cu 0. Notează numărul de fișiere și de linkuri rupte. (Exit 0: 1.505 fișiere, 4 linkuri rupte spre 1 țintă, `docs/plans/2026-10-03-timbre.md`, toate în baseline.)
- [x] `dotnet run --project Tools/PrismAudit -c Release -- --check` trece. Notează numerele afișate. (178 de intrări în catalog, 31 de proprietăți comune, 216 tipuri Prism publice și 8 extinse, zero lipsuri.)
- [x] Testele care citesc documente trec. Notează câte au rulat: (manifest 1/1, Timbre 2/2, corpus 2/2; 0 eșuate.)
  - `dotnet test tests/Cerneala.Tests.VisualStudio/Cerneala.Tests.VisualStudio.csproj -c Release --filter "FullyQualifiedName~ApiDocumentationManifestIsValidAndReferencesExistingFiles"`
  - `dotnet test tests/Cerneala.Tests.SourceGen/Cerneala.Tests.SourceGen.csproj -c Release --filter "FullyQualifiedName~DocumentedTimbreExamplesCompile"`
  - `dotnet test tests/Cerneala.Tests.Language/Cerneala.Tests.Language.csproj -c Release --filter "FullyQualifiedName~CorpusCoverageTests"`

**Gate etapa 0**
- [x] Toate comenzile de mai sus trec pe commit-ul de start, iar numerele sunt notate aici.

### Etapa 1 — Unelte temporare și baseline de constatări

- [x] Extinde `coverage.csx` cu potrivirea după aritate generică. Rulează-l pe cele 6 ansambluri din §2 (ieșirile Debug). Notează: tipuri publice, pagini lipsă, pagini pentru tipuri ne-publice, pagini pentru tipuri inexistente. (1.174 de intrări în manifest; 1.100 de tipuri publice în cele 5 ansambluri încărcate prin reflecție, toate cu pagină: 0 pagini lipsă. 69 de pagini pentru tipuri ne-publice și 1 pentru un tip inexistent (`ResourceDependencyTracker.ReferenceEqualityComparer`): exact cele 70 din Anexa A. 3 pagini de proprietate (`Control.Background`, `Control.BorderBrush`, `Control.Foreground`). `Cerneala.VisualStudio` nu se încarcă în unealta de reflecție; singura lui pagină, `CernealaPackage`, se verifică de mână în Etapa 8.)
- [x] Scrie `members.csx` după §6 și rulează-l pe toate paginile. Notează constatările pe categorii: titlu greșit, declarație greșită, membru inexistent, membru ne-public, tip de membru greșit, „Declared by” greșit, cale `Source`/„See also” inexistentă. (110 constatări pe 100 de pagini; 1.073 de pagini curate. 70 sunt paginile din Anexa A (69 `type-not-public`, 1 `type-not-found`). Celelalte 40: 11 membri ne-publici documentați, 9 titluri „Class” pentru un `record struct`, 5 nullability greșit la evenimente, 4 × 2 declarații cu baza `EventArgs` în loc de `RoutedEventArgs`, 2 tipuri de membru greșite, 2 câmpuri `const` trecute la proprietăți, 2 semnături greșite, 1 cale `Source` inexistentă, 1 membru de interfață explicit inexistent. 0 constatări de „Declared by”. 11 rânduri nu se pot verifica automat (9 „None”, 2 `operator ==`/`!=` fără parametri). Constatare nouă: 85 de pagini conțin fraza falsă „This page is generated from the repository API index…”; nu există niciun generator. Fișiere: `.artifacts/docs-audit/members-findings.tsv`, `members-stubs.txt`.)
- [x] Construiește harness-ul de exemple în `%TEMP%\cerneala-docs-audit\` și compilează toate blocurile `csharp` din secțiunile Examples ale paginilor API, din `docs/guides/` și din cele 10 referințe scrise de mână. Blocul cu declarația din `## Definition` nu se compilează; îl verifică `members.csx`. Notează: blocuri compilate, blocuri cu erori, fragmente. (1.181 de blocuri, compilate în memorie cu Roslyn 5.6 față de ieșirile Debug. API: 903 compilate, 33 cu erori (18 pe pagini din Anexa A, 15 pe pagini care rămân), 208 fragmente de context, 1 fragment-semnătură. Ghiduri: 19 compilate, 0 erori, 10 fragmente de context, 1 fragment-semnătură. Referințe: 5 compilate, 0 erori, 1 fragment de context. „Fragment de context” = singurele erori sunt nume pe care le dă cititorul (de exemplu `root`, `MyOwner`, `Save`), care nu sunt tipuri Cerneala, sau `using`-uri lipsă. Variabilele frecvente (`drawing`, `root`, `servo`, `button` și altele) primesc tipul lor documentat, ca apelurile pe ele să fie verificate. Fișier: `.artifacts/docs-audit/examples-results.tsv`.)
- [x] Verifică uneltele de mână: alege 10 constatări la întâmplare și 5 pagini „curate” la întâmplare și verifică-le în sursă cu Roslyn sau prin citire. O constatare falsă înseamnă că unealta se corectează și se rulează din nou. (Roslyn MCP nu era conectat; verificarea s-a făcut prin citirea fișierelor `Source:`. Primul eșantion: 9 adevărate, 1 falsă (`DefaultAspectTokens.Spacing`: declarația imbricată în clasa exterioară e corectă). Unealta a fost corectată și rulată din nou. Al doilea eșantion, 10 constatări din afara Anexei A: toate adevărate (de exemplu, `ItemsControl.SetVirtualizationContext` e `internal`, `ItemsControl.cs:386`; `TextLine` e `readonly record struct`, `TextLine.cs:3`). 5 pagini curate (`AspectPackage`, `AspectResolutionContext`, `TimeSensitiveRenderInvalidator`, `Servo`, `AspectToken<T>`): membrii și tipurile corespund sursei. Harness: 2 erori verificate (`InputButtonState(bool WasDown, …)`, `TextSelection(int Anchor, …)`, numele de parametri din exemple sunt greșite).)

**Gate etapa 1**
- [x] Cele 3 unelte rulează pe tot corpusul fără să se oprească, iar numerele de baseline sunt notate aici.
- [x] Verificarea de mână nu a găsit constatări false, sau unealta a fost corectată și verificată din nou.
- [x] Dacă `coverage.csx` găsește tipuri publice fără pagină: etapa se oprește și utilizatorul decide (§5). (Nu a fost cazul: 0 tipuri publice fără pagină.)

### Etapa 2 — Ștergerea paginilor pentru tipuri ne-publice

- [x] Pentru fiecare pagină din lista confirmată de `coverage.csx` (pornind de la Anexa A), confirmă cu Roslyn `workspace_symbol` + `hover` că tipul e `internal`/`private` sau nu există, inclusiv în fișiere `partial`. Dacă tipul e public, pagina rămâne și se scoate din listă. (Roslyn MCP nu era conectat; confirmarea vine din metadatele compilate, care includ toate fișierele `partial`: 25 `internal`, 1 imbricat `internal`, 43 imbricate `private`, 1 inexistent. Niciun tip public; lista = Anexa A.)
- [x] Cere unui explorator toate linkurile spre aceste pagini din repo (Markdown, HTML, JS, skill-uri). Fiecare link se scoate sau se înlocuiește cu pagina tipului public care îl folosește. (Niciun link din afara celor 70. Singurul link era între două pagini șterse (`NotAspectCondition` → `AspectConditionNode`). `FileTree.md` (altă sesiune) și `docs/archive/checklists/documentation-checklist.md` (non-obiectiv) doar listează numele de fișiere ca text.)
- [x] Șterge paginile cu `git rm` și scoate intrările lor din `manifest.json`. (70 de fișiere șterse; manifest: 350 de linii scoase, 0 adăugate, 1.174 → 1.104 intrări.)
- [x] Paginile rămase care pomenesc tipurile șterse în tabele sau în proză sunt notate pentru lotul lor (Etapele 3-8), nu se editează aici. (14 pagini, 16 rânduri, notate în `.artifacts/docs-audit/etapa2-mentions.txt`: lot 4 (2), lot 5 (3), lot 7 (8), lot 8 (1).)

**Gate etapa 2**
- [x] `coverage.csx` raportează 0 pagini pentru tipuri ne-publice sau inexistente pe toate cele 6 ansambluri. (`manifest=1104 apiTypes=1100 documented=1100 missingPages=0 nonPublicPages=0 absentPages=0`.)
- [x] `manifest.json` se parsează ca JSON, iar testul `ApiDocumentationManifestIsValidAndReferencesExistingFiles` trece. (1/1.)
- [x] Verificatorul de linkuri iese cu 0 față de baseline. (Exit 0: 1.435 de fișiere, aceleași 4 linkuri rupte din baseline.)

### Etapa 3 — Pagini API: `Cerneala.UI.Controls.*`

- [x] Corectează toate constatările `members.csx` din lot. (Membri `internal` scoși din `ItemsControl`, `ListBox`, `TabControl`, `TabItem`; tipuri de bază `RoutedEventArgs` la `Drag*EventArgs` și `SelectionChangedEventArgs`; tipuri de evenimente; cele 5 pagini goale ale lotului scrise complet: `ButtonKind`, `ButtonSize`, `ISelectableItemContainer`, `IScrollInfo`, `ScrollBarVisibility`. Unealta a primit selecția pe „Declared by” cu aritate și regula `declaration-interface-missing`.)
- [x] Corectează exemplele care nu compilează, folosind doar API public real. (0 erori în lot. `using`-uri lipsă adăugate în 23 de blocuri, `StackPanel` ambiguu reparat în `ItemsControl`, exemplul `TemplatePartAttribute` rescris: cel vechi arunca `ArgumentOutOfRangeException` la rulare. Harness-ul a primit statusul `missing-using` și tratează ca eroare un CS0104 cauzat de `using`-urile blocului.)
- [x] Auditul de proză (§6) pe toate paginile lotului; fiecare afirmație raportată e verificată de parent înainte de editare. (179 de pagini în 8 batch-uri; 69 de pagini corectate, fiecare afirmație verificată în sursă; afirmațiile de comportament nou din exemple verificate la rulare cu `dotnet-csi`.)

**Gate etapa 3**
- [x] `members.csx` și harness-ul raportează 0 constatări deschise pentru lot. (`members.csx`: `findings=0 cleanPages=179 stubPages=0 unparsedRows=0`. Harness: 177 compilate, 25 fragmente de context, 0 erori, 0 `missing-using`. Linkuri: exit 0, doar cele 4 din baseline.)
- [x] Fișierul de dovezi al lotului listează paginile citite și afirmațiile corectate, cu sursa. (`.artifacts/docs-audit/3-UI.Controls.txt`. Defect găsit: `TabControl.SelectedTabItem` ignoră `ItemsSource`, draft în `.artifacts/issue-drafts/2026-10-11-tabcontrol-selectedtabitem-itemssource.md`, neposat.)

### Etapa 4 — Pagini API: `Cerneala.UI.Motion.*` și `Cerneala.UI.Aspect.*`

- [x] Corectează constatările `members.csx` din lot. (2 constatări și 7 rânduri neparsate corectate; cele 21 de pagini goale scrise complet, cu fapte verificate la rulare.)
- [x] Corectează exemplele care nu compilează. (1 eroare și 12 `using` lipsă corectate; în plus 3 exemple care aruncau excepție la rulare.)
- [x] Auditul de proză pe toate paginile lotului. (Loturile 00–06; afirmațiile false corectate cu sursa în fișierul de dovezi.)

**Gate etapa 4**
- [x] `members.csx` și harness-ul raportează 0 constatări deschise pentru lot. (Motion și Aspect: 0 constatări, 0 neparsate, 0 pagini goale; harness 277 compilate, 10 fragmente de context, 0 erori.)
- [x] Fișierul de dovezi al lotului e complet. (`.artifacts/docs-audit/4-Motion-Aspect.txt`. Candidați cu contract neclar, neridicați: `FillMode` fără efect observabil, `PresenceState.Entering` niciodată raportat, `DragMotionController` rămâne `Settling`; candidat de tooling: `CernealaLanguageFacts.cs:88` numește `RetargetBehavior`.)

### Etapa 5 — Pagini API: `Cerneala.UI.Input.*`, `Detective`, `Servo`, `Accessibility`

- [x] Corectează constatările `members.csx` din lot. (3 constatări corectate; cele 17 pagini goale scrise complet: 8 enum-uri Input, 7 interfețe/delegate Input, 2 enum-uri Detective, cu exemple rulate.)
- [x] Corectează exemplele care nu compilează. (3 erori și 3 `using` lipsă corectate.)
- [x] Auditul de proză pe toate paginile lotului. (Loturile 00–06, plus o căutare dedicată pentru afirmația falsă „Handled oprește rutarea”; corecturile au sursa în fișierul de dovezi.)

**Gate etapa 5**
- [x] `members.csx` și harness-ul raportează 0 constatări deschise pentru lot. (Input, Detective, Servo, Accessibility: 0 constatări, 0 neparsate, 0 pagini goale; harness 141 compilate, 43 fragmente de context, 0 erori.)
- [x] Fișierul de dovezi al lotului e complet. (`.artifacts/docs-audit/5-Input-Detective-Servo-Accessibility.txt`. Defect nou, draft neridicat: `.artifacts/issue-drafts/2026-10-11-commandbindings-remove-during-dispatch.md`. De preluat în Etapa 6: `PrismDependencyChange.md` și `PrismRendererOptions.md` repetă afirmația falsă despre `EnableDevelopmentDiagnostics`.)

### Etapa 6 — Pagini API: `Cerneala.Drawing.*` și `Cerneala.UI.Prism.*`

- [x] Corectează constatările `members.csx` din lot. (Inclusiv 288 de constatări de titlu introduse de un script de audit și reparate; final 0 pe `Cerneala.Drawing`, 265 pagini, și 0 pe `Cerneala.UI.Prism`, 40 pagini.)
- [x] Corectează exemplele care nu compilează. (100 compilate, 48 fragmente; singurul cod rămas în fragmente e CS0103, nume de context. Un CS0104 din `DrawTransformScope` reparat.)
- [x] Auditul de proză pe toate paginile lotului. Cele 3 pagini cu mai multe fișiere sursă (`DrawCommand`, `DrawingContext`, `RenderSurface2DFrame`) se verifică pe toate fișierele lor. (Câte 4 fișiere parțiale fiecare, toate citite. Defect nou, draft neridicat: `.artifacts/issue-drafts/2026-10-11-drawpathparser-move-only-close.md`. Candidat fără reproducere la runtime: calea SDL_GPU nu citește resursele ColorMatrix, LensProfile, Lighting și GradientMap.)

**Gate etapa 6**
- [x] `members.csx` și harness-ul raportează 0 constatări deschise pentru lot.
- [x] Fișierul de dovezi al lotului e complet. (`.artifacts/docs-audit/6-Drawing-Prism.txt`.)

### Etapa 7 — Pagini API: restul `Cerneala.UI.*`

Layout, Markup, Text, Resources, Media, Data, Invalidation, Core, Elements, Rendering, Hosting, Platform, Theming, Ink, Timbre, Relay și tipurile direct în `Cerneala.UI`.

- [x] Corectează constatările `members.csx` din lot. (Titluri `record struct`, un membru `internal` scos și altele, prin `rt/lot7-mech.sh`; cele 37 de pagini goale scrise complet, cu exemple rulate în `dotnet-csi`, de exemplu `Invalidate(InvalidationFlags.Measure, …)` → `Measure, Arrange, Render`.)
- [x] Corectează exemplele care nu compilează. (Lot 7 în harness: 0 erori, 0 `using` lipsă.)
- [x] Auditul de proză pe toate paginile lotului. (10 batch-uri; 91 de editări prin `rt/lot7-*.spec`, fiecare afirmație verificată în sursă, unele și la rulare. Două drafturi noi, neposate: `2026-10-11-uifactory-missing-brush-resource-throws.md` și `2026-10-11-renderlayer-default-zero-opacity.md`.)

**Gate etapa 7**
- [x] `members.csx` și harness-ul raportează 0 constatări deschise pentru lot. (`Cerneala.UI`: `findings=0 cleanPages=796 unparsedRows=0 stubPages=0`. Harness: 252 compilate, 52 fragmente de context, 1 fragment-semnătură, 0 erori, 0 `missing-using`. Linkuri: exit 0, doar cele 4 din baseline. `git diff --check` curat.)
- [x] Fișierul de dovezi al lotului e complet. (`.artifacts/docs-audit/7-UI-rest.txt`.)

### Etapa 8 — Pagini API: `Cerneala.Timbre.*`, `Scene2D`, `SourceGen`, `VisualStudio`, `GameBootstrap`

- [x] Corectează constatările `members.csx` din lot. (Lotul avea 0 constatări și 0 pagini goale; `CernealaPackage`, pe care `members.csx` nu o încarcă, a fost verificată de mână și completată.)
- [x] Corectează exemplele care nu compilează. (`TimbreClipDefinition.md#2` compilează acum, verificat în `dotnet-csi`; `UiMarkupGenerator.md#2` era corect și e clasificat ca fragment de context, pentru că folosește o clasă generată.)
- [x] Auditul de proză pe toate paginile lotului. (4 batch-uri; 45 de editări prin `rt/lot8.spec`, `rt/lot8-b00.spec` și 2 editări multi-linie, fiecare afirmație verificată în sursă. Doi candidați de defect, neridicați: `CernealaPackage` cere din MEF un tip neexportat; Ogg tăiat la granița de pagină se termină normal.)

**Gate etapa 8**
- [x] `members.csx` și harness-ul raportează 0 constatări deschise pe **toate** paginile API. (`findings=0 cleanPages=1103 unparsedRows=0 stubPages=0`. Harness: 0 erori, 0 `missing-using` pe tot corpusul. Linkuri: exit 0, doar cele 4 din baseline. `git diff --check` curat. Dovezi: `.artifacts/docs-audit/8-Timbre-Scene2D-rest.txt`.)
- [x] `ApiDocumentationManifestIsValidAndReferencesExistingFiles` trece. (1 trecut, 0 picate.)

### Etapa 9 — Ghiduri (`docs/guides/`, 10 fișiere)

- [x] Fiecare identificator între backticks (tip, membru, cale, comandă, diagnostic) e verificat: simbolurile C# cu Roslyn, căile prin citire, comenzile prin citirea scriptului sau a proiectului. (Roslyn MCP nu era conectat în sesiunile exploratorilor; simbolurile C# au fost verificate prin citirea declarațiilor și, unde contau, prin compilare cu `dotnet-csi` și harness. 7 exploratori + 1 pentru întrebări punctuale.)
- [x] Fiecare bloc `csharp` compilează în harness sau e notat ca fragment, cu motivul. (19 compilate, 10 fragment-context, 1 fragment-signature, 0 erori; motivele sunt în dovezi. Blocurile motion-api au fost compilate separat cu variabile declarate: 2 erori reale corectate.)
- [x] Fiecare bloc `xml` (markup `.crn`) e verificat: elementele, atributele și directivele există în generator. Blocurile care sunt documente `.crn` întregi se compilează prin generator în harness; restul se verifică prin citire, cu sursa notată. (58 de blocuri prin generatorul real: 11 generate (înainte 9), 47 clasificate ca context și verificate prin citire. Probe separate pentru Sprite2D, `.parts.`, TargetType, `@when`, `Canvas.Left`.)
- [x] Afirmațiile în proză sunt auditate după §6. (57 de editări în 9 ghiduri: 51 prin `rt/lot9a.spec`, `rt/lot9b.spec`, `rt/lot9c.spec` și 6 editări multi-linie; 2 candidați de defect, neridicați.)

**Gate etapa 9**
- [x] Fișierul de dovezi listează fiecare ghid, cu identificatorii verificați și corecturile. (`.artifacts/docs-audit/9-guides.txt`. Linkuri: exit 0, doar cele 4 din baseline. `git diff --check` curat.)
- [x] `DocumentedTimbreExamplesCompile` și `CorpusCoverageTests` trec. (SourceGen: 2 trecute, 0 picate. Language: 2 trecute, 0 picate.)

### Etapa 10 — Referințe (`docs/reference/`, 12 fișiere)

- [x] Cele 10 documente scrise de mână sunt auditate ca ghidurile (Etapa 9). (13 exploratori + verificarea parent-ului în sursă; fiecare exemplu `.crn` schimbat trece prin generatorul real (probe `/tmp/crnprobe10*`). Corecturi: `prism-markup-syntax.md` ~45 (`@prism` în Aspect, straturi cu nume obligatorii, `WorkingColorProfile`, ordinea filtrelor, 12 rânduri sincronizate cu catalogul, fără `backdrop`), `motion-markup-syntax.md` 18, `prism-neighborhood-filters.md` ~20 de paragrafe rescrise, `prism-adjustment-filters.md` rescris complet, `markup-data-bindings.md` 6, `wpf-event-coverage.md` 5, `prism-public-api-baseline.md` 3, `developer-preview-scope.md` 2, `prism-catalog-filters.md` 2, `prism-distortion-filters.md` 1. Un defect nou, reprodus: vectorii Prism cu 2-3 componente generează C# care nu compilează (CS1503); draft în `.artifacts/issue-drafts/`, nepostat. Candidați de defect neridicați: D10-1..D10-7.)
- [x] `prism-public-api-baseline.md` e comparat cu tipurile Prism publice din `Cerneala.dll` și cu listele așteptate din `Tools/PrismAudit/Program.cs` (`ExpectedPublicPrismTypes`, `ExpectedHighLevelPrismTypes`, `ExpectedCrossSurfaceTypes`). (PrismAudit: 216 tipuri publice și 8 extinse, zero lipsuri; 3 corecturi de proză despre adaptoarele MonoGame/WindowsDX retrase.)
- [x] `pwsh -NoProfile -File Tools/scripts/New-PrismFilterReference.ps1 -OutputPath <temp>` și compară rezultatul cu `prism-filter-reference.generated.md`. Dacă diferă, regenerează fișierul din repo cu același script și notează de ce diferea. (Identic; nu a fost regenerat.)
- [x] Dacă s-a editat `prism-markup-syntax.md` sau `prism-public-api-baseline.md`: `dotnet run --project Tools/PrismAudit -c Release -- --write`, apoi `--check`. (`--write` a rescris `prism-completeness-report.generated.md`; `--check` trece.)

**Gate etapa 10**
- [x] PrismAudit `--check` trece. (178 de intrări în catalog, 31 de proprietăți comune, 216 tipuri Prism publice și 8 extinse, zero lipsuri. Linkuri: exit 0, doar cele 4 din baseline (1.435 de fișiere). `git diff --check -- docs docs-site` curat. CorpusCoverageTests: 2 trecute, 0 picate.)
- [x] Fișierul de dovezi listează fiecare referință, cu corecturile. (`.artifacts/docs-audit/10-reference.txt`, secțiunile 0-12 și tabelul final pe fișiere.)

### Etapa 11 — Site (`docs-site/*.html`, `wpf-compendium.js`)

- [x] Pentru fiecare pagină, un explorator extrage toate afirmațiile verificabile (liste de controale, stări, cifre, comenzi, căi, versiuni, linkuri spre repo), cu linia lor. Parent-ul le verifică în sursă, în teste sau în fișierele de rezultat. (9 exploratori Haiku: index ×2, documentation, wpf-compendium, roadmap, benchmarks, contributors+about, linkuri. Fiecare nepotrivire raportată a fost recitită în sursă de parent; dovezi în `.artifacts/docs-audit/11-site.txt`.)
- [x] `index.html`: lista de funcționalități, exemplele de cod, comenzile de instalare, starea; cifra „0 B allocated” (`:800`) primește sursa ei sau se scoate. (~78 de afirmații confirmate, 11 corectate: hook-urile Motion (6, din `MotionFramePhase`), a doua trecere din `UiHost`, coalescing-ul Relay, `$Name`, tokenii `FrameDiagnostics`, „No XML in the frame loop”, mock-ul hero refăcut după `MainWindow.crn` și marcat ilustrativ. „0 B” = verificări idle după warmup, cu link la `2026-07-13-queue-engine-2.md`.)
- [x] `documentation.html`: textele statice (de exemplu, numărul de pagini din placeholder) corespund manifestului de după Etapa 2. (1.104 intrări = 1.104 fișiere; „namespaces” → „groups” (58 de grupuri pe folder sursă, 56 de namespace-uri reale); numele de fișier afișat vine din `doc.file`, corect și pentru cele 3 tipuri generice.)
- [x] `wpf-compendium.html` + `wpf-compendium.js`: matricea de 93 de rânduri și listele de controale prezente/absente corespund codului. Contradicțiile ComboBox și Menu din §2 sunt rezolvate după sursă. Data snapshot-ului devine data auditului. (93 de rânduri: 79 confirmate, 11 corectate, 3 doar neverificabile. ComboBox: filtrarea e activă implicit (`IsTextFilterEnabled`=true) cu ranking fuzzy. Menu/MenuBar/MenuItem există și au trecut la „present”; ContextMenu rămâne absent. Data → 10/11/2026.)
- [x] `roadmap.html`: starea fiecărui nod corespunde codului și testelor; data devine data auditului. Dacă rezultatul contrazice `ROADMAP.md`, etapa se oprește și utilizatorul decide (§5). (14/14 stări fără dovezi contrare; corectate: niciun backend implicit în framework, planurile Cerberus V2 nu sunt în repo, hero-ul „primary backend”. După corecturi nu rămâne nicio contradicție de conținut cu `ROADMAP.md`, deci oprirea din §5 nu s-a declanșat. Data → 10/11/2026.)
- [x] `benchmarks.html`: se adaugă familiile de rezultate de după 2026-08-27 din `benchmarks/results/` și `benchmarks/Cerneala.Benchmarks/results/`. Fiecare cifră e copiată din fișierul ei, cu link spre el. „Last archive refresh” devine data celui mai nou rezultat inclus. (25 de familii noi (14-38), 2026-09-02 … 2026-10-07, fiecare cifră recitită din linia ei. „Last archive refresh” → October 7, 2026. Corectat rândul VS p95 (GREEN, fără buget) și eticheta „Latest visual diff”.)
- [x] `contributors.html`: faptele despre repo (numărul de skill-uri, uneltele Roslyn permise, versiunea mcpRoslyn, căile, comenzile) corespund repo-ului. Politicile și părerile rămân neschimbate (§5). (40 de afirmații confirmate; corectate `AGENTS_MD`→`CLAUDE_MD`, pasul `dotnet build` la instalarea Roslyn MCP, lista de unelte (doar config-ul Codex o are în git), breaker „outside Servo and Detective”, „does not merge it unless you ask”.)
- [x] `about.html`: doar afirmațiile despre framework (`:158`, `:190`, `:199`, `:206`). (`:206` „MonoGame developer” → „game developer”; `:158`, `:190`, `:199` confirmate.)
- [x] Linkurile GitHub spre fișiere din repo (`blob/master/...`) trimit la căi care există pe `master` după merge-ul acestui branch. (151 de linkuri în 7 HTML + 4 JS, 0 rupte; cele 41 de linkuri blob/tree din `benchmarks.html` și `index.html` după editare există toate pe `origin/master`.)

**Gate etapa 11**
- [x] Fișierul de dovezi listează, pentru fiecare pagină, fiecare afirmație verificată și sursa ei. (`.artifacts/docs-audit/11-site.txt`, câte o secțiune pe pagină.)
- [x] Nicio pagină HTML nu mai are o contradicție internă sau cu `wpf-compendium.js`. (Verificat după editare: designer, Menu, 3D, precedența și ComboBox sunt aliniate între HTML și JS; etichetele HTML sunt echilibrate față de HEAD; cele 93 de rânduri JS au câte 10 ghilimele fiecare; `git diff --check -- docs docs-site` curat.)
- [ ] **Verificare umană (rămâne deschisă până o face utilizatorul):** paginile se deschid și se afișează corect în browser. Pe acest workstation nu există `node` sau alt verificator JS/HTML, iar regulile interzic capturile de ecran din browser. Agentul nu bifează această căsuță.

### Etapa 12 — Verificare finală și curățenie

- [x] `dotnet build Cerneala.slnx -c Debug`, apoi `coverage.csx`, `members.csx` și harness-ul pe tot corpusul: 0 pagini lipsă, 0 pagini ne-publice, 0 constatări deschise, 0 exemple cu erori (fragmentele notate nu se numără). (Build Debug: 0 erori, 0 warning-uri. `coverage.csx`: `manifest=1104 apiTypes=1100 documented=1100 missingPages=0 nonPublicPages=0 absentPages=0` (3 pagini de proprietate, 1 manuală: `CernealaPackage`). `members.csx`: `findings=0 cleanPages=1103 unparsedRows=0 stubPages=0`. Harness C#: 1.199 de blocuri, 0 erori (API 961 compilate / 201 fragmente de context / 1 semnătură; ghiduri 19 / 10 / 1; referințe 5 / 1). CrnHarness: ghiduri 58 de blocuri, 11 generate, 47 de fragmente de context deja clasificate în Etapa 9; referințe 59 de blocuri, 8 generate, 51 de fragmente, identic cu rularea din Etapa 10.)
- [x] Verificatorul de linkuri iese cu 0 față de baseline. (Exit 0: 1.435 de fișiere, 4 linkuri rupte spre 1 țintă, toate în baseline.)
- [x] PrismAudit `--check` trece. (178 de intrări în catalog, 31 de proprietăți comune, 216 tipuri Prism publice și 8 extinse, zero lipsuri.)
- [x] Cele 3 teste din Etapa 0 trec. (`ApiDocumentationManifestIsValidAndReferencesExistingFiles` 1/1, `DocumentedTimbreExamplesCompile` 2/2, `CorpusCoverageTests` 2/2; 0 picate.)
- [x] `git diff --check` e curat; fișierele noi nu au spații la final de rând. (`git diff --check` curat; singurul fișier nou, acest plan, are 0 rânduri cu spațiu final.)
- [x] Șterge `.artifacts/docs-audit/` și `%TEMP%\cerneala-docs-audit\` după ce numerele finale sunt notate aici. (Șterse după notarea numerelor: `.artifacts/docs-audit/` (1,4 MB) și `%TEMP%\cerneala-docs-audit\` (516 MB), plus directoarele de probă `crnprobe*` și `ex9` din `%TEMP%`. Ciornele de issue din `.artifacts/issue-drafts/` au rămas.)
- [x] Actualizează rândul acestui plan în `docs/plans/README.md`. (`completed`, 82 / 1; nota explică singura căsuță deschisă: verificarea umană din Etapa 11.)

**Gate etapa 12**
- [x] Toate comenzile de mai sus trec, iar numerele finale sunt notate aici.
- [x] Commit-urile acestui plan nu modifică niciun fișier `.cs`, `.csproj`, `.crn` sau de configurare (`git diff --name-only <commit de start>..HEAD`). Fișierele altor sesiuni din worktree nu intră în commit. (Verificat pe setul pus în commit: doar `.md`, `.html`, `.js` și `manifest.json`; cele 5 fișiere ale altei sesiuni (`FileTree.md`, `New-FileTree.ps1` și 3 teste `.cs`) rămân în afara commit-ului.)

## 9. Ordinea recomandată

0 → 1 → 2 → 3 … 8 (în orice ordine după 2, dar unul câte unul) → 9 → 10 → 11 → 12. Etapa 11 vine după 2, pentru că `documentation.html` și `wpf-compendium.js` afișează numărul de pagini din manifest.

Suita completă nu e cerută: se schimbă doar Markdown, HTML, JS și JSON. CI-ul `desktop-backends.yml` rulează oricum regresia completă (`dotnet test ./Cerneala.slnx`, `:415`) la push și la pull request, pentru că filtrele lui de căi (`:17-18`, `:58-59`) includ `docs-site/documentation/classes/**` și `manifest.json`.

## 10. Condiții de oprire

- Un tip public fără pagină (Etapa 1).
- O afirmație din documentație care pare corectă, dar codul face altceva și nu e clar care e intenția: se raportează prin `github-issue-raise` sau se întreabă utilizatorul. Documentul nu se schimbă ca să copieze un bug.
- `roadmap.html` contrazice `ROADMAP.md` (Etapa 11).
- O unealtă temporară care dă constatări false și nu poate fi corectată: se trece la verificare de mână pentru lotul afectat și se notează asta în plan.

## 11. Definiția de gata

- `docs-site/documentation/classes/` conține doar pagini pentru tipuri publice care există. Declarațiile, tabelele de membri și exemplele lor se potrivesc cu codul compilat, iar proza a fost verificată în sursă.
- Ghidurile și referințele nu mai au identificatori, căi, comenzi sau exemple care nu există sau nu compilează.
- Paginile HTML spun starea de azi, fiecare cifră are o sursă în repo, iar data fiecărui snapshot e data auditului.
- Verificatorul de linkuri, PrismAudit `--check` și cele 3 teste care citesc documente trec.
- Uneltele temporare sunt șterse.
- Verificarea vizuală a site-ului e făcută de utilizator sau rămâne marcată explicit ca deschisă.

## Anexa A — Pagini candidate la ștergere (70)

Numele sunt cele din `manifest.json`. Lista e confirmată sau corectată în Etapa 2.

- `Cerneala.Drawing.DrawArgument`
- `Cerneala.SourceGen.UiMarkupGenerator.GenerationScope`
- `Cerneala.SourceGen.UiMarkupGenerator.MarkupSource`
- `Cerneala.UI.Aspect.AllAspectCondition`
- `Cerneala.UI.Aspect.AnyAspectCondition`
- `Cerneala.UI.Aspect.AspectCascadeKey`
- `Cerneala.UI.Aspect.AspectConditionNode`
- `Cerneala.UI.Aspect.AspectEngineElementState`
- `Cerneala.UI.Aspect.DataAspectCondition_TData_`
- `Cerneala.UI.Aspect.DataAspectCondition_TData_TValue_`
- `Cerneala.UI.Aspect.NotAspectCondition`
- `Cerneala.UI.Aspect.PredicateAspectCondition`
- `Cerneala.UI.Aspect.PropertyAspectCondition_TValue_`
- `Cerneala.UI.Aspect.StateAspectCondition`
- `Cerneala.UI.Aspect.VariantAspectCondition`
- `Cerneala.UI.Controls.ContentControl.ContentValueEqualityComparer`
- `Cerneala.UI.Controls.ContentPresenter.ReferenceContentEqualityComparer`
- `Cerneala.UI.Controls.Image.ReferenceImageComparer`
- `Cerneala.UI.Controls.InkCanvas.InkInputAction`
- `Cerneala.UI.Controls.InkCanvas.InkInputKey`
- `Cerneala.UI.Controls.InkCanvas.InkInputKind`
- `Cerneala.UI.Controls.Items.ItemContainerGenerator.ItemContainerInfo`
- `Cerneala.UI.Controls.Primitives.Selector.SelectorClickHandlerRegistration`
- `Cerneala.UI.Controls.Templates.ContentTemplateRegistry.CacheKey`
- `Cerneala.UI.Controls.Templates.ContentTemplateRegistry.RegisteredTemplate`
- `Cerneala.UI.Controls.Templates.TemplateChildOwner`
- `Cerneala.UI.Core.Unset`
- `Cerneala.UI.Core.Unset.UnsetValue`
- `Cerneala.UI.Detective.DirtyTreeDumper.DirtyTraceInfo`
- `Cerneala.UI.Elements.UIRoot.ThemeChangedSubscription`
- `Cerneala.UI.Input.CursorService.CursorBox`
- `Cerneala.UI.Input.DragDropController.DragSession`
- `Cerneala.UI.Input.KeyboardActivationController`
- `Cerneala.UI.Input.KeyboardDispatchKind`
- `Cerneala.UI.Input.KeyboardDispatchResult`
- `Cerneala.UI.Input.KeyboardNavigation.NavigationCandidate`
- `Cerneala.UI.Input.KeyboardNavigationController`
- `Cerneala.UI.Input.ManipulationSnapshot`
- `Cerneala.UI.Input.RetainedInputBindingProcessor`
- `Cerneala.UI.Invalidation.ElementQueueOrderIndex`
- `Cerneala.UI.Invalidation.ElementQueueUnit`
- `Cerneala.UI.Invalidation.ElementWorkQueue_TMetadata_`
- `Cerneala.UI.Layout.Panels.Canvas.CanvasPosition`
- `Cerneala.UI.Layout.Panels.Grid.GridPlacement`
- `Cerneala.UI.Motion.Core.DerivedMotionValue_T_.Subscription`
- `Cerneala.UI.Motion.Core.MotionValue_T_.Subscription`
- `Cerneala.UI.Motion.Core.MotionValue_T_.ValueNode`
- `Cerneala.UI.Motion.Properties.MotionPropertyBinding_T_.BindingNode`
- `Cerneala.UI.Motion.Properties.MotionPropertyStore.PendingWrite`
- `Cerneala.UI.Motion.Properties.MotionPropertyStore.PendingWriteKind`
- `Cerneala.UI.Motion.Specs.DecaySpec_T_.DecaySampler`
- `Cerneala.UI.Motion.Specs.Easings.LinearEasing`
- `Cerneala.UI.Motion.Specs.KeyframesSpec_T_.KeyframesSampler`
- `Cerneala.UI.Motion.Specs.Motion.UntypedSpringSpec`
- `Cerneala.UI.Motion.Specs.Motion.UntypedTweenSpec`
- `Cerneala.UI.Motion.Specs.PingPongSpec_T_.Sampler`
- `Cerneala.UI.Motion.Specs.RepeatSpec_T_.Sampler`
- `Cerneala.UI.Motion.Specs.RepeatSpec_T_.StaticSampler`
- `Cerneala.UI.Motion.Specs.SpringSpec_T_.VectorSpringSampler`
- `Cerneala.UI.Motion.Specs.TweenSpec_T_.TweenSampler`
- `Cerneala.UI.Motion.Transactions.MotionTransactionContext.UntypedMotionSpecAdapter_T_`
- `Cerneala.UI.Rendering.ClipNode.ClipBox`
- `Cerneala.UI.Resources.ResourceDependencyTracker.ReferenceEqualityComparer`
- `Cerneala.UI.Resources.ResourceDependencyTracker.ResourceDependency`
- `Cerneala.UI.Resources.ResourceDependencyTracker.ResourceKey`
- `Cerneala.UI.Resources.ResourceStore.Entry`
- `Cerneala.UI.Resources.ResourceStore.ResourceKey`
- `Cerneala.UI.Text.FontResolver.FallbackDrawFont`
- `Cerneala.UI.Text.TextLineMetrics`
- `Cerneala.UI.Theming.Theme.EntryKey`
