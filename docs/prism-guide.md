# Ghid Prism

Prism procesează doar prezentarea unui element Cerneala. Nu schimbă măsurarea,
aranjarea, hitbox-ul, focusul sau rutarea inputului.

## Modelul Photoshop și sursa implicită

Prism capturează o singură dată numai vizualul local al controlului: comenzile
produse de propriul `OnRender`, fără comenzile descendenților vizuali. Acea captură
este sursa implicită a stivei normale; copiii sunt desenați normal după rezultatul
Prism, iar numele layerelor și group-urilor sunt adrese pentru Motion și
diagnostics, nu surse de imagini.

Ordinea declarată este ca în panoul Photoshop: primul nod este în față, ultimul
este în spate, iar evaluarea se face de jos în sus. Un `@backdrop` este o suprafață
separată sub control, nu intră în stiva normală.

## Layer, group, mask și clipping

- `@layer` este leaf și conține filtre și/sau styles.
- `@group` conține layere sau grupuri imbricate și poate procesa rezultatul lor.
- `@mask` se aplică rezultatului pregătit după filtre și styles, înainte de
  opacity și blend.
- `ClipToBelow = true` folosește alpha-ul celui mai apropiat sibling normal
  neclipped de dedesubt, în același scope.
- `Visible = false` elimină întregul scope și munca lui; `Opacity = 0` păstrează
  evaluarea, apoi face contribuția transparentă.

Exemplul real folosit de capitolul Solar System:

```xml
<PrismComposition Name="PlanetCardPrism">
    @layer SignalPulse
    {
        Opacity = 0.18;
        BlendMode = Screen;

        @style OuterGlow
        {
            Size = 7;
            Opacity = 0.72;
            Color = #8060D8FF;
        }

        @mask
        {
            Image = $PlanetCardMask;
            Channel = Luminance;
            Feather = 1.5;
            Density = 0.42;
        }
    }

    @group CardTreatment
    {
        @layer CardClarity
        {
            @filter BrightnessContrast
            {
                Brightness = 0.02;
                Contrast = 0.08;
            }
        }
    }

    @backdrop SpaceGlass
    {
        Opacity = 0.76;
        @filter Blur { Radius = 8; }
    }
</PrismComposition>

<Border Name="PlanetInfoCard">
    @prism $PlanetCardPrism;
</Border>
```

Un `@backdrop` este opțional, unic și ultimul copil direct al compoziției.

## Motion paths

Motion intră în instanța Prism prin segmentul rezervat `.prism.`:

```text
$self.prism.SignalPulse.Opacity
$owner.prism.CardTreatment.CardClarity.Visible
$PlanetInfoCard.prism.SpaceGlass.Opacity
```

`$self`, `$owner` și numele din namescope urmează regulile Motion existente.
Segmentele intermediare traversează group-uri și noduri numite. Numerele și
culorile se interpolează continuu; bool, int și enum se schimbă discret la finalul
intervalului. Resursele nu sunt animabile. Generatorul validează calea și tipul,
iar runtime-ul nu face lookup textual per frame.

## Backdrop și backend-uri

Hostul analizează lista o singură dată și cere cel mult un lease readonly pentru
frame. Dacă providerul lipsește sau refuză achiziția, numai planul backdrop este
omis; stiva controlului și conținutul normal continuă. Lease-ul este eliberat în
același draw, inclusiv la excepții.

Un backend fără Prism ignoră `BeginPrism` și `EndPrism`, dar procesează toate
comenzile dintre ele. Nu trebuie să implementeze backdrop și nu apare nicio
schimbare de layout sau input.

## Diagnostics și bugete

`MonoGameDrawingBackend.RendererDiagnostics` oferă snapshot-uri immutable pentru
hit/miss/promotion/eviction, bytes, intrări, peak, capturi și passes economisite.
Diagnostics operaționale detaliate sunt interne, deterministe, redactează ID-uri
GPU instabile și se activează prin `PrismRendererOptions` numai la nevoie.

Defaulturile măsurate sunt 512 MiB hard pentru toate suprafețele Prism, 256 MiB
soft pentru cache-ul retained și 256 de intrări. La presiune se evacuează LRU
nepin-uit. Dacă hard limit-ul tot nu poate admite o suprafață transient, backend-ul
raportează `PRISM7006`/`SurfaceAllocationFailed`, restaurează hostul și continuă
comenzile interioare brute rămase, fără output parțial sau quality downgrade
ascuns.

Erorile de markup folosesc diagnostics `PRISM1xxx`-`PRISM6xxx`; failure paths de
runtime folosesc diagnostics precise, deduplicate unde contractul o cere.

## Catalog

Catalogul machine-readable rămâne singura sursă pentru operații, proprietăți,
tipuri, defaulturi și capabilități. Listele complete nu se copiază aici:

- [referința generată a filtrelor](prism-filter-reference.generated.md)
- [filtre de ajustare](prism-adjustment-filters.md)
- [filtre de distorsiune](prism-distortion-filters.md)
- [filtre de vecinătate](prism-neighborhood-filters.md)
- [catalogul de filtre](prism-catalog-filters.md)

Prima implementare nu include SDK public pentru operații third-party, shader
source la runtime, adaptive quality sau async compute.
