# Plan detaliat pentru Codex — Roslyn Repo Indexer

> Scop: implementează un indexer Roslyn simplu, performant și complet pentru daily usage, care poate funcționa ca motor de căutare local pentru întregul repository. Nu folosi AI embedded, embeddings, vector DB, LLM local/cloud sau motoare externe de căutare. Folosește Roslyn pentru indexarea semantică a codului .NET și un index inversat simplu, scris în cod, pentru căutare text.

> Format: checklist nebifat intenționat. Codex trebuie să parcurgă taskurile de sus în jos și să nu lase TODO-uri, stub-uri sau „implement later”.

---

## 0. Reguli obligatorii pentru implementare

- [x] Implementează totul ca un tool local .NET, fără server, fără daemon permanent obligatoriu, fără servicii externe.
- [x] Nu folosi modele AI, embeddings, vector search, ML.NET, OpenAI, Semantic Kernel, local LLM, cloud APIs sau librării similare.
- [x] Nu folosi ElasticSearch, Lucene, Meilisearch, Typesense, SQLite FTS sau alt search engine extern.
- [x] Nu trimite codul sursă în afara mașinii locale.
- [x] Nu introduce HTTP clients, telemetry, analytics, upload, sync, background network calls sau servicii externe.
- [x] Adaugă o verificare statică/test simplu care eșuează dacă proiectul tool-ului introduce dependențe evident AI/embedding/vector/HTTP/cloud.
- [x] Folosește Roslyn pentru partea semantică: soluții/proiecte/documente, syntax trees, semantic models, simboluri și referințe.
- [x] Folosește doar un index custom simplu pentru căutare: fișiere locale JSON/JSONL + dicționare în memorie.
- [x] Căutarea trebuie să funcționeze pentru tot repo-ul: cod C# semantic + text search pentru fișiere text non-C# relevante.
- [x] C# semantic indexing este obligatoriu și complet; non-C# text indexing este line-based, fără parser specializat.
- [x] CLI-ul trebuie să poată fi folosit ușor de Codex sau de un developer uman.
- [x] Toate comenzile importante trebuie să aibă output text human-readable și opțiune `--json` stabilă.
- [x] Orice eroare de workspace/proiect/document trebuie logată clar; tool-ul trebuie să continue cu index parțial când este sigur.
- [x] Nu lăsa cod duplicat mare, metode gigantice sau abstracții inutile; implementarea trebuie să rămână simplă.
- [x] Nu face optimizări premature complicate; prioritizează: corectitudine, incrementalitate simplă, căutare rapidă, teste.
- [x] Nu introduce threading agresiv; limitează paralelismul ca să nu explodeze memoria pe soluții mari.
- [x] Nu indexa `bin`, `obj`, `.git`, `.vs`, `.idea`, `.vscode`, `node_modules`, `.roslyn-index`, `TestResults`, `artifacts`, `packages`.
- [x] Nu indexa fișiere binare sau fișiere text foarte mari peste limita configurată.
- [x] Nu scrie în afara repository-ului decât dacă userul cere explicit.
- [x] Nu modifica sursa repo-ului indexat, în afară de adăugarea proiectului/tool-ului de indexare și a fișierelor lui de test.

---

## 1. Rezultat final așteptat

- [x] Creează un tool numit `ri` sau `roslyn-indexer`, cu proiect executabil packable ca .NET tool.
- [x] Tool-ul trebuie să poată fi rulat din orice subfolder al repo-ului și să detecteze automat root-ul.
- [x] Tool-ul trebuie să construiască indexul în folderul `.roslyn-index/` de la root-ul repo-ului.
- [x] Tool-ul trebuie să poată face full index la prima rulare.
- [x] Tool-ul trebuie să poată face incremental index la rulările următoare.
- [x] Tool-ul trebuie să poată căuta simboluri, fișiere, text, referințe semantice aproximativ-indexate și referințe exacte on-demand.
- [x] Tool-ul trebuie să poată sugera query-uri deterministe pentru agenți AI prin `ri suggest`, fără AI embedded.
- [x] Tool-ul trebuie să poată diagnostica mediul prin `ri doctor`.
- [x] Tool-ul trebuie să poată afișa rezultat cu path, line, column, kind, score, match reason și snippet.
- [x] Tool-ul trebuie să suporte JSON output pentru integrare cu Codex.
- [x] Tool-ul trebuie să includă teste unitare, teste de integrare și teste CLI.
- [x] Tool-ul trebuie să includă un README scurt cu usage real.
- [x] `dotnet build` trebuie să treacă.
- [x] `dotnet test` trebuie să treacă.
- [x] Nu trebuie să existe TODO-uri rămase în cod sau teste.

---

## 2. Structura recomandată în repo

- [x] Adaugă folderul `tools/RoslynRepoIndexer/`.
- [x] Creează soluția `tools/RoslynRepoIndexer/RoslynRepoIndexer.sln`.
- [x] Creează proiectul `tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/RoslynRepoIndexer.Core.csproj`.
- [x] Creează proiectul `tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Cli/RoslynRepoIndexer.Cli.csproj`.
- [x] Creează proiectul `tools/RoslynRepoIndexer/tests/RoslynRepoIndexer.Tests/RoslynRepoIndexer.Tests.csproj`.
- [x] Adaugă toate proiectele în soluție.
- [x] Fă `RoslynRepoIndexer.Cli` să refere `RoslynRepoIndexer.Core`.
- [x] Fă `RoslynRepoIndexer.Tests` să refere `RoslynRepoIndexer.Core` și, unde este util, să ruleze CLI-ul ca proces.
- [x] Dacă repo-ul folosește `Directory.Packages.props`, adaugă versiunile pachetelor acolo.
- [x] Dacă repo-ul nu folosește Central Package Management, pune versiunile direct în `.csproj`.

### Pachete runtime permise

- [x] Adaugă `Microsoft.CodeAnalysis.CSharp.Workspaces`.
- [x] Adaugă `Microsoft.CodeAnalysis.Workspaces.MSBuild`.
- [x] Adaugă `Microsoft.Build.Locator`.
- [x] Dacă repo-ul nu are deja pinning central, folosește versiunile stabile curente verificate la data planului: `Microsoft.CodeAnalysis.CSharp.Workspaces` `5.6.0`, `Microsoft.CodeAnalysis.Workspaces.MSBuild` `5.6.0`, `Microsoft.Build.Locator` `1.11.2`.
- [x] Dacă repo-ul are deja pachete Roslyn pinuite, aliniază versiunile ca să nu introduci conflict între proiecte.
- [x] Nu adăuga pachete runtime pentru CLI parsing; implementează parser simplu manual.
- [x] Nu adăuga pachete runtime pentru logging; folosește `Console.Error`, JSONL log și clase simple interne.
- [x] Nu adăuga pachete runtime pentru storage; folosește `System.Text.Json`, `FileStream`, `StreamReader`, `StreamWriter`.
- [x] Nu adăuga pachete runtime pentru HTTP, telemetry, AI, embeddings sau vector search.

### Pachete doar pentru teste

- [x] Adaugă `Microsoft.NET.Test.Sdk`.
- [x] Adaugă `xunit`.
- [x] Adaugă `xunit.runner.visualstudio`.
- [x] Nu adăuga framework de assertion separat decât dacă repo-ul îl folosește deja.

---

## 3. Target framework și setup proiect

- [x] Targetează `net8.0` minim pentru proiectele tool-ului, dacă repo-ul permite.
- [x] Dacă repo-ul impune `net9.0` sau `net10.0`, aliniază tool-ul la targetul standard al repo-ului.
- [x] În proiectul CLI, setează `OutputType` la `Exe`.
- [x] În proiectul CLI, setează `PackAsTool` la `true`.
- [x] În proiectul CLI, setează `ToolCommandName` la `ri`.
- [x] Activează `Nullable` în toate proiectele noi.
- [x] Activează `ImplicitUsings` în toate proiectele noi.
- [x] Setează `TreatWarningsAsErrors` la `true` pentru proiectele noi dacă repo-ul permite.
- [x] Evită referințe directe la `Microsoft.Build.*` runtime în output, cu excepția `Microsoft.Build.Locator`.
- [x] Înainte de orice folosire de API-uri MSBuild, apelează `MSBuildLocator.RegisterDefaults()` într-un punct izolat de startup.
- [x] Implementează `ri --version`.
- [x] Implementează `ri --help` și help per comandă.
- [x] Asigură-te că proiectul CLI poate fi instalat ca local/global `dotnet tool`.

---

## 4. CLI — comenzi obligatorii

### `ri index`

