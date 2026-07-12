# Plan: RoslynIndexer optimizat pentru agenti AI

**Data:** 2026-07-12  
**Status:** Implementat  
**Scop:** Transformarea RoslynIndexer dintr-un CLI corect semantic, dar costisitor la fiecare apel, intr-un serviciu MCP persistent, cu index query-oriented, latenta foarte mica, output compact si comenzi compuse construite special pentru fluxul de lucru al unui agent AI.

> Implementarea finala foloseste segmente binare content-addressed per document in locul tabelelor binare monolitice schitate initial. Alegerea pastreaza publish-ul atomic, reduce indexul Cerneala la aproximativ 13 MB si permite rescrierea stricta a documentelor dirty. Memory mapping a ramas intentionat neimplementat: buffered reads respecta bugetul initial de load, deci complexitatea suplimentara nu este justificata de benchmark.

## Rezumat executiv

RoslynIndexer are deja fundatia utila: indexare semantica Roslyn, cautare de simboluri, referinte, citire de fisiere, incrementalitate de baza si integrare MCP. Blocajul principal nu este lipsa de capabilitati, ci arhitectura de executie a query-urilor.

In forma actuala, fiecare apel:

1. citeste integral indexul JSONL de pe disc;
2. deserializeaza toate documentele, simbolurile, referintele si posting-urile;
3. reconstruieste dictionarele de lookup in memorie;
4. executa query-ul;
5. arunca toata starea la finalul apelului.

Pe repository-ul Cerneala, masuratorile din 2026-07-12 au fost:

| Operatie | Rezultat masurat |
|---|---:|
| Documente C# indexate | 910 |
| Simboluri | 24.861 |
| Referinte | 53.048 |
| Token postings | 452.324 |
| Cold index C#-only | 17,7 s |
| No-op incremental, 0 fisiere dirty | 15,1 s |
| Search one-shot | 2,1-2,6 s |
| Dimensiune totala index | aproximativ 129 MB |
| `tokens.jsonl` | 88,9 MB |

Prioritatea absoluta este reducerea latentei si a muncii repetate. Functionalitati precum ranking mai sofisticat sau extinderea `suggest` nu trebuie sa devanseze persistent query state, incrementalitatea reala si storage-ul orientat spre lookup.

## Principii de produs

1. **Agentul AI este clientul principal.** Contractele, comenzile si output-ul sunt optimizate pentru numar mic de round-trip-uri, latenta mica si consum redus de tokeni.
2. **MCP este suprafata principala.** CLI-ul ramane util pentru diagnostic, scripting si fallback, dar nu dicteaza arhitectura query path-ului MCP.
3. **Cost proportional cu query-ul.** Un lookup de simbol exact nu trebuie sa citeasca 129 MB si sa reconstruiasca toate posting-urile.
4. **No-op inseamna no-op.** Daca repository-ul si configuratia nu s-au schimbat, indexarea nu deschide MSBuildWorkspace si nu rescrie indexul.
5. **Output compact implicit.** Campurile duplicate, null-urile fara valoare si textul explicativ redundant sunt eliminate din raspunsurile MCP.
6. **Date structurate, nu proza.** Grafurile, relatiile si diagnosticele sunt returnate ca noduri, muchii, identificatori si coduri stabile.
7. **Bugetele sunt parte din contract.** Comenzile compuse accepta limite pentru rezultate, caractere, noduri, adancime si timp.
8. **Corectitudinea semantica nu se sacrifica.** Optimizarea nu transforma RoslynIndexer intr-un grep cu o palarie Roslyn pusa deasupra.

## Obiective masurabile

### Query path MCP

- Primul query dupa pornirea MCP: sub 500 ms pe repository-ul Cerneala.
- Query-uri ulterioare `search`: p50 sub 20 ms, p95 sub 50 ms.
- `goto` cu symbol ID sau FQN exact: p95 sub 10 ms.
- `refs` indexed: p95 sub 20 ms pentru maximum 100 rezultate.
- Nicio deserializare integrala a posting-urilor dupa incarcarea initiala.
- Nicio reconstructie a dictionarelor intre doua apeluri pe aceeasi generatie de index.

