using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using RoslynRepoIndexer.Core;

namespace RoslynRepoIndexer.Tests;

public sealed class RoslynSymbolIntegrationTests
{
    [Fact]
    public async Task Index_persists_namespace_delegate_constructor_async_overload_and_top_level_edge_cases()
    {
        using var repo = TestRepo.Create();
        await CreateEdgeCaseProjectAsync(repo.Root);

        await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);

        var symbols = ReadJsonLines(IndexFile(repo.Root, "symbols.jsonl")).ToArray();
        var documents = ReadJsonLines(IndexFile(repo.Root, "documents.jsonl")).ToArray();

        AssertSymbol(symbols, "namespace", "FileScopedFixture");
        AssertSymbol(symbols, "namespace", "BlockScopedFixture");
        AssertSymbol(symbols, "delegate", "WorkHandler").AssertParameterTypes("int");
        AssertSymbol(symbols, "constructor", "Worker").AssertParameterTypes("string");

        var asyncMethod = AssertSymbol(symbols, "method", "RunAsync");
        Assert.Contains("async", Modifiers(asyncMethod));
        asyncMethod.AssertParameterTypes("CancellationToken");

        var overloads = symbols
            .Where(s => s.GetProperty("kind").GetString() == "method"
                        && s.GetProperty("name").GetString() == "Overload")
            .Select(s => s.GetProperty("signature").GetString())
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();
        Assert.Contains("BlockScopedFixture.OverloadTarget.Overload(int)", overloads);
        Assert.Contains("BlockScopedFixture.OverloadTarget.Overload(string)", overloads);

