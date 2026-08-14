using System.Diagnostics;
using System.Xml.Linq;
using Cerneala.Language.Features;
using Cerneala.Language.Semantics;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Text;
using Cerneala.UI.Elements;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using LanguageSourceText = Cerneala.Language.Text.SourceText;

namespace Cerneala.Tests.Language;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class LanguagePerformanceCollection
{
    public const string Name = "Language performance";
}

[Collection(LanguagePerformanceCollection.Name)]
public sealed class CompletionTests
{
    private const string Caret = "|caret|";
    private static readonly CSharpCompilation Project = CreateProject();
    private static readonly RoslynCompilationSymbols ProjectSymbols = new(Project);

    [Theory]
    [InlineData("<Wi|caret| />", "Window", null)]
    [InlineData("<Window><StackPanel><Bu|caret| /></StackPanel></Window>", "Button", "Application")]
    [InlineData("<Window xmlns:custom=\"clr-namespace:Custom;assembly=CompletionCorpus\"><custom:Fan|caret| /></Window>", "custom:FancyControl", null)]
    [InlineData("<Window><Button.Con|caret| /></Window>", "Button.Content", null)]
    public void ElementAndPropertyElementCompletionProducesValidMarkup(
        string markup,
        string expected,
        string? impossible)
    {
        using CompletionFixture fixture = CompletionFixture.Create(markup, semantic: expected != "Window");

        CernealaCompletionItem item = Assert.Single(
            fixture.Complete().Where(candidate => candidate.Label == expected));
        AssertValidAfterInsertion(fixture.Text, item);
        if (impossible is not null)
        {
            Assert.DoesNotContain(fixture.Complete(), candidate => candidate.Label == impossible);
        }
    }

    [Fact]
    public void AttributesIncludePropertiesEventsAndCompatibleAttachedPropertiesWithoutDuplicates()
    {
        using CompletionFixture fixture = CompletionFixture.Create(
            "<Window><Button Width=\"1\" |caret| /></Window>");

        IReadOnlyList<CernealaCompletionItem> items = fixture.Complete();

        Assert.Contains(items, item => item.Label == "Height");
        Assert.Contains(items, item => item.Label == "Click" && item.Kind == CernealaCompletionItemKind.Event);
        Assert.Contains(items, item => item.Label == "Grid.Row");
        Assert.DoesNotContain(items, item => item.Label == "Width");
        Assert.Equal(items.Count, items.Select(item => item.Label).Distinct(StringComparer.Ordinal).Count());
        AssertValidAfterInsertion(fixture.Text, Assert.Single(items.Where(item => item.Label == "Height")));
    }

    [Theory]
    [InlineData("<Window><Button IsEnabled=\"|caret|\" /></Window>", "true")]
    [InlineData("<Window><Button HorizontalAlignment=\"|caret|\" /></Window>", "Center")]
    [InlineData("<Window><Button Margin=\"|caret|\" /></Window>", "8,4,8,4")]
    [InlineData("<Window><Button Background=\"|caret|\" /></Window>", "#FFFFFFFF")]
    [InlineData("<Window><Window.Resources><Tween Name=\"Quick\" Duration=\"|caret|\" /></Window.Resources></Window>", "250ms")]
    public void LiteralCompletionMatchesBuildConversions(string markup, string expected)
    {
        using CompletionFixture fixture = CompletionFixture.Create(markup);

        CernealaCompletionItem item = Assert.Single(
            fixture.Complete().Where(candidate => candidate.Label == expected));

        AssertValidAfterInsertion(fixture.Text, item);
    }

    [Fact]
    public void TypeAndNamespaceCompletionUsesAliasesAndAssignableTargetTypes()
    {
        using CompletionFixture dataType = CompletionFixture.Create(
            "<Window xmlns:test=\"clr-namespace:Test;assembly=CompletionCorpus\" DataType=\"test:|caret|\" />");
        using CompletionFixture targetType = CompletionFixture.Create(
            "<Window><Window.Resources><Aspect TargetType=\"|caret|\" /></Window.Resources></Window>");
        using CompletionFixture xmlns = CompletionFixture.Create(
            "<Window xmlns:custom=\"|caret|\" />");

        Assert.Contains(dataType.Complete(), item => item.Label == "test:ViewModel");
        Assert.Contains(targetType.Complete(), item => item.Label == "Button");
        Assert.DoesNotContain(targetType.Complete(), item => item.Detail == "System.String");
        Assert.Contains(xmlns.Complete(), item => item.InsertText.Contains("clr-namespace:Custom", StringComparison.Ordinal));
    }

