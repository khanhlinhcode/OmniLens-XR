using System;
using System.Collections.Generic;

namespace OmniLensXR.S05
{
    public sealed class PanelRegistry
    {
        public const int MaximumPanels = 8;
        public const int MaximumEvents = 64;

        private readonly Dictionary<string, PanelState> _states =
            new Dictionary<string, PanelState>(StringComparer.Ordinal);
        private readonly List<string> _focusHistory = new List<string>();
        private readonly Queue<PanelLifecycleEvent> _events =
            new Queue<PanelLifecycleEvent>();
        private long _nextEventSequence = 1;
        private int _nextZOrder = 1;

        public long Version { get; private set; }

        public int Count => _states.Count;

        public int EventCount => _events.Count;

        public PanelApplyResult Apply(PanelCommand command)
        {
            if (
                command.Kind == PanelCommandKind.Unknown
                || !Enum.IsDefined(typeof(PanelCommandKind), command.Kind)
                || command.ExpectedVersion < 0
            )
            {
                return Reject(PanelApplyError.InvalidCommand);
            }

            if (command.ExpectedVersion != Version)
            {
                return Reject(PanelApplyError.StaleVersion);
            }

            PanelApplyError error;
            switch (command.Kind)
            {
                case PanelCommandKind.Register:
                    error = Register(command.Definition);
                    break;
                case PanelCommandKind.Open:
                    error = Open(command.PanelId);
                    break;
                case PanelCommandKind.Close:
                    error = Close(command.PanelId);
                    break;
                case PanelCommandKind.Minimize:
                    error = Minimize(command.PanelId);
                    break;
                case PanelCommandKind.Restore:
                    error = Restore(command.PanelId);
                    break;
                case PanelCommandKind.Focus:
                    error = Focus(command.PanelId);
                    break;
                case PanelCommandKind.SetPinned:
                    error = SetPinned(command.PanelId, command.BoolValue);
                    break;
                case PanelCommandKind.Move:
                    error = Move(command.PanelId, command.PositionMeters);
                    break;
                case PanelCommandKind.Resize:
                    error = Resize(command.PanelId, command.SizeMeters);
                    break;
                default:
                    error = PanelApplyError.InvalidCommand;
                    break;
            }

            if (error != PanelApplyError.None)
            {
                return Reject(error);
            }

            Version += 1;
            PanelLifecycleEvent lifecycleEvent = new PanelLifecycleEvent(
                _nextEventSequence,
                Version,
                command.Kind,
                command.PanelId,
                VisiblePanelIds()
            );
            _nextEventSequence += 1;
            _events.Enqueue(lifecycleEvent);
            while (_events.Count > MaximumEvents)
            {
                _events.Dequeue();
            }

            return new PanelApplyResult(
                true,
                PanelApplyError.None,
                Version,
                lifecycleEvent
            );
        }

        public bool TryGet(string panelId, out PanelState state)
        {
            state = null;
            return panelId != null && _states.TryGetValue(panelId, out state);
        }

        public PanelSnapshot CreateSnapshot()
        {
            List<PanelState> panels = new List<PanelState>(_states.Values);
            panels.Sort(ComparePanelState);
            return new PanelSnapshot(Version, panels.AsReadOnly());
        }

        public IReadOnlyList<PanelLifecycleEvent> Events()
        {
            return new List<PanelLifecycleEvent>(_events).AsReadOnly();
        }

        private PanelApplyError Register(PanelDefinition definition)
        {
            if (definition == null)
            {
                return PanelApplyError.InvalidDefinition;
            }

            if (_states.ContainsKey(definition.PanelId))
            {
                return PanelApplyError.DuplicatePanelId;
            }

            if (_states.Count >= MaximumPanels)
            {
                return PanelApplyError.PanelLimitReached;
            }

            _states.Add(
                definition.PanelId,
                new PanelState(
                    definition,
                    PanelVisibility.Closed,
                    0,
                    false,
                    false
                )
            );
            return PanelApplyError.None;
        }

        private PanelApplyError Open(string panelId)
        {
            if (!TryGetState(panelId, out PanelState state, out PanelApplyError error))
            {
                return error;
            }

            if (state.Visibility == PanelVisibility.Visible)
            {
                return PanelApplyError.AlreadyInState;
            }

            if (OverlapsVisiblePanel(panelId, state.Bounds))
            {
                return PanelApplyError.PanelOverlap;
            }

            SetOnlyFocused(panelId);
            _states[panelId] = state.With(
                visibility: PanelVisibility.Visible,
                zOrder: _nextZOrder,
                isFocused: true
            );
            _nextZOrder += 1;
            RememberFocus(panelId);
            return PanelApplyError.None;
        }

