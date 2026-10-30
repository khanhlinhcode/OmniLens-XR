using System;

namespace OmniLensXR.S03
{
    public enum GazeDwellPhase
    {
        Idle,
        Focusing,
        Selected,
        Cooldown,
        AwaitingExit,
    }

    public readonly struct GazeDwellStepResult
    {
        public GazeDwellStepResult(
            GazeDwellPhase phase,
            float progress,
            bool selectionTriggered,
            bool focusCanceled
        )
        {
            Phase = phase;
            Progress = progress;
            SelectionTriggered = selectionTriggered;
            FocusCanceled = focusCanceled;
        }

        public GazeDwellPhase Phase { get; }

        public float Progress { get; }

        public bool SelectionTriggered { get; }

        public bool FocusCanceled { get; }
    }

    /// <summary>
    /// Pure fail-closed dwell state. One uninterrupted focus may produce at most one
    /// selection; the target must be exited before the state machine can re-arm it.
    /// </summary>
    public sealed class GazeDwellStateMachine
    {
        public const double MinimumDwellSeconds = 0.5;
        public const double MaximumDwellSeconds = 5.0;
        public const double MaximumCooldownSeconds = 5.0;

        private readonly double _dwellSeconds;
        private readonly double _cooldownSeconds;
        private int? _focusedTargetId;
        private int? _lockedTargetId;
        private double _focusedSeconds;
        private double _cooldownRemainingSeconds;

        public GazeDwellStateMachine(double dwellSeconds, double cooldownSeconds)
        {
            if (!IsFinite(dwellSeconds)
                || dwellSeconds < MinimumDwellSeconds
                || dwellSeconds > MaximumDwellSeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(dwellSeconds));
            }

            if (!IsFinite(cooldownSeconds)
                || cooldownSeconds < 0.0
                || cooldownSeconds > MaximumCooldownSeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(cooldownSeconds));
            }

            _dwellSeconds = dwellSeconds;
            _cooldownSeconds = cooldownSeconds;
        }

        public GazeDwellStepResult Advance(
            int? targetId,
            double deltaSeconds,
            bool interactionEnabled
        )
        {
            if (!IsFinite(deltaSeconds) || deltaSeconds < 0.0)
            {
                bool canceled = HasActiveState();
                Reset();
                return Result(GazeDwellPhase.Idle, 0.0, false, canceled);
            }

            if (!interactionEnabled || !targetId.HasValue)
            {
                bool canceled = HasActiveState();
                Reset();
                return Result(GazeDwellPhase.Idle, 0.0, false, canceled);
            }

            if (_lockedTargetId.HasValue)
            {
                if (_lockedTargetId.Value == targetId.Value)
                {
                    _cooldownRemainingSeconds = Math.Max(
                        0.0,
                        _cooldownRemainingSeconds - deltaSeconds
                    );
                    GazeDwellPhase lockedPhase = _cooldownRemainingSeconds > 0.0
                        ? GazeDwellPhase.Cooldown
                        : GazeDwellPhase.AwaitingExit;
                    return Result(lockedPhase, 1.0, false, false);
                }

                Reset();
            }

            bool focusCanceled = false;
            if (!_focusedTargetId.HasValue || _focusedTargetId.Value != targetId.Value)
            {
                focusCanceled = _focusedTargetId.HasValue;
                _focusedTargetId = targetId;
                _focusedSeconds = 0.0;
            }

            _focusedSeconds += deltaSeconds;
            double progress = Math.Min(1.0, _focusedSeconds / _dwellSeconds);
            if (_focusedSeconds < _dwellSeconds)
            {
                return Result(
                    GazeDwellPhase.Focusing,
                    progress,
                    false,
                    focusCanceled
                );
            }

            _lockedTargetId = targetId;
            _focusedTargetId = null;
            _focusedSeconds = 0.0;
            _cooldownRemainingSeconds = _cooldownSeconds;
            return Result(GazeDwellPhase.Selected, 1.0, true, focusCanceled);
        }

        public void Reset()
        {
            _focusedTargetId = null;
            _lockedTargetId = null;
            _focusedSeconds = 0.0;
            _cooldownRemainingSeconds = 0.0;
        }

        private bool HasActiveState()
        {
            return _focusedTargetId.HasValue || _lockedTargetId.HasValue;
        }

        private static GazeDwellStepResult Result(
            GazeDwellPhase phase,
            double progress,
            bool selectionTriggered,
            bool focusCanceled
        )
        {
            return new GazeDwellStepResult(
                phase,
                (float)Math.Max(0.0, Math.Min(1.0, progress)),
                selectionTriggered,
                focusCanceled
            );
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
