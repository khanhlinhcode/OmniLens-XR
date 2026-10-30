using System;
using System.Collections.Generic;
using OmniLensXR.S05;
using UnityEngine;

namespace OmniLensXR.S06
{
    public enum PanelPlacementError
    {
        None = 0,
        InvalidInput,
        OutsideSupportedPlane,
        NoCollisionFreePlacement,
    }

    public readonly struct PanelPlacementResult
    {
        public PanelPlacementResult(
            bool accepted,
            PanelPlacementError error,
            PanelBounds bounds
        )
        {
            Accepted = accepted;
            Error = error;
            Bounds = bounds;
        }

        public bool Accepted { get; }

        public PanelPlacementError Error { get; }

        public PanelBounds Bounds { get; }
    }

    public static class PanelPlacementService
    {
        public const float SnapStepMeters = 0.05f;
        public const int MaximumSearchRings = 32;

        public static PanelPlacementResult Resolve(
            IReadOnlyList<PanelState> states,
            string movingPanelId,
            Vector3 desiredPosition,
            Vector2 desiredSize
        )
        {
            if (!PanelDefinition.IsValidPanelId(movingPanelId))
            {
                return Error(PanelPlacementError.InvalidInput);
            }

            List<PanelBounds> occupied = new List<PanelBounds>();
            if (states != null)
            {
                foreach (PanelState state in states)
                {
                    if (
                        state != null
                        && state.PanelId != movingPanelId
                        && state.Visibility == PanelVisibility.Visible
                    )
                    {
                        occupied.Add(state.Bounds);
                    }
                }
            }

            return Resolve(occupied, desiredPosition, desiredSize);
        }

        public static PanelPlacementResult Resolve(
            IReadOnlyList<PanelBounds> occupied,
            Vector3 desiredPosition,
            Vector2 desiredSize
        )
        {
            if (
                !S04.SpatialUiTokens.IsFinite(desiredPosition)
                || !S04.SpatialUiTokens.IsFinite(desiredSize)
            )
            {
                return Error(PanelPlacementError.InvalidInput);
            }

            Vector2 size = new Vector2(
                Snap(
                    Mathf.Clamp(
                        desiredSize.x,
                        S04.SpatialUiTokens.MinimumPanelWidthMeters,
                        S04.SpatialUiTokens.MaximumPanelWidthMeters
                    )
                ),
                Snap(
                    Mathf.Clamp(
                        desiredSize.y,
                        S04.SpatialUiTokens.MinimumPanelHeightMeters,
                        S04.SpatialUiTokens.MaximumPanelHeightMeters
                    )
                )
            );
            float z = Snap(
                Mathf.Clamp(
                    desiredPosition.z,
                    S04.SpatialUiTokens.MinimumPanelDistanceMeters,
                    S04.SpatialUiTokens.MaximumPanelDistanceMeters
                )
            );
            float horizontalLimit = Mathf.Tan(
                    S04.SpatialUiTokens.HorizontalSafeZoneDegrees
                        * 0.5f
                        * Mathf.Deg2Rad
                )
                * z
                - size.x * 0.5f;
            float verticalLimit = Mathf.Tan(
                    S04.SpatialUiTokens.VerticalSafeZoneDegrees
                        * 0.5f
                        * Mathf.Deg2Rad
                )
                * z
                - size.y * 0.5f;
            float gridHorizontalLimit = Mathf.Floor(
                    horizontalLimit / SnapStepMeters
                )
                * SnapStepMeters;
            float gridVerticalLimit = Mathf.Floor(verticalLimit / SnapStepMeters)
                * SnapStepMeters;
            if (gridHorizontalLimit < 0.0f || gridVerticalLimit < 0.0f)
            {
                return Error(PanelPlacementError.OutsideSupportedPlane);
            }

            Vector3 origin = new Vector3(
                Mathf.Clamp(Snap(desiredPosition.x), -gridHorizontalLimit, gridHorizontalLimit),
                Mathf.Clamp(Snap(desiredPosition.y), -gridVerticalLimit, gridVerticalLimit),
                z
            );
            float maximumLateralDistance = Mathf.Sqrt(
                Mathf.Max(
                    0.0f,
                    S04.SpatialUiTokens.MaximumPanelDistanceMeters
                            * S04.SpatialUiTokens.MaximumPanelDistanceMeters
                        - z * z
                )
            );
            Vector2 lateral = new Vector2(origin.x, origin.y);
            if (lateral.magnitude > maximumLateralDistance)
            {
                Vector2 scaled = lateral.normalized * maximumLateralDistance;
                origin.x = SnapTowardZero(scaled.x);
                origin.y = SnapTowardZero(scaled.y);
            }

            PanelBounds direct = new PanelBounds(origin, size);
            if (IsCandidateAvailable(direct, occupied))
            {
                return Accepted(direct);
            }

            for (int ring = 1; ring <= MaximumSearchRings; ring += 1)
            {
                for (int x = -ring; x <= ring; x += 1)
                {
                    if (
                        TryOffset(
                            origin,
                            size,
                            x,
                            -ring,
                            gridHorizontalLimit,
                            gridVerticalLimit,
                            occupied,
                            out PanelBounds bottom
                        )
                    )
                    {
                        return Accepted(bottom);
                    }

                    if (
                        TryOffset(
                            origin,
                            size,
                            x,
                            ring,
                            gridHorizontalLimit,
                            gridVerticalLimit,
                            occupied,
                            out PanelBounds top
                        )
                    )
                    {
                        return Accepted(top);
                    }
                }

                for (int y = -ring + 1; y < ring; y += 1)
                {
                    if (
                        TryOffset(
                            origin,
                            size,
                            -ring,
                            y,
                            gridHorizontalLimit,
                            gridVerticalLimit,
                            occupied,
                            out PanelBounds left
                        )
                    )
                    {
                        return Accepted(left);
                    }

                    if (
                        TryOffset(
                            origin,
                            size,
                            ring,
                            y,
                            gridHorizontalLimit,
                            gridVerticalLimit,
                            occupied,
                            out PanelBounds right
                        )
                    )
                    {
                        return Accepted(right);
                    }
                }
            }

            return Error(PanelPlacementError.NoCollisionFreePlacement);
        }

