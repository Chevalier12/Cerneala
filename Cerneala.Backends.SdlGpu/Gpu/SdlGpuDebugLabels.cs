using Cerneala.Platforms.Sdl3;

namespace Cerneala.Backends.SdlGpu;

internal sealed class SdlGpuDebugLabels
{
    private readonly ISdlApi api;
    private readonly bool enabled;

    public SdlGpuDebugLabels(ISdlApi api, bool enabled)
    {
        this.api = api ?? throw new ArgumentNullException(nameof(api));
        this.enabled = enabled;
    }

    public IDisposable Push(nint commandBuffer, string label)
    {
        Validate(commandBuffer, label);
        if (!enabled)
        {
            return DebugGroupScope.Disabled;
        }

        api.PushGpuDebugGroup(commandBuffer, label);
        return new DebugGroupScope(api, commandBuffer);
    }

    public void Insert(nint commandBuffer, string label)
    {
        Validate(commandBuffer, label);
        if (enabled)
        {
            api.InsertGpuDebugLabel(commandBuffer, label);
        }
    }

    private static void Validate(nint commandBuffer, string label)
    {
        if (commandBuffer == 0)
        {
            throw new ArgumentException("An SDL GPU command buffer cannot be zero.", nameof(commandBuffer));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(label);
    }

    private sealed class DebugGroupScope : IDisposable
    {
        public static DebugGroupScope Disabled { get; } = new();

        private ISdlApi? api;
        private readonly nint commandBuffer;

        private DebugGroupScope()
        {
        }

        public DebugGroupScope(ISdlApi api, nint commandBuffer)
        {
            this.api = api;
            this.commandBuffer = commandBuffer;
        }

        public void Dispose() =>
            Interlocked.Exchange(ref api, null)?.PopGpuDebugGroup(commandBuffer);
    }
}
