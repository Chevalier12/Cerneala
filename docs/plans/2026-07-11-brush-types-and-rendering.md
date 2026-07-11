# Plan: completarea modelului Brush si a randarii

## Rezumat

Completam sistemul `Brush` astfel incat Cerneala sa poata reprezenta si randa culori solide, gradienti, imagini, desene si vizuale. Implementarea trebuie sa acopere atat modelul de date, cat si traducerea catre backend-ul WindowsDX/MonoGame; nu este suficient sa adaugam clase care raman simple containere.

La final, fiecare tip de brush acceptat de API trebuie sa aiba:

- validare proprie si egalitate determinista;
- suport in markup si source generator unde are sens;
- cache/resource lifetime corect pentru fiecare `GraphicsDevice`;
- o cale de randare testabila prin `IDrawingBackend`;
- teste de regresie pentru continut, clipping, opacity si transformari.

## Starea actuala

Exista deja:

- `Brush` cu `SolidColor` optional;
- `SolidColorBrush`;
- `LinearGradientBrush`;
- `RadialGradientBrush`;
- `Pen` care retine un `Brush`.

Lipsesc sau sunt incomplete:

- `ImageBrush`;
- `DrawingBrush`;
- `VisualBrush`;
- o abstractie comuna de tip `TileBrush` pentru stretch, alignment, viewport si tile mode;
- randarea gradientilor si a brush-urilor non-solide;
- cache-uri GPU per fereastra pentru resursele folosite de brush-uri;
- suport de markup pentru brush-uri compuse.

Rendererul actual deseneaza primitivele cu `Color`; gradient brush-urile existente nu ajung in backend.

## Decizii de arhitectura

- `Brush` ramane API-ul semantic public; backend-ul primeste o reprezentare interna pregatita pentru device.
- `SolidColorBrush` este calea rapida si nu creeaza resurse GPU suplimentare.
- Gradientii sunt randati prin resurse GPU per `GraphicsDevice`, nu prin sampling CPU la fiecare pixel.
- `ImageBrush` reutilizeaza loader-ul si cache-ul de imagini al ferestrei; niciun `Texture2D` nu este partajat intre ferestre.
- `DrawingBrush` foloseste o lista de comenzi rasterizabila, nu un al doilea renderer general.
- `VisualBrush` foloseste un render target offscreen si are protectie explicita impotriva ciclurilor vizuale.
- Nu introducem dependente de WPF sau de internals MonoGame.
- API-ul pastreaza coordonate Cerneala si aplica transformarea DPI o singura data, in acelasi loc ca restul randarii.

## Faza 1: contract si model comun

1. Definim contractul `Brush` pentru:
   - identificarea tipului;
   - opacity;
   - validarea valorilor;
   - conversia catre o descriere interna de sampling.
2. Introducem `TileBrush` daca proprietatile sunt comune:
   - `Stretch`;
   - `AlignmentX` si `AlignmentY`;
   - `Viewport` si `Viewbox`;
   - `TileMode`;
   - `Opacity`.
3. Stabilim enum-urile si valorile implicite fara a copia automat toate proprietatile WPF care nu pot fi randate in Cerneala.
4. Pastram `SolidColor` doar ca shortcut pentru `SolidColorBrush`; brush-urile compuse returneaza `null`.
5. Adaugam teste pentru validarea stop-urilor, radii, coordonate si egalitate structurala.

## Faza 2: gradienti

1. Definim formatul intern pentru gradient:
   - lista sortata de stop-uri;
   - premultiplicarea alpha;
   - interpolare in spatiul decis de renderer;
   - clamp pentru offset-uri si extensie pentru capete.
2. Implementam in `MonoGameDrawingBackend` o reprezentare GPU comuna pentru linear si radial:
   - textura de stop-uri sau buffer echivalent;
   - quad/mesh cu parametrii gradientului;
   - blending alpha compatibil cu textul si primitivele existente.
3. Aplicam corect `Clip`, `Opacity`, `RenderTransform` si `CoordinateScale`.
4. Tratam gradientii degenerati predictibil: un singur stop, lungime zero, radii invalide sau stop-uri duplicate.
5. Adaugam imagini de referinta si pixel diffs pentru linear/radial la scale 1.0, 1.25 si 1.5.

