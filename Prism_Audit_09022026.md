# Audit Prism — 2026-09-02

## Verdict

**CONFORM DUPĂ REMEDIERE pentru PRISM-01–PRISM-04.** La commitul auditat,
Prism era neconform: raportul `PASS` nu era reproductibil, verificarea depindea
de EOL, un test incremental era rupt, iar designul descria o arhitectură veche.
Cele patru constatări au fost remediate și reverificate în worktree-ul temporar
`C:\Users\lauri\Desktop\Cerneala-Prism-Fixes-09022026`.

Verdictul nu ascunde o abatere separată descoperită la poarta nativă ulterioară:
131 din 132 cazuri de conformance Prism WindowsDX–SDL GPU trec, iar `SpinBlur`
depășește pragul maxim (`62`, prag `49`). Artefactele shader binare inițiale și
cele regenerate din sursa nemodificată reproduc identic abaterea; ea nu este
cauzată de remedierile PRISM-01–PRISM-04 și rămâne în afara verdictului lor.

Auditul a identificat două defecte cu severitate ridicată și două derapaje de contract/verificare cu severitate medie:

1. verificarea shaderelor SDL și o parte din auditul Prism depind de line endings din checkout;
2. utilitarul canonic `PrismAudit` nu mai poate valida arhitectura împărțită pe assemblies și raportul generat este stale;
3. proiectul principal de teste are un test Prism rupt după mutarea shaderelor de stil;
4. designul tehnic încă declară backendurile non-MonoGame ca fiind amânate, deși SDL GPU este implementat, testat și benchmark-uit.

Nu am găsit, în scenariile automate rulate, o dovadă că nucleul grafului Prism sau rutarea resource-free din SDL produc rezultate funcționale greșite. Asta nu dovedește paritate vizuală: testele native de pixeli au fost omise de harness în mediul local.

## Baseline și metodă

- Commit auditat: `91299f4e3ef164728299ed36f8649af7e97c0fd5`.
- Auditul a fost executat într-un worktree separat: `C:\Users\lauri\Desktop\Cerneala-Prism-Audit-09022026`.
- Worktree-ul principal nu a fost folosit pentru build, teste sau experimente și modificările existente ale utilizatorului nu au fost atinse.
- Suprafața inspectată: catalogul și generatorul Prism, graful/runtime-ul, API-ul public de nivel înalt, integrarea UI, backendurile MonoGame și SDL GPU, compilarea și verificarea shaderelor, testele, benchmarkurile și documentația contractuală.
- Navigarea C# a fost făcută cu RoslynRepoIndexer după regenerarea `FileTree.md` și indexarea `Cerneala.slnx`.
- Nu au fost folosite skilluri.

Clasificare:

- **Ridicată**: blochează buildul/verificarea reproductibilă sau face ca un control canonic să raporteze o stare falsă.
- **Medie**: rupe o poartă relevantă ori lasă contractul arhitectural ambiguu, fără dovadă directă de defect runtime în scenariile rulate.

## Constatări

### PRISM-01 — Ridicată — Artefactele și hashurile text depind de EOL-ul checkout-ului

**Comportament observat**

Într-un worktree Windows curat, cu `core.autocrlf=true`, buildul testelor a eșuat înainte să ruleze testele relevante:

```text
SDL shader artifact '...\Cerneala.Backends.SdlGpu\Gpu\Shaders\Drawing.vert.msl' is missing or stale.
```

Fișierul `Drawing.vert.msl` avea 895 octeți și 35 secvențe CRLF în checkout. După regenerarea prin `Cerneala.SdlShaderCompiler`, același conținut logic avea 860 octeți, 35 LF și zero CRLF. Verificarea SDL a trecut după regenerare, dar `Shaders/artifacts.json` și cele cinci fișiere `.msl` au devenit modificate local doar din cauza reprezentării textului.

Aceeași problemă afectează hashul catalogului Prism:

- hash pe checkout CRLF: `36f683274710418ecd6e01a4d3e60a959b1b746806b23c60d79a1c9a49ef8d7a`;
- hash după normalizare LF: `93078fa3c59727a37d406a34e566bf5339e60301aa838a317267e4c10d622f4e`;
- al doilea hash este exact valoarea publicată în raportul Prism generat.

Pentru manifestul shaderelor SDL, hashul sursei agregate a oscilat analog între `7CDC7F...` (LF, valoarea comisă) și `5E96D4...` (CRLF).

`git check-attr text eol` raportează `unspecified` pentru fișierele `.msl` și pentru `prism-catalog.json`; repository-ul nu fixează contractul EOL pentru aceste intrări.

**Cauză confirmată**

`Tools/Cerneala.SdlShaderCompiler/Program.cs` compară artefactele text prin `File.ReadAllBytes(...).SequenceEqual(...)` și calculează hashuri din bytes raw. `Tools/PrismAudit/Program.cs` calculează de asemenea hashul fișierului raw. Aceste implementări cer identitate byte-for-byte, în timp ce Git poate rescrie EOL-urile la checkout. Contractele se contrazic.

**Impact**

- un checkout curat și uzual pe Windows nu poate construi proiectele care verifică artefactele SDL;
- `PrismAudit --check` produce un gap fals de hash;
- regenerarea produce churn în fișiere fără schimbare semantică;
- reproducibilitatea artefactelor este dependentă de configurația locală Git.

**Remediere recomandată**

Stabiliți un singur contract, apoi testați-l pe Windows și Linux:

1. fixați în `.gitattributes` EOL-ul pentru sursele și artefactele text generate (`*.hlsl`, `*.msl`, JSON și rapoarte generate relevante), preferabil LF; și/sau
2. canonicalizați explicit EOL-ul înainte de hashing și înainte de comparația artefactelor text;
3. păstrați comparația raw pentru artefactele binare (`.dxil`, `.spv`);
4. adăugați un test care verifică același input în variante LF și CRLF.

**Stare după remediere: ÎNCHIS.** `.gitattributes` fixează LF pentru sursele și
artefactele text contractuale. Compilatorul SDL canonicalizează EOL înainte de
hash și comparație pentru manifest, HLSL, MSL și metadata, păstrând comparația
raw pentru DXIL/SPIR-V. `PrismAudit` canonicalizează analog documentele și
raportul. Un experiment ostil cu HLSL, MSL, catalog, metadata și raport trecute
explicit la CRLF a lăsat ambele comenzi `--verify`/`--check` verzi.

### PRISM-02 — Ridicată — Auditul canonic raportează `PASS`, dar reproducerea sa eșuează

**Comportament observat**

`docs/prism-completeness-report.generated.md` declară:

```text
Status: PASS
Coverage gaps: 0
Catalog version: 1.0.0 (178 entries)
```

Comanda de reproducere publicată în același raport, `dotnet run --project .\Tools\PrismAudit\PrismAudit.csproj -- --check`, a eșuat cu 6 gaps în checkoutul inițial. După normalizarea catalogului la LF, gapul de hash a dispărut, dar au rămas 5 gaps independente:

- tipul MonoGame `IMonoGameBackdropFrameLease` lipsește din inventarul observat;
- `MonoGameDrawingBackend`, `MonoGameUiHost` și `MonoGameUiHostOptions` lipsesc;
- proprietățile așteptate pentru `MonoGameUiHostOptions` lipsesc;
- `DrawingFrameContext.StateAnalysis` este public, dar nu apare în baseline;
- întreaga familie nouă de API-uri publice high-level (`Prism`, `PrismImage`, `PrismPipeline`, filtre și stiluri) apare ca neașteptată.

**Cauză confirmată pentru tipurile MonoGame lipsă**

`Tools/PrismAudit/Program.cs` pornește inventarul din:

```csharp
Assembly assembly = typeof(PrismInstance).Assembly;
```

