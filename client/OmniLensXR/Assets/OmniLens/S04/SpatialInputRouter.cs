using System;
using System.Collections.Generic;

namespace OmniLensXR.S04
{
    public interface ISpatialInputSink
    {
        bool TryHandle(SpatialInputEvent inputEvent);
    }

    public enum SpatialRouteResult
    {
        Accepted = 1,
        InvalidEvent = 2,
        SourceDisabled = 3,
        StaleSequence = 4,
        NoHandler = 5,
        HandlerFailed = 6,
        AcceptedWithHandlerFailure = 7,
    }

    /// <summary>
    /// Direct bounded dispatch: no unbounded event queue and no arbitrary command names.
    /// </summary>
    public sealed class SpatialInputRouter
    {
        public const int MaximumSinks = 16;

        private readonly List<ISpatialInputSink> _sinks = new List<ISpatialInputSink>();
        private readonly Dictionary<SpatialInputSourceKind, long> _lastSequence =
            new Dictionary<SpatialInputSourceKind, long>();
        private readonly HashSet<SpatialInputSourceKind> _disabledSources =
            new HashSet<SpatialInputSourceKind>();

        public int SinkCount => _sinks.Count;

        public bool RegisterSink(ISpatialInputSink sink)
        {
            if (
                sink == null
                || _sinks.Count >= MaximumSinks
                || _sinks.Contains(sink)
            )
            {
                return false;
            }

            _sinks.Add(sink);
            return true;
        }

        public bool UnregisterSink(ISpatialInputSink sink)
        {
            return sink != null && _sinks.Remove(sink);
        }

        public bool SetSourceEnabled(SpatialInputSourceKind source, bool enabled)
        {
            if (
                source == SpatialInputSourceKind.Unknown
                || !Enum.IsDefined(typeof(SpatialInputSourceKind), source)
            )
            {
                return false;
            }

            if (enabled)
            {
                _disabledSources.Remove(source);
            }
            else
            {
                _disabledSources.Add(source);
            }

            return true;
        }

        public SpatialRouteResult Route(SpatialInputEvent inputEvent)
        {
            if (
                !SpatialInputEvent.TryCreate(
                    inputEvent.Sequence,
                    inputEvent.TimestampSeconds,
                    inputEvent.Kind,
                    inputEvent.Source,
                    inputEvent.HeadPose,
                    out _
                )
            )
            {
                return SpatialRouteResult.InvalidEvent;
            }

            bool isRestored = inputEvent.Kind == SpatialInputEventKind.SourceRestored;
            if (_disabledSources.Contains(inputEvent.Source) && !isRestored)
            {
                return SpatialRouteResult.SourceDisabled;
            }

            if (
                _lastSequence.TryGetValue(inputEvent.Source, out long previousSequence)
                && inputEvent.Sequence <= previousSequence
            )
            {
                return SpatialRouteResult.StaleSequence;
            }

            _lastSequence[inputEvent.Source] = inputEvent.Sequence;
            if (isRestored)
            {
                _disabledSources.Remove(inputEvent.Source);
            }

            bool handled = false;
            bool handlerFailed = false;
            ISpatialInputSink[] snapshot = _sinks.ToArray();
            foreach (ISpatialInputSink sink in snapshot)
            {
                try
                {
                    handled = sink.TryHandle(inputEvent) || handled;
                }
                catch (Exception)
                {
                    handlerFailed = true;
                }
            }

            if (inputEvent.Kind == SpatialInputEventKind.SourceLost)
            {
                _disabledSources.Add(inputEvent.Source);
            }

            if (handled)
            {
                return handlerFailed
                    ? SpatialRouteResult.AcceptedWithHandlerFailure
                    : SpatialRouteResult.Accepted;
            }

            return handlerFailed
                ? SpatialRouteResult.HandlerFailed
                : SpatialRouteResult.NoHandler;
        }
    }
}