### Indexare

- No-op incremental: p95 sub 100 ms, fara pornire MSBuild/Roslyn workspace.
- Schimbare body-only intr-un fisier: sub 1 s pe Cerneala.
- Schimbare de declaratie locala: sub 2 s cand project graph-ul ramane stabil.
- Full cold index: tinta initiala sub 12 s pe Cerneala, apoi optimizare pe baza profilerului.
- Persistenta incrementala rescrie doar segmentele afectate.

### Storage si memorie

- Reducerea indexului Cerneala de la aproximativ 129 MB la maximum 50 MB in prima versiune binara.
- Eliminarea repetarii stringurilor pentru path, project, symbol ID si token.
- Memorie MCP stabila dupa warm-up, fara crestere proportionala cu numarul de query-uri.
- Zero copii integrale inutile ale colectiilor mari pe query path.

### Output pentru agent

- Raspunsurile nu dubleaza aceleasi date in `data` si `results`.
- Fiecare lista potential mare suporta `truncated` si `continuationToken`.
- Fiecare comanda suporta un profil `compact`, iar acesta este implicit in MCP.
- Comenzile compuse respecta `maxResults`, `maxChars`, `maxNodes`, `depth` si timeout.

## Non-obiective initiale

- AI, embeddings, vector database sau servicii cloud.
- Daemon de retea sau HTTP server.
- Index semantic complet pentru limbaje non-C#.
- Refacerea simultana a tuturor comenzilor CLI.
- Ranking bazat pe modele statistice.
- Watcher complex inainte ca fast path-ul determinist sa fie dovedit.
- Compatibilitate eterna cu indexurile vechi; rebuild automat este acceptabil la schimbarea majora de schema.

## Arhitectura tinta

### 1. `RepositoryIndexSession`

MCP pastreaza o sesiune per repository:

```text
RepositorySessionRegistry
  repoRoot -> RepositoryIndexSession
                generationId
                manifestFingerprint
                QueryIndex
                async reload gate
                usage/latency counters
```

`RepositoryIndexSession` este responsabil pentru:

- rezolvarea si normalizarea unui singur `repoRoot`;
- incarcarea generatiei curente;
- pastrarea lookup-urilor imutabile;
- verificarea ieftina a generatiei inainte de query;
- reload o singura data cand manifestul se schimba;
- swap atomic intre generatii;
- eliberarea resurselor generatiei vechi dupa terminarea reader-ilor activi.

Nu se introduce cache global static in Core. Lifetime-ul sesiunilor este detinut explicit de serverul MCP si poate fi testat izolat.

### 2. `QueryIndex`

`QueryIndex` contine numai structurile necesare query-urilor:

```text
symbolId -> SymbolRecord
lowerName -> symbol IDs
lowerFqn -> symbol IDs
termId -> posting slice
symbolId -> reference slice
documentId -> DocumentRecord
pathId -> document ID
project graph adjacency
call graph adjacency
type hierarchy adjacency
```

Colectiile sunt imutabile dupa publicare. Query-urile pot rula concurent fara lock global.

### 3. Storage binar segmentat

Format propus:

```text
.roslyn-index/
  current.json
  generations/
    <generation-id>/
      manifest.json
      strings.bin
      documents.bin
      symbols.bin
      references.bin
      terms.bin
      postings.bin
      callgraph.bin
      hierarchy.bin
      diagnostics.jsonl
```

Reguli:

- `current.json` indica atomic generatia activa.
- Tabelele folosesc ID-uri numerice compacte.
- Stringurile comune sunt interned o singura data.
- Posting lists sunt ordonate si delta-encoded.
- Valorile numerice folosesc varint unde aduce castig masurabil.
- Tabelele mari pot fi memory-mapped.
- Fiecare fisier are header cu magic, schema version, generation ID, row count si checksum.
- Scrierea se face intr-o generatie temporara, validata, apoi publicata atomic.
- Generatia anterioara ramane disponibila pana cand reader-ii curenti o elibereaza.

Nu se adauga SQLite, Lucene sau alt motor extern. Formatul ramane local si controlat de proiect.

### 4. Incrementalitate pe segmente

Indexarea este impartita explicit in:

1. repository/config fingerprint;
2. workspace graph fingerprint;
3. file change detection;
4. syntax/declaration change classification;
5. semantic reindex plan;
6. segment merge;
7. atomic publish.

Fast path no-op se opreste dupa pasul 3. Nu deschide workspace-ul si nu rescrie manifestul doar ca sa demonstreze ca nu avea nimic de facut.

Pentru fisiere modificate:

- body-only: actualizeaza tokenii, call edges si referintele locale afectate;
- declaration change: invalideaza simbolul si proiectele dependente necesare;
- project/config change: reconstruieste workspace graph-ul;
- schema/tool version change: full rebuild explicit.

### 5. Agent response layer

Core-ul returneaza modele semantice bogate. MCP aplica un response profile:

- `compact`: campuri strict necesare, implicit;
- `standard`: include snippets si explicatii de matching;
- `diagnostic`: include timings, cache state si detalii de scoring.

Trunchierea se face determinist si este raportata explicit. Nicio comanda nu returneaza accidental sute de mii de tokeni.

## Contract MCP comun

Toate comenzile noi si existente trebuie sa foloseasca un envelope unic:

```json
{
  "success": true,
  "tool": "roslyn_goto",
  "repoRoot": "C:/repo",
  "generationId": "01J...",
  "elapsedMs": 7,
  "cache": {
    "sessionHit": true,
    "generationReloaded": false
  },
  "truncated": false,
  "continuationToken": null,
  "data": {}
}
```

### Reguli de schema

- Tipuri JSON exacte, fara union generic `string | number | boolean | null` pentru orice proprietate.
- Enum-uri reale pentru `mode`, `kind`, `direction`, `profile` si `include`.
- `minimum` si `maximum` pentru limite numerice.
- Required fields corecte pentru fiecare tool.
- Reguli mutual exclusive pentru variantele de partial read.
- Default-uri declarate in schema.
- `additionalProperties: false`.
- Erori cu `code`, `message`, `retryable` si `suggestedAction`.
- `repoRoot` devine optional cand serverul este pornit repo-bound.
- Se elimina duplicarea dintre `data` si `results`.

## Comenzi noi

### `roslyn_inspect`

Comanda principala pentru intelegerea unui simbol intr-un singur round-trip.

Input conceptual:

```json
{
  "symbol": "UIElement.InvalidateMeasure",
  "include": [
    "source",
    "signature",
    "documentation",
    "containingType",
    "baseTypes",
    "members",
    "callers",
    "callees",
    "references",
    "implementations",
    "tests"
  ],
  "depth": 1,
  "maxResults": 80,
  "maxChars": 30000,
  "profile": "compact"
}
```

Output-ul contine identificatori reutilizabili de celelalte comenzi. Ambiguitatea nu este ascunsa: daca query-ul rezolva mai multe simboluri, comanda returneaza candidatii si un cod `ambiguous-symbol`.

### `roslyn_outline`

Returneaza structura semantica a unui fisier, tip sau namespace fara continutul complet al fisierului.

Trebuie sa includa:

- namespaces si tipuri;
- membri cu kind, accessibility, signature si span;
- relatii base/interface;
- optional private/generated members;
- nesting pana la `depth`.

Este comanda implicita de orientare inainte de `roslyn_read` pentru fisiere mari.

### `roslyn_context`

