using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

return await PresentationFrameBudgetRunner.RunAsync(args);

internal static class PresentationFrameBudgetRunner
{
    private const double WarmTargetPercentile = 0.99;

    private static readonly string[] ExpectedChapters =
    [
        "RETAINED MODEL",
        "BUILD-TIME MARKUP",
        "ASPECT DESIGN SYSTEM",
        "MOTION",
        "PRISM",
        "FRAME PIPELINE",
        "DIAGNOSTICS"
    ];

    public static async Task<int> RunAsync(string[] args)
    {
        RunnerOptions options;
        try
        {
            options = RunnerOptions.Parse(args);
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 2;
        }

        string repositoryRoot = FindRepositoryRoot();
        string presentationPath = Path.Combine(
            repositoryRoot,
            "CernealaPresentation",
            "bin",
            "Release",
            "net8.0-windows",
            "CernealaPresentation.exe");
        if (!File.Exists(presentationPath))
        {
            Console.Error.WriteLine($"Presentation executable was not built at '{presentationPath}'.");
            return 3;
        }

        string reportPath = options.ReportPath is null
            ? Path.Combine(
                repositoryRoot,
                "benchmarks",
                "artifacts",
                $"presentation-frame-budget-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.json")
            : Path.GetFullPath(options.ReportPath, repositoryRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        File.Delete(reportPath);
        File.Delete(reportPath + ".error.txt");

        using Process process = CreatePresentationProcess(
            presentationPath,
            reportPath,
            options.Cycles,
            options.FramesPerLoad);
        Stopwatch elapsed = Stopwatch.StartNew();
        process.Start();

        bool timedOut = false;
        using CancellationTokenSource timeout = new(options.Timeout);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            timedOut = true;
            TryKill(process);
            await process.WaitForExitAsync();
        }

        string standardOutput = await process.StandardOutput.ReadToEndAsync();
        string standardError = await process.StandardError.ReadToEndAsync();
        if (timedOut)
        {
            Console.Error.WriteLine($"Presentation exceeded timeout {options.Timeout}.");
            return 4;
        }

        string errorPath = reportPath + ".error.txt";
        if (File.Exists(errorPath))
        {
            Console.Error.WriteLine(await File.ReadAllTextAsync(errorPath));
            return 5;
        }

        if (process.ExitCode != 0 || !File.Exists(reportPath))
        {
            Console.Error.WriteLine(
                $"Presentation failed with exit code {process.ExitCode}.{Environment.NewLine}" +
                standardOutput +
                standardError);
            return 6;
        }

        FrameBudgetReport? report;
        try
        {
            await using FileStream input = File.OpenRead(reportPath);
            report = await JsonSerializer.DeserializeAsync<FrameBudgetReport>(
                input,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception exception) when (exception is IOException or JsonException)
        {
            Console.Error.WriteLine($"Could not read frame-budget report: {exception.Message}");
            return 7;
        }

        IReadOnlyList<string> errors = Validate(report, options);
        PrintSummary(report!, options, reportPath, elapsed.Elapsed);
        if (errors.Count == 0)
        {
            return 0;
        }

        Console.Error.WriteLine("Frame-budget gate failed:");
        foreach (string error in errors)
        {
            Console.Error.WriteLine($"  - {error}");
        }

        return 1;
    }

    private static Process CreatePresentationProcess(
        string presentationPath,
        string reportPath,
        int cycles,
        int framesPerLoad)
    {
        ProcessStartInfo startInfo = new(presentationPath)
        {
            WorkingDirectory = Path.GetDirectoryName(presentationPath)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = false
        };
        startInfo.Environment["CERNEALA_PRESENTATION_FRAME_BUDGET_REPORT"] = reportPath;
        startInfo.Environment["CERNEALA_PRESENTATION_FRAME_BUDGET_CYCLES"] =
            cycles.ToString(CultureInfo.InvariantCulture);
        startInfo.Environment["CERNEALA_PRESENTATION_FRAME_BUDGET_FRAMES_PER_LOAD"] =
            framesPerLoad.ToString(CultureInfo.InvariantCulture);
        return new Process { StartInfo = startInfo };
    }

    private static IReadOnlyList<string> Validate(FrameBudgetReport? report, RunnerOptions options)
    {
        List<string> errors = [];
        if (report is null)
        {
            errors.Add("Report deserialized to null.");
            return errors;
        }

        if (report.SchemaVersion != 1)
        {
            errors.Add($"Expected schema version 1, found {report.SchemaVersion}.");
        }

        if (report.Cycles != options.Cycles || report.FramesPerLoad != options.FramesPerLoad)
        {
            errors.Add(
                $"Report configuration is {report.Cycles} x {report.FramesPerLoad}, " +
                $"expected {options.Cycles} x {options.FramesPerLoad}.");
        }

        string[] actualChapters = report.Samples
            .Select(sample => sample.Chapter)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] expectedChapters = ExpectedChapters.Order(StringComparer.Ordinal).ToArray();
        if (!actualChapters.SequenceEqual(expectedChapters, StringComparer.Ordinal))
        {
            errors.Add(
                $"Expected exactly [{string.Join(", ", expectedChapters)}], " +
                $"found [{string.Join(", ", actualChapters)}].");
        }

        if (report.Samples.Any(sample => sample.Chapter.Contains("WELCOME", StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add("Welcome must not appear in frame-budget samples.");
        }

        foreach (string chapter in ExpectedChapters)
        {
            FrameBudgetSample[] chapterSamples = report.Samples
                .Where(sample => string.Equals(sample.Chapter, chapter, StringComparison.Ordinal))
                .ToArray();
            int expectedCount = options.Cycles * options.FramesPerLoad;
            if (chapterSamples.Length != expectedCount)
            {
                errors.Add($"{chapter}: expected {expectedCount} samples, found {chapterSamples.Length}.");
            }

            for (int cycle = 1; cycle <= options.Cycles; cycle++)
            {
                FrameBudgetSample[] load = chapterSamples
                    .Where(sample => sample.Cycle == cycle)
                    .OrderBy(sample => sample.FrameIndex)
                    .ToArray();
                if (load.Length != options.FramesPerLoad ||
                    !load.Select(sample => sample.FrameIndex).SequenceEqual(Enumerable.Range(0, options.FramesPerLoad)))
                {
                    errors.Add($"{chapter}, cycle {cycle}: frame indexes are incomplete.");
                }
            }
        }

        foreach (string chapter in ExpectedChapters)
        {
            FrameBudgetSample[] chapterSamples = report.Samples
                .Where(sample => string.Equals(
                    sample.Chapter,
                    chapter,
                    StringComparison.Ordinal))
                .ToArray();
            double coldMaximum = Maximum(
                chapterSamples.Where(sample => sample.IsCold).ToArray());
            if (coldMaximum > options.ColdBudgetMilliseconds)
            {
                errors.Add(
                    $"{chapter}: cold maximum {coldMaximum:0.###} ms exceeded " +
                    $"{options.ColdBudgetMilliseconds:0.####} ms.");
            }

            double warmPercentile = Percentile(
                chapterSamples.Where(sample => !sample.IsCold),
                WarmTargetPercentile);
            if (warmPercentile > options.BudgetMilliseconds)
            {
                errors.Add(
                    $"{chapter}: warm p99 {warmPercentile:0.###} ms exceeded " +
                    $"{options.BudgetMilliseconds:0.####} ms.");
            }
        }

        return errors;
    }

    private static void PrintSummary(
        FrameBudgetReport report,
        RunnerOptions options,
        string reportPath,
        TimeSpan elapsed)
    {
        Console.WriteLine(
            $"Warm p99 budget: {options.BudgetMilliseconds:0.####} ms | " +
            $"cold maximum: {options.ColdBudgetMilliseconds:0.####} ms | " +
            $"{report.Cycles} cycles x {report.FramesPerLoad} frames/load");
        Console.WriteLine($"Report: {reportPath}");
        foreach (string chapter in ExpectedChapters)
        {
            FrameBudgetSample[] samples = report.Samples
                .Where(sample => string.Equals(sample.Chapter, chapter, StringComparison.Ordinal))
                .ToArray();
            FrameBudgetSample[] cold = samples.Where(sample => sample.IsCold).ToArray();
            FrameBudgetSample[] warm = samples.Where(sample => !sample.IsCold).ToArray();
            Console.WriteLine(
                $"{chapter,-22} cold max {Maximum(cold),8:0.000} ms " +
                $"({OverBudget(cold, options.ColdBudgetMilliseconds),3} over) | " +
                $"warm p99 {Percentile(warm, WarmTargetPercentile),8:0.000} ms, " +
                $"max {Maximum(warm),8:0.000} ms " +
                $"({OverBudget(warm, options.BudgetMilliseconds),3} over target)");
        }

        foreach (FrameBudgetSample sample in report.Samples
            .Where(sample => sample.ProcessingTimeMs > options.BudgetMilliseconds)
            .OrderByDescending(sample => sample.ProcessingTimeMs))
        {
            FrameBudgetTiming timing = sample.Timing;
            Console.WriteLine(
                $"  spike {sample.Chapter}, cycle {sample.Cycle}, frame {sample.FrameIndex}: " +
                $"update {timing.RetainedUpdateMs:0.000} ms " +
                $"[prep {timing.UpdatePreparationMs:0.000}, " +
                $"scheduled {timing.ScheduledProcessingMs:0.000}, " +
                $"input {timing.InputDispatchMs:0.000}, " +
                $"input-work {timing.InputProcessingMs:0.000}, " +
                $"commit {timing.RetainedCommitMs:0.000}, " +
                $"cursor {timing.CursorPublicationMs:0.000}; " +
                $"phases inherited {timing.ScheduledInheritedMs:0.000}, " +
                $"command {timing.ScheduledCommandStateMs:0.000}, " +
                $"aspect {timing.ScheduledAspectMs:0.000}, " +
                $"measure {timing.ScheduledMeasureMs:0.000}, " +
                $"arrange {timing.ScheduledArrangeMs:0.000}, " +
                $"render {timing.ScheduledRenderMs:0.000}, " +
                $"hit-test {timing.ScheduledHitTestMs:0.000}, " +
                $"motion {timing.ScheduledMotionMs:0.000}], " +
                $"draw {timing.DrawingMs:0.000} ms " +
                $"[prepare {timing.DrawingPreparationMs:0.000}, " +
                $"requests {timing.TextRequestCollectionMs:0.000}, " +
                $"raster {timing.TextRasterizationMs:0.000}, " +
                $"atlas {timing.TextAtlasUploadMs:0.000}, " +
                $"commands {timing.CommandRenderingMs:0.000}, " +
                $"cleanup {timing.DrawingCleanupMs:0.000}; " +
                $"{timing.TextRequestCount} text requests, " +
                $"{timing.RasterizedPixelCount} pixels]; " +
                $"GC {sample.Gen0Collections}/{sample.Gen1Collections}/{sample.Gen2Collections}, " +
                $"{sample.AllocatedBytes} allocated bytes");
        }

        Console.WriteLine($"Process duration: {elapsed.TotalSeconds:0.0} s");
    }

    private static double Maximum(IReadOnlyCollection<FrameBudgetSample> samples) =>
        samples.Count == 0 ? 0 : samples.Max(sample => sample.ProcessingTimeMs);

    private static int OverBudget(
        IEnumerable<FrameBudgetSample> samples,
        double budgetMilliseconds) =>
        samples.Count(sample => sample.ProcessingTimeMs > budgetMilliseconds);

    private static double Percentile(
        IEnumerable<FrameBudgetSample> samples,
        double percentile)
    {
        double[] sorted = samples
            .Select(sample => sample.ProcessingTimeMs)
            .Order()
            .ToArray();
        if (sorted.Length == 0)
        {
            return 0;
        }

        int index = Math.Clamp(
            (int)Math.Ceiling(percentile * sorted.Length) - 1,
            0,
            sorted.Length - 1);
        return sorted[index];
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

        throw new InvalidOperationException("Could not locate the Cerneala repository root.");
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }
}

internal sealed record RunnerOptions(
    int Cycles,
    int FramesPerLoad,
    double BudgetMilliseconds,
    double ColdBudgetMilliseconds,
    TimeSpan Timeout,
    string? ReportPath)
{
    public static RunnerOptions Parse(string[] args)
    {
        int cycles = 8;
        int framesPerLoad = 45;
        double budgetMilliseconds = 16.6667;
        double coldBudgetMilliseconds = 500;
        TimeSpan timeout = TimeSpan.FromMinutes(4);
        string? reportPath = null;

        for (int index = 0; index < args.Length; index++)
        {
            string value = RequireValue(args, ref index);
            switch (args[index - 1])
            {
                case "--cycles":
                    cycles = ParseInt(value, "--cycles", 1, 100);
                    break;
                case "--frames-per-load":
                    framesPerLoad = ParseInt(value, "--frames-per-load", 1, 1_000);
                    break;
                case "--budget-ms":
                    budgetMilliseconds = ParseDouble(value, "--budget-ms", double.Epsilon, 60_000);
                    break;
                case "--cold-budget-ms":
                    coldBudgetMilliseconds = ParseDouble(
                        value,
                        "--cold-budget-ms",
                        double.Epsilon,
                        60_000);
                    break;
                case "--timeout-seconds":
                    timeout = TimeSpan.FromSeconds(
                        ParseDouble(value, "--timeout-seconds", 1, 3_600));
                    break;
                case "--report":
                    reportPath = value;
                    break;
                default:
                    throw new ArgumentException($"Unknown option '{args[index - 1]}'.");
            }
        }

        return new RunnerOptions(
            cycles,
            framesPerLoad,
            budgetMilliseconds,
            coldBudgetMilliseconds,
            timeout,
            reportPath);
    }

    private static string RequireValue(string[] args, ref int index)
    {
        string option = args[index];
        if (!option.StartsWith("--", StringComparison.Ordinal))
        {
            throw new ArgumentException($"Expected an option, found '{option}'.");
        }

        index++;
        if (index >= args.Length || args[index].StartsWith("--", StringComparison.Ordinal))
        {
            throw new ArgumentException($"Option '{option}' requires a value.");
        }

        return args[index];
    }

    private static int ParseInt(string value, string option, int minimum, int maximum)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) &&
            parsed >= minimum &&
            parsed <= maximum
            ? parsed
            : throw new ArgumentException(
                $"Option '{option}' must be between {minimum} and {maximum}.");
    }

