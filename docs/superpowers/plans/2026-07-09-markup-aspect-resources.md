# Markup Aspect Resources Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `.cui.xml` source-generator support for `Resources`, named resources via `Name`, reusable `Aspect` declarations, `$Name` references, and deterministic cascade application.

**Architecture:** Keep the implementation inside `Cerneala.SourceGen/UiMarkupGenerator.cs` and split the existing nested `GenerationScope` into small nested records/helpers for document parsing, symbol collection, value parsing, and element emission. Preserve the current generated-code style: direct public property assignments, no runtime markup parser, no `SetValue`, and source-generator diagnostics for invalid authoring input.

**Tech Stack:** C# incremental source generator, `System.Xml` / `System.Xml.Linq`, xUnit source-generator tests, RoslynIndexer for navigation and re-indexing.

---

## File Structure

- Modify: `Cerneala.SourceGen/UiMarkupGenerator.cs`
  - Add diagnostics for resource/aspect/reference failures.
  - Add fragment-level markup parsing so a file may contain `<Resources>` plus one UI root as sibling top-level elements.
  - Add nested model records: `MarkupDocument`, `NamedSymbol`, `SolidColorBrushResource`, `AspectResource`, `AspectPropertyAssignment`, `PropertySpec`, and `GeneratedExpression`.
  - Refactor `GenerationScope` so property parsing can be reused by both XML attributes and aspect declaration bodies.
  - Emit resources before UI elements, then apply unnamed type aspects, named aspects, and local attributes in that order.
- Modify: `tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorTests.cs`
  - Add focused failing tests first for each behavior.
  - Reuse existing `RunGenerator`, `SingleGeneratedSource`, `InvokeCreate`, and `AssertDiagnostic`.
  - Add `using Cerneala.Drawing;` only if runtime assertions need direct `Color` equality.
- Modify: `FileTree.md`
  - Regenerate after source/test edits using `.\Tools\scripts\New-FileTree.ps1`.
- Reference only: `docs/superpowers/specs/2026-07-09-markup-aspect-resources-design.md`
  - Do not edit unless implementation reveals a spec contradiction.

## Implementation Notes

- Treat `Resources` as authoring metadata, not a UI element. It must not be emitted as a retained UI element.
- Valid `.cui.xml` may be an XML fragment with one optional top-level `Resources` element and exactly one top-level UI root element.
- `Name` is the only reference identity. `Key` is not supported in markup.
- `$Name` references resolve through one document-level namespace shared by elements and resources.
- `Aspect="$KickerText"` accepts only named `Aspect` resources.
- `Foreground = $PulseColor;` accepts a named resource that can produce a `Color`.
- `SolidColorBrush` resources emit `global::Cerneala.UI.Media.SolidColorBrush` instances and can be coerced to `Color` through their constructor color when assigned to `Color` properties.
- Run RoslynIndexer after each production or test edit:

```text
roslyn_index(repoRoot: "C:\\Users\\Shadow\\Desktop\\Cerneala", configPath: "C:\\Users\\Shadow\\Desktop\\Cerneala\\Cerneala.slnx", includeNonCSharpText: false, includeGenerated: false)
```

---

### Task 1: Fragment Parser And Resource Stripping

**Files:**
- Modify: `Cerneala.SourceGen/UiMarkupGenerator.cs`
- Test: `tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorTests.cs`

- [ ] **Step 1: Add failing tests for fragment parsing and resource stripping**

Add these tests near the existing successful generator tests:

```csharp
[Fact]
public void ResourcesCanPrecedeSingleUiRootWithoutEmittingResourceElement()
{
    const string markup = """
        <Resources>
          <SolidColorBrush Name="PulseColor" Color="#FF5D73" />
        </Resources>
        <TextBlock Text="Hello" />
        """;

    GeneratorRunResult result = RunGenerator("ResourceFragment.cui.xml", markup, out Compilation compilation);
    string generatedSource = SingleGeneratedSource(result);

    Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    Assert.Contains("public static partial class ResourceFragmentFactory", generatedSource);
    Assert.DoesNotContain("global::Cerneala.UI.Controls.Resources", generatedSource);
    Assert.Contains("global::Cerneala.UI.Controls.TextBlock", generatedSource);

    using MemoryStream stream = new();
    EmitResult emit = compilation.Emit(stream);
    Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

    TextBlock root = Assert.IsType<TextBlock>(InvokeCreate(stream, "Cerneala.GeneratedUi.ResourceFragmentFactory"));
    Assert.Equal("Hello", root.Text);
}

[Fact]
public void MultipleUiRootsReportMalformedMarkupDiagnostic()
{
    const string markup = """
        <TextBlock Text="One" />
        <TextBlock Text="Two" />
        """;

    GeneratorRunResult result = RunGenerator("MultipleRoots.cui.xml", markup, out _);

    Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI001", "MultipleRoots.cui.xml");
    Assert.Contains("exactly one UI root", diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
    Assert.Empty(result.GeneratedSources);
}
```

- [ ] **Step 2: Run tests and confirm they fail**

Run:

```powershell
dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj --filter FullyQualifiedName~UiMarkupGeneratorTests
```

Expected: the first test fails because current `XDocument.Parse` rejects sibling top-level elements; the second fails because there is no custom “exactly one UI root” diagnostic.

- [ ] **Step 3: Add fragment document model and parser**

In `UiMarkupGenerator`, add this descriptor near the existing diagnostics:

```csharp
private static readonly DiagnosticDescriptor InvalidDocumentShape = new(
    "CERNEALAUI005",
    "Invalid UI markup document shape",
    "Markup file '{0}' has invalid document shape: {1}",
    "Cerneala.UiMarkup",
    DiagnosticSeverity.Error,
    true);
```

Add these nested records near `MarkupSource`:

```csharp
private sealed record MarkupDocument(XElement? Resources, XElement Root);

private sealed record ParsedDocument(MarkupDocument? Document, Diagnostic? Diagnostic);
```

Replace the body of `GenerateFile` after the `file.Text is null` guard with a call to a new parser:

```csharp
ParsedDocument parsed = ParseDocument(file);
if (parsed.Diagnostic is not null)
{
    context.ReportDiagnostic(parsed.Diagnostic);
    return;
}

MarkupDocument document = parsed.Document!;
GenerationScope scope = new(context, file, document);
string rootVariable = scope.EmitElement(document.Root);
```

Add the parser:

```csharp
private static ParsedDocument ParseDocument(MarkupSource file)
{
    try
    {
        List<XElement> elements = [];
        XmlReaderSettings settings = new()
        {
            ConformanceLevel = ConformanceLevel.Fragment,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true
        };

        using StringReader stringReader = new(file.Text ?? string.Empty);
        using XmlReader reader = XmlReader.Create(stringReader, settings);
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                elements.Add((XElement)XNode.ReadFrom(reader));
            }
        }

        XElement? resources = null;
        List<XElement> roots = [];
        foreach (XElement element in elements)
        {
            if (element.Name.LocalName == "Resources")
            {
                if (resources is not null)
                {
                    return InvalidShape(file, element, "Only one top-level Resources element is allowed.");
                }

                resources = element;
                continue;
            }

            roots.Add(element);
        }

        if (roots.Count != 1)
        {
            return InvalidShape(file, roots.FirstOrDefault() ?? resources ?? new XElement("Missing"), "Markup must contain exactly one UI root element.");
        }

        return new ParsedDocument(new MarkupDocument(resources, roots[0]), null);
    }
    catch (XmlException ex)
    {
        return new ParsedDocument(null, Diagnostic.Create(MalformedMarkup, CreateLocation(file, ex.LineNumber, ex.LinePosition), Path.GetFileName(file.Path), ex.Message));
    }
}

private static ParsedDocument InvalidShape(MarkupSource file, object locationSource, string message)
{
    return new ParsedDocument(null, Diagnostic.Create(InvalidDocumentShape, CreateLocation(file, locationSource), Path.GetFileName(file.Path), message));
}
```

- [ ] **Step 4: Update `GenerationScope` constructor**

Change the constructor fields:

```csharp
private readonly MarkupDocument document;

public GenerationScope(SourceProductionContext context, MarkupSource file, MarkupDocument document)
{
    this.context = context;
    this.file = file;
    this.document = document;
}
```

- [ ] **Step 5: Run tests and confirm Task 1 passes**

Run:

```powershell
dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj --filter FullyQualifiedName~UiMarkupGeneratorTests
```

Expected: all existing generator tests plus the two new Task 1 tests pass.

- [ ] **Step 6: Re-index and commit**

Run RoslynIndexer for `Cerneala.slnx`, then:

```powershell
git add .\Cerneala.SourceGen\UiMarkupGenerator.cs .\tests\Cerneala.Tests.SourceGen\UiMarkupGeneratorTests.cs
git commit -m "feat: parse markup resources as document metadata"
```

---

### Task 2: Property Specs And Shared Value Parsing

**Files:**
- Modify: `Cerneala.SourceGen/UiMarkupGenerator.cs`
- Test: `tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorTests.cs`

- [ ] **Step 1: Add failing regression test for existing typed generation**

Add this test to lock the current direct-property behavior before refactoring:

```csharp
[Fact]
public void RefactoredPropertySpecsPreserveExistingDirectAssignments()
{
    const string markup = """
        <Border Background="White" BorderBrush="0, 1, 2, 3" BorderThickness="1" Padding="2">
          <TextBlock Text="Typed" FontFamily="Consolas" FontSize="12" Foreground="Black" Margin="1,2,3,4" />
        </Border>
        """;

    GeneratorRunResult result = RunGenerator("DirectAssignments.cui.xml", markup, out Compilation compilation);
    string generatedSource = SingleGeneratedSource(result);

    Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    Assert.Contains(".Background = global::Cerneala.Drawing.Color.White;", generatedSource);
    Assert.Contains(".BorderBrush = new global::Cerneala.Drawing.Color(0, 1, 2, 3);", generatedSource);
    Assert.Contains(".BorderThickness = new global::Cerneala.UI.Layout.Thickness(1f);", generatedSource);
    Assert.Contains(".Padding = new global::Cerneala.UI.Layout.Thickness(2f);", generatedSource);
    Assert.Contains(".FontFamily = \"Consolas\";", generatedSource);
    Assert.Contains(".FontSize = 12f;", generatedSource);
    Assert.Contains(".Foreground = global::Cerneala.Drawing.Color.Black;", generatedSource);
    Assert.Contains(".Margin = new global::Cerneala.UI.Layout.Thickness(1f, 2f, 3f, 4f);", generatedSource);

    using MemoryStream stream = new();
    EmitResult emit = compilation.Emit(stream);
    Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
}
```

- [ ] **Step 2: Run test and confirm current code passes**

Run:

```powershell
dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj --filter FullyQualifiedName~RefactoredPropertySpecsPreserveExistingDirectAssignments
```

Expected: PASS before refactor; this is a guard, not a red test.

- [ ] **Step 3: Add reusable property/value model**

Inside `GenerationScope`, add:

```csharp
private enum MarkupValueKind
{
    String,
    Bool,
    Float,
    PositiveFloat,
    Thickness,
    NonNegativeThickness,
    Color
}

private sealed record PropertySpec(
    string Name,
    Func<string, bool> AppliesToElement,
    MarkupValueKind ValueKind);

private sealed record GeneratedExpression(string Code, MarkupValueKind Kind);

private static readonly PropertySpec[] PropertySpecs =
[
    new("Text", element => element == "TextBlock", MarkupValueKind.String),
    new("Content", element => element == "Button", MarkupValueKind.String),
    new("IsEnabled", _ => true, MarkupValueKind.Bool),
    new("IsVisible", _ => true, MarkupValueKind.Bool),
    new("Margin", _ => true, MarkupValueKind.Thickness),
    new("Background", IsControlElement, MarkupValueKind.Color),
    new("Foreground", IsControlElement, MarkupValueKind.Color),
    new("BorderBrush", IsControlElement, MarkupValueKind.Color),
    new("BorderThickness", IsControlElement, MarkupValueKind.NonNegativeThickness),
    new("Padding", IsControlElement, MarkupValueKind.NonNegativeThickness),
    new("FontFamily", IsControlElement, MarkupValueKind.String),
    new("FontSize", IsControlElement, MarkupValueKind.PositiveFloat)
];
```

Add lookup:

```csharp
private static PropertySpec? FindPropertySpec(string elementName, string propertyName)
{
    return PropertySpecs.FirstOrDefault(spec => spec.Name == propertyName && spec.AppliesToElement(elementName));
}
```

- [ ] **Step 4: Refactor `EmitProperty` to use `PropertySpec`**

Replace the switch in `EmitProperty` with:

```csharp
PropertySpec? spec = FindPropertySpec(elementName, propertyName);
if (spec is null)
{
    if (!HasErrors)
    {
        Report(UnsupportedProperty, attribute, elementName, propertyName);
    }

    return;
}

GeneratedExpression? expression = ParseLiteralValue(elementName, propertyName, attribute, value, spec.ValueKind);
if (expression is null)
{
    return;
}

Lines.Add(variable + "." + propertyName + " = " + expression.Code + ";");
```

Add parser dispatcher:

```csharp
private GeneratedExpression? ParseLiteralValue(string elementName, string propertyName, XAttribute attribute, string value, MarkupValueKind kind)
{
    string? code = kind switch
    {
        MarkupValueKind.String when !string.IsNullOrWhiteSpace(value) => Literal(value),
        MarkupValueKind.Bool => Bool(elementName, propertyName, attribute),
        MarkupValueKind.Float => Float(elementName, propertyName, attribute),
        MarkupValueKind.PositiveFloat => PositiveFloat(elementName, propertyName, attribute),
        MarkupValueKind.Thickness => Thickness(elementName, propertyName, attribute),
        MarkupValueKind.NonNegativeThickness => NonNegativeThickness(elementName, propertyName, attribute),
        MarkupValueKind.Color => Color(elementName, propertyName, attribute),
        _ => null
    };

    if (code is null)
    {
        if (kind == MarkupValueKind.String)
        {
            Report(InvalidPropertyValue, attribute, elementName, propertyName, value);
        }

        return null;
    }

    return new GeneratedExpression(code, kind);
}
```

- [ ] **Step 5: Run full source-generator tests**

Run:

```powershell
dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj --filter FullyQualifiedName~UiMarkupGeneratorTests
```

Expected: PASS. Generated source remains direct public property assignment.

- [ ] **Step 6: Re-index and commit**

Run RoslynIndexer, then:

```powershell
git add .\Cerneala.SourceGen\UiMarkupGenerator.cs .\tests\Cerneala.Tests.SourceGen\UiMarkupGeneratorTests.cs
git commit -m "refactor: share markup property value parsing"
```

---

### Task 3: `SolidColorBrush` Resource Parsing And Hex Colors

**Files:**
- Modify: `Cerneala.SourceGen/UiMarkupGenerator.cs`
- Test: `tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorTests.cs`

- [ ] **Step 1: Add failing tests for named brush resources**

Add:

```csharp
[Fact]
public void SolidColorBrushResourceEmitsNamedBrushVariable()
{
    const string markup = """
        <Resources>
          <SolidColorBrush Name="PulseColor" Color="#FF5D73" />
        </Resources>
        <TextBlock Text="Hello" />
        """;

    GeneratorRunResult result = RunGenerator("BrushResource.cui.xml", markup, out Compilation compilation);
    string generatedSource = SingleGeneratedSource(result);

    Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    Assert.Contains("global::Cerneala.UI.Media.SolidColorBrush PulseColor = new(new global::Cerneala.Drawing.Color(255, 93, 115));", generatedSource);

    using MemoryStream stream = new();
    EmitResult emit = compilation.Emit(stream);
    Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
}

[Fact]
public void InvalidSolidColorBrushColorReportsDiagnostic()
{
    const string markup = """
        <Resources>
          <SolidColorBrush Name="PulseColor" Color="#NOPE" />
        </Resources>
        <TextBlock Text="Hello" />
        """;

    GeneratorRunResult result = RunGenerator("BadBrush.cui.xml", markup, out _);

    Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI004", "BadBrush.cui.xml");
    Assert.Contains("SolidColorBrush.Color", diagnostic.GetMessage());
    Assert.Empty(result.GeneratedSources);
}
```

- [ ] **Step 2: Run tests and confirm they fail**

Run:

```powershell
dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj --filter "FullyQualifiedName~SolidColorBrushResourceEmitsNamedBrushVariable|FullyQualifiedName~InvalidSolidColorBrushColorReportsDiagnostic"
```

Expected: FAIL because resource parsing and hex color parsing do not exist.

- [ ] **Step 3: Add resource model**

Inside `GenerationScope`, add:

```csharp
private enum NamedSymbolKind
{
    Element,
    SolidColorBrush,
    Aspect
}

private sealed record NamedSymbol(string Name, NamedSymbolKind Kind, object Source);

private sealed record SolidColorBrushResource(string Name, string Variable, string ColorExpression, ColorLiteral Color, XElement Source);

private readonly record struct ColorLiteral(byte R, byte G, byte B, byte A)
{
    public string ToExpression()
    {
        return A == 255
            ? "new global::Cerneala.Drawing.Color(" + R + ", " + G + ", " + B + ")"
            : "new global::Cerneala.Drawing.Color(" + R + ", " + G + ", " + B + ", " + A + ")";
    }
}
```

