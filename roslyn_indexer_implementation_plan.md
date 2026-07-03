# Plan de implementare: Roslyn Indexer semantic pentru tot repository-ul

**Data generării:** 2026-07-03  
**Scop:** acest document este un checklist complet, explicit și executabil pentru Codex, pentru implementarea unui indexer Roslyn local-first care devine motorul principal de căutare semantică pentru repository-ul curent.

---

## Instrucțiuni obligatorii pentru Codex

- [ ] Tratează acest document ca specificație de produs + plan de implementare, nu ca propunere exploratorie.
- [ ] Nu lăsa stub-uri, TODO-uri, interfețe neimplementate sau cod mort.
- [ ] Nu implementa doar un „narrow MVP”; implementează pipeline-ul complet: discovery repo, Roslyn load, semantic extraction, chunking, embeddings, vector index, search hibrid, CLI, daemon, MCP, incremental watch, storage persistent, teste, benchmark-uri și documentație.
- [ ] Fă fiecare pas incremental, cu build și teste după fiecare etapă mare.
- [ ] Menține compatibilitate cross-platform: Windows, Linux, macOS.
- [ ] Nu trimite cod sursă către servicii remote fără configurare explicită `allowRemoteEmbeddings: true`.
- [ ] Orice funcționalitate care citește repository-ul trebuie să fie root-scoped, cu path canonicalization și protecție împotriva path traversal.
- [ ] Nu modifica fișierele utilizatorului în afara directorului de cache/index fără comandă explicită.
- [ ] Orice comandă publică trebuie să aibă teste end-to-end și output stabil pentru consum de către agenți.
- [ ] Orice API intern important trebuie să aibă teste unitare și teste de regresie.
- [ ] Orice decizie de degradare, fallback sau eroare trebuie să fie vizibilă în `ridx doctor`, `ridx status` și loguri.
- [ ] La final, `dotnet test`, testele E2E CLI/MCP și benchmark smoke trebuie să ruleze cu succes.

---

## Principii de design

- [ ] Local-first: indexul și căutarea trebuie să funcționeze complet local.
- [ ] Semantic-first: căutarea implicită trebuie să fie semantică + hibridă, nu simplu grep.
- [ ] Roslyn-native: pentru C# și VB, indexarea trebuie să folosească sintaxa, semantic model, simboluri și relații Roslyn.
- [ ] Whole-repo: indexează cod Roslyn, proiecte, documentație, config, scripts, fișiere text relevante și metadate repo; exclude build artifacts și fișiere ignorate.
- [ ] Daily usage: pornire rapidă, căutare rapidă, indexare incrementală, watcher stabil, consum CPU/RAM controlat.
- [ ] Agent-friendly: fiecare rezultat trebuie să includă context suficient pentru Codex: fișier, span, preview, simboluri, relații, motivul scorului și comenzi de follow-up.
- [ ] Privacy by default: embedding remote doar opt-in; redacție de secrete înainte de embedding remote.
- [ ] Determinism: același repo + aceeași configurare + același model de embeddings produc index compatibil și rezultate stabile.
- [ ] Observabilitate: tot ce durează, eșuează sau degradează calitatea trebuie măsurat.
- [ ] Extensibilitate: providerii de embeddings, vector store, search reranker și conectorii de agent trebuie să fie plug-in-uri curate.

---

## Nume și formă produs

- [ ] Folosește numele intern `RoslynIndexer` pentru proiecte și namespace-uri.
- [ ] Expune binary-ul CLI ca `ridx`.
- [ ] Expune daemon-ul local ca `ridx daemon`.
- [ ] Expune serverul MCP ca `ridx mcp`.
- [ ] Folosește directorul de index implicit `.ridx/` în repo dacă repo-ul este privat/local și `.git/info/ridx/` sau cache-ul utilizatorului dacă repo-ul nu permite fișiere noi.
- [ ] Permite override prin `RIDX_HOME`, `--index-dir`, `--cache-dir` și config.
- [ ] Creează `roslyn-indexer.json` la `ridx init`.

---

## Arhitectură soluție

- [ ] Creează soluția `RoslynIndexer.slnx` sau `RoslynIndexer.sln`, în funcție de tooling-ul repo-ului.
- [ ] Creează proiectul `src/RoslynIndexer.Abstractions` pentru contracte publice și modele.
- [ ] Creează proiectul `src/RoslynIndexer.Core` pentru orchestrare, config, pipeline și utilitare comune.
- [ ] Creează proiectul `src/RoslynIndexer.Roslyn` pentru workspace loading, semantic model, symbol extraction și graph extraction.
- [ ] Creează proiectul `src/RoslynIndexer.Indexing` pentru chunking, hashing, incremental invalidation și scheduling.
- [ ] Creează proiectul `src/RoslynIndexer.Search` pentru query pipeline, lexical search, vector search, reranking și result explanation.
- [ ] Creează proiectul `src/RoslynIndexer.Storage.Sqlite` pentru metadata store, migrations, FTS și cache persistent.
- [ ] Creează proiectul `src/RoslynIndexer.Storage.Vector` pentru interfața vector index și implementarea locală.
- [ ] Creează proiectul `src/RoslynIndexer.Storage.Qdrant` ca provider opțional pentru repo-uri foarte mari sau utilizare de echipă.
- [ ] Creează proiectul `src/RoslynIndexer.Embeddings` pentru providerii de embeddings și batching.
- [ ] Creează proiectul `src/RoslynIndexer.Cli` pentru comenzi terminal.
- [ ] Creează proiectul `src/RoslynIndexer.Daemon` pentru procesul local long-running.
- [ ] Creează proiectul `src/RoslynIndexer.Mcp` pentru MCP STDIO și HTTP.
- [ ] Creează proiectul `src/RoslynIndexer.Diagnostics` pentru health checks, logging și profiling.
- [ ] Creează proiectul `tests/RoslynIndexer.UnitTests`.
- [ ] Creează proiectul `tests/RoslynIndexer.RoslynTests`.
- [ ] Creează proiectul `tests/RoslynIndexer.IntegrationTests`.
- [ ] Creează proiectul `tests/RoslynIndexer.E2ETests`.
- [ ] Creează proiectul `tests/RoslynIndexer.SearchQualityTests`.
- [ ] Creează proiectul `benchmarks/RoslynIndexer.Benchmarks`.
- [ ] Creează folderul `samples/` cu repo-uri miniaturale pentru testare.
- [ ] Creează folderul `docs/` cu documentația utilizatorului și documentația de design.

---

## Pachete și dependențe

- [ ] Adaugă `Microsoft.CodeAnalysis.CSharp.Workspaces` pentru suport C# Workspace.
- [ ] Adaugă `Microsoft.CodeAnalysis.VisualBasic.Workspaces` pentru suport VB Workspace, chiar dacă C# este prioritar.
- [ ] Adaugă `Microsoft.CodeAnalysis.Workspaces.MSBuild` pentru încărcarea `.sln`, `.slnf`, `.csproj`, `.vbproj` prin MSBuild Workspace.
- [ ] Adaugă `Microsoft.Build.Locator` și înregistrează MSBuild înainte ca orice tip MSBuild să fie încărcat în AppDomain/proces.
- [ ] Adaugă `Microsoft.VisualStudio.SolutionPersistence` sau fallback echivalent pentru citirea `.sln` și `.slnx` atunci când `MSBuildWorkspace` nu poate încărca direct formatul.
- [ ] Adaugă `System.CommandLine` sau parser CLI stabil echivalent.
- [ ] Adaugă `Microsoft.Data.Sqlite` pentru metadata store.
- [ ] Adaugă suport pentru SQLite FTS5; validează la runtime că extensia FTS5 este disponibilă.
- [ ] Adaugă `BenchmarkDotNet` pentru benchmark-uri.
- [ ] Adaugă `xUnit` sau `NUnit` și assertion library stabilă.
- [ ] Adaugă `Verify` sau snapshot testing echivalent pentru output CLI/MCP stabil.
- [ ] Adaugă `Microsoft.Extensions.Logging` și logging structured.
- [ ] Adaugă `Microsoft.Extensions.Configuration` pentru config JSON, env vars și CLI overrides.
- [ ] Adaugă `Microsoft.Extensions.Hosting` pentru daemon și services.
- [ ] Adaugă `Microsoft.Extensions.VectorData.Abstractions` doar dacă este util ca abstracție peste providerii vectoriali; nu lega designul de preview-uri instabile fără adapter intern.
- [ ] Adaugă provider local de embeddings prin adapter intern `IEmbeddingProvider`.
- [ ] Adaugă provider OpenAI/Azure/OpenAI-compatible doar ca modul opțional, opt-in.
- [ ] Adaugă provider Qdrant doar ca modul opțional, nu ca dependență obligatorie pentru funcționare locală.
- [ ] Pune versiunile în `Directory.Packages.props`.
- [ ] Activează nullable reference types, treat warnings as errors pentru codul nou și analyzers recomandate.

---

## Structură de directoare pentru index

