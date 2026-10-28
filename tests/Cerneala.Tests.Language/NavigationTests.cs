using System.ComponentModel;
using Cerneala.Language.Diagnostics;
using Cerneala.Language.Features;
using Cerneala.Language.Semantics;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Text;
using Cerneala.UI.Elements;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using LanguageSourceText = Cerneala.Language.Text.SourceText;

namespace Cerneala.Tests.Language;

public sealed class NavigationTests
{
    private const string FixtureRoot = "C:/cerneala-navigation-fixture";
    private const string MarkupPath = FixtureRoot + "/View.crn";
    private const string CodePath = MarkupPath + ".cs";
    private const string GeneratedPath = FixtureRoot + "/obj/View.g.cs";

    [Fact]
    public void HoverIncludesDocsDefaultsDeclaringTypesAndEmbeddedSymbolKinds()
    {
        const string markup = "<Window DataType=\"Test.ViewModel\"><TextBlock Text=\"$DataContext.Title\" /></Window>";
        using NavigationFixture fixture = NavigationFixture.Create(markup);
        CernealaNavigationService service = new();

        CernealaHoverInfo member = Assert.IsType<CernealaHoverInfo>(
            service.GetHover(fixture.Model, OffsetOf(markup, "Title")));
        Assert.Contains("Title", member.Signature, StringComparison.Ordinal);
        Assert.Equal("Test.ViewModel", member.DeclaringType);
        Assert.Equal("\"Untitled\"", member.DefaultValue);
        Assert.Contains("Visible title", member.Documentation, StringComparison.Ordinal);
        Assert.Equal("NavigationCorpus", member.AssemblyName);

        CernealaHoverInfo element = Assert.IsType<CernealaHoverInfo>(
            service.GetHover(fixture.Model, OffsetOf(markup, "TextBlock")));
        Assert.Equal("Cerneala.UI.Controls.TextBlock", element.Signature);
        Assert.NotNull(element.InheritedFrom);

        const string eventMarkup = "<Window><Button Click=\"OnClick\" /></Window>";
        using NavigationFixture eventFixture = NavigationFixture.Create(eventMarkup);
        CernealaHoverInfo eventHover = Assert.IsType<CernealaHoverInfo>(
            service.GetHover(eventFixture.Model, eventMarkup.IndexOf("Click", StringComparison.Ordinal) + 1));
        Assert.Equal(CernealaSemanticSymbolKind.Event.ToString(), eventHover.Category);
        Assert.Contains("Click", eventHover.Signature, StringComparison.Ordinal);
        Assert.NotNull(eventHover.DeclaringType);

        const string resources = """
            <Window>
              <Window.Resources>
                <SolidColorBrush Name="Accent" />
                <Tween Name="Quick" Duration="100ms" />
                <MotionClip Name="Pulse" TargetType="Button"></MotionClip>
                <Aspect Name="Primary" TargetType="Button"></Aspect>
                <PrismClip Name="Fx">
                  @layer Surface {
                    @parameter Strength: number = 1;
                    Opacity = Strength;
                  }
                </PrismClip>
              </Window.Resources>
            </Window>
            """;
        using NavigationFixture embedded = NavigationFixture.Create(resources);
        CernealaSemanticSymbolKind[] expectedKinds =
        [
            CernealaSemanticSymbolKind.Resource,
            CernealaSemanticSymbolKind.MotionSpec,
            CernealaSemanticSymbolKind.MotionComposition,
            CernealaSemanticSymbolKind.Aspect,
            CernealaSemanticSymbolKind.PrismClip
        ];
        foreach (CernealaSemanticSymbolKind kind in expectedKinds)
        {
            CernealaSemanticSymbol symbol = Assert.Single(embedded.Model.Symbols.Where(candidate => candidate.Kind == kind).Take(1));
            CernealaHoverInfo hover = Assert.IsType<CernealaHoverInfo>(service.GetHover(embedded.Model, symbol.Span.Start));
            Assert.Equal(kind.ToString(), hover.Category);
        }

        const string invalid = "<Window DataType=\"Test.ViewModel\"><TextBlock Text=\"$DataContext.Missing\" /></Window>";
        using NavigationFixture broken = NavigationFixture.Create(invalid);
        LanguageDiagnostic diagnostic = Assert.Single(broken.Model.Diagnostics.Where(candidate => candidate.Id == "CERNEALAUI007"));
        CernealaHoverInfo explanation = Assert.IsType<CernealaHoverInfo>(
            service.GetHover(broken.Model, diagnostic.Span.Start));
        Assert.Contains("typed binding", explanation.DiagnosticExplanation, StringComparison.Ordinal);
        Assert.DoesNotContain(diagnostic.Message, explanation.DiagnosticExplanation, StringComparison.Ordinal);
    }

