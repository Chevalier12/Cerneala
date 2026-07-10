# Plan: migrarea `Window` la Win32 + MonoGame WindowsDX

## Rezumat

Inlocuim backend-ul general Skia folosit acum de ferestrele native cu randare MonoGame WindowsDX. Cerneala ramane Windows-first in aceasta etapa, pastreaza ferestrele Win32 si foloseste Skia + HarfBuzz numai pentru pipeline-ul de text.

Aplicatia nu expune `Game1`, `Game.Run()` sau boilerplate de startup. Runtime-ul Cerneala detine un singur message pump Win32 si o colectie de sesiuni grafice, cate una pentru fiecare `Window`.

```text
Proces Cerneala
└── WindowApplicationRuntime
    ├── WindowContext A
    │   ├── HWND A
    │   ├── GraphicsDevice A
    │   ├── swap-chain A
    │   ├── MonoGameDrawingBackend A
    │   ├── UIRoot A
    │   └── Win32InputSource A
    ├── WindowContext B
    └── WindowContext C
```

## Decizii fixate

- V1 este Windows-only si foloseste `MonoGame.Framework.WindowsDX` `3.8.4.1`.
- Win32 ramane responsabil pentru `HWND`, mesaje, input, focus, DPI, ownership, dialoguri si lifecycle.
- MonoGame este singurul backend general de randare UI.
- Fiecare fereastra primeste propriul `GraphicsDevice`, propriul swap-chain si propriile resurse GPU.
- Runtime-ul foloseste un singur thread UI si un singur message pump pentru toate ferestrele.
- Nu cream instante `Game` si nu rulam mai multe `Game.Run()`. Folosim direct API-urile publice `GraphicsDevice`, `SpriteBatch` si `PresentationParameters`.
- `PresentationParameters.DeviceWindowHandle` primeste `HWND`-ul ferestrei Cerneala.
- Skia si HarfBuzz raman exclusiv in masurarea, shaping-ul si rasterizarea textului. Textura rezultata este incarcata si desenata prin MonoGame.
- Nu pornim procese secundare si nu folosim reflection pentru a accesa internals MonoGame.
- API-ul public `Window`, generatorul paired markup si startup-ul fara `Program.cs` raman neschimbate.

## Motivatie

Implementarea actuala creeaza corect ferestre native Win32, dar `Win32WindowPlatform` construieste un `SkiaDrawingBackend` care deseneaza toate comenzile UI intr-un bitmap BGRA si il prezinta prin Win32. Aceasta incalca limita arhitecturala a proiectului: Skia trebuie sa deserveasca textul, nu sa devina rendererul general.

WindowsDX permite evitarea unui fork MonoGame:

- `GraphicsDevice` are constructor public;
- `PresentationParameters.DeviceWindowHandle` este public;
- backend-ul WindowsDX creeaza swap-chain-ul pentru handle-ul furnizat;
- `GraphicsDevice.Present()` prezinta swap-chain-ul asociat.

DesktopGL nu este folosit in aceasta etapa deoarece implementarea sa leaga contextul OpenGL de singletonul intern `SdlGameWindow.Instance`, ceea ce impiedica asocierea curata a mai multor ferestre Cerneala prin API-ul public.

Referinte:

- <https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.GraphicsDevice.html>
- <https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.PresentationParameters.html>
- <https://github.com/MonoGame/MonoGame/blob/v3.8.4.1/MonoGame.Framework/Platform/Graphics/GraphicsDevice.DirectX.cs>
- <https://github.com/MonoGame/MonoGame/blob/v3.8.4.1/MonoGame.Framework/Platform/Graphics/GraphicsDevice.OpenGL.cs>

## Contracte pastrate

Nu schimbam comportamentul public deja implementat:

- `Show()`, `Hide()`, `Activate()` si `Close()`;
- `ShowDialogAsync()`, `Owner`, `OwnedWindows` si `DialogResult`;
- `SourceInitialized`, `Initialized`, `Loaded`, `ContentRendered`, `Closing` si `Closed`;
- interdictia de a redeschide o fereastra inchisa;
- inchiderea ferestrelor owned si politica de shutdown pentru `MainWindow`;
- thread affinity pentru toate operatiile Window;
- DPI logic pentru layout si pixeli fizici pentru backbuffer;
- `MainWindow.cui.xml` + `MainWindow.cui.xml.cs`;
- constructorul si entry point-ul generate;
- integrarea DI si `App.ConfigureServices`;
- `@when`, `@if`, resources, aspects, names si event handlers.

## Non-obiective

- Linux, macOS, DesktopGL sau Vulkan;
- mai multe procese;
- instante independente de `Game`;
- partajarea obiectelor `Texture2D` intre ferestre;
- un singur `GraphicsDevice` cu mai multe swap-chain-uri;
- custom chrome, transparenta sau ferestre borderless noi;
- schimbari ale gramaticii markup;
- redesign pentru `MonoGameUiHost` folosit in jocurile existente, in afara adaptarilor necesare pentru WindowsDX.