- [ ] Creează `.ridx/config.json` pentru config efectiv materializat.
- [ ] Creează `.ridx/index.db` pentru SQLite metadata + FTS.
- [ ] Creează `.ridx/vectors/` pentru segmente vectoriale locale.
- [ ] Creează `.ridx/vectors/manifest.json` pentru model, dimensiune, metrică și versiuni.
- [ ] Creează `.ridx/wal/` pentru operațiuni incremental-index recoverable.
- [ ] Creează `.ridx/locks/` pentru lock de proces și lock de scriere index.
- [ ] Creează `.ridx/logs/` pentru loguri rotative.
- [ ] Creează `.ridx/tmp/` pentru operațiuni atomice.
- [ ] Creează `.ridx/metrics/` pentru profilări și benchmark snapshots.
- [ ] Creează `.ridx/schema-version` pentru migrații.
- [ ] Creează `.ridx/.gitignore` care ignoră DB, vectors, logs, tmp și lock-uri.

---

## Configurare `roslyn-indexer.json`

- [ ] Definește schema JSON publică și versiune: `schemaVersion`.
- [ ] Definește `repoRoot` opțional; implicit se detectează din `.git` sau cwd.
- [ ] Definește `solutions`: listă explicită de `.sln`, `.slnx`, `.slnf`, `.csproj`, `.vbproj`.
- [ ] Definește `autoDiscoverSolutions: true|false`.
- [ ] Definește `include` și `exclude` cu glob-uri.
- [ ] Respectă `.gitignore` implicit.
- [ ] Definește `.ridxignore` pentru excluderi suplimentare.
- [ ] Exclude implicit `bin/`, `obj/`, `.git/`, `.vs/`, `.idea/`, `.vscode/`, `node_modules/`, `packages/`, `.nuget/`, `artifacts/`, `coverage/`, `TestResults/`, `dist/`, `build/`, `.ridx/`.
- [ ] Definește `indexGeneratedCode` cu valori `none`, `metadataOnly`, `full`.
- [ ] Definește `indexNonRoslynText: true`.
- [ ] Definește `maxFileBytes` pentru text files.
- [ ] Definește `maxBinaryProbeBytes` pentru detectare binară.
- [ ] Definește `targetFrameworkPolicy`: `all`, `first`, `currentRuntime`, `explicit`.
- [ ] Definește `configurations`: `Debug`, `Release`, custom.
- [ ] Definește `platforms`: `AnyCPU`, custom.
- [ ] Definește `embedding.provider`: `local`, `openai`, `azureOpenAI`, `ollama`, `customHttp`, `none`.
- [ ] Definește `embedding.model`, `embedding.dimensions`, `embedding.batchSize`, `embedding.maxConcurrency`.
- [ ] Definește `embedding.allowRemoteEmbeddings: false` implicit.
- [ ] Definește `embedding.redactSecretsBeforeRemote: true` implicit.
- [ ] Definește `vector.backend`: `localHnsw`, `sqliteVec`, `qdrant`, `exactFlat`.
- [ ] Definește `search.defaultMode`: `smart`.
- [ ] Definește ponderi de scoring configurabile.
- [ ] Definește boosts pe path-uri, proiecte, tipuri de simboluri și teste.
- [ ] Definește `daemon.autoStart`, `daemon.socket`, `daemon.httpPort`, `daemon.bindAddress`.
- [ ] Definește `watch.enabled`, `watch.debounceMs`, `watch.maxBatchFiles`.
- [ ] Definește `performance.maxCpuPercent`, `performance.maxMemoryMb`, `performance.lowPriorityMode`.
- [ ] Definește `security.allowedRoots`.
- [ ] Definește `logging.level`, `logging.json`, `logging.redact`.
- [ ] Adaugă validare strictă cu mesaje clare.
- [ ] Adaugă `ridx config validate`.
- [ ] Adaugă `ridx config print-effective`.

---

## CLI: comenzi obligatorii

- [ ] Implementează `ridx init`.
- [ ] Implementează `ridx doctor`.
- [ ] Implementează `ridx index`.
- [ ] Implementează `ridx index --full`.
- [ ] Implementează `ridx index --incremental`.
- [ ] Implementează `ridx watch`.
- [ ] Implementează `ridx daemon start`.
- [ ] Implementează `ridx daemon stop`.
- [ ] Implementează `ridx daemon status`.
- [ ] Implementează `ridx mcp` prin STDIO.
- [ ] Implementează `ridx mcp --http` cu autentificare locală prin token.
- [ ] Implementează `ridx search "query"`.
- [ ] Implementează `ridx search --json "query"`.
- [ ] Implementează `ridx search --mode semantic|lexical|symbol|smart`.
- [ ] Implementează `ridx search --project`, `--path`, `--kind`, `--language`, `--tests`, `--limit`.
- [ ] Implementează `ridx symbol "NameOrFully.Qualified.Name"`.
- [ ] Implementează `ridx refs <symbolId|file:line:col>`.
- [ ] Implementează `ridx callers <symbolId|file:line:col>`.
- [ ] Implementează `ridx callees <symbolId|file:line:col>`.
- [ ] Implementează `ridx impls <symbolId|file:line:col>`.
- [ ] Implementează `ridx overrides <symbolId|file:line:col>`.
- [ ] Implementează `ridx impact <symbolId|file:line:col>`.
- [ ] Implementează `ridx context <symbolId|file:line:col>`.
- [ ] Implementează `ridx explain <resultId>`.
- [ ] Implementează `ridx stats`.
- [ ] Implementează `ridx purge`.
- [ ] Implementează `ridx migrate` pentru schema DB.
- [ ] Implementează `ridx export --format jsonl`.
- [ ] Implementează `ridx import --format jsonl` pentru debugging și teste.
- [ ] Implementează `ridx version --json`.
- [ ] Fiecare comandă trebuie să aibă help complet.
- [ ] Fiecare comandă trebuie să respecte `--repo`, `--config`, `--index-dir`, `--verbose`, `--quiet`, `--json` unde are sens.

---

## Discovery repository

- [ ] Detectează repo root prin `.git`, workspace root explicit sau cwd.
- [ ] Detectează toate soluțiile `.sln`, `.slnx`, `.slnf`.
- [ ] Detectează toate proiectele `.csproj`, `.vbproj`, `.fsproj`.
- [ ] Pentru `.fsproj`, indexează proiectul ca text/project metadata, chiar dacă Roslyn semantic nu se aplică.
- [ ] Detectează `global.json` și SDK version pinning.
- [ ] Detectează `Directory.Build.props` și `Directory.Build.targets`.
- [ ] Detectează `Directory.Packages.props`.
- [ ] Detectează `.editorconfig`, `.globalconfig`, analyzers și additional files.
- [ ] Detectează `.slnf` și citește lista de proiecte incluse.
- [ ] Pentru `.slnx`, încearcă încărcare directă; dacă eșuează, folosește parser SolutionPersistence și încarcă proiectele individual prin `OpenProjectAsync`.
- [ ] Pentru repo fără solution, creează virtual solution din toate proiectele detectate.
- [ ] Pentru repo fără proiecte .NET, indexează tot repo-ul ca text semantic non-Roslyn.
- [ ] Construiește un `RepoManifest` cu path-uri canonicalizate, hash-uri, limbi, tipuri de fișiere și project membership.
- [ ] Persistă `RepoManifest` și checksum în DB.
- [ ] Adaugă test pentru repo cu o singură soluție.
- [ ] Adaugă test pentru repo cu soluții multiple.
- [ ] Adaugă test pentru repo cu `.slnf`.
- [ ] Adaugă test pentru repo cu `.slnx`.
- [ ] Adaugă test pentru repo fără soluție, doar proiecte.
- [ ] Adaugă test pentru repo fără .NET.

---

## MSBuild și Workspace loading

- [ ] Înregistrează MSBuild prin `MSBuildLocator` înainte de orice referință la tipuri MSBuild.
- [ ] Detectează toate instanțele MSBuild disponibile și loghează alegerea.
- [ ] Preferă SDK-ul din `global.json`, dacă există și poate fi folosit.
- [ ] Permite override `--msbuild-path`.
- [ ] Creează `MSBuildWorkspace` cu proprietăți configurabile: `Configuration`, `Platform`, `TargetFramework`, `DesignTimeBuild`, `SkipCompilerExecution`, `ProvideCommandLineArgs`.
- [ ] Capturează `WorkspaceFailed` și persistă warning/error în `diagnostics`.
- [ ] Încarcă soluția completă cu cancellation token.
- [ ] Încarcă proiecte individuale când soluția nu poate fi încărcată complet.
- [ ] Suportă proiecte multi-target prin indexare per target framework sau policy configurabil.
- [ ] Suportă conditional compilation symbols per project/configuration.
- [ ] Suportă generated documents unde Roslyn le expune.
- [ ] Suportă metadata references și project references.
- [ ] Nu face build complet; folosește design-time build și compilations Roslyn.
- [ ] Adaugă timeout configurabil pentru workspace loading.
- [ ] Adaugă fallback pentru proiecte care nu pot fi evaluate: index text + diagnostics.
- [ ] Adaugă progres incremental: project loaded, document loaded, compilation ready.
- [ ] Persistă mapping `ProjectId`, path, TFMs, language, output kind.
- [ ] Adaugă test pentru proiect SDK-style.
- [ ] Adaugă test pentru proiect non-SDK-style dacă poate fi creat sample minimal.
- [ ] Adaugă test pentru multi-targeting.
- [ ] Adaugă test pentru conditional compilation.
- [ ] Adaugă test pentru project references.
- [ ] Adaugă test pentru workspace failure fără crash.