    private static double ParseDouble(string value, string option, double minimum, double maximum)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) &&
            double.IsFinite(parsed) &&
            parsed >= minimum &&
            parsed <= maximum
            ? parsed
            : throw new ArgumentException(
                $"Option '{option}' must be between {minimum} and {maximum}.");
    }
}

internal sealed record FrameBudgetReport(
    int SchemaVersion,
    DateTimeOffset StartedUtc,
    int Cycles,
    int FramesPerLoad,
    IReadOnlyList<FrameBudgetSample> Samples);

internal sealed record FrameBudgetSample(
    int Cycle,
    string Chapter,
    int ChapterIndex,
    int FrameIndex,
    double ProcessingTimeMs,
    double ElapsedTimeMs,
    JsonElement FrameStats,
    FrameBudgetTiming Timing,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections,
    long AllocatedBytes,
    bool IsCold,
    double TimestampMs);

internal sealed record FrameBudgetTiming(
    double InputCollectionMs,
    double RetainedUpdateMs,
    double UpdatePreparationMs,
    double ScheduledProcessingMs,
    double InputDispatchMs,
    double InputProcessingMs,
    double RetainedCommitMs,
    double CursorPublicationMs,
    double ScheduledInheritedMs,
    double ScheduledCommandStateMs,
    double ScheduledAspectMs,
    double ScheduledMeasureMs,
    double ScheduledArrangeMs,
    double ScheduledRenderMs,
    double ScheduledHitTestMs,
    double ScheduledMotionMs,
    double BeginFrameMs,
    double DrawingMs,
    double DrawingPreparationMs,
    double TextRequestCollectionMs,
    double TextRasterizationMs,
    double TextAtlasUploadMs,
    double CommandRenderingMs,
    double DrawingCleanupMs,
    int TextRequestCount,
    long RasterizedPixelCount);
