using System;
using System.Collections.Generic;
using OmniLensXR.S08;

namespace OmniLensXR.S09
{
    public enum AnimationPlaybackState
    {
        Idle = 1,
        Playing = 2,
        Paused = 3,
    }

    public enum AnimationControlError
    {
        None = 0,
        InvalidDefinition,
        InvalidCommand,
        StaleVersion,
        UnknownAnimation,
        AlreadyInState,
        NotPlaying,
        SeekOutOfBounds,
        SpeedOutOfBounds,
    }

    public sealed class SceneAnimationClipDefinition
    {
        public SceneAnimationClipDefinition(
            string animationName,
            string targetPartId,
            float durationSeconds
        )
        {
            AnimationName = animationName;
            TargetPartId = targetPartId;
            DurationSeconds = durationSeconds;
        }

        public string AnimationName { get; }
        public string TargetPartId { get; }
        public float DurationSeconds { get; }
    }

    public sealed class SceneAnimationCatalog
    {
        private readonly Dictionary<string, SceneAnimationClipDefinition> _clips;

        private SceneAnimationCatalog(
            Dictionary<string, SceneAnimationClipDefinition> clips
        )
        {
            _clips = clips;
        }

        public int Count => _clips.Count;

        public static bool TryCreate(
            SceneManifestData manifest,
            IReadOnlyDictionary<string, float> durationsSeconds,
            out SceneAnimationCatalog catalog,
            out AnimationControlError error
        )
        {
            catalog = null;
            error = AnimationControlError.None;
            if (
                !SceneManifestCodec.TryValidate(manifest, out _)
                || durationsSeconds == null
            )
            {
                error = AnimationControlError.InvalidDefinition;
                return false;
            }

            Dictionary<string, SceneAnimationClipDefinition> clips =
                new Dictionary<string, SceneAnimationClipDefinition>(
                    StringComparer.Ordinal
                );
            foreach (SceneAnimationData animation in manifest.animations)
            {
                if (
                    !durationsSeconds.TryGetValue(
                        animation.animationName,
                        out float durationSeconds
                    )
                    || !IsFinite(durationSeconds)
                    || durationSeconds <= 0.0f
                    || durationSeconds > 3600.0f
                )
                {
                    error = AnimationControlError.InvalidDefinition;
                    return false;
                }

                clips.Add(
                    animation.animationName,
                    new SceneAnimationClipDefinition(
                        animation.animationName,
                        animation.targetPartId,
                        durationSeconds
                    )
                );
            }

            if (clips.Count == 0 || clips.Count != durationsSeconds.Count)
            {
                error = AnimationControlError.InvalidDefinition;
                return false;
            }

            catalog = new SceneAnimationCatalog(clips);
            return true;
        }

