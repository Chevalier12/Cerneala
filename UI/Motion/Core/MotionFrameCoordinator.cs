using Cerneala.UI.Elements;

namespace Cerneala.UI.Motion.Core;

public sealed class MotionFrameCoordinator
{
    private readonly MotionSystem motion;
    private MotionFrameReason currentReason = MotionFrameReason.Scheduled;
    private bool sampledThisFrame;

    public MotionFrameCoordinator(UIRoot root, MotionSystem motion)
    {
        ArgumentNullException.ThrowIfNull(root);
        this.motion = motion ?? throw new ArgumentNullException(nameof(motion));
    }

    public MotionFrameResult BeginFrame(MotionFrameReason reason)
    {
        motion.VerifyAccess();
        currentReason = reason;
        sampledThisFrame = false;
        motion.Diagnostics.BeginFrame();
        MotionFramePhase phase = reason == MotionFrameReason.Input
            ? MotionFramePhase.AfterInput
            : MotionFramePhase.PreInput;
        motion.Diagnostics.RecordPhase(phase);
        return MotionFrameResult.Empty(new MotionFrame(default, default, 0, reason, phase));
    }

    public MotionFrameResult BeforeLayout()
    {
        motion.VerifyAccess();
        motion.Diagnostics.RecordPhase(MotionFramePhase.BeforeLayout);
        motion.Diagnostics.CaptureBeforeLayoutSnapshots();
        motion.Layout.CaptureFirstSnapshots();
        if (!motion.HasActiveMotion)
        {
            return MotionFrameResult.Empty(new MotionFrame(default, default, 0, currentReason, MotionFramePhase.BeforeLayout));
        }

        sampledThisFrame = true;
        return motion.Tick(currentReason, MotionFramePhase.BeforeLayout);
    }

    public MotionFrameResult AfterLayout()
    {
        motion.VerifyAccess();
        motion.Diagnostics.RecordPhase(MotionFramePhase.AfterLayout);
        motion.Diagnostics.CaptureAfterLayoutSnapshots();
        motion.Layout.CaptureLastSnapshotsAndStartCorrections();
        return MotionFrameResult.Empty(new MotionFrame(default, default, 0, currentReason, MotionFramePhase.AfterLayout));
    }

    public MotionFrameResult BeforeRender()
    {
        motion.VerifyAccess();
        motion.Diagnostics.RecordPhase(MotionFramePhase.BeforeRender);
        if (sampledThisFrame)
        {
            return MotionFrameResult.Empty(new MotionFrame(default, default, 0, currentReason, MotionFramePhase.BeforeRender));
        }

        sampledThisFrame = true;
        return motion.Tick(currentReason, MotionFramePhase.BeforeRender);
    }

    public MotionFrameResult EndFrame()
    {
        motion.VerifyAccess();
        motion.Diagnostics.RecordPhase(MotionFramePhase.AfterRender);
        return MotionFrameResult.Empty(new MotionFrame(default, default, 0, currentReason, MotionFramePhase.AfterRender));
    }
}