Construieste un pachet compact pentru o locatie sau un simbol:

- containing symbol;
- fragmentul sursa relevant;
- declaratiile dependintelor directe;
- callers/callees limitati;
- teste candidate;
- diagnostics locale, daca sunt disponibile.

Bugetul de caractere este obligatoriu si respectat dupa ordonarea relevantei.

### `roslyn_callgraph`

Returneaza graf structurat:

```json
{
  "nodes": [
    { "id": "...", "name": "...", "kind": "method", "path": "...", "line": 10 }
  ],
  "edges": [
    { "from": "...", "to": "...", "kind": "invocation" }
  ]
}
```

Suporta `direction: callers | callees | both`, `depth`, `maxNodes`, `includeTests` si `includeExternal`.

### `roslyn_impact`

Raspunde la intrebarea "ce poate fi afectat daca modific acest simbol sau fisier?".

Include:

- callers si references;
- derived types si implementations;
- overrides;
- public API exposure;
- proiecte dependente;
- teste candidate;
- nivel de incredere determinist si motivul fiecarei legaturi.

Nu pretinde ca prezice comportamentul runtime. Returneaza impact semantic si structural demonstrabil.

### `roslyn_batch`

Executa mai multe operatii intr-un singur round-trip si permite dependente intre ele:

```json
{
  "operations": [
    { "id": "definition", "operation": "goto", "query": "UIElement" },
    { "id": "uses", "operation": "refs", "symbolFrom": "definition:0" },
    { "id": "shape", "operation": "outline", "fileFrom": "definition:0" }
  ],
  "maxChars": 40000,
  "timeoutMs": 1000
}
```

Operatiile permise sunt enumerate explicit; batch-ul nu devine un shell generic. Un esec poate fi configurat `stop` sau `continue`, iar fiecare rezultat pastreaza ID-ul operatiei.

### `roslyn_changes`

Produce semantic diff fata de:

- working tree versus `HEAD`;
- index generation curenta versus precedenta;
- doua commit-uri locale;
- doua generatii de index.

Returneaza simboluri adaugate, sterse si modificate, signature changes, public API changes si proiecte afectate.

### `roslyn_tests_for`

Rankeaza testele relevante pentru symbol, file sau change set folosind numai:

- referinte semantice;
- call graph;
- project references;
- naming conventions;
- path proximity;
- istoric local optional numai daca este disponibil fara retea.

Fiecare candidat include motivele scorului. Comanda nu ruleaza testele.

### `roslyn_capabilities`

Returneaza versiunea serverului, comenzile, schema indexului, repository binding, starea sesiunii si limitele suportate. Aceasta comanda rezolva cazul in care agentul nu stie daca MCP-ul este instalat, configurat sau compatibil.

### `roslyn_profile`

Diagnostic local pentru dezvoltarea tool-ului, nu pentru utilizare zilnica. Returneaza:

- load/reload timings;
- query stage timings;
- allocation estimates disponibile;
- dimensiunea segmentelor;
- cache hit rates;
- top term posting sizes;
- no-op index breakdown.

Nu trimite telemetrie si nu persista date in afara `.roslyn-index/`.

## Upgrade-uri pentru comenzile existente

### `roslyn_search`

- Foloseste lookup direct pentru symbol exact si FQN exact.
- Evita scanarea tuturor simbolurilor/referintelor daca exista candidati exacti.
- Adauga `fields`, `profile`, `continuationToken` si bugete explicite.
- Returneaza `matchReason` ca enum plus componente de scor in profil diagnostic.
- Aplica timeout-ul si in load/reload, nu doar in scoring.

### `roslyn_goto`

- Accepta direct `symbolId` fara query textual.
- Returneaza signature si declaration span compact.
- Diferentiaza declaration, partial declaration si generated declaration.

### `roslyn_refs`