## Arhitectura tinta

### `WindowApplicationRuntime`

Runtime-ul ramane proprietarul tuturor ferestrelor si continua sa pompeze mesajele o singura data:

```csharp
while (windows.Count > 0)
{
    platform.PumpEvents();

    foreach (WindowContext context in contexts)
    {
        context.Update(elapsedTime);
        context.RenderIfNeeded();
    }
}
```

Nu introducem un al doilea scheduler. `WindowApplicationRuntime` continua sa decida cand exista lucru retained, motion activ sau un repaint cerut.

### `Win32WindowPlatform`

Platforma continua sa creeze si sa detina fiecare `HWND`. Dupa crearea handle-ului, cere unei fabrici grafice sa construiasca sesiunea WindowsDX.

```text
CreateWindow
├── CreateWindowEx
├── determine DPI and client size
├── create Win32InputSource
└── create WindowGraphicsSession(hwnd, pixelWidth, pixelHeight)
```

`WM_SIZE` si `WM_DPICHANGED` actualizeaza viewport-ul si cer redimensionarea backbuffer-ului. Dimensiunile zero din minimize nu reseteaza device-ul si nu declanseaza randare.

### `WindowGraphicsSession`

Introducem o abstractie interna testabila:

```csharp
internal interface IWindowGraphicsSession : IDisposable
{
    IDrawingBackend DrawingBackend { get; }
    IImageLoader ImageLoader { get; }
    ImageResourceCache ImageResourceCache { get; }

    void Resize(int pixelWidth, int pixelHeight, float coordinateScale);
    void BeginFrame(DrawColor clearColor);
    void Present();
}
```

Implementarea `WindowsDxWindowGraphicsSession` detine:

- `GraphicsDevice`;
- `SpriteBatch`;
- textura alba 1x1;
- `SkiaTextRasterizer`;
- `MonoGameDrawingBackend`;
- `MonoGameImageLoader` si cache-ul de imagini al ferestrei;
- `PresentationParameters` curente.

Crearea device-ului foloseste API public:

```csharp
PresentationParameters parameters = new()
{
    DeviceWindowHandle = hwnd,
    BackBufferWidth = pixelWidth,
    BackBufferHeight = pixelHeight,
    BackBufferFormat = SurfaceFormat.Color,
    DepthStencilFormat = DepthFormat.None,
    IsFullScreen = false,
    PresentationInterval = PresentInterval.One
};

GraphicsDevice device = new(
    GraphicsAdapter.DefaultAdapter,
    GraphicsProfile.HiDef,
    parameters);
```

Erorile de creare trebuie transformate intr-o exceptie Cerneala descriptiva care include adapterul, profilul, dimensiunea si handle-ul, fara a pierde exceptia originala.

### Resurse GPU

Resursele MonoGame sunt legate de `GraphicsDevice`, deci cache-urile GPU devin per-window:

- o imagine path-backed poate partaja bytes/decoded pixels la nivel CPU;
- fiecare sesiune incarca propria instanta `Texture2D`;
- textul shaped/rasterizat poate partaja rezultate CPU numai daca identitatea DPI/font este compatibila;
- textura de text ramane in cache-ul `MonoGameDrawingBackend` al sesiunii;
- un `MonoGameImage` creat pentru device-ul A nu poate fi desenat de device-ul B.

`IWindowPlatform.ImageLoader` si `ImageResourceCache` nu mai pot fi globale. Ele se muta pe `IPlatformWindow` sau in `IWindowGraphicsSession`, iar `WindowApplicationRuntime` le ataseaza la `UIRoot`-ul corespunzator.

### Text

Pipeline-ul tinta este:

```text
TextBlock
→ HarfBuzz shaping
→ Skia metrics/rasterization
→ RGBA pixels
→ Texture2D pe GraphicsDevice-ul ferestrei
→ MonoGameDrawingBackend
→ swap-chain WindowsDX
```

`SkiaDrawingBackend` nu participa la acest flux. `SkiaFont`, `SkiaTextShaper` si `SkiaTextRasterizer` raman valide.

### Input

`Win32InputSource` ramane per fereastra. Nu folosim `Keyboard`, `Mouse` sau `GamePad` statice din MonoGame pentru Window hosting.

Mesajele sunt rutate natural dupa `HWND`:

- pointer si wheel;
- keyboard si focus;
- text input;
- resize si move;
- activate/deactivate;
- close;
- DPI change.

Astfel doua ferestre nu isi pot consuma sau suprascrie reciproc input-ul.

## Modificari de proiect