---

## Model de date persistent

- [ ] Definește tabel `repos`.
- [ ] Definește tabel `index_runs`.
- [ ] Definește tabel `files` cu path relativ, path absolut hash-uit opțional, size, mtime, content hash, language, ignored flag.
- [ ] Definește tabel `solutions`.
- [ ] Definește tabel `projects`.
- [ ] Definește tabel `documents`.
- [ ] Definește tabel `compilations` cu project, TFM, configuration și checksum.
- [ ] Definește tabel `symbols`.
- [ ] Definește tabel `symbol_parts` pentru partial classes/methods.
- [ ] Definește tabel `symbol_declarations`.
- [ ] Definește tabel `symbol_references`.
- [ ] Definește tabel `symbol_edges` pentru call/read/write/inherits/implements/overrides/uses/creates/throws/catches/attribute/di-registration.
- [ ] Definește tabel `chunks`.
- [ ] Definește tabel `chunk_symbols`.
- [ ] Definește tabel `embeddings`.
- [ ] Definește tabel `vector_segments`.
- [ ] Definește tabel `lexical_docs` sau FTS virtual table.
- [ ] Definește tabel `diagnostics`.
- [ ] Definește tabel `metrics`.
- [ ] Definește tabel `query_cache`.
- [ ] Definește tabel `schema_migrations`.
- [ ] Folosește IDs deterministe bazate pe SHA-256 pentru repo, file, symbol, chunk.
- [ ] Pentru `symbolId`, include repo id, project id, language, assembly, `SymbolKey`, span normalizat și symbol kind.
- [ ] Pentru chunk id, include file hash, symbol id, chunk kind, span și normalized semantic text hash.
- [ ] Salvează `SymbolKey` serializat pentru rezolvare ulterioară.
- [ ] Salvează `DocumentationCommentId` unde există.
- [ ] Salvează fully qualified metadata name.
- [ ] Salvează display name, short name, namespace, containing type, containing assembly.
- [ ] Salvează signature canonicală.
- [ ] Salvează accessibility, modifiers, static/abstract/virtual/override/sealed/async/partial/extern.
- [ ] Salvează generic arity și constraints.
- [ ] Salvează base types și interfaces ca edges.
- [ ] Salvează attributes ca edges și text.
- [ ] Salvează XML docs și summary extras.
- [ ] Salvează `sourceHash` pentru invalidare.
- [ ] Creează indexuri SQL pentru path, symbol name, FQN, kind, project, content hash, chunk hash.
- [ ] Adaugă migrații atomice cu rollback.
- [ ] Adaugă teste de migrare de la schema goală la schema curentă.
- [ ] Adaugă test de compatibilitate: DB creat cu versiune anterioară minimală se migrează corect.

---

## Extractor Roslyn: simboluri

- [ ] Creează `IRoslynSymbolExtractor`.
- [ ] Extrage namespace-uri.
- [ ] Extrage clase.
- [ ] Extrage record classes.
- [ ] Extrage structs.
- [ ] Extrage record structs.
- [ ] Extrage interfaces.
- [ ] Extrage enums.
- [ ] Extrage delegates.
- [ ] Extrage methods.
- [ ] Extrage constructors.
- [ ] Extrage static constructors.
- [ ] Extrage destructors/finalizers.
- [ ] Extrage operators.
- [ ] Extrage conversion operators.
- [ ] Extrage properties.
- [ ] Extrage indexers.
- [ ] Extrage events.
- [ ] Extrage fields.
- [ ] Extrage constants.
- [ ] Extrage local functions.
- [ ] Extrage parameters.
- [ ] Extrage type parameters.
- [ ] Extrage local variables în mod configurabil, cu scope limits, pentru rezultate relevante dar fără explozie de index.
- [ ] Extrage top-level statements ca simbol virtual `Program.<top-level>`.
- [ ] Extrage lambdas ca chunk semantic legat de metoda/containerul părinte, nu ca simbol global zgomotos.
- [ ] Extrage anonymous types ca facts în chunk, nu ca simbol global.
- [ ] Extrage extension methods și marchează receiver type.
- [ ] Extrage partial declarations și le unește sub același symbol canonical.
- [ ] Extrage generated symbols separat, cu flag `isGenerated`.
- [ ] Extrage XML documentation comments.
- [ ] Extrage attributes, inclusiv constructor args constante unde sunt disponibile.
- [ ] Extrage nullable annotations.
- [ ] Extrage async/iterator markers.
- [ ] Extrage exception types din `throw`, `catch`, XML docs și attributes relevante.
- [ ] Extrage DTO-like facts: record positional parameters, required members, init-only setters.
- [ ] Extrage ASP.NET facts: controllers, actions, endpoints, route attributes.
- [ ] Extrage DI facts: `IServiceCollection` registrations, lifetime, service type, implementation type.
- [ ] Extrage EF Core facts: `DbContext`, `DbSet<T>`, entity configs, migrations ca text/semantic.
- [ ] Extrage test facts: framework, test class, test method, traits/categories.
- [ ] Extrage source generator facts unde sunt vizibile prin compilation.
- [ ] Optimizează: folosește syntax filtering înainte de semantic lookup.
- [ ] Optimizează: obține `SemanticModel` o singură dată per document per compilation context.
- [ ] Optimizează: evită `SymbolFinder.FindReferencesAsync` în full-index pentru fiecare simbol; folosește traversal semantic per document.
- [ ] Folosește `SymbolEqualityComparer.Default` în memorie.
- [ ] Persistă simbolurile în batch-uri tranzacționale.
- [ ] Adaugă teste pentru fiecare tip de simbol.
- [ ] Adaugă teste pentru partial classes.
- [ ] Adaugă teste pentru generics și constraints.
- [ ] Adaugă teste pentru overloads.
- [ ] Adaugă teste pentru extension methods.
- [ ] Adaugă teste pentru records și required members.
- [ ] Adaugă teste pentru top-level statements.
- [ ] Adaugă teste pentru local functions și lambdas.
- [ ] Adaugă teste pentru nullable annotations.
- [ ] Adaugă teste pentru XML docs.
- [ ] Adaugă teste pentru generated code policy.

---

## Extractor Roslyn: referințe și relații

- [ ] Creează `IRoslynReferenceExtractor`.
- [ ] Extrage referințe la tipuri.
- [ ] Extrage referințe la metode.
- [ ] Extrage referințe la proprietăți.
- [ ] Extrage referințe la fields.
- [ ] Extrage referințe la events.
- [ ] Extrage referințe la constructors.
- [ ] Extrage referințe la extension methods.
- [ ] Extrage call edges pentru invocații normale.
- [ ] Extrage call edges pentru invocații delegate.
- [ ] Extrage call edges pentru lambdas și local functions legate de container.
- [ ] Extrage object creation edges.
- [ ] Extrage read/write edges pentru fields/properties unde se poate distinge assignment/read.
- [ ] Extrage inheritance edges.
- [ ] Extrage implements edges.
- [ ] Extrage override edges.
- [ ] Extrage interface implementation explicită.
- [ ] Extrage base constructor calls.
- [ ] Extrage attribute usage edges.
- [ ] Extrage generic type argument edges.
- [ ] Extrage `nameof` edges ca referințe light.
- [ ] Extrage reflection string candidates în mod separat, cu confidence scăzut.
- [ ] Extrage serialization contract references pentru `JsonPropertyName`, `DataMember`, etc.
- [ ] Extrage ASP.NET route edges între controllers/actions și route chunks.
- [ ] Extrage DI edges între service registration și implementation.
- [ ] Extrage test-to-production edges când testul invocă simboluri din codul de producție.
- [ ] Marchează fiecare edge cu `confidence`: exact, inferred, heuristic.
- [ ] Marchează fiecare edge cu location span.
- [ ] Adaugă deduplicare edge per source/target/kind/span.
- [ ] Oferă API `FindReferencesOnDemandAsync` care folosește `SymbolFinder.FindReferencesAsync` pentru simboluri punctuale.
- [ ] Folosește `SymbolFinder.FindReferencesAsync` în `ridx refs` ca validare/on-demand, nu ca singura sursă persistentă.
- [ ] Adaugă teste pentru references în același proiect.
- [ ] Adaugă teste pentru references cross-project.
- [ ] Adaugă teste pentru interface implementation.
- [ ] Adaugă teste pentru override.
- [ ] Adaugă teste pentru extension method call.
- [ ] Adaugă teste pentru property read/write.
- [ ] Adaugă teste pentru DI registration.
- [ ] Adaugă teste pentru ASP.NET route attributes.

---

## Indexare fișiere non-Roslyn