- Grupeaza optional dupa `referenceKind`, file sau project.
- Suporta paging stabil.
- Exact refs foloseste o sesiune Roslyn reutilizabila optional, nu porneste workspace complet la fiecare apel.
- Cache-ul exact este invalidat pe baza simbolului si a proiectelor afectate, nu doar prin timestamp global.

### `roslyn_read` si `roslyn_pread`

- Returneaza line numbers numai cand sunt cerute.
- Adauga `maxChars` si semnal explicit de trunchiere.
- Adauga `contentHash` pentru verificarea ca fisierul nu s-a schimbat intre read si edit.
- `pread` accepta semantic span: containing member, declaration sau body.

### `roslyn_status`

- Nu citeste integral fisierele JSONL doar pentru counts.
- Citeste counts si generation state direct din manifest.
- Raporteaza separat `indexState`, `sessionState` si `workspaceState`.

### `roslyn_doctor`

- Include verificarea MCP repo binding.
- Include schema/tool compatibility.
- Include motivul exact pentru care indexul este stale.
- Are un mod `quick` fara deschiderea workspace-ului si un mod `deep` explicit.

### `roslyn_suggest`

- Ramane functional, dar nu primeste investitii majore inaintea comenzilor semantice compuse.
- Poate deveni un router determinist catre `inspect`, `impact`, `callgraph` si `tests_for`.
- Nu mai genereaza stringuri CLI cand clientul este MCP; returneaza operatii structurate.

## Etape de implementare

### Etapa 0: baseline reproductibil

- [ ] Adauga un corpus benchmark determinist cu clase small, medium si Cerneala-like.
- [ ] Separa timpul de process startup, index load, lookup build, scoring si snippet hydration.
- [ ] Masoara p50/p95 dupa warm-up pentru minimum 100 query-uri.
- [ ] Masoara allocations si peak working set pentru load si query.
- [ ] Masoara dimensiunea fiecarui fisier de index.
- [ ] Salveaza baseline-ul ca artifact de test/benchmark, nu ca prag dependent de masina in unit tests.
- [ ] Adauga un benchmark pentru 20 apeluri MCP in acelasi proces.
- [ ] Adauga un benchmark no-op incremental care verifica explicit ca MSBuild nu este pornit.

**Gate:** Nicio optimizare nu este acceptata fara comparatie cu baseline-ul si fara test functional echivalent.

### Etapa 1: sesiune MCP persistenta

- [ ] Introdu `RepositorySessionRegistry` cu lifetime detinut de host-ul MCP.
- [ ] Introdu `RepositoryIndexSession` si `QueryIndex` imutabil.
- [ ] Incarca indexul o singura data per generation ID.
- [ ] Detecteaza schimbarea generatiei printr-o citire mica a manifestului curent.
- [ ] Implementeaza reload single-flight.
- [ ] Implementeaza swap atomic si concurenta intre readeri.
- [ ] Adauga eviction configurabil pentru mai multe repository-uri, fara timer agresiv.
- [ ] Instrumenteaza `sessionHit`, `reloadCount`, `loadMs` si `queryMs`.
- [ ] Adauga teste concurente pentru query in timpul reload-ului.

**Gate:** 100 query-uri consecutive nu reconstruiesc lookup-urile, iar p95 warm search este sub 50 ms pe corpusul Cerneala-like.

### Etapa 2: fast path pentru no-op incremental

- [ ] Separa discovery fingerprint de workspace load.
- [ ] Calculeaza config si workspace input fingerprint fara Roslyn.
- [ ] Foloseste datele Git disponibile pentru lista de fisiere schimbate.
- [ ] Adauga fallback filesystem determinist cand Git nu este disponibil.
- [ ] Nu recalcula content hash pentru toate fisierele daca metadata si Git confirma ca sunt neschimbate.
- [ ] Returneaza imediat cand change set-ul este gol.
- [ ] Nu rescrie indexul, diagnostics sau generation pointer pe no-op.
- [ ] Adauga test care interzice crearea MSBuildWorkspace pe no-op.