    [Fact]
    public void ScopedResourcesNamesAspectsMotionAndPrismAreContextual()
    {
        const string sourceMarkup = """
            <Window>
              <Window.Resources>
                <SolidColorBrush Name="Accent" />
                <Tween Name="Quick" Duration="100ms" />
                <Aspect Name="Primary" TargetType="Button" />
              </Window.Resources>
              <StackPanel>
                <Button Name="Action" Aspect="$Primary" />
                <TextBlock Text="$|caret|" />
              </StackPanel>
            </Window>
            """;
        using CompletionFixture sources = CompletionFixture.Create(sourceMarkup);
        using CompletionFixture aspect = CompletionFixture.Create(sourceMarkup.Replace(
            "Aspect=\"$Primary\"",
            "Aspect=\"$|caret|\"",
            StringComparison.Ordinal).Replace("Text=\"$|caret|\"", "Text=\"ok\"", StringComparison.Ordinal));
        using CompletionFixture motion = CompletionFixture.Create(
            "<Window><Window.Resources><Tween Name=\"Quick\" Duration=\"100ms\" /></Window.Resources><Button Tag=\"$|caret|\" /></Window>");
        using CompletionFixture prism = CompletionFixture.Create(
            "<Window><Window.Resources><PrismComposition Name=\"Fx\">@filter |caret|</PrismComposition></Window.Resources></Window>");

        IReadOnlyList<CernealaCompletionItem> sourceItems = sources.Complete();
        Assert.Contains(sourceItems, item => item.Label == "$Accent");
        Assert.Contains(sourceItems, item => item.Label == "$Action");
        Assert.Contains(aspect.Complete(), item => item.Label == "$Primary");
        Assert.Contains(motion.Complete(), item => item.Label == "$Quick");
        Assert.Contains(prism.Complete(), item => item.Kind == CernealaCompletionItemKind.Function);
    }

    [Fact]
    public void TypedBindingsFollowLocalDataContextAndOfferOnlyLegalModes()
    {
        const string markup = """
            <Window DataType="Test.ViewModel">
              <StackPanel DataContext="$DataContext.Person">
                <TextBlock Text="$DataContext.|caret|" />
              </StackPanel>
            </Window>
            """;
        using CompletionFixture local = CompletionFixture.Create(markup);
        using CompletionFixture writableMode = CompletionFixture.Create(markup.Replace(
            "$DataContext.|caret|",
            "$DataContext.Name:|caret|",
            StringComparison.Ordinal));
        using CompletionFixture readOnlyMode = CompletionFixture.Create(markup.Replace(
            "$DataContext.|caret|",
            "$DataContext.ReadOnly:|caret|",
            StringComparison.Ordinal));
        using CompletionFixture impossibleChain = CompletionFixture.Create(
            "<Window><Button Width=\"$self.Width.|caret|\" /></Window>");

        Assert.Contains(local.Complete(), item => item.Label == "Name");
        Assert.DoesNotContain(local.Complete(), item => item.Label == "Person");
        Assert.Contains(writableMode.Complete(), item => item.Label == "TwoWay");
        Assert.Contains(readOnlyMode.Complete(), item => item.Label == "OneWay");
        Assert.DoesNotContain(readOnlyMode.Complete(), item => item.Label == "TwoWay");
        Assert.Empty(impossibleChain.Complete());
    }

    [Fact]
    public void DirectivesArgumentsAndSignatureHelpUseTheExactEmbeddedContext()
    {
        using CompletionFixture aspect = CompletionFixture.Create(
            "<Window><Window.Resources><Aspect TargetType=\"Button\">@an|caret|</Aspect></Window.Resources></Window>");
        using CompletionFixture normal = CompletionFixture.Create(
            "<Window>@|caret|</Window>");
        using CompletionFixture motionOption = CompletionFixture.Create(
            "<Window><Window.Resources><Aspect TargetType=\"Button\">@animate { hold|caret| }</Aspect></Window.Resources></Window>");
        using CompletionFixture motionOptionValue = CompletionFixture.Create(
            "<Window><Window.Resources><Aspect TargetType=\"Button\">@animate { holdOnComplete = |caret| }</Aspect></Window.Resources></Window>");
        using CompletionFixture motionProperty = CompletionFixture.Create(
            "<Window><Window.Resources><Aspect TargetType=\"Button\">@animate { @to { Op|caret| } }</Aspect></Window.Resources></Window>");
        CernealaCompletionService service = new();
        CernealaDocument signatureDocument = new(
            "Signature.cui.xml",
            LanguageSourceText.From("<Window>Tween(100ms, )</Window>"));
        int signatureOffset = signatureDocument.Text.ToString().IndexOf(")", StringComparison.Ordinal);

        Assert.Contains(aspect.Complete(), item => item.Label == "@animate");
        Assert.DoesNotContain(normal.Complete(), item => item.Label == "@layer");
        Assert.Contains(motionOption.Complete(), item => item.Label == "holdOnComplete");
        Assert.Contains(motionOptionValue.Complete(), item => item.Label == "true");
        Assert.Contains(motionProperty.Complete(), item => item.Label == "Opacity");
        CernealaSignatureHelp help = Assert.IsType<CernealaSignatureHelp>(
            service.GetSignatureHelp(signatureDocument, signatureOffset));
        Assert.Equal(1, help.ActiveParameter);
        Assert.Contains("duration", Assert.Single(help.Signatures).Label, StringComparison.Ordinal);
    }

