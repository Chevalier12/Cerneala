using Cerneala.UI.Motion.Core;
using Cerneala.UI.Detective;
using Cerneala.UI.Motion.Interpolation;

namespace Cerneala.UI.Motion.Specs;

public sealed record MotionSpecContext(
    ReducedMotionPolicy ReducedMotion,
    ValueMixerRegistry Mixers,
    MotionDiagnostics? Diagnostics,
    TimeSpan Now,
    string? DebugName = null);