**Gate:** No-op incremental sub 100 ms p95 pe Cerneala si zero fisiere modificate in `.roslyn-index/`.

### Etapa 3: contract MCP compact si strict

- [ ] Inlocuieste schema generica cu JSON Schema per tool.
- [ ] Elimina campul duplicat `results` sau `data`.
- [ ] Adauga response profiles.
- [ ] Adauga structured error codes.
- [ ] Adauga paging si continuation tokens semnate local cu generation ID.
- [ ] Fa `repoRoot` optional pentru server repo-bound.
- [ ] Adauga `roslyn_capabilities`.
- [ ] Testeaza serializarea exacta si compatibilitatea schema-contract.

**Gate:** Raspunsul compact pentru un `goto` exact este sub 2 KB si nu contine campuri duplicate.

### Etapa 4: storage binar query-oriented

- [ ] Defineste formatul si documenteaza invariants/header-ele.
- [ ] Introdu string table si ID-uri numerice.
- [ ] Implementeaza writer si reader pentru documents/symbols.
- [ ] Implementeaza term dictionary si posting slices.
- [ ] Implementeaza references grouped by symbol ID.
- [ ] Adauga checksum si detectare de truncation/corruption.
- [ ] Adauga memory mapping numai dupa benchmark comparativ cu buffered reads.
- [ ] Pastreaza temporar reader-ul JSONL pentru migrare si teste comparative.
- [ ] Adauga comanda interna de rebuild/migrate prin `roslyn_index --force`.
- [ ] Elimina reader-ul vechi dupa o perioada explicita de compatibilitate.

**Gate:** Index sub 50 MB pe Cerneala, rezultate byte-for-byte echivalente semantic si primul load sub 500 ms.

### Etapa 5: persistenta incrementala pe segmente

- [ ] Introdu per-document ownership pentru simboluri, referinte si postings.
- [ ] Scrie numai segmentele documentelor dirty.
- [ ] Implementeaza segment merge/compaction determinist.
- [ ] Clasifica body-only versus declaration change cu syntax/semantic declaration hash robust.
- [ ] Invalideaza proiectele dependente numai cand forma declaratiilor o cere.
- [ ] Pastreaza generatia veche pana la publicarea si validarea celei noi.
- [ ] Adauga recovery dupa process kill in fiecare etapa de publicare.

**Gate:** Modificarea body-only a unui fisier nu rescrie segmente fara legatura si termina sub 1 s pe Cerneala.

### Etapa 6: `roslyn_outline`, `roslyn_inspect` si `roslyn_context`

- [ ] Defineste modele comune pentru symbol summary, source span si related item.
- [ ] Implementeaza outline din index, fara workspace Roslyn on-demand.
- [ ] Implementeaza resolver strict pentru symbol ID/FQN/query ambiguu.
- [ ] Implementeaza inspect cu include flags si bugete.
- [ ] Implementeaza context ranking determinist.
- [ ] Evita duplicarea aceluiasi fragment sau simbol in acelasi raspuns.
- [ ] Adauga teste de trunchiere si stabilitate a ordinii.

**Gate:** Investigarea unui simbol tipic necesita un singur apel `inspect`, iar output-ul respecta exact `maxChars`.

### Etapa 7: call graph, hierarchy si impact

- [ ] Indexeaza invocation edges separat de referintele generice.
- [ ] Indexeaza base type, interface implementation si override edges.
- [ ] Introdu `roslyn_callgraph` cu traversare bounded.
- [ ] Introdu `roslyn_impact` cu motive deterministe.
- [ ] Detecteaza si marcheaza nodurile externe sau nerezolvate.
- [ ] Protejeaza traversarea impotriva ciclurilor si graph explosion.
- [ ] Adauga teste pentru overloads, extension methods, virtual dispatch si partial methods.

**Gate:** Grafurile sunt stabile, bounded si nu confunda overload-urile cu acelasi nume.