- [ ] Creează `ITextFileIndexer`.
- [ ] Indexează Markdown, reStructuredText, AsciiDoc.
- [ ] Indexează JSON, YAML, TOML, XML ca text structurat.
- [ ] Indexează `.csproj`, `.props`, `.targets`, `.sln`, `.slnx`, `.slnf` ca documente relevante.
- [ ] Indexează scripts: `.ps1`, `.sh`, `.cmd`, `.bat`.
- [ ] Indexează Dockerfile și compose files.
- [ ] Indexează CI configs: GitHub Actions, Azure Pipelines, GitLab CI.
- [ ] Indexează SQL migrations și seed scripts.
- [ ] Indexează frontend files dacă există: `.ts`, `.tsx`, `.js`, `.jsx`, `.vue`, `.svelte`, `.css`, `.scss`, `.html` ca text semantic, fără pretenții Roslyn.
- [ ] Detectează fișiere binare și le exclude.
- [ ] Detectează fișiere mari și le trunchiază sau exclude conform config.
- [ ] Respectă `.gitignore` și `.ridxignore`.
- [ ] Redactează potențiale secrete în textul trimis la embedding remote.
- [ ] Creează chunks pe heading-uri pentru Markdown.
- [ ] Creează chunks pe obiecte/keys pentru JSON/YAML/TOML mari.
- [ ] Creează chunks pe targets/items/properties pentru MSBuild XML.
- [ ] Adaugă teste pentru fiecare tip de fișier suportat.

---

## Chunking semantic

- [ ] Creează `IChunker` cu implementări pentru Roslyn symbols și text generic.
- [ ] Pentru fiecare simbol public/internal relevant, creează chunk `symbol-definition`.
- [ ] Pentru metode lungi, creează chunk-uri `method-body-slice` pe blocuri semantice.
- [ ] Pentru clase mari, creează chunk `type-overview` cu membri, base types, interfaces și summary.
- [ ] Pentru namespace/proiect, creează chunk `project-overview`.
- [ ] Pentru soluție/repo, creează chunk `repo-map`.
- [ ] Pentru teste, creează chunk `test-case` și le leagă de codul testat.
- [ ] Pentru fișiere text, creează chunk-uri după heading/section/paragraph/code fence.
- [ ] Fiecare chunk trebuie să aibă span exact în fișier unde e posibil.
- [ ] Fiecare chunk trebuie să aibă `displayText` pentru user.
- [ ] Fiecare chunk trebuie să aibă `semanticText` pentru embedding.
- [ ] `semanticText` pentru simbol trebuie să includă nume, FQN, kind, signature, docs, attributes, containing types, project, path, modifiers, dependencies importante, callers/callees top N și snippet curățat.
- [ ] `semanticText` trebuie să includă identificatori split în tokeni naturali: `GetUserById` -> `get user by id`.
- [ ] `semanticText` trebuie să includă sinonime repo-local generate din nume și docs, nu sinonime inventate agresiv.
- [ ] Nu include boilerplate excesiv în embedding.
- [ ] Nu include secrete în embedding remote.
- [ ] Limitează chunk-urile la dimensiuni configurabile.
- [ ] Creează overlap mic între chunk-uri text, dar nu între simboluri mici.
- [ ] Normalizează line endings.
- [ ] Normalizează whitespace pentru hash, păstrând span original.
- [ ] Calculează hash pentru `semanticText` și `displayText`.
- [ ] Refolosește embedding când hash-ul chunk-ului nu s-a schimbat.
- [ ] Adaugă teste pentru chunking de metodă scurtă.
- [ ] Adaugă teste pentru metodă lungă.
- [ ] Adaugă teste pentru clasă mare.
- [ ] Adaugă teste pentru Markdown cu heading-uri.
- [ ] Adaugă teste pentru MSBuild XML.

---

## Embeddings

- [ ] Definește interfața `IEmbeddingProvider`.
- [ ] Definește `EmbeddingRequest` cu chunk id, semantic text, content hash, privacy flags.
- [ ] Definește `EmbeddingVector` cu dimensiune, dtype, model id, checksum.
- [ ] Implementează provider `NoneEmbeddingProvider` pentru teste și fallback lexical-only.
- [ ] Implementează provider local implicit.
- [ ] Providerul local trebuie să funcționeze offline după instalarea modelului.
- [ ] Adaugă comandă `ridx models list`.
- [ ] Adaugă comandă `ridx models install <model>`.
- [ ] Adaugă comandă `ridx models verify`.
- [ ] Stochează manifest pentru model: name, version, dimensions, tokenizer, normalization, checksum, license info.
- [ ] Blochează indexarea dacă modelul din manifest nu corespunde vectorilor existenți, cu mesaj de reindexare.
- [ ] Implementează provider OpenAI-compatible opțional.
- [ ] Implementează provider Azure OpenAI opțional.
- [ ] Implementează provider local HTTP opțional pentru Ollama/LM Studio/servicii locale compatibile.
- [ ] Nu permite provider remote dacă `allowRemoteEmbeddings` nu e true.
- [ ] Redactează secrete pentru provider remote.
- [ ] Implementează batching cu batch size configurabil.
- [ ] Implementează retry cu backoff pentru provider remote.
- [ ] Implementează rate limit pentru provider remote.
- [ ] Implementează cache de embeddings după `modelId + semanticTextHash`.
- [ ] Implementează normalizare vectorială pentru cosine similarity.
- [ ] Implementează detectare dimensiune mismatch.
- [ ] Implementează metrici: tokens/chars processed, embeddings/sec, cache hit rate, failures.
- [ ] Adaugă teste unitare pentru cache hit.
- [ ] Adaugă teste pentru dimensiune mismatch.
- [ ] Adaugă teste pentru provider `None`.
- [ ] Adaugă teste fake provider deterministic pentru search quality tests.
- [ ] Adaugă teste pentru redacție remote.

---

## Vector index local

- [ ] Definește interfața `IVectorIndex`.
- [ ] Definește operații `Upsert`, `Delete`, `Search`, `Flush`, `Snapshot`, `Load`, `Compact`.
- [ ] Implementează backend `exactFlat` pentru corectitudine, teste și repo-uri mici.
- [ ] Implementează backend local performant `localHnsw` cu graph persistent sau integrează o bibliotecă HNSW matură prin adapter izolat.
- [ ] Salvează vectorii în segmente append-only.
- [ ] Salvează graph/index metadata în fișier separat cu checksum.
- [ ] Folosește memory mapping pentru warm load rapid unde are sens.
- [ ] Implementează compaction pentru segmente cu multe tombstones.
- [ ] Implementează delete logic prin tombstones + compaction.
- [ ] Implementează metrică cosine implicită.
- [ ] Implementează fallback la exact search când indexul ANN este corupt sau incomplet.
- [ ] Implementează `topK` + overfetch pentru reranking.
- [ ] Implementează filtre eficiente pe project/path/language/kind folosind metadata prefilter.
- [ ] Implementează rebuild atomic de segment.
- [ ] Implementează crash recovery: dacă snapshot nou e incomplet, folosește ultimul snapshot valid.
- [ ] Implementează checksums pentru toate segmentele.
- [ ] Adaugă test pentru upsert/search.
- [ ] Adaugă test pentru delete.
- [ ] Adaugă test pentru snapshot/load.
- [ ] Adaugă test pentru compaction.
- [ ] Adaugă test pentru index corupt -> fallback exact.
- [ ] Adaugă benchmark pentru 10k, 100k și 1M vectori sintetici dacă resursele permit.

---

## Provider vector opțional: SQLite vector / Qdrant

- [ ] Creează adapter `sqliteVec` dacă extensia aleasă este disponibilă și portabilă pentru platformele țintă.
- [ ] Nu face `sqliteVec` obligatoriu pentru funcționarea produsului.
- [ ] Creează adapter `QdrantVectorIndex` opțional.
- [ ] Qdrant adapter trebuie să suporte collections per repo + model id.
- [ ] Qdrant adapter trebuie să creeze payload indexes pentru filtre path/project/language/kind.
- [ ] Qdrant adapter trebuie să aibă retry, timeout și health checks.
- [ ] Qdrant adapter nu trebuie să pornească container Docker implicit fără consimțământ explicit.
- [ ] Adaugă config pentru endpoint, API key, collection prefix, TLS.
- [ ] Adaugă teste cu testcontainer sau skip explicit când Docker/Qdrant nu e disponibil.
- [ ] Adaugă parity tests între `exactFlat`, `localHnsw` și `qdrant` pentru top results aproximative.

---

## Lexical index și FTS

- [ ] Creează FTS5 index pentru chunks.
- [ ] Indexează nume de simboluri cu boost separat.
- [ ] Indexează FQN cu boost separat.
- [ ] Indexează path-uri cu boost separat.
- [ ] Indexează doc comments.
- [ ] Indexează string literals relevante în mod limitat.
- [ ] Indexează route strings și config keys.
- [ ] Split camelCase, PascalCase, snake_case, kebab-case.
- [ ] Normalizează diacritice doar pentru căutări text generale, nu pentru cod.
- [ ] Suportă exact phrase search.
- [ ] Suportă prefix search pentru simboluri.
- [ ] Suportă fuzzy typo-tolerant pentru nume scurte în mod controlat.
- [ ] Returnează highlights pentru rezultate lexicale.
- [ ] Adaugă teste pentru symbol exact match.
- [ ] Adaugă teste pentru FQN match.
- [ ] Adaugă teste pentru path match.
- [ ] Adaugă teste pentru camelCase split.
- [ ] Adaugă teste pentru phrase search.

---

## Query understanding

