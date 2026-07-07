using Cerneala.UI.Elements;

namespace Cerneala.UI.Motion.Core;

public sealed class MotionFrameCoordinator
{
    private readonly MotionSystem motion;
    private MotionFrameReason currentReason = MotionFrameReason.Scheduled;

    public MotionFrameCoordinator(UIRoot root, MotionSystem motion)
    {
        ArgumentNullException.ThrowIfNull(root);
        this.motion = motion ?? throw new ArgumentNullException(nameof(motion));
    }

    public MotionFrameResult BeginFrame(MotionFrameReason reason)
    {
        motion.ThreadGuard.VerifyAccess();
        currentReason = reason;
        motion.Diagnostics.BeginFrame();
        MotionFramePhase phase = reason == MotionFrameReason.Input
            ? MotionFramePhase.AfterInput
            : MotionFramePhase.PreInput;
        motion.Diagnostics.RecordPhase(phase);
        return MotionFrameResult.Empty(new MotionFrame(default, default, 0, reason, phase));
    }

    public MotionFrameResult BeforeLayout()
    {
        motion.ThreadGuard.VerifyAccess();
        motion.Diagnostics.RecordPhase(MotionFramePhase.BeforeLayout);
        motion.Diagnostics.CaptureBeforeLayoutSnapshots();
        return MotionFrameResult.Empty(new MotionFrame(default, default, 0, currentReason, MotionFramePhase.BeforeLayout));
    }

    public MotionFrameResult AfterLayout()
    {
        motion.ThreadGuard.VerifyAccess();
        motion.Diagnostics.RecordPhase(MotionFramePhase.AfterLayout);
        motion.Diagnostics.CaptureAfterLayoutSnapshots();
        return MotionFrameResult.Empty(new MotionFrame(default, default, 0, currentReason, MotionFramePhase.AfterLayout));
    }

    public MotionFrameResult BeforeRender()
    {
        motion.ThreadGuard.VerifyAccess();
        motion.Diagnostics.RecordPhase(MotionFramePhase.BeforeRender);
        return motion.Tick(currentReason, MotionFramePhase.BeforeRender);
    }

    public MotionFrameResult EndFrame()
    {
        motion.ThreadGuard.VerifyAccess();
        motion.Diagnostics.RecordPhase(MotionFramePhase.AfterRender);
        return MotionFrameResult.Empty(new MotionFrame(default, default, 0, currentReason, MotionFramePhase.AfterRender));
    }
}
