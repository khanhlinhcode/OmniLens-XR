using System;
using System.Collections.Generic;
using UnityEngine;

namespace OmniLensXR.S05
{
    public enum PanelKind
    {
        Unknown = 0,
        Chat = 1,
        Science3D = 2,
        Document = 3,
        Notes = 4,
    }

    public enum PanelVisibility
    {
        Closed = 1,
        Visible = 2,
        Minimized = 3,
    }

    public enum PanelCommandKind
    {
        Unknown = 0,
        Register = 1,
        Open = 2,
        Close = 3,
        Minimize = 4,
        Restore = 5,
        Focus = 6,
        SetPinned = 7,
        Move = 8,
        Resize = 9,
    }

    public enum PanelApplyError
    {
        None = 0,
        InvalidCommand,
        StaleVersion,
        InvalidDefinition,
        DuplicatePanelId,
        PanelLimitReached,
        UnknownPanel,
        AlreadyInState,
        PanelNotVisible,
        PanelNotMinimized,
        ViewStateConflict,
        InvalidBounds,
        PanelOverlap,
    }

    public readonly struct PanelBounds
    {
        public PanelBounds(Vector3 positionMeters, Vector2 sizeMeters)
        {
            PositionMeters = positionMeters;
            SizeMeters = sizeMeters;
        }

        public Vector3 PositionMeters { get; }

        public Vector2 SizeMeters { get; }

        public bool IsValid()
        {
            if (
                !S04.SpatialUiTokens.IsFinite(PositionMeters)
                || !S04.SpatialUiTokens.IsValidPanelSize(SizeMeters)
            )
            {
                return false;
            }

            float radialDistance = PositionMeters.magnitude;
            if (
                PositionMeters.z <= 0.0f
                || radialDistance < S04.SpatialUiTokens.MinimumPanelDistanceMeters
                || radialDistance > S04.SpatialUiTokens.MaximumPanelDistanceMeters
            )
            {
                return false;
            }

            float horizontalDegrees = Mathf.Abs(
                Mathf.Atan2(PositionMeters.x, PositionMeters.z) * Mathf.Rad2Deg
            );
            float planarDistance = Mathf.Sqrt(
                PositionMeters.x * PositionMeters.x
                    + PositionMeters.z * PositionMeters.z
            );
            float verticalDegrees = Mathf.Abs(
                Mathf.Atan2(PositionMeters.y, planarDistance) * Mathf.Rad2Deg
            );
            return horizontalDegrees
                    <= S04.SpatialUiTokens.HorizontalSafeZoneDegrees * 0.5f
                && verticalDegrees
                    <= S04.SpatialUiTokens.VerticalSafeZoneDegrees * 0.5f;
        }
    }

    public sealed class PanelDefinition
    {
        private PanelDefinition(string panelId, PanelKind kind, PanelBounds bounds)
        {
            PanelId = panelId;
            Kind = kind;
            Bounds = bounds;
        }

        public string PanelId { get; }

        public PanelKind Kind { get; }

        public PanelBounds Bounds { get; }

        public static bool TryCreate(
            string panelId,
            PanelKind kind,
            PanelBounds bounds,
            out PanelDefinition definition
        )
        {
            definition = null;
            if (
                !IsValidPanelId(panelId)
                || kind == PanelKind.Unknown
                || !Enum.IsDefined(typeof(PanelKind), kind)
                || !bounds.IsValid()
            )
            {
                return false;
            }

            definition = new PanelDefinition(panelId, kind, bounds);
            return true;
        }

        public static bool IsValidPanelId(string panelId)
        {
            if (string.IsNullOrWhiteSpace(panelId) || panelId.Length > 64)
            {
                return false;
            }

            foreach (char character in panelId)
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

            return true;
        }
    }

    public sealed class PanelState
    {
        public PanelState(
            PanelDefinition definition,
            PanelVisibility visibility,
            int zOrder,
            bool isFocused,
            bool isPinned
        )
        {
            Definition = definition;
            Visibility = visibility;
            ZOrder = zOrder;
            IsFocused = isFocused;
            IsPinned = isPinned;
        }

        public PanelDefinition Definition { get; }

        public string PanelId => Definition.PanelId;

        public PanelKind Kind => Definition.Kind;

        public PanelBounds Bounds => Definition.Bounds;

