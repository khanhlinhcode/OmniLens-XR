using System;
using OmniLensXR.S08;
using UnityEngine;

namespace OmniLensXR.S09
{
    public enum ModelViewerAction
    {
        Unknown = 0,
        SelectPart = 1,
        Zoom = 2,
        Rotate = 3,
        ToggleAnnotations = 4,
        Recenter = 5,
    }

    public enum ModelViewerHitRegion
    {
        Outside = 0,
        Panel = 1,
        Scene = 2,
    }

    public enum ModelViewerError
    {
        None = 0,
        InvalidCommand,
        StaleVersion,
        NotPanelHit,
        UnknownPart,
        SelectionRejected,
        ZoomOutOfBounds,
        RotationOutOfBounds,
        AlreadyInState,
    }

    public readonly struct ModelViewerCommand
    {
        private ModelViewerCommand(
            ModelViewerAction action,
            long expectedVersion,
            ModelViewerHitRegion hitRegion,
            string partId,
            float value
        )
        {
            Action = action;
            ExpectedVersion = expectedVersion;
            HitRegion = hitRegion;
            PartId = partId;
            Value = value;
        }

        public ModelViewerAction Action { get; }
        public long ExpectedVersion { get; }
        public ModelViewerHitRegion HitRegion { get; }
        public string PartId { get; }
        public float Value { get; }

        public static ModelViewerCommand Select(
            long expectedVersion,
            ModelViewerHitRegion hitRegion,
            string partId
        )
        {
            return new ModelViewerCommand(
                ModelViewerAction.SelectPart,
                expectedVersion,
                hitRegion,
                partId,
                0.0f
            );
        }

        public static ModelViewerCommand Number(
            ModelViewerAction action,
            long expectedVersion,
            ModelViewerHitRegion hitRegion,
            float value
        )
        {
            return new ModelViewerCommand(
                action,
                expectedVersion,
                hitRegion,
                null,
                value
            );
        }

        public static ModelViewerCommand Simple(
            ModelViewerAction action,
            long expectedVersion,
            ModelViewerHitRegion hitRegion
        )
        {
            return new ModelViewerCommand(
                action,
                expectedVersion,
                hitRegion,
                null,
                0.0f
            );
        }
    }

    public readonly struct ModelViewerResult
    {
        public ModelViewerResult(
            bool accepted,
            bool consumed,
            ModelViewerError error,
            long version,
            string selectedPartId
        )
        {
            Accepted = accepted;
            Consumed = consumed;
            Error = error;
            Version = version;
            SelectedPartId = selectedPartId;
        }

        public bool Accepted { get; }
        public bool Consumed { get; }
        public ModelViewerError Error { get; }
        public long Version { get; }
        public string SelectedPartId { get; }
    }

    public sealed class ModelViewerPanelController
    {
        public const float MinimumZoom = 0.5f;
        public const float MaximumZoom = 2.0f;
        public const float MaximumRotationDeltaDegrees = 45.0f;

        private readonly SceneSelectionController _selection;
        private readonly Transform _modelRoot;
        private readonly Vector3 _anchorPosition;
        private readonly Quaternion _anchorRotation;
        private readonly Vector3 _anchorScale;

        public ModelViewerPanelController(
            SceneSelectionController selection,
            Transform modelRoot
        )
        {
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
            _modelRoot = modelRoot ?? throw new ArgumentNullException(nameof(modelRoot));
            _anchorPosition = modelRoot.localPosition;
            _anchorRotation = modelRoot.localRotation;
            _anchorScale = modelRoot.localScale;
            Zoom = 1.0f;
        }

        public long Version { get; private set; }
        public float Zoom { get; private set; }
        public float RotationDegrees { get; private set; }
        public bool AnnotationsVisible { get; private set; }
        public int PanelCommandsConsumed { get; private set; }
        public int SceneSelectionDispatches { get; private set; }
        public int ClickThroughCount { get; private set; }