### Etapa 8: batch, changes si test selection

- [ ] Implementeaza executorul bounded pentru `roslyn_batch`.
- [ ] Valideaza referintele intre operatii inainte de executie.
- [ ] Refoloseste aceeasi sesiune si aceeasi generatie pentru intregul batch.
- [ ] Implementeaza semantic diff intre generatii.
- [ ] Integreaza working tree/HEAD fara acces la retea.
- [ ] Implementeaza `roslyn_tests_for` cu scoring explicabil.
- [ ] Adauga limite globale pentru timp, output si numar de operatii.

**Gate:** Un batch `goto -> refs -> outline` face un singur generation check si are latenta mai mica decat trei apeluri separate.

### Etapa 9: hardening si observabilitate locala

- [ ] Introdu `roslyn_profile` si stage timings consistente.
- [ ] Adauga stress test cu query-uri concurente si reindexari repetate.
- [ ] Adauga test de memory stability pe minimum 10.000 query-uri.
- [ ] Adauga test de crash recovery pentru fiecare punct de publicare.
- [ ] Adauga test de schema incompatibila si rebuild action clar.
- [ ] Adauga fuzz tests pentru query/schema/continuation token.
- [ ] Verifica path traversal si izolarea stricta la repo root.

**Gate:** Nicio corupere, deadlock, crestere continua de memorie sau raspuns dintr-o generatie amestecata.

## Strategie de testare

### Teste unitare

- codec binar si varint;
- string table;
- posting list encode/decode;
- generation pointer;
- query exact/prefix/token;
- paging si continuation token;
- graph traversal bounded;
- response profile si truncation;
- config/fingerprint classification.

### Teste de integrare

- full index -> load -> query;
- no-op incremental;
- body-only update;
- declaration update;
- project reference update;
- query concurent cu atomic generation swap;
- process kill inainte si dupa publish;
- index corupt/trunchiat;
- server repo-bound versus explicit repo root;
- backward compatibility pe perioada migrarii.

### Benchmark-uri

- BenchmarkDotNet pentru codec, load si query hot paths;
- test separat end-to-end MCP cu proces persistent;
- corpusuri versionate small/medium/large;
- rezultate p50/p95 si allocations;
- size budget per table;
- comparatie cu baseline-ul din Etapa 0.

Testele functionale nu folosesc praguri largi de zeci de secunde ca dovada de performanta. Performance budgets sensibile la hardware ruleaza in benchmark job dedicat; testele CI obisnuite verifica invariants precum "nu a fost apelat workspace loader" si "indexul nu a fost rescris".

## Migrare si compatibilitate

1. Creste schema indexului si adauga `storageFormat` in manifest.
2. Reader-ul detecteaza JSONL vechi si raspunde cu actiunea structurata `rebuild-required`.
3. `roslyn_index` reconstruieste in formatul nou intr-o generatie separata.
4. MCP continua sa serveasca generatia veche pana cand cea noua este validata.
5. Dupa publish, sesiunea face swap atomic.
6. Nu se incearca transformarea in-place a fisierelor vechi.

Contractele MCP existente raman disponibile initial, dar raspunsurile compacte noi trebuie versionate clar daca eliminarea duplicarii `data/results` este breaking.

## Riscuri si masuri

### Memorie prea mare in procesul MCP

Sesiunea persistenta poate muta costul de pe CPU pe memorie. Se masoara working set-ul, se folosesc structuri compacte si memory mapping numai unde benchmark-ul demonstreaza castig. Registry-ul are eviction explicit pentru repository-uri inactive.

### Format binar greu de intretinut

Formatul primeste specificatie, headers versionate, readers mici per tabela, golden fixtures si teste corruption. Nu se inventeaza un mini-database general; se implementeaza strict operatiile necesare.

### Incrementalitate semantic incorecta

