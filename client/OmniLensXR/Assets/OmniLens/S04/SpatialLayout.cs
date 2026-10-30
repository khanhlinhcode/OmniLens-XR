using System;
using System.Collections.Generic;
using UnityEngine;

namespace OmniLensXR.S04
{
    public readonly struct SpatialPanelSpec
    {
        public SpatialPanelSpec(
            string panelId,
            Vector3 positionMeters,
            Vector2 sizeMeters,
            bool isPrimary
        )
            : this(
                panelId,
                positionMeters,
                sizeMeters,
                isPrimary,
                SpatialLayoutGeometry.FacingYawDegrees(positionMeters)
            )
        {
        }

        public SpatialPanelSpec(
            string panelId,
            Vector3 positionMeters,
            Vector2 sizeMeters,
            bool isPrimary,
            float yawDegrees
        )
        {
            PanelId = panelId;
            PositionMeters = positionMeters;
            SizeMeters = sizeMeters;
            IsPrimary = isPrimary;
            YawDegrees = yawDegrees;
        }

        public string PanelId { get; }

        public Vector3 PositionMeters { get; }

        public Vector2 SizeMeters { get; }

        public bool IsPrimary { get; }

        public float YawDegrees { get; }
    }

    public sealed class SpatialLayoutDefinition
    {
        public SpatialLayoutDefinition(
            int schemaVersion,
            SpatialWireframeKind kind,
            SpatialLayoutMode mode,
            IReadOnlyList<SpatialPanelSpec> panels
        )
        {
            SchemaVersion = schemaVersion;
            Kind = kind;
            Mode = mode;
            Panels = panels == null
                ? null
                : new List<SpatialPanelSpec>(panels).AsReadOnly();
        }

        public int SchemaVersion { get; }

        public SpatialWireframeKind Kind { get; }

        public SpatialLayoutMode Mode { get; }

        public IReadOnlyList<SpatialPanelSpec> Panels { get; }
    }

    public enum SpatialLayoutError
    {
        None = 0,
        MissingLayout,
        UnsupportedVersion,
        UnsupportedKind,
        UnsupportedMode,
        MissingPanels,
        TooManyPanels,
        InvalidPanelId,
        DuplicatePanelId,
        InvalidPanelSize,
        InvalidPanelPosition,
        InvalidPanelOrientation,
        OutsideSafeZone,
        MissingPrimaryPanel,
        MultiplePrimaryPanels,
        PanelOverlap,
    }

    public readonly struct SpatialLayoutValidationResult
    {
        public SpatialLayoutValidationResult(SpatialLayoutError error, string panelId)
        {
            Error = error;
            PanelId = panelId;
        }

        public SpatialLayoutError Error { get; }

        public string PanelId { get; }

        public bool IsValid => Error == SpatialLayoutError.None;
    }

