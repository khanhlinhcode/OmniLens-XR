using System;
using System.Collections.Generic;

namespace OmniLensXR.S07
{
    public enum AgentActivityStatus
    {
        Unknown = 0,
        Pending = 1,
        Accepted = 2,
        Failed = 3,
        Cancelled = 4,
    }

    public enum AgentTimelineError
    {
        None = 0,
        InvalidEvent,
        DuplicateId,
        CapacityReached,
        StreamError,
    }

    public readonly struct AgentActivityEvent
    {
        public AgentActivityEvent(
            string commandId,
            AgentActivityStatus status,
            string label
        )
        {
            CommandId = commandId;
            Status = status;
            Label = label;
        }

        public string CommandId { get; }
        public AgentActivityStatus Status { get; }
        public string Label { get; }
    }

    public sealed class AgentActivityTimeline
    {
        public const int MaximumEvents = 30;
        public const int MaximumIdLength = 32;
        public const int MaximumLabelLength = 96;
        private readonly List<AgentActivityEvent> _events =
            new List<AgentActivityEvent>();
        private readonly HashSet<string> _ids =
            new HashSet<string>(StringComparer.Ordinal);

        public IReadOnlyList<AgentActivityEvent> Events => _events.AsReadOnly();

        public AgentTimelineError LastError { get; private set; }

        public AgentTimelineError Append(AgentActivityEvent activityEvent)
        {
            if (!IsValid(activityEvent))
            {
                LastError = AgentTimelineError.InvalidEvent;
                return LastError;
            }

            if (_events.Count >= MaximumEvents)
            {
                LastError = AgentTimelineError.CapacityReached;
                return LastError;
            }

            if (!_ids.Add(activityEvent.CommandId))
            {
                LastError = AgentTimelineError.DuplicateId;
                return LastError;
            }

            _events.Add(activityEvent);
            LastError = AgentTimelineError.None;
            return LastError;
        }

        public void MarkStreamError()
        {
            LastError = AgentTimelineError.StreamError;
        }

        public string RenderSummary()
        {
            if (LastError == AgentTimelineError.StreamError)
            {
                return "Agent activity unavailable (mock stream error)";
            }

            if (_events.Count == 0)
            {
                return "No agent activity";
            }

            int pending = 0;
            int accepted = 0;
            int failed = 0;
            int cancelled = 0;
            foreach (AgentActivityEvent item in _events)
            {
                switch (item.Status)
                {
                    case AgentActivityStatus.Pending:
                        pending += 1;
                        break;
                    case AgentActivityStatus.Accepted:
                        accepted += 1;
                        break;
                    case AgentActivityStatus.Failed:
                        failed += 1;
                        break;
                    case AgentActivityStatus.Cancelled:
                        cancelled += 1;
                        break;
                }
            }

            return $"Activity total={_events.Count} pending={pending} "
                + $"accepted={accepted} failed={failed} cancelled={cancelled}";
        }

        private static bool IsValid(AgentActivityEvent activityEvent)
        {
            if (
                string.IsNullOrWhiteSpace(activityEvent.CommandId)
                || activityEvent.CommandId.Length > MaximumIdLength
                || string.IsNullOrWhiteSpace(activityEvent.Label)
                || activityEvent.Label.Length > MaximumLabelLength
                || activityEvent.Status == AgentActivityStatus.Unknown
                || !Enum.IsDefined(typeof(AgentActivityStatus), activityEvent.Status)
            )
            {
                return false;
            }

            foreach (char character in activityEvent.CommandId)
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

    public sealed class AgentActivityFixture
    {
        public AgentActivityFixture(
            int index,
            AgentActivityTimeline timeline,
            string expectedKind
        )
        {
            Index = index;
            Timeline = timeline;
            ExpectedKind = expectedKind;
        }

        public int Index { get; }
        public AgentActivityTimeline Timeline { get; }
        public string ExpectedKind { get; }
    }

    public static class AgentActivityFixtureFactory
    {
        public static IReadOnlyList<AgentActivityFixture> CreateThirty()
        {
            List<AgentActivityFixture> fixtures = new List<AgentActivityFixture>();
            fixtures.Add(new AgentActivityFixture(0, new AgentActivityTimeline(), "empty"));
            AgentActivityTimeline streamError = new AgentActivityTimeline();
            streamError.MarkStreamError();
            fixtures.Add(new AgentActivityFixture(1, streamError, "error"));

            AgentActivityStatus[] statuses =
            {
                AgentActivityStatus.Pending,
                AgentActivityStatus.Accepted,
                AgentActivityStatus.Failed,
                AgentActivityStatus.Cancelled,
            };
            for (int index = 2; index < 30; index += 1)
            {
                AgentActivityTimeline timeline = new AgentActivityTimeline();
                int itemCount = 1 + index % 5;
                for (int item = 0; item < itemCount; item += 1)
                {
                    AgentActivityStatus status = statuses[(index + item) % statuses.Length];
                    AgentTimelineError result = timeline.Append(
                        new AgentActivityEvent(
                            $"fixture-{index:D2}-{item:D2}",
                            status,
                            $"Mock task {index:D2}/{item:D2}"
                        )
                    );
                    if (result != AgentTimelineError.None)
                    {
                        throw new InvalidOperationException(
                            $"Invalid deterministic activity fixture: {result}."
                        );
                    }
                }

                fixtures.Add(
                    new AgentActivityFixture(index, timeline, statuses[index % 4].ToString())
                );
            }

            return fixtures.AsReadOnly();
        }
    }
}
