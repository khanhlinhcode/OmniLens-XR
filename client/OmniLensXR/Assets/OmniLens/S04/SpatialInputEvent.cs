using System;
using UnityEngine;

namespace OmniLensXR.S04
{
    public enum SpatialInputSourceKind
    {
        Unknown = 0,
        Cardboard = 1,
        Simulator = 2,
        Controller = 3,
        System = 4,
    }

    public enum SpatialInputEventKind
    {
        Unknown = 0,
        HeadPose = 1,
        GazeEnter = 2,
        GazeExit = 3,
        TriggerPressed = 4,
        Back = 5,
        Home = 6,
        LongPress = 7,
        Recenter = 8,
        Pause = 9,
        Resume = 10,
        SourceLost = 11,
        SourceRestored = 12,
    }

    public readonly struct SpatialHeadPose
    {
        public SpatialHeadPose(Vector3 positionMeters, Quaternion rotation)
        {
            PositionMeters = positionMeters;
            Rotation = rotation;
        }

        public Vector3 PositionMeters { get; }

        public Quaternion Rotation { get; }

        public bool IsValid()
        {
            if (
                !SpatialUiTokens.IsFinite(PositionMeters)
                || !SpatialUiTokens.IsFinite(Rotation.x)
                || !SpatialUiTokens.IsFinite(Rotation.y)
                || !SpatialUiTokens.IsFinite(Rotation.z)
                || !SpatialUiTokens.IsFinite(Rotation.w)
            )
            {
                return false;
            }

            float squaredMagnitude = Rotation.x * Rotation.x
                + Rotation.y * Rotation.y
                + Rotation.z * Rotation.z
                + Rotation.w * Rotation.w;
            return squaredMagnitude
                    >= SpatialUiTokens.MinimumQuaternionSquaredMagnitude
                && squaredMagnitude
                    <= SpatialUiTokens.MaximumQuaternionSquaredMagnitude;
        }
    }

    public readonly struct SpatialInputEvent
    {
        private SpatialInputEvent(
            long sequence,
            double timestampSeconds,
            SpatialInputEventKind kind,
            SpatialInputSourceKind source,
            SpatialHeadPose? headPose
        )
        {
            Sequence = sequence;
            TimestampSeconds = timestampSeconds;
            Kind = kind;
            Source = source;
            HeadPose = headPose;
        }

        public long Sequence { get; }

        public double TimestampSeconds { get; }

        public SpatialInputEventKind Kind { get; }

        public SpatialInputSourceKind Source { get; }

        public SpatialHeadPose? HeadPose { get; }

        public static bool TryCreate(
            long sequence,
            double timestampSeconds,
            SpatialInputEventKind kind,
            SpatialInputSourceKind source,
            SpatialHeadPose? headPose,
            out SpatialInputEvent inputEvent
        )
        {
            inputEvent = default;
            bool timestampValid = !double.IsNaN(timestampSeconds)
                && !double.IsInfinity(timestampSeconds)
                && timestampSeconds >= 0.0;
            if (
                sequence <= 0
                || !timestampValid
                || kind == SpatialInputEventKind.Unknown
                || !Enum.IsDefined(typeof(SpatialInputEventKind), kind)
                || source == SpatialInputSourceKind.Unknown
                || !Enum.IsDefined(typeof(SpatialInputSourceKind), source)
            )
            {
                return false;
            }

            bool isHeadPose = kind == SpatialInputEventKind.HeadPose;
            if (isHeadPose != headPose.HasValue)
            {
                return false;
            }

            if (headPose.HasValue && !headPose.Value.IsValid())
            {
                return false;
            }

            inputEvent = new SpatialInputEvent(
                sequence,
                timestampSeconds,
                kind,
                source,
                headPose
            );
            return true;
        }
    }

    public interface ISpatialInputSource
    {
        SpatialInputSourceKind Source { get; }

        bool TryEmit(
            SpatialInputEventKind kind,
            double timestampSeconds,
            SpatialHeadPose? headPose,
            out SpatialInputEvent inputEvent
        );
    }

    public sealed class SpatialInputSimulator : ISpatialInputSource
    {
        private long _nextSequence = 1;

        public SpatialInputSourceKind Source => SpatialInputSourceKind.Simulator;

        public bool TryEmit(
            SpatialInputEventKind kind,
            double timestampSeconds,
            SpatialHeadPose? headPose,
            out SpatialInputEvent inputEvent
        )
        {
            if (
                !SpatialInputEvent.TryCreate(
                    _nextSequence,
                    timestampSeconds,
                    kind,
                    Source,
                    headPose,
                    out inputEvent
                )
            )
            {
                return false;
            }

            _nextSequence += 1;
            return true;
        }
    }

    public sealed class CardboardInputSource : ISpatialInputSource
    {
        private long _nextSequence = 1;

        public SpatialInputSourceKind Source => SpatialInputSourceKind.Cardboard;

        public bool TryEmit(
            SpatialInputEventKind kind,
            double timestampSeconds,
            SpatialHeadPose? headPose,
            out SpatialInputEvent inputEvent
        )
        {
            if (
                !CardboardInputEventFactory.TryCreate(
                    _nextSequence,
                    timestampSeconds,
                    kind,
                    headPose,
                    out inputEvent
                )
            )
            {
                return false;
            }

            _nextSequence += 1;
            return true;
        }
    }

    public static class CardboardInputEventFactory
    {
        public static bool TryCreate(
            long sequence,
            double timestampSeconds,
            SpatialInputEventKind kind,
            SpatialHeadPose? headPose,
            out SpatialInputEvent inputEvent
        )
        {
            return SpatialInputEvent.TryCreate(
                sequence,
                timestampSeconds,
                kind,
                SpatialInputSourceKind.Cardboard,
                headPose,
                out inputEvent
            );
        }
    }
}