- [x] Implementează `ri index [path]`.
- [x] Dacă `path` lipsește, folosește current directory.
- [x] Detectează repo root pornind de la `path`.
- [x] Construiește sau actualizează indexul din `.roslyn-index/`.
- [x] Default: incremental index dacă există manifest valid.
- [x] Suportă `--force` pentru full rebuild.
- [x] Suportă `--json` pentru sumar machine-readable.
- [x] Suportă `--include-generated` pentru source-generated documents Roslyn, default `false`.
- [x] Suportă `--include-non-csharp-text true|false`, default `true`.
- [x] Suportă `--max-text-file-bytes <bytes>`, default `1048576`.
- [x] Suportă `--max-degree-of-parallelism <n>`, default `min(Environment.ProcessorCount, 4)`.
- [x] Suportă `--config <file>`, default `.roslyn-index.json` dacă există.
- [x] La final, afișează: repo root, soluții/proiecte detectate, docs indexate, docs skipped, simboluri, referințe, tokens, durată, warning count.

### `ri search`

- [x] Implementează `ri search <query>`.
- [x] Dacă indexul lipsește, afișează mesaj clar: rulează `ri index`.
- [x] Caută în simboluri, text și fișiere.
- [x] Suportă `--mode all|symbol|text|file|reference`, default `all`.
- [x] Suportă `--kind <kind1,kind2>` pentru simboluri: `namespace,type,class,record,struct,interface,enum,delegate,method,constructor,property,indexer,event,field,enum-member,operator,local-function,parameter,local`.
- [x] Suportă `--path <substring-or-glob-lite>`.
- [x] Suportă `--project <name-or-path-substring>`.
- [x] Suportă `--from-file <path>` pentru context-aware ranking.
- [x] Suportă `--from-project <projectName>` pentru context-aware ranking.
- [x] Suportă `--include-tests` și `--exclude-tests`.
- [x] Suportă `--include-generated` pentru search explicit în fișiere generate indexate.
- [x] Suportă `--limit <n>`, default `50`.
- [x] Suportă `--json`.
- [x] Suportă query cu ghilimele pentru phrase search simplu: `"Customer Service"`.
- [x] Suportă token search case-insensitive by default.
- [x] Suportă exact symbol search dacă query-ul arată ca FQN: `Namespace.Type.Member`.
- [x] Rezultatele trebuie sortate stabil: score desc, path asc, line asc, column asc.

### `ri suggest`

- [x] Implementează `ri suggest <natural-language-question>`.
- [x] Scop: traduce întrebări naturale în sugestii deterministe de comenzi `ri search`, `ri goto` și `ri refs`.
- [x] Nu executa automat sugestiile în varianta default; doar propune comenzile.
- [x] Nu folosi AI, embeddings, LLM, vector DB, servicii externe sau modele locale.
- [x] Folosește doar indexul local existent: simboluri, tokens, path-uri, referințe și metadata de proiect.
- [x] Suportă `--json`.
- [x] Suportă `--limit <n>`, default `5`.
- [x] Suportă `--execute-top <n>` opțional, default `0`, pentru a rula primele N sugestii și a returna rezultate combinate.
- [x] Detectează intenții simple:
  - [x] „unde este definit X?” / „where is X defined?” => sugerează `ri goto X`.
  - [x] „cine folosește X?” / „where is X used?” / „unde e apelat X?” => sugerează `ri refs X`.
  - [x] „unde se face X?” / „how is X done?” => sugerează `ri search` cu tokenuri extrase.
  - [x] „config/settings/options” => boost pe config files, options classes și path-uri relevante.
  - [x] „controller/endpoint/route/api” => boost pe Controllers, Minimal APIs și route-like code.
  - [x] „test/spec/fixture” => include și boost pe proiecte/fișiere de test.
- [x] Extrage tokenuri cu același `Tokenizer` folosit de search.
- [x] Elimină stopwords română/engleză: `unde`, `care`, `cum`, `cine`, `este`, `sunt`, `se`, `face`, `găsește`, `find`, `where`, `how`, `what`, `who`, `is`, `are`, `the`, `a`, `an`, `to`, `of`.
- [x] Păstrează termeni code-like: CamelCase, PascalCase, snake_case, kebab-case, quoted phrases, FQN-uri și identificatori cu `.`.
- [x] Mapează sinonime simple și deterministe:
  - [x] `login`, `auth`, `authentication`, `authorize`, `jwt`, `token`.
  - [x] `config`, `settings`, `options`.
  - [x] `db`, `database`, `repository`, `context`, `DbContext`.
  - [x] `endpoint`, `controller`, `route`, `api`.
  - [x] `validate`, `validation`, `validator`.
  - [x] `serialize`, `json`, `deserialize`.
  - [x] `save`, `persist`, `store`, `insert`, `update`.
- [x] Pentru fiecare sugestie returnează: `command`, `query`, `mode`, `confidence`, `reason`, `expectedResultKind`.
- [x] Sortează sugestiile determinist după `confidence desc`, apoi `command asc`.
- [x] Dacă indexul lipsește, returnează mesaj clar: rulează `ri index`.

### `ri refs`

- [x] Implementează `ri refs <symbol-query>`.
- [x] Caută întâi simbolul în indexul local.
- [x] Dacă există mai multe simboluri candidate, afișează lista de candidați și cere `--symbol-id` pentru dezambiguizare.
- [x] Suportă `--symbol-id <id>`.
- [x] Suportă `--exact` pentru referințe exacte via Roslyn `SymbolFinder.FindReferencesAsync` on-demand.
- [x] Default: folosește referințele semantice indexate, apoi recomandă `--exact` dacă rezultatul poate fi ambiguu.
- [x] Suportă `--json`.
- [x] Afișează path, line, column, snippet și kind-ul referinței.

### `ri goto`

- [x] Implementează `ri goto <symbol-query>`.
- [x] Returnează declarațiile potrivite.
- [x] Suportă `--json`.
- [x] Suportă `--limit`, default `20`.
- [x] Pentru overload-uri, afișează semnătura completă.

### `ri symbols`

- [x] Implementează `ri symbols`.
- [x] Suportă `--prefix <prefix>`.
- [x] Suportă `--contains <text>`.
- [x] Suportă `--kind <kind1,kind2>`.
- [x] Suportă `--json`.
- [x] Suportă `--limit`, default `100`.

### `ri doctor`

- [x] Implementează `ri doctor [path]`.
- [x] Detectează repo root.
- [x] Detectează `.sln`, `.slnx` și `.csproj` disponibile.
- [x] Detectează SDK-urile .NET instalate, dacă pot fi citite fără build.
- [x] Verifică dacă MSBuild poate fi localizat și înregistrat prin `Microsoft.Build.Locator`.
- [x] Verifică dacă `MSBuildWorkspace` poate deschide soluția/proiectele selectate.
- [x] Raportează proiecte unsupported sau care nu se pot încărca.
- [x] Raportează directoare și fișiere skip-uite de configurare.
- [x] Raportează dacă `.roslyn-index/` există și dacă schema este compatibilă.
- [x] Nu modifică indexul și nu scrie fișiere, cu excepția outputului către stdout/stderr.
- [x] Suportă `--json`.
- [x] Returnează diagnostics machine-readable: `checks`, `status`, `message`, `severity`, `details`.

### `ri status`

- [x] Implementează `ri status [path]`.
- [x] Arată dacă indexul există.
- [x] Arată schema version.
- [x] Arată repo root indexat.
- [x] Arată data ultimei indexări.
- [x] Arată numărul de documente, simboluri, referințe, tokens.
- [x] Arată câte fișiere par dirty față de manifest.
- [x] Arată dacă indexul este stale, missing, valid, corrupt sau schema-incompatible.
- [x] Arată ultimele warning-uri relevante.
- [x] Suportă `--json`.
- [x] Nu pornește Roslyn/MSBuild; trebuie să folosească doar filesystem + manifest.

### `ri clean`

- [x] Implementează `ri clean [path]`.
- [x] Șterge folderul `.roslyn-index/` doar din repo root detectat.
- [x] Cere `--yes` pentru ștergere fără confirmare interactivă.
- [x] Nu șterge nimic dacă repo root nu este detectat sigur.

---

## 5. Coduri de exit obligatorii

- [x] Returnează `0` pentru succes.
- [x] Returnează `1` pentru user/input error: comandă invalidă, argumente invalide, query lipsă, path invalid.
- [x] Returnează `2` pentru repo/project/workspace loading error critic.
- [x] Returnează `3` pentru index unavailable, missing, corrupt sau schema-incompatible când comanda cere index existent.
- [x] Returnează `4` pentru internal error neașteptat.
- [x] Returnează `5` pentru timeout/cancelled.
- [x] `ri doctor` poate returna `0` dacă poate produce diagnostics chiar dacă unele checks sunt warning/fail; folosește exit non-zero doar când doctor însuși nu poate rula.
- [x] În `--json`, include mereu `exitCode`, `success`, `warnings`, `errors`.
- [x] Documentează codurile de exit în README.
- [x] Testează codurile de exit pentru failure modes comune.

---

## 6. Config file `.roslyn-index.json`

