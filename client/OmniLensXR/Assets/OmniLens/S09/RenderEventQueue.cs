using System;
using System.Collections.Generic;

namespace OmniLensXR.S09
{
    public enum RenderCommandKind
    {
        Unknown = 0,
        AnimationPlay = 1,
        AnimationPause = 2,
        AnimationSeek = 3,
        AnimationSpeed = 4,
        SelectPart = 5,
        Zoom = 6,
        Rotate = 7,
        ToggleAnnotations = 8,
        Recenter = 9,
    }

    public enum RenderAckStatus
    {
        Queued = 1,
        Completed = 2,
        Failed = 3,
    }

    public enum RenderQueueError
    {
        None = 0,
        InvalidCommand,
        QueueFull,
    }

    public readonly struct RenderCommand
    {
        private RenderCommand(
            string commandId,
            long expectedSceneVersion,
            RenderCommandKind kind,
            string targetId,
            float numberValue
        )
        {
            CommandId = commandId;
            ExpectedSceneVersion = expectedSceneVersion;
            Kind = kind;
            TargetId = targetId;
            NumberValue = numberValue;
        }

        public string CommandId { get; }
        public long ExpectedSceneVersion { get; }
        public RenderCommandKind Kind { get; }
        public string TargetId { get; }
        public float NumberValue { get; }

        public static RenderCommand ForTarget(
            string commandId,
            long expectedSceneVersion,
            RenderCommandKind kind,
            string targetId
        )
        {
            return new RenderCommand(
                commandId,
                expectedSceneVersion,
                kind,
                targetId,
                0.0f
            );
        }

        public static RenderCommand ForNumber(
            string commandId,
            long expectedSceneVersion,
            RenderCommandKind kind,
            float numberValue
        )
        {
            return new RenderCommand(
                commandId,
                expectedSceneVersion,
                kind,
                null,
                numberValue
            );
        }

        public static RenderCommand Simple(
            string commandId,
            long expectedSceneVersion,
            RenderCommandKind kind
        )
        {
            return new RenderCommand(
                commandId,
                expectedSceneVersion,
                kind,
                null,
                0.0f
            );
        }
    }

    public readonly struct RenderExecutionResult
    {
        public RenderExecutionResult(bool succeeded, long sceneVersion, string errorCode)
        {
            Succeeded = succeeded;
            SceneVersion = sceneVersion;
            ErrorCode = errorCode;
        }

        public bool Succeeded { get; }
        public long SceneVersion { get; }
        public string ErrorCode { get; }

        public static RenderExecutionResult Completed(long sceneVersion)
        {
            return new RenderExecutionResult(true, sceneVersion, null);
        }

        public static RenderExecutionResult Failed(long sceneVersion, string errorCode)
        {
            return new RenderExecutionResult(false, sceneVersion, errorCode);
        }
    }

    public interface IRenderCommandExecutor
    {
        long SceneVersion { get; }
        RenderExecutionResult Execute(RenderCommand command);
    }

    public sealed class RenderCommandAck
    {
        internal RenderCommandAck(
            string commandId,
            RenderAckStatus status,
            long sceneVersion,
            string errorCode
        )
        {
            CommandId = commandId;
            Status = status;
            SceneVersion = sceneVersion;
            ErrorCode = errorCode;
        }

        public string CommandId { get; }
        public RenderAckStatus Status { get; internal set; }
        public long SceneVersion { get; internal set; }
        public string ErrorCode { get; internal set; }
    }

    public readonly struct RenderEnqueueResult
    {
        public RenderEnqueueResult(
            bool accepted,
            bool duplicate,
            RenderQueueError error,
            RenderCommandAck ack
        )
        {
            Accepted = accepted;
            Duplicate = duplicate;
            Error = error;
            Ack = ack;
        }

        public bool Accepted { get; }
        public bool Duplicate { get; }
        public RenderQueueError Error { get; }
        public RenderCommandAck Ack { get; }
    }