        public bool TryGet(string animationName, out SceneAnimationClipDefinition clip)
        {
            clip = null;
            return animationName != null
                && _clips.TryGetValue(animationName, out clip);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    public readonly struct AnimationControlResult
    {
        public AnimationControlResult(
            bool accepted,
            AnimationControlError error,
            long version
        )
        {
            Accepted = accepted;
            Error = error;
            Version = version;
        }

        public bool Accepted { get; }
        public AnimationControlError Error { get; }
        public long Version { get; }
    }

    public sealed class AnimationControllerV1
    {
        public const float MinimumSpeed = 0.25f;
        public const float MaximumSpeed = 2.0f;

        private readonly SceneAnimationCatalog _catalog;

        public AnimationControllerV1(SceneAnimationCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            State = AnimationPlaybackState.Idle;
            Speed = 1.0f;
        }

        public long Version { get; private set; }
        public AnimationPlaybackState State { get; private set; }
        public string AnimationName { get; private set; }
        public string TargetPartId { get; private set; }
        public float PositionSeconds { get; private set; }
        public float DurationSeconds { get; private set; }
        public float Speed { get; private set; }

        public AnimationControlResult Play(long expectedVersion, string animationName)
        {
            if (!ValidVersion(expectedVersion))
            {
                return Reject(AnimationControlError.StaleVersion);
            }

            if (!_catalog.TryGet(animationName, out SceneAnimationClipDefinition clip))
            {
                return Reject(AnimationControlError.UnknownAnimation);
            }

            if (
                State == AnimationPlaybackState.Playing
                && AnimationName == animationName
            )
            {
                return Reject(AnimationControlError.AlreadyInState);
            }

            if (AnimationName != animationName)
            {
                PositionSeconds = 0.0f;
            }

            AnimationName = clip.AnimationName;
            TargetPartId = clip.TargetPartId;
            DurationSeconds = clip.DurationSeconds;
            State = AnimationPlaybackState.Playing;
            return Accept();
        }

        public AnimationControlResult Pause(long expectedVersion)
        {
            if (!ValidVersion(expectedVersion))
            {
                return Reject(AnimationControlError.StaleVersion);
            }

            if (State != AnimationPlaybackState.Playing)
            {
                return Reject(AnimationControlError.NotPlaying);
            }

            State = AnimationPlaybackState.Paused;
            return Accept();
        }

        public AnimationControlResult Seek(long expectedVersion, float positionSeconds)
        {
            if (!ValidVersion(expectedVersion))
            {
                return Reject(AnimationControlError.StaleVersion);
            }

            if (AnimationName == null)
            {
                return Reject(AnimationControlError.InvalidCommand);
            }

            if (
                !IsFinite(positionSeconds)
                || positionSeconds < 0.0f
                || positionSeconds > DurationSeconds
            )
            {
                return Reject(AnimationControlError.SeekOutOfBounds);
            }

            if (Math.Abs(PositionSeconds - positionSeconds) <= 0.0001f)
            {
                return Reject(AnimationControlError.AlreadyInState);
            }

            PositionSeconds = positionSeconds;
            return Accept();
        }

        public AnimationControlResult SetSpeed(long expectedVersion, float speed)
        {
            if (!ValidVersion(expectedVersion))
            {
                return Reject(AnimationControlError.StaleVersion);
            }

            if (!IsFinite(speed) || speed < MinimumSpeed || speed > MaximumSpeed)
            {
                return Reject(AnimationControlError.SpeedOutOfBounds);
            }

            if (Math.Abs(Speed - speed) <= 0.0001f)
            {
                return Reject(AnimationControlError.AlreadyInState);
            }

            Speed = speed;
            return Accept();
        }

        public AnimationControlResult Stop(long expectedVersion)
        {
            if (!ValidVersion(expectedVersion))
            {
                return Reject(AnimationControlError.StaleVersion);
            }

            if (State == AnimationPlaybackState.Idle && AnimationName == null)
            {
                return Reject(AnimationControlError.AlreadyInState);
            }

            State = AnimationPlaybackState.Idle;
            AnimationName = null;
            TargetPartId = null;
            PositionSeconds = 0.0f;
            DurationSeconds = 0.0f;
            return Accept();
        }

        public void Tick(float deltaSeconds)
        {
            if (
                State != AnimationPlaybackState.Playing
                || !IsFinite(deltaSeconds)
                || deltaSeconds <= 0.0f
            )
            {
                return;
            }

            PositionSeconds = Math.Min(
                DurationSeconds,
                PositionSeconds + deltaSeconds * Speed
            );
            if (PositionSeconds >= DurationSeconds)
            {
                State = AnimationPlaybackState.Paused;
            }
        }

        private bool ValidVersion(long expectedVersion)
        {
            return expectedVersion >= 0 && expectedVersion == Version;
        }

        private AnimationControlResult Accept()
        {
            Version += 1;
            return new AnimationControlResult(
                true,
                AnimationControlError.None,
                Version
            );
        }

        private AnimationControlResult Reject(AnimationControlError error)
        {
            return new AnimationControlResult(false, error, Version);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