- [ ] Creează `IQueryParser`.
- [ ] Detectează intenție `conceptual`: „where do we validate invoices”.
- [ ] Detectează intenție `symbol`: `InvoiceValidator`, `Namespace.Type.Method`.
- [ ] Detectează intenție `navigation`: „go to definition”.
- [ ] Detectează intenție `impact`: „what breaks if I change X”.
- [ ] Detectează intenție `tests`: „tests for payment retry”.
- [ ] Detectează intenție `config`: „where is timeout configured”.
- [ ] Detectează filtre inline: `path:`, `project:`, `kind:`, `lang:`, `test:`.
- [ ] Detectează file references: `src/Foo.cs:123`.
- [ ] Detectează symbol references din `file:line:column` prin Roslyn lookup.
- [ ] Generează query variants: original, identifier split, symbol-ish, natural-language normalized.
- [ ] Nu folosi LLM pentru query parsing în varianta implicită; trebuie să fie rapid și determinist.
- [ ] Adaugă teste pentru fiecare intenție.
- [ ] Adaugă teste pentru filtre inline.
- [ ] Adaugă teste pentru `file:line:col`.

---

## Search pipeline smart

- [ ] Creează `SearchService` cu pipeline configurabil.
- [ ] Rulează lexical search în paralel cu vector search.
- [ ] Rulează symbol lookup exact în paralel cu lexical/vector.
- [ ] Rulează path lookup când query pare path.
- [ ] Pentru query conceptual, crește ponderea vectorială.
- [ ] Pentru query de simbol, crește ponderea symbol/lexical exact.
- [ ] Pentru query de impact, include graph expansion.
- [ ] Pentru query de tests, boostează chunks cu `isTest`.
- [ ] Overfetch vector results de 3x-5x pentru reranking.
- [ ] Overfetch lexical results de 2x-3x pentru reranking.
- [ ] Deduplicate results pe symbol/chunk/file span.
- [ ] Merguiește scoruri din surse diferite.
- [ ] Aplică filtre hard înainte de reranking unde e posibil.
- [ ] Aplică boosts configurabile.
- [ ] Aplică graph neighborhood boost: callers/callees/overrides/tests apropiate.
- [ ] Aplică recency boost mic pentru fișiere modificate recent doar dacă config permite.
- [ ] Penalizează generated code dacă nu e întrebat explicit.
- [ ] Penalizează vendored/generated/minified files.
- [ ] Penalizează fișiere mari cu match slab.
- [ ] Penalizează rezultate fără span exact.
- [ ] Rerankează top 100 cu scoring deterministic.
- [ ] Returnează top N cu explicație.
- [ ] Include `resultId` stabil pentru follow-up.
- [ ] Include `why` cu contribuții: semantic, lexical, symbol, graph, boost.
- [ ] Include `nextActions`: `context`, `refs`, `impact`, `tests`.
- [ ] Include `confidence`: high/medium/low.
- [ ] Include `staleness`: fresh/stale/partial.
- [ ] Adaugă teste pentru semantic-only hit.
- [ ] Adaugă teste pentru lexical exact hit.
- [ ] Adaugă teste pentru symbol exact hit.
- [ ] Adaugă teste pentru dedup.
- [ ] Adaugă teste pentru graph boost.
- [ ] Adaugă teste pentru generated code penalty.

---

## Scoring recomandat inițial

- [ ] Implementează formula configurabilă, nu hardcodată definitiv.
- [ ] Setează default pentru query conceptual: `0.50 semantic + 0.20 lexical + 0.10 symbol + 0.15 graph + 0.05 boosts`.
- [ ] Setează default pentru query symbol: `0.15 semantic + 0.30 lexical + 0.40 symbol + 0.10 graph + 0.05 boosts`.
- [ ] Setează default pentru query impact: `0.25 semantic + 0.15 lexical + 0.20 symbol + 0.35 graph + 0.05 boosts`.
- [ ] Normalizează scorurile pe fiecare canal înainte de merge.
- [ ] Adaugă `scoreBreakdown` în JSON output.
- [ ] Adaugă teste care verifică ordinea așteptată, nu scorul exact fragil.

---

## Result model

- [ ] Definește `SearchResult` stabil.
- [ ] Include `resultId`.
- [ ] Include `chunkId`.
- [ ] Include `symbolId` opțional.
- [ ] Include `title`.
- [ ] Include `kind`.
- [ ] Include `language`.
- [ ] Include `project`.
- [ ] Include `file` relativ.
- [ ] Include `span`: startLine, startColumn, endLine, endColumn.
- [ ] Include `score`.
- [ ] Include `confidence`.
- [ ] Include `matchTypes`.
- [ ] Include `preview`.
- [ ] Include `highlights`.
- [ ] Include `why`.
- [ ] Include `scoreBreakdown`.
- [ ] Include `relatedSymbols` top N.
- [ ] Include `callers` count.
- [ ] Include `callees` count.
- [ ] Include `references` count.
- [ ] Include `tests` count.
- [ ] Include `staleness`.
- [ ] Include `nextActions`.
- [ ] Adaugă JSON schema pentru output.
- [ ] Adaugă snapshot tests pentru output JSON.

---

## Context retrieval pentru agenți

- [ ] Implementează `ridx context <resultId|symbolId|file:line:col>`.
- [ ] Returnează definiția simbolului.
- [ ] Returnează semnătura completă.
- [ ] Returnează doc comments.
- [ ] Returnează containing type/namespace/project.
- [ ] Returnează imports relevante.
- [ ] Returnează base types/interfaces.
- [ ] Returnează callers/callees top N.
- [ ] Returnează references top N cu group by file.
- [ ] Returnează tests asociate.
- [ ] Returnează snippets înainte/după span, cu limite configurabile.
- [ ] Returnează warnings dacă indexul e stale.
- [ ] Returnează token-budgeted context pentru Codex.
- [ ] Adaugă `--budget-tokens` sau `--budget-chars`.
- [ ] Adaugă `--include-callers`, `--include-callees`, `--include-tests`, `--include-references`.
- [ ] Adaugă teste pentru context de metodă.
- [ ] Adaugă teste pentru context de clasă partială.
- [ ] Adaugă teste pentru context budget.

---

## Graph queries

- [ ] Implementează `refs` cu opțiuni `--exact`, `--fast`, `--include-generated`, `--group-by file|project|symbol`.
- [ ] Implementează `callers`.
- [ ] Implementează `callees`.
- [ ] Implementează `impls`.
- [ ] Implementează `overrides`.
- [ ] Implementează `impact`.
- [ ] `impact` trebuie să traverseze references, callers, overrides, interface implementations, DI registrations și tests.
- [ ] `impact` trebuie să limiteze traversarea cu depth configurabil.
- [ ] `impact` trebuie să marcheze edge confidence.
- [ ] `impact` trebuie să returneze `riskLevel`: low/medium/high.
- [ ] `impact` trebuie să sugereze fișiere de test relevante.
- [ ] `impact` trebuie să explice de ce un nod e inclus.
- [ ] Adaugă output text și JSON.
- [ ] Adaugă teste pentru impact simplu.
- [ ] Adaugă teste pentru impact cu override/interface.
- [ ] Adaugă teste pentru impact cu DI.
- [ ] Adaugă teste pentru impact cu tests.

---

## Incremental indexing

- [ ] Creează `IndexOrchestrator`.
- [ ] Creează `ChangeDetector` bazat pe content hash, mtime și git status.
- [ ] Creează `FileSystemWatcher` cu debouncing robust.
- [ ] Creează fallback polling pentru platforme unde watcher-ul pierde evenimente.
- [ ] Detectează schimbări în `.cs`, `.vb`, project files, props/targets, solution files, config files și text files.
- [ ] Pentru schimbări în fișier sursă, reindexează documentul și invalidează simbolurile/chunk-urile afectate.
- [ ] Pentru schimbări în `.csproj`, `.props`, `.targets`, reload project/solution afectat.
- [ ] Pentru schimbări în `Directory.Build.props/targets`, reload proiectele afectate.
- [ ] Pentru schimbări în `.editorconfig`, reload semantic options unde e necesar.
- [ ] Pentru schimbări în `global.json`, rulează `doctor` warning și cere reload complet.
- [ ] Pentru schimbări în model embeddings/config vector, cere rebuild vector index.
- [ ] Pentru rename/move, păstrează istoric dacă content hash corespunde.
- [ ] Pentru delete, marchează tombstone pentru symbols/chunks/vectors.
- [ ] Pentru branch switch, detectează schimbări masive și rulează batch incremental cu progres.
- [ ] Nu bloca search în timpul indexării; folosește snapshot consistent.
- [ ] Expune staleness per result dacă snapshot-ul folosit este anterior schimbărilor.
- [ ] Permite search degraded: lexical/symbol disponibil înainte ca embeddings să fie gata.
- [ ] Implementează queue cu priorități: fișiere deschise/recent modificate primele dacă input e disponibil.
- [ ] Limitează CPU în watch/daemon conform config.
- [ ] Implementează pause/resume indexing.
- [ ] Implementează cancellation clean.
- [ ] Implementează crash recovery din WAL.
- [ ] Adaugă teste pentru modificare fișier.
- [ ] Adaugă teste pentru delete.
- [ ] Adaugă teste pentru rename.
- [ ] Adaugă teste pentru schimbare project file.
- [ ] Adaugă teste pentru branch-like large change.
- [ ] Adaugă teste pentru crash recovery.
- [ ] Adaugă teste pentru search în timp ce indexarea rulează.

