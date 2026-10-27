# Plan: issue-uri bug grupate pe componentă, înainte de fix

> Data: 2026-10-28
> Status: in progres
> Scop: la începutul fazei Fix, watcher-ul strânge issue-urile bug deschise cu aceeași componentă în issue-uri de grup („umbrella”) de cel mult 6 bug-uri, închide originalele ca duplicate și dă fiecărui issue de grup o singură fereastră de fix.

Planul atinge două locuri:

- aplicația watcher `C:\Users\lauri\Desktop\CernealaWatchers` (nu e repository git: backup înainte de editare, ca în `watcher-skill-tuning`);
- skill-urile Cerneala `cerneala-fix-bug-group` (`.claude/skills/` și copia Codex din `.codex/skills/`).

## 1. Rezumat

Acum, fiecare issue primește întâi propriul fix și propriul PR. Abia după aceea watcher-ul grupează PR-urile după fișierele de cod comune. După planul ăsta, gruparea vine **înainte** de fix și se face după **componenta** din titlu:

- `ComboBox: Padding is ignored ...` (#1131) și încă 3 issue-uri `ComboBox: ...` devin un singur issue nou, `ComboBox: 4 grouped bugs (1/1)`.
- Cele 4 originale primesc un comentariu și sunt închise ca duplicate ale lui.
- O singură fereastră de fix repară cele 4 bug-uri, cu un singur PR `Fixes #<umbrella>`.

## 2. Baseline și problema actuală

Fapte observate (2026-10-28):

- `IssueWatcher.RunFixPhaseAsync` (`src/CernealaWatchers.Core/IssueWatcher.cs:121-190`) deschide câte o fereastră `Prompts.IssueFix` pentru fiecare issue al rundei.
- Gruparea există doar după fix: `IssueWatcher.EnterGroupPhaseAsync` (`IssueWatcher.cs:193-213`) cu `PullRequestGroups.Build` (`PullRequestGroups.cs:55-134`), după fișierele de cod ale PR-urilor.
- În runda 7, `watchers.log` (2026-10-27 02:03:01) scrie „9 grupuri, 54 PR-uri rămân singure”. Cele 9 grupuri au înghițit 30 de PR-uri. GitHub arată 30 de PR-uri închise fără merge de la 2026-10-26, adică ~30 de ferestre de fix (~16 min fiecare) refăcute de 9 ferestre de grup.
- **Defect găsit:** `GhCliGitHub.ListOpenBugIssuesAsync` (`GitHub.cs:159-166`) cere `--limit 100`. Sunt 203 issue-uri bug deschise (`gh issue list --label bug --state open --limit 1000`), deci rundele 7 și 8 au avut exact 100 de issue-uri, iar restul nu se văd.
- Singurul apelant al lui `ListOpenBugIssuesAsync` e `IssueWatcher.cs:39` (căutare în tot `src`, inclusiv App). Interogarea țintă `gh issue list ... --author <login> --label bug --json number,title,state,body,closedByPullRequestsReferences --limit 1000` a mers, doar citire, pe 2026-10-28: exit 0 în 3 s, 203 issue-uri, toate cu body, cel mai lung de 10 850 de caractere. Deci 6 corpuri lungi pot depăși 60 000 de caractere, iar tăierea din secțiunea 6 e necesară.
- `Issue` (`GitHub.cs:55-62`) nu are `Body`. `IGitHub` (`GitHub.cs:66-96`) nu poate crea, comenta sau închide un issue.
- `gh 2.98.0` are `gh issue close <n> --duplicate-of <m>` și `--reason duplicate` (`gh issue close --help`).
- Titlurile au un prefix de componentă `<Componentă>: `. Din 203 issue-uri: 68 de prefixe distincte, 26 cu ≥2 issue-uri (161 de issue-uri). Exemple: `Language` 27, `Text` 14, `Markup` 13, `SourceGen` 12, `Motion` 11, `ComboBox` 4.
- Corpul unui issue are formatul #3: `## What happens`, `## Minimal reproduction`, `## Expected behavior`, `## Acceptance criteria`. #1131 are 4312 caractere.
- `cerneala-fix-bug-group/SKILL.md:14-20` (deciziile utilizatorului din 2026-10-18) spune: „No new umbrella issue is created, and no issue is closed by hand” și „Titles that start with the same component name are a hint, not the criterion”.

## 3. Decizii ale utilizatorului (2026-10-28)

Ele înlocuiesc cele două decizii din 2026-10-18 citate mai sus:

- Issue-urile se grupează după **componenta pe care o afectează**, adică prefixul titlului (de exemplu `ComboBox`), nu după fișierele de cod.
- Un issue de grup are **cel mult 6 bug-uri**. Exemplu: `Language`, cu 27 de issue-uri, devine 5 issue-uri de grup (6, 6, 6, 6, 3).
- Originalele se închid **ca duplicate** ale issue-ului de grup, cu un comentariu care spune unde au fost mutate.
- Un issue **singur** pe componenta lui rămâne cum e și primește o fereastră de fix normală.
- Un issue care are un **branch local `worktree-issue-<n>`** nu se grupează și nu se închide. Exemple: #867, cu fix-ul necomis rămas în `fix-slot-1`, și #977, cu un branch gol. E aceeași regulă ca la ferestre: watcher-ul nu deschide fereastră cât branch-ul există (decizie adăugată în timpul implementării).
- **Runda 8:** nu mai pornesc fix-uri noi. Watcher-ul Issues e oprit din 2026-10-28 01:18:05 („[Issues] Info: Oprit.”). Issue-urile fără PR din runda 8 se grupează imediat după livrarea planului.

Presupuneri mecanice (le poate schimba utilizatorul):

- Componenta e textul dinaintea primului `": "` din titlu, fără spații la capete și comparat fără majuscule. Un titlu fără `": "` nu are componentă și rămâne singur. `Prism` și `Prism Motion` sunt componente diferite.
- Bug-urile unei componente se împart în bucăți în ordinea numărului de issue. Exemplu: 27 → #a..#f, #g..#l, ...
- Titlul issue-ului de grup e `<Componentă>: <n> grouped bugs (<k>/<m>)`. Textul de pe GitHub e în engleză, ca restul artefactelor.

## 4. Obiective

- Watcher-ul vede toate issue-urile bug deschise, nu doar 100.
- La începutul fazei Fix, componentele cu ≥2 issue-uri fără PR devin issue-uri de grup de ≤6 bug-uri. Originalele sunt comentate și închise ca duplicate. Lista rundei conține issue-urile de grup și issue-urile singure.
- Un issue de grup primește o fereastră care rulează `cerneala-fix-bug-group <U> --umbrella`. Fiecare bug primește tot tratamentul `cerneala-fix-bug` (reproducere, test RED, cauză, fix, verificare), cu o singură aplicație de reproducere de grup și un singur PR `Fixes #<U>`.
- Pașii sunt reluabili: un crash între „issue de grup creat” și „original închis” se termină la verificarea următoare, fără duplicate noi.
- `--dry-run` doar scrie în log ce ar crea și ce ar închide.

## 5. Non-obiective

- Nu se schimbă faza Group (`PullRequestGroups`, după fișiere): rămâne plasă de siguranță pentru PR-urile care tot ajung să atingă aceleași fișiere.
- Nu se grupează issue-urile care au deja un PR legat.
- Nu se grupează issue-urile de grup între ele și nici într-un issue de grup nou. Un issue de grup rămas deschis (fereastra s-a terminat fără PR) primește în runda următoare iar o fereastră proprie.
- Componenta nu se ghicește din cod și nu se „apropie” componenta unui issue singur de alta (decizia utilizatorului: rămân cum sunt).
- Nu se schimbă review-ul, validarea sau `cerneala-pr-review-merge`.

## 6. Arhitectura propusă

**Cine deține ce:**

- `IssueConsolidation` (nou, static și pur, ca `PullRequestGroups`) decide grupurile: `Component(string title)`, `Build(IReadOnlyList<IssueWithBody> issues)` → `UmbrellaPlan` (componentă, părțile `k/m`, originalele), `ComposeBody(...)` și `ComposeTitle(...)`. Nu face I/O.
- `IGitHub` / `GhCliGitHub` primesc 4 metode noi:
  - `ListOpenBugIssuesWithBodyAsync`: `gh issue list ... --json number,title,state,body,closedByPullRequestsReferences --limit 1000`;
  - `CreateIssueAsync(title, body, label)`: `gh issue create --label bug --body-file`; întoarce numărul;
  - `CommentOnIssueAsync`: `gh issue comment --body-file`;
  - `CloseIssueAsDuplicateAsync(n, of)`: `gh issue close n --duplicate-of of`.
- `WatcherCycle.State` primește:
  - `Consolidated` (bool), cu valoarea `false` după `StartRound`;
  - `Umbrellas` (lista `{Number, Component, Part, Parts, Originals[], Closed[]}`): progresul salvat în `cycle.json`, pentru reluare.
- `IssueWatcher.RunFixPhaseAsync` rulează întâi `ConsolidateAsync` cât timp `Consolidated == false`, apoi alege issue-urile ca acum.

**Fluxul `ConsolidateAsync` (o singură trecere pe verificare):**

1. Reia ce a rămas: pentru fiecare `Umbrella` salvată, fiecare original deschis și neînchis primește comentariul (dacă lipsește) și apoi `close --duplicate-of`.
2. Candidații sunt issue-urile bug deschise care:
   - nu au PR legat;
   - nu sunt lucrate de o fereastră de fix deschisă (`kind=issue-fix number=<n>`);
   - nu sunt deja issue-uri de grup (marcajul din corp).
3. `IssueConsolidation.Build` alege componentele cu ≥2 candidați și le taie în bucăți de ≤6.
4. Pentru fiecare bucată:
   - creează issue-ul de grup;
   - salvează imediat `Umbrella` în `cycle.json`;
   - comentează fiecare original și îl închide ca duplicate.
5. Lista rundei devine issue-urile singure plus issue-urile de grup. Apoi `Consolidated = true`.

**Marcajul și corpul issue-ului de grup:**

- Prima linie e `<!-- cerneala-umbrella component=ComboBox part=1/1 issues=1131,1132,1158,1201 -->`.
- Apoi vine o listă cu link-uri spre originale.
- Apoi, pentru fiecare original, o secțiune `## Bug #1131: <titlu>` cu corpul copiat. Titlurile originalului coboară un nivel: `## What happens` devine `### What happens`.
- Dacă un corp depășește 60 000 de caractere, bucata se taie mai departe până intră. Limita GitHub de 65 536 de caractere nu e verificată aici; Etapa 0 o verifică.

**Comentariul pe original:** `Moved to #<U> (<titlu U>). The reproduction and the acceptance criteria of this bug are copied there; the fix closes #<U>.`

**Runda 8 (migrare):** `cycle.json` din runda 8 nu are `Consolidated`, iar deserializarea dă `false`. Deci prima verificare după livrare grupează candidații de la pasul 2. Asta acoperă cele ~100 de issue-uri care nu se vedeau din cauza limitei de 100. Lista rundei se înlocuiește cu rezultatul.

**Skill-ul:** `cerneala-fix-bug-group` primește un al treilea mod, „Umbrella issue” (args `<U> --umbrella`):

- grupul e lista din marcajul lui `#<U>`;
- fiecare `## Bug #<o>` e un bug cu reproducerea și criteriile lui;
- aplicația de reproducere e `New-ReproApp.ps1 -Issue <o1> -AlsoIssues <o2,...>` (verdict pe numărul original);
- PR-ul are o singură linie `Fixes #<U>`;
- nu se preia și nu se închide niciun PR.

## 7. Fișiere estimate

Watcher (`C:\Users\lauri\Desktop\CernealaWatchers`):

- `src/CernealaWatchers.Core/GitHub.cs`: limita 1000, `IssueWithBody` și cele 4 metode noi.
- `src/CernealaWatchers.Core/IssueConsolidation.cs`: nou.
- `src/CernealaWatchers.Core/WatcherCycle.cs`: `Consolidated`, `Umbrellas`, `ReplaceIssues`, `MarkConsolidated`, `RecordUmbrella`, `MarkOriginalClosed`.
- `src/CernealaWatchers.Core/IssueWatcher.cs`: `ConsolidateAsync` și alegerea prompt-ului pentru un issue de grup.
- `src/CernealaWatchers.Core/Prompts.cs`: `UmbrellaFix` (sau un parametru în `IssueFix`), cu args `<U> --umbrella`.
- `tests/CernealaWatchers.Tests/Fakes.cs`: `FakeGitHub` cu metodele noi, care înregistrează scrierile.
- `tests/CernealaWatchers.Tests/IssueConsolidationTests.cs`: nou.
- `IssueWatcherTests.cs`, `WatcherCycleTests.cs`, `GhCliGitHubTests.cs`.

Cerneala:

- `.claude/skills/cerneala-fix-bug-group/SKILL.md` și `.codex/skills/cerneala-fix-bug-group/SKILL.md`.

## 8. Etape de implementare

### Etapa 0 - Backup, baseline și verificări fără scriere

- [x] Copiază fiecare fișier al watcher-ului din secțiunea 7 în `C:\Users\lauri\.claude\scheduled-tasks\watcher-skill-tuning\backups\<data>\<cale relativă>`, înainte de prima editare. (9 fișiere în `backups\2026-10-28\`, hash egal cu sursa.)
- [x] Rulează `dotnet test tests/CernealaWatchers.Tests/CernealaWatchers.Tests.csproj -c Release --filter "FullyQualifiedName~IssueWatcherTests|FullyQualifiedName~WatcherCycleTests|FullyQualifiedName~GhCliGitHubTests|FullyQualifiedName~PullRequestGroupsTests"` și notează rezultatul ca baseline GREEN. (Cu și `WorkWindowCommandLineTests`: `Passed!  - Failed: 0, Passed: 93, Skipped: 0, Total: 93`.)
- [x] Verifică limita de lungime a corpului unui issue GitHub dintr-o sursă oficială (documentație GitHub) și notează URL-ul. Dacă nu se găsește, păstrează pragul de 60 000 de caractere și scrie în plan că limita e neverificată. (Nicio documentație oficială; eroarea API „Body is too long (maximum is 65536 characters)” e raportată în https://github.com/hashicorp/web-unified-docs/pull/386. Limita e neverificată oficial; pragul e 60 000 de octeți UTF-8, vezi Note de lucru.)
- [x] Verifică fără scriere argumentele `gh`: `gh issue create --help`, `gh issue comment --help` și `gh issue close --help` (`--duplicate-of`). Notează flag-urile exacte. (`gh 2.98.0`: create `-t/--title`, `-F/--body-file`, `-l/--label`, `-R/--repo`; comment `-F/--body-file`; close `--duplicate-of`, `-r/--reason {completed|not planned|duplicate}`, `-c/--comment`.)

**Gate etapa 0**
- [x] Backup-urile există. Testele de baseline au trecut, cu liniile brute notate.
- [x] Flag-urile `gh` folosite în etapele 1–3 sunt confirmate de `--help`.

### Etapa 1 - Watcher-ul vede toate issue-urile

- [x] RED în `GhCliGitHubTests`: `ListOpenBugIssuesAsync` trimite `--limit 1000`. Acum pică, pentru că trimite `100`. (`ListOpenBugIssuesAsync_asks_for_up_to_1000_issues`: RED cu `Actual: ...--limit 100`.)
- [x] Schimbă `--limit 100` în `--limit 1000` în `GitHub.cs:163`. (Constanta `OpenBugIssueLimit = "1000"`.)

**Gate etapa 1**
- [x] Testul nou e GREEN, iar celelalte teste din `GhCliGitHubTests` rămân GREEN. (`Passed!  - Failed: 0, Passed: 5, Total: 5`.)

### Etapa 2 - `IssueConsolidation` (pur)

- [x] RED în `IssueConsolidationTests`:
  - `Component("ComboBox: Padding is ignored ...") == "ComboBox"`;
  - `Component("Prism Motion: ...") == "Prism Motion"`;
  - un titlu fără `": "` nu are componentă;
  - `Component` nu ține cont de majuscule și de spațiile de la capete.
- [x] RED: `Build` cu 27 de issue-uri `Language` dă 5 bucăți (6, 6, 6, 6, 3), în ordinea numărului. Cu 1 issue `Servo Motion` nu dă nimic. Cu 4 issue-uri `ComboBox` dă o bucată `1/1`.
- [x] RED: `ComposeBody`:
  - începe cu marcajul `<!-- cerneala-umbrella component=ComboBox part=1/1 issues=1131,... -->`;
  - are câte o secțiune `## Bug #<n>: <titlu>` pentru fiecare original;
  - coboară `## What happens` la `### What happens`;
  - păstrează textul original octet cu octet, în afară de titluri.
- [x] RED: un corp compus de peste 60 000 de caractere face ca `Build` să taie bucata mai departe. Exemplu: 6 corpuri de 12 000 de caractere dau 2 bucăți de 3.
- [x] RED: `IsUmbrella(body)` e adevărat doar pentru un corp care începe cu marcajul.
- [x] Implementează `IssueConsolidation.cs`.

**Gate etapa 2**
- [x] Toate testele `IssueConsolidationTests` sunt GREEN, iar RED-urile au picat înainte de implementare din motivul așteptat (metodă sau tip lipsă, rezultat greșit), nu din cauza fixture-ului.

### Etapa 3 - `IGitHub`: citire cu body și cele 3 scrieri

- [x] RED în `GhCliGitHubTests`, cu `FakeCommandRunner`, pentru argumentele exacte:
  - `issue list ... --json number,title,state,body,closedByPullRequestsReferences --limit 1000`;
  - `issue create --repo <r> --title <t> --label bug --body-file <f>`, cu numărul citit din URL-ul întors;
  - `issue comment <n> --repo <r> --body-file <f>`;
  - `issue close <n> --repo <r> --duplicate-of <u>`.
- [x] Implementează metodele în `GhCliGitHub`. Fișierele temporare sunt UTF-8 fără BOM și se șterg în `finally`, ca în `CommentOnPullRequestAsync` (`GitHub.cs:202-217`).
- [x] Extinde `FakeGitHub` (`Fakes.cs`) cu issue-uri cu body și cu listele `CreatedIssues`, `IssueComments`, `ClosedAsDuplicate`.

**Gate etapa 3**
- [x] `GhCliGitHubTests` e complet GREEN.

### Etapa 4 - `WatcherCycle`: starea grupării

- [x] RED în `WatcherCycleTests`:
  - `StartRound` lasă `Consolidated == false`;
  - un `cycle.json` vechi, fără `Consolidated`, se citește cu `false`;
  - `RecordUmbrella`, `MarkOriginalClosed`, `ReplaceIssues` și `MarkConsolidated` se salvează și se citesc înapoi după o instanță nouă;
  - în `DryRun` nu se scrie nimic pe disc.
- [x] Implementează câmpurile și metodele.

**Gate etapa 4**
- [x] `WatcherCycleTests` e GREEN, iar testele vechi rămân neschimbate.

### Etapa 5 - `IssueWatcher.ConsolidateAsync`

- [x] RED în `IssueWatcherTests`, cu `FakeGitHub`:
  - 4 issue-uri `ComboBox` fără PR și 1 issue `Servo` produc 1 issue creat, 4 comentarii, 4 închideri cu `--duplicate-of <U>`;
  - lista rundei devine `[Servo, U]`;
  - prima fereastră deschisă e pentru cel mai mic număr din listă.
- [x] RED: un issue `ComboBox` cu PR legat și un issue `ComboBox` cu o fereastră de fix deschisă nu intră în grup. Dacă rămâne un singur candidat `ComboBox`, nu se creează nimic.
- [x] RED: un issue care e deja issue de grup (corp cu marcaj) nu intră într-un grup nou.
- [x] RED: un issue cu branch local `worktree-issue-<n>` nu intră în grup (`FixSlots.ListIssueBranchesAsync`, un singur `git for-each-ref`). Adăugat după decizia utilizatorului din 2026-10-28. (`An_issue_with_a_local_worktree_issue_branch_is_not_grouped`: RED cu `Actual: "ComboBox: 4 grouped bugs (1/1)"`, apoi GREEN.)
- [x] RED: reluare. `FakeGitHub` aruncă excepție la a doua închidere. La verificarea următoare se închid doar originalele rămase, fără un al doilea issue de grup și fără comentarii duble.
- [x] RED: în `DryRun`, nicio scriere pe GitHub. Log-ul conține câte un rând pentru fiecare issue de grup propus, de exemplu `Aș crea „ComboBox: 4 grouped bugs (1/1)” din #1131, #1132, #1158, #1201 și aș închide originalele ca duplicate.`
- [x] RED: migrarea rundei 8. O stare în faza Fix cu `Consolidated` lipsă grupează candidații actuali și înlocuiește `Cycle.Issues`.
- [x] Implementează `ConsolidateAsync` și apelul lui din `RunFixPhaseAsync` (`IssueWatcher.cs:121`), înainte de alegerea issue-ului. Fiecare pas e scris în log, în română, ca restul log-ului.

**Gate etapa 5**
- [x] `IssueWatcherTests` e GREEN, inclusiv testele vechi ale fazelor Fix, Group și Merge.

### Etapa 6 - Fereastra pentru un issue de grup și skill-ul

- [x] RED în `IssueWatcherTests` / `WorkWindowCommandLineTests`:
  - pentru un issue de grup, prompt-ul cere `Invoke the cerneala-fix-bug-group skill with args: <U> --umbrella`, pe branch-ul `worktree-issue-<U>`, cu raportul `Settings.FixReportPath(U)`;
  - nu are `gh pr close` în regulile permise.
- [x] Implementează `Prompts.UmbrellaFix`. Autorizarea e aceeași ca la `IssueFix`, plus „fixes every bug listed in #<U>”. Fără închideri de PR și fără ștergeri de release.
- [x] În `.claude/skills/cerneala-fix-bug-group/SKILL.md`:
  - înlocuiește decizia „No new umbrella issue is created, and no issue is closed by hand” și criteriul „Titles ... are a hint” cu deciziile datate 2026-10-28 din secțiunea 3, păstrând-o pe cea din 2026-10-18 pentru faza Group (PR-uri cu fișiere comune);
  - adaugă modul „Umbrella issue” (args `<U> --umbrella`) cu un exemplu concret (#U cu #1131, #1132), cu aplicația `New-ReproApp.ps1 -Issue 1131 -AlsoIssues 1132,...` și cu `Fixes #<U>`.
- [x] Oglindește aceeași schimbare în `.codex/skills/cerneala-fix-bug-group/SKILL.md`, cu uneltele Codex.
- [x] Verifică că fiecare comandă, cale, parametru de script și nume de skill din textul nou există. De exemplu, `-AlsoIssues` din `New-ReproApp.ps1:31-34` și `Publish-ReproApps.ps1:48-49`.
- [x] Generează în scratchpad o aplicație de grup `New-ReproApp.ps1 -Issue 90011 -AlsoIssues 90012,90013`, construiește-o și rulează check mode (exit 0), ca dovadă că modul skill-ului se sprijină pe un script care merge.

**Gate etapa 6**
- [x] Testele noi și vechi sunt GREEN. Diff-ul skill-ului Claude și al copiei Codex are aceleași reguli. Aplicația de probă a dat `Build succeeded.` și exit 0.
- [x] Schimbarea de skill e pe `origin/master` (commit cu mesaj structurat, push fără force) înainte de deploy-ul watcher-ului: fereastra pornește din `origin/master`. (`e6c27a3c`, push `63bb7f0f..e6c27a3c`.)

### Etapa 7 - Deploy și prima rulare (runda 8)

- [x] Fără nicio fereastră de lucru deschisă (`pwsh` cu `# cerneala-watchers kind=`), oprește watcher-ul, fă build `-c Release` și `-c Debug`, apoi pornește-l cu **`--dry-run`**.
- [x] Arată utilizatorului lista de grupuri din log (componentă, părți, originale) și cere un „da” înainte de rularea reală. Asta e o scriere publică: ~160 de issue-uri închise.
- [x] După „da”: repornește fără `--dry-run`, apasă „Pornește” pe Issues. Verifică pe GitHub, pentru o componentă (de exemplu `ComboBox`), că:
  - issue-ul de grup are marcajul și toate secțiunile;
  - fiecare original are comentariul și starea `CLOSED` cu motivul duplicate (`gh issue view <n> --json state,stateReason`).

**Gate etapa 7**
- [x] `cycle.json` are `Consolidated: true`, iar `Issues` conține issue-urile de grup și issue-urile singure.
- [ ] Prima fereastră pentru un issue de grup a pornit cu `cerneala-fix-bug-group <U> --umbrella`.

## 9. Ordinea recomandată

Etapa 0 → 1 → 2 → 3 → 4 → 5 → 6 (skill-ul ajunge pe `master`) → 7.

Etapele 1–5 nu scriu nimic pe GitHub. Etapa 7 e singura care o face, și doar după „da”.

## 10. Condiții de oprire

- Dacă `gh issue close --duplicate-of` nu setează `stateReason: DUPLICATE` la verificarea din etapa 7, oprește-te după prima componentă și întreabă utilizatorul. Nu închide restul cu alt motiv.
- Dacă prima fereastră pentru un issue de grup nu poate face aplicația de reproducere de grup cu numerele originale, raportează blocajul. Nu redenumi și nu redeschide originalele fără decizia utilizatorului.
- Nu schimba faza Group, review-ul sau `PullRequestGroups` în acest plan.

## 11. Definiția de gata

- Watcher-ul citește până la 1000 de issue-uri bug. Testul pentru `--limit 1000` e GREEN.
- La începutul fazei Fix, componentele cu ≥2 issue-uri fără PR devin issue-uri de grup de ≤6 bug-uri, cu marcaj și cu corpurile originale copiate. Originalele sunt comentate și închise ca duplicate. Totul e reluabil după un crash, iar în `--dry-run` doar se scrie în log.
- Un issue de grup primește o fereastră `cerneala-fix-bug-group <U> --umbrella`. Skill-ul (Claude și Codex) descrie modul cu un exemplu concret și are deciziile datate 2026-10-28.
- Testele focalizate ale `CernealaWatchers.Tests` (`IssueConsolidationTests`, `IssueWatcherTests`, `WatcherCycleTests`, `GhCliGitHubTests`, `PullRequestGroupsTests`, `WorkWindowCommandLineTests`) sunt GREEN. Suita completă nu se rulează.
- Runda 8 e grupată în realitate după „da”-ul utilizatorului, iar prima fereastră de grup rulează.

## Note de lucru

- Etapa 0: GitHub nu documentează oficial limita corpului unui issue. Implementarea măsoară corpul în octeți UTF-8 (cel puțin cât numărul de caractere), cu pragul 60 000, ca să acopere ambele variante: „caractere” și „octeți”.
- Backup-urile watcher-ului sunt în `C:\Users\lauri\.claude\scheduled-tasks\watcher-skill-tuning\backups\2026-10-28\`.
- Etapa 2: RED = `error CS0246: The type or namespace name 'IssueWithBody' could not be found` (tip lipsă, motivul așteptat); GREEN = `Passed!  - Failed: 0, Passed: 13, Total: 13` (`IssueConsolidationTests`). `IssueWithBody` stă lângă `Issue`, în `GitHub.cs`. În marcaj, spațiile din numele componentei devin `_` (`component=Live_Preview`), ca marcajul să rămână `cheie=valoare` fără spații. Tăierea: întâi bucăți de 6 în ordinea numărului (decizia utilizatorului: 27 → 6, 6, 6, 6, 3), apoi doar o bucată prea mare (peste 60 000 de octeți) se înjumătățește (6 corpuri de 12 000 → 3 + 3). Un singur issue prea mare rămâne singur în bucata lui.
- Etapa 3: RED = `error CS1061` pentru cele 4 metode lipsă din `GhCliGitHub`; GREEN = `Passed!  - Failed: 0, Passed: 10, Total: 10` (`GhCliGitHubTests`, inclusiv cele 2 teste vechi `CommentOnPullRequestAsync`, care trec acum prin helper-ul comun `RunWithBodyFileAsync`). `IGitHub` are doar 2 implementări: `GhCliGitHub` și `FakeGitHub` (căutare `: IGitHub` în tot `CernealaWatchers`).
- Etapa 4: RED = `error CS0246` (`Umbrella`) și `CS1061` pentru cele 7 membre lipsă; GREEN = `Passed!  - Failed: 0, Passed: 12, Total: 12` (`WatcherCycleTests`; cele 8 teste vechi neschimbate). În plus față de plan: `IsUmbrella(number)`, folosit de etapa 6 ca să aleagă prompt-ul. Un issue de grup dintr-o rundă veche, încă deschis, se înregistrează în `Umbrellas` cu lista de originale goală (vezi etapa 5).
- Etapa 5: RED = exact cele 6 teste noi `IssueWatcherTests` (`Failed: 6, Passed: 43`); GREEN = `Passed!  - Failed: 0, Passed: 49, Total: 49` (`IssueWatcherTests` + `WatcherCycleTests`). Abatere de la plan: `Umbrella` are și `Commented` (cu `WatcherCycle.MarkOriginalCommented` și testul `A_commented_original_is_saved_so_a_retry_does_not_comment_again`), pentru că fără el o închidere eșuată după un comentariu reușit ar fi postat comentariul de două ori la reluare. Limitare: lista de issue-uri a verificării e citită înainte de grupare, deci fereastra unui issue de grup nou pornește abia la verificarea următoare (issue-urile singure pornesc imediat).
- Etapa 6: RED = `An_umbrella_issue_gets_a_fix_window_that_runs_the_group_skill_in_umbrella_mode` cu „Sub-string not found: Invoke the cerneala-fix-bug-group skill”; GREEN = `Passed!  - Failed: 0, Passed: 172, Total: 172` (filtrele `IssueWatcherTests|WorkWindowCommandLineTests|WatcherCycleTests|IssueConsolidationTests|GhCliGitHubTests|PullRequestGroupsTests|PullRequestWatcherTests|FixSlotsTests`). `Prompts.IssueFix` și `Prompts.UmbrellaFix` folosesc acum un singur `FixRequest` privat; textul prompt-ului pentru issue-uri obișnuite a rămas același. Skill-ul Claude și copia Codex diferă tot prin aceleași 17 rânduri ca înainte (`git diff --no-index --stat`), deci adăugările sunt identice. Aplicația de probă: `Build succeeded.` (128 s, la rece) și `check exit: 0: OK: bugs #90011, #90012, #90013 do not reproduce so far.`
- Greșeală de proces: `FixSlots.cs` a fost editat fără backup înainte. Backup-ul a fost refăcut după aceea, scoțând exact metoda nouă `ListIssueBranchesAsync` (152 de rânduri, cât avea originalul).
- Etapa 7: `--dry-run` scrie log-ul doar în fereastra aplicației (`MainViewModel.cs:32`), nu în `watchers.log`; lista a fost citită dintr-o captură făcută cu `--screenshot` (API-ul aplicației) și, completă, cu un `dotnet-csi` care apelează `IssueConsolidation.Build` din DLL-ul construit, pe datele live (doar citire). Prima listă avea `Collision: 1 grouped bugs (2/2)` și `Markup: 1 grouped bugs (3/3)`: regula nouă „niciun issue de grup cu un singur bug” (testul `Build_never_makes_an_umbrella_of_one_bug_and_leaves_the_seventh_issue_alone`, RED cu „The collection contained 2 items”, apoi `Passed: 173, Total: 173`). Lista finală: 203 deschise, 188 candidați, 34 de issue-uri de grup, 142 de bug-uri grupate, 46 singure; #867–#880 și #977 rămân negrupate (branch local). După „da”-ul utilizatorului: 34 create (#1221–#1254), 142 închise, `cycle.json` cu `Consolidated: true`, 95 de issue-uri în rundă. Exemplu verificat: #1223 `ComboBox: 4 grouped bugs (1/1)`, label `bug`, marcaj, 4 secțiuni; #776, #840, #841, #1131 `CLOSED DUPLICATE` cu comentariul „Moved to #1223 ...”.
- Gate deschis: ferestrele pornesc în ordinea numărului, iar issue-urile de grup au numere noi (#1221–#1254), deci prima fereastră pentru un issue de grup pornește după issue-urile singure ale rundei (prima fereastră a fost #768, la 01:57:43). Prompt-ul `--umbrella` e acoperit de testul `An_umbrella_issue_gets_a_fix_window_that_runs_the_group_skill_in_umbrella_mode`; rularea reală e încă de observat.
- Watcher-ul nu e repository git: munca din etapele 1–5 și din etapa 6 (partea de watcher) nu intră în commit-ul Cerneala; în commit intră doar planul și skill-ul.