        private PanelApplyError Close(string panelId)
        {
            if (!TryGetState(panelId, out PanelState state, out PanelApplyError error))
            {
                return error;
            }

            if (state.Visibility == PanelVisibility.Closed)
            {
                return PanelApplyError.AlreadyInState;
            }

            bool restoreFocus = state.IsFocused;
            _states[panelId] = state.With(
                visibility: PanelVisibility.Closed,
                zOrder: 0,
                isFocused: false
            );
            _focusHistory.Remove(panelId);
            if (restoreFocus)
            {
                RestorePreviousFocus();
            }

            return PanelApplyError.None;
        }

        private PanelApplyError Minimize(string panelId)
        {
            if (!TryGetState(panelId, out PanelState state, out PanelApplyError error))
            {
                return error;
            }

            if (state.Visibility != PanelVisibility.Visible)
            {
                return PanelApplyError.PanelNotVisible;
            }

            bool restoreFocus = state.IsFocused;
            _states[panelId] = state.With(
                visibility: PanelVisibility.Minimized,
                isFocused: false
            );
            _focusHistory.Remove(panelId);
            if (restoreFocus)
            {
                RestorePreviousFocus();
            }

            return PanelApplyError.None;
        }

        private PanelApplyError Restore(string panelId)
        {
            if (!TryGetState(panelId, out PanelState state, out PanelApplyError error))
            {
                return error;
            }

            if (state.Visibility != PanelVisibility.Minimized)
            {
                return PanelApplyError.PanelNotMinimized;
            }

            if (OverlapsVisiblePanel(panelId, state.Bounds))
            {
                return PanelApplyError.PanelOverlap;
            }

            SetOnlyFocused(panelId);
            _states[panelId] = state.With(
                visibility: PanelVisibility.Visible,
                zOrder: _nextZOrder,
                isFocused: true
            );
            _nextZOrder += 1;
            RememberFocus(panelId);
            return PanelApplyError.None;
        }

        private PanelApplyError Focus(string panelId)
        {
            if (!TryGetState(panelId, out PanelState state, out PanelApplyError error))
            {
                return error;
            }

            if (state.Visibility != PanelVisibility.Visible)
            {
                return PanelApplyError.PanelNotVisible;
            }

            if (state.IsFocused)
            {
                return PanelApplyError.AlreadyInState;
            }

            SetOnlyFocused(panelId);
            _states[panelId] = state.With(zOrder: _nextZOrder, isFocused: true);
            _nextZOrder += 1;
            RememberFocus(panelId);
            return PanelApplyError.None;
        }

        private PanelApplyError SetPinned(string panelId, bool pinned)
        {
            if (!TryGetState(panelId, out PanelState state, out PanelApplyError error))
            {
                return error;
            }

            if (state.IsPinned == pinned)
            {
                return PanelApplyError.AlreadyInState;
            }

            _states[panelId] = state.With(isPinned: pinned);
            return PanelApplyError.None;
        }

        private PanelApplyError Move(string panelId, UnityEngine.Vector3 positionMeters)
        {
            if (!TryGetState(panelId, out PanelState state, out PanelApplyError error))
            {
                return error;
            }

            PanelBounds bounds = new PanelBounds(positionMeters, state.Bounds.SizeMeters);
            if (
                UnityEngine.Vector3.Distance(
                    positionMeters,
                    state.Bounds.PositionMeters
                ) <= 0.0001f
            )
            {
                return PanelApplyError.AlreadyInState;
            }

            if (
                !PanelDefinition.TryCreate(
                    panelId,
                    state.Kind,
                    bounds,
                    out PanelDefinition definition
                )
            )
            {
                return PanelApplyError.InvalidBounds;
            }

            if (
                state.Visibility == PanelVisibility.Visible
                && OverlapsVisiblePanel(panelId, bounds)
            )
            {
                return PanelApplyError.PanelOverlap;
            }

            _states[panelId] = state.WithDefinition(definition);
            return PanelApplyError.None;
        }

