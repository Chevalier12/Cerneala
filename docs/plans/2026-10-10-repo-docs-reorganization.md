# Plan: Reorganizarea documentației din repo (mutare, unificare, ștergere, corecturi)

> Data: 2026-10-10
> Status: finalizat
> Baseline: commit `def7f419` (master).
> Plan dependent: [documentele de arhitectură](2026-10-10-architecture-docs.md) — începe după Etapa 5 din acest plan.
> Scop: fiecare document are un singur loc previzibil după tipul lui (ghid, referință, arhitectură, plan, audit, arhivă), nimic nu mai stă aruncat în rădăcină, iar documentele vii nu mai conțin fapte false despre cod.

## 1. Rezumat

Utilizatorul a cerut toate cele patru operații: **mutare**, **unificare**, **ștergere**, **documentație nouă de arhitectură**. Acest plan acoperă primele trei, plus corecturile de conținut dovedit greșit. Documentația nouă de arhitectură este în planul dependent.

Regula de bază: **un document nu se mută până nu sunt actualizate toate locurile care îl citesc după cale** (teste, `Tools/PrismAudit`, CI, linkuri). Fiecare mutare este verificată de un verificator de linkuri care nu există încă în repo și se scrie în Etapa 0.

## 2. Decizii (2026-10-10)

Utilizatorul a delegat deciziile de organizare („Pai, daca stiam eu te mai intrebam?”, apoi „Concret inseamna toate cele 4”). Deciziile de mai jos sunt ale agentului, fiecare cu motiv din inventar.

| Decizie | Valoare | Motiv |
|---|---|---|
| Organizare `docs/` | După tipul documentului: `guides/`, `reference/`, `architecture/`, `plans/`, `audits/`, `archive/`, `assets/`. | Haosul raportat amestecă tipuri (ghid lângă propunere, lângă raport generat). |
| Rădăcina repo-ului | Rămân doar `README.md`, `CLAUDE.md`, `ROADMAP.md`, `LICENSE`, `FileTree.md` (generat) și fișierele de build/config. | `ROADMAP.md` este legat din `docs-site/roadmap.html:278` prin URL GitHub; mutarea lui ar rupe site-ul. |
| `docs/plans/` | Planurile **rămân pe loc**, inclusiv cele finalizate; se adaugă `docs/plans/README.md` cu starea reală a fiecărui plan. | Planurile leagă relativ `evidence/` (1.873 fișiere urmărite); mutarea a 53 de planuri ar rescrie sute de linkuri fără câștig. Data din nume le ordonează deja. |
| `docs/plans/evidence/` | Neatins. | 277 MB urmăriți; scoaterea din arbore nu micșorează istoricul, iar rescrierea istoricului nu este autorizată. |
| Numire | Fișiere noi/mutate: litere mici, cuvinte cu `-`; documentele datate încep cu `AAAA-LL-ZZ-`. | Azi coexistă `Prism_Audit_09022026.md` (LLZZAAAA), `prism-visual-algorithm-checklist-2026-07-25.md` (sufix) și `2026-08-25-…` (prefix). |
| Ce se șterge | Doar copii dovedit moarte: `claude-skills-import/`, `AGENTS_DEPRECATED.md`, 2 pagini API pentru tipuri inexistente (după verificare Roslyn). | Ștergerile prin git sunt recuperabile din istoric. |
| Ce se arhivează | Documente istorice sau înlocuite: rămân citibile în `docs/archive/`, cu un banner de o linie la început. | Păstrează contextul fără să-l prezinte ca stare actuală. |
| Linkuri rupte deja în documente arhivate | Nu se repară. | Arhiva este istorie; doar documentele vii trebuie să fie corecte. |
| Fișiere locale ignorate de git (10+ GB) | Etapă separată, executată doar după confirmarea explicită a utilizatorului pentru lista exactă. | Nu sunt în git, deci ștergerea nu se poate recupera. |

## 3. Baseline (fapte observate)

Sursele: inventarele exploratorilor din 2026-10-10 și citire directă a fișierelor numite.

**Rădăcina** are 17 fișiere `.md` în afară de `CLAUDE.md`:
- curente: `README.md`, `architecture.md`, `ROADMAP.md` (link rupt spre `docs/plans/2026-08-27-cerberus-v2.md`, liniile 165 și 254);
- istorice: `Aspect_Audit_09022026.md`, `Motion_Audit_09022026.md`, `Prism_Audit_09022026.md`, `Relay_Audit_09022026.md` (toate cu „snapshot 2026-09-02”), `ROADMAPv2.md` (42 de linii MonoGame; `ROADMAP.md:256-258` îl declară istoric), `ROADMAPv2_AUDIT.md`, `AUDIT_FIX_PLAN.md`, `ClassChecklist.md` (snapshot 2026-08-18);
- checklist-uri terminate (0 `[ ]`): `AspectChecklist.md`, `MotionChecklist.md`, `PrismChecklist.md`, `DOCUMENTATION_CHECKLIST.md` (17 bife spre pagini inexistente);
- premisă falsă: `ConceptualIdeas.md` („framework for MonoGame”);
- dublură: `AGENTS_DEPRECATED.md` (copie Codex a principiilor din `CLAUDE.md`; trimite la `docs/documentation/`, interzis).

**`docs/`** are 29 de fișiere de nivel 1 cu tipuri amestecate. Cele două `*-proposal.md` se declară implementate. `architecture-v2.md` conține afirmații false (ex. `SdlGpuApplicationBackend.UseSdlGpu` nu există; ordinea de precedență are 6 surse, `UiPropertyStore` are 9). `docs/superpowers/` are 51 de planuri din iulie, făcute cu un plugin abandonat; nicio referință din cod.

**Consumatori după cale** (citit direct):
- `Tools/PrismAudit/Program.cs:190-193` cere existența a 4 documente `docs/prism-*`. `:221` scrie `docs/prism-completeness-report.generated.md`. `:276` citește referința de filtre și verifică hash-ul catalogului. `:282-295` cere tokenuri în propunere și în designul tehnic. `:321-323` pune hash-urile a 3 documente în raport, iar `--check` (CI, `desktop-backends.yml:408`) compară raportul byte cu byte.
- `Tools/scripts/New-PrismFilterReference.ps1:15-18, 148-156` scrie `docs/prism-filter-reference.generated.md`.
- `.github/workflows/desktop-backends.yml:15-16, 56-57` are filtrele de cale `docs/prism-*.md` și `docs/sdl-desktop-backend.md`.
- `tests/Cerneala.Tests/Drawing/Prism/PrismVisualStyleSourcePairTests.cs:12-16` citește `docs/audits/prism-visual-style-algorithm-checklist-2026-08-02.md`.
- `tests/Cerneala.Tests.Language/Corpus/repository-documents.txt:19-22` este verificat de `CorpusCoverageTests.cs:28-36` și listează `docs/CernealaMarkupGuide.md`, `docs/markup-data-bindings.md`, `docs/motion-markup-syntax-proposal.md`, `docs/prism-markup-syntax-proposal.md`.
- `tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorTimbreTests.cs:255-259, 295-305` compilează exemplele din `docs/timbre-guide.md` și `docs/CernealaMarkupGuide.md` (rădăcina este `docs/`).
- `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json:485, 501, 519, 533` conține ancore `prism-markup-syntax-proposal.md#…`. Compilatorul verifică doar că șirul nu e gol (`PrismCatalogCompiler.cs:478-503`); nicio ancoră nu corespunde unui titlu. Fluxul mai departe al șirului este necunoscut (vezi Etapa 5).
- `tests/Cerneala.Tests.VisualStudio/Stage6ReleaseHarnessTests.cs:110-124` cere ca fiecare `file` din `docs-site/documentation/manifest.json` să existe.