    public static class SpatialLayoutValidator
    {
        public static SpatialLayoutValidationResult Validate(
            SpatialLayoutDefinition layout
        )
        {
            if (layout == null)
            {
                return Error(SpatialLayoutError.MissingLayout);
            }

            if (layout.SchemaVersion != SpatialUiTokens.SchemaVersion)
            {
                return Error(SpatialLayoutError.UnsupportedVersion);
            }

            if (!Enum.IsDefined(typeof(SpatialWireframeKind), layout.Kind))
            {
                return Error(SpatialLayoutError.UnsupportedKind);
            }

            if (!Enum.IsDefined(typeof(SpatialLayoutMode), layout.Mode))
            {
                return Error(SpatialLayoutError.UnsupportedMode);
            }

            if (layout.Panels == null || layout.Panels.Count == 0)
            {
                return Error(SpatialLayoutError.MissingPanels);
            }

            if (layout.Panels.Count > SpatialUiTokens.MaximumPanelsPerLayout)
            {
                return Error(SpatialLayoutError.TooManyPanels);
            }

            HashSet<string> identifiers = new HashSet<string>(StringComparer.Ordinal);
            int primaryCount = 0;
            for (int index = 0; index < layout.Panels.Count; index += 1)
            {
                SpatialPanelSpec panel = layout.Panels[index];
                if (!IsValidIdentifier(panel.PanelId))
                {
                    return Error(SpatialLayoutError.InvalidPanelId, panel.PanelId);
                }

                if (!identifiers.Add(panel.PanelId))
                {
                    return Error(SpatialLayoutError.DuplicatePanelId, panel.PanelId);
                }

                if (!SpatialUiTokens.IsValidPanelSize(panel.SizeMeters))
                {
                    return Error(SpatialLayoutError.InvalidPanelSize, panel.PanelId);
                }

                if (!SpatialUiTokens.IsFinite(panel.PositionMeters))
                {
                    return Error(SpatialLayoutError.InvalidPanelPosition, panel.PanelId);
                }

                if (
                    !SpatialUiTokens.IsFinite(panel.YawDegrees)
                    || Mathf.Abs(
                        Mathf.DeltaAngle(
                            panel.YawDegrees,
                            SpatialLayoutGeometry.FacingYawDegrees(
                                panel.PositionMeters
                            )
                        )
                    ) > SpatialUiTokens.MaximumPanelFacingErrorDegrees
                )
                {
                    return Error(
                        SpatialLayoutError.InvalidPanelOrientation,
                        panel.PanelId
                    );
                }

                if (!IsInsideSafeZone(panel.PositionMeters))
                {
                    return Error(SpatialLayoutError.OutsideSafeZone, panel.PanelId);
                }

                if (panel.IsPrimary)
                {
                    primaryCount += 1;
                }
                for (int previous = 0; previous < index; previous += 1)
                {
                    if (PanelsOverlap(panel, layout.Panels[previous]))
                    {
                        return Error(SpatialLayoutError.PanelOverlap, panel.PanelId);
                    }
                }
            }

            if (primaryCount == 0)
            {
                return Error(SpatialLayoutError.MissingPrimaryPanel);
            }

            return primaryCount == 1
                ? Error(SpatialLayoutError.None)
                : Error(SpatialLayoutError.MultiplePrimaryPanels);
        }

        private static bool IsValidIdentifier(string panelId)
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

        private static bool IsInsideSafeZone(Vector3 position)
        {
            float radialDistance = position.magnitude;
            if (
                position.z <= 0.0f
                || radialDistance < SpatialUiTokens.MinimumPanelDistanceMeters
                || radialDistance > SpatialUiTokens.MaximumPanelDistanceMeters
            )
            {
                return false;
            }

            float horizontal = Mathf.Abs(
                Mathf.Atan2(position.x, position.z) * Mathf.Rad2Deg
            );
            float planarDistance = Mathf.Sqrt(
                position.x * position.x + position.z * position.z
            );
            float vertical = Mathf.Abs(
                Mathf.Atan2(position.y, planarDistance) * Mathf.Rad2Deg
            );
            return horizontal <= SpatialUiTokens.HorizontalSafeZoneDegrees * 0.5f
                && vertical <= SpatialUiTokens.VerticalSafeZoneDegrees * 0.5f;
        }

        private static bool PanelsOverlap(
            SpatialPanelSpec first,
            SpatialPanelSpec second
        )
        {
            if (Mathf.Abs(first.PositionMeters.z - second.PositionMeters.z) > 0.25f)
            {
                return false;
            }

            float horizontalGap = Mathf.Abs(
                    first.PositionMeters.x - second.PositionMeters.x
                )
                - (first.SizeMeters.x + second.SizeMeters.x) * 0.5f;
            float verticalGap = Mathf.Abs(
                    first.PositionMeters.y - second.PositionMeters.y
                )
                - (first.SizeMeters.y + second.SizeMeters.y) * 0.5f;
            return horizontalGap < SpatialUiTokens.MinimumPanelGapMeters
                && verticalGap < SpatialUiTokens.MinimumPanelGapMeters;
        }

