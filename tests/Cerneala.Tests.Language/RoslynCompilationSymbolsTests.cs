using Cerneala.Language.Semantics.Symbols;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Runtime.CompilerServices;

namespace Cerneala.Tests.Language;

[CollectionDefinition("Language symbol adapter lifetime", DisableParallelization = true)]
public sealed class SymbolAdapterLifetimeCollection;

[Collection("Language symbol adapter lifetime")]
public sealed class RoslynCompilationSymbolsTests
{
    [Fact]
    public void RepeatedTypeQueriesReuseTheCompilationOwnedAdapter()
    {
        RoslynCompilationSymbols symbols = new(CreateCompilation());
        ILanguageTypeSymbol type = Assert.IsAssignableFrom<ILanguageTypeSymbol>(symbols.FindType("Fixture.Derived"));

        Assert.Same(type, symbols.FindType("Fixture.Derived"));
        Assert.Same(type, Assert.Single(symbols.FindTypes("Derived")));
        Assert.Same(type, Assert.Single(symbols.GetTypes(), candidate => candidate.MetadataName == "Fixture.Derived"));
        Assert.Same(type, symbols.FindDeclaredTypeForFile("Fixture.cs", "Derived"));
        Assert.Same(type, Assert.Single(type.GetMembers("Child")).ValueType);
        Assert.Same(type, Assert.Single(type.GetMembers("Children")).ValueType!.CollectionElementType);
        Assert.Same(type, Assert.Single(Assert.Single(type.GetMembers("Items")).ValueType!.TypeArguments));
        Assert.Same(symbols.FindType("Fixture.Base"), type.BaseType);
    }

    [Fact]
    public void ConcurrentQueriesPreserveTypeFactsAndAdapterIdentity()
    {
        RoslynCompilationSymbols symbols = new(CreateCompilation());
        ILanguageTypeSymbol[] results = new ILanguageTypeSymbol[128];
        Parallel.For(0, results.Length, index =>
        {
            ILanguageTypeSymbol type = symbols.FindType("Fixture.Derived")!;
            Assert.Equal("Fixture.Derived", type.MetadataName);
            Assert.Equal("Fixture", type.Namespace);
            Assert.True(type.HasAccessibleParameterlessConstructor);
            Assert.True(type.IsOrDerivesFrom("Fixture.Base"));
            Assert.False(type.IsOrDerivesFrom("Fixture.Missing"));
            results[index] = type;
        });
        Assert.All(results, result => Assert.Same(results[0], result));
    }

    [Fact]
    public void DisplayNamesPreserveConstructedTypesTuplesAndSpecialTypes()
    {
        RoslynCompilationSymbols symbols = new(CreateCompilation());
        ILanguageTypeSymbol type = symbols.FindType("Fixture.Derived")!;
        for (int iteration = 0; iteration < 3; iteration++)
        {
            Assert.Equal("int", symbols.FindType("System.Int32")!.MetadataName);
            Assert.Equal("System.Collections.Generic.List<Fixture.Derived>", Member("Items").ValueTypeMetadataName);
            Assert.Equal("(int left, int right)", Member("First").ValueTypeMetadataName);
            Assert.Equal("(int top, int bottom)", Member("Second").ValueTypeMetadataName);
            Assert.Equal("int?", Member("Optional").ValueTypeMetadataName);
            Assert.True(Member("Items").ValueType!.IsOrImplements("System.Collections.Generic.IEnumerable<Fixture.Derived>"));
            Assert.True(symbols.FindType("System.Int32")!.IsOrDerivesFrom("int"));
            Assert.False(symbols.FindType("Fixture.PrivateConstructor")!.HasAccessibleParameterlessConstructor);
        }

        ILanguageMemberSymbol Member(string name) => Assert.Single(type.GetMembers(name));
    }

    [Fact]
    public void ReplacementCompilationDoesNotReuseOldTypeFacts()
    {
        CSharpCompilation original = CreateCompilation();
        RoslynCompilationSymbols first = new(original, 1);
        Assert.Null(first.FindType("Fixture.Later"));
        Assert.Null(first.FindType("fixture.Derived"));
        CSharpCompilation changed = original.ReplaceSyntaxTree(original.SyntaxTrees.Single(),
            CSharpSyntaxTree.ParseText("namespace Fixture { public sealed class Derived { private Derived() { } } public class Later { } }", path: "Fixture.cs"));
        RoslynCompilationSymbols second = new(changed, 2);
        ILanguageTypeSymbol oldType = first.FindType("Fixture.Derived")!;
        ILanguageTypeSymbol newType = second.FindType("Fixture.Derived")!;

        Assert.NotSame(oldType, newType);
        Assert.True(oldType.HasAccessibleParameterlessConstructor);
        Assert.True(oldType.IsOrDerivesFrom("Fixture.Base"));
        Assert.False(newType.HasAccessibleParameterlessConstructor);
        Assert.False(newType.IsOrDerivesFrom("Fixture.Base"));
        Assert.NotNull(second.FindType("Fixture.Later"));
        Assert.Null(first.FindType("Fixture.Later"));
    }

    [Fact]
    public void EvictedTypeNamesAreReleasedWithoutChangingLookupResults()
    {
        RoslynCompilationSymbols symbols = new(CreateCompilation());
        ILanguageTypeSymbol type = symbols.FindType("Fixture.Derived")!;
        WeakReference query = FillTypeNameCache(symbols);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(query.IsAlive);
        Assert.Same(type, symbols.FindType("Fixture.Derived"));
        Assert.Null(symbols.FindType("Fixture.DoesNotExist"));
        GC.KeepAlive(symbols);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference FillTypeNameCache(RoslynCompilationSymbols symbols)
    {
        string query = new("Fixture.DoesNotExist".ToCharArray());
        Assert.Null(symbols.FindType(query));
        WeakReference reference = new(query);
        for (int index = 0; index < 1024; index++)
        {
            Assert.Null(symbols.FindType("Fixture.Missing" + index));
        }

        return reference;
    }

    [Fact]
    public void ReleasingTheCompilationReleasesItsAdaptersAndCachedFacts()
    {
        WeakReference[] references = CreateReleasedSymbols();
        for (int attempt = 0; attempt < 3 && references.Any(reference => reference.IsAlive); attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        Assert.All(references, reference => Assert.False(reference.IsAlive));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference[] CreateReleasedSymbols()
    {
        CSharpCompilation compilation = CreateCompilation();
        RoslynCompilationSymbols symbols = new(compilation);
        ILanguageTypeSymbol type = symbols.FindType("Fixture.Derived")!;
        Assert.True(type.HasAccessibleParameterlessConstructor);
        Assert.Equal("Fixture.Derived", type.MetadataName);
        Assert.True(type.IsOrDerivesFrom("Fixture.Base"));
        return [new(compilation), new(symbols), new(type)];
    }

    private static CSharpCompilation CreateCompilation()
    {
        CSharpCompilation compilation = CSharpCompilation.Create("SymbolAdapterFixture",
            [CSharpSyntaxTree.ParseText("""
                namespace Fixture
                {
                    public class Base { }
                    public sealed class Derived : Base
                    {
                        public Derived Child => this;
                        public Derived[] Children => null;
                        public System.Collections.Generic.List<Derived> Items => null;
                        public (int left, int right) First => default;
                        public (int top, int bottom) Second => default;
                        public int? Optional => null;
                    }
                    public sealed class PrivateConstructor { private PrivateConstructor() { } }
                }
                """, path: "Fixture.cs")],
            ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
                .Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        return compilation;
    }
}