și auditează doar `assembly.GetExportedTypes()`. Acesta este assembly-ul core `Cerneala`, în timp ce baseline-ul cere și tipuri din assembly-ul separat MonoGame. `Cerneala.csproj` exclude explicit sursele `Drawing/MonoGame/**` și `UI/Hosting/MonoGame/**`, deci utilitarul nu poate găsi tipurile cerute folosind assembly-ul ales.

**Contract rezolvat din istoricul repository-ului**

Commitul `fc041bfb` a introdus explicit API-ul public `PrismImage`/`PrismPipeline`
și cele 144 tipuri catalog-generated, iar `a4294734` l-a păstrat în livrarea
RenderSurface2D. `DrawingFrameContext.StateAnalysis` a fost introdus și documentat
separat în `e1f21268`; nu este membru Prism. Baseline-ul vechi era partea stale,
nu implementarea publică actuală.

**Factor de propagare**

Nu există nicio referință la `PrismAudit` sau `prism-completeness-report` în `.github`. Controlul canonic nu este impus de workflowurile CI curente, ceea ce explică de ce un raport `PASS` nereproductibil a putut rămâne comis.

**Impact**

- raportul canonic oferă încredere falsă;
- driftul de API și de assemblies nu este detectat înainte de merge;
- `PASS` nu poate fi folosit ca dovadă pentru completitudinea Prism.

**Remediere recomandată**

1. faceți inventarul explicit pe toate assemblies contractuale, nu pe assembly-ul unui singur tip marker;
2. decideți dacă noul API high-level și `StateAnalysis` sunt parte aprobată din API-ul public;
3. actualizați baseline-ul numai după acea decizie;
4. regenerați raportul și cereți ca `PrismAudit --check` să treacă într-un checkout curat;
5. adăugați comanda în CI și eșuați jobul când raportul generat diferă sau există gaps.

**Stare după remediere: ÎNCHIS.** Auditul încarcă explicit assemblies core și
MonoGame, derivă tipurile high-level din catalog, compară pe tipurile extinse doar
membrii Prism/backdrop, iar raportul regenerat inventariază 217 tipuri Prism și
10 tipuri extinse. Proiectul auditului este membru al `Cerneala.slnx`, iar comanda
Release `--no-build --no-restore` trece exact ca în jobul WindowsDX din
`desktop-backends.yml`; trigger-ele includ sursele, API docs, manifestul și
documentele contractuale relevante.

### PRISM-03 — Medie — Testul de incrementality al shaderelor de stil indică o cale eliminată

**Comportament observat**

Atât filtrul Prism, cât și întregul proiect `Cerneala.Tests` eșuează în:

```text
PrismShaderBuildIncrementalityTests.StyleShadersCompileInAnIndependentIncrementalPackage
DirectoryNotFoundException: ...\Drawing\MonoGame\Prism\Shaders\Styles
```

Linia 27 din test enumeră `Drawing/MonoGame/Prism/Shaders/Styles/*.fx`. Directorul nu există. `Styles.fx` include acum 13 module partajate din `Drawing/Prism/Shaders/Hlsl/Styles/*.hlsl`, iar proiectul MonoGame folosește aceeași locație partajată.

**Cauză confirmată**

Fixture-ul testului nu a fost actualizat după unificarea modulelor de stil în arborele HLSL backend-neutral.

**Impact**

- proiectul principal de teste rămâne roșu: 1 fail din 3.158 teste;
- testul nu mai verifică invariantul declarat de incrementality;
- un regres real de packaging incremental poate trece neobservat deoarece testul moare înainte de aserțiuni.

**Remediere recomandată**

Actualizați testul pentru structura partajată reală și păstrați aserțiunea asupra graniței de rebuild: o schimbare într-un modul de stil trebuie să reconstruiască pachetul de stil necesar, fără recompilarea nejustificată a pachetului de filtre. Confirmați RED pentru o dependență incrementală greșită, apoi GREEN pentru structura actuală.