Add fields:

```csharp
private readonly Dictionary<string, NamedSymbol> symbols = new(StringComparer.Ordinal);
private readonly Dictionary<string, SolidColorBrushResource> solidColorBrushes = new(StringComparer.Ordinal);
```

- [ ] **Step 4: Parse resources in constructor**

At the end of `GenerationScope` constructor:

```csharp
ReadResources();
```

Add:

```csharp
private void ReadResources()
{
    if (document.Resources is null)
    {
        return;
    }

    foreach (XElement resource in document.Resources.Elements())
    {
        switch (resource.Name.LocalName)
        {
            case "SolidColorBrush":
                ReadSolidColorBrush(resource);
                break;
            case "Aspect":
                break;
            case "Resources":
                Report(InvalidDocumentShape, resource, Path.GetFileName(file.Path), "Nested Resources declarations are not supported.");
                break;
            default:
                Report(UnsupportedElement, resource, resource.Name.LocalName);
                break;
        }
    }
}
```

Add brush reader:

```csharp
private void ReadSolidColorBrush(XElement resource)
{
    string? name = RequiredName(resource);
    if (name is null)
    {
        return;
    }

    XAttribute? colorAttribute = resource.Attribute("Color");
    if (colorAttribute is null || ParseHexColor(colorAttribute.Value) is not ColorLiteral color)
    {
        Report(InvalidPropertyValue, colorAttribute ?? resource, "SolidColorBrush", "Color", colorAttribute?.Value ?? string.Empty);
        return;
    }

    string variable = CreateIdentifier(name);
    SolidColorBrushResource brush = new(name, variable, color.ToExpression(), color, resource);
    if (!AddSymbol(name, NamedSymbolKind.SolidColorBrush, brush, resource))
    {
        return;
    }

    solidColorBrushes[name] = brush;
    Lines.Add("global::Cerneala.UI.Media.SolidColorBrush " + variable + " = new(" + brush.ColorExpression + ");");
}
```

Add helpers:

```csharp
private string? RequiredName(XElement element)
{
    string? name = element.Attribute("Name")?.Value;
    if (string.IsNullOrWhiteSpace(name))
    {
        Report(InvalidPropertyValue, element, element.Name.LocalName, "Name", name ?? string.Empty);
        return null;
    }

    return name;
}

private bool AddSymbol(string name, NamedSymbolKind kind, object source, XElement location)
{
    if (symbols.ContainsKey(name))
    {
        Report(InvalidDocumentShape, location, Path.GetFileName(file.Path), "Duplicate Name '" + name + "'.");
        return false;
    }

    symbols.Add(name, new NamedSymbol(name, kind, source));
    return true;
}
```

Add hex parser:

```csharp
private static ColorLiteral? ParseHexColor(string value)
{
    if (value.Length != 7 && value.Length != 9)
    {
        return null;
    }

    if (value[0] != '#')
    {
        return null;
    }

    static bool TryByte(string text, out byte value)
    {
        return byte.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
    }

    if (value.Length == 7 &&
        TryByte(value.Substring(1, 2), out byte r) &&
        TryByte(value.Substring(3, 2), out byte g) &&
        TryByte(value.Substring(5, 2), out byte b))
    {
        return new ColorLiteral(r, g, b, 255);
    }

    if (value.Length == 9 &&
        TryByte(value.Substring(1, 2), out byte a) &&
        TryByte(value.Substring(3, 2), out byte rr) &&
        TryByte(value.Substring(5, 2), out byte gg) &&
        TryByte(value.Substring(7, 2), out byte bb))
    {
        return new ColorLiteral(rr, gg, bb, a);
    }

    return null;
}
```

- [ ] **Step 5: Run tests and confirm Task 3 passes**

Run:

```powershell
dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj --filter FullyQualifiedName~UiMarkupGeneratorTests
```

Expected: PASS.

- [ ] **Step 6: Re-index and commit**

Run RoslynIndexer, then:

```powershell
git add .\Cerneala.SourceGen\UiMarkupGenerator.cs .\tests\Cerneala.Tests.SourceGen\UiMarkupGeneratorTests.cs
git commit -m "feat: parse solid color brush markup resources"
```

---

### Task 4: Aspect Body Parser

**Files:**
- Modify: `Cerneala.SourceGen/UiMarkupGenerator.cs`
- Test: `tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorTests.cs`

- [ ] **Step 1: Add failing tests for unnamed and named aspect parsing**

Add:

```csharp
[Fact]
public void UnnamedAspectAppliesToEveryMatchingElement()
{
    const string markup = """
        <Resources>
          <Aspect Target="TextBlock">
            @default
            {
              FontFamily = "Consolas";
              FontSize = 12;
            }
          </Aspect>
        </Resources>
        <StackPanel>
          <TextBlock Text="One" />
          <TextBlock Text="Two" />
        </StackPanel>
        """;

    GeneratorRunResult result = RunGenerator("DefaultTextAspect.cui.xml", markup, out Compilation compilation);
    string generatedSource = SingleGeneratedSource(result);

    Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    Assert.Equal(2, Count(generatedSource, ".FontFamily = \"Consolas\";"));
    Assert.Equal(2, Count(generatedSource, ".FontSize = 12f;"));

    using MemoryStream stream = new();
    EmitResult emit = compilation.Emit(stream);
    Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
}

[Fact]
public void NamedAspectAppliesAfterUnnamedDefault()
{
    const string markup = """
        <Resources>
          <Aspect Target="TextBlock">
            @default
            {
              FontSize = 14;
              Foreground = Black;
            }
          </Aspect>
          <Aspect Name="KickerText" Target="TextBlock">
            @default
            {
              FontSize = 12;
              Margin = "0,0,0,12";
            }
          </Aspect>
        </Resources>
        <TextBlock Aspect="$KickerText" Text="HELLO" />
        """;

    GeneratorRunResult result = RunGenerator("NamedAspect.cui.xml", markup, out Compilation compilation);
    string generatedSource = SingleGeneratedSource(result);

    Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    Assert.True(generatedSource.IndexOf(".FontSize = 14f;", StringComparison.Ordinal) < generatedSource.IndexOf(".FontSize = 12f;", StringComparison.Ordinal));
    Assert.True(generatedSource.IndexOf(".FontSize = 12f;", StringComparison.Ordinal) < generatedSource.IndexOf(".Text = \"HELLO\";", StringComparison.Ordinal));
    Assert.Contains(".Margin = new global::Cerneala.UI.Layout.Thickness(0f, 0f, 0f, 12f);", generatedSource);

    using MemoryStream stream = new();
    EmitResult emit = compilation.Emit(stream);
    Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
}
```