Body-only si declaration-change sunt validate prin teste diferentiale: rezultatul incremental trebuie sa fie identic cu un full rebuild pe acelasi repository.

### Reload in timpul query-ului

Fiecare query captureaza o generatie imutabila. Nu combina date din generatii diferite. Generatia veche este eliberata numai dupa iesirea ultimului reader.

### Comenzi compuse cu output exploziv

Toate traversarile au depth/maxNodes/maxResults/maxChars si timeout. Trunchierea este determinista, vizibila si paginabila.

### Optimizare prematura a cold indexului

Cold index este important, dar query latency si no-op incremental au prioritate mai mare. Se profileaza inainte de paralelizare sau caching semantic complicat.

## Ordine recomandata de livrare

1. Baseline si instrumentare.
2. Sesiune MCP persistenta.
3. Fast path no-op.
4. Contract MCP compact si `roslyn_capabilities`.
5. Storage binar.
6. Persistenta incrementala pe segmente.
7. `outline`, `inspect`, `context`.
8. Call graph si impact.
9. Batch, semantic changes si test selection.
10. Hardening, profiling si eliminarea formatului vechi.

Nu se incepe cu `suggest` sau cu ranking sofisticat. A face sugestii mai inteligente peste un query de 2,5 secunde inseamna sa pui spoiler pe tractor.

## Verificare finala

1. Ruleaza build-ul complet fara warnings si errors.
2. Ruleaza toate testele RoslynRepoIndexer.
3. Ruleaza testele diferentiale full versus incremental.
4. Ruleaza benchmark-urile pe corpusurile stabilite.
5. Verifica bugetele p50/p95, allocations, working set si disk size.
6. Ruleaza minimum 10.000 query-uri intr-o singura sesiune MCP.
7. Ruleaza query-uri concurente in timpul reindexarii.
8. Simuleaza process kill si valideaza recovery-ul generatiei anterioare.
9. Verifica toate JSON Schema-urile MCP fata de contractele C#.
10. Ruleaza `git diff --check`.
11. Regenereaza `FileTree.md` daca apar fisiere noi.
12. Reindexeaza `Cerneala.slnx` si confirma index valid, fara dirty files sau warnings.

## Criterii de acceptare

Implementarea este considerata completa numai cand:

- query-urile MCP reutilizeaza o sesiune persistenta si o generatie imutabila;
- warm search p95 este sub 50 ms pe corpusul Cerneala-like;
- no-op incremental este sub 100 ms si nu deschide MSBuildWorkspace;
- indexul Cerneala ocupa maximum 50 MB;
- un body-only change nu provoaca full persist sau full semantic rebuild;
- contractele MCP sunt stricte, compacte si fara campuri duplicate;
- `inspect`, `outline`, `context`, `callgraph`, `impact`, `batch`, `changes` si `tests_for` sunt bounded si testate;
- incremental build produce aceleasi rezultate ca full rebuild;
- query-urile concurente nu observa generatii partiale;
- crash recovery pastreaza ultima generatie valida;
- memoria ramane stabila pe 10.000 de query-uri;
- documentatia tool-ului descrie arhitectura, comenzile si bugetele reale de performanta.

## Decizii care trebuie confirmate inainte de implementare

1. Format binar custom versus pastrarea JSONL pentru tabelele mici; recomandarea este binar pentru query tables si JSON numai pentru manifest/diagnostics.
2. Memory mapping de la prima versiune versus dupa buffered binary reader; recomandarea este benchmark intai, apoi alegere.
3. Breaking change imediat pentru eliminarea `data/results` versus versionare temporara; recomandarea este o versiune noua de contract MCP.
4. O singura sesiune repo-bound versus registry multi-repo; recomandarea este suport intern multi-repo, dar configuratie repo-bound implicita.
5. Exact Roslyn workspace persistent versus cache on-demand; recomandarea este amanarea lui pana dupa optimizarea indexed refs si masurarea utilizarii reale.