    [Fact]
    public void DefinitionsPreferUserAuthoredCompanionAndResolveCSharpAndDeclarativeSymbols()
    {
        const string markup = """
            <Window DataType="Test.ViewModel">
              <Window.Resources>
                <SolidColorBrush Name="Accent" />
                <Tween Name="Quick" Duration="100ms" />
                <MotionClip Name="Pulse" TargetType="Button"></MotionClip>
                <Aspect Name="Primary" TargetType="Button">
                  @run $Pulse();
                  @animate with $Quick { @to { Width = 1; } }
                </Aspect>
                <PrismClip Name="Fx">
                  @layer Surface {
                    @parameter Strength: number = 1;
                    Opacity = Strength;
                  }
                </PrismClip>
              </Window.Resources>
              <StackPanel>
                <ItemsControl>
                  @templates
                  {
                    <ContentTemplate DataType="Test.ViewModel">
                      <TextBlock Text="$DataContext.Title" />
                    </ContentTemplate>
                  }
                </ItemsControl>
                <Button Name="Action" Background="$Accent" Aspect="$Primary" />
                <TextBlock Text="$DataContext.Title" />
                <TextBlock Text="$Action.Width" />
              </StackPanel>
            </Window>
            """;
        using NavigationFixture fixture = NavigationFixture.Create(markup);
        CernealaNavigationService service = new();

        CernealaLocation root = Assert.Single(service.GetDefinitions(
            fixture.Model,
            markup.IndexOf("Window", StringComparison.Ordinal) + 1));
        Assert.Equal(Normalize(CodePath), Normalize(root.Path));
        Assert.DoesNotContain(".g.cs", root.Path, StringComparison.OrdinalIgnoreCase);

        CernealaLocation member = Assert.Single(service.GetDefinitions(fixture.Model, OffsetOf(markup, "Title")));
        Assert.Equal(Normalize(CodePath), Normalize(member.Path));

        foreach (string name in new[] { "Accent", "Primary", "Pulse", "Quick" })
        {
            CernealaSemanticSymbol? reference = fixture.Model.Symbols.SingleOrDefault(symbol =>
                symbol.Kind == CernealaSemanticSymbolKind.ResourceReference && symbol.Name == name);
            Assert.True(reference is not null, "Missing resource reference for " + name + ".");
            CernealaLocation definition = Assert.Single(service.GetDefinitions(fixture.Model, reference!.Span.Start));
            Assert.Contains(fixture.Model.Symbols, symbol =>
                symbol.DefinitionLocation is LanguageSourceLocation location &&
                !string.IsNullOrWhiteSpace(location.Path) &&
                Normalize(location.Path) == Normalize(MarkupPath) &&
                location.Span.Equals(definition.Span) &&
                symbol.Span.Equals(definition.Span) &&
                symbol.Kind is CernealaSemanticSymbolKind.Resource or
                    CernealaSemanticSymbolKind.Aspect or
                    CernealaSemanticSymbolKind.MotionSpec or
                    CernealaSemanticSymbolKind.MotionComposition);
        }

        CernealaSemanticSymbol namedReference = Assert.Single(fixture.Model.Symbols.Where(symbol =>
            symbol.Kind == CernealaSemanticSymbolKind.BindingSource && symbol.Name == "Action"));
        CernealaLocation namedDefinition = Assert.Single(service.GetDefinitions(fixture.Model, namedReference.Span.Start));
        Assert.Equal("Action", fixture.Document.Text.Substring(namedDefinition.Span));

        CernealaSemanticSymbol template = Assert.Single(fixture.Model.Symbols.Where(symbol =>
            symbol.Kind == CernealaSemanticSymbolKind.ContentTemplate));
        CernealaLocation templateDefinition = Assert.Single(service.GetDefinitions(fixture.Model, template.Span.Start));
        Assert.Equal("ContentTemplate", fixture.Document.Text.Substring(templateDefinition.Span));

        CernealaSemanticSymbol prism = Assert.Single(fixture.Model.Symbols.Where(symbol =>
            symbol.Kind == CernealaSemanticSymbolKind.PrismClip));
        CernealaLocation prismDefinition = Assert.Single(service.GetDefinitions(fixture.Model, prism.Span.Start));
        Assert.Equal("Fx", fixture.Document.Text.Substring(prismDefinition.Span));

        CernealaSemanticSymbol prismReference = Assert.Single(fixture.Model.Symbols.Where(symbol =>
            symbol.Kind == CernealaSemanticSymbolKind.PrismValue && symbol.Name == "Strength" &&
            symbol.DefinitionLocation is not null));
        CernealaLocation prismParameter = Assert.Single(service.GetDefinitions(fixture.Model, prismReference.Span.Start));
        Assert.Equal("Strength", fixture.Document.Text.Substring(prismParameter.Span));
        Assert.Equal(2, service.GetReferences(
            fixture.Model,
            [fixture.Model],
            prismReference.Span.Start,
            includeDeclaration: true).Count);
    }

    [Fact]
    public void MotionRunResourceReferenceNavigatesAcrossTheWholeDollarReference()
    {
        const string markup = """
            <Window>
              <Window.Resources>
                <MotionClip Name="LoadingSequence" TargetType="Window">
                  @sequence { @set { Opacity = 1; } }
                </MotionClip>
                <Aspect Name="LoadingMotion" TargetType="Window">
                  @handle Loading;
                  @on Loaded { @run $LoadingSequence as Loading; }
                </Aspect>
              </Window.Resources>
            </Window>
            """;
        using NavigationFixture fixture = NavigationFixture.Create(markup);
        CernealaNavigationService service = new();
        int referenceStart = markup.IndexOf("$LoadingSequence", StringComparison.Ordinal);

        for (int offset = referenceStart; offset < referenceStart + "$LoadingSequence".Length; offset++)
        {
            CernealaLocation definition = Assert.Single(service.GetDefinitions(fixture.Model, offset));
            Assert.Equal("LoadingSequence", fixture.Document.Text.Substring(definition.Span));
        }
    }

