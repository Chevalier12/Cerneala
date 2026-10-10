# Complete TextBlock Layout Contract Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Prioritize throughput with subagents for independent inspection, test drafting, implementation patches, and verification, while preserving this plan's dependency order.

**Goal:** Make `TextBlock` expose and use the text layout knobs already present in `UI/Text`: `TextWrapping` and `TextTrimming`. MVP text can remain approximate, but measure/render/cache identity must be coherent and testable.

**Architecture:** Keep text rendering built on existing `TextMeasurer`, `TextRenderer`, `LineBreakService`, `TextLayoutCache`, and `TextRunStyle`. Do not add rich text, a XAML text model, or a second text primitive.

**Tech Stack:** C#/.NET 8, xUnit, existing `UI/Text`, retained scheduler, root-owned resources.

---

## File Structure

- Modify: `UI/Controls/TextBlock.cs`
  - Add `TextWrappingProperty` and `TextTrimmingProperty`.
  - Include them in `CreateTextStyle()`.
  - Ensure changes invalidate measure/render.
- Modify: `UI/Text/TextRunStyle.cs`
  - Keep validation and immutable style contract.
- Modify: `UI/Text/TextMeasurer.cs`
  - Verify layout key includes wrapping width, wrapping, trimming, font identity, scale.
- Modify: `UI/Text/TextRenderer.cs`
  - Ensure render uses same measurement path/key as measure.
- Modify: `UI/Text/LineBreakService.cs`
  - Keep MVP deterministic; no Unicode-perfect wrapping.
- Modify: `UI/Controls/TextBoxBase.cs`
  - Resolve `FontResourceId` through `Root.ResourceProvider` when local provider is null.
- Create: `tests/Cerneala.Tests/Controls/TextBlockLayoutContractTests.cs`
- Create: `tests/Cerneala.Tests/UI/Text/TextBlockTextServiceIntegrationTests.cs`

## Important Existing Behavior

`TextRunStyle` already exposes `Wrapping` and `Trimming`, and `TextLayoutKey` already includes them. But `TextBlock.CreateTextStyle()` currently does not expose or pass those values from public `TextBlock` API.

Target behavior:

- `TextBlock.TextWrapping` defaults to `TextWrapping.NoWrap`.
- `TextBlock.TextTrimming` defaults to `TextTrimming.None`.
- Changing either property invalidates measure and render.
- Measure and render use the same `TextRunStyle` and compatible layout key.
- Root font resource mutation invalidates wrapped text measurement and render cache.

## Rules

- [ ] Do not implement full Unicode segmentation.
- [ ] Do not implement rich text/inlines.
- [ ] Do not claim production ellipsis/trimming unless implemented and tested.
- [ ] Do not add new drawing/layout primitives.
- [ ] Do not expand TextBox beyond root resource provider fallback.

---

### Task 1: Add RED TextBlock Layout Contract Tests

**Files:**
- Create: `tests/Cerneala.Tests/Controls/TextBlockLayoutContractTests.cs`
- Create: `tests/Cerneala.Tests/UI/Text/TextBlockTextServiceIntegrationTests.cs`

- [ ] **Step 1: Add TextBlock API/invalidation tests**

Create tests:

```csharp
TextBlockWrappingAffectsMeasure()
TextBlockWrappingInvalidatesMeasureAndRender()
TextBlockTrimmingPropertyInvalidatesRenderWithoutClaimingProductionEllipsis()
TextBlockMeasureAndRenderUseSameTextLayoutKey()
TextBlockRootFontResourceChangeInvalidatesWrappedMeasurement()
```

Test intent:

- Use long text with finite available width.
- Assert `Wrap` produces more lines or greater height than `NoWrap` using current simple line-break service.
- Assert wrapping/trimming changes create retained layout/render work.
- Assert trimming affects layout identity even if not a production ellipsis renderer.
- Assert root font resource mutation invalidates wrapped measurement/render.

- [ ] **Step 2: Add TextBox resource fallback test**

Create:

```csharp
TextBoxBaseUsesRootResourceProviderWhenLocalProviderIsNull()
```

Attach a `TextBox` to root, set root `ResourceStore`, set `FontResourceId`, and assert measure/render can resolve the font without assigning `TextBoxBase.ResourceProvider` directly.

- [ ] **Step 3: Run targeted tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~TextBlockLayoutContractTests|FullyQualifiedName~TextBlockTextServiceIntegrationTests"
```

Expected: RED because `TextBlock.TextWrapping`/`TextTrimming` do not exist and `TextBoxBase` only checks local provider.

- [ ] **Step 4: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\Controls\TextBlockLayoutContractTests.cs tests\Cerneala.Tests\UI\Text\TextBlockTextServiceIntegrationTests.cs
git commit -m "test: capture textblock layout contract"
```