- [x] Dacă fișierul există în repo root, citește-l automat.
- [x] Dacă fișierul nu există, folosește defaulturi interne.
- [x] Suportă JSON cu schema simplă:

```json
{
  "solution": null,
  "includeGenerated": false,
  "includeNonCSharpText": true,
  "maxTextFileBytes": 1048576,
  "maxDegreeOfParallelism": 4,
  "searchResultLimit": 50,
  "suggestionLimit": 5,
  "exactRefsTimeoutSeconds": 30,
  "excludeDirectories": [
    ".git",
    "bin",
    "obj",
    ".vs",
    ".idea",
    ".vscode",
    "node_modules",
    ".roslyn-index",
    "TestResults",
    "artifacts",
    "packages"
  ],
  "excludeFileSuffixes": [
    ".dll",
    ".exe",
    ".pdb",
    ".png",
    ".jpg",
    ".jpeg",
    ".gif",
    ".webp",
    ".ico",
    ".pdf",
    ".zip",
    ".7z",
    ".tar",
    ".gz"
  ]
}
```

- [x] Validează configul și afișează warning clar pentru proprietăți necunoscute sau valori invalide.
- [x] Nu pica dacă lipsește o proprietate; folosește default.
- [x] Nu implementa globbing complex; pentru `excludeDirectories`, compară path segment case-insensitive pe Windows și case-sensitive pe Linux/macOS.
- [x] Pentru `excludeFileSuffixes`, compară extensii case-insensitive.

---

## 7. Descoperirea repo-ului

- [x] Creează clasa `RepositoryDiscovery`.
- [x] Pornind de la path-ul primit sau current directory, urcă până găsești `.git`.
- [x] Dacă nu există `.git`, urcă până găsești `.sln`, `.slnx` sau `.csproj`.
- [x] Dacă nu se găsește nimic relevant, returnează eroare cu mesaj explicit.
- [x] Normalizează root path la full path fără separator final.
- [x] Păstrează path-uri relative la repo root în index.
- [x] Pentru enumerare text non-C#, folosește `git ls-files -co --exclude-standard` dacă `.git` există și `git` este disponibil.
- [x] Dacă `git ls-files` eșuează, fallback la `Directory.EnumerateFiles` cu excluderile configurate.
- [x] Nu include fișiere din `.roslyn-index/` în index.

---

## 8. Descoperirea soluțiilor și proiectelor

- [x] Creează clasa `WorkspaceDiscovery`.
- [x] Dacă configul specifică `solution`, folosește soluția respectivă.
- [x] Dacă există exact o soluție `.sln` sau `.slnx` în root, folosește-o.
- [x] Dacă există mai multe soluții în root, indexează toate și dedupează documentele după `fullPath + projectContext`.
- [x] Dacă nu există soluții în root, caută recursiv `.sln` și `.slnx`, excluzând directoarele ignorate.
- [x] Dacă nu există soluții, caută recursiv `.csproj`, excluzând directoarele ignorate.
- [x] Deschide soluțiile cu `MSBuildWorkspace.OpenSolutionAsync`.
- [x] Deschide proiectele standalone cu `MSBuildWorkspace.OpenProjectAsync`.
- [x] Atașează handler la `workspace.WorkspaceFailed` și colectează warnings/errors.
- [x] Dacă un proiect nu se poate încărca, loghează și continuă cu restul proiectelor.
- [x] Dacă niciun document C# nu poate fi încărcat, eșuează cu exit code `4`.
- [x] Pentru fiecare proiect, păstrează `ProjectId`, `Name`, `FilePath`, `Language`, target framework/context dacă este disponibil.
- [x] Dedupează documentele linkuite: același `FilePath` poate apărea în mai multe proiecte; păstrează fiecare context semantic, dar evită duplicate în text index.

---

## 9. Modele de date interne

- [x] Creează record `IndexManifest`.
- [x] Creează record `ProjectEntry`.
- [x] Creează record `DocumentEntry`.
- [x] Creează record `SymbolEntry`.
- [x] Creează record `ReferenceEntry`.
- [x] Creează record `TokenPosting`.
- [x] Creează record `SearchResult`.
- [x] Creează record `QuerySuggestion`.
- [x] Creează record `CommandResponse<T>` pentru output JSON uniform.
- [x] Creează record `IndexDiagnostics`.

### `IndexManifest`

- [x] Include `SchemaVersion`.
- [x] Include `ToolVersion`.
- [x] Include `RepoRoot`.
- [x] Include `CreatedUtc`.
- [x] Include `UpdatedUtc`.
- [x] Include `ConfigHash`.
- [x] Include `WorkspaceInputsHash`.
- [x] Include lista de soluții/proiecte indexate.
- [x] Include map `DocumentsByRelativePath` cu `DocumentState`.
- [x] Include counters: `DocumentCount`, `SymbolCount`, `ReferenceCount`, `TokenCount`, `WarningCount`.

### `DocumentEntry`

- [x] Include `DocumentId` intern stabil.
- [x] Include `ProjectId`.
- [x] Include `ProjectName`.
- [x] Include `RelativePath`.
- [x] Include `FullPath` doar transient, nu obligatoriu în index persistat.
- [x] Include `Language`.
- [x] Include `IsGenerated`.
- [x] Include `IsNonCSharpText`.
- [x] Include `LengthBytes`.
- [x] Include `LastWriteUtc`.
- [x] Include `ContentHash`.
- [x] Include `DeclarationHash` pentru C#.
- [x] Include `LineCount`.

### `SymbolEntry`

- [x] Include `SymbolId` stabil.
- [x] Include `DocumentId`.
- [x] Include `ProjectId`.
- [x] Include `Kind`.
- [x] Include `Name`.
- [x] Include `MetadataName`.
- [x] Include `FullyQualifiedName`.
- [x] Include `ContainerName`.
- [x] Include `Signature`.
- [x] Include `Accessibility`.
- [x] Include `Modifiers` relevante: `static`, `abstract`, `virtual`, `override`, `async`, `partial`, `readonly`, `required`.
- [x] Include `FilePath` relativ.
- [x] Include `StartLine`, `StartColumn`, `EndLine`, `EndColumn`.
- [x] Include `SpanStart`, `SpanLength`.
- [x] Include `IsDefinition`.
- [x] Include `IsPartial`.
- [x] Include `ParameterTypes` pentru overload-uri.
- [x] Include `ReturnType` pentru metode/proprietăți unde există.

### `ReferenceEntry`

- [x] Include `ReferenceId` sau compus `SymbolId + DocumentId + SpanStart`.
- [x] Include `SymbolId`.
- [x] Include `DocumentId`.
- [x] Include `ProjectId`.
- [x] Include `FilePath` relativ.
- [x] Include `StartLine`, `StartColumn`, `EndLine`, `EndColumn`.
- [x] Include `SpanStart`, `SpanLength`.
- [x] Include `ReferenceKind`: `read`, `write`, `invocation`, `type-use`, `attribute`, `object-creation`, `inheritance`, `unknown`.
- [x] Include `ReferencedName` pentru fallback text.

### `TokenPosting`

- [x] Include `Token` normalizat lowercase invariant.
- [x] Include `DocumentId`.
- [x] Include `FilePath` relativ.
- [x] Include `Line`.
- [x] Include `Column`.
- [x] Include `Weight`: `symbol-name`, `identifier`, `keyword`, `string`, `comment`, `path`, `text`.
- [x] Nu stoca snippets mari în posting; citește snippetul din fișier la afișare.

### `SearchResult`

- [x] Include `Kind`.
- [x] Include `Score`.
- [x] Include `MatchReason` explicit și scurt.
- [x] Include `SymbolId`, când există.
- [x] Include `SymbolName`, când există.
- [x] Include `ContainingType`, când există.
- [x] Include `FullyQualifiedName`, când există.
- [x] Include `ProjectName`.
- [x] Include `FilePath` relativ.
- [x] Include `StartLine`, `StartColumn`, `EndLine`, `EndColumn`.
- [x] Include `Snippet`, doar în output/render, nu obligatoriu în index.
- [x] Include `ReferenceKind`, când rezultatul este referință.

### `QuerySuggestion`

- [x] Include `Command` complet care poate fi rulat de Codex.
- [x] Include `Query`.
- [x] Include `Mode`.
- [x] Include `Confidence` între `0.0` și `1.0`.
- [x] Include `Reason`.
- [x] Include `ExpectedResultKind`.
- [x] Include `ExecutedResults` doar când `--execute-top` este folosit.

### `CommandResponse<T>`

- [x] Include `Success`.
- [x] Include `ExitCode`.
- [x] Include `Command`.
- [x] Include `Query`, când comanda are query.
- [x] Include `RepoRoot`.
- [x] Include `ElapsedMs`.
- [x] Include `IndexUpdatedUtc`, când există index.
- [x] Include `Results` sau payload specific comenzii.
- [x] Include `Warnings`.
- [x] Include `Errors`.