    [Fact]
    public void PrismAndScopedMotionCallsCompleteArgumentsAndTrackActiveParameters()
    {
        (string prismSymbol, LanguageArgumentFact prismArgument) = new[] { "filter", "style", "mask", "backdrop" }
            .SelectMany(kind => CernealaLanguageFacts.GetPrismSymbols(kind))
            .Select(symbol => (Symbol: symbol, Arguments: CernealaLanguageFacts.FindPrismProperties(symbol)))
            .Where(candidate => candidate.Arguments.Count > 1)
            .Select(candidate => (candidate.Symbol, candidate.Arguments[0]))
            .First();
        using CompletionFixture prism = CompletionFixture.Create(
            "<Window><Window.Resources><PrismComposition Name=\"Fx\">@filter " +
            prismSymbol + "(|caret|)</PrismComposition></Window.Resources></Window>");
        const string motionMarkup = """
            <Window>
              <Window.Resources>
                <MotionClip Name="Pulse" TargetType="Button">
                  @parameter Amount: float = 1;
                </MotionClip>
                <Aspect Name="Animated" TargetType="Button">
                  @run $Pulse(|caret|)
                </Aspect>
              </Window.Resources>
            </Window>
            """;
        using CompletionFixture motion = CompletionFixture.Create(motionMarkup);
        CernealaCompletionService service = new();

        Assert.Contains(prism.Complete(), item => item.Label == prismArgument.Name);
        Assert.Contains(motion.Complete(), item => item.Label == "Amount");
        CernealaSignatureHelp motionHelp = Assert.IsType<CernealaSignatureHelp>(
            service.GetSignatureHelp(motion.Document, motion.Offset, motion.Model));
        Assert.Contains("Amount", Assert.Single(motionHelp.Signatures).Label, StringComparison.Ordinal);

        string prismSignatureText = prism.Text.Replace(")", ", )", StringComparison.Ordinal);
        CernealaDocument prismSignatureDocument = new("PrismSignature.cui.xml", LanguageSourceText.From(prismSignatureText));
        int signatureOffset = prismSignatureText.IndexOf(")", StringComparison.Ordinal);
        CernealaSignatureHelp prismHelp = Assert.IsType<CernealaSignatureHelp>(
            service.GetSignatureHelp(prismSignatureDocument, signatureOffset));
        Assert.Equal(1, prismHelp.ActiveParameter);
    }

    [Fact]
    public void ResolveLoadsSignatureDeclaringTypeDocumentationDeprecationAndAssemblyOnDemand()
    {
        using CompletionFixture fixture = CompletionFixture.Create(
            "<Window DataType=\"Test.ViewModel\"><TextBlock Text=\"$DataContext.Person.|caret|\" /></Window>");
        CernealaCompletionService service = new();
        CernealaCompletionItem name = Assert.Single(fixture.Complete().Where(item => item.Label == "Name"));
        CernealaCompletionItem legacy = Assert.Single(fixture.Complete().Where(item => item.Label == "Legacy"));

        Assert.NotNull(name.MemberName);
        CernealaResolvedCompletion resolved = Assert.IsType<CernealaResolvedCompletion>(
            service.Resolve(fixture.Model!, name.TypeMetadataName!, name.MemberName));

        Assert.Contains("Name", resolved.Signature, StringComparison.Ordinal);
        Assert.Equal("Test.Person", resolved.DeclaringType);
        Assert.Contains("Editable person name", resolved.Documentation, StringComparison.Ordinal);
        Assert.False(resolved.IsDeprecated);
        Assert.Equal("CompletionCorpus", resolved.AssemblyName);
        CernealaResolvedCompletion deprecated = Assert.IsType<CernealaResolvedCompletion>(
            service.Resolve(fixture.Model!, legacy.TypeMetadataName!, legacy.MemberName));
        Assert.True(deprecated.IsDeprecated);
    }