    public sealed class RenderEventQueue
    {
        public const int MaximumPending = 32;
        public const int MaximumHistory = 128;

        private readonly IRenderCommandExecutor _executor;
        private readonly Queue<RenderCommand> _pending = new Queue<RenderCommand>();
        private readonly Dictionary<string, RenderCommandAck> _acks =
            new Dictionary<string, RenderCommandAck>(StringComparer.Ordinal);
        private readonly Queue<string> _historyOrder = new Queue<string>();

        public RenderEventQueue(IRenderCommandExecutor executor)
        {
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }

        public int PendingCount => _pending.Count;
        public int HistoryCount => _acks.Count;

        public RenderEnqueueResult Enqueue(RenderCommand command)
        {
            if (!IsValid(command))
            {
                return new RenderEnqueueResult(
                    false,
                    false,
                    RenderQueueError.InvalidCommand,
                    null
                );
            }

            if (_acks.TryGetValue(command.CommandId, out RenderCommandAck existing))
            {
                return new RenderEnqueueResult(
                    true,
                    true,
                    RenderQueueError.None,
                    existing
                );
            }

            if (_pending.Count >= MaximumPending)
            {
                return new RenderEnqueueResult(
                    false,
                    false,
                    RenderQueueError.QueueFull,
                    null
                );
            }

            RenderCommandAck ack = new RenderCommandAck(
                command.CommandId,
                RenderAckStatus.Queued,
                _executor.SceneVersion,
                null
            );
            _acks.Add(command.CommandId, ack);
            _historyOrder.Enqueue(command.CommandId);
            _pending.Enqueue(command);
            TrimHistory();
            return new RenderEnqueueResult(
                true,
                false,
                RenderQueueError.None,
                ack
            );
        }

        public bool ProcessNext(out RenderCommandAck ack)
        {
            ack = null;
            if (_pending.Count == 0)
            {
                return false;
            }

            RenderCommand command = _pending.Dequeue();
            ack = _acks[command.CommandId];
            RenderExecutionResult result;
            try
            {
                result = _executor.Execute(command);
            }
            catch (Exception exception) when (
                exception is ArgumentException
                || exception is InvalidOperationException
            )
            {
                result = RenderExecutionResult.Failed(
                    _executor.SceneVersion,
                    "executor_exception"
                );
            }

            ack.Status = result.Succeeded
                ? RenderAckStatus.Completed
                : RenderAckStatus.Failed;
            ack.SceneVersion = result.SceneVersion;
            ack.ErrorCode = result.ErrorCode;
            return true;
        }

        public bool TryGetAck(string commandId, out RenderCommandAck ack)
        {
            ack = null;
            return commandId != null && _acks.TryGetValue(commandId, out ack);
        }

        private static bool IsValid(RenderCommand command)
        {
            if (
                !IsSafeId(command.CommandId)
                || command.ExpectedSceneVersion < 0
                || command.Kind == RenderCommandKind.Unknown
                || !Enum.IsDefined(typeof(RenderCommandKind), command.Kind)
                || float.IsNaN(command.NumberValue)
                || float.IsInfinity(command.NumberValue)
            )
            {
                return false;
            }

            bool needsTarget = command.Kind == RenderCommandKind.AnimationPlay
                || command.Kind == RenderCommandKind.SelectPart;
            return !needsTarget || IsSafeId(command.TargetId);
        }

        private static bool IsSafeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 64)
            {
                return false;
            }

            foreach (char character in value)
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

        private void TrimHistory()
        {
            while (_acks.Count > MaximumHistory && _historyOrder.Count > 0)
            {
                string candidate = _historyOrder.Peek();
                if (
                    _acks.TryGetValue(candidate, out RenderCommandAck ack)
                    && ack.Status == RenderAckStatus.Queued
                )
                {
                    break;
                }

                _historyOrder.Dequeue();
                _acks.Remove(candidate);
            }
        }
    }
}