---

## 10. Persistența indexului

- [x] Creează folder `.roslyn-index/v1/`.
- [x] Scrie `manifest.json`.
- [x] Scrie `documents.jsonl`.
- [x] Scrie `symbols.jsonl`.
- [x] Scrie `references.jsonl`.
- [x] Scrie `tokens.jsonl` sau `token-postings.jsonl`.
- [x] Scrie `diagnostics.jsonl`.
- [x] Opțional, scrie cache pentru exact refs în `.roslyn-index/v1/exact-refs-cache/`, invalidat la schimbarea indexului.
- [x] Folosește `System.Text.Json` cu opțiuni explicite și stabile.
- [x] Scrie fișierele în `tmp-{guid}` și apoi fă replace atomic la nivel de folder sau fișier.
- [x] Nu lăsa index corupt dacă procesul se oprește la mijloc.
- [x] La citire, validează `SchemaVersion`.
- [x] Dacă schema e incompatibilă, cere `ri index --force` sau rebuild automat cu mesaj clar.
- [x] Nu serializa path-uri absolute în rezultatele persistate, cu excepția `RepoRoot` din manifest.
- [x] Normalizează separatorul de path în index la `/`.
- [x] Menține output determinist: sortează intrările după path, project, span.

---

## 11. Hashing și incremental indexing

- [x] Creează clasa `DocumentHasher`.
- [x] Pentru fiecare fișier, citește rapid `LengthBytes` și `LastWriteUtc`.
- [x] Dacă length și last write sunt identice cu manifestul, consideră fișierul unchanged fără recitire.
- [x] Dacă s-au schimbat, calculează `ContentHash` folosind SHA-256 din BCL.
- [x] Pentru C#, calculează `DeclarationHash` din lista de declarații: kind + fully-qualified name + semnătură + accessibility + modifiers.
- [x] Dacă doar body-ul unei metode s-a schimbat și `DeclarationHash` rămâne identic, reindexează doar documentul schimbat.
- [x] Dacă `DeclarationHash` s-a schimbat, marchează proiectul curent și proiectele dependente direct ca semantic dirty.
- [x] Dacă s-au schimbat `.sln`, `.slnx`, `.csproj`, `.props`, `.targets`, `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, `global.json`, `NuGet.config`, `packages.lock.json`, marchează full rebuild semantic.
- [x] Dacă s-au șters fișiere, elimină complet documentele, simbolurile, referințele și tokenurile lor.
- [x] Dacă s-au adăugat fișiere, indexează-le și actualizează manifestul.
- [x] Reindexează proiectele afectate când project references se schimbă.
- [x] Reindexează documentele/proiectele afectate când compilation options se schimbă.
- [x] Stochează `IndexSchemaVersion` și forțează full rebuild la schimbare de schema.
- [x] Adaugă tests pentru stale index prevention.
- [x] Dacă mai mult de 20% din documentele C# sunt dirty, fă full semantic rebuild pentru simplitate.
- [x] După orice incremental run, reconstruiește fișierele globale `symbols.jsonl`, `references.jsonl`, `tokens.jsonl` din segmentele actuale ca să eviți intrări stale.

---

## 12. Încărcarea Roslyn corectă

- [x] Creează `MSBuildRegistration` cu metodă statică `EnsureRegistered()`.
- [x] Apelează `MSBuildLocator.RegisterDefaults()` înainte de orice tip MSBuild.
- [x] Ține codul care folosește MSBuild în metode apelate după registrare.
- [x] Creează `MSBuildWorkspace` cu proprietăți rezonabile, inclusiv `LoadMetadataForReferencedProjects = true` dacă este disponibil și util.
- [x] Atașează `WorkspaceFailed`.
- [x] Încarcă `Solution` sau `Project` asincron.
- [x] Folosește `CancellationToken` în toate operațiile async.
- [x] Nu rula `dotnet build`, `dotnet test` sau scripturi ale repo-ului ca parte din indexare.
- [x] Nu pica dacă restore-ul nu este perfect; indexează cât se poate și loghează lipsurile.
- [x] Pentru source-generated docs, implementează doar dacă `--include-generated` este setat.

---

## 13. Colectarea documentelor

- [x] Din fiecare `Project`, citește `Documents`.
- [x] Ignoră documentele fără `FilePath`.
- [x] Ignoră documentele sub directoare excluse.
- [x] Ignoră documentele din `bin`/`obj`, chiar dacă Roslyn le expune accidental.
- [x] Include doar fișiere existente pe disk, cu excepția source-generated docs când sunt cerute explicit.
- [x] Pentru fiecare document, creează `DocumentEntry`.
- [x] Pentru același fișier fizic inclus în mai multe proiecte, creează documente semantice separate per proiect, dar un singur text entry pentru full-text deduplicat.
- [x] Pentru non-C# text, enumeră fișierele repo-ului prin git/fallback și exclude fișierele deja reprezentate ca C# documents.

---

## 14. Colectarea simbolurilor C#

- [x] Creează `SymbolCollector`.
- [x] Pentru fiecare document C# dirty, obține `SyntaxRoot`.
- [x] Pentru fiecare document C# dirty, obține un singur `SemanticModel` și reutilizează-l în acel document.
- [x] Vizitează nodurile de declarație și obține simbolul declarat cu `semanticModel.GetDeclaredSymbol(...)`.
- [x] Nu apela `GetDeclaredSymbol` pe fiecare nod arbitrar dacă nu este necesar; filtrează mai întâi după tipuri de nod relevante.
- [x] Indexează namespace-uri file-scoped și block-scoped.
- [x] Indexează clase.
- [x] Indexează record classes.
- [x] Indexează record structs.
- [x] Indexează structuri.
- [x] Indexează interfețe.
- [x] Indexează enum-uri.
- [x] Indexează delegate.
- [x] Indexează constructori.
- [x] Indexează destructori/finalizers.
- [x] Indexează metode.
- [x] Indexează local functions.
- [x] Indexează operatori.
- [x] Indexează conversion operators.
- [x] Indexează proprietăți.
- [x] Indexează indexers.
- [x] Indexează events.
- [x] Indexează fields.
- [x] Indexează enum members.
- [x] Indexează parameters.
- [x] Indexează locals unde Roslyn oferă simbol stabil; marchează-le `local`.
- [x] Indexează type parameters pentru tipuri/metode generice.
- [x] Indexează extension methods și marchează-le cu modifier/flag.
- [x] Indexează partial declarations ca intrări separate care au același FQN, dar locații diferite.
- [x] Indexează overload-uri ca intrări separate prin semnătură completă.
- [x] Pentru fiecare simbol, generează `SymbolId` stabil.
- [x] Preferă `DocumentationCommentId.CreateDeclarationId(symbol)` pentru simboluri publice/member-level unde returnează valoare.
- [x] Folosește `SymbolKey.Create(symbol, cancellationToken).ToString()` ca fallback semantic.
- [x] Pentru locals/parameters fără ID global, folosește fallback determinist: `projectId + relativePath + span + name + kind`.
- [x] Normalizează display string fără `global::` pentru UX, dar păstrează fully qualified form intern.
- [x] Nu include simboluri din metadata externă ca declarații repo.

---

## 15. Colectarea referințelor semantice indexate

- [x] Creează `ReferenceCollector`.
- [x] Nu rula `SymbolFinder.FindReferencesAsync` pentru fiecare simbol în timpul indexării; ar fi prea lent.
- [x] Pentru fiecare document C# dirty, folosește același `SemanticModel` ca la simboluri.
- [x] Vizitează noduri de folosire relevante: identifier names, generic names, member access names, qualified names, object creation types, invocation expressions, attribute syntax, base type syntax.
- [x] Pentru fiecare nod relevant, apelează `semanticModel.GetSymbolInfo(node, ct)`.
- [x] Dacă `Symbol` este null și există un singur `CandidateSymbol`, folosește candidatul cu flag `ambiguous` intern sau warning low-level.
- [x] Ignoră simbolurile care nu au nicio legătură cu repo-ul dacă nu există declarație locală indexată.
- [x] Mapează simbolul la `SymbolId` cu aceeași strategie ca la declarations.
- [x] Evită duplicatele prin cheie `symbolId + documentId + spanStart + spanLength`.
- [x] Clasifică reference kind simplu: invocation, object creation, attribute, inheritance, read, write, type-use, unknown.
- [x] Pentru read/write, folosește sintaxa părinte și operații simple; nu implementa dataflow complex.
- [x] Păstrează referințele aproximativ-indexate pentru căutări rapide.
- [x] Pentru exact references, implementează `ri refs --exact` cu `SymbolFinder.FindReferencesAsync` on-demand și cache opțional în index.

---

## 16. Indexarea textului și tokenizarea

- [x] Creează `Tokenizer`.
- [x] Tokenizarea trebuie să funcționeze pe C# și fișiere text.
- [x] Split pe whitespace, punctuație, operatori și separators.
- [x] Split suplimentar pe camelCase și PascalCase.
- [x] Split suplimentar pe snake_case și kebab-case.
- [x] Normalizează tokenurile la lowercase invariant.
- [x] Păstrează și forma întreagă pentru identificatori compuși: `CustomerService` produce `customerservice`, `customer`, `service`.
- [x] Pentru `IHttpClientFactory`, produce `ihttpclientfactory`, `http`, `client`, `factory`.
- [x] Include tokenuri de lungime 1 doar dacă sunt semnificative în cod: `i`, `x`, `y`, `T` pentru generics; altfel filtrează-le din text simplu.
- [x] Pentru C#, folosește tokens Roslyn ca să marchezi greutăți: identifier, keyword, string, comment.
- [x] Pentru non-C# text, folosește citire line-by-line.
- [x] Pentru path-uri, indexează segmentele de path și numele fișierului cu weight `path`.
- [x] Nu indexa fișiere binare: detectează NUL bytes în primii 8KB.
- [x] Nu indexa fișiere mai mari decât `maxTextFileBytes`, dar loghează skip.

---

## 17. Query suggestion engine determinist

- [x] Creează `SuggestionService`.
- [x] `SuggestionService` primește întrebarea naturală, indexul încărcat și opțiunile CLI.
- [x] Normalizează textul folosind `Tokenizer`.
- [x] Elimină stopwords română/engleză.
- [x] Păstrează frazele dintre ghilimele ca termeni cu prioritate.
- [x] Detectează tokenuri code-like și le tratează ca posibili identificatori.
- [x] Aplică sinonimele configurate sau hardcodate simplu.
- [x] Detectează intenția principală: definition, references, broad search, config, endpoint, tests, persistence, validation, serialization.
- [x] Generează 3-5 sugestii concrete, nu zeci.
- [x] Sugestiile trebuie să fie comenzi CLI complete, ușor de rulat de Codex.
- [x] Nu introduce explicații lungi în `reason`; maxim o propoziție scurtă.
- [x] Nu executa `ri search` intern decât dacă userul a cerut `--execute-top`.
- [x] Cu `--execute-top`, rulează doar comenzi index-based; nu rula `ri refs --exact` automat.
- [x] Rezultatul trebuie să fie deterministic pentru aceeași întrebare și același index.

---

## 18. Search engine simplu

- [x] Creează `IndexReader` care încarcă manifest + jsonl în memorie.
- [x] Creează `SearchService`.
- [x] Construiește dicționare în memorie:
  - [x] `symbolsById`.
  - [x] `symbolsByLowerName`.
  - [x] `symbolsByLowerFullyQualifiedName`.
  - [x] `tokenToPostings`.
  - [x] `referencesBySymbolId`.
  - [x] `documentsById`.
- [x] Nu ține snippets în memorie; citește linia din fișier la render.
- [x] Query parser simplu:
  - [x] Separă phrase query între ghilimele.
  - [x] Separă tokens normale.
  - [x] Recunoaște prefixe simple `kind:`, `path:`, `project:`, `mode:`.
  - [x] Nu implementa limbaj query complex cu operatori booleeni compleți.
- [x] Pentru symbol search:
  - [x] Exact FQN match are scor maxim.
  - [x] Exact simple name match are scor mare.
  - [x] Prefix simple name match are scor mediu-mare.
  - [x] Contains simple/FQN match are scor mediu.
  - [x] CamelCase acronym match are scor mediu.
  - [x] Token overlap cu nume/simbol are scor mic-mediu.
- [x] Pentru text search:
  - [x] Intersectează postings pentru toate tokenurile query unde este posibil.
  - [x] Dacă intersecția e goală, folosește union cu scor mai mic.
  - [x] Phrase search verifică linia/snippetul real din fișier înainte de rezultat.
- [x] Pentru file search:
  - [x] Caută în path segments și file name.
- [x] Pentru reference search:
  - [x] Găsește simboluri candidate, apoi citește `referencesBySymbolId`.
- [x] Pentru context-aware search:
  - [x] Dacă există `--from-file`, detectează proiectul documentului și boostează același proiect.
  - [x] Dacă există `--from-project`, boostează proiectul respectiv.
  - [x] Boosteză proiectele legate prin project references.
  - [x] Penalizează test projects default, exceptând query-uri de test sau `--include-tests`.
  - [x] Exclude test projects când `--exclude-tests` este setat.
- [x] Deduplicate results după `path + line + column + kind + symbolId`.
- [x] Sortează stabil.
- [x] Limitează la `--limit`, dar calculează suficient intern ca să ai rezultate bune după filtrare.

---

## 19. Scoring recomandat

- [x] Pornește scorul de la `0`.
- [x] Exact FQN symbol match: `+1000`.
- [x] Exact simple symbol name: `+800`.
- [x] Prefix symbol name: `+600`.
- [x] CamelCase acronym match: `+500`.
- [x] Contains symbol/FQN: `+350`.
- [x] Token match în symbol name: `+250` per token.
- [x] Token match în path: `+120` per token.
- [x] Token match în identifier: `+100` per token.
- [x] Token match în keyword: `+60` per token.
- [x] Token match în string/comment/text: `+40` per token.
- [x] Phrase match exact în linie: `+300`.
- [x] Boost pentru același proiect când `--from-file`/`--from-project` este folosit: `+120`.
- [x] Boost pentru proiecte direct referențiate/referențiate de context: `+60`.
- [x] Penalizare pentru test projects: `-80`, cu excepția query-urilor explicit test/spec/fixture sau `--include-tests`.
- [x] Penalizare pentru fișier generated: `-100`, dacă include-generated este activ.
- [x] Penalizare pentru path foarte adânc sau vendor-like dacă nu a fost exclus: `-20`.
- [x] Fiecare scor trebuie să producă un `MatchReason` scurt: `exact-fqn`, `exact-symbol`, `prefix-symbol`, `token-overlap`, `path-match`, `reference-match`, `phrase-match`, `context-boost`.
- [x] Păstrează scoringul într-o singură clasă `SearchScorer` și testează-l separat.
- [x] Adaugă teste pentru ranking order și match reasons.

---

## 20. Output human-readable

- [x] Pentru fiecare rezultat, afișează o linie de titlu:

```text
[method] CustomerService.GetCustomerAsync(int id)  src/App/Services/CustomerService.cs:42:17  score=920
```

- [x] Afișează snippet pe linia următoare:

```text
    public Task<Customer> GetCustomerAsync(int id)
