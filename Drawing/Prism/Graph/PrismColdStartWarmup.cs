using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Elements;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism.Graph;

internal static class PrismColdStartWarmup
{
    private static readonly Lazy<Task> warmup = new(StartCore);

    public static void Begin() => _ = warmup.Value;

    private static Task StartCore() =>
        Task.Factory.StartNew(
            WarmGraphPipeline,
            CancellationToken.None,
            TaskCreationOptions.DenyChildAttach |
                TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

    private static void WarmGraphPipeline()
    {
        WarmRuntimeAndRetainedPipeline();
        (DrawCommandList commands, PrismFrameAnalysis analysis) = CreateOuterGlowWorkload();
        PrismGraph graph =
            new PrismGraphBuilder().Build(analysis);
        PrismGraphExecutionPlan plan =
            new PrismGraphOptimizer().Optimize(graph);
        GC.KeepAlive(plan);
        GC.KeepAlive(commands);
    }

    internal static (DrawCommandList Commands, PrismFrameAnalysis Analysis) CreateOuterGlowWorkload()
    {
        PrismInstance instance = CreateOuterGlowInstance();
        WarmOuterGlowStateAccess(instance);
        return CreateOuterGlowWorkload(instance);
    }

    private static PrismInstance CreateOuterGlowInstance()
    {
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "ColdStart",
            styles: [new PrismStyleDefinition(PrismStyleId.OuterGlow)]);
        PrismCompositionDefinition definition = new(
            "ColdStart",
            [layer]);
        return new PrismInstance(definition);
    }

    private static (DrawCommandList Commands, PrismFrameAnalysis Analysis) CreateOuterGlowWorkload(
        PrismInstance instance)
    {
        PrismDrawScope scope = new(
            instance,
            new PrismCacheOwnerToken(1),
            new DrawRect(0, 0, 64, 64),
            Matrix3x2.Identity,
            1,
            1);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.BeginPrism(scope));
        commands.Add(DrawCommand.FillRectangle(
            new DrawRect(8, 8, 48, 48),
            Color.White));
        commands.Add(DrawCommand.EndPrism());

        PrismFrameAnalysis analysis =
            new PrismFrameAnalyzer().Analyze(commands);
        return (commands, analysis);
    }

    private static void WarmRuntimeAndRetainedPipeline()
    {
        UIRoot root = new(64, 64);
        UIElement owner = new();
        using IDisposable attachment = PrismAttachment.Set(
            owner,
            CreateOuterGlowInstance,
            Array.Empty<Func<PrismInstance, IDisposable>>());

        root.VisualChildren.Add(owner);
        root.ProcessFrame();
        DrawCommandList commands = root.RetainedRenderer.Commit(root);
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        GC.KeepAlive(analysis);
    }

    private static void WarmOuterGlowStateAccess(PrismInstance instance)
    {
        PrismStyleState state = instance
            .GetLayerState(new PrismNodeId(1))
            .Styles[0];
        PrismCatalogOperationInfo operation = PrismCatalog.GetStyle(PrismStyleId.OuterGlow);
        foreach (PrismCatalogParameterInfo parameter in operation.Parameters)
        {
            switch (parameter.ValueKind)
            {
                case PrismCatalogValueKind.Boolean:
                    state.SetValue(parameter, state.GetValue<bool>(parameter));
                    break;
                case PrismCatalogValueKind.Integer:
                    state.SetValue(parameter, state.GetValue<int>(parameter));
                    break;
                case PrismCatalogValueKind.Number:
                    state.SetValue(parameter, state.GetValue<float>(parameter));
                    break;
                case PrismCatalogValueKind.Color:
                    state.SetValue(parameter, state.GetValue<Color>(parameter));
                    break;
                case PrismCatalogValueKind.Vector:
                    state.SetValue(parameter, state.GetValue<Vector4>(parameter));
                    break;
                case PrismCatalogValueKind.Symbol:
                    state.SetValue(parameter, state.GetValue<string>(parameter));
                    break;
                case PrismCatalogValueKind.Resource:
                    state.SetValue(parameter, state.GetValue<PrismResourceId>(parameter));
                    break;
            }
        }
    }
}
