using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Cerneala.Drawing;
using Cerneala.UI.Controls;

namespace Cerneala.Benchmarks;

internal static class CollisionStageTwoBenchmarkRunner
{
    private const int Seed = 0xC0111D3;
    private const int WarmupPasses = 8;
    private const int MeasurementPasses = 48;

    internal static void Run(string reportPath)
    {
        string fullPath = Path.GetFullPath(reportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        CollisionFixture[] fixtures = CollisionFixture.CreateAll(Seed);
        CollisionStageTwoScenarioReport[] scenarios = fixtures.Select(Measure).ToArray();
        CollisionStageTwoReport report = new(
            SchemaVersion: 1,
            TimestampUtc: DateTimeOffset.UtcNow,
            Runtime: RuntimeInformation.FrameworkDescription,
            OperatingSystem: RuntimeInformation.OSDescription,
            Processor: Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "unknown",
            LogicalProcessorCount: Environment.ProcessorCount,
            StopwatchFrequency: Stopwatch.Frequency,
            Seed,
            WarmupPasses,
            MeasurementPasses,
            Scenarios: scenarios,
            GatesPassed: scenarios.All(static scenario => scenario.GatePassed));

        File.WriteAllText(
            fullPath,
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);

        Console.WriteLine($"Collision stage-2 production benchmark: {fullPath}");
        foreach (CollisionStageTwoScenarioReport scenario in scenarios)
        {
            Console.WriteLine(
                $"{scenario.Scenario,-18} build={scenario.BuildMicroseconds,9:F2}us " +
                $"update-p95={scenario.UpdateP95Microseconds,9:F2}us query-p95={scenario.QueryP95Microseconds,9:F2}us " +
                $"candidates={scenario.AverageCandidates,8:F2} retained={scenario.EstimatedRetainedBytes,10}B " +
                $"false-negative={scenario.FalseNegatives} gate={(scenario.GatePassed ? "PASS" : "FAIL")}");
        }

        if (!report.GatesPassed)
        {
            Environment.ExitCode = 2;
        }
    }

    private static CollisionStageTwoScenarioReport Measure(CollisionFixture fixture)
    {
        for (int pass = 0; pass < WarmupPasses; pass++)
        {
            SparseCollisionGrid2D warmup = Build(fixture.Boxes);
            CollisionAabb[] warmupBoxes = fixture.Boxes.ToArray();
            ApplyUpdates(warmup, warmupBoxes, fixture, pass);
            ExerciseQueries(warmup, fixture.Queries);
        }

        long buildStarted = Stopwatch.GetTimestamp();
        SparseCollisionGrid2D grid = Build(fixture.Boxes);
        double buildMicroseconds = Stopwatch.GetElapsedTime(buildStarted).TotalMicroseconds;
        CollisionAabb[] current = fixture.Boxes.ToArray();
        double[] updateSamples = new double[MeasurementPasses];
        double[] querySamples = new double[MeasurementPasses];
        long candidateTotal = 0;
        int falseNegatives = 0;
        List<int> results = new(fixture.Boxes.Length);
        for (int pass = 0; pass < MeasurementPasses; pass++)
        {
            long updateStarted = Stopwatch.GetTimestamp();
            ApplyUpdates(grid, current, fixture, pass);
            updateSamples[pass] = Stopwatch.GetElapsedTime(updateStarted).TotalMicroseconds;

            long queryStarted = Stopwatch.GetTimestamp();
            foreach (CollisionAabb query in fixture.Queries)
            {
                results.Clear();
                grid.Query(ToDrawRect(query), results);
                candidateTotal += results.Count;
            }
            querySamples[pass] = Stopwatch.GetElapsedTime(queryStarted).TotalMicroseconds;
        }

        foreach (CollisionAabb query in fixture.Queries)
        {
            results.Clear();
            grid.Query(ToDrawRect(query), results);
            HashSet<int> actual = [.. results];
            for (int id = 0; id < current.Length; id++)
            {
                if (current[id].Intersects(query) && !actual.Contains(id))
                {
                    falseNegatives++;
                }
            }
        }

        Array.Sort(updateSamples);
        Array.Sort(querySamples);
        double updateP95 = Percentile(updateSamples, 0.95);
        double queryP95 = Percentile(querySamples, 0.95);
        bool gatePassed = falseNegatives == 0 && MeetsScenarioGate(
            fixture.Description.Name,
            updateP95,
            queryP95,
            grid.EstimatedRetainedBytes);
        return new CollisionStageTwoScenarioReport(
            fixture.Description.Name,
            buildMicroseconds,
            Percentile(updateSamples, 0.50),
            updateP95,
            Percentile(querySamples, 0.50),
            queryP95,
            (double)candidateTotal / (MeasurementPasses * fixture.Queries.Length),
            grid.EstimatedRetainedBytes,
            falseNegatives,
            gatePassed);
    }

    private static SparseCollisionGrid2D Build(IReadOnlyList<CollisionAabb> boxes)
    {
        SparseCollisionGrid2D grid = new();
        for (int id = 0; id < boxes.Count; id++)
        {
            grid.AddOrUpdate(id, ToDrawRect(boxes[id]));
        }

        return grid;
    }

    private static void ApplyUpdates(
        SparseCollisionGrid2D grid,
        CollisionAabb[] current,
        CollisionFixture fixture,
        int pass)
    {
        float direction = (pass & 1) == 0 ? 1 : -1;
        for (int update = 0; update < fixture.MovingIndices.Length; update++)
        {
            int id = fixture.MovingIndices[update];
            CollisionAabb original = fixture.Boxes[id];
            float multiplier = fixture.Description.FastMotion ? 128 : 0.75f;
            CollisionAabb moved = original.Translate(
                direction * multiplier,
                ((update & 1) * 2 - 1) * multiplier);
            current[id] = moved;
            grid.AddOrUpdate(id, ToDrawRect(moved));
        }
    }

    private static void ExerciseQueries(
        SparseCollisionGrid2D grid,
        IReadOnlyList<CollisionAabb> queries)
    {
        List<int> results = [];
        foreach (CollisionAabb query in queries)
        {
            grid.Query(ToDrawRect(query), results);
        }
    }

    private static bool MeetsScenarioGate(
        string scenario,
        double updateP95,
        double queryP95,
        long retainedBytes) => scenario switch
        {
            "large-sparse" => queryP95 <= 500 && updateP95 <= 150 && retainedBytes <= 1_500_000,
            "high-churn" => queryP95 <= 250 && updateP95 <= 1_000,
            "long-fence" => queryP95 <= 150,
            _ => true
        };

    private static DrawRect ToDrawRect(CollisionAabb box) =>
        new(box.MinX, box.MinY, box.MaxX - box.MinX, box.MaxY - box.MinY);

    private static double Percentile(double[] sorted, double percentile)
    {
        int index = (int)Math.Ceiling((sorted.Length * percentile) - 1);
        return sorted[Math.Clamp(index, 0, sorted.Length - 1)];
    }
}

internal sealed record CollisionStageTwoReport(
    int SchemaVersion,
    DateTimeOffset TimestampUtc,
    string Runtime,
    string OperatingSystem,
    string Processor,
    int LogicalProcessorCount,
    long StopwatchFrequency,
    int Seed,
    int WarmupPasses,
    int MeasurementPasses,
    IReadOnlyList<CollisionStageTwoScenarioReport> Scenarios,
    bool GatesPassed);

internal sealed record CollisionStageTwoScenarioReport(
    string Scenario,
    double BuildMicroseconds,
    double UpdateP50Microseconds,
    double UpdateP95Microseconds,
    double QueryP50Microseconds,
    double QueryP95Microseconds,
    double AverageCandidates,
    long EstimatedRetainedBytes,
    int FalseNegatives,
    bool GatePassed);