```

- [x] Pentru simboluri, include container/FQN când nu e redundant.
- [x] Pentru referințe, include `ref-kind`.
- [x] Pentru rezultate multe, afișează `showing N of M`.
- [x] Pentru warnings, afișează sumar pe stderr, nu amesteca în stdout când `--json` este folosit.

---

## 21. Output JSON stabil și contract pentru agenți AI

- [x] Toate comenzile cu `--json` emit un singur obiect JSON valid, nu JSONL.
- [x] Nu scrie text human-readable în stdout când `--json` este activ.
- [x] Warnings și logs merg în câmpul JSON `warnings`; stderr poate primi doar erori fatale non-JSON.
- [x] Definește un contract comun pentru toate comenzile:

```json
{
  "success": true,
  "exitCode": 0,
  "command": "search",
  "query": "CustomerService",
  "repoRoot": "/absolute/path",
  "elapsedMs": 12,
  "indexUpdatedUtc": "2026-07-04T00:00:00Z",
  "results": [],
  "warnings": [],
  "errors": []
}
```

- [x] Pentru `ri search --json`, rezultatele trebuie să includă:
  - [x] `filePath`.
  - [x] `startLine`.
  - [x] `startColumn`.
  - [x] `endLine`.
  - [x] `endColumn`.
  - [x] `kind`.
  - [x] `symbolId`, când există.
  - [x] `symbolName`, când există.
  - [x] `containingType`, când există.
  - [x] `fullyQualifiedName`, când există.
  - [x] `projectName`.
  - [x] `score`.
  - [x] `matchReason`.
  - [x] `snippet`.
- [x] Exemplu `ri search --json`:

```json
{
  "success": true,
  "exitCode": 0,
  "command": "search",
  "query": "CustomerService",
  "mode": "all",
  "repoRoot": "/absolute/path",
  "elapsedMs": 12,
  "indexUpdatedUtc": "2026-07-04T00:00:00Z",
  "totalMatches": 2,
  "results": [
    {
      "kind": "method",
      "score": 920,
      "matchReason": "exact-symbol",
      "symbolId": "...",
      "symbolName": "GetCustomerAsync",
      "containingType": "CustomerService",
      "fullyQualifiedName": "MyApp.Services.CustomerService.GetCustomerAsync(int)",
      "projectName": "MyApp",
      "filePath": "src/App/Services/CustomerService.cs",
      "startLine": 42,
      "startColumn": 17,
      "endLine": 42,
      "endColumn": 61,
      "snippet": "public Task<Customer> GetCustomerAsync(int id)",
      "referenceKind": null
    }
  ],
  "warnings": [],
  "errors": []
}
```

- [x] Pentru `ri suggest --json`, rezultatele trebuie să includă:
  - [x] `command`.
  - [x] `query`.
  - [x] `mode`.
  - [x] `confidence`.
  - [x] `reason`.
  - [x] `expectedResultKind`.
- [x] Exemplu `ri suggest --json`:

```json
{
  "success": true,
  "exitCode": 0,
  "command": "suggest",
  "query": "unde se validează tokenul JWT?",
  "repoRoot": "/absolute/path",
  "elapsedMs": 8,
  "results": [
    {
      "command": "ri search jwt validation token --mode all --json",
      "query": "jwt validation token",
      "mode": "all",
      "confidence": 0.86,
      "reason": "matched auth and validation terms",
      "expectedResultKind": "method-or-class"
    }
  ],
  "warnings": [],
  "errors": []
}
```

- [x] Pentru `ri index --json`, include counters și timings: `discoveryMs`, `workspaceLoadMs`, `semanticIndexMs`, `textIndexMs`, `persistMs`, `totalMs`.
- [x] Pentru `ri refs --json`, include candidate ambiguity dacă există.
- [x] Pentru `ri doctor --json`, include lista de checks cu `name`, `status`, `severity`, `message`, `details`.
- [x] Pentru `ri status --json`, include `indexState`: `missing`, `valid`, `stale`, `corrupt`, `schema-incompatible`.
- [x] Nu schimba numele câmpurilor după ce sunt introduse; adaugă câmpuri noi fără breaking change.
- [x] Adaugă snapshot tests pentru forma JSON a fiecărei comenzi.

---

## 22. Performanță pentru daily usage

- [x] Search-ul din index existent nu trebuie să pornească Roslyn.
- [x] Search-ul trebuie doar să citească fișierele indexului și liniile necesare pentru snippets.
- [x] `ri status` nu trebuie să pornească Roslyn.
- [x] `ri clean` nu trebuie să pornească Roslyn.
- [x] `ri index` este singura comandă care pornește workspace Roslyn, cu excepția `ri refs --exact`.
- [x] Nu păstra `SemanticModel` în cache global după ce documentul a fost procesat.
- [x] Obține un singur `SemanticModel` per document procesat și reutilizează-l pentru declarații + referințe.
- [x] Nu construi `Compilation` separat pentru fiecare nod.
- [x] Procesează proiectele secvențial sau cu paralelism limitat.
- [x] Procesează documentele cu paralelism limitat și configurabil.
- [x] Folosește streaming IO pentru fișiere JSONL.
- [x] Nu serializa obiecte Roslyn.
- [x] Nu ține `Solution`, `Project`, `Compilation`, `SemanticModel` în indexul persistat.
- [x] Pentru rezultate, citește snippetul direct din fișier doar pentru top results, nu pentru toate candidatele.
- [x] Măsoară durate pentru: discovery, workspace load, semantic index, text index, persist, search load, search score.
- [x] Scrie aceste timings în diagnostics și în outputul `ri index --json`.

---

## 23. Performance budgets și benchmark smoke tests

- [x] Definește clase de repo pentru testare și documentare:
  - [x] small: sub 500 fișiere.
  - [x] medium: 500-5.000 fișiere.
  - [x] large: 5.000-25.000 fișiere.
- [x] Definește bugete măsurabile în README/config pentru:
  - [x] cold index.
  - [x] warm incremental index fără schimbări.
  - [x] warm incremental index după o schimbare de fișier.
  - [x] query latency pentru `ri search`.
  - [x] query latency pentru `ri goto`.
  - [x] query latency pentru `ri suggest`.
  - [x] approximate refs latency.
  - [x] exact refs latency cu timeout.
  - [x] memorie maximă aproximativă.
- [x] Nu pune praguri fragile în testele unitare normale.
- [x] Pune benchmark/smoke tests separate, relaxate, care rulează robust în CI.
- [x] Fiecare comandă trebuie să raporteze `elapsedMs`.
- [x] `ri search`, `ri goto`, `ri symbols`, `ri status`, `ri suggest` nu trebuie să pornească Roslyn/MSBuild.
- [x] `ri refs --exact` trebuie să aibă timeout configurabil și cancellation token.

---

## 24. Robustețe și edge cases

- [x] Funcționează pe Windows, Linux și macOS.
- [x] Normalizează path-uri cu `/` în index.
- [x] Compară path-uri case-insensitive pe Windows și case-sensitive pe Linux/macOS.
- [x] Suportă repo-uri cu spații în path.
- [x] Suportă fișiere cu UTF-8 BOM.
- [x] Suportă fișiere cu CRLF și LF.
- [x] Suportă cod care nu compilează complet, cât timp Roslyn poate produce syntax/semantic parțial.
- [x] Suportă proiecte unloadable parțial: log + continuă.
- [x] Suportă multi-targeting prin context de proiect separat.
- [x] Suportă linked files prin document semantic separat și text dedupe.
- [x] Suportă partial classes și partial methods.
- [x] Suportă top-level statements.
- [x] Suportă global usings.
- [x] Suportă file-scoped namespaces.
- [x] Suportă nullable annotations în display string.
- [x] Suportă generics și nested types în FQN.
- [x] Suportă overload-uri și operators.
- [x] Suportă extension methods.
- [x] Suportă records și primary constructors.
- [x] Suportă collection expressions și sintaxă C# modernă prin Roslyn curent.
- [x] Nu crapă pe fișiere generate mari; le exclude default.
- [x] Nu crapă pe path-uri lungi.
- [x] Nu crapă dacă indexul este șters în timp ce rulează search; afișează eroare clară.

---

## 25. Clase recomandate în `Core`

- [x] `RepositoryDiscovery` — detectează root și enumeră fișiere.
- [x] `IndexerConfig` — model config + defaulturi + validare.
- [x] `ConfigLoader` — citește `.roslyn-index.json`.
- [x] `MSBuildRegistration` — izolează `MSBuildLocator`.
- [x] `WorkspaceDiscovery` — găsește soluții/proiecte.
- [x] `WorkspaceLoader` — deschide workspace/solution/project.
- [x] `DocumentHasher` — hash incremental.
- [x] `BinaryFileDetector` — detectează fișiere non-text.
- [x] `Tokenizer` — tokenizare text și identificatori.
- [x] `SymbolIdProvider` — generează ID-uri stabile.
- [x] `SymbolCollector` — colectează declarații.
- [x] `ReferenceCollector` — colectează referințe semantice rapide.
- [x] `TextIndexer` — indexează C# tokens + text non-C#.
- [x] `IndexBuilder` — orchestrează indexarea.
- [x] `IndexStore` — citește/scrie indexul.
- [x] `IndexReader` — încarcă indexul pentru search.
- [x] `QueryParser` — parsează query simplu.
- [x] `SearchScorer` — scorare.
- [x] `SearchService` — execută căutarea.
- [x] `ExactReferenceService` — folosește `SymbolFinder.FindReferencesAsync` on-demand.
- [x] `SnippetReader` — citește linii/snippets.
- [x] `DiagnosticsCollector` — warnings, errors, timings.
- [x] `JsonOutputWriter` — scrie JSON stabil.
- [x] `HumanOutputWriter` — scrie output text.

---

## 26. CLI parser simplu

- [x] Implementează parser manual în `RoslynRepoIndexer.Cli`.
- [x] Primul argument este comanda.
- [x] Restul argumentelor sunt poziționale sau opțiuni `--name value` / flags.
- [x] Suportă `--help` global.
- [x] Suportă `ri <command> --help`.
- [x] Pentru argumente invalide, afișează help scurt și exit code `2`.
- [x] Nu introduce librării pentru CLI.
- [x] Nu implementa subcomenzi ascunse.

---

## 27. Algoritm `ri index` — pași expliciți

- [x] Parsează argumentele.
- [x] Detectează repo root.
- [x] Încarcă config.
- [x] Calculează `ConfigHash`.
- [x] Încarcă manifestul existent dacă există și `--force` nu este setat.
- [x] Descoperă soluții/proiecte.
- [x] Calculează `WorkspaceInputsHash` din `.sln/.slnx/.csproj/.props/.targets` relevante.
- [x] Decide full vs incremental.
- [x] Înregistrează MSBuild cu `MSBuildLocator`.
- [x] Deschide workspace/solution/project.
- [x] Colectează documentele C#.
- [x] Enumeră fișierele text non-C#.
- [x] Calculează state pentru documente și fișiere.
- [x] Identifică added/changed/deleted/unchanged.
- [x] Pentru documente C# dirty, colectează syntax root și semantic model.
- [x] Colectează simboluri.
- [x] Colectează referințe semantice rapide.
- [x] Colectează tokenuri C#.
- [x] Pentru fișiere non-C# dirty, colectează tokenuri text.
- [x] Elimină intrările vechi pentru deleted/changed.
- [x] Combină intrările unchanged existente cu intrările noi.
- [x] Reconstruiește indexurile globale sorted/determinist.
- [x] Scrie indexul într-un temp folder.
- [x] Validează că fișierele scrise pot fi citite.
- [x] Fă replace atomic.
- [x] Afișează sumar.

---

## 28. Algoritm `ri suggest` — pași expliciți

- [x] Parsează argumentele.
- [x] Detectează repo root.
- [x] Verifică existența indexului.
- [x] Încarcă manifest + index files în memorie.
- [x] Normalizează întrebarea.
- [x] Extrage tokens, phrases și code-like identifiers.
- [x] Elimină stopwords.
- [x] Aplică sinonimele deterministe.
- [x] Detectează intenția.
- [x] Generează sugestii de comenzi.
- [x] Calculează confidence pentru fiecare sugestie.
- [x] Sortează determinist.
- [x] Dacă `--execute-top` > 0, execută doar top N comenzi index-based și atașează rezultate.
- [x] Scrie human output sau JSON.

---

## 29. Algoritm `ri doctor` — pași expliciți

- [x] Parsează argumentele.
- [x] Detectează repo root.
- [x] Citește configul dacă există.
- [x] Detectează soluții/proiecte.
- [x] Verifică MSBuild Locator.
- [x] Încearcă deschiderea workspace-ului într-un mod sigur, cu cancellation.
- [x] Colectează `WorkspaceFailed` diagnostics.
- [x] Verifică existența și schema indexului.
- [x] Verifică exclude directories și limitele configurate.
- [x] Scrie raport human-readable sau JSON.

---

## 30. Algoritm `ri status` — pași expliciți

- [x] Parsează argumentele.
- [x] Detectează repo root.
- [x] Verifică existența `.roslyn-index/`.
- [x] Citește manifestul fără Roslyn/MSBuild.
- [x] Verifică schema version.
- [x] Verifică fișiere index lipsă/corupte.
- [x] Calculează rapid dirty count pe baza length/last write/hash doar când este necesar.
- [x] Returnează state: missing, valid, stale, corrupt, schema-incompatible.
- [x] Scrie human output sau JSON.

---

## 31. Algoritm `ri search` — pași expliciți

- [x] Parsează argumentele.
- [x] Detectează repo root.
- [x] Verifică existența indexului.
- [x] Încarcă manifest + index files în memorie.
- [x] Parsează query.
- [x] Aplică filtre.
- [x] Rulează symbol search dacă mode permite.
- [x] Rulează text search dacă mode permite.
- [x] Rulează file search dacă mode permite.
- [x] Rulează reference search dacă mode permite.
- [x] Deduplicate.
- [x] Scorează și sortează.
- [x] Citește snippets doar pentru top results.
- [x] Scrie human output sau JSON.

---

## 32. Algoritm `ri refs --exact`

- [x] Parsează argumentele.
- [x] Detectează repo root.
- [x] Încarcă indexul pentru a găsi simbolul candidat.
- [x] Dacă simbolul este ambiguu, returnează candidați și oprește.
- [x] Înregistrează MSBuild.
- [x] Redeschide aceeași soluție/proiect din manifest.
- [x] Rezolvă simbolul în Roslyn folosind `SymbolKey` sau fallback după FQN + signature + location.
- [x] Rulează `SymbolFinder.FindReferencesAsync(symbol, solution, cancellationToken)`.
- [x] Transformă rezultatele în `SearchResult`.
- [x] Sortează după path/line/column.
- [x] Scrie output.
- [x] Opțional, cache-uiește rezultatul exact în `.roslyn-index/v1/exact-refs-cache/` cu invalidare după `UpdatedUtc`.

---

## 33. Strategie clară pentru referințe

- [x] `ri search` trebuie să fie rapid și index-based.
- [x] `ri refs <symbol>` folosește default referințe aproximative indexate.
- [x] `ri refs <symbol> --exact` redeschide soluția/proiectele cu Roslyn și folosește `SymbolFinder.FindReferencesAsync`.
- [x] `ri refs --exact` trebuie să suporte cancellation.
- [x] `ri refs --exact` trebuie să suporte timeout configurabil prin `--timeout <seconds>` și config `exactRefsTimeoutSeconds`.
- [x] Dacă timeout-ul este atins, returnează rezultate parțiale cu warning clar și exit code `5` doar dacă nu există niciun rezultat utilizabil.
- [x] Nu apela `SymbolFinder.FindReferencesAsync` în `ri search`, `ri suggest`, `ri goto`, `ri symbols` sau `ri status`.

---

## 34. Teste unitare obligatorii

- [x] Test `RepositoryDiscovery` detectează `.git` root.
- [x] Test `RepositoryDiscovery` detectează root fără `.git`, dar cu `.sln`.
- [x] Test excludere directoare `bin`, `obj`, `.roslyn-index`.
- [x] Test config default.
- [x] Test config invalid produce warning, nu crash.
- [x] Test `Tokenizer` pentru camelCase.
- [x] Test `Tokenizer` pentru PascalCase.
- [x] Test `Tokenizer` pentru snake_case.
- [x] Test `Tokenizer` pentru kebab-case.
- [x] Test `Tokenizer` pentru `IHttpClientFactory`.
- [x] Test `Tokenizer` păstrează token întreg + subtokenuri.
- [x] Test binary detector cu NUL bytes.
- [x] Test path normalization Windows-like și Unix-like.
- [x] Test `SearchScorer` exact FQN > exact simple > prefix > contains.
- [x] Test `QueryParser` cu phrase query.
- [x] Test `QueryParser` cu `kind:` și `path:`.
- [x] Test JSON serialization pentru manifest.
- [x] Test JSON output schema pentru search result.
- [x] Test deterministic sorting.
- [x] Test `SuggestionService` pentru întrebări de tip definition -> `ri goto`.
- [x] Test `SuggestionService` pentru întrebări de tip references -> `ri refs`.
- [x] Test `SuggestionService` pentru întrebări broad -> `ri search`.
- [x] Test stopword removal română/engleză.
- [x] Test synonym expansion auth/config/database/endpoints/validation/serialization/persistence.
- [x] Test confidence ordering deterministic.
- [x] Test că output JSON respectă contractul comun.
- [x] Test că `matchReason` este populat pentru rezultate.
- [x] Test că nu există referințe de proiect către HTTP/AI/vector packages.

---

## 35. Teste Roslyn/integration obligatorii

- [x] Creează fixture temporar cu o soluție minimală și un proiect C# SDK-style.
- [x] Indexează o clasă simplă și verifică `class` symbol.
- [x] Indexează namespace file-scoped.
- [x] Indexează namespace block-scoped.
- [x] Indexează record class.
- [x] Indexează record struct.
- [x] Indexează struct.
- [x] Indexează interface.
- [x] Indexează enum + enum members.
- [x] Indexează delegate.
- [x] Indexează constructor.
- [x] Indexează method async.
- [x] Indexează property.
- [x] Indexează indexer.
- [x] Indexează event.
- [x] Indexează field.
- [x] Indexează operator.
- [x] Indexează conversion operator.
- [x] Indexează local function.
- [x] Indexează parameters.
- [x] Indexează generic type și generic method.
- [x] Indexează nested type.
- [x] Indexează partial class în două fișiere.
- [x] Indexează overload-uri și verifică semnături diferite.
- [x] Indexează extension method și verifică flag/modifier.
- [x] Indexează top-level statements fără crash.
- [x] Indexează global usings fără crash.
- [x] Verifică referință semantică la metodă prin invocation.
- [x] Verifică referință semantică la tip prin object creation.
- [x] Verifică referință semantică la atribut.
- [x] Verifică referință semantică în inheritance/base type.
- [x] Verifică linked file în două proiecte.
- [x] Verifică proiect cu cod incomplet produce index parțial, nu crash.
- [x] Indexează attributes și verifică simbol/referință.
- [x] Indexează controllers ASP.NET Core minimal ca fixture text/symbol, fără a porni aplicația.
- [x] Indexează Minimal APIs cu top-level statements.
- [x] Indexează nullable reference types fără crash.
- [x] Indexează aliases și using aliases.
- [x] Indexează interface implementations și metode implementate.
- [x] Indexează multi-targeted project fără duplicate instabile.
- [x] Verifică `ri refs --exact` găsește referințe reale pentru un simbol simplu.

---

## 36. Teste incremental obligatorii

- [x] Full index creează manifest și toate fișierele indexului.
- [x] A doua rulare fără schimbări marchează 0 dirty documents.
- [x] Modificarea body-ului unei metode reindexează documentul, nu forțează full rebuild.
- [x] Modificarea numelui unei metode schimbă `DeclarationHash`.
- [x] Modificarea unei declarații marchează proiectul semantic dirty.
- [x] Adăugarea unui fișier C# adaugă document/simboluri/tokens.
- [x] Ștergerea unui fișier C# elimină document/simboluri/referințe/tokens.
- [x] Modificarea `.csproj` declanșează rebuild semantic.
- [x] Modificarea `Directory.Build.props` declanșează rebuild semantic.
- [x] Modificarea `Directory.Build.targets` declanșează rebuild semantic.
- [x] Modificarea `global.json` declanșează rebuild semantic.
- [x] Modificarea `NuGet.config` declanșează rebuild semantic.
- [x] Modificarea `packages.lock.json` declanșează rebuild semantic.
- [x] Modificarea project references declanșează reindexare proiecte afectate.
- [x] Schimbarea configului declanșează rebuild necesar.
- [x] Index corupt sau schema veche produce mesaj clar și rebuild/eroare controlată.

---

## 37. Teste search obligatorii

- [x] `ri search CustomerService` găsește clasa cu scor mare.
- [x] `ri search My.Namespace.CustomerService` găsește exact FQN cu scor maxim.
- [x] `ri search CS` găsește `CustomerService` prin acronym/camel case dacă implementat.
- [x] `ri search customer service` găsește tokenuri separate.
- [x] `ri search "Customer Service"` verifică phrase search în text.
- [x] `ri search --mode symbol --kind method GetCustomerAsync` returnează metode.
- [x] `ri search --mode file CustomerService.cs` returnează fișierul.
- [x] `ri search --path Services CustomerService` filtrează după path.
- [x] `ri goto CustomerService` returnează declarația.
- [x] `ri refs GetCustomerAsync` returnează referințele indexate.
- [x] `ri refs GetCustomerAsync --exact` returnează referințele exacte.
- [x] Rezultatele sunt sortate determinist.
- [x] `--limit 1` returnează un singur rezultat.
- [x] `--json` este JSON valid.
- [x] `ri suggest "unde se validează tokenul JWT?" --json` returnează sugestii `ri search` relevante.
- [x] `ri suggest "unde este definit CustomerService?" --json` sugerează `ri goto CustomerService`.
- [x] `ri suggest "cine folosește GetCustomerAsync?" --json` sugerează `ri refs GetCustomerAsync`.
- [x] `ri search CustomerService --from-file <path>` boostează rezultate din proiectul curent.
- [x] `ri search CustomerService --exclude-tests` exclude proiecte de test.

---

## 38. Teste CLI obligatorii

- [x] `ri --help` returnează exit code 0.
- [x] `ri index --help` returnează exit code 0.
- [x] Comandă necunoscută returnează exit code 2.
- [x] Argument invalid returnează exit code 2.
- [x] `ri search` fără query returnează exit code 2.
- [x] `ri search query` fără index returnează exit code 3.
- [x] `ri clean --yes` șterge indexul.
- [x] `ri status` înainte de index afișează că indexul lipsește.
- [x] `ri status` după index afișează counters.
- [x] `ri doctor --json` returnează checks machine-readable.
- [x] `ri suggest` fără întrebare returnează exit code 1.
- [x] `ri suggest question` fără index returnează exit code 3.
- [x] `ri --version` returnează exit code 0.
- [x] Timeout controlat returnează exit code 5 unde este aplicabil.
- [x] CLI nu scrie warnings în stdout când `--json` este activ.

---

## 39. Teste performance/smoke obligatorii

- [x] Generează în test un repo temporar cu minim 200 fișiere C# simple.
- [x] Full index trebuie să termine fără out-of-memory și cu counters corecți.
- [x] Search după index nu trebuie să pornească MSBuild/Roslyn; testează printr-un seam/mock simplu sau prin separarea serviciilor.
- [x] `ri suggest`, `ri goto`, `ri symbols` și `ri status` după index nu trebuie să pornească MSBuild/Roslyn.
- [x] Search pe indexul generat trebuie să returneze rezultat sub un prag relaxat, doar ca smoke test, nu benchmark strict.
- [x] Incremental după modificarea unui fișier trebuie să marcheze sub 10% documente dirty în cazul body-only change.
- [x] Testul de performance trebuie să fie robust în CI; evită praguri prea stricte de timp.

---

## 40. README obligatoriu

- [x] Adaugă `tools/RoslynRepoIndexer/README.md`.
- [x] Explică ce face tool-ul în 5-8 rânduri.
- [x] Explică explicit că nu folosește AI, embeddings sau search engine extern.
- [x] Include comenzi:

```bash
dotnet build tools/RoslynRepoIndexer/RoslynRepoIndexer.sln
dotnet test tools/RoslynRepoIndexer/RoslynRepoIndexer.sln
dotnet run --project tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Cli -- index .
dotnet run --project tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Cli -- status .
dotnet run --project tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Cli -- doctor .
dotnet run --project tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Cli -- search CustomerService
dotnet run --project tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Cli -- suggest "unde se validează tokenul JWT?"
dotnet run --project tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Cli -- refs CustomerService --exact
```

- [x] Include exemplu JSON output pentru `search`, `suggest`, `status` și `doctor`.
- [x] Include secțiune „Troubleshooting”.
- [x] Include ce foldere sunt excluse default.
- [x] Include cum se configurează `.roslyn-index.json`.
- [x] Include secțiune pentru instalare ca `dotnet tool` local/global.
- [x] Include sample `.riignore` sau explică de ce excluderile sunt doar în `.roslyn-index.json`.
- [x] Include lista codurilor de exit.

---

## 41. Privacy, local-only și no-network guarantee

- [x] Tool-ul nu face requesturi HTTP.
- [x] Tool-ul nu pornește servicii externe.
- [x] Tool-ul nu trimite cod sursă sau metadata în afara mașinii.
- [x] Tool-ul nu folosește telemetry sau analytics.
- [x] Tool-ul nu folosește AI, embeddings, vector databases sau LLM APIs.
- [x] Adaugă test/static check care inspectează proiectele noi pentru package names interzise evidente: `OpenAI`, `SemanticKernel`, `MLNet`, `Pinecone`, `Qdrant`, `Weaviate`, `Elasticsearch`, `Lucene`, `HttpClientFactory` runtime dacă nu e justificat.
- [x] Documentează explicit în README că tool-ul este local-only.

---

## 42. Definition of Done

- [x] `dotnet build tools/RoslynRepoIndexer/RoslynRepoIndexer.sln` trece.
- [x] `dotnet test tools/RoslynRepoIndexer/RoslynRepoIndexer.sln` trece.
- [x] `ri index .` funcționează într-un repo C# real.
- [x] `ri status .` arată index valid.
- [x] `ri search <nume-clasă>` returnează declarația clasei.
- [x] `ri search <nume-metodă>` returnează metoda și referințe/text relevante.
- [x] `ri goto <symbol>` returnează declarația.
- [x] `ri refs <symbol>` returnează referințe indexate.
- [x] `ri refs <symbol> --exact` folosește Roslyn on-demand și returnează referințe exacte.
- [x] `ri search <query> --json` returnează JSON valid conform schemei.
- [x] `ri suggest <question> --json` returnează sugestii deterministe utile pentru Codex.
- [x] `ri doctor . --json` returnează diagnostics utile.
- [x] `ri --version` și `ri --help` funcționează.
- [x] Contractele JSON au snapshot tests.
- [x] Incremental indexing nu reprocesează totul la o schimbare body-only.
- [x] Fișierele deleted sunt eliminate complet din index.
- [x] Nu există TODO/stub/not implemented.
- [x] Nu există dependențe AI/embedding/vector/search-server/HTTP/cloud telemetry.
- [x] Nu există dependențe runtime inutile în afară de Roslyn/MSBuildLocator.
- [x] README există și poate fi urmat de un developer nou.

---

## 43. Surse oficiale utile pentru Codex

- [x] Roslyn Workspace model: https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/work-with-workspace
- [x] `Document` API: https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.document
- [x] `SemanticModel` API: https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.semanticmodel
- [x] `GetDeclaredSymbol` API: https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.csharpextensions.getdeclaredsymbol
- [x] `SymbolFinder.FindReferencesAsync`: https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.findsymbols.symbolfinder.findreferencesasync
- [x] `Microsoft.CodeAnalysis.CSharp.Workspaces` NuGet: https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp.Workspaces/
- [x] `Microsoft.CodeAnalysis.Workspaces.MSBuild` NuGet: https://www.nuget.org/packages/Microsoft.CodeAnalysis.Workspaces.MSBuild/
- [x] `Microsoft.Build.Locator` NuGet: https://www.nuget.org/packages/Microsoft.Build.Locator/
- [x] MSBuild Locator guidance: https://learn.microsoft.com/en-us/visualstudio/msbuild/find-and-use-msbuild-versions
