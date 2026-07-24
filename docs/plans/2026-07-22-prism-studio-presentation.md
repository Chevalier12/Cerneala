# Plan: Prism Studio in CernealaPresentation

> Data: 2026-07-22
> Status: finalizat
> Scop: Adauga dupa Motion un capitol Prism cu editor Photoshop-like complet, alimentat de catalogul public Prism si verificat end-to-end.

## Decizii si non-obiective

- Galeria are patru tinte locale: mascota, poster tipografic, badge geometric si card UI.
- Acelasi stack se muta intre tinte si porneste dintr-un preset determinist de doua layere.
- Layerele contin liste separate de filtre si styles; nu se simuleaza intercalarea lor.
- Catalogul afiseaza toate cele 134 de filtre si 10 styles; operatiile cu resurse obligatorii raman vizibile, dar blocate.
- Stack-ul nu are limita artificiala; diagnostics arata costul si fallback-urile.
- Nu se implementeaza persistenta, importul resurselor sau schimbarea semanticii Prism.

## Etape de implementare

### Etapa 0 - Catalogul public si accesul tipizat

- [x] Extinde catalogul si generatorul cu metadata publica immutable pentru operatie, parametru, tip, default, unitate, domeniu numeric, optiuni symbol si dependenta de resurse.
- [x] Expune `PrismCatalog`, `PrismCatalogOperationInfo`, `PrismCatalogParameterInfo`, `PrismCatalogOperationKind` si `PrismCatalogValueKind` fara a duplica JSON-ul in Presentation.
- [x] Adauga `GetValue<T>` si `SetValue<T>` pe `PrismFilterState` si `PrismStyleState`, cu validare pentru operatie, descriptor, tip si symbol.
- [x] Pastreaza compatibile helper-ele generate si comportamentul runtime existent.
- [x] Adauga teste SourceGen si runtime pentru 134 filtre, 10 styles, toate tipurile, metadata, round-trip, versionare si state expirat dupa `ReplaceDefinition`.
- [x] Documenteaza toate API-urile publice noi in `docs-site/documentation/classes/` si sincronizeaza manifestul.

**Gate etapa 0**

- [x] Build-ul si testele Prism/SourceGen tintite sunt verzi, iar API-ul public nu cere slot-uri sau string-uri magice consumatorului.

### Etapa 1 - Modelul editorului Prism Studio

- [x] Adauga modelul Presentation pentru layere, filtre, styles, selectie, valori tipizate si presetul initial.
- [x] Construieste definitia Prism in ordinea declarata, reaplica valorile dupa schimbari structurale si conserva stack-ul la schimbarea tintei.
- [x] Implementeaza add/remove/reorder/visibility/reset fara limita de stack si blocheaza operatiile cu resurse obligatorii.
- [x] Adauga teste pentru ordinea filtre/styles, reset, selectie, stack nelimitat, operatii blocate si conservarea valorilor.

**Gate etapa 1**

- [x] Modelul produce compozitii valide si toate testele lui sunt verzi fara dependenta de UI sau GPU.

### Etapa 2 - View-ul interactiv si preview-urile

- [x] Adauga `PrismChapterView` cu preview, galerie de patru tinte, panou de layere si inspector/catalog responsive la 1320x860 si 1080x720.
- [x] Randaza integral local cele patru mostre, inclusiv asset-ul mascotei, pentru captura Prism corecta.
- [x] Conecteaza actiunile de layer/filter/style, search, categorii, tab-uri si reset la model si `PrismInstance.ReplaceDefinition`.
- [x] Genereaza editori pentru number, integer, boolean, color, vector, symbol si starea resource read-only.
- [x] Afiseaza toate operatiile, marcajul `RESOURCE REQUIRED` si diagnostics live pentru pass-uri, surfaces, bytes si fallback.
- [x] Gestioneaza explicit attachment-ul, detach-ul, schimbarea tintei si resursele preview-urilor.

**Gate etapa 2**

- [x] Interactiunile modifica imediat preview-ul, layout-ul nu se suprapune, iar view-ul nu lasa attachment-uri sau resurse dupa detach.

### Etapa 3 - Integrarea in tur si automatizare

- [x] Insereaza `PRISM` dupa `MOTION`, renumeroteaza Pipeline/Diagnostics si actualizeaza paginile, toggle-urile, handler-ele si counter-ul la 8 capitole.
- [x] Inlocuieste indecsii magici din frame callbacks si automatizare cu identificare semantica stabila.
- [x] Extinde automatizarea si smoke frame-budget pentru noul capitol si diagnostics Prism.
- [x] Adauga regresie de lifecycle pentru navigari repetate spre/dinspre Prism.

**Gate etapa 3**

- [x] Turul complet, captura directa a capitolului 06 si automatizarea repetata ruleaza fara leak, timeout sau capitol gresit.

### Etapa 4 - Verificarea finala

- [x] Ruleaza reindexarea si doctorul RoslynIndexer.
- [x] Ruleaza testele SourceGen, testele Cerneala si `dotnet test .\Cerneala.slnx` in starea finala.
- [x] Ruleaza automatizarea Presentation si un frame-budget smoke cycle.
- [x] Captureaza si inspecteaza vizual Prism la dimensiunea implicita si minima, inclusiv preview neblank, text, clipping si diagnostics.
- [x] Ruleaza verificarea documentatiei/API, `git diff --check` si auditul final al diff-ului.

**Gate etapa 4**

- [x] Toate verificarile sunt verzi si nu exista cod temporar, churn generat accidental sau regresii vizuale cunoscute.

## Definitia de gata

- [x] Capitolul Prism Studio este complet functional dupa Motion, cu toate filtrele/styles descoperite din catalog, editor tipizat, patru tinte si diagnostics live.
- [x] API-ul public, documentatia, lifecycle-ul, automatizarea, performanta smoke si suita completa sunt verificate.
- [x] Toate etapele si gate-urile sunt bifate, iar statusul planului este `finalizat`.
