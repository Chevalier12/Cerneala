using System.Reflection;
using System.Runtime.CompilerServices;
using Cerneala.Drawing.MonoGame.Prism.Kernels;
using Cerneala.Drawing.MonoGame.Prism.Surfaces;

namespace Cerneala.Drawing.MonoGame.Prism.Execution;

internal static class PrismExecutionColdStartWarmup
{
    private static readonly Lazy<Task> warmup = new(StartCore);

    public static void Begin() => _ = warmup.Value;

    private static Task StartCore() =>
        Task.Factory.StartNew(
            PrepareExecutionMethods,
            CancellationToken.None,
            TaskCreationOptions.DenyChildAttach |
                TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

    private static void PrepareExecutionMethods()
    {
        Type[] types =
        [
            typeof(PrismGraphExecutor),
            typeof(PrismKernelRegistry),
            typeof(PrismSurfacePool)
        ];
        const BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly;

        foreach (Type type in types)
        {
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                if (!method.ContainsGenericParameters)
                {
                    RuntimeHelpers.PrepareMethod(method.MethodHandle);
                }
            }
        }
    }
}