        private static SpatialLayoutValidationResult Error(
            SpatialLayoutError error,
            string panelId = null
        )
        {
            return new SpatialLayoutValidationResult(error, panelId);
        }
    }

    public static class SpatialLayoutGeometry
    {
        public static float FacingYawDegrees(Vector3 positionMeters)
        {
            // Unity's built-in Quad front face points along local -Z.
            return Mathf.Atan2(positionMeters.x, positionMeters.z)
                * Mathf.Rad2Deg;
        }

        public static Vector3 CurvedPosition(
            float horizontalMeters,
            float verticalMeters,
            float radialDistanceMeters
        )
        {
            float remainingSquared = radialDistanceMeters * radialDistanceMeters
                - horizontalMeters * horizontalMeters
                - verticalMeters * verticalMeters;
            float forwardMeters = remainingSquared > 0.0f
                ? Mathf.Sqrt(remainingSquared)
                : float.NaN;
            return new Vector3(horizontalMeters, verticalMeters, forwardMeters);
        }
    }

    public static class SpatialWireframeFactory
    {
        public static SpatialLayoutDefinition Create(
            SpatialWireframeKind kind,
            SpatialLayoutMode mode
        )
        {
            float distance = mode == SpatialLayoutMode.Stereo
                ? 2.5f
                : SpatialUiTokens.DefaultPanelDistanceMeters;
            switch (kind)
            {
                case SpatialWireframeKind.HomeLauncher:
                    return Layout(
                        kind,
                        mode,
                        new SpatialPanelSpec(
                            "home-launcher",
                            SpatialLayoutGeometry.CurvedPosition(0.0f, 0.0f, distance),
                            new Vector2(1.5f, 1.0f),
                            true
                        )
                    );
                case SpatialWireframeKind.ThreePanelWorkspace:
                    return Layout(
                        kind,
                        mode,
                        new SpatialPanelSpec(
                            "workspace-left",
                            SpatialLayoutGeometry.CurvedPosition(-1.05f, 0.0f, distance),
                            new Vector2(0.8f, 0.9f),
                            false
                        ),
                        new SpatialPanelSpec(
                            "workspace-primary",
                            SpatialLayoutGeometry.CurvedPosition(0.0f, 0.0f, distance),
                            new Vector2(1.0f, 1.1f),
                            true
                        ),
                        new SpatialPanelSpec(
                            "workspace-right",
                            SpatialLayoutGeometry.CurvedPosition(1.05f, 0.0f, distance),
                            new Vector2(0.8f, 0.9f),
                            false
                        )
                    );
                case SpatialWireframeKind.SettingsAccessibility:
                    return Layout(
                        kind,
                        mode,
                        new SpatialPanelSpec(
                            "settings-primary",
                            SpatialLayoutGeometry.CurvedPosition(-0.65f, 0.0f, distance),
                            new Vector2(1.0f, 1.1f),
                            true
                        ),
                        new SpatialPanelSpec(
                            "accessibility",
                            SpatialLayoutGeometry.CurvedPosition(0.65f, 0.0f, distance),
                            new Vector2(1.0f, 1.1f),
                            false
                        )
                    );
                default:
                    return new SpatialLayoutDefinition(
                        SpatialUiTokens.SchemaVersion,
                        kind,
                        mode,
                        Array.Empty<SpatialPanelSpec>()
                    );
            }
        }

        private static SpatialLayoutDefinition Layout(
            SpatialWireframeKind kind,
            SpatialLayoutMode mode,
            params SpatialPanelSpec[] panels
        )
        {
            return new SpatialLayoutDefinition(
                SpatialUiTokens.SchemaVersion,
                kind,
                mode,
                panels
            );
        }
    }
}

