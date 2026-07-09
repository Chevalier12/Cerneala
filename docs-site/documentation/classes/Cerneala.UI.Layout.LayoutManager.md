# LayoutManager Class

## Definition
Namespace: `Cerneala.UI.Layout`

Assembly/Project: `Cerneala`

Source: `UI/Layout/LayoutManager.cs`

Coordoneaza masurarea si aranjarea elementelor UI pentru un `UIRoot`, cu cache pentru rezultate si invalidare de randare cand limitele aranjate se schimba.

```csharp
public sealed class LayoutManager
```

Inheritance:
`Object` -> `LayoutManager`

## Examples
Masurarea si aranjarea radacinii UI pentru un viewport fix:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

UIRoot root = new(viewportWidth: 800, viewportHeight: 600);
LayoutManager layout = root.LayoutManager;

LayoutResult measure = layout.Measure(root, new LayoutSize(800, 600));
LayoutResult arrange = layout.Arrange(root, new LayoutRect(0, 0, 800, 600));

bool boundsChanged = arrange.BoundsChanged;
```

Crearea procesoarelor folosite de frame scheduler pentru fazele de layout:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

UIRoot root = new(800, 600);
FramePhaseProcessors processors = root.LayoutManager.CreatePhaseProcessors();

processors.Measure?.Invoke(root);
processors.Arrange?.Invoke(root);
```

## Remarks
`LayoutManager` este legat de un singur `UIRoot`. Constructorul primeste radacina, iar metodele publice opereaza pe elemente `UIElement` din acel arbore.

`CreatePhaseProcessors` construieste un `FramePhaseProcessors` care seteaza doar fazele `Measure` si `Arrange`. Actiunile generate aleg automat dimensiunea disponibila si dreptunghiul final pe baza radacinii, parintelui vizual, pozitiei in `Canvas` si ultimului slot de aranjare cunoscut.

`Measure` evita masurarea cand elementul nu are flag-ul `InvalidationFlags.Measure`, dimensiunea disponibila este aceeasi cu ultima masurare, iar `LayoutVersion` nu s-a schimbat. Cand masurarea ruleaza, metoda apeleaza `UIElement.Measure(new MeasureContext(availableSize))`, actualizeaza cache-ul intern al elementului si returneaza un `LayoutResult`.

`Arrange` evita aranjarea in aceleasi conditii de cache pentru `InvalidationFlags.Arrange`, dreptunghiul final si `LayoutVersion`. Cand aranjarea ruleaza, metoda apeleaza `UIElement.Arrange(new ArrangeContext(finalRect))`, actualizeaza cache-ul intern si verifica daca `ArrangedBounds` s-a schimbat. Daca limitele s-au schimbat pentru un element atasat, elementul este invalidat pentru `Render` si `HitTest`.

Pentru descendenti atasati care au cache de randare valid, aceleasi dependinte de randare si aceeasi dimensiune de continut, dar alta pozitie, managerul elimina lucrarea de randare din `RenderQueue` si invalideaza radacina cache-ului retinut. Comportamentul asta permite reutilizarea continutului randat cand s-a schimbat doar translatia.

Regulile implicite de spatiu sunt:

| Caz | Spatiu folosit |
| --- | --- |
| Elementul este `UIRoot` | Viewport-ul radacinii. |
| Parintele vizual este `UIRoot` | Viewport-ul radacinii. |
| Parintele are `ArrangedBounds` cu latime si inaltime pozitive | Dimensiunea sau dreptunghiul parintelui. |
| Copil intr-un `Canvas` | Pozitia parintelui plus `Canvas.GetLeft(element)` si `Canvas.GetTop(element)`, cu dimensiunea `DesiredSize`. |
| Nu exista parinte utilizabil | Pentru masurare se foloseste viewport-ul; pentru aranjare se foloseste `DesiredSize` la originea `(0, 0)`. |

## Constructors
| Name | Description |
| --- | --- |
| `LayoutManager(UIRoot root)` | Creeaza un manager de layout pentru `root`. Arunca `ArgumentNullException` daca `root` este `null`. |

## Methods
| Name | Description |
| --- | --- |
| `FramePhaseProcessors CreatePhaseProcessors()` | Returneaza procesoare pentru fazele `Measure` si `Arrange`, legate la regulile interne de dimensiune disponibila si dreptunghi final. |
| `LayoutResult Measure(UIElement element, LayoutSize availableSize)` | Masoara `element` pentru `availableSize` sau returneaza rezultatul din cache cand datele de layout sunt inca valide. |
| `LayoutResult Arrange(UIElement element, LayoutRect finalRect)` | Aranjeaza `element` in `finalRect` sau returneaza rezultatul din cache cand datele de layout sunt inca valide. Invalideaza randarea si hit testing-ul cand limitele se schimba pentru un element atasat. |

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| `LayoutManager(UIRoot root)` | `ArgumentNullException` | `root` este `null`. |
| `Measure(UIElement element, LayoutSize availableSize)` | `ArgumentNullException` | `element` este `null`. |
| `Arrange(UIElement element, LayoutRect finalRect)` | `ArgumentNullException` | `element` este `null`. |

## Return Value Details
| Member | `LayoutResult` fields |
| --- | --- |
| `Measure` cache hit | `DesiredSize` si `ArrangedBounds` vin de pe element, `UsedMeasureCache` este `true`, `UsedArrangeCache` si `BoundsChanged` sunt `false`. |
| `Measure` executat | `DesiredSize` este rezultatul masurarii, `ArrangedBounds` este valoarea curenta a elementului, iar flag-urile de cache si schimbare sunt `false`. |
| `Arrange` cache hit | `DesiredSize` si `ArrangedBounds` vin de pe element, `UsedArrangeCache` este `true`, `UsedMeasureCache` si `BoundsChanged` sunt `false`. |
| `Arrange` executat | `DesiredSize` vine de pe element, `ArrangedBounds` este rezultatul aranjarii, iar `BoundsChanged` indica daca dreptunghiul anterior difera de cel nou. |

## Applies to
Project: `Cerneala`

Runtime context: sistemul UI al proiectului, in special procesarea frame-urilor prin `UIRoot.ProcessFrame` si `UiFrameScheduler`.

## See also
- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Layout.LayoutResult`
- `Cerneala.UI.Invalidation.FramePhaseProcessors`