---

### Task 2: Add TextBlock Wrapping And Trimming Properties

**Files:**
- Modify: `UI/Controls/TextBlock.cs`

- [ ] **Step 1: Register properties**

Add near `TextProperty`:

```csharp
public static readonly UiProperty<TextWrapping> TextWrappingProperty = UiProperty<TextWrapping>.Register(
    nameof(TextWrapping),
    typeof(TextBlock),
    new UiPropertyMetadata<TextWrapping>(
        TextWrapping.NoWrap,
        UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender,
        validateValue: value => Enum.IsDefined(value)));

public static readonly UiProperty<TextTrimming> TextTrimmingProperty = UiProperty<TextTrimming>.Register(
    nameof(TextTrimming),
    typeof(TextBlock),
    new UiPropertyMetadata<TextTrimming>(
        TextTrimming.None,
        UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender,
        validateValue: value => Enum.IsDefined(value)));
```

- [ ] **Step 2: Add CLR properties**

```csharp
public TextWrapping TextWrapping
{
    get => GetValue(TextWrappingProperty);
    set => SetValue(TextWrappingProperty, value);
}

public TextTrimming TextTrimming
{
    get => GetValue(TextTrimmingProperty);
    set => SetValue(TextTrimmingProperty, value);
}
```

- [ ] **Step 3: Pass into `TextRunStyle`**

Use named arguments to avoid constructor-order mistakes:

```csharp
return new TextRunStyle(
    FontFamily,
    FontSize,
    wrapping: TextWrapping,
    trimming: TextTrimming,
    color: Foreground,
    fontResourceId: FontResourceId);
```

---

### Task 3: Keep Text Service Identity Coherent

**Files:**
- Modify: `UI/Text/TextMeasurer.cs`
- Modify: `UI/Text/TextRenderer.cs`
- Modify: `UI/Text/LineBreakService.cs` only if tests expose deterministic bugs.

- [ ] **Step 1: Verify `TextLayoutKey` changes with wrapping/trimming**

`TextMeasurer.Measure(...)` should construct a key containing text, font identity, font size, wrapping, wrapping width, trimming, and scale.

- [ ] **Step 2: Ensure render uses the same measurer path**

`TextRenderer.Render(...)` should call `textMeasurer.Measure(...)` and draw returned lines. Do not duplicate line-breaking in renderer.

- [ ] **Step 3: Keep trimming honest**

If trimming enum has an ellipsis mode, either implement tiny deterministic truncation or keep rendering unchanged while key/invalidation changes. Do not overbuild typography.

---

### Task 4: Add Root Resource Provider Fallback To TextBoxBase

**Files:**
- Modify: `UI/Controls/TextBoxBase.cs`

- [ ] **Step 1: Add provider helper**

```csharp
private IResourceProvider? ResolveResourceProvider()
{
    return ResourceProvider ?? Root?.ResourceProvider;
}
```

- [ ] **Step 2: Use helper in measurer/renderer**

Use fallback provider before constructing `FontResolver` in `GetTextMeasurer()` and `GetTextRenderer()`.

- [ ] **Step 3: Do not add broader TextBox resource dependency tracking unless tests require it**

This plan closes provider resolution only.

---

### Task 5: Verify GREEN And Regressions

- [ ] **Step 1: Run targeted tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~TextBlockLayoutContractTests|FullyQualifiedName~TextBlockTextServiceIntegrationTests|FullyQualifiedName~TextBlockTests|FullyQualifiedName~TextBlockInvalidationTests|FullyQualifiedName~TextBoxTests"
```

Expected: GREEN.

- [ ] **Step 2: Run text pipeline tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~TextPipelineTests|FullyQualifiedName~TextMeasurer|FullyQualifiedName~TextRenderer"
```

Expected: GREEN.

- [ ] **Step 3: Run full suite**

```powershell
dotnet test Cerneala.slnx
```

Expected: GREEN.

- [ ] **Step 4: Commit implementation**

```powershell
git add UI\Controls\TextBlock.cs UI\Controls\TextBoxBase.cs UI\Text tests\Cerneala.Tests\Controls\TextBlockLayoutContractTests.cs tests\Cerneala.Tests\UI\Text\TextBlockTextServiceIntegrationTests.cs
git commit -m "feat: complete textblock layout contract"
```