Add helper at the bottom of the test class:

```csharp
private static int Count(string text, string value)
{
    int count = 0;
    int index = 0;
    while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
    {
        count++;
        index += value.Length;
    }

    return count;
}
```

- [ ] **Step 2: Run tests and confirm they fail**

Run:

```powershell
dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj --filter "FullyQualifiedName~UnnamedAspectAppliesToEveryMatchingElement|FullyQualifiedName~NamedAspectAppliesAfterUnnamedDefault"
```

Expected: FAIL because `Aspect` resources are not parsed or applied.

- [ ] **Step 3: Add aspect models**

Inside `GenerationScope`, add:

```csharp
private sealed record AspectResource(string? Name, string TargetName, IReadOnlyList<AspectPropertyAssignment> Assignments, XElement Source);

private sealed record AspectPropertyAssignment(string PropertyName, string RawValue, bool IsReference, XObject Source);

private readonly Dictionary<string, AspectResource> namedAspects = new(StringComparer.Ordinal);
private readonly Dictionary<string, AspectResource> defaultAspectsByTarget = new(StringComparer.Ordinal);
```

- [ ] **Step 4: Parse `Aspect` resources**

In `ReadResources`, replace the empty `case "Aspect":` with:

```csharp
ReadAspect(resource);
break;
```

Add:

```csharp
private void ReadAspect(XElement resource)
{
    string? targetName = resource.Attribute("Target")?.Value;
    if (string.IsNullOrWhiteSpace(targetName))
    {
        Report(InvalidPropertyValue, resource, "Aspect", "Target", targetName ?? string.Empty);
        return;
    }

    if (ResolveElementType(targetName) is null)
    {
        Report(UnsupportedElement, resource, targetName);
        return;
    }

    string? name = resource.Attribute("Name")?.Value;
    IReadOnlyList<AspectPropertyAssignment> assignments = ParseAspectAssignments(resource);
    if (HasErrors)
    {
        return;
    }

    AspectResource aspect = new(string.IsNullOrWhiteSpace(name) ? null : name, targetName, assignments, resource);
    if (aspect.Name is null)
    {
        if (defaultAspectsByTarget.ContainsKey(targetName))
        {
            Report(InvalidDocumentShape, resource, Path.GetFileName(file.Path), "Duplicate unnamed Aspect for target '" + targetName + "'.");
            return;
        }

        defaultAspectsByTarget.Add(targetName, aspect);
        return;
    }

    if (!AddSymbol(aspect.Name, NamedSymbolKind.Aspect, aspect, resource))
    {
        return;
    }

    namedAspects.Add(aspect.Name, aspect);
}
```

Extract the element type switch from `EmitElement` into:

```csharp
private static string? ResolveElementType(string elementName)
{
    return elementName switch
    {
        "Panel" => "global::Cerneala.UI.Controls.Panel",
        "StackPanel" => "global::Cerneala.UI.Controls.StackPanel",
        "Border" => "global::Cerneala.UI.Controls.Border",
        "Button" => "global::Cerneala.UI.Controls.Button",
        "TextBlock" => "global::Cerneala.UI.Controls.TextBlock",
        _ => null
    };
}
```

- [ ] **Step 5: Add simple aspect declaration parser**

Add:

```csharp
private IReadOnlyList<AspectPropertyAssignment> ParseAspectAssignments(XElement aspect)
{
    string text = string.Concat(aspect.Nodes().OfType<XText>().Select(node => node.Value));
    int start = text.IndexOf('{');
    int end = text.LastIndexOf('}');
    if (start < 0 || end <= start)
    {
        Report(InvalidPropertyValue, aspect, "Aspect", "#body", text.Trim());
        return [];
    }

    string body = text.Substring(start + 1, end - start - 1);
    List<AspectPropertyAssignment> assignments = [];
    foreach (string rawStatement in body.Split(';'))
    {
        string statement = rawStatement.Trim();
        if (statement.Length == 0)
        {
            continue;
        }

        int equals = statement.IndexOf('=');
        if (equals <= 0 || equals == statement.Length - 1)
        {
            Report(InvalidPropertyValue, aspect, "Aspect", "#body", statement);
            return [];
        }

        string propertyName = statement.Substring(0, equals).Trim();
        string value = statement.Substring(equals + 1).Trim();
        bool isReference = value.StartsWith("$", StringComparison.Ordinal);
        if (isReference)
        {
            value = value.Substring(1);
        }

        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
        {
            value = value.Substring(1, value.Length - 2);
        }

        assignments.Add(new AspectPropertyAssignment(propertyName, value, isReference, aspect));
    }

    return assignments;
}
```

- [ ] **Step 6: Apply aspects during element emission**

In `EmitElement`, after creating the element and before local attributes:

```csharp
ApplyAspects(element, variable);
```

Add:

```csharp
private void ApplyAspects(XElement element, string variable)
{
    string elementName = element.Name.LocalName;
    if (defaultAspectsByTarget.TryGetValue(elementName, out AspectResource? defaultAspect))
    {
        EmitAspectAssignments(elementName, variable, defaultAspect);
    }

    XAttribute? aspectAttribute = element.Attribute("Aspect");
    if (aspectAttribute is null)
    {
        return;
    }

    string referenceName = ReadReferenceName(elementName, "Aspect", aspectAttribute);
    if (referenceName.Length == 0)
    {
        return;
    }

    if (!namedAspects.TryGetValue(referenceName, out AspectResource? namedAspect))
    {
        Report(InvalidPropertyValue, aspectAttribute, elementName, "Aspect", aspectAttribute.Value);
        return;
    }

    if (!string.Equals(namedAspect.TargetName, elementName, StringComparison.Ordinal))
    {
        Report(InvalidPropertyValue, aspectAttribute, elementName, "Aspect", aspectAttribute.Value);
        return;
    }

    EmitAspectAssignments(elementName, variable, namedAspect);
}
```

Add:

```csharp
private string ReadReferenceName(string elementName, string propertyName, XAttribute attribute)
{
    string value = attribute.Value.Trim();
    if (!value.StartsWith("$", StringComparison.Ordinal) || value.Length == 1)
    {
        Report(InvalidPropertyValue, attribute, elementName, propertyName, attribute.Value);
        return string.Empty;
    }

    return value.Substring(1);
}
```

Update the local attribute loop to skip `Aspect` and `Name`:

```csharp
foreach (XAttribute attribute in element.Attributes().Where(attribute => !attribute.IsNamespaceDeclaration && attribute.Name.LocalName is not "Aspect" and not "Name"))
```

Add:

```csharp
private void EmitAspectAssignments(string elementName, string variable, AspectResource aspect)
{
    foreach (AspectPropertyAssignment assignment in aspect.Assignments)
    {
        PropertySpec? spec = FindPropertySpec(elementName, assignment.PropertyName);
        if (spec is null)
        {
            Report(UnsupportedProperty, assignment.Source, elementName, assignment.PropertyName);
            return;
        }

        GeneratedExpression? expression = assignment.IsReference
            ? ResolveReferenceValue(elementName, assignment.PropertyName, assignment.RawValue, spec.ValueKind, assignment.Source)
            : ParseAspectLiteralValue(elementName, assignment.PropertyName, assignment.RawValue, spec.ValueKind, assignment.Source);

        if (expression is null)
        {
            return;
        }

        Lines.Add(variable + "." + assignment.PropertyName + " = " + expression.Code + ";");
    }
}
```

Add literal wrapper:

```csharp
private GeneratedExpression? ParseAspectLiteralValue(string elementName, string propertyName, string value, MarkupValueKind kind, XObject source)
{
    XAttribute synthetic = new(propertyName, value);
    return ParseLiteralValue(elementName, propertyName, synthetic, value, kind);
}
```

- [ ] **Step 7: Run tests and confirm Task 4 passes**

Run:

```powershell
dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj --filter FullyQualifiedName~UiMarkupGeneratorTests
```

Expected: PASS.

- [ ] **Step 8: Re-index and commit**

Run RoslynIndexer, then:

```powershell
git add .\Cerneala.SourceGen\UiMarkupGenerator.cs .\tests\Cerneala.Tests.SourceGen\UiMarkupGeneratorTests.cs
git commit -m "feat: parse and apply markup aspect resources"
```

---

### Task 5: `$Name` Resource References In Aspect Values

**Files:**
- Modify: `Cerneala.SourceGen/UiMarkupGenerator.cs`
- Test: `tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorTests.cs`

- [ ] **Step 1: Add failing tests for resource references**

Add:

```csharp
[Fact]
public void AspectCanReferenceSolidColorBrushForColorProperty()
{
    const string markup = """
        <Resources>
          <SolidColorBrush Name="PulseColor" Color="#FF5D73" />
          <Aspect Name="KickerText" Target="TextBlock">
            @default
            {
              Foreground = $PulseColor;
            }
          </Aspect>
        </Resources>
        <TextBlock Aspect="$KickerText" Text="HELLO" />
        """;

    GeneratorRunResult result = RunGenerator("AspectBrushReference.cui.xml", markup, out Compilation compilation);
    string generatedSource = SingleGeneratedSource(result);

    Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    Assert.Contains(".Foreground = new global::Cerneala.Drawing.Color(255, 93, 115);", generatedSource);

    using MemoryStream stream = new();
    EmitResult emit = compilation.Emit(stream);
    Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

    TextBlock root = Assert.IsType<TextBlock>(InvokeCreate(stream, "Cerneala.GeneratedUi.AspectBrushReferenceFactory"));
    Assert.Equal(new Cerneala.Drawing.Color(255, 93, 115), root.Foreground);
}

[Fact]
public void UnknownNameReferenceReportsDiagnostic()
{
    const string markup = """
        <Resources>
          <Aspect Name="KickerText" Target="TextBlock">
            @default
            {
              Foreground = $MissingColor;
            }
          </Aspect>
        </Resources>
        <TextBlock Aspect="$KickerText" />
        """;

    GeneratorRunResult result = RunGenerator("UnknownReference.cui.xml", markup, out _);

    Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI004", "UnknownReference.cui.xml");
    Assert.Contains("MissingColor", diagnostic.GetMessage());
    Assert.Empty(result.GeneratedSources);
}
```

Add `using Cerneala.Drawing;` only if the test uses unqualified `Color`; the snippet above uses fully qualified `Cerneala.Drawing.Color`.

- [ ] **Step 2: Run tests and confirm they fail**

Run:

```powershell
dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj --filter "FullyQualifiedName~AspectCanReferenceSolidColorBrushForColorProperty|FullyQualifiedName~UnknownNameReferenceReportsDiagnostic"
```