---

## Performanță și bugete

- [ ] Definește benchmark baseline pentru repo mic: sub 50k LOC.
- [ ] Definește benchmark baseline pentru repo mediu: 250k-500k LOC.
- [ ] Definește benchmark baseline pentru repo mare sintetic: 1M+ LOC.
- [ ] Țintă cold full index repo mediu: acceptabil pentru prima rulare, progres vizibil și resume dacă se întrerupe.
- [ ] Țintă warm daemon ready: sub 2 secunde pentru metadata + index manifest.
- [ ] Țintă search warm p95: sub 350 ms pentru top 20 pe repo mediu cu index local complet.
- [ ] Țintă search degraded lexical/symbol p95: sub 150 ms pe repo mediu.
- [ ] Țintă incremental reindex fișier mic: sub 2 secunde fără embedding remote.
- [ ] Țintă incremental reindex project file: sub 30 secunde pentru soluție medie, cu progres și fallback.
- [ ] Țintă memorie daemon repo mediu: sub 1.5 GB implicit.
- [ ] Țintă memorie CLI one-shot search: sub 512 MB dacă daemon e disponibil.
- [ ] Măsoară timpul pe faze: discovery, load workspace, extract, chunk, embed, vector upsert, FTS, commit.
- [ ] Măsoară cache hit rate pentru syntax, semantic, chunks, embeddings, vectors.
- [ ] Măsoară număr simboluri/secundă.
- [ ] Măsoară chunks/secundă.
- [ ] Măsoară embeddings/secundă.
- [ ] Măsoară query latency p50/p95/p99.
- [ ] Creează `ridx stats --perf`.
- [ ] Creează benchmark CI smoke rapid.
- [ ] Creează benchmark local extins, opt-in.
- [ ] Nu aloca string-uri masive inutil; folosește pooling unde e justificat.
- [ ] Evită `ToList()` pe colecții mari dacă nu e necesar.
- [ ] Folosește batch SQL transactions.
- [ ] Folosește prepared statements pentru hot paths.
- [ ] Folosește async I/O unde ajută, dar evită overhead async în loops CPU-bound.
- [ ] Folosește bounded concurrency.
- [ ] Adaugă profilare alocări pentru extract și search.

---

## Daemon local

- [ ] `ridx daemon start` pornește proces local per repo.
- [ ] Daemon-ul folosește lock file ca să nu existe doi writeri pe același index.
- [ ] Daemon-ul expune IPC local prin named pipe/Unix domain socket implicit.
- [ ] HTTP trebuie să fie opt-in sau doar loopback cu token.
- [ ] Daemon-ul menține workspace/index snapshot warm.
- [ ] Daemon-ul rulează watcher incremental.
- [ ] Daemon-ul procesează search requests concurent, cu limitare.
- [ ] Daemon-ul procesează index writes serializat sau cu tranzacții sigure.
- [ ] Daemon-ul degradează frumos dacă workspace load eșuează.
- [ ] Daemon-ul publică health: ready, indexing, stale, degraded, error.
- [ ] Daemon-ul oprește graceful la SIGTERM/Ctrl+C.
- [ ] Daemon-ul are loguri rotative.
- [ ] Daemon-ul are idle shutdown configurabil.
- [ ] Daemon-ul poate fi dezactivat; CLI poate funcționa one-shot.
- [ ] Adaugă teste E2E daemon start/search/stop.
- [ ] Adaugă teste pentru lock conflict.
- [ ] Adaugă teste pentru restart cu index existent.

---

## MCP pentru Codex

- [ ] Implementează MCP STDIO în `ridx mcp`.
- [ ] Implementează MCP Streamable HTTP opțional în `ridx mcp --http`.
- [ ] Expune instrucțiuni MCP clare: Codex trebuie să folosească întâi semantic search pentru întrebări despre repo.
- [ ] Expune tool `roslyn_semantic_search`.
- [ ] Expune tool `roslyn_symbol_search`.
- [ ] Expune tool `roslyn_get_context`.
- [ ] Expune tool `roslyn_find_references`.
- [ ] Expune tool `roslyn_callers`.
- [ ] Expune tool `roslyn_callees`.
- [ ] Expune tool `roslyn_implementations`.
- [ ] Expune tool `roslyn_impact_analysis`.
- [ ] Expune tool `roslyn_related_tests`.
- [ ] Expune tool `roslyn_repo_map`.
- [ ] Expune tool `roslyn_index_status`.
- [ ] Expune tool `roslyn_refresh_index`.
- [ ] Tool-urile trebuie să fie read-only cu excepția refresh index.
- [ ] Tool-urile trebuie să valideze `repoRoot` împotriva root-ului configurat.
- [ ] Tool-urile trebuie să returneze structured content JSON.
- [ ] Tool-urile trebuie să returneze text scurt prietenos cu agenții.
- [ ] Tool-urile trebuie să aibă input schema strictă.
- [ ] Tool-urile trebuie să aibă output schema documentată.
- [ ] `roslyn_semantic_search` trebuie să accepte `query`, `limit`, `filters`, `includeContext`, `budgetChars`.
- [ ] `roslyn_get_context` trebuie să accepte `resultId` sau `symbolId` sau `file/line/column`.
- [ ] `roslyn_impact_analysis` trebuie să accepte `depth`, `includeTests`, `includeInferred`.
- [ ] `roslyn_refresh_index` trebuie să accepte `mode: incremental|full`.
- [ ] Adaugă example config pentru Codex CLI în docs.
- [ ] Adaugă example config pentru VS Code MCP în docs.
- [ ] Adaugă `AGENTS.md` snippet: „folosește întotdeauna RoslynIndexer MCP înainte să scanezi manual repo-ul”.
- [ ] Adaugă teste MCP protocol pentru list tools.
- [ ] Adaugă teste MCP pentru fiecare tool.
- [ ] Adaugă teste de securitate MCP pentru path traversal.
- [ ] Adaugă teste MCP pentru token budget.

---

## Config Codex recomandat

- [ ] Documentează configurare globală cu `codex mcp add roslynIndexer -- ridx mcp --repo .` dacă CLI-ul acceptă comenzi locale cu args.
- [ ] Documentează configurare manuală în `~/.codex/config.toml`:

```toml
[mcp_servers.roslynIndexer]
command = "ridx"
args = ["mcp", "--repo", "."]
```

- [ ] Documentează variantă proiect-locală doar dacă e sigură și acceptată explicit de utilizator.
- [ ] Documentează `AGENTS.md`:

```md
When answering questions about this repository, first call the RoslynIndexer MCP tools:
1. Use `roslyn_semantic_search` for conceptual/codebase questions.
2. Use `roslyn_get_context` before editing a symbol.
3. Use `roslyn_impact_analysis` before refactors.
4. Use `roslyn_related_tests` before running or creating tests.
Do not grep manually until the semantic index returns insufficient results.
```

- [ ] Adaugă `ridx mcp print-config --target codex`.
- [ ] Adaugă `ridx mcp print-agents-md-snippet`.

---

## Securitate și privacy

- [ ] Canonicalizează toate path-urile.
- [ ] Refuză path-uri în afara repo root.
- [ ] Refuză symlink traversal în afara root dacă nu e permis explicit.
- [ ] Nu indexa `.env`, key files, certificates, secrets files implicit.
- [ ] Detectează și redactează pattern-uri de secrete: API keys, tokens, private keys, connection strings, passwords.
- [ ] Redacția trebuie să se aplice înainte de embeddings remote.
- [ ] Redacția nu trebuie să modifice snippets locale afișate utilizatorului decât dacă config cere.
- [ ] Remote embeddings trebuie să fie opt-in explicit.
- [ ] Logurile nu trebuie să includă secrete.
- [ ] MCP tool descriptions nu trebuie să includă date sensibile.
- [ ] HTTP MCP trebuie să aibă token.
- [ ] Nu expune daemon-ul pe `0.0.0.0` fără flag explicit `--unsafe-bind-all` și warning mare.
- [ ] Nu rula comenzi shell arbitrare din MCP.
- [ ] Nu permite plugin-uri neîncredere fără allowlist.
- [ ] Adaugă `ridx security audit-index` care raportează dacă indexul conține fișiere suspecte.
- [ ] Adaugă teste pentru secret redaction.
- [ ] Adaugă teste pentru path traversal.
- [ ] Adaugă teste pentru symlink escape.
- [ ] Adaugă teste pentru HTTP token required.

---

## Observabilitate și diagnostic