1. Inlocuim `MonoGame.Framework.DesktopGL` cu `MonoGame.Framework.WindowsDX` `3.8.4.1`.
2. Mutam proiectele runtime pe `net8.0-windows`:
   - `Cerneala.csproj`;
   - Playground;
   - testele runtime care referentiaza Cerneala.
3. `Cerneala.SourceGen` ramane `netstandard2.0`.
4. Eliminam `SkiaSharp.NativeAssets.Linux` din configuratia Windows-first.
5. Verificam graful NuGet pentru native assets Skia Windows si adaugam explicit pachetul Win32 numai daca nu este deja tranzitiv.
6. Adaugam proprietatile Windows SDK strict necesare, fara a activa implicit WinForms sau WPF.
7. Pastram `AllowUnsafeBlocks` dezactivat daca WindowsDX si interop-ul existent nu il cer.

## Plan de implementare

### Etapa 0: probe tehnice RED/GREEN

Inainte de migrarea runtime-ului, adaugam smoke tests Windows intr-un proces separat:

1. cream un `HWND` de test;
2. cream un `GraphicsDevice` WindowsDX cu acel handle;
3. desenam o culoare si apelam `Present()`;
4. verificam prin readback/captura ca suprafata nu este goala;
5. cream doua `HWND` si doua `GraphicsDevice` in acelasi proces;
6. prezentam culori diferite in fiecare;
7. inchidem prima sesiune si demonstram ca a doua continua sa randeze;
8. redimensionam independent ambele backbuffer-uri;
9. impunem timeout si cleanup fortat pentru a nu bloca suita.

Aceasta proba este poarta de intrare. Nu eliminam backend-ul actual pana cand scenariul cu doua ferestre nu este demonstrat pe Windows.

### Etapa 1: limita grafica interna

- adaugam `IWindowGraphicsSession`;
- adaugam o fabrica injectabila pentru teste;
- adaptam fake platformele existente;
- pastram temporar o implementare Skia numai pentru a mentine testele verzi in timpul migrarii;
- mutam image loader/cache de la platforma globala la sesiunea ferestrei.

### Etapa 2: sesiunea WindowsDX

- implementam `WindowsDxWindowGraphicsSession`;
- cream `GraphicsDevice`, `SpriteBatch`, white pixel si `MonoGameDrawingBackend`;
- conectam `SkiaTextRasterizer`;
- implementam begin frame, clear, draw si present;
- implementam resize cu `GraphicsDevice.Reset` si aceleasi `PresentationParameters`/`HWND`;
- tratam minimize, device lost, resize failure si disposal partial.

### Etapa 3: integrarea Win32

- `Win32PlatformWindow` creeaza sesiunea dupa `CreateWindowEx`;
- `DrawingBackend` vine din sesiune;
- `Present()` deleaga la `GraphicsDevice.Present()`;
- eliminam bufferul BGRA, `WM_PAINT` blit-ul Skia si resize-ul bitmapului;
- `WM_PAINT` valideaza paint region si marcheaza contextul pentru repaint, fara randare reentranta in `WndProc`;
- `WM_SIZE` si `WM_DPICHANGED` reprogrameaza resize-ul pe runtime.

### Etapa 4: resurse si text

- conectam `MonoGameImageLoader` per fereastra;
- demonstram ca aceeasi imagine poate fi incarcata in doua device-uri fara a partaja `Texture2D`;
- pastram cache-ul text per backend;
- verificam shaping, line metrics, baseline, clipping si DPI;
- eliminam conversiile specifice `SkiaDrawImage` din Window hosting.

### Etapa 5: mai multe ferestre

- demonstram doua si apoi trei ferestre simultane in acelasi PID;
- verificam update/draw/present independent;
- ascunderea unei ferestre nu suspenda celelalte;
- inchiderea unei ferestre elibereaza numai device-ul si resursele sale;
- owner/dialog disable afecteaza input-ul, nu schedulerul sau device-ul owner-ului;
- inchiderea `MainWindow` continua sa aplice politica Cerneala existenta.

### Etapa 6: eliminarea backend-ului general Skia

Dupa ce WindowsDX este complet verde:

- eliminam `SkiaDrawingBackend` din `Win32WindowPlatform`;
- eliminam `SkiaDrawImage` si `SkiaImageLoader` daca Roslyn confirma ca nu mai au consumatori legitimi;
- eliminam testele backend-ului general Skia sau le inlocuim cu teste pentru componentele text Skia;
- adaugam un architecture test care interzice dependenta `UI/Hosting/Windows` de `Cerneala.Drawing.Skia.SkiaDrawingBackend`;
- adaugam un test care cere `MonoGameDrawingBackend` pentru fiecare sesiune Window reala.

### Etapa 7: Playground si generator

