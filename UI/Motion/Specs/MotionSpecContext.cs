using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Diagnostics;
using Cerneala.UI.Motion.Interpolation;

namespace Cerneala.UI.Motion.Specs;

public sealed record MotionSpecContext(
    ReducedMotionPolicy ReducedMotion,
    ValueMixerRegistry Mixers,
    MotionDiagnostics? Diagnostics,
    TimeSpan Now,
    string? DebugName = null);
