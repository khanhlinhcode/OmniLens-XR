using UnityEngine;

namespace OmniLensXR.S04
{
    public enum SpatialLayoutMode
    {
        Mono = 1,
        Stereo = 2,
    }

    public enum SpatialWireframeKind
    {
        HomeLauncher = 1,
        ThreePanelWorkspace = 2,
        SettingsAccessibility = 3,
    }

    public enum SpatialTextTier
    {
        Large = 1,
        Body = 2,
        Detail = 3,
    }

    /// <summary>
    /// Provisional software candidates. Physical readability remains unverified until
    /// measured through the selected viewer.
    /// </summary>
    public static class SpatialUiTokens
    {
        public const int SchemaVersion = 1;
        public const int MaximumPanelsPerLayout = 8;
        public const float MinimumPanelDistanceMeters = 1.0f;
        public const float DefaultPanelDistanceMeters = 2.25f;
        public const float MaximumPanelDistanceMeters = 4.0f;
        public const float MinimumPanelWidthMeters = 0.35f;
        public const float MaximumPanelWidthMeters = 2.4f;
        public const float MinimumPanelHeightMeters = 0.25f;
        public const float MaximumPanelHeightMeters = 1.6f;
        public const float MinimumPanelGapMeters = 0.12f;
        public const float HorizontalSafeZoneDegrees = 80.0f;
        public const float VerticalSafeZoneDegrees = 55.0f;
        public const float MaximumPanelFacingErrorDegrees = 10.0f;
        public const float LargeTextHeightMeters = 0.060f;
        public const float BodyTextHeightMeters = 0.040f;
        public const float DetailTextHeightMeters = 0.028f;
        public const float MinimumFocusContrastRatio = 4.5f;
        public const float MinimumQuaternionSquaredMagnitude = 0.81f;
        public const float MaximumQuaternionSquaredMagnitude = 1.21f;

        public static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        public static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        public static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        public static bool IsValidPanelSize(Vector2 size)
        {
            return IsFinite(size)
                && size.x >= MinimumPanelWidthMeters
                && size.x <= MaximumPanelWidthMeters
                && size.y >= MinimumPanelHeightMeters
                && size.y <= MaximumPanelHeightMeters;
        }

        public static bool TryGetTextHeight(
            SpatialTextTier tier,
            out float heightMeters
        )
        {
            switch (tier)
            {
                case SpatialTextTier.Large:
                    heightMeters = LargeTextHeightMeters;
                    return true;
                case SpatialTextTier.Body:
                    heightMeters = BodyTextHeightMeters;
                    return true;
                case SpatialTextTier.Detail:
                    heightMeters = DetailTextHeightMeters;
                    return true;
                default:
                    heightMeters = 0.0f;
                    return false;
            }
        }
    }
}