        public PanelVisibility Visibility { get; }

        public int ZOrder { get; }

        public bool IsFocused { get; }

        public bool IsPinned { get; }

        public PanelState With(
            PanelVisibility? visibility = null,
            int? zOrder = null,
            bool? isFocused = null,
            bool? isPinned = null
        )
        {
            return new PanelState(
                Definition,
                visibility ?? Visibility,
                zOrder ?? ZOrder,
                isFocused ?? IsFocused,
                isPinned ?? IsPinned
            );
        }

        public PanelState WithDefinition(PanelDefinition definition)
        {
            if (definition == null || definition.PanelId != PanelId)
            {
                throw new ArgumentException(
                    "Replacement definition must preserve the panel ID.",
                    nameof(definition)
                );
            }

            return new PanelState(
                definition,
                Visibility,
                ZOrder,
                IsFocused,
                IsPinned
            );
        }
    }

    public readonly struct PanelCommand
    {
        private PanelCommand(
            PanelCommandKind kind,
            long expectedVersion,
            string panelId,
            PanelDefinition definition,
            bool boolValue,
            Vector3 positionMeters,
            Vector2 sizeMeters
        )
        {
            Kind = kind;
            ExpectedVersion = expectedVersion;
            PanelId = panelId;
            Definition = definition;
            BoolValue = boolValue;
            PositionMeters = positionMeters;
            SizeMeters = sizeMeters;
        }

        public PanelCommandKind Kind { get; }

        public long ExpectedVersion { get; }

        public string PanelId { get; }

        public PanelDefinition Definition { get; }

        public bool BoolValue { get; }

        public Vector3 PositionMeters { get; }

        public Vector2 SizeMeters { get; }

        public static PanelCommand Register(long expectedVersion, PanelDefinition definition)
        {
            return new PanelCommand(
                PanelCommandKind.Register,
                expectedVersion,
                definition?.PanelId,
                definition,
                false,
                Vector3.zero,
                Vector2.zero
            );
        }

        public static PanelCommand For(
            PanelCommandKind kind,
            long expectedVersion,
            string panelId,
            bool boolValue = false
        )
        {
            return new PanelCommand(
                kind,
                expectedVersion,
                panelId,
                null,
                boolValue,
                Vector3.zero,
                Vector2.zero
            );
        }

        public static PanelCommand Move(
            long expectedVersion,
            string panelId,
            Vector3 positionMeters
        )
        {
            return new PanelCommand(
                PanelCommandKind.Move,
                expectedVersion,
                panelId,
                null,
                false,
                positionMeters,
                Vector2.zero
            );
        }

        public static PanelCommand Resize(
            long expectedVersion,
            string panelId,
            Vector2 sizeMeters
        )
        {
            return new PanelCommand(
                PanelCommandKind.Resize,
                expectedVersion,
                panelId,
                null,
                false,
                Vector3.zero,
                sizeMeters
            );
        }
    }

    public sealed class PanelSnapshot
    {
        public PanelSnapshot(long version, IReadOnlyList<PanelState> panels)
        {
            Version = version;
            Panels = panels;
        }

        public long Version { get; }

        public IReadOnlyList<PanelState> Panels { get; }
    }

    public sealed class PanelLifecycleEvent
    {
        public PanelLifecycleEvent(
            long sequence,
            long registryVersion,
            PanelCommandKind kind,
            string panelId,
            IReadOnlyList<string> visiblePanelIds
        )
        {
            Sequence = sequence;
            RegistryVersion = registryVersion;
            Kind = kind;
            PanelId = panelId;
            VisiblePanelIds = visiblePanelIds;
        }

        public long Sequence { get; }

        public long RegistryVersion { get; }

        public PanelCommandKind Kind { get; }

        public string PanelId { get; }

        public IReadOnlyList<string> VisiblePanelIds { get; }
    }

    public readonly struct PanelApplyResult
    {
        public PanelApplyResult(
            bool accepted,
            PanelApplyError error,
            long version,
            PanelLifecycleEvent lifecycleEvent
        )
        {
            Accepted = accepted;
            Error = error;
            Version = version;
            LifecycleEvent = lifecycleEvent;
        }

        public bool Accepted { get; }

        public PanelApplyError Error { get; }

        public long Version { get; }

        public PanelLifecycleEvent LifecycleEvent { get; }
    }
}