    [Fact]
    public async Task WarmCompletionP95StaysBelowBudgetAndIndependentDocumentsDoNotBlock()
    {
        using IDisposable performanceGate = AcquirePerformanceGate();
        string children = string.Concat(Enumerable.Range(0, 1200)
            .Select(index => "<TextBlock Text=\"Row " + index + "\" />"));
        using CompletionFixture large = CompletionFixture.Create(
            "<Window><StackPanel>" + children + "<Button |caret| /></StackPanel></Window>");
        using CompletionFixture second = CompletionFixture.Create(
            "<Window><TextBlock |caret| /></Window>");
        _ = large.Complete();
        _ = second.Complete();

        List<double> samples = new();
        for (int index = 0; index < 30; index++)
        {
            Stopwatch watch = Stopwatch.StartNew();
            _ = large.Complete();
            watch.Stop();
            samples.Add(watch.Elapsed.TotalMilliseconds);
        }

        double p95 = samples.OrderBy(value => value).ElementAt((int)Math.Ceiling(samples.Count * 0.95) - 1);
        Assert.True(p95 < 100, "Warm completion p95 was " + p95.ToString("F2") + " ms.");

        Task<IReadOnlyList<CernealaCompletionItem>> largeRequest = Task.Run(large.Complete);
        Task<IReadOnlyList<CernealaCompletionItem>> independentRequest = Task.Run(second.Complete);
        await Task.WhenAll(largeRequest, independentRequest).WaitAsync(TimeSpan.FromSeconds(2));
        IReadOnlyList<CernealaCompletionItem> independentItems = await independentRequest;
        Assert.Contains(independentItems, item => item.Label == "Text");
    }

    private static IDisposable AcquirePerformanceGate()
    {
        Semaphore semaphore = new(initialCount: 1, maximumCount: 1, "Cerneala.Language.PerformanceGate");
        bool acquired = semaphore.WaitOne(TimeSpan.FromMinutes(2));

        if (!acquired)
        {
            semaphore.Dispose();
            throw new TimeoutException("Timed out waiting for the language performance gate.");
        }

        return new PerformanceGateLease(semaphore);
    }

    private static void AssertValidAfterInsertion(string source, CernealaCompletionItem item)
    {
        string applied = source.Substring(0, item.ReplacementSpan.Start) +
            item.InsertText +
            source.Substring(item.ReplacementSpan.End);
        _ = XDocument.Parse(applied);
    }

    private static CSharpCompilation CreateProject()
    {
        const string code = """
            using System;
            using System.Collections.Generic;
            using System.ComponentModel;
            using Cerneala.UI.Controls;

            namespace Test
            {
                public sealed class ViewModel : INotifyPropertyChanged
                {
                    public Person Person { get; set; } = new Person();
                    public bool IsReady { get; set; }
                    public event PropertyChangedEventHandler? PropertyChanged;
                }

                public sealed class Person : INotifyPropertyChanged
                {
                    /// <summary>Editable person name.</summary>
                    public string Name { get; set; } = string.Empty;
                    public string ReadOnly => Name;
                    [Obsolete] public string Legacy { get; set; } = string.Empty;
                    public event PropertyChangedEventHandler? PropertyChanged;
                }
            }

            namespace Custom
            {
                public sealed class FancyControl : ContentControl
                {
                }
            }
            """;
        string trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("Trusted platform assemblies are unavailable.");
        MetadataReference[] references = trustedAssemblies
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(UIElement).Assembly.Location))
            .ToArray();
        return CSharpCompilation.Create(
            "CompletionCorpus",
            [CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest))],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private sealed class CompletionFixture : IDisposable
    {
        private readonly CernealaCompilation? compilation;
        private readonly CernealaCompletionService service = new();

        private CompletionFixture(
            string text,
            int offset,
            CernealaDocument document,
            CernealaCompilation? compilation,
            CernealaSemanticModel? model)
        {
            Text = text;
            Offset = offset;
            Document = document;
            this.compilation = compilation;
            Model = model;
        }

        public string Text { get; }
        public int Offset { get; }
        public CernealaDocument Document { get; }
        public CernealaSemanticModel? Model { get; }

        public static CompletionFixture Create(string markedText, bool semantic = true)
        {
            int offset = markedText.IndexOf(Caret, StringComparison.Ordinal);
            Assert.True(offset >= 0, "The completion fixture requires a caret marker.");
            string text = markedText.Replace(Caret, string.Empty, StringComparison.Ordinal);
            CernealaDocument document = new("View.cui.xml", LanguageSourceText.From(text));
            if (!semantic)
            {
                return new CompletionFixture(text, offset, document, null, null);
            }

            CernealaCompilation compilation = new(ProjectSymbols, [document]);
            CernealaSemanticModel model = compilation.GetSemanticModel(document.Path);
            return new CompletionFixture(text, offset, document, compilation, model);
        }

        public IReadOnlyList<CernealaCompletionItem> Complete() =>
            service.GetCompletions(Document, Model, Offset);

        public void Dispose() => compilation?.Dispose();
    }

    private sealed class PerformanceGateLease(Semaphore semaphore) : IDisposable
    {
        public void Dispose()
        {
            semaphore.Release();
            semaphore.Dispose();
        }
    }
}
