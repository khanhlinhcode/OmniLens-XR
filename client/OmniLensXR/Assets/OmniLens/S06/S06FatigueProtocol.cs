using System;
using System.Globalization;

namespace OmniLensXR.S06
{
    public enum FatigueProtocolState
    {
        NotStarted = 0,
        Running = 1,
        CompletedSynthetic = 2,
        Failed = 3,
    }

    public enum FatigueInteractionKind
    {
        Selection = 1,
        Misclick = 2,
        HeadTurn = 3,
        Recenter = 4,
        LongTextEntryAttempt = 5,
    }

    public enum FatigueRecordError
    {
        None = 0,
        NotRunning,
        InvalidTimestamp,
        NonMonotonicTimestamp,
        SampleLimitReached,
        LongTextEntryProhibited,
        InvalidInteraction,
    }

    public sealed class FatigueProtocolSummary
    {
        public FatigueProtocolSummary(
            FatigueProtocolState state,
            double elapsedSeconds,
            int selections,
            int misclicks,
            int headTurns,
            int recenters,
            int prohibitedLongTextAttempts
        )
        {
            State = state;
            ElapsedSeconds = elapsedSeconds;
            Selections = selections;
            Misclicks = misclicks;
            HeadTurns = headTurns;
            Recenters = recenters;
            ProhibitedLongTextAttempts = prohibitedLongTextAttempts;
        }

        public FatigueProtocolState State { get; }

        public double ElapsedSeconds { get; }

        public int Selections { get; }

        public int Misclicks { get; }

        public int HeadTurns { get; }

        public int Recenters { get; }

        public int ProhibitedLongTextAttempts { get; }

        public string PhysicalViewerResult => "NOT_RUN";
    }

    public sealed class S06FatigueProtocol
    {
        public const double RequiredDurationSeconds = 600.0;
        public const int MaximumAcceptedSamples = 600;

        private double _startedAt;
        private double _lastTimestamp;
        private double _elapsedSeconds;
        private int _acceptedSamples;
        private int _selections;
        private int _misclicks;
        private int _headTurns;
        private int _recenters;
        private int _prohibitedLongTextAttempts;

        public FatigueProtocolState State { get; private set; } =
            FatigueProtocolState.NotStarted;

        public bool Start(double timestampSeconds)
        {
            if (
                State != FatigueProtocolState.NotStarted
                || !IsValidTimestamp(timestampSeconds)
            )
            {
                return false;
            }

            _startedAt = timestampSeconds;
            _lastTimestamp = timestampSeconds;
            State = FatigueProtocolState.Running;
            return true;
        }

        public FatigueRecordError Record(
            FatigueInteractionKind kind,
            double timestampSeconds
        )
        {
            FatigueRecordError timestampError = ValidateRunningTimestamp(
                timestampSeconds
            );
            if (timestampError != FatigueRecordError.None)
            {
                return timestampError;
            }

            if (!Enum.IsDefined(typeof(FatigueInteractionKind), kind))
            {
                return FatigueRecordError.InvalidInteraction;
            }

            _lastTimestamp = timestampSeconds;
            _elapsedSeconds = timestampSeconds - _startedAt;
            if (_acceptedSamples >= MaximumAcceptedSamples)
            {
                return FatigueRecordError.SampleLimitReached;
            }

            _acceptedSamples += 1;
            if (kind == FatigueInteractionKind.LongTextEntryAttempt)
            {
                _prohibitedLongTextAttempts += 1;
                return FatigueRecordError.LongTextEntryProhibited;
            }
            switch (kind)
            {
                case FatigueInteractionKind.Selection:
                    _selections += 1;
                    break;
                case FatigueInteractionKind.Misclick:
                    _misclicks += 1;
                    break;
                case FatigueInteractionKind.HeadTurn:
                    _headTurns += 1;
                    break;
                case FatigueInteractionKind.Recenter:
                    _recenters += 1;
                    break;
            }

            return FatigueRecordError.None;
        }

        public FatigueRecordError Advance(double timestampSeconds)
        {
            FatigueRecordError error = ValidateRunningTimestamp(timestampSeconds);
            if (error != FatigueRecordError.None)
            {
                return error;
            }

            _lastTimestamp = timestampSeconds;
            _elapsedSeconds = timestampSeconds - _startedAt;
            if (_elapsedSeconds >= RequiredDurationSeconds)
            {
                State = FatigueProtocolState.CompletedSynthetic;
            }

            return FatigueRecordError.None;
        }

        public FatigueProtocolSummary Summary()
        {
            return new FatigueProtocolSummary(
                State,
                _elapsedSeconds,
                _selections,
                _misclicks,
                _headTurns,
                _recenters,
                _prohibitedLongTextAttempts
            );
        }

        private FatigueRecordError ValidateRunningTimestamp(double timestampSeconds)
        {
            if (State != FatigueProtocolState.Running)
            {
                return FatigueRecordError.NotRunning;
            }

            if (!IsValidTimestamp(timestampSeconds))
            {
                return FatigueRecordError.InvalidTimestamp;
            }

            return timestampSeconds < _lastTimestamp
                ? FatigueRecordError.NonMonotonicTimestamp
                : FatigueRecordError.None;
        }

        private static bool IsValidTimestamp(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0.0;
        }
    }

    public static class S06FatigueMetricLog
    {
        public static string Format(FatigueProtocolSummary summary)
        {
            if (summary == null)
            {
                throw new ArgumentNullException(nameof(summary));
            }

            return $"[S06_METRICS] state={summary.State} elapsed="
                + summary.ElapsedSeconds.ToString("F1", CultureInfo.InvariantCulture)
                + $" physical={summary.PhysicalViewerResult}";
        }
    }
}
