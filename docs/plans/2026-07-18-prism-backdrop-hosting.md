# Prism — backdrop și integrarea hostului

## Scop

Permite Prism să proceseze lumea jocului și UI-ul aflat sub control, fără
readback CPU și fără a confunda backdrop-ul cu imaginea controlului.

**Dependențe:** `2026-07-18-prism-retained-composition-graph.md` și
`2026-07-18-prism-monogame-compositor.md`.

## Etapa 0 — contracte RED

- [x] Adaugă teste RED backend-neutral pentru zero/unu backdrop, obligația de a
  fi ultimul copil direct și excluderea conținutului propriu/suprapus.
- [x] Adaugă teste RED de host pentru achiziție zero când nu e cerut, exact una
  per frame când mai multe compoziții îl folosesc și release în `finally`.
- [x] Fixează prin teste ordinea: lumea jocului + UI inferior → backdrop plane
  → conținutul controlului → UI superior.
- [x] Adaugă cazuri RED pentru host fără provider, resize, source replacement,
  nested Prism, Hidden/Collapsed și excepție în executor.

### Gate etapa 0

- [x] Testele disting clar „capture control” de „backdrop frame” și nu presupun
  o implementare MonoGame în contractele backend-neutral.

## Etapa 1 — contractele hostului

- [x] Adaugă un contract public minim `IBackdropFrameSource` și un lease
  frame-scoped readonly, fără API generic de texture ownership.
- [x] Definește metadata necesară: dimensiune, pixel scale, color profile,
  alpha/format, coordinate transform, `ContentVersion` monoton și lifetime până
  la finalul frame-ului.
- [x] Adaugă providerul opțional în `IUiBackend`/`MonoGameUiHostOptions` ori în
  cel mai mic contract de host deja responsabil pentru frame acquisition;
  evită service locator și singleton global.
- [x] Validează compatibilitatea provider/backend la pornirea hostului și oferă
  diagnostic clar când backdrop-ul nu poate fi furnizat.
- [x] Documentează explicit că aplicația păstrează ownership-ul scenei, iar
  Cerneala împrumută numai frame-ul deja randat.

### Gate etapa 1

- [x] Hostul fără Prism/backdrop nu trebuie să implementeze nimic nou
  obligatoriu, iar lease-ul nu poate supraviețui frame-ului.

## Etapa 2 — achiziție coordonată de analiză

- [x] Folosește exclusiv `PrismFrameAnalysis` pentru a decide dacă frame-ul cere
  backdrop; nu rescana `DrawCommandList`.
- [x] Achiziționează cel mult un lease per frame și pune-l în
  `DrawingFrameContext` pentru toți consumatorii compatibili.
- [x] Sari achiziția când toate backdrop-urile sunt invizibile, clipped-out sau
  eliminate de optimizer.
- [x] Eliberează lease-ul în `finally` după submit, inclusiv la excepții și
  device reset.
- [x] Adaugă counters pentru requested/acquired/shared/skipped/failed și teste
  pentru fiecare cale.

### Gate etapa 2

- [x] Un frame fără nevoie face zero apeluri, iar un frame cu oricâte
  backdrop-uri compatibile face o singură achiziție.

## Etapa 3 — semantica grafului

- [x] Extinde graful cu un input backdrop separat și cu decuparea lui în
  coordonatele controlului, fără a-l transforma în `Source` de layer.
- [x] Procesează filtrele/styles/mask/proprietățile declarate în `@backdrop`
  conform catalogului și compune rezultatul înaintea layerelor controlului.
- [x] Respectă ordinea UI: backdrop-ul vede numai ce hostul a finalizat sub
  control, nu frații desenați ulterior sau conținutul controlului însuși.
- [x] Aplică profilul de culoare și alpha metadata o singură dată; respinge ori
  degradează observabil formatele incompatibile prin politica centrală.
- [x] Adaugă snapshots de graf pentru un backdrop, mai multe controale,
  nested groups și layer invizibil.

### Gate etapa 3

- [x] Graful nu conține cicluri și arată explicit ce frame backdrop este
  împărțit și unde este decupat/convertit.

## Etapa 4 — adaptorul MonoGame

- [x] Implementează adaptorul WindowsDX care oferă textura scenei deja randate
  sau un resolve GPU explicit; interzice `GetData` și copierea prin CPU.
- [x] Integrează ordinea în `WindowsDxWindowGraphicsSession` și calea normală
  `MonoGameUiHost.Draw`, păstrând același contract pentru `RenderPng`.
- [x] Refolosește lease-ul frame-local între compoziții și alocă doar
  suprafețele intermediare cerute de graf.
- [x] Gestionează resize, MSAA resolve, format/color mismatch, device reset și
  provider replacement fără resurse orfane.
- [x] Propagă `ContentVersion`, versiunile UI inferioare și metadata de raster în
  dependency stamp-ul consumat de planul cache-ului retained; hostul nu reține
  textura sursă după frame.

### Gate etapa 4

- [x] Backdrop-ul rulează complet GPU-side, o singură dată per frame, iar
  suprafețele și lease-urile sunt eliberate la toate ieșirile.

## Etapa 5 — conformance, lifecycle și API docs

- [x] Adaugă scene automate cu gameplay/background recognoscibil, UI inferior,
  control cu blur/color backdrop și UI superior neafectat.
- [x] Capturează prin `IWindowPlatform.RenderPng` și verifică golden-uri pentru
  coordonate, blur edges, alpha, resize și două controale ce împart frame-ul.
  (Contractul real din repo este `IWindowScreenshotSource.RenderPng`, folosit de
  runtime peste sesiunea creată de `IWindowPlatform`.)
- [x] Rulează stress de navigare, hide/unhide, provider replacement și device
  reset cu contoare de resurse și `WeakReference`.
- [x] Documentează contractele publice cu skill-ul
  `writing-api-documentation`, actualizează `IUiBackend`,
  `MonoGameUiHostOptions`, tipurile backdrop și manifestul.
- [x] Rulează reindexarea după fiecare lot C#/proiect.
- [x] Rulează
  `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "PrismBackdrop|MonoGameUiHost|RenderPng"`,
  `dotnet test .\Cerneala.slnx` și `git diff --check`.

## Definiția de gata

- [x] Prism poate procesa lumea jocului/UI-ul inferior printr-un lease
  frame-scoped, fără readback, și expune toate versiunile necesare reutilizării
  corecte a rezultatului procesat între frame-uri.
- [x] Ordinea, sharing-ul, failure paths, lifecycle-ul și documentația sunt
  verificate automat.