**Verificator de linkuri:** nu există. Afirmația „0 broken links” din `DOCUMENTATION_CHECKLIST.md:10` nu numește nicio comandă. Convenția testelor de script este pwsh simplu cu `Assert-True` (`Tools/scripts/Archive-Repo.Tests.ps1`).

**Skill-uri:** `writing-api-documentation/SKILL.md` spune la liniile 3, 10, 20, 26, 44 să scrii în `docs/documentation/`, în toate cele 3 copii (`.claude/`, `.codex/`, `claude-skills-import/`); la fel `.codex/skills/writing-api-documentation/agents/openai.yaml:4`. Asta contrazice `CLAUDE.md:155` și `.codex/config.toml:103`.

**Lucru străin în arbore:** o altă sesiune are modificări necomise în `FileTree.md`, `Tools/scripts/New-FileTree.ps1`, 3 teste din `tests/Cerneala.Tests/` și fișierul nou `tests/Cerneala.Tests/UI/Aspect/ZzBreakerProbe.cs`. Acest plan nu le atinge.

## 4. Obiective

- Fiecare fișier `.md` din `docs/` stă în folderul tipului lui; rădăcina conține doar fișierele din §2.
- Niciun test, tool sau filtru CI nu se rupe: cele 4 teste focalizate, `PrismAudit --check` și suita completă trec după mutări.
- Nicio legătură nouă ruptă în documentele vii, verificat cu `Tools/scripts/Test-MarkdownLinks.ps1`.
- Cele 10 fapte false identificate în documentele vii sunt corectate (Etapa 6).
- Există un index `docs/README.md` (ce e unde, regula de numire) și un index `docs/plans/README.md` (starea reală a fiecărui plan).
- Skill-ul `writing-api-documentation` trimite la `docs-site/documentation/classes/`.

## 5. Non-obiective

- Rescrierea istoricului git ca să scape de cele 651 de fișiere `.trx` (278 MB).
- Mutarea sau redenumirea planurilor din `docs/plans/` și a folderului `evidence/`.
- Uniformizarea câmpului `Status:` în toate cele 65 de planuri. Se corectează doar cele 3 contradictorii (Etapa 6), restul primește starea în index.
- Repararea linkurilor rupte din documentele arhivate.
- Reorganizarea `benchmarks/results/` și `benchmarks/Cerneala.Benchmarks/results/` (legate din `docs-site/benchmarks.html`).
- O secțiune de arhitectură în `docs-site`, rularea verificatorului de linkuri în CI, verificarea ancorelor `#…`.
- Traducerea documentelor în română sau engleză.
- Numele „owner” greșite din raportul generat de PrismAudit (linia 266; vin din `RequiredSourcePaths`). Notat ca datorie.
- Cele 3 nume de manifest cu `<…>` în loc de `_T_`. Notat ca datorie.
- Conținutul documentelor de arhitectură (planul dependent).

## 6. Structura țintă

```text
README.md, CLAUDE.md, ROADMAP.md, LICENSE, FileTree.md
docs/
  README.md                       index nou
  guides/                         cum folosești ceva
    getting-started.md, application-markup.md, markup-guide.md (fost CernealaMarkupGuide.md),
    motion-api.md, motion-diagnostics.md, prism-guide.md, timbre-guide.md, servo.md,
    visual-studio-community.md, language-server.md
  reference/                      contracte exacte, liste, rapoarte generate
    markup-data-bindings.md, motion-markup-syntax.md (fost motion-markup-syntax-proposal.md),
    prism-markup-syntax.md (fost prism-markup-syntax-proposal.md),
    prism-adjustment-filters.md, prism-catalog-filters.md, prism-distortion-filters.md,
    prism-neighborhood-filters.md, prism-filter-reference.generated.md,
    prism-completeness-report.generated.md, prism-public-api-baseline.md,
    wpf-event-coverage.md, developer-preview-scope.md
  architecture/                   cum merge pe dinăuntru
    overview.md (fost /architecture.md), aspect.md (fost aspect-system.md),
    motion.md (fost motion-system.md), prism-technical-design.md, sdl-desktop-backend.md,
    diagrams/ (fost docs/diagrams/)
  plans/                          neschimbat + README.md index nou
  audits/                         toate auditurile, nume AAAA-LL-ZZ-subiect.md
  archive/                        istoric, cu banner
    superpowers/ (fost docs/superpowers/), roadmap-v2/, checklists/,
    architecture-v2.md, conceptual-ideas.md, visual-studio-community-spike.md,
    developer-preview-checklist.md
  assets/                         neschimbat
```

Fișierele Prism își păstrează prefixul `prism-`, ca filtrul CI să devină `docs/**/prism-*.md`.

Bannerul de arhivă este o singură linie, imediat sub titlu:

```markdown
> Arhivat la 2026-10-10: <motiv într-o propoziție>. Sursa actuală: [<nume>](<cale relativă>).
```

## 7. Fișiere estimate

Estimare, nu promisiune.

- **Nou:** `Tools/scripts/Test-MarkdownLinks.ps1`, `Tools/scripts/Test-MarkdownLinks.Tests.ps1`, `docs/README.md`, `docs/plans/README.md`.
- **Cod/config modificat:**
  - `Tools/PrismAudit/Program.cs` (căile de la liniile 190-193, 221, 276, 282-283, 323);
  - `Tools/scripts/New-PrismFilterReference.ps1`;
  - `.github/workflows/desktop-backends.yml` (liniile 15-16, 56-57);
  - `tests/Cerneala.Tests/Drawing/Prism/PrismVisualStyleSourcePairTests.cs`;
  - `tests/Cerneala.Tests.Language/Corpus/repository-documents.txt`;
  - `tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorTimbreTests.cs`;
  - eventual `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json` (după descoperirea din Etapa 5);
  - `.gitignore`.
- **Instrucțiuni:** `.claude/skills/writing-api-documentation/SKILL.md`, `.codex/skills/writing-api-documentation/SKILL.md`, `.codex/skills/writing-api-documentation/agents/openai.yaml`.
- **Site:** `docs-site/contributors.html` (linia 223); 2 pagini din `docs-site/documentation/classes/` și `docs-site/documentation/manifest.json`.
- **Documente:** ~45 de mutări/redenumiri; ~25 de documente vii cu linkuri actualizate; ~10 corecturi de conținut.
- **Șterse:** `claude-skills-import/` (13 fișiere), `AGENTS_DEPRECATED.md`, 2 pagini API.

## 8. Etape de implementare

Comenzile rulează din rădăcina repo-ului, în PowerShell.

### Etapa 0 — Verificator de linkuri și baseline

