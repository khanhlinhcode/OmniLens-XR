using System;
using OmniLensXR.S05;
using UnityEngine;

namespace OmniLensXR.S06
{
    public enum PanelHandleKind
    {
        Unknown = 0,
        MoveLeft = 1,
        MoveRight = 2,
        MoveUp = 3,
        MoveDown = 4,
        ResizeWider = 5,
        ResizeNarrower = 6,
        ResizeTaller = 7,
        ResizeShorter = 8,
        TogglePin = 9,
    }

    public enum PanelManipulationError
    {
        None = 0,
        InvalidHandle,
        UnknownPanel,
        PanelNotVisible,
        PlacementRejected,
        RegistryRejected,
    }

    public readonly struct PanelManipulationResult
    {
        public PanelManipulationResult(
            bool accepted,
            PanelManipulationError error,
            PanelApplyResult applyResult
        )
        {
            Accepted = accepted;
            Error = error;
            ApplyResult = applyResult;
        }

        public bool Accepted { get; }

        public PanelManipulationError Error { get; }

        public PanelApplyResult ApplyResult { get; }
    }

    public static class PanelManipulationTokens
    {
        public const float TranslationStepMeters = 0.10f;
        public const float ResizeStepMeters = 0.10f;
        public const float MinimumHandleHitSizeMeters = 0.12f;
    }

    public sealed class PanelManipulationController
    {
        private readonly PanelWorkspaceController _workspace;

        public PanelManipulationController(PanelWorkspaceController workspace)
        {
            _workspace = workspace
                ?? throw new ArgumentNullException(nameof(workspace));
        }

        public PanelManipulationResult Apply(PanelHandleKind handle, string panelId)
        {
            if (
                handle == PanelHandleKind.Unknown
                || !Enum.IsDefined(typeof(PanelHandleKind), handle)
            )
            {
                return Reject(PanelManipulationError.InvalidHandle);
            }

            if (!_workspace.Registry.TryGet(panelId, out PanelState state))
            {
                return Reject(PanelManipulationError.UnknownPanel);
            }

            if (state.Visibility != PanelVisibility.Visible)
            {
                return Reject(PanelManipulationError.PanelNotVisible);
            }

            if (handle == PanelHandleKind.TogglePin)
            {
                return ApplyRegistry(
                    PanelCommand.For(
                        PanelCommandKind.SetPinned,
                        _workspace.Registry.Version,
                        panelId,
                        !state.IsPinned
                    )
                );
            }

            Vector3 desiredPosition = state.Bounds.PositionMeters;
            Vector2 desiredSize = state.Bounds.SizeMeters;
            switch (handle)
            {
                case PanelHandleKind.MoveLeft:
                    desiredPosition.x -= PanelManipulationTokens.TranslationStepMeters;
                    break;
                case PanelHandleKind.MoveRight:
                    desiredPosition.x += PanelManipulationTokens.TranslationStepMeters;
                    break;
                case PanelHandleKind.MoveUp:
                    desiredPosition.y += PanelManipulationTokens.TranslationStepMeters;
                    break;
                case PanelHandleKind.MoveDown:
                    desiredPosition.y -= PanelManipulationTokens.TranslationStepMeters;
                    break;
                case PanelHandleKind.ResizeWider:
                    desiredSize.x += PanelManipulationTokens.ResizeStepMeters;
                    break;
                case PanelHandleKind.ResizeNarrower:
                    desiredSize.x -= PanelManipulationTokens.ResizeStepMeters;
                    break;
                case PanelHandleKind.ResizeTaller:
                    desiredSize.y += PanelManipulationTokens.ResizeStepMeters;
                    break;
                case PanelHandleKind.ResizeShorter:
                    desiredSize.y -= PanelManipulationTokens.ResizeStepMeters;
                    break;
                default:
                    return Reject(PanelManipulationError.InvalidHandle);
            }

            PanelPlacementResult placement = PanelPlacementService.Resolve(
                _workspace.Registry.CreateSnapshot().Panels,
                panelId,
                desiredPosition,
                desiredSize
            );
            if (!placement.Accepted)
            {
                return Reject(PanelManipulationError.PlacementRejected);
            }

            bool isMove = handle == PanelHandleKind.MoveLeft
                || handle == PanelHandleKind.MoveRight
                || handle == PanelHandleKind.MoveUp
                || handle == PanelHandleKind.MoveDown;
            if (
                !isMove
                && Vector3.Distance(
                    placement.Bounds.PositionMeters,
                    state.Bounds.PositionMeters
                ) > 0.0001f
            )
            {
                return Reject(PanelManipulationError.PlacementRejected);
            }

            return ApplyRegistry(
                isMove
                    ? PanelCommand.Move(
                        _workspace.Registry.Version,
                        panelId,
                        placement.Bounds.PositionMeters
                    )
                    : PanelCommand.Resize(
                        _workspace.Registry.Version,
                        panelId,
                        placement.Bounds.SizeMeters
                    )
            );
        }

        private PanelManipulationResult ApplyRegistry(PanelCommand command)
        {
            PanelApplyResult applyResult = _workspace.Apply(command);
            return applyResult.Accepted
                ? new PanelManipulationResult(
                    true,
                    PanelManipulationError.None,
                    applyResult
                )
                : new PanelManipulationResult(
                    false,
                    PanelManipulationError.RegistryRejected,
                    applyResult
                );
        }

        private PanelManipulationResult Reject(PanelManipulationError error)
        {
            return new PanelManipulationResult(
                false,
                error,
                new PanelApplyResult(
                    false,
                    PanelApplyError.InvalidCommand,
                    _workspace.Registry.Version,
                    null
                )
            );
        }
    }
}