- [ ] Implementează logging structured cu scopes: repo, run id, project id, phase.
- [ ] Implementează `ridx doctor`.
- [ ] `doctor` verifică .NET SDK disponibil.
- [ ] `doctor` verifică MSBuildLocator și MSBuild instance.
- [ ] `doctor` verifică soluții/proiecte detectate.
- [ ] `doctor` verifică workspace load errors.
- [ ] `doctor` verifică SQLite și FTS5.
- [ ] `doctor` verifică vector backend.
- [ ] `doctor` verifică embedding provider/model.
- [ ] `doctor` verifică config și ignore patterns.
- [ ] `doctor` verifică permissions pentru `.ridx`.
- [ ] `doctor` verifică stale index.
- [ ] `doctor` recomandă comenzi de repair.
- [ ] Implementează `ridx stats` cu număr files/projects/symbols/chunks/vectors/edges.
- [ ] Implementează `ridx diagnostics export` ca zip/json pentru issue reports, fără secrete.
- [ ] Adaugă metrici per fază în DB.
- [ ] Adaugă correlation id pentru fiecare search.
- [ ] Adaugă teste pentru `doctor` happy path.
- [ ] Adaugă teste pentru `doctor` cu MSBuild missing.
- [ ] Adaugă teste pentru DB corrupt.

---

## Reziliență și recovery

- [ ] Toate scrierile DB trebuie să fie tranzacționale.
- [ ] Vector segment writes trebuie să fie atomice: scrie temp, fsync, rename.
- [ ] Menține ultimul snapshot valid.
- [ ] La crash, rulează recovery la următoarea pornire.
- [ ] La schema mismatch, rulează migrații sau cere rebuild clar.
- [ ] La model mismatch, marchează embeddings stale și cere re-embedding.
- [ ] La workspace load partial, indexează ce se poate și marchează proiectele eșuate.
- [ ] La embedding provider eșuat, continuă lexical/symbol și marchează vector index incomplete.
- [ ] La vector index corupt, fallback exact sau rebuild.
- [ ] La DB corupt, oferă `ridx repair` și `ridx purge --keep-config`.
- [ ] Adaugă teste de crash în timpul DB write.
- [ ] Adaugă teste de crash în timpul vector write.
- [ ] Adaugă teste de model mismatch.
- [ ] Adaugă teste de workspace partial failure.

---

## API intern principal

- [ ] Definește `IRepoDiscoveryService`.
- [ ] Definește `IWorkspaceLoader`.
- [ ] Definește `ISymbolExtractor`.
- [ ] Definește `IReferenceExtractor`.
- [ ] Definește `ITextFileIndexer`.
- [ ] Definește `IChunker`.
- [ ] Definește `IEmbeddingProvider`.
- [ ] Definește `IVectorIndex`.
- [ ] Definește `ILexicalIndex`.
- [ ] Definește `IIndexStore`.
- [ ] Definește `IIndexOrchestrator`.
- [ ] Definește `ISearchService`.
- [ ] Definește `IContextService`.
- [ ] Definește `IGraphQueryService`.
- [ ] Definește `IDaemonClient`.
- [ ] Definește `IMcpToolRegistry`.
- [ ] Fiecare interfață trebuie să aibă documentație XML minimă.
- [ ] Fiecare interfață trebuie să aibă test dublu/fake unde e folosită în teste.

---

## Format JSON pentru output search

- [ ] Creează schema `docs/schemas/search-result.schema.json`.
- [ ] Output minimal:

```json
{
  "query": "where is invoice validation handled",
  "mode": "smart",
  "indexSnapshot": "2026-07-03T12:00:00Z:abc123",
  "staleness": "fresh",
  "results": [
    {
      "resultId": "res_...",
      "chunkId": "chk_...",
      "symbolId": "sym_...",
      "title": "InvoiceValidator.ValidateAsync(Invoice invoice)",
      "kind": "method",
      "language": "C#",
      "project": "Billing.Application",
      "file": "src/Billing.Application/Invoices/InvoiceValidator.cs",
      "span": { "startLine": 42, "startColumn": 5, "endLine": 88, "endColumn": 6 },
      "score": 0.91,
      "confidence": "high",
      "matchTypes": ["semantic", "symbol", "graph"],
      "preview": "public async Task<ValidationResult> ValidateAsync(...)",
      "why": "High semantic match to invoice validation; exact Validator symbol; called by InvoiceService.",
      "scoreBreakdown": { "semantic": 0.52, "lexical": 0.16, "symbol": 0.14, "graph": 0.09 },
      "nextActions": ["context", "refs", "impact", "related_tests"]
    }
  ]
}
```

- [ ] Snapshot tests trebuie să stabilizeze ordinea proprietăților JSON.

---

## Search quality suite

- [ ] Creează `tests/fixtures/QualityRepo` cu domenii realiste: billing, users, auth, background jobs, config, tests.
- [ ] Scrie queries conceptuale și expected top symbols.
- [ ] Scrie queries de simbol și expected exact result.
- [ ] Scrie queries de config și expected files.
- [ ] Scrie queries de tests și expected test methods.
- [ ] Scrie queries de impact și expected graph nodes.
- [ ] Creează metrici: MRR@10, Recall@10, ExactTop1 pentru symbol queries.
- [ ] Fail CI dacă metricile scad sub prag.
- [ ] Păstrează pragurile realiste și deterministe folosind fake embeddings provider.
- [ ] Adaugă golden set pentru false positives generated code.
- [ ] Adaugă golden set pentru duplicate partial classes.

---

## Teste unitare obligatorii

- [ ] Teste config parse/merge/validation.
- [ ] Teste glob include/exclude.
- [ ] Teste `.gitignore` respect.
- [ ] Teste path canonicalization.
- [ ] Teste secret redaction.
- [ ] Teste repo discovery.
- [ ] Teste chunk hashing.
- [ ] Teste symbol id stability.
- [ ] Teste chunk id stability.
- [ ] Teste lexical tokenizer.
- [ ] Teste query parser.
- [ ] Teste scoring normalization.
- [ ] Teste result dedup.
- [ ] Teste score explanations.
- [ ] Teste vector exact search.
- [ ] Teste HNSW/vector adapter basics.
- [ ] Teste DB migrations.
- [ ] Teste WAL/recovery helpers.
- [ ] Teste daemon lock.
- [ ] Teste MCP schema generation.

---

## Teste Roslyn obligatorii

- [ ] Test C# class/method/property/event/field extraction.
- [ ] Test C# records.
- [ ] Test C# structs/interfaces/enums/delegates.
- [ ] Test constructors/operators/conversions.
- [ ] Test extension methods.
- [ ] Test async/iterator methods.
- [ ] Test nullable reference types.
- [ ] Test generics constraints.
- [ ] Test partial classes/methods.
- [ ] Test nested types.
- [ ] Test top-level statements.
- [ ] Test local functions.
- [ ] Test lambdas as chunks.
- [ ] Test XML docs extraction.
- [ ] Test attributes extraction.
- [ ] Test inheritance edges.
- [ ] Test interface implementation edges.
- [ ] Test override edges.
- [ ] Test method call edges.
- [ ] Test property read/write edges.
- [ ] Test object creation edges.
- [ ] Test cross-project references.
- [ ] Test generated code policy.
- [ ] Test VB basic class/method extraction.
- [ ] Test VB references basic.

---

## Teste integration/E2E obligatorii

- [ ] `ridx init` creează config valid.
- [ ] `ridx doctor` trece pe sample repo.
- [ ] `ridx index --full` creează DB și vectors.
- [ ] `ridx search` returnează rezultate utile pe sample repo.
- [ ] `ridx search --json` respectă schema.
- [ ] `ridx symbol` găsește simbol exact.
- [ ] `ridx refs` găsește referințe.
- [ ] `ridx context` returnează context budgeted.
- [ ] `ridx impact` returnează graph.
- [ ] `ridx watch` reindexează după modificare.
- [ ] `ridx daemon start/status/stop` funcționează.
- [ ] `ridx mcp` listează tools.
- [ ] Fiecare MCP tool are test E2E.
- [ ] Teste pe Windows path separators.
- [ ] Teste pe Linux/macOS path separators.
- [ ] Teste cu repo path care conține spații.
- [ ] Teste cu fișiere UTF-8 BOM și fără BOM.
- [ ] Teste cu line endings CRLF/LF.

---

## Benchmark-uri obligatorii

- [ ] Benchmark discovery pe repo sintetic.
- [ ] Benchmark workspace load pe sample multi-project.
- [ ] Benchmark symbol extraction pe 10k/100k symbols sintetice.
- [ ] Benchmark reference extraction.
- [ ] Benchmark chunking.
- [ ] Benchmark embedding fake provider throughput.
- [ ] Benchmark vector upsert.
- [ ] Benchmark vector search.
- [ ] Benchmark lexical search.
- [ ] Benchmark smart search end-to-end.
- [ ] Benchmark incremental single-file.
- [ ] Benchmark cold CLI search cu daemon oprit.
- [ ] Benchmark warm CLI search cu daemon pornit.
- [ ] Publică rezultate în `benchmarks/results/README.md`.
- [ ] CI rulează doar benchmark smoke scurt.

---

## CI și calitate cod

- [ ] Configurează GitHub Actions sau pipeline existent.
- [ ] Rulează `dotnet restore`.
- [ ] Rulează `dotnet build -warnaserror`.
- [ ] Rulează `dotnet test`.
- [ ] Rulează teste E2E marcate non-flaky.
- [ ] Rulează format/analyzers.
- [ ] Rulează coverage.
- [ ] Publică coverage summary.
- [ ] Rulează benchmark smoke.
- [ ] Rulează pe Linux.
- [ ] Rulează pe Windows.
- [ ] Rulează pe macOS dacă bugetul CI permite.
- [ ] Cache NuGet packages.
- [ ] Nu cache-ui `.ridx` între teste decât explicit.
- [ ] Blochează PR dacă search quality metrics scad sub prag.
- [ ] Blochează PR dacă output JSON schema se schimbă fără snapshot update intenționat.