**Stare după remediere: ÎNCHIS.** Testul enumeră acum modulele reale
`Drawing/Prism/Shaders/Hlsl/Styles/*.hlsl`, verifică include-urile din `Styles.fx`
și globul `PrismStyleInclude` al proiectului. Testul focalizat și proiectul core
complet trec.

### PRISM-04 — Medie — Documentul arhitectural nu descrie sistemul livrat

**Comportament observat**

`docs/prism-technical-design.md` declară explicit că Prism composers pentru alte backenduri decât MonoGame sunt amânate și descrie MonoGame drept backendul concret. Repository-ul conține însă:

- implementarea `Cerneala.Backends.SdlGpu/Prism/**`;
- compilator și manifest de shadere SDL offline;
- teste SDL Prism;
- workflow de desktop backends;
- un raport benchmark WindowsDX versus SDL GPU din 2026-08-27.

**Impact**

- documentul care ar trebui să definească ownership-ul arhitectural oferă o hartă falsă;
- contribuțiile viitoare pot pune codul sau contractele în stratul greșit;
- auditul generat folosește drept input un document de design care nu mai reprezintă starea implementată.

**Remediere recomandată**

Actualizați designul pentru modelul multi-backend real: ce rămâne backend-neutral, ce deține fiecare executor, ce paritate este obligatorie și ce capabilități sunt încă amânate. Sincronizați apoi hashul contractual și raportul generat.

**Stare după remediere: ÎNCHIS.** Designul descrie acum ownership-ul comun HLSL,
executarea MonoGame/WindowsDX și SDL GPU, resursele backend-owned, porțile native,
suprafața publică actuală și capabilitățile încă amânate. Raportul canonic a fost
regenerat cu hashurile documentelor actualizate.

## Dovezi pozitive

Următoarele rezultate sunt reale și merită păstrate, dar nu anulează constatările de mai sus:

- generatorul catalogului are 50/50 teste Prism trecute;
- parserul/limbajul are 40/40 teste Prism trecute;
- proiectul SDL GPU a trecut 33 teste Prism, cu 2 teste native omise;
- proiectul core a trecut 3.155 din 3.158 teste; singurul eșec este PRISM-03, iar două teste native au fost omise;
- testele SDL cu API fake parcurg intrările resource-free ale catalogului și verifică lipsa fallbackului pentru acea suprafață;
- catalogul are 178 intrări și este folosit transversal de generator, graph builder, kernel registry, teste și raportare;
- benchmarkul comis din 2026-08-27 oferă un baseline comparativ WindowsDX/SDL, inclusiv frame CPU, alocări și memorie GPU. Nu a fost remăsurat în acest audit.

## Matricea verificării auditului inițial

| Verificare | Rezultat |
| --- | --- |
| `New-FileTree.ps1` și citire `FileTree.md` | executat în worktree-ul temporar |
| RoslynRepoIndexer build + index `Cerneala.slnx` | 3.575 documente, 92.458 simboluri, 358.027 referințe; 10 warnings de indexare |
| Build incremental `tests/Cerneala.Tests` după regenerarea artefactelor SDL | PASS, 0 warnings, 0 errors |
| `Cerneala.Tests.SourceGen`, filtru Prism | PASS: 50/50 |
| `Cerneala.Tests.Language`, filtru Prism | PASS: 40/40 |
| `Cerneala.Tests.SdlGpu`, filtru Prism | PASS: 33; SKIP: 2 |
| `Cerneala.Tests`, filtru Prism | FAIL: 836 pass, 1 fail, 1 skip |
| `Cerneala.Tests`, proiect complet | FAIL: 3.155 pass, 1 fail, 2 skip |
| SDL shader compiler `--verify`, checkout inițial CRLF | FAIL: artefact `.msl` stale |
| SDL shader compiler `--verify`, după regenerare LF | PASS |
| `PrismAudit --check`, checkout inițial | FAIL: 6 gaps |
| `PrismAudit --check`, catalog normalizat LF | FAIL: 5 gaps |
| Căutare CI pentru `PrismAudit`/raport | zero referințe în `.github` |

