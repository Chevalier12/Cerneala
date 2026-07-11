# Plan: migrarea `Foreground` la tipul `Brush`

## Rezumat

Schimbam `Control.Foreground` din `Color` in `Brush?` fara sa expunem un contract fals in care doar `SolidColorBrush` functioneaza. Spre deosebire de background si border, foreground-ul ajunge in rasterizarea textului si necesita mascarea glyph-urilor cu brush-uri compuse.

## Contract final propus

```csharp
public static readonly UiProperty<Brush?> ForegroundProperty;
public Brush? Foreground { get; set; }
```

- Valoarea implicita este un `SolidColorBrush(Color.Black)` concret, deoarece textul mostenit trebuie sa ramana vizibil.
- Proprietatea ramane mostenita si afecteaza render.
- `Foreground="Tomato"` produce explicit `SolidColorBrush`.
- Nu pastram un alias `ForegroundColor` si nu adaugam conversie implicita din `Color`.

## Problema tehnica

Astazi `Foreground` este consumat ca `Color` de `TextBlock`, `Button`, `CheckBox`, `ContentPresenter`, `TextBoxBase`, `TextAspect`, `TextRenderer`, `DrawTextRun` si backend-ul Skia/MonoGame. Un gradient sau image brush trebuie aplicat peste acoperirea glyph-urilor, nu peste dreptunghiul complet al textului.

## Faza 1: modelul de comanda pentru text

1. Extindem comanda de text sa transporte `IDrawBrush`, pastrand overload-ul de culoare doar daca este API intern compatibil.
2. Separarea ramane clara: shaping-ul produce glyph-uri si metrici, brush-ul produce culoarea finala.
3. Cache key-ul textului separa masca glyph-urilor de brush, astfel incat schimbarea brush-ului sa nu rerasterizeze inutil forma textului.
4. Cache-urile si resursele raman izolate per `GraphicsDevice`.

## Faza 2: rasterizare si compozitie

1. Skia produce o masca alpha sau o textura de acoperire independenta de culoare.
2. `SolidColorBrush` coloreaza masca prin calea rapida existenta.
3. Linear si radial gradient sunt esantionate in coordonatele layout-ului textului.
4. Image/drawing/visual brush folosesc aceleasi reguli de stretch, viewport, viewbox si tile mode ca celelalte suprafete.
5. Opacitatea brush-ului si opacitatea elementului se compun o singura data.

## Faza 3: controale si mostenire

1. Migrăm `Control.ForegroundProperty` si toate binding-urile/template-urile la `Brush?`.
2. `TextBlock`, `Button`, `CheckBox`, `Label`, `ContentPresenter`, `TextBoxBase` si controalele derivate trimit brush-ul complet catre `TextRenderer`.
3. `CaretColor` si `SelectionBackground` raman `Color` in aceasta etapa.
4. Verificam propagarea mostenita si invalidarea subarborilor.

## Faza 4: aspecte, teme si motion

1. Token-urile foreground devin `AspectToken<Brush?>`.
2. Tema deriva brush-uri solide din culorile semantice existente.
3. `BrushMixer` anima solid brush-uri si gradienti structural compatibili.
4. Image/drawing/visual brush fac snap la destinatie sau cer animarea proprietatilor interne, nu interpolare intre obiecte.

## Faza 5: markup

1. Shorthand-ul de culoare, resursele si property elements au aceeasi semantica runtime/sourcegen.
2. Exemple acceptate:

```xml
<TextBlock Foreground="Tomato" Text="Salut" />
```

```xml
<TextBlock Text="Gradient">
  <TextBlock.Foreground>
    <LinearGradientBrush ... />
  </TextBlock.Foreground>
</TextBlock>
```

3. Resursele care nu sunt `Brush` produc diagnostic de tip.

## Testare si acceptanta

- teste API pentru `UiProperty<Brush?>`, default si mostenire;
- teste de shaping care demonstreaza ca brush-ul nu schimba metricile;
- pixel tests pentru text solid, linear gradient, radial gradient si image brush;
- teste bidi, wrapping, trimming, selectie si clipping;
- teste de cache pentru reutilizarea mastii glyph-urilor;
- teste DPI, resize, device reset si ferestre cu device-uri diferite;
- teste motion pentru solid si gradient compatibil;
- build si suita completa fara warnings sau erori;
- documentatie API si ghid de migrare sincronizate.

## Riscuri

- colorarea textului direct in textura actuala poate multiplica exploziv cache-ul;
- coordonatele brush-ului trebuie stabilite pentru fiecare linie si run bidi;
- subpixel antialiasing-ul poate fi incompatibil cu o masca alpha simpla;
- visual brush poate introduce cicluri prin continut care include textul sursa.

## Non-obiective

- migrarea `CaretColor` sau `SelectionBackground`;
- animarea arbitrara intre doua image/visual brushes;
- schimbarea algoritmilor de shaping, bidi sau line breaking;
- compatibilitate binara cu `UiProperty<Color>`.