    [Theory]
    [InlineData("<Window><Window.Resources><SolidColorBrush Name=\"Primary\" /></Window.Resources><Button Background=\"$Primary\" /></Window>")]
    [InlineData("<Window><Window.Resources><Aspect Name=\"Primary\" TargetType=\"Button\"></Aspect></Window.Resources><Button Aspect=\"$Primary\" /></Window>")]
    [InlineData("<Window><Window.Resources><PrismClip Name=\"Primary\">@layer Foreground { @filter Blur { Radius = 4; } }</PrismClip></Window.Resources><Border><Border.Aspect>@prism $Primary;</Border.Aspect></Border></Window>")]
    public void ReferencesOfAResourceIncludeTheDeclarationAndTheDollarUse(string markup)
    {
        using NavigationFixture fixture = NavigationFixture.Create(markup);
        CernealaNavigationService service = new();
        int declaration = markup.IndexOf("Name=\"Primary\"", StringComparison.Ordinal) + "Name=\"".Length + 1;

        IReadOnlyList<CernealaLocation> references = service.GetReferences(fixture.Model, [fixture.Model], declaration, includeDeclaration: true);

        Assert.Equal(2, references.Count);
        Assert.All(references, location => Assert.Equal("Primary", fixture.Document.Text.Substring(location.Span)));
        Assert.Equal(2, service.GetDocumentHighlights(fixture.Model, [fixture.Model], declaration).Count);
    }

    [Fact]
    public void PrismClipResourceIsRenamedWithItsPrismApplicationReferences()
    {
        const string markup = "<Window><Window.Resources><PrismClip Name=\"Primary\">@layer Foreground { @filter Blur { Radius = 4; } }</PrismClip></Window.Resources><Border><Border.Aspect>@prism $Primary;</Border.Aspect></Border></Window>";
        using NavigationFixture fixture = NavigationFixture.Create(markup);
        CernealaNavigationService service = new();
        int declaration = markup.IndexOf("Name=\"Primary\"", StringComparison.Ordinal) + "Name=\"".Length + 1;
        int application = markup.IndexOf("$Primary", StringComparison.Ordinal);

        CernealaPrepareRenameResult prepared = service.PrepareRename(fixture.Model, [fixture.Model], application);
        CernealaRenameResult rename = service.Rename(fixture.Model, [fixture.Model], declaration, "Secondary");

        Assert.Equal("Primary", prepared.Placeholder);
        Assert.True(rename.Succeeded, rename.Error);
        Assert.Equal(2, rename.Edits.Count);
        string renamed = Apply(markup, rename.Edits);
        Assert.Contains("Name=\"Secondary\"", renamed, StringComparison.Ordinal);
        Assert.Contains("@prism $Secondary;", renamed, StringComparison.Ordinal);
        Assert.DoesNotContain("Primary", renamed, StringComparison.Ordinal);
    }

