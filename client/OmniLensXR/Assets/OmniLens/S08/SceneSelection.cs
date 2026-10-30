using System;
using System.Collections.Generic;
using UnityEngine;

namespace OmniLensXR.S08
{
    public enum SceneSelectionAction
    {
        Unknown = 0,
        Select = 1,
        Isolate = 2,
        Highlight = 3,
        Focus = 4,
        Clear = 5,
    }

    public enum SceneSelectionError
    {
        None = 0,
        InvalidCommand,
        StaleVersion,
        UnknownPart,
        AlreadyInState,
        ViewMismatch,
    }

    public readonly struct SceneSelectionCommand
    {
        private SceneSelectionCommand(
            SceneSelectionAction action,
            long expectedVersion,
            string partId
        )
        {
            Action = action;
            ExpectedVersion = expectedVersion;
            PartId = partId;
        }

        public SceneSelectionAction Action { get; }
        public long ExpectedVersion { get; }
        public string PartId { get; }

        public static SceneSelectionCommand For(
            SceneSelectionAction action,
            long expectedVersion,
            string partId
        )
        {
            return new SceneSelectionCommand(action, expectedVersion, partId);
        }

        public static SceneSelectionCommand Clear(long expectedVersion)
        {
            return new SceneSelectionCommand(
                SceneSelectionAction.Clear,
                expectedVersion,
                null
            );
        }
    }

    public readonly struct SceneSelectionResult
    {
        public SceneSelectionResult(
            bool accepted,
            SceneSelectionError error,
            long version,
            string affectedPartId
        )
        {
            Accepted = accepted;
            Error = error;
            Version = version;
            AffectedPartId = affectedPartId;
        }

        public bool Accepted { get; }
        public SceneSelectionError Error { get; }
        public long Version { get; }
        public string AffectedPartId { get; }
    }

    public sealed class SceneSelectionState
    {
        private readonly HashSet<string> _parts;

        public SceneSelectionState(IEnumerable<string> partIds)
        {
            if (partIds == null)
            {
                throw new ArgumentNullException(nameof(partIds));
            }

            _parts = new HashSet<string>(partIds, StringComparer.Ordinal);
            if (_parts.Count == 0)
            {
                throw new ArgumentException("At least one scene part is required.", nameof(partIds));
            }
        }

        public long Version { get; private set; }
        public string SelectedPartId { get; private set; }
        public string IsolatedPartId { get; private set; }
        public string HighlightedPartId { get; private set; }
        public string FocusedPartId { get; private set; }

        public SceneSelectionResult Apply(SceneSelectionCommand command)
        {
            if (
                command.Action == SceneSelectionAction.Unknown
                || !Enum.IsDefined(typeof(SceneSelectionAction), command.Action)
                || command.ExpectedVersion < 0
            )
            {
                return Reject(SceneSelectionError.InvalidCommand);
            }

            if (command.ExpectedVersion != Version)
            {
                return Reject(SceneSelectionError.StaleVersion);
            }

            if (
                command.Action != SceneSelectionAction.Clear
                && !_parts.Contains(command.PartId)
            )
            {
                return Reject(SceneSelectionError.UnknownPart);
            }

            bool changed;
            switch (command.Action)
            {
                case SceneSelectionAction.Select:
                    changed = SelectedPartId != command.PartId;
                    SelectedPartId = command.PartId;
                    break;
                case SceneSelectionAction.Isolate:
                    changed = IsolatedPartId != command.PartId
                        || SelectedPartId != command.PartId;
                    IsolatedPartId = command.PartId;
                    SelectedPartId = command.PartId;
                    break;
                case SceneSelectionAction.Highlight:
                    changed = HighlightedPartId != command.PartId;
                    HighlightedPartId = command.PartId;
                    break;
                case SceneSelectionAction.Focus:
                    changed = FocusedPartId != command.PartId
                        || SelectedPartId != command.PartId;
                    FocusedPartId = command.PartId;
                    SelectedPartId = command.PartId;
                    break;
                case SceneSelectionAction.Clear:
                    changed = SelectedPartId != null
                        || IsolatedPartId != null
                        || HighlightedPartId != null
                        || FocusedPartId != null;
                    SelectedPartId = null;
                    IsolatedPartId = null;
                    HighlightedPartId = null;
                    FocusedPartId = null;
                    break;
                default:
                    return Reject(SceneSelectionError.InvalidCommand);
            }

            if (!changed)
            {
                return Reject(SceneSelectionError.AlreadyInState);
            }

            Version += 1;
            return new SceneSelectionResult(
                true,
                SceneSelectionError.None,
                Version,
                command.PartId
            );
        }

        private SceneSelectionResult Reject(SceneSelectionError error)
        {
            return new SceneSelectionResult(false, error, Version, null);
        }
    }

    public sealed class ScenePartView : MonoBehaviour
    {
        public string PartId { get; private set; }
        public bool IsSelected { get; private set; }
        public bool IsHighlighted { get; private set; }
        public bool IsFocused { get; private set; }

        public void Initialize(string partId)
        {
            if (!SceneManifestCodec.IsSafeId(partId))
            {
                throw new ArgumentException("Invalid scene part ID.", nameof(partId));
            }

            PartId = partId;
        }

        public void Synchronize(SceneSelectionState state, bool visible)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            gameObject.SetActive(visible);
            IsSelected = state.SelectedPartId == PartId;
            IsHighlighted = state.HighlightedPartId == PartId;
            IsFocused = state.FocusedPartId == PartId;
        }
    }

    public sealed class SceneSelectionController
    {
        private readonly SceneManifestData _manifest;
        private readonly Dictionary<string, ScenePartView> _views;
        private readonly Dictionary<string, string> _parents;

        public SceneSelectionController(
            SceneManifestData manifest,
            IReadOnlyDictionary<string, ScenePartView> views
        )
        {
            if (!SceneManifestCodec.TryValidate(manifest, out _))
            {
                throw new ArgumentException("A valid manifest is required.", nameof(manifest));
            }

            _manifest = manifest;
            _views = new Dictionary<string, ScenePartView>(StringComparer.Ordinal);
            _parents = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (ScenePartData part in manifest.parts)
            {
                if (
                    views == null
                    || !views.TryGetValue(part.partId, out ScenePartView view)
                    || view == null
                    || view.PartId != part.partId
                )
                {
                    throw new ArgumentException(
                        $"Missing view for scene part {part.partId}.",
                        nameof(views)
                    );
                }

                _views.Add(part.partId, view);
                _parents.Add(part.partId, part.parentPartId);
            }

            State = new SceneSelectionState(_views.Keys);
            Synchronize();
        }

        public SceneSelectionState State { get; }

        public SceneSelectionResult Apply(SceneSelectionCommand command)
        {
            SceneSelectionResult result = State.Apply(command);
            if (result.Accepted)
            {
                Synchronize();
            }

            return result;
        }

        private void Synchronize()
        {
            foreach (ScenePartData part in _manifest.parts)
            {
                bool visible = State.IsolatedPartId == null
                    || IsRelated(part.partId, State.IsolatedPartId);
                _views[part.partId].Synchronize(State, visible);
            }
        }

        private bool IsRelated(string candidate, string isolated)
        {
            return IsAncestorOrSelf(candidate, isolated)
                || IsAncestorOrSelf(isolated, candidate);
        }

        private bool IsAncestorOrSelf(string ancestor, string descendant)
        {
            string current = descendant;
            while (!string.IsNullOrEmpty(current))
            {
                if (current == ancestor)
                {
                    return true;
                }

                current = _parents[current];
            }

            return false;
        }
    }
}