---

## Documentație utilizator

- [ ] `docs/getting-started.md`.
- [ ] `docs/configuration.md`.
- [ ] `docs/cli.md`.
- [ ] `docs/codex-mcp.md`.
- [ ] `docs/search-syntax.md`.
- [ ] `docs/indexing-model.md`.
- [ ] `docs/privacy-security.md`.
- [ ] `docs/performance.md`.
- [ ] `docs/troubleshooting.md`.
- [ ] `docs/architecture.md`.
- [ ] `docs/extending-providers.md`.
- [ ] Include exemple reale pentru `ridx search`, `ridx context`, `ridx impact`.
- [ ] Include exemple pentru repo-uri cu soluții multiple.
- [ ] Include exemple pentru repo-uri fără `.sln`.
- [ ] Include explicație pentru index generated code.
- [ ] Include explicație pentru remote embeddings opt-in.
- [ ] Include ghid pentru curățare index.

---

## Definition of Done global

- [ ] Produsul se poate instala sau rula local ca CLI `ridx`.
- [ ] `ridx init` funcționează într-un repo real.
- [ ] `ridx index --full` indexează soluții/proiecte C#/VB și fișiere text relevante.
- [ ] `ridx search` folosește implicit semantic/hybrid search.
- [ ] `ridx search` returnează rezultate cu span, scor, context și explicație.
- [ ] `ridx context` produce context util pentru Codex.
- [ ] `ridx refs/callers/callees/impact` funcționează pe simboluri reale.
- [ ] `ridx watch` actualizează indexul incremental.
- [ ] `ridx daemon` menține indexul warm și servește căutări rapide.
- [ ] `ridx mcp` expune tools utilizabile de Codex.
- [ ] Codex poate folosi MCP pentru a căuta semantic în repo fără grep manual.
- [ ] Indexul supraviețuiește restarturilor.
- [ ] Indexul se recuperează după întrerupere în timpul indexării.
- [ ] Remote embeddings sunt opt-in și protejate de redacție.
- [ ] Testele unitare trec.
- [ ] Testele Roslyn trec.
- [ ] Testele integration/E2E trec.
- [ ] Testele MCP trec.
- [ ] Search quality suite trece.
- [ ] Benchmark smoke trece.
- [ ] Documentația acoperă setup, config, usage, MCP, troubleshooting.
- [ ] Nu există TODO/stub-uri în codul final.
- [ ] Nu există warnings netratate în build-ul proiectelor noi.

---

## Roadmap de implementare recomandat pentru Codex

### Faza 0: Bootstrap și contracte

- [ ] Creează soluția și proiectele.
- [ ] Configurează `Directory.Build.props`.
- [ ] Configurează `Directory.Packages.props`.
- [ ] Adaugă logging/config/test infrastructure.
- [ ] Definește modelele principale și interfețele.
- [ ] Adaugă primele teste unitare pentru config și IDs.
- [ ] Rulează build/test.

### Faza 1: Repo discovery și workspace loading

- [ ] Implementează repo discovery.
- [ ] Implementează `.gitignore` + `.ridxignore`.
- [ ] Implementează MSBuildLocator.
- [ ] Implementează load `.sln`, `.slnf`, `.slnx`, `.csproj`, `.vbproj`.
- [ ] Persistă manifest minimal.
- [ ] Adaugă `ridx doctor` minimal.
- [ ] Adaugă teste discovery/workspace.
- [ ] Rulează build/test.

### Faza 2: Storage și migrații

- [ ] Implementează SQLite schema.
- [ ] Implementează migrations.
- [ ] Implementează repository/store APIs.
- [ ] Implementează FTS minimal.
- [ ] Adaugă teste DB/migrations.
- [ ] Rulează build/test.

### Faza 3: Symbol/reference extraction

- [ ] Implementează symbol extraction complet.
- [ ] Implementează reference/edge extraction complet.
- [ ] Implementează generated code policy.
- [ ] Persistă symbols/refs/edges.
- [ ] Adaugă teste Roslyn exhaustive.
- [ ] Rulează build/test.

### Faza 4: Chunking și text indexing

- [ ] Implementează chunking Roslyn.
- [ ] Implementează chunking text files.
- [ ] Implementează chunk semantic text.
- [ ] Implementează hashing/reuse.
- [ ] Persistă chunks.
- [ ] Adaugă teste chunking.
- [ ] Rulează build/test.

### Faza 5: Embeddings și vector backend

- [ ] Implementează fake/deterministic embedding provider pentru teste.
- [ ] Implementează local embedding provider.
- [ ] Implementează embedding cache.
- [ ] Implementează exactFlat vector index.
- [ ] Implementează localHnsw sau adapter local performant.
- [ ] Adaugă vector snapshot/recovery.
- [ ] Adaugă teste vector/embedding.
- [ ] Rulează build/test.

### Faza 6: Search smart

- [ ] Implementează query parser.
- [ ] Implementează lexical search.
- [ ] Implementează vector search.
- [ ] Implementează symbol search.
- [ ] Implementează merge/rerank/explain.
- [ ] Implementează JSON output.
- [ ] Adaugă search quality suite.
- [ ] Rulează build/test.

### Faza 7: CLI complet și context/graph

- [ ] Implementează toate comenzile CLI.
- [ ] Implementează context service.
- [ ] Implementează graph query service.
- [ ] Implementează impact analysis.
- [ ] Adaugă E2E CLI tests.
- [ ] Rulează build/test.

### Faza 8: Incremental watch și daemon

- [ ] Implementează incremental invalidation.
- [ ] Implementează watcher.
- [ ] Implementează daemon IPC.
- [ ] Implementează staleness și snapshots.
- [ ] Adaugă teste daemon/watch/recovery.
- [ ] Rulează build/test.

### Faza 9: MCP pentru Codex

- [ ] Implementează MCP STDIO.
- [ ] Implementează MCP HTTP opțional.
- [ ] Expune toate tools.
- [ ] Adaugă schemas și docs.
- [ ] Adaugă Codex config generator.
- [ ] Adaugă MCP tests.
- [ ] Rulează build/test.

### Faza 10: Performance, hardening și docs

- [ ] Adaugă benchmark-uri.
- [ ] Optimizează hot paths.
- [ ] Adaugă security tests.
- [ ] Completează docs.
- [ ] Rulează test suite completă.
- [ ] Rulează benchmark smoke.
- [ ] Curăță TODO/stub/warnings.
- [ ] Pregătește release notes.

---

## Criterii de acceptare pentru daily usage

- [ ] Pot rula `ridx index --full` într-un repo .NET real și primesc progres clar.
- [ ] Pot rula `ridx search "where is authentication configured"` și primesc rezultate semantice relevante.
- [ ] Pot rula `ridx search "UserService.GetById"` și primesc simbolul exact sau candidați foarte apropiați.
- [ ] Pot rula `ridx context <resultId>` și primesc context suficient pentru modificare.
- [ ] Pot rula `ridx impact <symbol>` înainte de refactor.
- [ ] Pot porni `ridx watch` și modificările mici sunt reflectate rapid.
- [ ] Pot conecta Codex prin MCP și Codex poate apela tools fără config manual complicat.
- [ ] Pot lucra fără internet folosind provider local.
- [ ] Dacă aleg provider remote, trebuie să confirm explicit și să văd warning de privacy.
- [ ] Dacă ceva eșuează, `ridx doctor` îmi spune concret ce și cum repar.

---

## Referințe tehnice consultate

- [ ] Roslyn SDK overview: https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/
- [ ] Roslyn semantic analysis: https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/get-started/semantic-analysis
- [ ] Microsoft.CodeAnalysis.Workspaces.MSBuild NuGet: https://www.nuget.org/packages/Microsoft.CodeAnalysis.Workspaces.MSBuild/
- [ ] Microsoft.Build.Locator / MSBuild versions: https://learn.microsoft.com/en-us/visualstudio/msbuild/find-and-use-msbuild-versions
- [ ] SymbolFinder.FindReferencesAsync: https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.findsymbols.symbolfinder.findreferencesasync
- [ ] Microsoft.Extensions.VectorData abstractions: https://www.nuget.org/packages/Microsoft.Extensions.VectorData.Abstractions/
- [ ] Qdrant documentation: https://qdrant.tech/documentation/
- [ ] sqlite-vec: https://github.com/asg017/sqlite-vec
- [ ] OpenAI Codex MCP documentation: https://developers.openai.com/codex/mcp
- [ ] OpenAI Docs MCP quickstart/config: https://developers.openai.com/learn/docs-mcp
- [ ] MCP tools specification: https://modelcontextprotocol.io/specification/2025-06-18/server/tools
- [ ] dotnet sln command and `.slnx` support: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-sln
- [ ] Microsoft.VisualStudio.SolutionPersistence: https://github.com/microsoft/vs-solutionpersistence