        public static bool IsFullyInsideSafeZone(PanelBounds bounds)
        {
            if (!bounds.IsValid())
            {
                return false;
            }

            float z = bounds.PositionMeters.z;
            float horizontalExtent = Mathf.Tan(
                    S04.SpatialUiTokens.HorizontalSafeZoneDegrees
                        * 0.5f
                        * Mathf.Deg2Rad
                )
                * z;
            float verticalExtent = Mathf.Tan(
                    S04.SpatialUiTokens.VerticalSafeZoneDegrees
                        * 0.5f
                        * Mathf.Deg2Rad
                )
                * z;
            return Mathf.Abs(bounds.PositionMeters.x) + bounds.SizeMeters.x * 0.5f
                    <= horizontalExtent + 0.0001f
                && Mathf.Abs(bounds.PositionMeters.y) + bounds.SizeMeters.y * 0.5f
                    <= verticalExtent + 0.0001f;
        }

        public static bool Overlaps(PanelBounds first, PanelBounds second)
        {
            if (
                Mathf.Abs(first.PositionMeters.z - second.PositionMeters.z) > 0.25f
            )
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
            return horizontalGap < S04.SpatialUiTokens.MinimumPanelGapMeters
                && verticalGap < S04.SpatialUiTokens.MinimumPanelGapMeters;
        }

        public static bool IsSnapped(PanelBounds bounds)
        {
            return IsSnapped(bounds.PositionMeters.x)
                && IsSnapped(bounds.PositionMeters.y)
                && IsSnapped(bounds.PositionMeters.z)
                && IsSnapped(bounds.SizeMeters.x)
                && IsSnapped(bounds.SizeMeters.y);
        }

        private static bool TryOffset(
            Vector3 origin,
            Vector2 size,
            int xSteps,
            int ySteps,
            float horizontalLimit,
            float verticalLimit,
            IReadOnlyList<PanelBounds> occupied,
            out PanelBounds bounds
        )
        {
            Vector3 position = new Vector3(
                origin.x + xSteps * SnapStepMeters,
                origin.y + ySteps * SnapStepMeters,
                origin.z
            );
            bounds = new PanelBounds(position, size);
            return Mathf.Abs(position.x) <= horizontalLimit + 0.0001f
                && Mathf.Abs(position.y) <= verticalLimit + 0.0001f
                && IsCandidateAvailable(bounds, occupied);
        }

        private static bool IsCandidateAvailable(
            PanelBounds candidate,
            IReadOnlyList<PanelBounds> occupied
        )
        {
            if (!IsFullyInsideSafeZone(candidate))
            {
                return false;
            }

            if (occupied == null)
            {
                return true;
            }

            foreach (PanelBounds existing in occupied)
            {
                if (Overlaps(candidate, existing))
                {
                    return false;
                }
            }

            return true;
        }

        private static float Snap(float value)
        {
            return Mathf.Round(value / SnapStepMeters) * SnapStepMeters;
        }

        private static float SnapTowardZero(float value)
        {
            return Mathf.Sign(value)
                * Mathf.Floor(Mathf.Abs(value) / SnapStepMeters)
                * SnapStepMeters;
        }

        private static bool IsSnapped(float value)
        {
            return Mathf.Abs(value - Snap(value)) <= 0.0001f;
        }

        private static PanelPlacementResult Accepted(PanelBounds bounds)
        {
            return new PanelPlacementResult(
                true,
                PanelPlacementError.None,
                bounds
            );
        }

        private static PanelPlacementResult Error(PanelPlacementError error)
        {
            return new PanelPlacementResult(false, error, default);
        }
    }

    public sealed class S06PlacementFixture
    {
        public S06PlacementFixture(
            int index,
            PanelBounds primary,
            IReadOnlyList<PanelBounds> occupied
        )
        {
            Index = index;
            Primary = primary;
            Occupied = occupied;
        }

        public int Index { get; }

        public PanelBounds Primary { get; }

        public IReadOnlyList<PanelBounds> Occupied { get; }
    }

    public static class S06PlacementFixtureFactory
    {
        public static IReadOnlyList<S06PlacementFixture> CreateTwenty()
        {
            List<S06PlacementFixture> fixtures = new List<S06PlacementFixture>();
            for (int index = 0; index < 20; index += 1)
            {
                int column = index % 5;
                int row = index / 5;
                Vector3 desired = new Vector3(
                    (column - 2) * 1.0f,
                    (row - 1.5f) * 0.45f,
                    2.5f
                );
                List<PanelBounds> occupied = new List<PanelBounds>();
                if (index % 3 == 0)
                {
                    occupied.Add(
                        new PanelBounds(
                            new Vector3(0.0f, 0.0f, 2.5f),
                            new Vector2(0.65f, 0.65f)
                        )
                    );
                }

                PanelPlacementResult result = PanelPlacementService.Resolve(
                    occupied,
                    desired,
                    new Vector2(0.65f, 0.65f)
                );
                if (!result.Accepted)
                {
                    throw new InvalidOperationException(
                        $"S06 fixture {index} could not be placed: {result.Error}."
                    );
                }

                fixtures.Add(
                    new S06PlacementFixture(
                        index,
                        result.Bounds,
                        occupied.AsReadOnly()
                    )
                );
            }

            return fixtures.AsReadOnly();
        }
    }
}