        Assert.Contains(documents, d => d.GetProperty("relativePath").GetString() == "Program.cs");
        AssertSymbol(symbols, "local", "topLevelValue");
    }

    [Fact]
    public async Task Index_persists_semantic_reference_kinds_for_attributes_inheritance_creation_and_invocation()
    {
        using var repo = TestRepo.Create();
        await CreateEdgeCaseProjectAsync(repo.Root);

        await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);

        var symbols = ReadJsonLines(IndexFile(repo.Root, "symbols.jsonl")).ToArray();
        var references = ReadJsonLines(IndexFile(repo.Root, "references.jsonl")).ToArray();

        AssertSymbol(symbols, "class", "MarkerAttribute");
        AssertReference(references, "attribute", "MarkerAttribute");
        AssertReference(references, "inheritance", "BaseThing");
        AssertReference(references, "object-creation", "CreatedThing");
        AssertReference(references, "invocation", "CallTarget");
    }

    [Fact]
    public async Task Index_uses_single_candidate_symbol_when_symbol_info_is_null()
    {
        using var repo = TestRepo.Create();
        await CreateCandidateReferenceProjectAsync(repo.Root);

        await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);

        var symbols = ReadJsonLines(IndexFile(repo.Root, "symbols.jsonl")).ToArray();
        var references = ReadJsonLines(IndexFile(repo.Root, "references.jsonl")).ToArray();

        var candidateDeclaration = AssertSymbol(symbols, "method", "CandidateOnly");
        var candidateReferences = references
            .Where(r => r.GetProperty("referencedName").GetString() == "CandidateOnly")
            .ToArray();

        Assert.NotEmpty(candidateReferences);
        var reference = Assert.Single(candidateReferences.Where(r => r.GetProperty("spanLength").GetInt32() == "CandidateOnly".Length));
        Assert.Equal("invocation", reference.GetProperty("referenceKind").GetString());
        Assert.Equal(candidateDeclaration.GetProperty("symbolId").GetString(), reference.GetProperty("symbolId").GetString());
    }

    [Fact]
    public async Task Index_references_only_local_declarations_and_uses_declaration_symbol_id_with_span_length_dedupe_key()
    {
        using var repo = TestRepo.Create();
        await CreateCandidateReferenceProjectAsync(repo.Root);

        await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);

        var symbols = ReadJsonLines(IndexFile(repo.Root, "symbols.jsonl")).ToArray();
        var references = ReadJsonLines(IndexFile(repo.Root, "references.jsonl")).ToArray();

        var localDeclaration = AssertSymbol(symbols, "method", "LocalCall");
        var localReferences = references
            .Where(r => r.GetProperty("referencedName").GetString() == "LocalCall")
            .ToArray();

        Assert.NotEmpty(localReferences);
        var reference = Assert.Single(localReferences.Where(r => r.GetProperty("spanLength").GetInt32() == "LocalCall".Length));
        Assert.Equal(localDeclaration.GetProperty("symbolId").GetString(), reference.GetProperty("symbolId").GetString());
        Assert.Equal(ExpectedStableId(
            reference.GetProperty("symbolId").GetString()!,
            reference.GetProperty("documentId").GetString()!,
            reference.GetProperty("spanStart").GetInt32(),
            reference.GetProperty("spanLength").GetInt32()), reference.GetProperty("referenceId").GetString());
        Assert.Equal(references.Count(), references
            .Select(r => string.Join('|',
                r.GetProperty("symbolId").GetString(),
                r.GetProperty("documentId").GetString(),
                r.GetProperty("spanStart").GetInt32(),
                r.GetProperty("spanLength").GetInt32()))
            .Distinct(StringComparer.Ordinal)
            .Count());

        Assert.DoesNotContain(references, r => r.GetProperty("referencedName").GetString() == "WriteLine");
        Assert.DoesNotContain(references, r => r.GetProperty("referencedName").GetString() == "Console");
    }

    [Fact]
    public async Task Index_handles_incomplete_partial_code_without_crashing()
    {
        using var repo = TestRepo.Create();
        await CreateIncompleteProjectAsync(repo.Root);

        var summary = await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);

        Assert.True(summary.Documents > 0);
        var symbols = ReadJsonLines(IndexFile(repo.Root, "symbols.jsonl")).ToArray();

        var partialDeclarations = symbols
            .Where(s => s.GetProperty("kind").GetString() == "class"
                        && s.GetProperty("name").GetString() == "BrokenPartial")
            .ToArray();
        Assert.Equal(2, partialDeclarations.Length);
        Assert.All(partialDeclarations, s => Assert.True(s.GetProperty("isPartial").GetBoolean()));
        AssertSymbol(symbols, "method", "Survives");
    }

    [Fact]
    public async Task Index_persists_common_csharp_symbol_declarations()
    {
        using var repo = TestRepo.Create();
        await CreateSymbolProjectAsync(repo.Root);

        var summary = await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);

        Assert.True(summary.Symbols > 0);
        var symbols = ReadJsonLines(IndexFile(repo.Root, "symbols.jsonl")).ToArray();

        AssertSymbol(symbols, "record", "ShapeRecord");
        AssertSymbol(symbols, "record", "Quantity");
        AssertSymbol(symbols, "struct", "Vector");
        AssertSymbol(symbols, "interface", "IShape");
        AssertSymbol(symbols, "enum", "Paint");
        AssertSymbol(symbols, "enum-member", "Red");
        AssertSymbol(symbols, "enum-member", "Blue");
        AssertSymbol(symbols, "property", "Name");
        AssertSymbol(symbols, "indexer", "this[]");
        AssertSymbol(symbols, "event", "Changed");
        AssertSymbol(symbols, "field", "X");
        AssertSymbol(symbols, "destructor", "Finalize");
        AssertSymbolWithSignature(symbols, "operator", "operator +");
        AssertSymbolWithSignature(symbols, "operator", "explicit operator int");
        AssertSymbol(symbols, "local-function", "LocalHelper");
        AssertSymbol(symbols, "parameter", "input");
        AssertSymbol(symbols, "parameter", "item");
        AssertSymbol(symbols, "method", "Transform").AssertParameterTypes("TInput");
        AssertSymbol(symbols, "method", "Echo").AssertParameterTypes("TValue");
        AssertSymbol(symbols, "class", "Nested");

        var partialDeclarations = symbols
            .Where(s => s.GetProperty("kind").GetString() == "class"
                        && s.GetProperty("name").GetString() == "PartialBox")
            .ToArray();
        Assert.Equal(2, partialDeclarations.Length);
        Assert.All(partialDeclarations, s =>
        {
            Assert.True(s.GetProperty("isPartial").GetBoolean());
            Assert.Contains("partial", Modifiers(s));
        });
        Assert.Equal(2, partialDeclarations.Select(s => s.GetProperty("path").GetString()).Distinct(StringComparer.Ordinal).Count());
        Assert.Single(partialDeclarations.Select(s => s.GetProperty("fullyQualifiedName").GetString()).Distinct(StringComparer.Ordinal));

        var extension = AssertSymbol(symbols, "method", "Describe");
        Assert.Contains("static", Modifiers(extension));
        Assert.Contains("extension", Modifiers(extension));
        extension.AssertParameterTypes("IShape<int>");
    }

    [Fact]
    public async Task Index_uses_semantic_symbol_ids_and_deterministic_local_fallbacks()
    {
        using var repo = TestRepo.Create();
        await CreateSymbolIdProjectAsync(repo.Root);

        await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);

        var symbols = ReadJsonLines(IndexFile(repo.Root, "symbols.jsonl")).ToArray();

        var service = AssertSymbol(symbols, "class", "CustomerService");
        Assert.Equal("doc-comment-id", RequiredString(service, "symbolKey"));
        Assert.Equal("T:IdentityFixture.CustomerService", service.GetProperty("symbolId").GetString());
        Assert.Equal("IdentityFixture.CustomerService", service.GetProperty("fullyQualifiedName").GetString());
        Assert.DoesNotContain("global::", service.GetProperty("fullyQualifiedName").GetString(), StringComparison.Ordinal);

        var genericMethod = AssertSymbol(symbols, "method", "Resolve");
        Assert.Equal("doc-comment-id", RequiredString(genericMethod, "symbolKey"));
        Assert.Equal("M:IdentityFixture.CustomerService.Resolve``1(System.String)~``0", genericMethod.GetProperty("symbolId").GetString());
        Assert.Equal("IdentityFixture.CustomerService.Resolve<T>(string)", genericMethod.GetProperty("signature").GetString());
        Assert.DoesNotContain("global::", genericMethod.GetProperty("signature").GetString(), StringComparison.Ordinal);

        var typeParameter = AssertSymbol(symbols, "type-parameter", "T");
        Assert.StartsWith("symbol-key:", typeParameter.GetProperty("symbolId").GetString(), StringComparison.Ordinal);
        Assert.NotEqual("doc-comment-id", RequiredString(typeParameter, "symbolKey"));
        Assert.NotEqual("deterministic-local", RequiredString(typeParameter, "symbolKey"));
        Assert.False(RequiredString(typeParameter, "symbolKey")?.StartsWith("semantic|", StringComparison.Ordinal));

        var parameter = AssertSymbol(symbols, "parameter", "input");
        Assert.Equal("deterministic-local", RequiredString(parameter, "symbolKey"));
        Assert.StartsWith("local:", parameter.GetProperty("symbolId").GetString(), StringComparison.Ordinal);
        Assert.Contains("Identity.cs", parameter.GetProperty("symbolId").GetString(), StringComparison.Ordinal);
        Assert.Contains("|input|parameter", parameter.GetProperty("symbolId").GetString(), StringComparison.Ordinal);

        var local = AssertSymbol(symbols, "local", "normalizedValue");
        Assert.Equal("deterministic-local", RequiredString(local, "symbolKey"));
        Assert.StartsWith("local:", local.GetProperty("symbolId").GetString(), StringComparison.Ordinal);
        Assert.Contains("Identity.cs", local.GetProperty("symbolId").GetString(), StringComparison.Ordinal);
        Assert.Contains("|normalizedValue|local", local.GetProperty("symbolId").GetString(), StringComparison.Ordinal);

        var secondRunIds = ReadJsonLines(IndexFile(repo.Root, "symbols.jsonl"))
            .Where(s => s.GetProperty("name").GetString() is "input" or "normalizedValue")
            .Select(s => s.GetProperty("symbolId").GetString())
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();

        await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);

        var thirdRunIds = ReadJsonLines(IndexFile(repo.Root, "symbols.jsonl"))
            .Where(s => s.GetProperty("name").GetString() is "input" or "normalizedValue")
            .Select(s => s.GetProperty("symbolId").GetString())
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(secondRunIds, thirdRunIds);
    }

    [Fact]
    public async Task Index_persists_aspnet_controller_and_minimal_api_text_and_symbols_without_running_app()
    {
        using var repo = TestRepo.Create();
        await CreateAspNetProjectAsync(repo.Root);

        await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);

        var documents = ReadJsonLines(IndexFile(repo.Root, "documents.jsonl")).ToArray();
        var symbols = ReadJsonLines(IndexFile(repo.Root, "symbols.jsonl")).ToArray();
        var tokens = ReadJsonLines(IndexFile(repo.Root, "tokens.jsonl")).ToArray();
        var references = ReadJsonLines(IndexFile(repo.Root, "references.jsonl")).ToArray();

        Assert.Contains(documents, d => d.GetProperty("relativePath").GetString() == "Controllers/InkController.cs");
        Assert.Contains(documents, d => d.GetProperty("relativePath").GetString() == "Program.cs");

        var controller = AssertSymbol(symbols, "class", "InkController");
        Assert.Equal("InkWeb.Controllers.InkController", controller.GetProperty("fullyQualifiedName").GetString());
        AssertSymbol(symbols, "method", "Get").AssertParameterTypes("int");
        AssertSymbol(symbols, "local", "builder");
        AssertSymbol(symbols, "local", "app");

        Assert.Contains(tokens, t => t.GetProperty("token").GetString() == "map");
        Assert.Contains(tokens, t => t.GetProperty("token").GetString() == "get");
        Assert.Contains(tokens, t => t.GetProperty("token").GetString() == "api");
        Assert.Contains(tokens, t => t.GetProperty("token").GetString() == "controller");
        Assert.Contains(tokens, t => t.GetProperty("token").GetString() == "base");
    }

    [Fact]
    public async Task Index_persists_modern_csharp_alias_nullable_interface_partial_generic_nested_and_collection_expression_cases()
    {
        using var repo = TestRepo.Create();
        await CreateModernCSharpProjectAsync(repo.Root);

        await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);

        var symbols = ReadJsonLines(IndexFile(repo.Root, "symbols.jsonl")).ToArray();
        var tokens = ReadJsonLines(IndexFile(repo.Root, "tokens.jsonl")).ToArray();
        var references = ReadJsonLines(IndexFile(repo.Root, "references.jsonl")).ToArray();

        var maybe = AssertSymbol(symbols, "method", "Maybe");
        maybe.AssertParameterTypes("string?");
        Assert.Equal("string?", maybe.GetProperty("returnType").GetString());

        var partialDeclarations = symbols
            .Where(s => s.GetProperty("kind").GetString() == "class"
                        && s.GetProperty("name").GetString() == "Outer")
            .ToArray();
        Assert.Equal(2, partialDeclarations.Length);
        Assert.All(partialDeclarations, s => Assert.True(s.GetProperty("isPartial").GetBoolean()));

        var partialHooks = symbols
            .Where(s => s.GetProperty("kind").GetString() == "method"
                        && s.GetProperty("name").GetString() == "OnSaved")
            .ToArray();
        Assert.Equal(2, partialHooks.Length);
        Assert.All(partialHooks, s => Assert.Contains("partial", Modifiers(s)));

        var nested = AssertSymbol(symbols, "class", "Inner");
        Assert.Equal("ModernFixture.Outer<T>.Inner<U>", nested.GetProperty("fullyQualifiedName").GetString());
        var convert = AssertSymbol(symbols, "method", "Convert");
        Assert.Equal("ModernFixture.Outer<T>.Inner<U>.Convert<V>(U, V)", convert.GetProperty("signature").GetString());

        AssertSymbol(symbols, "interface", "IStore");
        AssertSymbol(symbols, "method", "Save").AssertParameterTypes("string");
        AssertReference(references, "inheritance", "IStore");
        AssertReference(references, "invocation", "OnSaved");

        AssertSymbol(symbols, "local", "numbers");
        Assert.Contains(tokens, t => t.GetProperty("token").GetString() == "name");
        Assert.Contains(tokens, t => t.GetProperty("token").GetString() == "map");
        Assert.Contains(tokens, t => t.GetProperty("token").GetString() == "text");
        Assert.Contains(tokens, t => t.GetProperty("token").GetString() == "alias");
    }

    private static async Task CreateSymbolProjectAsync(string root)
    {
        await File.WriteAllTextAsync(Path.Combine(root, "Sample.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "Symbols.cs"), """
            namespace SymbolFixture;

            public enum Paint
            {
                Red,
                Blue
            }

            public interface IShape<T>
            {
                string Name { get; }
                event EventHandler? Changed;
                T this[int index] { get; }
                T Transform<TInput>(TInput input);
            }

            public record class ShapeRecord(string Name);

            public readonly record struct Quantity(int Value);

            public struct Vector
            {
                public int X;
                public int Y { get; set; }
                public event EventHandler? Changed;

                public Vector(int x)
                {
                    X = x;
                    Y = 0;
                    Changed = null;
                }

                public int this[int index] => index == 0 ? X : Y;

                public static Vector operator +(Vector left, Vector right) => new(left.X + right.X);

                public static explicit operator int(Vector value) => value.X;

                public TValue Echo<TValue>(TValue input)
                {
                    TValue LocalHelper<TLocal>(TLocal item, int count) => input;
                    return LocalHelper(input, 1);
                }
            }

            public partial class PartialBox<T>
            {
                public class Nested { }

                ~PartialBox()
                {
                }
            }

            public static class ShapeExtensions
            {
                public static string Describe(this IShape<int> shape) => shape.Name;
            }
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "PartialBox.More.cs"), """
            namespace SymbolFixture;

            public partial class PartialBox<T>
            {
                public T? Value { get; set; }
            }
            """);
    }

    private static async Task CreateAspNetProjectAsync(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, "Controllers"));
        await File.WriteAllTextAsync(Path.Combine(root, "InkWeb.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "Controllers", "InkController.cs"), """
            using Microsoft.AspNetCore.Mvc;

            namespace InkWeb.Controllers;

            [ApiController]
            [Route("api/[controller]")]
            public sealed class InkController : ControllerBase
            {
                [HttpGet("{id:int}")]
                public ActionResult<string> Get(int id) => Ok($"ink-{id}");
            }
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "Program.cs"), """
            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();

            app.MapGet("/api/ping", () => Results.Ok("pong"));

            app.Run();
            """);
    }

    private static async Task CreateModernCSharpProjectAsync(string root)
    {
        await File.WriteAllTextAsync(Path.Combine(root, "Modern.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <LangVersion>latest</LangVersion>
              </PropertyGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "Modern.One.cs"), """
            using NameMap = System.Collections.Generic.Dictionary<string, string>;
            using TextAlias = string;

            namespace ModernFixture;

            public interface IStore
            {
                string Save(string value);
            }

            public sealed partial class Outer<T> : IStore
            {
                private readonly NameMap names = [];

                public string? Maybe(string? input) => input;

                public string Save(TextAlias value)
                {
                    OnSaved(value);
                    return value;
                }

                partial void OnSaved(string value);

                public sealed class Inner<U>
                {
                    public V Convert<V>(U item, V fallback) => fallback;
                }
            }
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "Modern.Two.cs"), """
            namespace ModernFixture;

            public sealed partial class Outer<T>
            {
                partial void OnSaved(string value)
                {
                    int[] numbers = [1, 2, 3];
                    _ = numbers.Length;
                }
            }
            """);
    }

    private static async Task CreateEdgeCaseProjectAsync(string root)
    {
        await File.WriteAllTextAsync(Path.Combine(root, "EdgeCases.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "GlobalUsings.cs"), """
            global using System;
            global using System.Threading;
            global using System.Threading.Tasks;
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "FileScoped.cs"), """
            namespace FileScopedFixture;

            public delegate Task WorkHandler(int count);

            public sealed class Worker
            {
                private readonly string name;

                public Worker(string name)
                {
                    this.name = name;
                }

                public async Task<string> RunAsync(CancellationToken cancellationToken)
                {
                    await Task.Delay(1, cancellationToken);
                    return name;
                }
            }
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "BlockScoped.cs"), """
            namespace BlockScopedFixture
            {
                [AttributeUsage(AttributeTargets.Class)]
                public sealed class MarkerAttribute : Attribute
                {
                }

                public class BaseThing
                {
                    public void CallTarget()
                    {
                    }
                }

                public sealed class CreatedThing
                {
                }

                [Marker]
                public sealed class DerivedThing : BaseThing
                {
                    public void Use()
                    {
                        var created = new CreatedThing();
                        CallTarget();
                    }
                }

                public sealed class OverloadTarget
                {
                    public void Overload(int count)
                    {
                    }

                    public void Overload(string name)
                    {
                    }
                }
            }
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "Program.cs"), """
            using BlockScopedFixture;

            var topLevelValue = new CreatedThing();
            Console.WriteLine(topLevelValue.GetType().Name);
            """);
    }

    private static async Task CreateSymbolIdProjectAsync(string root)
    {
        await File.WriteAllTextAsync(Path.Combine(root, "Identity.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "Identity.cs"), """
            namespace IdentityFixture;

            public sealed class CustomerService
            {
                public T Resolve<T>(string input)
                {
                    var normalizedValue = input.Trim();
                    return default!;
                }
            }
            """);
    }

    private static async Task CreateIncompleteProjectAsync(string root)
    {
        await File.WriteAllTextAsync(Path.Combine(root, "Incomplete.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "Partial.One.cs"), """
            namespace BrokenFixture;

            public partial class BrokenPartial
            {
                public void Survives()
                {
                }
            }
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "Partial.Two.cs"), """
            namespace BrokenFixture;

            public partial class BrokenPartial
            {
                public void MissingBrace()
                {
                    var value = 
            """);
    }

    private static async Task CreateCandidateReferenceProjectAsync(string root)
    {
        await File.WriteAllTextAsync(Path.Combine(root, "CandidateRefs.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "CandidateRefs.cs"), """
            namespace CandidateRefs;

            public sealed class ReferenceTarget
            {
                public void LocalCall()
                {
                }

                public void CandidateOnly(int value)
                {
                }

                public void Use()
                {
                    LocalCall();
                    CandidateOnly("wrong argument type");
                    System.Console.WriteLine("external reference");
                }
            }
            """);
    }

    private static JsonElement AssertSymbol(IReadOnlyList<JsonElement> symbols, string kind, string name)
    {
        var matches = symbols
            .Where(s => s.GetProperty("kind").GetString() == kind
                        && s.GetProperty("name").GetString() == name)
            .ToArray();
        Assert.NotEmpty(matches);
        return matches[0];
    }

    private static JsonElement AssertReference(IReadOnlyList<JsonElement> references, string kind, string referencedName)
    {
        var matches = references
            .Where(r => r.GetProperty("referenceKind").GetString() == kind
                        && r.GetProperty("referencedName").GetString() == referencedName)
            .ToArray();
        Assert.NotEmpty(matches);
        return matches[0];
    }

    private static JsonElement AssertSymbolWithSignature(IReadOnlyList<JsonElement> symbols, string kind, string signaturePart)
    {
        var matches = symbols
            .Where(s => s.GetProperty("kind").GetString() == kind
                        && s.GetProperty("signature").GetString()?.Contains(signaturePart, StringComparison.Ordinal) == true)
            .ToArray();
        Assert.NotEmpty(matches);
        return matches[0];
    }

    private static IReadOnlyList<string?> Modifiers(JsonElement symbol)
        => symbol.GetProperty("modifiers").EnumerateArray().Select(m => m.GetString()).ToArray();

    private static string? RequiredString(JsonElement element, string propertyName)
    {
        Assert.True(element.TryGetProperty(propertyName, out var property), $"Expected JSON property '{propertyName}' to exist.");
        return property.GetString();
    }

    private static IEnumerable<JsonElement> ReadJsonLines(string path)
    {
        foreach (var line in File.ReadLines(path))
        {
            using var document = JsonDocument.Parse(line);
            yield return document.RootElement.Clone();
        }
    }

    private static string IndexFile(string root, string fileName)
        => Path.Combine(root, ".roslyn-index", "v1", fileName);

    private static string ExpectedStableId(string symbolId, string documentId, int spanStart, int spanLength)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{symbolId}|{documentId}|{spanStart}|{spanLength}"));
        return Convert.ToHexString(bytes).ToLowerInvariant()[..16];
    }
}

internal static class SymbolJsonAssertions
{
    public static void AssertParameterTypes(this JsonElement symbol, params string[] expected)
    {
        var actual = symbol.GetProperty("parameterTypes").EnumerateArray().Select(p => p.GetString()).ToArray();
        Assert.Equal(expected, actual);
    }
}

