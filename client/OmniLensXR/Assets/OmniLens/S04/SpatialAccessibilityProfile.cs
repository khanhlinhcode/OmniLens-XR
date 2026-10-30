using System;
using UnityEngine;

namespace OmniLensXR.S04
{
    public enum SpatialAccessibilityError
    {
        None = 0,
        UnsupportedTextTier,
        MissingStaticFocusIndicator,
        InvalidFocusColor,
        InsufficientFocusContrast,
        InvalidFocusScaleDelta,
        ReducedMotionUsesScaleAnimation,
    }

    public readonly struct SpatialAccessibilityProfile
    {
        public SpatialAccessibilityProfile(
            bool reducedMotion,
            bool staticFocusIndicator,
            SpatialTextTier minimumTextTier,
            Color focusColor,
            Color backgroundColor,
            float focusScaleDelta
        )
        {
            ReducedMotion = reducedMotion;
            StaticFocusIndicator = staticFocusIndicator;
            MinimumTextTier = minimumTextTier;
            FocusColor = focusColor;
            BackgroundColor = backgroundColor;
            FocusScaleDelta = focusScaleDelta;
        }

        public bool ReducedMotion { get; }

        public bool StaticFocusIndicator { get; }

        public SpatialTextTier MinimumTextTier { get; }

        public Color FocusColor { get; }

        public Color BackgroundColor { get; }

        public float FocusScaleDelta { get; }

        public static SpatialAccessibilityProfile ReducedMotionDefault()
        {
            return new SpatialAccessibilityProfile(
                true,
                true,
                SpatialTextTier.Body,
                Color.white,
                Color.black,
                0.0f
            );
        }
    }

    public static class SpatialAccessibilityValidator
    {
        public static SpatialAccessibilityError Validate(
            SpatialAccessibilityProfile profile
        )
        {
            if (!SpatialUiTokens.TryGetTextHeight(profile.MinimumTextTier, out _))
            {
                return SpatialAccessibilityError.UnsupportedTextTier;
            }

            if (!profile.StaticFocusIndicator)
            {
                return SpatialAccessibilityError.MissingStaticFocusIndicator;
            }

            if (
                !IsFinite(profile.FocusColor)
                || !IsFinite(profile.BackgroundColor)
            )
            {
                return SpatialAccessibilityError.InvalidFocusColor;
            }

            if (
                ContrastRatio(profile.FocusColor, profile.BackgroundColor)
                < SpatialUiTokens.MinimumFocusContrastRatio
            )
            {
                return SpatialAccessibilityError.InsufficientFocusContrast;
            }

            if (
                !SpatialUiTokens.IsFinite(profile.FocusScaleDelta)
                || profile.FocusScaleDelta < 0.0f
                || profile.FocusScaleDelta > 0.15f
            )
            {
                return SpatialAccessibilityError.InvalidFocusScaleDelta;
            }

            if (profile.ReducedMotion && profile.FocusScaleDelta > 0.0f)
            {
                return SpatialAccessibilityError.ReducedMotionUsesScaleAnimation;
            }

            return SpatialAccessibilityError.None;
        }

        public static float ContrastRatio(Color first, Color second)
        {
            float firstLuminance = RelativeLuminance(first);
            float secondLuminance = RelativeLuminance(second);
            float lighter = Mathf.Max(firstLuminance, secondLuminance);
            float darker = Mathf.Min(firstLuminance, secondLuminance);
            return (lighter + 0.05f) / (darker + 0.05f);
        }

        private static float RelativeLuminance(Color color)
        {
            return 0.2126f * Linearize(color.r)
                + 0.7152f * Linearize(color.g)
                + 0.0722f * Linearize(color.b);
        }

        private static float Linearize(float channel)
        {
            float clamped = Mathf.Clamp01(channel);
            return clamped <= 0.04045f
                ? clamped / 12.92f
                : Mathf.Pow((clamped + 0.055f) / 1.055f, 2.4f);
        }

        private static bool IsFinite(Color color)
        {
            return SpatialUiTokens.IsFinite(color.r)
                && SpatialUiTokens.IsFinite(color.g)
                && SpatialUiTokens.IsFinite(color.b)
                && SpatialUiTokens.IsFinite(color.a);
        }
    }
}