- generatorul si API-ul markup nu se schimba;
- Playground ramane fara `Program.cs` si fara `Game1.cs`;
- startup-ul generat porneste `WindowApplicationRuntime`;
- `MainWindow` este randat prin WindowsDX;
- adaugam o fereastra secundara minima in testele de integrare, nu neaparat in showcase-ul initial;
- verificam ca toate ferestrele au PID-ul procesului Playground.

## Testare

### Unit si contract

- fabrica grafica primeste exact `HWND`, pixel size si DPI scale;
- fiecare `WindowContext` primeste alta sesiune;
- resize-ul actualizeaza viewport-ul si backbuffer-ul o singura data;
- minimize nu creeaza backbuffer `0x0`;
- `Hide()` pastreaza device-ul;
- `Close()` dispune backend, cache-uri, SpriteBatch si GraphicsDevice exact o data;
- erorile partiale de initializare nu lasa `HWND`, device sau resurse native active;
- thread affinity ramane impusa.

### Rendering Windows

- toate `DrawCommandKind` sunt randate de `MonoGameDrawingBackend`;
- clipping si scissor sunt restaurate intre frame-uri;
- textul Skia/HarfBuzz ajunge intr-o textura MonoGame si este vizibil;
- imaginea path-backed este vizibila;
- DPI 100%, 125%, 150% si 200%;
- resize repetat nu pierde continut si nu scurge resurse;
- pixel checks distincte pentru doua ferestre simultane.

### Multi-window

- doua si trei `HWND` au acelasi PID;
- fiecare are `GraphicsDevice` distinct;
- focusul si input-ul sunt rutate numai ferestrei tinta;
- inchiderea A nu afecteaza randarea B;
- owner handle si modal disable sunt corecte;
- dialogurile imbricate isi completeaza task-urile cu rezultatul corect;
- shutdown-ul principal elibereaza toate device-urile si handle-urile.

### Compatibilitate

- toate testele SourceGen raman verzi;
- `UserControl`, markup standalone, resources, aspects si `@when/@if` raman neschimbate;
- `MonoGameUiHost` continua sa functioneze intr-un joc WindowsDX existent;
- niciun proiect runtime nu mai referentiaza DesktopGL;
- Skia general rendering nu mai este accesibil din Window hosting.

## Verificare finala

```powershell
dotnet restore Cerneala.slnx
dotnet test Cerneala.slnx --no-restore
dotnet build Cerneala.slnx --no-restore
dotnet format Cerneala.slnx --no-restore --verify-no-changes
```

In plus:

- rulam smoke test-ul Win32/WindowsDX intr-un proces separat cu timeout;
- lansam Playground si verificam `MainWindow` plus cel putin o fereastra secundara;
- verificam PID, `HWND`, resize, input, randare si exit code `0`;
- rulam `git diff --check`;
- regeneram `FileTree.md`;
- reindexam `Cerneala.slnx` cu RoslynIndexer dupa fiecare modificare de cod sau proiect.

## Riscuri si masuri

### Cost GPU per fereastra

Fiecare `GraphicsDevice` dubleaza resursele GPU. V1 accepta costul pentru izolare si simplitate. Adaugam contoare diagnostice per sesiune si limite descriptive pentru esecuri de alocare.

### Device lost si resize

WindowsDX poate pierde sau reseta device-ul. Toate resursele create de sesiune trebuie reconstruibile, iar erorile trebuie izolate la fereastra afectata cand este posibil.

### VSync cu mai multe ferestre

Mai multe apeluri secventiale `PresentInterval.One` pot bloca acelasi thread. Proba tehnica masoara costul. Daca este necesar, runtime-ul foloseste VSync numai pentru fereastra activa sau trece sesiunile secundare pe `PresentInterval.Immediate`, fara a schimba API-ul public.

### Dependenta WindowsDX

WindowsDX face runtime-ul Windows-only. Aceasta este o decizie explicita pentru V1, nu un accident ascuns. Contractele interne raman separate astfel incat un backend viitor sa poata implementa aceeasi `IWindowGraphicsSession`.

## Criterii de acceptare

Migrarea este completa numai cand:

- nicio fereastra Cerneala nu foloseste `SkiaDrawingBackend` pentru randare generala;
- `MainWindow` si ferestrele secundare sunt randate prin `MonoGameDrawingBackend`;
- fiecare fereastra are propriul `GraphicsDevice` si propriul swap-chain;
- doua ferestre pot randa, primi input, fi redimensionate si inchise independent in acelasi proces;
- Skia/HarfBuzz sunt folosite numai pentru text;
- Playground nu contine `Game1.cs`, `Program.cs` sau boilerplate de startup;
- toate testele, build-ul, formatterul si smoke test-ul nativ sunt verzi;
- procesul se inchide cu exit code `0` si fara handle-uri/device-uri ramase active.
