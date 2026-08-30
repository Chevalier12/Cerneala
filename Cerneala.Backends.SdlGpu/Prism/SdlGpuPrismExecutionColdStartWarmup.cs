using System.Reflection;
using System.Runtime.CompilerServices;

namespace Cerneala.Backends.SdlGpu;

internal static class SdlGpuPrismExecutionColdStartWarmup
{
    private static readonly Lazy<Task> warmup = new(StartCore);

    public static void Begin() => _ = warmup.Value;

    public static void Complete() => warmup.Value.GetAwaiter().GetResult();

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
            typeof(SdlGpuPrismExecutor),
            typeof(SdlGpuPrismKernelSelector),
            typeof(SdlGpuPrismDeviceResources)
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