        private PanelApplyError Resize(string panelId, UnityEngine.Vector2 sizeMeters)
        {
            if (!TryGetState(panelId, out PanelState state, out PanelApplyError error))
            {
                return error;
            }

            PanelBounds bounds = new PanelBounds(state.Bounds.PositionMeters, sizeMeters);
            if (
                UnityEngine.Vector2.Distance(sizeMeters, state.Bounds.SizeMeters)
                <= 0.0001f
            )
            {
                return PanelApplyError.AlreadyInState;
            }

            if (
                !PanelDefinition.TryCreate(
                    panelId,
                    state.Kind,
                    bounds,
                    out PanelDefinition definition
                )
            )
            {
                return PanelApplyError.InvalidBounds;
            }

            if (
                state.Visibility == PanelVisibility.Visible
                && OverlapsVisiblePanel(panelId, bounds)
            )
            {
                return PanelApplyError.PanelOverlap;
            }

            _states[panelId] = state.WithDefinition(definition);
            return PanelApplyError.None;
        }

        private bool TryGetState(
            string panelId,
            out PanelState state,
            out PanelApplyError error
        )
        {
            state = null;
            if (!PanelDefinition.IsValidPanelId(panelId))
            {
                error = PanelApplyError.InvalidCommand;
                return false;
            }

            if (!_states.TryGetValue(panelId, out state))
            {
                error = PanelApplyError.UnknownPanel;
                return false;
            }

            error = PanelApplyError.None;
            return true;
        }

        private void SetOnlyFocused(string panelId)
        {
            List<string> identifiers = new List<string>(_states.Keys);
            foreach (string identifier in identifiers)
            {
                PanelState state = _states[identifier];
                if (state.IsFocused && identifier != panelId)
                {
                    _states[identifier] = state.With(isFocused: false);
                }
            }
        }

        private void RememberFocus(string panelId)
        {
            _focusHistory.Remove(panelId);
            _focusHistory.Add(panelId);
        }

        private void RestorePreviousFocus()
        {
            for (int index = _focusHistory.Count - 1; index >= 0; index -= 1)
            {
                string candidateId = _focusHistory[index];
                PanelState candidate = _states[candidateId];
                if (candidate.Visibility != PanelVisibility.Visible)
                {
                    _focusHistory.RemoveAt(index);
                    continue;
                }

                _states[candidateId] = candidate.With(
                    zOrder: _nextZOrder,
                    isFocused: true
                );
                _nextZOrder += 1;
                return;
            }
        }

        private IReadOnlyList<string> VisiblePanelIds()
        {
            List<string> identifiers = new List<string>();
            foreach (PanelState state in _states.Values)
            {
                if (state.Visibility == PanelVisibility.Visible)
                {
                    identifiers.Add(state.PanelId);
                }
            }

            identifiers.Sort(StringComparer.Ordinal);
            return identifiers.AsReadOnly();
        }

        private bool OverlapsVisiblePanel(string panelId, PanelBounds candidate)
        {
            foreach (PanelState state in _states.Values)
            {
                if (
                    state.PanelId == panelId
                    || state.Visibility != PanelVisibility.Visible
                )
                {
                    continue;
                }

                PanelBounds existing = state.Bounds;
                if (
                    Math.Abs(
                        candidate.PositionMeters.z - existing.PositionMeters.z
                    ) > 0.25f
                )
                {
                    continue;
                }

                float horizontalGap = Math.Abs(
                        candidate.PositionMeters.x - existing.PositionMeters.x
                    )
                    - (candidate.SizeMeters.x + existing.SizeMeters.x) * 0.5f;
                float verticalGap = Math.Abs(
                        candidate.PositionMeters.y - existing.PositionMeters.y
                    )
                    - (candidate.SizeMeters.y + existing.SizeMeters.y) * 0.5f;
                if (
                    horizontalGap < S04.SpatialUiTokens.MinimumPanelGapMeters
                    && verticalGap < S04.SpatialUiTokens.MinimumPanelGapMeters
                )
                {
                    return true;
                }
            }

            return false;
        }

        private static int ComparePanelState(PanelState first, PanelState second)
        {
            int zOrder = first.ZOrder.CompareTo(second.ZOrder);
            return zOrder != 0
                ? zOrder
                : string.Compare(first.PanelId, second.PanelId, StringComparison.Ordinal);
        }

        private PanelApplyResult Reject(PanelApplyError error)
        {
            return new PanelApplyResult(false, error, Version, null);
        }
    }
}
