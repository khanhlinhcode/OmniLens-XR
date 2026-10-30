using System;
using OmniLensXR.S03;

namespace OmniLensXR.S04
{
    public readonly struct SpatialTargetDescriptor
    {
        private SpatialTargetDescriptor(int targetId, string actionId, bool isEnabled)
        {
            TargetId = targetId;
            ActionId = actionId;
            IsEnabled = isEnabled;
        }

        public int TargetId { get; }

        public string ActionId { get; }

        public bool IsEnabled { get; }

        public static bool TryCreate(
            int targetId,
            string actionId,
            bool isEnabled,
            out SpatialTargetDescriptor target
        )
        {
            target = default;
            if (
                targetId <= 0
                || string.IsNullOrWhiteSpace(actionId)
                || actionId.Length > 64
            )
            {
                return false;
            }

            foreach (char character in actionId)
            {
                if (
                    !char.IsLetterOrDigit(character)
                    && character != '-'
                    && character != '_'
                )
                {
                    return false;
                }
            }

            target = new SpatialTargetDescriptor(targetId, actionId, isEnabled);
            return true;
        }
    }

    public readonly struct SpatialFocusStep
    {
        public SpatialFocusStep(
            GazeDwellPhase phase,
            float progress,
            string selectedActionId,
            bool focusCanceled
        )
        {
            Phase = phase;
            Progress = progress;
            SelectedActionId = selectedActionId;
            FocusCanceled = focusCanceled;
        }

        public GazeDwellPhase Phase { get; }

        public float Progress { get; }

        public string SelectedActionId { get; }

        public bool FocusCanceled { get; }

        public bool SelectionRequested => SelectedActionId != null;
    }

    /// <summary>
    /// Spatial focus composes the proven S03 dwell state rather than implementing a
    /// competing timer or repeat policy.
    /// </summary>
    public sealed class SpatialFocusState
    {
        private readonly GazeDwellStateMachine _dwellState;

        public SpatialFocusState(double dwellSeconds, double cooldownSeconds)
        {
            _dwellState = new GazeDwellStateMachine(dwellSeconds, cooldownSeconds);
        }

        public SpatialFocusStep Advance(
            SpatialTargetDescriptor? target,
            double deltaSeconds,
            bool interactionEnabled
        )
        {
            int? targetId = target.HasValue && target.Value.IsEnabled
                ? target.Value.TargetId
                : null;
            GazeDwellStepResult result = _dwellState.Advance(
                targetId,
                deltaSeconds,
                interactionEnabled
            );
            string selectedAction = result.SelectionTriggered && target.HasValue
                ? target.Value.ActionId
                : null;
            return new SpatialFocusStep(
                result.Phase,
                result.Progress,
                selectedAction,
                result.FocusCanceled
            );
        }

        public void Cancel()
        {
            _dwellState.Reset();
        }
    }
}

