namespace Cerneala.SdlGpuSmoke;

internal sealed record SmokeOptions(
    string Mode,
    string ArtifactDirectory,
    bool CaptureScreenshots,
    bool RequireInput)
{
    private static readonly HashSet<string> Modes = new(StringComparer.OrdinalIgnoreCase)
    {
        "single-window",
        "multi-window",
        "input",
        "resize",
        "drawing",
        "rendersurface2d",
        "prism",
        "screenshot",
        "servo"
    };

    public static SmokeOptions Current { get; private set; } = new(
        "single-window",
        Path.GetFullPath(Path.Combine("artifacts", "sdlgpu-smoke")),
        CaptureScreenshots: true,
        RequireInput: false);

    public static void Initialize(IReadOnlyList<string> args)
    {
        string mode = "single-window";
        string artifacts = Path.GetFullPath(Path.Combine("artifacts", "sdlgpu-smoke"));
        bool captureScreenshots = true;
        bool requireInput = false;

        for (int index = 0; index < args.Count; index++)
        {
            switch (args[index])
            {
                case "--mode" when index + 1 < args.Count:
                    mode = args[++index];
                    break;
                case "--artifacts" when index + 1 < args.Count:
                    artifacts = Path.GetFullPath(args[++index]);
                    break;
                case "--no-screenshot":
                    captureScreenshots = false;
                    break;
                case "--require-input":
                    requireInput = true;
                    break;
                default:
                    throw new ArgumentException(
                        $"Unknown or incomplete SDL_GPU smoke argument '{args[index]}'.");
            }
        }

        if (!Modes.Contains(mode))
        {
            throw new ArgumentException(
                $"Unknown SDL_GPU smoke mode '{mode}'. Expected one of: {string.Join(", ", Modes.Order())}.");
        }

        Current = new SmokeOptions(
            mode.ToLowerInvariant(),
            artifacts,
            captureScreenshots,
            requireInput);
    }
}