- [x] Scrie `Tools/scripts/Test-MarkdownLinks.ps1` cu acest contract:
  - Parametri: `-Root` (implicit rădăcina repo-ului), `-Baseline <fișier>` (opțional), `-WriteBaseline <fișier>` (opțional).
  - Fișiere scanate: `git -C $Root ls-files --cached --others --exclude-standard -- '*.md'`, fără `docs/plans/evidence/**`.
  - Linkuri: inline `[text](țintă)`, `[text](<țintă>)`, cu titlu opțional `"…"`, și linkuri de referință `[id]: țintă`. Ignoră blocurile de cod delimitate cu ``` și codul inline cu `.
  - Ignoră țintele `http:`, `https:`, `mailto:` și cele care sunt doar `#ancoră`. Taie `#ancoră` din restul; decodează `%20`.
  - Rezolvă ținta relativ la folderul fișierului sursă. Ținta e validă dacă există ca fișier sau ca folder.
  - Ieșire: câte o linie `sursă:linie -> țintă-rezolvată`, cu ținta relativă la rădăcină și separatori `/`.
  - Cod de ieșire 1 dacă există o țintă ruptă care nu apare în `-Baseline`. Comparația se face pe ținta rezolvată, ca o mutare care păstrează ținta să nu creeze o diferență.