    [Fact]
    public void ReferencesHighlightsAndRenameRespectShadowingAndLeaveArbitraryTextAlone()
    {
        const string markup = """
            <Window>
              <Window.Resources>
                <SolidColorBrush Name="Accent" />
              </Window.Resources>
              <StackPanel>
                <StackPanel.Resources>
                  <SolidColorBrush Name="Accent" />
                </StackPanel.Resources>
                <Button Background="$Accent" />
              </StackPanel>
              <TextBlock Text="Accent" />
              <Button Background="$Accent" />
            </Window>
            """;
        using NavigationFixture fixture = NavigationFixture.Create(markup);
        CernealaNavigationService service = new();
        CernealaSemanticSymbol outerReference = fixture.Model.Symbols
            .Where(symbol => symbol.Kind == CernealaSemanticSymbolKind.ResourceReference && symbol.Name == "Accent")
            .OrderBy(symbol => symbol.Span.Start)
            .Last();

        IReadOnlyList<CernealaLocation> references = service.GetReferences(
            fixture.Model,
            [fixture.Model],
            outerReference.Span.Start,
            includeDeclaration: true);
        Assert.True(
            references.Count == 2,
            string.Join(" | ", references.Select(location =>
                location.Span + "=" + fixture.Document.Text.Substring(location.Span))));
        Assert.DoesNotContain(references, location => location.Span.Start == markup.IndexOf("Name=\"Accent\"", markup.IndexOf("StackPanel.Resources", StringComparison.Ordinal), StringComparison.Ordinal) + "Name=\"".Length);

        IReadOnlyList<CernealaDocumentHighlight> highlights = service.GetDocumentHighlights(
            fixture.Model,
            [fixture.Model],
            outerReference.Span.Start);
        Assert.Equal(2, highlights.Count);
        Assert.Contains(highlights, highlight => highlight.Kind == CernealaDocumentHighlightKind.Read);

        CernealaRenameResult rename = service.Rename(
            fixture.Model,
            [fixture.Model],
            outerReference.Span.Start,
            "GlobalAccent");
        Assert.True(rename.Succeeded, rename.Error);
        Assert.Equal(2, rename.Edits.Count);
        string updated = Apply(markup, rename.Edits.Where(edit => Normalize(edit.Path) == Normalize(MarkupPath)));
        Assert.Contains("Text=\"Accent\"", updated, StringComparison.Ordinal);
        Assert.Contains("StackPanel.Resources", updated, StringComparison.Ordinal);
        Assert.Equal(2, Count(updated, "GlobalAccent"));
        Assert.Equal(1, Count(updated, "Name=\"Accent\""));
        Assert.Equal(1, Count(updated, "\"$Accent\""));

        using NavigationFixture rebound = NavigationFixture.Create(updated);
        Assert.DoesNotContain(rebound.Model.Diagnostics, diagnostic =>
            diagnostic.Id == "CERNEALAUI004" && diagnostic.Message.Contains("GlobalAccent", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("<Window><Window.Resources><SolidColorBrush Name=\"Primary\" /></Window.Resources><Button Background=\"$Primary\" /></Window>")]
    [InlineData("<Window><Window.Resources><Aspect Name=\"Primary\" TargetType=\"Button\"></Aspect></Window.Resources><Button Aspect=\"$Primary\" /></Window>")]
    [InlineData("<Window><Window.Resources><Aspect Name=\"Primary\" TargetType=\"Button\">@default { Opacity = 0.5; }</Aspect></Window.Resources><Button Aspect=\"$Primary\" /></Window>")]
    [InlineData("<Window><Window.Resources><Aspect Name=\"Primary\" TargetType=\"Button\">@template { <TextBlock Text=\"Named\" /> }</Aspect></Window.Resources><Button Aspect=\"$Primary\" /></Window>")]
    public void ResourceDeclaredByNameIsRenamedWithItsDollarReferences(string markup)
    {
        using NavigationFixture fixture = NavigationFixture.Create(markup);
        CernealaNavigationService service = new();
        int declaration = markup.IndexOf("Name=\"Primary\"", StringComparison.Ordinal) + "Name=\"".Length + 1;

        CernealaRenameResult rename = service.Rename(fixture.Model, [fixture.Model], declaration, "Secondary");

        Assert.True(rename.Succeeded, rename.Error);
        Assert.Equal(2, rename.Edits.Count);
        string renamed = Apply(markup, rename.Edits);
        Assert.Contains("Name=\"Secondary\"", renamed, StringComparison.Ordinal);
        Assert.Contains("\"$Secondary\"", renamed, StringComparison.Ordinal);
        Assert.DoesNotContain("Primary", renamed, StringComparison.Ordinal);
        Assert.Equal(2, service.GetReferences(fixture.Model, [fixture.Model], declaration, includeDeclaration: true).Count);
        Assert.Equal(2, service.GetDocumentHighlights(fixture.Model, [fixture.Model], declaration).Count);
    }

    [Theory]
    [InlineData("<Window><Window.Resources><Aspect TargetType=\"Button\">@default { Opacity = 0.5; }</Aspect></Window.Resources><Button /><Button /></Window>", "<Bu", "Button@110,Button@120")]
    [InlineData("<Window><Window.Resources><Aspect Name=\"@x\" TargetType=\"Button\"></Aspect></Window.Resources><Button Aspect=\"$@x\" /></Window>", "Name=\"@x", "@x@40,@x@109")]
    public void AspectReferencesKeepTheDefaultAspectApplicationsAndTheDeclaration(string markup, string anchor, string expected)
    {
        using NavigationFixture fixture = NavigationFixture.Create(markup);
        CernealaNavigationService service = new();
        int offset = markup.LastIndexOf(anchor, StringComparison.Ordinal) + anchor.Length - 1;

        IReadOnlyList<CernealaLocation> references = service.GetReferences(fixture.Model, [fixture.Model], offset, includeDeclaration: true);

        Assert.Equal(expected, string.Join(",", references.Select(location =>
            markup.Substring(location.Span.Start, location.Span.Length) + "@" + location.Span.Start)));
        Assert.Equal(references.Count, service.GetDocumentHighlights(fixture.Model, [fixture.Model], offset).Count);
    }

    [Theory]
    [InlineData("<Window><Window.Resources><Aspect Name=\"Aspect\" TargetType=\"Button\"></Aspect></Window.Resources><Button Aspect=\"$Aspect\" /></Window>", "Aspect", "<Window><Window.Resources><Aspect Name=\"Secondary\" TargetType=\"Button\"></Aspect></Window.Resources><Button Aspect=\"$Secondary\" /></Window>")]
    [InlineData("<Window><Window.Resources><Aspect Name=\"@x\" TargetType=\"Button\"></Aspect></Window.Resources><Button Aspect=\"$@x\" /></Window>", "@x", "<Window><Window.Resources><Aspect Name=\"Secondary\" TargetType=\"Button\"></Aspect></Window.Resources><Button Aspect=\"$Secondary\" /></Window>")]
    public void AspectRenameEditsOnlyTheNameAndItsDollarReference(string markup, string name, string expected)
    {
        using NavigationFixture fixture = NavigationFixture.Create(markup);
        CernealaNavigationService service = new();
        int declaration = markup.IndexOf("Name=\"" + name + "\"", StringComparison.Ordinal) + "Name=\"".Length + 1;

        CernealaRenameResult rename = service.Rename(fixture.Model, [fixture.Model], declaration, "Secondary");

        Assert.True(rename.Succeeded, rename.Error);
        Assert.Equal(expected, Apply(markup, rename.Edits));
    }

    [Fact]
    public void RenameRejectsDuplicateScopesWhileNavigationSurvivesAnUnrelatedInvalidNode()
    {
        const string duplicate = "<Window><StackPanel><Button Name=\"Same\" /><Button Name=\"Same\" /><TextBlock Text=\"$Same.Width\" /></StackPanel></Window>";
        using NavigationFixture duplicateFixture = NavigationFixture.Create(duplicate);
        CernealaNavigationService service = new();
        CernealaSemanticSymbol declaration = Assert.Single(duplicateFixture.Model.Symbols.Where(symbol =>
            symbol.Kind == CernealaSemanticSymbolKind.Name && symbol.Name == "Same"));
        CernealaRenameResult rejected = service.Rename(
            duplicateFixture.Model,
            [duplicateFixture.Model],
            declaration.Span.Start,
            "Renamed");
        Assert.False(rejected.Succeeded);
        Assert.Contains("duplicates", rejected.Error, StringComparison.OrdinalIgnoreCase);

        const string root = "<Window />";
        using NavigationFixture rootFixture = NavigationFixture.Create(root);
        CernealaRenameResult mismatchedRoot = service.Rename(
            rootFixture.Model,
            [rootFixture.Model],
            OffsetOf(root, "Window"),
            "RenamedView");
        Assert.False(mismatchedRoot.Succeeded);
        Assert.Contains("map exactly", mismatchedRoot.Error, StringComparison.OrdinalIgnoreCase);

        const string partial = "<Window><StackPanel><Button Name=\"Action\" /><TextBlock Text=\"$Action.Width\" /><Button Bogus=\"1\" /></StackPanel></Window>";
        using NavigationFixture partialFixture = NavigationFixture.Create(partial);
        CernealaSemanticSymbol reference = Assert.Single(partialFixture.Model.Symbols.Where(symbol =>
            symbol.Kind == CernealaSemanticSymbolKind.BindingSource && symbol.Name == "Action"));
        CernealaLocation definition = Assert.Single(service.GetDefinitions(partialFixture.Model, reference.Span.Start));
        Assert.Equal("Action", partialFixture.Document.Text.Substring(definition.Span));
        Assert.Contains(partialFixture.Model.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI003");
    }

    [Fact]
    public void CSharpReferencesAndRenameStaySemanticAcrossProjectCompilationsAndRemainCompilable()
    {
        const string alphaPath = FixtureRoot + "/Alpha.cs";
        const string betaPath = FixtureRoot + "/Beta.cs";
        const string alphaMarkupPath = FixtureRoot + "/Alpha.crn";
        const string betaMarkupPath = FixtureRoot + "/Beta.crn";
        const string alphaCode = """
            using System.ComponentModel;
            namespace Shared;
            public sealed class ViewModel : INotifyPropertyChanged
            {
                public string Title { get; set; } = string.Empty;
                public event PropertyChangedEventHandler? PropertyChanged;
            }
            """;
        const string betaCode = """
            namespace Consumer;
            public sealed class Reader
            {
                public string Read(Shared.ViewModel value) => value.Title;
                public string TitleText => "Title";
            }
            """;
        const string alphaMarkup = "<Window DataType=\"Shared.ViewModel\"><TextBlock Text=\"$DataContext.Title\" /></Window>";
        const string betaMarkup = "<Window DataType=\"Shared.ViewModel\"><TextBlock Text=\"$DataContext.Title\" /></Window>";

        MetadataReference[] baseReferences = CreateReferences();
        CSharpCompilation alphaProject = CreateCompilation("Alpha", alphaCode, alphaPath, baseReferences);
        MetadataReference alphaReference = EmitReference(alphaProject);
        CSharpCompilation betaProject = CreateCompilation(
            "Beta",
            betaCode,
            betaPath,
            baseReferences.Append(alphaReference));
        CernealaDocument alphaDocument = CreateDocument(alphaMarkupPath, alphaMarkup);
        CernealaDocument betaDocument = CreateDocument(betaMarkupPath, betaMarkup);
        using CernealaCompilation alphaLanguage = new(new RoslynCompilationSymbols(alphaProject), [alphaDocument]);
        using CernealaCompilation betaLanguage = new(new RoslynCompilationSymbols(betaProject), [betaDocument]);
        CernealaSemanticModel alphaModel = alphaLanguage.GetSemanticModel(alphaMarkupPath);
        CernealaSemanticModel betaModel = betaLanguage.GetSemanticModel(betaMarkupPath);
        CernealaNavigationService service = new();
        int queryOffset = OffsetOf(alphaMarkup, "Title");

        IReadOnlyList<CernealaLocation> references = service.GetReferences(
            alphaModel,
            [alphaModel, betaModel],
            queryOffset,
            includeDeclaration: true);
        Assert.Contains(references, location => Normalize(location.Path) == Normalize(alphaPath));
        Assert.Contains(references, location => Normalize(location.Path) == Normalize(betaPath));
        Assert.Contains(references, location => Normalize(location.Path) == Normalize(alphaMarkupPath));
        Assert.Contains(references, location => Normalize(location.Path) == Normalize(betaMarkupPath));

        CernealaRenameResult rename = service.Rename(
            alphaModel,
            [alphaModel, betaModel],
            queryOffset,
            "Heading");
        Assert.True(rename.Succeeded, rename.Error);
        Assert.Equal(4, rename.Edits.Count);
        string renamedAlphaCode = Apply(alphaCode, rename.Edits.Where(edit => Normalize(edit.Path) == Normalize(alphaPath)));
        string renamedBetaCode = Apply(betaCode, rename.Edits.Where(edit => Normalize(edit.Path) == Normalize(betaPath)));
        string renamedAlphaMarkup = Apply(alphaMarkup, rename.Edits.Where(edit => Normalize(edit.Path) == Normalize(alphaMarkupPath)));
        string renamedBetaMarkup = Apply(betaMarkup, rename.Edits.Where(edit => Normalize(edit.Path) == Normalize(betaMarkupPath)));
        Assert.Contains("TitleText", renamedBetaCode, StringComparison.Ordinal);
        Assert.Contains("\"Title\"", renamedBetaCode, StringComparison.Ordinal);

        CSharpCompilation renamedAlpha = CreateCompilation("Alpha", renamedAlphaCode, alphaPath, baseReferences);
        Assert.DoesNotContain(renamedAlpha.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        CSharpCompilation renamedBeta = CreateCompilation(
            "Beta",
            renamedBetaCode,
            betaPath,
            baseReferences.Append(EmitReference(renamedAlpha)));
        Assert.DoesNotContain(renamedBeta.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using CernealaCompilation renamedAlphaLanguage = new(
            new RoslynCompilationSymbols(renamedAlpha),
            [CreateDocument(alphaMarkupPath, renamedAlphaMarkup)]);
        using CernealaCompilation renamedBetaLanguage = new(
            new RoslynCompilationSymbols(renamedBeta),
            [CreateDocument(betaMarkupPath, renamedBetaMarkup)]);
        Assert.DoesNotContain(renamedAlphaLanguage.GetSemanticModel(alphaMarkupPath).Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI007");
        Assert.DoesNotContain(renamedBetaLanguage.GetSemanticModel(betaMarkupPath).Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI007");
    }

    // Issue #960: the closing tag </t:Card> names the same type as the opening tag, so rename edits both tags.
    [Theory]
    [InlineData("<Window xmlns:t=\"clr-namespace:Shared;assembly=Alpha\"><t:Card /></Window>")]
    [InlineData("<Window xmlns:t=\"clr-namespace:Shared;assembly=Alpha\"><t:Card></t:Card></Window>")]
    public void RenamingAProjectTypeRenamesTheClosingTagToo(string markup)
    {
        const string alphaPath = FixtureRoot + "/Alpha.cs";
        const string markupPath = FixtureRoot + "/Alpha.crn";
        const string code = "namespace Shared; public sealed class Card : Cerneala.UI.Controls.Border { }";
        CSharpCompilation project = CreateCompilation("Alpha", code, alphaPath, CreateReferences());
        CernealaDocument document = CreateDocument(markupPath, markup);
        using CernealaCompilation language = new(new RoslynCompilationSymbols(project), [document]);
        CernealaSemanticModel model = language.GetSemanticModel(markupPath);
        CernealaNavigationService service = new();

        CernealaRenameResult rename = service.Rename(model, [model], markup.IndexOf("Card", StringComparison.Ordinal) + 1, "Tile");
        Assert.True(rename.Succeeded, rename.Error);
        string renamedMarkup = Apply(markup, rename.Edits.Where(edit => Normalize(edit.Path) == Normalize(markupPath)));
        string renamedCode = Apply(code, rename.Edits.Where(edit => Normalize(edit.Path) == Normalize(alphaPath)));

        string expected = markup.Replace("Card", "Tile", StringComparison.Ordinal);
        Assert.True(
            renamedMarkup == expected && renamedCode.Contains("class Tile", StringComparison.Ordinal),
            $"Renamed markup: {renamedMarkup} (expected {expected}); edits: {rename.Edits.Count}.");
    }

    // Issue #960: <t:Card.Title>x</t:Card.Title> names the member Title in both tags.
    [Theory]
    [InlineData("<Window xmlns:t=\"clr-namespace:Shared;assembly=Alpha\"><t:Card Title=\"x\" /></Window>")]
    [InlineData("<Window xmlns:t=\"clr-namespace:Shared;assembly=Alpha\"><t:Card><t:Card.Title>x</t:Card.Title></t:Card></Window>")]
    public void RenamingAProjectMemberRenamesTheClosingTagOfItsPropertyElementToo(string markup)
    {
        const string alphaPath = FixtureRoot + "/Alpha.cs";
        const string markupPath = FixtureRoot + "/Alpha.crn";
        const string code = "namespace Shared; public sealed class Card : Cerneala.UI.Controls.Border { public string Title { get; set; } = \"\"; }";
        CSharpCompilation project = CreateCompilation("Alpha", code, alphaPath, CreateReferences());
        CernealaDocument document = CreateDocument(markupPath, markup);
        using CernealaCompilation language = new(new RoslynCompilationSymbols(project), [document]);
        CernealaSemanticModel model = language.GetSemanticModel(markupPath);
        CernealaNavigationService service = new();

        CernealaRenameResult rename = service.Rename(model, [model], markup.IndexOf("Title", StringComparison.Ordinal) + 1, "Heading");
        Assert.True(rename.Succeeded, rename.Error);
        string renamedMarkup = Apply(markup, rename.Edits.Where(edit => Normalize(edit.Path) == Normalize(markupPath)));
        string renamedCode = Apply(code, rename.Edits.Where(edit => Normalize(edit.Path) == Normalize(alphaPath)));

        string expected = markup.Replace("Title", "Heading", StringComparison.Ordinal);
        Assert.True(
            renamedMarkup == expected && renamedCode.Contains("string Heading", StringComparison.Ordinal),
            $"Renamed markup: {renamedMarkup} (expected {expected}); edits: {rename.Edits.Count}.");
    }

    // Issue #960: references and document highlights list the closing tag like rename edits it.
    [Fact]
    public void ReferencesAndHighlightsOfAnElementTypeIncludeItsClosingTag()
    {
        const string alphaPath = FixtureRoot + "/Alpha.cs";
        const string markupPath = FixtureRoot + "/Alpha.crn";
        const string code = "namespace Shared; public sealed class Card : Cerneala.UI.Controls.Border { }";
        const string markup = "<Window xmlns:t=\"clr-namespace:Shared;assembly=Alpha\"><t:Card></t:Card></Window>";
        CSharpCompilation project = CreateCompilation("Alpha", code, alphaPath, CreateReferences());
        CernealaDocument document = CreateDocument(markupPath, markup);
        using CernealaCompilation language = new(new RoslynCompilationSymbols(project), [document]);
        CernealaSemanticModel model = language.GetSemanticModel(markupPath);
        CernealaNavigationService service = new();
        int offset = markup.IndexOf("Card", StringComparison.Ordinal) + 1;
        TextSpan opening = new(markup.IndexOf("Card", StringComparison.Ordinal), "Card".Length);
        TextSpan closing = new(markup.LastIndexOf("Card", StringComparison.Ordinal), "Card".Length);

        TextSpan[] references = service.GetReferences(model, [model], offset, includeDeclaration: true)
            .Where(location => Normalize(location.Path) == Normalize(markupPath))
            .Select(location => location.Span)
            .ToArray();
        TextSpan[] highlights = service.GetDocumentHighlights(model, [model], offset)
            .Select(highlight => highlight.Span)
            .ToArray();

        Assert.Equal([opening, closing], references);
        Assert.Equal([opening, closing], highlights);
    }

    // Issue #825: a framework type or member is declared in metadata, so its declaration cannot be renamed.
    [Theory]
    [InlineData("<Window><Button Content=\"x\" /></Window>", "Button")]
    [InlineData("<Window><Border Width=\"5\" /></Window>", "Width")]
    [InlineData("<Window><Button Content=\"x\" /></Window>", "Content")]
    public void PrepareRenameAndRenameRejectFrameworkTypesAndMembers(string markup, string token)
    {
        using NavigationFixture fixture = NavigationFixture.Create(markup);
        CernealaNavigationService service = new();

        CernealaPrepareRenameResult prepared = service.PrepareRename(fixture.Model, [fixture.Model], OffsetOf(markup, token));

        Assert.False(prepared.CanRename, "PrepareRename offered a range for the framework symbol '" + token + "'.");
    }

    [Theory]
    [InlineData("<Window><Button Content=\"x\" /></Window>", "Button")]
    [InlineData("<Window><Border Width=\"5\" /></Window>", "Width")]
    [InlineData("<Window><Button Content=\"x\" /></Window>", "Content")]
    public void RenameRejectsFrameworkTypesAndMembers(string markup, string token)
    {
        using NavigationFixture fixture = NavigationFixture.Create(markup);
        CernealaNavigationService service = new();

        CernealaRenameResult rename = service.Rename(fixture.Model, [fixture.Model], OffsetOf(markup, token), "Renamed" + token);

        Assert.False(rename.Succeeded, "Rename produced " + rename.Edits.Count + " edit(s) for the framework symbol '" + token + "'.");
    }

    [Theory]
    [InlineData("<Window><Window.Resources><SolidColorBrush Name=\"Accent\" /></Window.Resources><TextBlock Text=\"$Accent\" /></Window>", "Accent")]
    [InlineData("<Window DataType=\"Test.ViewModel\"><TextBlock Text=\"$DataContext.Title\" /></Window>", "Title")]
    public void PrepareRenameAcceptsDeclarationsOfTheProject(string markup, string token)
    {
        using NavigationFixture fixture = NavigationFixture.Create(markup);
        CernealaNavigationService service = new();

        CernealaPrepareRenameResult prepared = service.PrepareRename(fixture.Model, [fixture.Model], OffsetOf(markup, token));

        Assert.True(prepared.CanRename, prepared.Error);
    }

    // Rename must not rewrite C# references of a framework type; the control renames a project type and must edit its
    // C# references.
    [Theory]
    [InlineData("<Window><Button Content=\"x\" /></Window>", "Button", true)]
    [InlineData("<Window xmlns:t=\"clr-namespace:Shared;assembly=Alpha\"><t:Card /></Window>", "Card", false)]
    public void RenameDoesNotRewriteCSharpReferencesOfAFrameworkType(string markup, string token, bool framework)
    {
        const string alphaPath = FixtureRoot + "/Alpha.cs";
        const string markupPath = FixtureRoot + "/Alpha.crn";
        const string code = "namespace Shared; public sealed class Card : Cerneala.UI.Controls.Border { } " +
            "public sealed class User { public Cerneala.UI.Controls.Button Field = null!; public Card Other = null!; }";
        CSharpCompilation project = CreateCompilation("Alpha", code, alphaPath, CreateReferences());
        CernealaDocument document = CreateDocument(markupPath, markup);
        using CernealaCompilation language = new(new RoslynCompilationSymbols(project), [document]);
        CernealaSemanticModel model = language.GetSemanticModel(markupPath);
        CernealaNavigationService service = new();

        CernealaRenameResult rename = service.Rename(model, [model], OffsetOf(markup, token), "Renamed" + token);

        int codeEdits = rename.Edits.Count(edit => Normalize(edit.Path) == Normalize(alphaPath));
        Assert.True(
            framework ? codeEdits == 0 : codeEdits > 0,
            $"token={token}: succeeded={rename.Succeeded}, {rename.Edits.Count} edit(s) in total, {codeEdits} in the C# file ({string.Join(", ", rename.Edits.Select(edit => Normalize(edit.Path) + "@" + edit.Span.Start))}).");
    }

    private static int OffsetOf(string text, string value) =>
        text.LastIndexOf(value, StringComparison.Ordinal) + Math.Min(1, value.Length - 1);

    private static string Apply(string source, IEnumerable<CernealaTextEdit> edits)
    {
        foreach (CernealaTextEdit edit in edits.OrderByDescending(candidate => candidate.Span.Start))
        {
            source = source.Substring(0, edit.Span.Start) + edit.NewText + source.Substring(edit.Span.End);
        }

        return source;
    }

    private static int Count(string text, string value)
    {
        int count = 0;
        int offset = 0;
        while ((offset = text.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }

        return count;
    }

    private static string Normalize(string path) => path.Replace('\\', '/');

    private static CernealaDocument CreateDocument(string path, string text) =>
        new(path, LanguageSourceText.From(text));

    private static CSharpCompilation CreateCompilation(
        string assemblyName,
        string code,
        string path,
        IEnumerable<MetadataReference> references) => CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(
                code,
                CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest)
                    .WithDocumentationMode(DocumentationMode.Parse),
                path)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

    private static MetadataReference EmitReference(CSharpCompilation compilation)
    {
        using MemoryStream stream = new();
        Microsoft.CodeAnalysis.Emit.EmitResult result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    private static MetadataReference[] CreateReferences()
    {
        string trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("Trusted platform assemblies are unavailable.");
        return trustedAssemblies
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(UIElement).Assembly.Location))
            .ToArray();
    }

    private sealed class NavigationFixture : IDisposable
    {
        private const string Code = """
            using System;
            using System.ComponentModel;
            using Cerneala.UI.Controls;

            namespace Test
            {
                public sealed partial class View : Window
                {
                }

                public sealed class ViewModel : INotifyPropertyChanged
                {
                    /// <summary>Visible title used by the view.</summary>
                    [DefaultValue("Untitled")]
                    public string Title { get; set; } = string.Empty;
                    public event PropertyChangedEventHandler? PropertyChanged;
                }
            }
            """;

        private const string Generated = """
            namespace Test
            {
                public sealed partial class View
                {
                }
            }
            """;

        private readonly CernealaCompilation compilation;

        private NavigationFixture(CernealaDocument document, CernealaCompilation compilation)
        {
            Document = document;
            this.compilation = compilation;
            Model = compilation.GetSemanticModel(document.Path);
        }

        public CernealaDocument Document { get; }

        public CernealaSemanticModel Model { get; }

        public static NavigationFixture Create(string markup)
        {
            MetadataReference[] references = CreateReferences();
            SyntaxTree user = CSharpSyntaxTree.ParseText(
                Code,
                CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest)
                    .WithDocumentationMode(DocumentationMode.Parse),
                CodePath);
            SyntaxTree generated = CSharpSyntaxTree.ParseText(
                Generated,
                CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest),
                GeneratedPath);
            CSharpCompilation project = CSharpCompilation.Create(
                "NavigationCorpus",
                [user, generated],
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithNullableContextOptions(NullableContextOptions.Enable));
            CernealaDocument document = CreateDocument(MarkupPath, markup);
            CernealaCompilation compilation = new(new RoslynCompilationSymbols(project), [document]);
            return new NavigationFixture(document, compilation);
        }

        public void Dispose() => compilation.Dispose();
    }
}