        public ModelViewerResult Apply(ModelViewerCommand command)
        {
            bool consumed = command.HitRegion == ModelViewerHitRegion.Panel;
            if (consumed)
            {
                PanelCommandsConsumed += 1;
            }

            if (
                command.Action == ModelViewerAction.Unknown
                || !Enum.IsDefined(typeof(ModelViewerAction), command.Action)
                || command.ExpectedVersion < 0
                || !IsFinite(command.Value)
            )
            {
                return Reject(consumed, ModelViewerError.InvalidCommand);
            }

            if (command.ExpectedVersion != Version)
            {
                return Reject(consumed, ModelViewerError.StaleVersion);
            }

            if (!consumed)
            {
                return Reject(false, ModelViewerError.NotPanelHit);
            }

            switch (command.Action)
            {
                case ModelViewerAction.SelectPart:
                    return Select(command.PartId);
                case ModelViewerAction.Zoom:
                    return SetZoom(command.Value);
                case ModelViewerAction.Rotate:
                    return Rotate(command.Value);
                case ModelViewerAction.ToggleAnnotations:
                    AnnotationsVisible = !AnnotationsVisible;
                    return Accept(null);
                case ModelViewerAction.Recenter:
                    return Recenter();
                default:
                    return Reject(consumed, ModelViewerError.InvalidCommand);
            }
        }

        private ModelViewerResult Select(string partId)
        {
            SceneSelectionResult selection = _selection.Apply(
                SceneSelectionCommand.For(
                    SceneSelectionAction.Select,
                    _selection.State.Version,
                    partId
                )
            );
            if (!selection.Accepted)
            {
                return Reject(
                    true,
                    selection.Error == SceneSelectionError.UnknownPart
                        ? ModelViewerError.UnknownPart
                        : ModelViewerError.SelectionRejected
                );
            }

            SceneSelectionDispatches += 1;
            return Accept(partId);
        }

        private ModelViewerResult SetZoom(float zoom)
        {
            if (zoom < MinimumZoom || zoom > MaximumZoom)
            {
                return Reject(true, ModelViewerError.ZoomOutOfBounds);
            }

            if (Math.Abs(Zoom - zoom) <= 0.0001f)
            {
                return Reject(true, ModelViewerError.AlreadyInState);
            }

            Zoom = zoom;
            ApplyTransform();
            return Accept(null);
        }

        private ModelViewerResult Rotate(float deltaDegrees)
        {
            if (
                deltaDegrees < -MaximumRotationDeltaDegrees
                || deltaDegrees > MaximumRotationDeltaDegrees
            )
            {
                return Reject(true, ModelViewerError.RotationOutOfBounds);
            }

            if (Math.Abs(deltaDegrees) <= 0.0001f)
            {
                return Reject(true, ModelViewerError.AlreadyInState);
            }

            RotationDegrees = NormalizeDegrees(RotationDegrees + deltaDegrees);
            ApplyTransform();
            return Accept(null);
        }

        private ModelViewerResult Recenter()
        {
            bool changed = Math.Abs(Zoom - 1.0f) > 0.0001f
                || Math.Abs(RotationDegrees) > 0.0001f
                || _modelRoot.localPosition != _anchorPosition;
            if (!changed)
            {
                return Reject(true, ModelViewerError.AlreadyInState);
            }

            Zoom = 1.0f;
            RotationDegrees = 0.0f;
            _modelRoot.localPosition = _anchorPosition;
            _modelRoot.localRotation = _anchorRotation;
            _modelRoot.localScale = _anchorScale;
            return Accept(null);
        }

        private void ApplyTransform()
        {
            _modelRoot.localPosition = _anchorPosition;
            _modelRoot.localRotation = _anchorRotation
                * Quaternion.Euler(0.0f, RotationDegrees, 0.0f);
            _modelRoot.localScale = _anchorScale * Zoom;
        }

        private ModelViewerResult Accept(string selectedPartId)
        {
            Version += 1;
            return new ModelViewerResult(
                true,
                true,
                ModelViewerError.None,
                Version,
                selectedPartId
            );
        }

        private ModelViewerResult Reject(bool consumed, ModelViewerError error)
        {
            if (consumed && SceneSelectionDispatches > Version)
            {
                ClickThroughCount += 1;
            }

            return new ModelViewerResult(
                false,
                consumed,
                error,
                Version,
                null
            );
        }

