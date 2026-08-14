using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Cerneala.LanguageServer.Protocol;
using StreamJsonRpc;

namespace Cerneala.Tests.LanguageServer;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class LanguageServerPerformanceCollection
{
    public const string Name = "Language server performance";
}

[Collection(LanguageServerPerformanceCollection.Name)]
public sealed class HardeningProtocolTests
{
    [Fact]
    public async Task ConcurrentTypingCancellationReloadAndWarmBudgetsStayBounded()
    {
        using IDisposable performanceGate = AcquirePerformanceGate();
        using TemporaryHardeningWorkspace fixture = TemporaryHardeningWorkspace.Create();
        using CancellationTokenSource timeout = new(TimeSpan.FromMinutes(2));
        using StringWriter logs = new();
        await using ProtocolTestClient client = ProtocolTestClient.Start(logs);
        await client.InitializeAsync(timeout.Token, fixture.ProjectPath);
        await client.Rpc.NotifyWithParameterObjectAsync("initialized", new { });
        await client.Rpc.NotifyWithParameterObjectAsync("$/setTrace", new { value = "verbose" });

        string firstText = File.ReadAllText(fixture.FirstMarkupPath);
        string secondText = File.ReadAllText(fixture.SecondMarkupPath);
        await OpenAsync(client, fixture.FirstUri, firstText);
        await OpenAsync(client, fixture.SecondUri, secondText);

        LspPosition completionPosition = PositionAt(firstText, firstText.IndexOf("<Button", StringComparison.Ordinal) + 7);
        LspPosition navigationPosition = PositionAt(firstText, firstText.IndexOf("Button", StringComparison.Ordinal) + 1);
        TextDocumentPositionParams completionRequest = Request(fixture.FirstUri, completionPosition);
        TextDocumentPositionParams navigationRequest = Request(fixture.FirstUri, navigationPosition);

        for (int warmup = 0; warmup < 5; warmup++)
        {
            _ = await CompletionAsync(client, completionRequest, timeout.Token);
            _ = await DiagnosticsAsync(client, fixture.FirstUri, timeout.Token);
            _ = await client.Rpc.InvokeWithParameterObjectAsync<LspHover?>(
                "textDocument/hover",
                navigationRequest,
                timeout.Token);
        }

        List<double> completionSamples = new();
        List<double> diagnosticSamples = new();
        List<double> navigationSamples = new();
        for (int sample = 0; sample < 20; sample++)
        {
            completionSamples.Add(await MeasureAsync(() => CompletionAsync(client, completionRequest, timeout.Token)));
            diagnosticSamples.Add(await MeasureAsync(() => DiagnosticsAsync(client, fixture.FirstUri, timeout.Token)));
            navigationSamples.Add(await MeasureAsync(() => client.Rpc.InvokeWithParameterObjectAsync<LspHover?>(
                "textDocument/hover",
                navigationRequest,
                timeout.Token)));
            navigationSamples.Add(await MeasureAsync(() => client.Rpc.InvokeWithParameterObjectAsync<LspLocation[]>(
                "textDocument/definition",
                navigationRequest,
                timeout.Token)));
        }

        AssertBudget("completion", completionSamples, 100);
        AssertBudget("diagnostics", diagnosticSamples, 200);
        AssertBudget("navigation", navigationSamples, 100);
        Assert.True(
            completionSamples.Concat(diagnosticSamples).Concat(navigationSamples).Max() < 500,
            "A warm request exceeded the 500 ms cancellation boundary.");

        List<CancellationTokenSource> cancellations = Enumerable.Range(0, 100)
            .Select(_ => new CancellationTokenSource())
            .ToList();
        Task[] cancelledRequests = cancellations.Select(source => ObserveCancellationAsync(
            CompletionAsync(client, completionRequest, source.Token))).ToArray();
        await Task.Yield();
        foreach (CancellationTokenSource source in cancellations)
        {
            source.Cancel();
        }

        await Task.WhenAll(cancelledRequests).WaitAsync(timeout.Token);
        foreach (CancellationTokenSource source in cancellations)
        {
            source.Dispose();
        }

        for (int version = 2; version <= 21; version++)
        {
            firstText = firstText.Replace(
                "Title=\"Version " + (version - 2) + "\"",
                "Title=\"Version " + (version - 1) + "\"",
                StringComparison.Ordinal);
            secondText = secondText.Replace(
                "Title=\"Version " + (version - 2) + "\"",
                "Title=\"Version " + (version - 1) + "\"",
                StringComparison.Ordinal);
            await ChangeAsync(client, fixture.FirstUri, version, firstText);
            await ChangeAsync(client, fixture.SecondUri, version, secondText);
        }

        Assert.Equal("21", (await DiagnosticsAsync(client, fixture.FirstUri, timeout.Token)).ResultId);
        Assert.Equal("21", (await DiagnosticsAsync(client, fixture.SecondUri, timeout.Token)).ResultId);
        await client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didSave",
            new DidSaveTextDocumentParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = fixture.FirstUri }
            });
        Assert.NotEmpty((await CompletionAsync(
            client,
            Request(fixture.FirstUri, PositionAt(firstText, firstText.IndexOf("<Button", StringComparison.Ordinal) + 7)),
            timeout.Token)).Items);

        Assert.Equal(0, await client.StopAsync(timeout.Token));
        string logText = logs.ToString();
        Assert.DoesNotContain("Version 20", logText, StringComparison.Ordinal);
        Assert.DoesNotContain(fixture.FirstUri, logText, StringComparison.Ordinal);
        string[] operations = logText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => JsonDocument.Parse(line))
            .Where(document => document.RootElement.TryGetProperty("event", out JsonElement value) &&
                value.GetString() == "performance.measurement")
            .Select(document => document.RootElement.GetProperty("operation").GetString()!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Assert.Contains("parse", operations);
        Assert.Contains("bind", operations);
        Assert.Contains("queue", operations);
        Assert.Contains("completion", operations);
        Assert.Contains("diagnostics", operations);
        Assert.Contains("navigation", operations);
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

    private static async Task<double> MeasureAsync(Func<Task> action)
    {
        long started = Stopwatch.GetTimestamp();
        await action();
        return Stopwatch.GetElapsedTime(started).TotalMilliseconds;
    }

    private static void AssertBudget(string operation, IReadOnlyList<double> samples, double budget)
    {
        double p95 = samples.OrderBy(value => value).ElementAt((int)Math.Ceiling(samples.Count * 0.95) - 1);
        Assert.True(p95 < budget, operation + " warm p95 was " + p95.ToString("F2") + " ms.");
    }

    private static async Task ObserveCancellationAsync(Task request)
    {
        try
        {
            await request;
        }
        catch (OperationCanceledException)
        {
        }
        catch (RemoteInvocationException exception) when (exception.ErrorCode == -32800)
        {
        }
    }

    private static Task<CompletionList> CompletionAsync(
        ProtocolTestClient client,
        TextDocumentPositionParams request,
        CancellationToken cancellationToken) => client.Rpc.InvokeWithParameterObjectAsync<CompletionList>(
            "textDocument/completion",
            request,
            cancellationToken);

    private static Task<FullDocumentDiagnosticReport> DiagnosticsAsync(
        ProtocolTestClient client,
        string uri,
        CancellationToken cancellationToken) => client.Rpc.InvokeWithParameterObjectAsync<FullDocumentDiagnosticReport>(
            "textDocument/diagnostic",
            new DocumentDiagnosticParams { TextDocument = new TextDocumentIdentifier { Uri = uri } },
            cancellationToken);

    private static Task OpenAsync(ProtocolTestClient client, string uri, string text) =>
        client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didOpen",
            new DidOpenTextDocumentParams
            {
                TextDocument = new TextDocumentItem
                {
                    Uri = uri,
                    LanguageId = "cerneala",
                    Version = 1,
                    Text = text
                }
            });

    private static Task ChangeAsync(ProtocolTestClient client, string uri, int version, string text) =>
        client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didChange",
            new DidChangeTextDocumentParams
            {
                TextDocument = new VersionedTextDocumentIdentifier { Uri = uri, Version = version },
                ContentChanges = [new TextDocumentContentChangeEvent { Text = text }]
            });

    private static TextDocumentPositionParams Request(string uri, LspPosition position) => new()
    {
        TextDocument = new TextDocumentIdentifier { Uri = uri },
        Position = position
    };

    private static LspPosition PositionAt(string text, int offset)
    {
        int line = 0;
        int lineStart = 0;
        for (int index = 0; index < offset; index++)
        {
            if (text[index] == '\n')
            {
                line++;
                lineStart = index + 1;
            }
        }

        return new LspPosition { Line = line, Character = offset - lineStart };
    }

    private sealed class TemporaryHardeningWorkspace : IDisposable
    {
        private TemporaryHardeningWorkspace(string rootPath, string projectPath, string firstMarkupPath, string secondMarkupPath)
        {
            RootPath = rootPath;
            ProjectPath = projectPath;
            FirstMarkupPath = firstMarkupPath;
            SecondMarkupPath = secondMarkupPath;
        }

        public string RootPath { get; }

        public string ProjectPath { get; }

        public string FirstMarkupPath { get; }

        public string SecondMarkupPath { get; }

        public string FirstUri => new Uri(FirstMarkupPath).AbsoluteUri;

        public string SecondUri => new Uri(SecondMarkupPath).AbsoluteUri;

        public static TemporaryHardeningWorkspace Create()
        {
            string repositoryRoot = FindRepositoryRoot();
            string root = Path.Combine(Path.GetTempPath(), "cerneala-hardening-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            string project = Path.Combine(root, "Fixture.csproj");
            string first = Path.Combine(root, "View.cui.xml");
            string second = Path.Combine(root, "Second.cui.xml");
            XDocument projectDocument = new(
                new XElement("Project",
                    new XAttribute("Sdk", "Microsoft.NET.Sdk"),
                    new XElement("PropertyGroup",
                        new XElement("TargetFramework", "net10.0-windows"),
                        new XElement("AssemblyName", "HardeningProtocolFixture")),
                    new XElement("ItemGroup",
                        new XElement("ProjectReference", new XAttribute("Include", Path.Combine(repositoryRoot, "Cerneala.csproj"))),
                        new XElement("AdditionalFiles", new XAttribute("Include", "View.cui.xml")),
                        new XElement("AdditionalFiles", new XAttribute("Include", "Second.cui.xml")))));
            projectDocument.Save(project);
            File.WriteAllText(Path.Combine(root, "Views.cs"), """
                using Cerneala.UI.Controls;
                namespace Fixture;
                public sealed partial class View : Window { }
                public sealed partial class Second : Window { }
                """);
            string markup = CreateMarkup();
            File.WriteAllText(first, markup);
            File.WriteAllText(second, markup);
            return new TemporaryHardeningWorkspace(root, project, first, second);
        }

        public void Dispose()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }

        private static string CreateMarkup()
        {
            StringBuilder builder = new("<Window Title=\"Version 0\">\n  <StackPanel>\n");
            for (int index = 0; index < 100; index++)
            {
                builder.Append("    <Button Width=\"").Append(index + 1).Append("\" />\n");
            }

            return builder.Append("  </StackPanel>\n</Window>").ToString();
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate the Cerneala repository root.");
        }
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