## Ce nu a fost verificat în auditul inițial

- Testul nativ de paritate pixel WindowsDX–SDL pentru intrările resource-free a fost omis deoarece `CERNEALA_SDL_NATIVE_TESTS` nu este activat.
- Nu a fost efectuată validare manuală de UI.
- Nu a fost rulat un benchmark proaspăt; valorile din raportul existent nu sunt măsurători ale acestui audit.
- Nu a fost demonstrată paritatea vizuală pentru operațiile care cer resurse externe.
- Nu a fost obținut un full-solution GREEN: verificarea inițială a fost blocată de PRISM-01, iar proiectul core complet rămâne roșu din PRISM-03.

## Verificare după remediere

| Verificare | Rezultat |
| --- | --- |
| `dotnet restore .\Cerneala.slnx` | PASS |
| `dotnet build .\Cerneala.slnx --no-restore` | PASS: 0 warnings, 0 errors după restore-ul fixture-ului dinamic VisualStudioIntegrationHost |
| `Cerneala.Tests`, proiect complet | PASS: 3.156; SKIP: 2; FAIL: 0 |
| `Cerneala.Tests.SourceGen`, filtru Prism | PASS: 50/50 |
| `Cerneala.Tests.Language`, filtru Prism | PASS: 40/40 |
| `Cerneala.Tests.SdlGpu`, filtru Prism | PASS: 33; SKIP: 2 |
| testul focalizat de incrementality | PASS: 1/1 |
| SDL shader compiler `--verify` | PASS: 5 artefacte |
| comanda CI Release `PrismAudit --check --no-build --no-restore` | PASS: 178 intrări, 217 tipuri Prism, 10 tipuri extinse, zero gaps |
| experiment LF/CRLF pentru verificările text | PASS pentru ambele reprezentări |
| `dotnet format` pe cele trei fișiere C# modificate | PASS; warning de workspace load, zero abateri de format |
| conformance nativ WindowsDX–SDL GPU | 131 PASS; 1 FAIL (`SpinBlur`: MAE 0,0431, P99 1, max 62 > 49) |

Rularea întregii soluții de teste în configurația Debug a mai expus două eșecuri
în afara suprafeței modificate: un test de formatter care așteaptă LF dar primește
CRLF pe Windows și un test LanguageServer care caută executabilul Release după un
build Debug. Niciun fișier din acele subsisteme nu a fost modificat. Aceste
rezultate nu sunt raportate ca GREEN și nu sunt atribuite Prism fără dovadă.

Nu a fost efectuată validare manuală UI și nu a fost rerulat benchmarkul Prism.

## Ordinea de remediere aplicată

1. **PRISM-01** — contract EOL/hashing canonicalizat și verificat ostil.
2. **PRISM-02** — contract API stabilit din istoric, audit multi-assembly reparat,
   raport regenerat și poartă CI adăugată.
3. **PRISM-03** — test incremental mutat pe arborele HLSL partajat și readus pe GREEN.
4. **PRISM-04** — design tehnic sincronizat cu backendul SDL GPU livrat.
5. Buildul soluției, proiectul core, suitele Prism, auditul și conformance-ul nativ
   au fost rulate; excepțiile sunt raportate exact mai sus.

## Concluzie

Cele patru probleme ale auditului sunt închise: artefactele text au contract EOL
reproductibil, raportul canonic este din nou executabil și impus în CI, testul de
incrementality verifică structura reală, iar documentația descrie arhitectura
multi-backend livrată. `PASS` din raportul generat este acum reproductibil pentru
contractul pe care îl declară.

Nu rezultă însă că întregul sistem este perfect: abaterea nativă `SpinBlur` și
cele două eșecuri full-solution descrise mai sus rămân dovezi distincte, nu sunt
ascunse sub verdictul remedierilor și necesită scope separat dacă trebuie reparate.