        private static float NormalizeDegrees(float degrees)
        {
            float normalized = degrees % 360.0f;
            return normalized < 0.0f ? normalized + 360.0f : normalized;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    public sealed class ModelViewerRenderExecutor : IRenderCommandExecutor
    {
        private readonly AnimationControllerV1 _animation;
        private readonly ModelViewerPanelController _viewer;

        public ModelViewerRenderExecutor(
            AnimationControllerV1 animation,
            ModelViewerPanelController viewer
        )
        {
            _animation = animation ?? throw new ArgumentNullException(nameof(animation));
            _viewer = viewer ?? throw new ArgumentNullException(nameof(viewer));
        }

        public long SceneVersion { get; private set; }
        public int ExecutionCount { get; private set; }

        public RenderExecutionResult Execute(RenderCommand command)
        {
            if (command.ExpectedSceneVersion != SceneVersion)
            {
                return RenderExecutionResult.Failed(SceneVersion, "stale_scene_version");
            }

            ExecutionCount += 1;
            bool accepted;
            string error;
            switch (command.Kind)
            {
                case RenderCommandKind.AnimationPlay:
                    AnimationControlResult play = _animation.Play(
                        _animation.Version,
                        command.TargetId
                    );
                    accepted = play.Accepted;
                    error = play.Error.ToString();
                    break;
                case RenderCommandKind.AnimationPause:
                    AnimationControlResult pause = _animation.Pause(_animation.Version);
                    accepted = pause.Accepted;
                    error = pause.Error.ToString();
                    break;
                case RenderCommandKind.AnimationSeek:
                    AnimationControlResult seek = _animation.Seek(
                        _animation.Version,
                        command.NumberValue
                    );
                    accepted = seek.Accepted;
                    error = seek.Error.ToString();
                    break;
                case RenderCommandKind.AnimationSpeed:
                    AnimationControlResult speed = _animation.SetSpeed(
                        _animation.Version,
                        command.NumberValue
                    );
                    accepted = speed.Accepted;
                    error = speed.Error.ToString();
                    break;
                case RenderCommandKind.SelectPart:
                    ModelViewerResult select = _viewer.Apply(
                        ModelViewerCommand.Select(
                            _viewer.Version,
                            ModelViewerHitRegion.Panel,
                            command.TargetId
                        )
                    );
                    accepted = select.Accepted;
                    error = select.Error.ToString();
                    break;
                case RenderCommandKind.Zoom:
                    ModelViewerResult zoom = _viewer.Apply(
                        ModelViewerCommand.Number(
                            ModelViewerAction.Zoom,
                            _viewer.Version,
                            ModelViewerHitRegion.Panel,
                            command.NumberValue
                        )
                    );
                    accepted = zoom.Accepted;
                    error = zoom.Error.ToString();
                    break;
                case RenderCommandKind.Rotate:
                    ModelViewerResult rotate = _viewer.Apply(
                        ModelViewerCommand.Number(
                            ModelViewerAction.Rotate,
                            _viewer.Version,
                            ModelViewerHitRegion.Panel,
                            command.NumberValue
                        )
                    );
                    accepted = rotate.Accepted;
                    error = rotate.Error.ToString();
                    break;
                case RenderCommandKind.ToggleAnnotations:
                    ModelViewerResult annotations = _viewer.Apply(
                        ModelViewerCommand.Simple(
                            ModelViewerAction.ToggleAnnotations,
                            _viewer.Version,
                            ModelViewerHitRegion.Panel
                        )
                    );
                    accepted = annotations.Accepted;
                    error = annotations.Error.ToString();
                    break;
                case RenderCommandKind.Recenter:
                    ModelViewerResult recenter = _viewer.Apply(
                        ModelViewerCommand.Simple(
                            ModelViewerAction.Recenter,
                            _viewer.Version,
                            ModelViewerHitRegion.Panel
                        )
                    );
                    accepted = recenter.Accepted;
                    error = recenter.Error.ToString();
                    break;
                default:
                    accepted = false;
                    error = "invalid_command";
                    break;
            }

            if (!accepted)
            {
                return RenderExecutionResult.Failed(
                    SceneVersion,
                    error.ToLowerInvariant()
                );
            }

            SceneVersion += 1;
            return RenderExecutionResult.Completed(SceneVersion);
        }
    }
}