- [x] Scrie `Tools/scripts/Test-MarkdownLinks.Tests.ps1` în stilul `Archive-Repo.Tests.ps1`: repo temporar cu `git init`, fără Pester. Cazuri care trebuie să treacă, fiecare cu `Assert-True`:
  - link valid spre fișier → nu e raportat;
  - link spre fișier lipsă → raportat cu linia corectă;
  - `https://…` → ignorat;
  - `#sectiune` → ignorat;
  - link într-un bloc ``` → ignorat;
  - `[id]: lipsa.md` → raportat;
  - `-Baseline` care conține ținta lipsă → cod de ieșire 0;
  - folder existent ca țintă → valid.
- [x] Rulează `pwsh -NoProfile -File Tools/scripts/Test-MarkdownLinks.Tests.ps1`: trece.
- [x] Salvează baseline-ul în afara git: `pwsh -NoProfile -File Tools/scripts/Test-MarkdownLinks.ps1 -WriteBaseline .artifacts/docs-reorg/links-baseline.txt`. Notează în acest plan numărul de ținte rupte la baseline. (Rezultat: 1.498 fișiere scanate, 6 linkuri rupte spre 2 ținte unice: `docs/plans/2026-10-03-timbre.md`, legat din 3 planuri Timbre, și `docs/plans/2026-08-27-cerberus-v2.md`, legat din `ROADMAP.md:165, 254`.)
- [x] Rulează și notează rezultatul baseline (toate trebuie să treacă înainte de orice mutare):
  - `dotnet run --project Tools/PrismAudit -- --check`
  - `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj -c Release --filter "FullyQualifiedName~PrismVisualStyleSourcePairTests"`
  - `dotnet test tests/Cerneala.Tests.Language/Cerneala.Tests.Language.csproj -c Release --filter "FullyQualifiedName~CorpusCoverageTests|FullyQualifiedName~MarkupParserTests"`
  - `dotnet test tests/Cerneala.Tests.SourceGen/Cerneala.Tests.SourceGen.csproj -c Release --filter "FullyQualifiedName~DocumentedTimbreExamplesCompile"`
  - `dotnet test tests/Cerneala.Tests.VisualStudio/Cerneala.Tests.VisualStudio.csproj -c Release --filter "FullyQualifiedName~Stage6ReleaseHarnessTests"`
  - Rezultat 2026-10-10: PrismAudit „178 catalog entries, 31 common properties, 216 public Prism types and 8 extended public types, zero gaps”; teste 1/1, 8/8, 2/2, 4/4 trecute. Log: `.artifacts/docs-reorg/stage0-baseline.log`.
- [x] Notează `git status --short`, ca fișierele altei sesiuni (§3) să rămână neatinse până la final. (Modificate de altă sesiune: `FileTree.md`, `Tools/scripts/New-FileTree.ps1`, `tests/Cerneala.Tests/Controls/ElementAspectTests.cs`, `tests/Cerneala.Tests/UI/Aspect/AspectTemplateCatalogIntegrationTests.cs`, `tests/Cerneala.Tests/UI/Aspect/AspectUnificationContractTests.cs`; `ZzBreakerProbe.cs` nu mai există.)

**Gate etapa 0**
- [x] Testul scriptului trece; baseline-ul de linkuri este salvat; numărul de ținte rupte este notat în plan.
- [x] Toate cele 5 comenzi baseline trec. Dacă una pică înainte de orice schimbare, oprește-te și raportează: nu e cauzată de acest plan.

### Etapa 1 — Ștergeri de copii moarte și instrucțiuni contradictorii

- [x] Șterge `claude-skills-import/` (`git rm -r`). Dovada că e copie moartă: nicio referință din `Cerneala.slnx`, din proiecte, din CI sau din `Tools/scripts`; e mai vechi decât `.claude/skills/` și `.codex/skills/` și nu are `git-session-commit-push` și `github-issue-raise`.
- [x] Șterge `AGENTS_DEPRECATED.md` (`git rm`). Principiile lui sunt în `CLAUDE.md`, iar regula lui despre `docs/documentation/` este falsă.
- [x] Citește contextul din `docs-site/contributors.html:223` și înlocuiește linkul `AGENTS.md`, care nu există, cu fișierul de instrucțiuni care există, `CLAUDE.md`, păstrând sensul propoziției. (`AGENTS.md` era text în avertismentul maintainerului, nu link; înlocuit cu `CLAUDE.md`.)
- [x] În `.claude/skills/writing-api-documentation/SKILL.md` și `.codex/skills/writing-api-documentation/SKILL.md` înlocuiește destinația `docs/documentation/` (liniile 3, 10, 20, 26, 44) cu `docs-site/documentation/classes/`. Adaugă obligația de a sincroniza `docs-site/documentation/manifest.json`. Șterge pasul „creează folderul dacă nu există”. Aplică aceeași corectură în `.codex/skills/writing-api-documentation/agents/openai.yaml:4`. (Pasul de numire a fost aliniat la paginile existente, `<Namespace>.<Type>.md`. Linia 17, „local AGENTS.md instructions”, a rămas neschimbată: e în afara acestei sarcini.)
- [x] Adaugă în `.gitignore`: `.local/`, `.kilo/`, `**/.claude/worktrees/`. Azi ultimul există doar în `.git/info/exclude`, care e local și nu ajunge într-o clonă nouă.

**Gate etapa 1**
- [x] `Test-MarkdownLinks.ps1 -Baseline .artifacts/docs-reorg/links-baseline.txt` iese cu 0.
- [x] O căutare a șirului `docs/documentation/` în fișierele urmărite (delegată conform `CLAUDE.md`) găsește doar interdicții: `CLAUDE.md`, `.codex/config.toml` și istoricul din `docs/plans/`. (Rezultat: 6 interdicții, inclusiv cele noi din cele 2 skill-uri; 7 apariții istorice în planuri; 0 instrucțiuni. O pagină API vie, `Cerneala.UI.Controls.Templates.ContentTemplateContext_TData_.md:93`, trimitea la `docs/documentation/`; corectată spre pagina existentă din `docs-site/documentation/classes/`.)
- [x] `git check-ignore -v .local/x .kilo/x .claude/worktrees/x` arată regulile din `.gitignore`.

### Etapa 2 — Arhivare

- [x] `git mv docs/superpowers docs/archive/superpowers`.
- [x] `git mv ROADMAPv2.md docs/archive/roadmap-v2/roadmap-v2.md`, `git mv ROADMAPv2_AUDIT.md docs/archive/roadmap-v2/roadmap-v2-audit.md`, `git mv AUDIT_FIX_PLAN.md docs/archive/roadmap-v2/audit-fix-plan.md`.
- [x] Mută în `docs/archive/checklists/`:
  - `AspectChecklist.md` → `aspect-checklist.md`
  - `MotionChecklist.md` → `motion-checklist.md`
  - `PrismChecklist.md` → `prism-checklist.md`
  - `ClassChecklist.md` → `2026-08-18-class-checklist.md`
  - `DOCUMENTATION_CHECKLIST.md` → `documentation-checklist.md`
- [x] Mută `ConceptualIdeas.md` → `docs/archive/conceptual-ideas.md`, `docs/architecture-v2.md` → `docs/archive/architecture-v2.md`, `docs/visual-studio-community-spike.md` → `docs/archive/visual-studio-community-spike.md`, `docs/developer-preview-checklist.md` → `docs/archive/developer-preview-checklist.md`. Ultimul filtrează după 3 clase de test care nu există; doar `DeveloperPreviewScopeTests` există.
- [x] Adaugă bannerul din §6 în fiecare fișier arhivat de nivel 1, cu motivul lui: (12 bannere + `docs/archive/superpowers/README.md`. Bannerele care trimit la fișiere încă nemutate, de exemplu `architecture.md`, folosesc calea de azi; mutările din etapele următoare le rescriu automat.)

  | Fișier | Motiv | Sursa actuală |
  |---|---|---|
  | roadmap-v2 | status din 2026-08-18; MonoGame scos | `ROADMAP.md` |
  | architecture-v2 | afirmații false despre API | `docs/architecture/overview.md` |
  | checklist-uri | inventare terminate | `docs-site/documentation/manifest.json` pentru API |
  | superpowers | plugin abandonat | `docs/plans/` |
  | celelalte | motivul din §3 | — |

  Pentru `docs/archive/superpowers/` se pune un singur `README.md` cu bannerul, nu câte un banner în fiecare dintre cele 51 de fișiere.
- [x] Rescrie linkurile relative care pleacă din fișierele mutate, ca să rezolve aceleași ținte ca înainte. Actualizează linkurile din documentele vii care intră spre fișierele mutate: `ROADMAP.md:256-258`, `docs/plans/2026-07-14-relay-auto-marshaling-implementation-notes.md:53` și restul raportat de verificator. (Fișierele mutate nu au niciun link relativ, iar scriptul temporar de mutare a găsit 0 linkuri de rescris. `ROADMAP.md:256-258` pomenea `ROADMAPv2.md` și `ROADMAPv2_AUDIT.md` doar ca text; acum sunt linkuri spre `docs/archive/roadmap-v2/`. Notele Relay de la linia 53 sunt istoric de plan și rămân neschimbate. O căutare delegată în fișierele urmărite care nu sunt `.md` a găsit 0 consumatori după cale.)

**Gate etapa 2**
- [x] `Test-MarkdownLinks.ps1 -Baseline …` iese cu 0. (1.485 fișiere, 6 linkuri rupte spre aceleași 2 ținte ca la baseline.)
- [x] Rădăcina nu mai conține fișierele `.md` mutate în această etapă. (Rămân doar `README.md`, `CLAUDE.md`, `ROADMAP.md`, `FileTree.md`, `architecture.md` pentru Etapa 5 și cele 4 audituri pentru Etapa 3.)

### Etapa 3 — Audituri

- [x] Mută în `docs/audits/`:
  - `Aspect_Audit_09022026.md` → `2026-09-02-aspect-audit.md`
  - `Motion_Audit_09022026.md` → `2026-09-02-motion-audit.md`
  - `Prism_Audit_09022026.md` → `2026-09-02-prism-audit.md`
  - `Relay_Audit_09022026.md` → `2026-09-02-relay-audit.md`
- [x] Redenumește `docs/audits/prism-visual-algorithm-checklist-2026-07-25.md` → `docs/audits/2026-07-25-prism-visual-algorithm-checklist.md`.
- [x] Redenumește `docs/audits/prism-visual-style-algorithm-checklist-2026-08-02.md` → `docs/audits/2026-08-02-prism-visual-style-algorithm-checklist.md`. În aceeași schimbare actualizează numele din `PrismVisualStyleSourcePairTests.cs:12-16`. (Căutarea delegată în fișierele urmărite care nu sunt `.md` a găsit doar acest consumator.)
- [x] Adaugă în `docs/audits/2026-09-02-prism-audit.md` și `docs/audits/2026-09-08-prism-architecture-remediation.md` câte un link reciproc. Azi nu se leagă între ele, deși tratează același subiect.
- [x] Actualizează linkurile intrate și ieșite raportate de verificator. (Nu a fost necesar: scriptul de mutare a găsit 0 linkuri spre sau din cele 6 fișiere. Rămân pomeniri ca text doar în istoric: `docs/archive/**` și `FileTree.md`, care e generat și modificat de altă sesiune.)

**Gate etapa 3**
- [x] `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj -c Release --filter "FullyQualifiedName~PrismVisualStyleSourcePairTests"` trece. (1/1.)
- [x] Verificatorul de linkuri iese cu 0; rădăcina nu mai are fișiere `*_Audit_*.md`. (1.485 fișiere, 6 linkuri rupte spre aceleași 2 ținte.)

### Etapa 4 — Ghiduri și referințe (fără Prism)

- [x] `git mv` în `docs/guides/`: `getting-started.md`, `application-markup.md`, `CernealaMarkupGuide.md` (→ `markup-guide.md`), `motion-api.md`, `motion-diagnostics.md`, `timbre-guide.md`, `servo.md`, `visual-studio-community.md`, `language-server.md`.
- [x] `git mv` în `docs/reference/`: `markup-data-bindings.md`, `motion-markup-syntax-proposal.md` (→ `motion-markup-syntax.md`), `wpf-event-coverage.md`, `developer-preview-scope.md`.
- [x] Actualizează `tests/Cerneala.Tests.Language/Corpus/repository-documents.txt:19-21` la `docs/guides/markup-guide.md`, `docs/reference/markup-data-bindings.md`, `docs/reference/motion-markup-syntax.md`.
- [x] Actualizează `UiMarkupGeneratorTimbreTests.DocumentedTimbreExamplesCompile`: `InlineData("guides/timbre-guide.md")` și `InlineData("guides/markup-guide.md")`. `DocumentationRoot()` rămâne `docs/`.
- [x] Actualizează linkurile spre ghidurile mutate din `README.md` (liniile 11, 215 și restul raportat), din documentele vii și din `ROADMAP.md`. Actualizează linkurile relative care pleacă din ghidurile mutate. (Scriptul de mutare a rescris 40 de linkuri în 11 fișiere, inclusiv `README.md`, `architecture.md`, `docs/sdl-desktop-backend.md` și bannerele din arhivă. `ROADMAP.md` nu lega aceste ghiduri. În plus, 11 căi vechi scrise ca text în 9 pagini API din `docs-site/documentation/classes/` au fost actualizate. Căutarea delegată în fișierele care nu sunt `.md` a găsit doar cele două teste de mai sus; nu există URL-uri GitHub spre aceste documente.)

**Gate etapa 4**
- [x] `CorpusCoverageTests`, `MarkupParserTests` și `DocumentedTimbreExamplesCompile` trec, cu comenzile din Etapa 0. (8/8 și 2/2.)
- [x] Verificatorul de linkuri iese cu 0. (1.485 fișiere, 6 linkuri rupte spre aceleași 2 ținte.)

### Etapa 5 — Prism, arhitectură existentă și tool-urile care le citesc

Etapă atomică: toate căile Prism se schimbă împreună cu `PrismAudit`, scriptul generator și CI.

- [x] **Descoperire înainte de orice editare:** urmărește unde ajunge câmpul cu ancorele `prism-markup-syntax-proposal.md#…` din `prism-catalog.json:485, 501, 519, 533`, pornind de la `PrismCatalogCompiler.cs:478-503`. Variante:
  - șirul ajunge în cod generat, diagnostice, pagini docs-site sau referința de filtre → se actualizează la `docs/reference/prism-markup-syntax.md#…`, iar lanțul de regenerare de mai jos devine obligatoriu;
  - șirul nu ajunge nicăieri vizibil → se actualizează oricum, ca să nu trimită la un fișier inexistent.

  Notează rezultatul în acest plan.

  **Rezultat:** cele 4 șiruri sunt `coverage.documentation` din `conformance.features[]`, nu din `entries`. `PrismCatalogCompiler` citește doar `entries` (`PrismCatalogCompiler.cs:263`; `conformance` apare doar în lista de câmpuri permise, linia 21), deci șirurile **nu** ajung în codul generat și nici în diagnostice. Le citește `Tools/PrismAudit/Program.cs:1025` și le scrie în tabelul de conformanță al raportului (`:660`, rândurile 971-974 din raport), pe care CI îl verifică cu `--check`. `PrismColorBlendStyleCoverageTests.cs:50` verifică doar că nu sunt goale. `New-PrismFilterReference.ps1` nu citește câmpul, dar pune hash-ul fișierului întreg. Concluzie: s-au actualizat la `docs/reference/prism-markup-syntax.md#…`, iar lanțul de regenerare a fost obligatoriu. Ancorele nu corespund niciunui titlu din document; verificarea ancorelor e non-obiectiv (§5).
- [x] `git mv` în `docs/reference/`: `prism-markup-syntax-proposal.md` (→ `prism-markup-syntax.md`), `prism-adjustment-filters.md`, `prism-catalog-filters.md`, `prism-distortion-filters.md`, `prism-neighborhood-filters.md`, `prism-filter-reference.generated.md`, `prism-completeness-report.generated.md`, `prism-public-api-baseline.md`. `prism-guide.md` merge în `docs/guides/`.
- [x] `git mv` în `docs/architecture/`:
  - `/architecture.md` → `overview.md`
  - `docs/aspect-system.md` → `aspect.md`
  - `docs/motion-system.md` → `motion.md`
  - `docs/prism-technical-design.md`, numele rămâne
  - `docs/sdl-desktop-backend.md`, numele rămâne
  - `docs/diagrams/` → `docs/architecture/diagrams/`
- [x] Actualizează `Tools/PrismAudit/Program.cs`: cele 4 căi din `RequiredSourcePaths`, `reportPath` (linia 221), referința de filtre (276), propunerea și designul tehnic (282-283), baseline-ul API (323). Tokenurile cerute la liniile 286-295 nu se schimbă. (În plus, căile scrise ca text în tabelul „Design inputs” al raportului, liniile 568-570.)
- [x] Actualizează calea de ieșire din `Tools/scripts/New-PrismFilterReference.ps1:15-18`.
- [x] Actualizează `tests/Cerneala.Tests.Language/Corpus/repository-documents.txt:22` la `docs/reference/prism-markup-syntax.md`.
- [x] În `.github/workflows/desktop-backends.yml:15-16` și `:56-57`: `docs/prism-*.md` → `docs/**/prism-*.md`; `docs/sdl-desktop-backend.md` → `docs/architecture/sdl-desktop-backend.md`. (Colateral necesar: `.gitattributes:5`, `/docs/prism-*.md text eol=lf` → `/docs/**/prism-*.md text eol=lf`, ca documentele Prism mutate să-și păstreze LF.)
- [x] Rulează `pwsh -NoProfile -File Tools/scripts/New-PrismFilterReference.ps1`. Diff-ul referinței de filtre trebuie să fie gol; dacă catalogul s-a schimbat, singura diferență permisă este linia `catalog-sha256` (și ancorele, dacă descoperirea a arătat că apar în referință). (Diff: o singură linie, `catalog-sha256` `34e0a054…` → `71c56eaf…`, pentru că s-au schimbat cele 4 căi din catalog.)
- [x] Rulează `dotnet run --project Tools/PrismAudit -- --write`. Revizuiește diff-ul raportului: sunt permise doar hash-urile documentelor redenumite sau editate și căile. Orice altă diferență oprește etapa. (Diff: 9 linii: hash-ul catalogului, cele 3 căi și hash-uri din „Design inputs” și coloana de documentație a celor 4 rânduri de conformanță; `--word-diff` confirmă că în acele rânduri s-a schimbat doar calea.)
- [x] Actualizează linkurile spre fișierele mutate și linkurile relative care pleacă din ele, inclusiv `docs/assets/cerneala-architecture.png` folosit în `overview.md`. (Scriptul de mutare a rescris 39 de linkuri în 12 fișiere; în `overview.md` imaginea este acum `../assets/cerneala-architecture.png`. Căile scrise ca text au fost actualizate în 2 pagini API și în `prism-public-api-baseline.md:94`.)

**Gate etapa 5**
- [x] `dotnet run --project Tools/PrismAudit -- --check` trece. („178 catalog entries, 31 common properties, 216 public Prism types and 8 extended public types, zero gaps”.)
- [x] Dacă `prism-catalog.json` s-a schimbat: `dotnet test tests/Cerneala.Tests.SourceGen/Cerneala.Tests.SourceGen.csproj -c Release --filter "FullyQualifiedName~Prism"` trece. (70/70. În plus, `PrismColorBlendStyleCoverageTests`, care citește câmpul schimbat: 51/51.)
- [x] `CorpusCoverageTests` trece; verificatorul de linkuri iese cu 0. (2/2; 1.485 fișiere, 6 linkuri rupte spre aceleași 2 ținte.)
- [x] Filtrul CI a fost verificat prin citire: în `docs/` nu mai rămâne niciun fișier `prism-*.md` pe care `docs/**/prism-*.md` să nu-l prindă. **Politică de platformă:** workflow-ul nu poate fi rulat local; rularea reală se observă la primul push. Până atunci starea este „verificat prin citire”, nu GREEN. (Verificat prin citire: toate fișierele `prism-*.md` stau în `docs/reference/`, `docs/guides/` și `docs/architecture/`. Starea: verificat prin citire, nu rulat.)
- [x] Nu mai există niciun fișier `.md` direct în `docs/` (`docs/README.md` se scrie abia în Etapa 7).

### Etapa 6 — Corecturi de conținut în documentele vii

Fiecare corectură se verifică în sursă înainte de editare: Roslyn MCP pentru simboluri C#, citire directă pentru căi.

- [x] `README.md:102`: mută `Line`, `Polyline`, `Polygon` la „Available now” (clase publice în `UI/Controls/Shapes/`). (Roslyn `list_document_symbols`: `public sealed class Line : Shape` la `UI/Controls/Shapes/Line.cs:8`, la fel `Polyline.cs:8` și `Polygon.cs:8`; `Shape` este `public abstract class Shape : Control`, `Shape.cs:11`. Coloana „Not implemented yet” a rândului rămâne goală.)
- [x] `docs/guides/getting-started.md:74-81`: forma de referință din `CernealaPresentation` are fereastra `PresentationWindow.crn`, nu `MainWindow.crn` (`CernealaPresentation/App.crn:2`). Corectează descrierea fără să schimbi regula generală. (Paragraful de după lista de fișiere spune acum că proiectul de referință folosește `PresentationWindow.crn`/`.crn.cs` și `StartupWindow="PresentationWindow"`, iar exemplele folosesc `MainWindow`. Dovada: `CernealaPresentation/App.crn:2`.)
- [x] `docs/guides/getting-started.md`, cerințele (liniile 14-19): spune că `Cerneala.VisualStudio` țintește `net472` și cere targeting pack-ul .NET Framework 4.7.2. Afirmația se verifică întâi în `Cerneala.VisualStudio.csproj:4, 38`; dacă build-ul pe acest workstation arată altceva, scrie ce arată build-ul. (`Cerneala.VisualStudio.csproj:4` = `<TargetFramework>net472</TargetFramework>`, fără pachetul `Microsoft.NETFramework.ReferenceAssemblies`; `Cerneala.slnx:51` include proiectul; pe workstation există `Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2` și `Cerneala.VisualStudio/bin/Release/net472`, deci build-ul local confirmă.)
- [x] `docs/guides/motion-diagnostics.md`: exemplul `root.Motion.Diagnostics.IsEnabled = true;` nu compilează din afara assembly-ului (`MotionSystem.Diagnostics` este `internal`, `UI/Motion/Core/MotionSystem.cs:59`). Înlocuiește-l cu ruta publică `root.Detective.Motion.IsEnabled = true;` (`UI/Elements/UIRoot.cs:129`, `UI/Detective/Detective.cs:25`). (Roslyn `hover`: `internal MotionDiagnostics Diagnostics` la `MotionSystem.cs:59`; `public Detective Detective { get; }` la `UIRoot.cs:129`; `public MotionDiagnostics Motion => root.Motion.Diagnostics;` la `Detective.cs:25`; `public bool IsEnabled { get; set; }` la `UI/Detective/MotionDiagnostics.cs:10`.)
- [x] `docs/reference/prism-catalog-filters.md:103`: `Filters/Catalog/Grain.fx` → `Drawing/Prism/Shaders/Hlsl/Filters/Catalog/Grain.hlsl`, după verificarea existenței fișierului. (`git ls-files` găsește `Drawing/Prism/Shaders/Hlsl/Filters/Catalog/Grain.hlsl`; niciun `Grain.fx` urmărit.)
- [x] `docs/guides/markup-guide.md:49`: `tests/CodexPresentationHarness/generated/` nu există. Înlocuiește-l cu locul real al surselor generate, verificat în sursă, sau scoate fraza dacă nu există un echivalent. (Calea scoasă; regula rămâne „Never edit generated files under `obj/` or `bin/`.” Dovada: folderul nu există și n-a fost urmărit niciodată (`git log --all` gol). Generatoarele adaugă sursele doar în memorie, prin `context.AddSource` (`UiMarkupGenerator.cs:575` și celelalte 4 generatoare); niciun proiect urmărit nu setează `EmitCompilerGeneratedFiles`, deci nu există un loc echivalent pe disc.)
- [x] `docs/reference/motion-markup-syntax.md`: verifică în `Cerneala.SourceGen/` și `Cerneala.Language/` cuvintele `@else`, `$event`, `@complete`, `@clear` (azi fără potriviri textuale). Cele implementate rămân. Cele neimplementate se marchează explicit „neimplementat” în document. Statutul de la început devine „referință”, nu „propunere”. Documentul nu este pin-uit de `PrismAudit`. (Nu a fost necesară nicio editare: liniile 3-6 spun deja „defines the implemented Cerneala Motion markup language”, fără „proposal”, iar toate cele 4 cuvinte sunt deja în secțiunea „Deferred Surface” (liniile 934-942), marcate „not accepted Motion markup”. Niciunul nu e implementat: lipsesc din tabelul de directive `Cerneala.SourceGen/MotionMarkupLanguage.cs:10-29`; `@complete` cade în eroarea „Unsupported directive” (`UiMarkupDirectiveParser.cs:335`), cerută de `UiMarkupGeneratorMotionHandleTests.cs:128`.)
- [x] `docs/reference/prism-markup-syntax.md`: statutul de la început devine „referință (implementat)”. Apoi `dotnet run --project Tools/PrismAudit -- --write` și `--check` (hash-ul documentului e în raport). (Statutul începe acum cu „Reference (implemented).”; `--write` a regenerat raportul, `--check`: „178 catalog entries, 31 common properties, 216 public Prism types and 8 extended public types, zero gaps”.)
- [x] `ROADMAP.md:165, 254`: linkul spre `docs/plans/2026-08-27-cerberus-v2.md`, care nu există. Găsește planul Cerberus real (candidat din inventar: `docs/plans/2026-09-02-private-sdlgpu-cerberus.md`, de confirmat după conținut). Dacă se potrivește, leagă-l; altfel scoate linkul și păstrează textul. (Nu se potrivește: candidatul este planul finalizat al executorului privat de batching, fără compiler/encoder/multi-texture. Fișierul V2 n-a fost niciodată urmărit: `git log --all -- docs/plans/2026-08-27-cerberus-v2.md` e gol. Ambele linkuri au fost scoase; secțiunea spune acum că planurile V2 n-au fost puse în repo și leagă planul Cerberus care a intrat în cod. Au fost scoase și frazele despre „checked-in plans” și căsuțele lor, care descriau aceleași fișiere inexistente.)
- [x] `benchmarks/Cerneala.PresentationFrameBudget/README.md:3-8`: proiectul pornește `CernealaPresentation` pe SDL_GPU, nu WindowsDX / Direct3D 11. Rescrie cerințele după `Program.cs` și `.csproj`. (`Program.cs:34-41` pornește `CernealaPresentation/bin/Release/net8.0-windows/CernealaPresentation.exe`; `CernealaPresentation/BackendRegistration.cs:1-2` selectează `SdlGpuApplicationBackend`; `.csproj` țintește `net8.0-windows`. Și fraza finală despre „same WindowsDX machine” a fost corectată.)
- [x] Pagini API pentru tipuri inexistente:
  - `Cerneala.UI.Text.LineBreakService.TextElement`
  - `Cerneala.UI.Resources.ResourceDependencyTracker.ResourceProviderReferenceEqualityComparer`

  Confirmă cu Roslyn `workspace_symbol` că tipul nu există nicăieri, inclusiv în fișiere partial. Atenție la cazul `UIRoot.ThemeChangedSubscription`, declarat într-un fișier soră. Abia apoi șterge pagina și intrarea din `manifest.json`. Dacă tipul există, pagina rămâne și se corectează doar `source`. (Roslyn `workspace_symbol`: niciun tip `TextElement` (doar `Cerneala.Drawing.Text.UnicodeTextElement`, alt tip) și zero simboluri `ResourceProviderReferenceEqualityComparer`. `LineBreakService` și `ResourceDependencyTracker` nu sunt `partial` și nu declară aceste tipuri imbricate. Tipurile au fost scoase din cod în commit-urile din istoric (de exemplu `0146c1e2`). Ambele pagini au fost șterse cu `git rm`, iar cele 2 intrări din `manifest.json` au fost scoase; manifestul se parsează în continuare ca JSON.)
- [x] Starea a 3 planuri contradictorii, doar în antetul lor:
  - `2026-07-10-inline-component-template-markup.md`: „implementat; căsuțele nu au fost bifate; lipsesc demo-urile din Playground și `getting-started`”.
  - `2026-07-13-queue-engine-2.md`: o notă că cele 16 căsuțe deschise sunt condiții de oprire și subiecte de commit, iar munca a intrat în `ba27cacc`.
  - `2026-09-04-rendersurface2d-world-authoring-plan-index.md`: „în lucru: planurile copil finalizate, gate-ul final al indexului deschis (include criteriul MonoGame, retras)”.

  (Notele au fost scrise în limba fiecărui plan. `git log -1 ba27cacc` = „Queue Engine 2.0”; planul queue-engine are 16 căsuțe `[ ]`, planul inline-component are 100 de căsuțe `[ ]` și niciuna `[x]`.)

**Gate etapa 6**
- [x] `Stage6ReleaseHarnessTests` și `PrismAudit --check` trec; verificatorul de linkuri iese cu 0. (În starea finală a etapei: `Stage6ReleaseHarnessTests` 4/4; `PrismAudit --check`: „178 catalog entries, … zero gaps”; linkuri: exit 0, 1.483 fișiere, 4 linkuri rupte spre o singură țintă (`docs/plans/2026-10-03-timbre.md`), pentru că cele 2 linkuri Cerberus au fost reparate. În plus, pentru documentele citite de teste: testele `Corpus` din `Cerneala.Tests.Language` 117/117 și `DocumentedTimbreExamplesCompile` 2/2.)
- [x] Fiecare corectură are notată în acest plan sursa care o dovedește (fișier:linie sau rezultat Roslyn).

### Etapa 7 — Indexuri

- [x] Scrie `docs/README.md`: (Scris: tabel cu cele 7 foldere și câte un fișier exemplu legat, secțiunea „API documentation”, regula de numire și tabelul „Where new documents go”.)
  - ce conține fiecare folder din §6, cu câte un exemplu de fișier;
  - regula de numire din §2;
  - unde stă documentația API: doar în `docs-site/documentation/classes/`;
  - unde se scrie un plan nou (`docs/plans/AAAA-LL-ZZ-subiect.md`) și un audit nou (`docs/audits/AAAA-LL-ZZ-subiect.md`).
- [x] Scrie `docs/plans/README.md`, cu un tabel pentru toate cele 65+2 planuri: fișier, dată, stare reală (finalizat / în lucru / propus / fără checklist), motivul când starea diferă de antet. Starea vine din numărarea `- [x]` / `- [ ]` și din verificările din Etapa 6, nu din antet. (67 de rânduri; documentul e în engleză, ca restul `docs/`, deci stările sunt `completed` / `in progress` / `proposed` / `no checklist`. Numărătorile vin dintr-un inventar delegat cu `grep -c -E '^\s*[-*] \[[xX]\]'` și `'^\s*[-*] \[ \]'` pe fiecare fișier. Rezultat: 55 completed, 2 in progress, 2 proposed, 8 no checklist.)
- [x] `README.md`: secțiunea de documentație trimite la `docs/README.md` și la `docs/architecture/overview.md`. (`overview.md` era deja legat; s-a adăugat „Documentation index” spre `docs/README.md`.)

**Gate etapa 7**
- [x] Fiecare fișier `.md` din `docs/plans/` (fără `evidence/`) apare o singură dată în `docs/plans/README.md`. Verificarea compară lista `git ls-files docs/plans/*.md` cu tabelul. (`git ls-files` urmărite + neurmărite neignorate, doar copii directe, fără `README.md`: 67 fișiere; linkurile din tabel: 67; zero duplicate; `diff` între cele două liste: identic.)
- [x] Verificatorul de linkuri iese cu 0. (1.485 fișiere; 4 linkuri rupte spre aceeași țintă, `docs/plans/2026-10-03-timbre.md`.)

### Etapa 8 — Verificare finală

- [x] `pwsh -NoProfile -File Tools/scripts/Test-MarkdownLinks.Tests.ps1` și `Test-MarkdownLinks.ps1 -Baseline …` trec. Raportează câte ținte rupte au rămas față de baseline; numărul nu are voie să crească. („Test-MarkdownLinks tests passed.”; verificatorul: exit 0, 1.485 fișiere, 4 linkuri rupte spre 1 țintă (`docs/plans/2026-10-03-timbre.md`), față de 6 linkuri spre 2 ținte la baseline.)
- [x] Căutare delegată a tuturor căilor vechi din §6 (`architecture.md`, `docs/CernealaMarkupGuide.md`, `docs/prism-`, `docs/diagrams/`, `docs/superpowers/`, numele auditurilor din rădăcină etc.) în fișierele urmărite. Rămân permise doar în `docs/archive/**`, `docs/plans/**` (istoric) și `docs/plans/evidence/**`. (48 de tipare, căutate în 6.878 fișiere urmărite plus 7 neurmărite. O singură potrivire vie: eticheta de link `` `prism-markup-syntax-proposal.md` `` din `docs/architecture/prism-technical-design.md:22`, a cărei țintă era deja nouă. Eticheta a fost corectată, apoi `PrismAudit --write` și `--check` au trecut, pentru că raportul conține hash-ul documentului. Restul aparițiilor sunt în `docs/archive/`, `docs/plans/`, `docs/audits/`, `benchmarks/**/results/` sau `FileTree.md` (dirty, al altei sesiuni). Și verificările suplimentare de linkuri relative și căi din cod au dat zero căi învechite.)
- [x] Prerechizitele din `CLAUDE.md`: `dotnet build Cerneala.slnx -c Debug` și `dotnet restore tests/Fixtures/VisualStudioIntegrationHost/VisualStudioIntegrationHost.csproj`. („Build succeeded”, 0 erori, 34 avertismente; restore: „All projects are up-to-date”.)
- [x] Suita completă, în fundal: `dotnet test Cerneala.slnx -c Release -p:NuGetAudit=false`. Orice eșec se investighează. Un eșec care apare și pe baseline-ul `def7f419` se raportează ca preexistent, cu dovada. (12 proiecte de test; 11 verzi. `Cerneala.Tests`: 4.415 trecute, 4 picate, 2 sărite. Cele 4 teste picate sunt `ElementAspectTests.BehaviorThatReplacesItsAspectWhileAttachingDisposesEachLifetimeWithItsAspect` (2 cazuri), `AspectUnificationContractTests.LongRunningSessionDoesNotExhaustCompositeCatalogVersions` și `AspectTemplateCatalogIntegrationTests.TemplateReplacedDuringAspectPassDoesNotReprocessItsDetachedPart`. Ele **nu există** în `def7f419` (`git grep` pe `HEAD` nu le găsește). Au fost adăugate doar de modificările necomise ale altei sesiuni (§3), +150 linii în cele 3 fișiere. Acest plan nu atinge nici fișierele acelea, nici codul de runtime Aspect: fișierele non-doc schimbate de plan sunt doar `Tools/PrismAudit/Program.cs`, `New-PrismFilterReference.ps1`, șirurile `coverage.documentation` din catalog, CI, `.gitattributes`, `Test-MarkdownLinks*.ps1` și 3 căi de documente din teste. Concluzie: eșecurile nu țin de acest plan și sunt raportate, nu ascunse. Restul proiectelor: VisualStudio 48/48, SourceGen 683/683, Language 410 + 1 sărit, Tetris 31/31, SceneVillage 36 + 8 sărite, LanguageServer 45/45, Scene2DImporters 173/173, SdlGpu 601 + 245 sărite (nativ, opt-in), Scene2DPackages 104/104, Timbre 454/454, PreviewHost 23/23.)
- [x] `git status --short`: fișierele altei sesiuni (§3) sunt neschimbate de acest plan. (`FileTree.md`, `Tools/scripts/New-FileTree.ps1` și cele 3 fișiere de test Aspect apar ` M`, exact ca la începutul sesiunii; planul nu le-a editat.)

**Gate etapa 8**
- [x] Toate punctele de mai sus trec, sau eșecurile sunt dovedite preexistente și raportate. (Singurele eșecuri, 4 teste din `Cerneala.Tests`, sunt adăugate de altă sesiune și nu există pe baseline; sunt raportate mai sus.)

### Etapa 9 — Curățenie locală (doar cu confirmarea utilizatorului)

Aceste foldere sunt ignorate de git. Ștergerea lor **nu se poate recupera**. Etapa începe doar după ce utilizatorul confirmă lista exactă.

- [x] Recalculează dimensiunile și prezintă lista. Mărimile din inventarul de la 2026-10-10: (Recalculate cu `du -sh` înainte de întrebare; aceleași valori ca în tabel; `stage3` din evidence avea 1,8 GB, iar `stage1` 437 MB. Utilizatorul a confirmat toate cele 4 grupe prin `AskUserQuestion`.)

  | Cale | Mărime |
  |---|---|
  | `artifacts/` | 10 GB |
  | `tests/CodexGpuDriverHarness/` | 500 MB |
  | `tests/CodexMonoGameParityHarness/` | 14 MB |
  | `tests/CodexTetrisStyleHarness/` | 138 MB |
  | `tests/CodexUiImagePerfHarness/` | 512 MB |
  | `tests/Cerneala.WindowsDxSmoke/` | 1,3 GB; doar `bin`/`obj`, sursa a fost ștearsă în `7890b6ae` |
  | `TestResults/` | 503 MB |
  | `.kilo/` | 61 MB |
  | `.superpowers/` | 9 KB |
  | `.local/`, `.claude/worktrees/` | foldere goale |
  | `docs/plans/evidence/2026-10-03-timbre-core-stage0/`, `-stage3/`, `2026-10-05-timbre-core-stage0-rebuild/`, `2026-10-05-timbre-core-stage1/` | doar `bin`/`obj` ignorate |
- [x] **Exclus explicit:** `.artifacts/`, pentru că conține `issue-drafts/` și dovezi de lucru. (Neatins.)
- [x] După confirmare: verifică fiecare cale cu `git ls-files <cale>` (trebuie să dea 0 fișiere urmărite), apoi șterge-o. Pentru evidence, șterge doar `bin/` și `obj/`. (Scriptul a verificat rădăcina repo-ului și `git ls-files -- <cale>` = 0 înainte de fiecare `rm -rf`; nicio cale nu a fost sărită. În evidence s-au șters doar cele 50 de foldere `bin/`/`obj/` găsite sub cele 4 foldere Timbre.)

**Gate etapa 9**
- [x] Raport cu fiecare cale ștearsă și spațiul eliberat; `git status` nu arată nicio schimbare urmărită. (Șterse: `artifacts/` 10 GB, `tests/CodexGpuDriverHarness/` 500 MB, `tests/CodexMonoGameParityHarness/` 14 MB, `tests/CodexTetrisStyleHarness/` 138 MB, `tests/CodexUiImagePerfHarness/` 512 MB, `tests/Cerneala.WindowsDxSmoke/` 1,3 GB, `TestResults/` 503 MB, `.kilo/` 61 MB, `.superpowers/` 9 KB, `.local/`, `.claude/worktrees/`, plus `bin/`/`obj/` din cele 4 foldere de evidence Timbre. Spațiu eliberat: 15.355 MB (după `df`). `git ls-files -d` = 0, deci niciun fișier urmărit nu a fost șters.)

## 9. Ordinea recomandată

0 → 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8. Etapa 9 este independentă și se face oricând după confirmare. Planul [documentele de arhitectură](2026-10-10-architecture-docs.md) poate începe după Gate etapa 5.

## 10. Condiții de oprire

- Un test sau `PrismAudit --check` pică pe baseline (Etapa 0) → oprire și raport, fără mutări.
- Diff-ul raportului PrismAudit sau al referinței de filtre conține altceva decât căi/hash-uri → oprire, nu se acceptă regenerarea.
- O mutare ar cere schimbarea unui fișier modificat de altă sesiune (§3) → oprire și întrebare.
- Tentația de a rescrie conținutul documentelor de arhitectură aici → aparține planului dependent.

## 11. Definiția de gata

- Rădăcina are doar `README.md`, `CLAUDE.md`, `ROADMAP.md`, `LICENSE`, `FileTree.md` ca fișiere de documentație.
- În `docs/` există doar `README.md` direct; restul e în `guides/`, `reference/`, `architecture/`, `plans/`, `audits/`, `archive/`, `assets/`.
- `claude-skills-import/` și `AGENTS_DEPRECATED.md` nu mai există; skill-ul `writing-api-documentation` trimite la `docs-site/documentation/classes/`.
- `Test-MarkdownLinks.ps1` există, are test care trece, iar numărul de ținte rupte nu e mai mare decât la baseline.
- `PrismAudit --check`, cele 4 teste focalizate și suita completă trec, sau eșecurile sunt dovedite preexistente.
- Cele 10 corecturi din Etapa 6 au dovada notată.
- `docs/README.md` și `docs/plans/README.md` există și acoperă toate fișierele.
- Etapa 9 este fie executată după confirmare, fie notată „neconfirmată”.