## Faza 3: ImageBrush

1. Introducem `ImageBrush` cu:
   - sursa (`IDrawImage` sau URI/path rezolvat prin loader);
   - `Stretch`, alignment, viewport si tile mode;
   - opacity;
   - comportament explicit pentru imagine lipsa sau invalida.
2. Separaram datele CPU de textura GPU:
   - decoded image/cache CPU partajabil;
   - `Texture2D` creat per sesiune/fereastra;
   - invalidare cand sursa se schimba.
3. Implementam sampling si tiling in backend, inclusiv clipping si DPI.
4. Adaugam teste pentru aspect ratio, crop, repeat, mirror si device isolation.

## Faza 4: DrawingBrush

1. Stabilim forma publica a continutului: lista de `DrawCommand` sau un obiect de desen imutabil.
2. Definim limite clare:
   - fara acces la `UIElement` din `DrawingBrush`;
   - fara efecte care cer cicluri de layout;
   - continutul trebuie sa fie sigur de re-randat.
3. Rasterizam continutul intr-un render target/textura cache-uita per device.
4. Aplicam tile, transform, opacity si clip peste continutul rasterizat.
5. Testam ca schimbarea unei comenzi invalideaza doar brush-ul afectat.

## Faza 5: VisualBrush

1. Definim daca sursa este un `UIElement` existent sau un template separat; prima versiune foloseste un element existent.
2. Introducem un render pass offscreen explicit, separat de frame-ul principal.
3. Detectam cicluri de tip `VisualBrush -> element -> brush` si esuam controlat, fara recursie infinita.
4. Stabilim politica pentru elemente detached, resurse, input si focus: `VisualBrush` este doar vizual.
5. Cache-uim render target-ul pe device si invalidam la schimbari de layout, proprietati sau continut.
6. Adaugam teste pentru sursa proprie, sursa parinte, cicluri si schimbari de dimensiune.

## Faza 6: markup si resurse

1. Extindem schema runtime si source generator-ul pentru:
   - `<SolidColorBrush ... />`;
   - `<LinearGradientBrush ...>`;
   - `<RadialGradientBrush ...>`;
   - `<ImageBrush ...>`;
   - `<DrawingBrush ...>`;
   - `<VisualBrush ...>`.
2. Permitem proprietati simple pentru brush (`Color`, `Opacity`, stop-uri, sursa) si proprietate-element unde continutul este compus.
3. Facem referintele de resurse tip-safe: `$Accent` trebuie sa produca `Brush`, nu sa fie fortat in `Color`.
4. Pastreaza diagnosticele pentru tipuri incompatibile, stop-uri lipsa, surse necunoscute si combinatii imposibile.
5. Documentam ce sintaxa este compilata si ce sintaxa ramane runtime-only.

## Faza 7: integrare cu backend-urile si lifetime

1. Extindem `IDrawingBackend` cu operatii interne pentru brush, fara sa rupem API-ul public al `DrawingContext`.
2. Mutam toate resursele GPU brush in ownership-ul sesiunii grafice a ferestrei.
3. Eliberam render target-uri, texturi si cache-uri la `Dispose`, resize si device reset.
4. Verificam ca doua ferestre pot folosi aceeasi descriere Brush fara sa partajeze obiecte GPU.
5. Pastram fallback-ul solid pentru backend-uri care nu suporta inca brush-uri compuse si raportam diagnostic clar.

## Testare si acceptanta

- teste unitare pentru fiecare model Brush si proprietatile comune;
- teste source generator si runtime markup pentru toate tipurile;
- teste de render pentru culoare, alpha, clipping, transform si DPI;
- pixel diffs pentru linear, radial si image brush;
- teste de lifetime per `GraphicsDevice` si device reset;
- teste de cicluri si invalidare pentru `VisualBrush`;
- build fara warnings si suitele runtime/sourcegen complet verzi;
- documentatie API actualizata si niciun tip Brush declarat dar nerandabil fara un diagnostic explicit.

## Non-obiective

- compatibilitate binara cu o implementare WPF;
- efecte WPF care cer compozitor separat;
- shader-e arbitrare expuse utilizatorului in prima versiune;
- partajarea resurselor GPU intre ferestre;
- schimbarea proprietatilor `Background` si `Foreground` in acest plan.
