# Plan: migrarea `Background` si `BorderBrush` la tipul `Brush`

## Rezumat

Schimbam `Control.Background` si `Control.BorderBrush` din `Color` in `Brush?`, astfel incat controalele sa poata folosi culori solide, gradienti, imagini, desene si brush-uri vizuale. Planul depinde de infrastructura din `2026-07-11-brush-types-and-rendering.md`.

Acestea sunt breaking changes intentionate. Valorile `Color` trebuie impachetate explicit in `SolidColorBrush`; nu adaugam conversii implicite si nu pastram aliasuri de culoare.

## Contracte finale

```csharp
public static readonly UiProperty<Brush?> BackgroundProperty;
public Brush? Background { get; set; }

public static readonly UiProperty<Brush?> BorderBrushProperty;
public Brush? BorderBrush { get; set; }
```

- Ambele proprietati au valoarea implicita `null`.
- `null` inseamna ca suprafata sau conturul nu se deseneaza.
- `BorderThickness` ramane separat; grosimea zero suprima conturul.
- `Background="Tomato"` si `BorderBrush="Tomato"` produc explicit `new SolidColorBrush(Color.Tomato)`.
- Referintele de resurse trebuie sa rezolve la `Brush`.
- Formele property-element accepta brush-uri compuse.
- `Foreground` ramane `Color` in acest plan si are un plan separat.

## Faza 1: contractul `Control`

1. Schimbam `BackgroundProperty` si `BorderBrushProperty` in `UiProperty<Brush?>`.
2. Schimbam proprietatile CLR in `Brush?` cu default `null`.
3. Pastram invalidarea de render si input visual.
4. Eliminam presupunerile bazate pe `Color.A`.
5. Adaugam teste API pentru tip, default si absenta aliasului `BorderColor`.

## Faza 2: controale si randare

1. Adaptam `Border`, `Button`, `CheckBox`, `ListBoxItem`, `ScrollBar`, `Thumb`, `Track`, `ProgressBar` si `TextBoxBase`.
2. Ordinea ramane background -> border -> content.
3. Brush-urile sunt trimise intacte prin `DrawingContext` si `DrawCommand` catre backend.
4. `SolidColorBrush` foloseste calea rapida existenta; brush-urile compuse folosesc pipeline-ul texturat.
5. Testam fill si stroke cu solid, linear gradient, radial gradient si image brush.

## Faza 3: aspecte, teme si motion

1. `ButtonTokens.Background`, `HoverBackground`, `PressedBackground` si `BorderBrush` devin `AspectToken<Brush?>`.
2. Adaugam token-uri semantice `DefaultAspectTokens.Brush.Background`, `Surface` si `Border`.
3. Culorile din `ThemePalette` raman culori semantice pentru clear color si derivarea brush-urilor; proprietatile de control primesc token-uri `Brush`.
4. `AnimatablePropertyRegistry` foloseste `BrushMixer` pentru ambele proprietati.
5. Solid brush-urile si gradientii cu stop-uri compatibile se interpoleaza; image/drawing/visual brush-urile nu se interpoleaza ca valori intregi.

## Faza 4: markup si generator

1. Runtime schema si source generator detecteaza ambele proprietati ca `Brush?`.
2. Suportam shorthand de culoare, resurse si property elements:

```xml
<Border Background="Tomato" BorderBrush="#FF334455" />
```

```xml
<Border Background="$Surface" BorderBrush="$Outline" />
```

```xml
<Border>
  <Border.Background>
    <LinearGradientBrush ... />
  </Border.Background>
  <Border.BorderBrush>
    <ImageBrush ... />
  </Border.BorderBrush>
</Border>
```

3. Resursele incompatibile si brush-urile incomplete produc diagnostice, nu conversii ascunse.

## Faza 5: migrare si documentatie

```csharp
// vechi
control.Background = Color.White;
control.BorderBrush = Color.Red;

// nou
control.Background = new SolidColorBrush(Color.White);
control.BorderBrush = new SolidColorBrush(Color.Red);
```

1. Actualizam sample-urile, testele, paginile API si manifestul documentatiei.
2. Documentam `null` ca transparent semantic.
3. Nu introducem aliasuri sau operatori impliciti pentru `Color`.

## Acceptanta

- build complet fara warnings sau erori;
- toate testele existente migrate;
- teste API si invalidare pentru ambele proprietati;
- teste markup pentru shorthand, resurse si property elements;
- teste de randare pentru solid, linear, radial si image brush pe fill si border;
- teste motion pentru solid si gradient compatibil;
- documentatia si manifestul sincronizate.

## Non-obiective

- migrarea `Foreground`;
- schimbarea `SelectionBackground`, `CaretColor` sau a clear color-ului ferestrei;
- conversii implicite `Color` -> `Brush`;
- un sistem nou de layout sau clipping.