Expected: FAIL because `ResolveReferenceValue` is not implemented.

- [ ] **Step 3: Implement reference resolution**

Add:

```csharp
private GeneratedExpression? ResolveReferenceValue(string elementName, string propertyName, string referenceName, MarkupValueKind targetKind, XObject source)
{
    if (!symbols.TryGetValue(referenceName, out NamedSymbol? symbol))
    {
        Report(InvalidPropertyValue, source, elementName, propertyName, "$" + referenceName);
        return null;
    }

    if (targetKind == MarkupValueKind.Color && symbol.Source is SolidColorBrushResource brush)
    {
        return new GeneratedExpression(brush.ColorExpression, MarkupValueKind.Color);
    }

    Report(InvalidPropertyValue, source, elementName, propertyName, "$" + referenceName);
    return null;
}
```

- [ ] **Step 4: Run tests and confirm Task 5 passes**

Run:

```powershell
dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj --filter FullyQualifiedName~UiMarkupGeneratorTests
```

Expected: PASS.

- [ ] **Step 5: Re-index and commit**

Run RoslynIndexer, then:

```powershell
git add .\Cerneala.SourceGen\UiMarkupGenerator.cs .\tests\Cerneala.Tests.SourceGen\UiMarkupGeneratorTests.cs
git commit -m "feat: resolve markup resource references"
```

---

### Task 6: Element `Name` Symbol Registration

**Files:**
- Modify: `Cerneala.SourceGen/UiMarkupGenerator.cs`
- Test: `tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorTests.cs`

- [ ] **Step 1: Add failing tests for element `Name` registration and duplicate detection**

Add:

```csharp
[Fact]
public void ElementNameRegistersGeneratedVariableSymbol()
{
    const string markup = """
        <TextBlock Name="KickerLabel" Text="HELLO" />
        """;

    GeneratorRunResult result = RunGenerator("NamedElement.cui.xml", markup, out Compilation compilation);
    string generatedSource = SingleGeneratedSource(result);

    Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    Assert.Contains("global::Cerneala.UI.Controls.TextBlock KickerLabel = new();", generatedSource);
    Assert.Contains("KickerLabel.Text = \"HELLO\";", generatedSource);

    using MemoryStream stream = new();
    EmitResult emit = compilation.Emit(stream);
    Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
}

[Fact]
public void DuplicateNameAcrossResourceAndElementReportsDiagnostic()
{
    const string markup = """
        <Resources>
          <SolidColorBrush Name="Duplicate" Color="#FF5D73" />
        </Resources>
        <TextBlock Name="Duplicate" Text="HELLO" />
        """;

    GeneratorRunResult result = RunGenerator("DuplicateName.cui.xml", markup, out _);

    Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI005", "DuplicateName.cui.xml");
    Assert.Contains("Duplicate", diagnostic.GetMessage());
    Assert.Empty(result.GeneratedSources);
}
```

- [ ] **Step 2: Run tests and confirm they fail**

Run:

```powershell
dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj --filter "FullyQualifiedName~ElementNameRegistersGeneratedVariableSymbol|FullyQualifiedName~DuplicateNameAcrossResourceAndElementReportsDiagnostic"
```

Expected: FAIL because `Name` is skipped but not registered, and variables still use `elementN`.

- [ ] **Step 3: Use `Name` for element variables**

In `EmitElement`, replace variable assignment with:

```csharp
string? requestedName = element.Attribute("Name")?.Value;
string variable;
if (string.IsNullOrWhiteSpace(requestedName))
{
    variable = "element" + nextId.ToString(CultureInfo.InvariantCulture);
    nextId++;
}
else
{
    variable = CreateIdentifier(requestedName);
    if (!AddSymbol(requestedName, NamedSymbolKind.Element, variable, element))
    {
        variable = "element" + nextId.ToString(CultureInfo.InvariantCulture);
        nextId++;
    }
}
```

The existing `Lines.Add(typeName + " " + variable + " = new();");` stays unchanged.

- [ ] **Step 4: Run tests and confirm Task 6 passes**

Run:

```powershell
dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj --filter FullyQualifiedName~UiMarkupGeneratorTests
```

Expected: PASS.

- [ ] **Step 5: Re-index and commit**

Run RoslynIndexer, then:

```powershell
git add .\Cerneala.SourceGen\UiMarkupGenerator.cs .\tests\Cerneala.Tests.SourceGen\UiMarkupGeneratorTests.cs
git commit -m "feat: register markup names"
```

---

### Task 7: Validation Coverage For Spec Diagnostics

**Files:**
- Modify: `Cerneala.SourceGen/UiMarkupGenerator.cs`
- Test: `tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorTests.cs`

- [ ] **Step 1: Add failing validation tests**

Add:

```csharp
[Fact]
public void AspectTargetMismatchReportsDiagnostic()
{
    const string markup = """
        <Resources>
          <Aspect Name="KickerText" Target="TextBlock">
            @default
            {
              FontSize = 12;
            }
          </Aspect>
        </Resources>
        <Button Aspect="$KickerText" />
        """;

    GeneratorRunResult result = RunGenerator("AspectMismatch.cui.xml", markup, out _);

    Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI004", "AspectMismatch.cui.xml");
    Assert.Contains("Button.Aspect", diagnostic.GetMessage());
    Assert.Empty(result.GeneratedSources);
}

[Fact]
public void DuplicateUnnamedAspectForTargetReportsDiagnostic()
{
    const string markup = """
        <Resources>
          <Aspect Target="TextBlock">
            @default { FontSize = 12; }
          </Aspect>
          <Aspect Target="TextBlock">
            @default { FontSize = 14; }
          </Aspect>
        </Resources>
        <TextBlock />
        """;

    GeneratorRunResult result = RunGenerator("DuplicateDefaultAspect.cui.xml", markup, out _);

    Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI005", "DuplicateDefaultAspect.cui.xml");
    Assert.Contains("TextBlock", diagnostic.GetMessage());
    Assert.Empty(result.GeneratedSources);
}

[Fact]
public void UnsupportedAspectPropertyReportsDiagnostic()
{
    const string markup = """
        <Resources>
          <Aspect Target="TextBlock">
            @default
            {
              Width = 100;
            }
          </Aspect>
        </Resources>
        <TextBlock />
        """;

    GeneratorRunResult result = RunGenerator("UnsupportedAspectProperty.cui.xml", markup, out _);

    Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI003", "UnsupportedAspectProperty.cui.xml");
    Assert.Contains("TextBlock.Width", diagnostic.GetMessage());
    Assert.Empty(result.GeneratedSources);
}

[Fact]
public void NestedResourcesReportsDiagnostic()
{
    const string markup = """
        <Resources>
          <Resources />
        </Resources>
        <TextBlock />
        """;

    GeneratorRunResult result = RunGenerator("NestedResources.cui.xml", markup, out _);

    Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI005", "NestedResources.cui.xml");
    Assert.Contains("Nested Resources", diagnostic.GetMessage());
    Assert.Empty(result.GeneratedSources);
}
```

- [ ] **Step 2: Run tests and confirm failures identify missing validation**

Run:

```powershell
dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj --filter "FullyQualifiedName~AspectTargetMismatchReportsDiagnostic|FullyQualifiedName~DuplicateUnnamedAspectForTargetReportsDiagnostic|FullyQualifiedName~UnsupportedAspectPropertyReportsDiagnostic|FullyQualifiedName~NestedResourcesReportsDiagnostic"
```

Expected: any missing validation fails. If a test already passes from previous tasks, keep it as coverage.

- [ ] **Step 3: Tighten diagnostics where needed**

Ensure these code paths exist and report the expected descriptor:

```csharp
Report(InvalidDocumentShape, resource, Path.GetFileName(file.Path), "Nested Resources declarations are not supported.");
Report(InvalidDocumentShape, resource, Path.GetFileName(file.Path), "Duplicate unnamed Aspect for target '" + targetName + "'.");
Report(UnsupportedProperty, assignment.Source, elementName, assignment.PropertyName);
Report(InvalidPropertyValue, aspectAttribute, elementName, "Aspect", aspectAttribute.Value);
```

- [ ] **Step 4: Run full source-generator test suite**

Run:

```powershell
dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj
```

Expected: PASS.

- [ ] **Step 5: Re-index and commit**

Run RoslynIndexer, then:

```powershell
git add .\Cerneala.SourceGen\UiMarkupGenerator.cs .\tests\Cerneala.Tests.SourceGen\UiMarkupGeneratorTests.cs
git commit -m "test: cover markup aspect validation"
```

---

### Task 8: Full Integration, File Tree, And Documentation Alignment

**Files:**
- Modify: `FileTree.md`
- Optional modify: `docs/superpowers/specs/2026-07-09-markup-aspect-resources-design.md` only if implementation required a documented behavior correction.
- Test: source-generator project and solution-level build/test smoke.

- [ ] **Step 1: Run focused source-generator tests**

Run:

```powershell
dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj
```

Expected: PASS with 0 failed tests.

- [ ] **Step 2: Run broader tests that compile the main library**

Run:

```powershell
dotnet test .\Cerneala.slnx --filter "FullyQualifiedName~Markup|FullyQualifiedName~Media"
```

Expected: PASS with 0 failed tests. This catches accidental breakage around existing runtime markup and media types.

- [ ] **Step 3: Build the solution**

Run:

```powershell
dotnet build .\Cerneala.slnx
```

Expected: exit code 0.

- [ ] **Step 4: Regenerate `FileTree.md`**

Run:

```powershell
.\Tools\scripts\New-FileTree.ps1
```

Expected: command reports `Wrote C:\Users\Shadow\Desktop\Cerneala\FileTree.md`.

- [ ] **Step 5: Re-index final C# state**

Run RoslynIndexer for `Cerneala.slnx`.

Expected: index succeeds with no errors.

- [ ] **Step 6: Inspect final diff**

Run:

```powershell
git diff --stat
git diff -- .\Cerneala.SourceGen\UiMarkupGenerator.cs .\tests\Cerneala.Tests.SourceGen\UiMarkupGeneratorTests.cs
```

Expected: only source-generator, source-generator tests, `FileTree.md`, and an explicitly justified spec correction appear.

- [ ] **Step 7: Commit integration**

If Step 6 shows only expected files, run:

```powershell
git add .\Cerneala.SourceGen\UiMarkupGenerator.cs .\tests\Cerneala.Tests.SourceGen\UiMarkupGeneratorTests.cs .\FileTree.md
git commit -m "feat: complete markup aspect resource generator"
```

If a spec correction was made, include it explicitly:

```powershell
git add .\docs\superpowers\specs\2026-07-09-markup-aspect-resources-design.md
git commit -m "docs: align markup aspect resource spec"
```

---

## Self-Review Checklist

- [ ] Spec coverage: Resources, `SolidColorBrush`, `Name`, `$Name`, unnamed type aspects, named aspects, cascade, diagnostics, generated direct assignments, and tests are each covered by at least one task.
- [ ] Placeholder scan: this plan contains no unresolved placeholder markers or vague edge-case instructions.
- [ ] Type consistency: `SolidColorBrushResource`, `AspectResource`, `AspectPropertyAssignment`, `PropertySpec`, `MarkupValueKind`, `GeneratedExpression`, and `NamedSymbol` are defined before later tasks use them.
- [ ] Test discipline: each behavior-changing task starts with a failing or guard test and ends with a pass command.
- [ ] Repo discipline: every code/test modification task includes RoslynIndexer re-indexing and a commit step.
