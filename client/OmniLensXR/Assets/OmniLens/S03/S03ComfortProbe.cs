using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace OmniLensXR.S03
{
    public enum S03ThermalState
    {
        Unavailable = -1,
        Nominal = 0,
        Fair = 1,
        Serious = 2,
        Critical = 3,
    }

    public readonly struct ComfortFrameStats
    {
        public ComfortFrameStats(
            int count,
            float averageMilliseconds,
            float p50Milliseconds,
            float p95Milliseconds,
            float minimumMilliseconds,
            float maximumMilliseconds
        )
        {
            Count = count;
            AverageMilliseconds = averageMilliseconds;
            P50Milliseconds = p50Milliseconds;
            P95Milliseconds = p95Milliseconds;
            MinimumMilliseconds = minimumMilliseconds;
            MaximumMilliseconds = maximumMilliseconds;
        }

        public int Count { get; }

        public float AverageMilliseconds { get; }

        public float P50Milliseconds { get; }

        public float P95Milliseconds { get; }

        public float MinimumMilliseconds { get; }

        public float MaximumMilliseconds { get; }
    }

    public static class ComfortFrameCalculator
    {
        public static bool TryCalculate(
            IReadOnlyList<float> samples,
            out ComfortFrameStats stats
        )
        {
            stats = default;
            if (samples == null || samples.Count == 0)
            {
                return false;
            }

            List<float> sorted = new List<float>(samples.Count);
            double total = 0.0;
            foreach (float sample in samples)
            {
                if (sample <= 0.0f || float.IsNaN(sample) || float.IsInfinity(sample))
                {
                    return false;
                }

                sorted.Add(sample);
                total += sample;
            }

            sorted.Sort();
            stats = new ComfortFrameStats(
                sorted.Count,
                (float)(total / sorted.Count),
                Percentile(sorted, 0.50f),
                Percentile(sorted, 0.95f),
                sorted[0],
                sorted[sorted.Count - 1]
            );
            return true;
        }

        private static float Percentile(IReadOnlyList<float> sorted, float percentile)
        {
            int index = Mathf.Clamp(
                Mathf.CeilToInt(sorted.Count * percentile) - 1,
                0,
                sorted.Count - 1
            );
            return sorted[index];
        }
    }

    public static class ComfortCheckpointSchedule
    {
        private static readonly int[] Minutes = { 5, 10, 20 };

        public static int[] CrossedMinutes(double previousSeconds, double currentSeconds)
        {
            if (
                double.IsNaN(previousSeconds)
                || double.IsInfinity(previousSeconds)
                || double.IsNaN(currentSeconds)
                || double.IsInfinity(currentSeconds)
                || previousSeconds < 0.0
                || currentSeconds < previousSeconds
            )
            {
                return Array.Empty<int>();
            }

            List<int> crossed = new List<int>();
            foreach (int minute in Minutes)
            {
                double threshold = minute * 60.0;
                if (previousSeconds < threshold && currentSeconds >= threshold)
                {
                    crossed.Add(minute);
                }
            }

            return crossed.ToArray();
        }
    }

    public static class S03ThermalReader
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int OmniLensS03ThermalState();
#endif

        public static S03ThermalState Read()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return FromNativeValue(OmniLensS03ThermalState());
#else
            return S03ThermalState.Unavailable;
#endif
        }

        public static S03ThermalState FromNativeValue(int value)
        {
            return value >= (int)S03ThermalState.Nominal
                    && value <= (int)S03ThermalState.Critical
                ? (S03ThermalState)value
                : S03ThermalState.Unavailable;
        }
    }

    /// <summary>
    /// Bounded local diagnostics for the future physical 20-minute viewer run. Source or
    /// simulator output cannot satisfy the comfort gate.
    /// </summary>
    public sealed class S03ComfortProbe : MonoBehaviour
    {
        private const int FrameWindowSize = 300;
        private const int MaximumLogRows = 2000;
        private const double MaximumProbeSeconds = 21.0 * 60.0;
        private const string LogPrefix = "[S03_COMFORT] ";

        private readonly List<float> _frameTimesMilliseconds = new List<float>(
            FrameWindowSize
        );
        private string _outputPath;
        private double _startedAt;
        private double _lastElapsedSeconds;
        private int _emittedRows;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _outputPath = Path.Combine(
                Application.persistentDataPath,
                "omnilens-s03-comfort.jsonl"
            );
            try
            {
                File.WriteAllText(_outputPath, string.Empty);
            }
            catch (Exception exception)
            {
                Debug.LogError(LogPrefix + "log_initialization_failed=" + exception.GetType().Name);
            }

            _startedAt = Time.realtimeSinceStartupAsDouble;
            Emit("startup", "probe_limit_minutes=21,raw_media=false");
            EmitCheckpoint(0);
        }

        private void Update()
        {
            double elapsedSeconds = Time.realtimeSinceStartupAsDouble - _startedAt;
            if (elapsedSeconds > MaximumProbeSeconds)
            {
                return;
            }

            float frameMilliseconds = Time.unscaledDeltaTime * 1000.0f;
            if (
                frameMilliseconds > 0.0f
                && !float.IsNaN(frameMilliseconds)
                && !float.IsInfinity(frameMilliseconds)
            )
            {
                _frameTimesMilliseconds.Add(frameMilliseconds);
            }

            if (_frameTimesMilliseconds.Count >= FrameWindowSize)
            {
                EmitFrameWindow(elapsedSeconds);
                _frameTimesMilliseconds.Clear();
            }

            foreach (
                int minute in ComfortCheckpointSchedule.CrossedMinutes(
                    _lastElapsedSeconds,
                    elapsedSeconds
                )
            )
            {
                EmitCheckpoint(minute);
            }

            _lastElapsedSeconds = elapsedSeconds;
        }

        private void OnApplicationPause(bool paused)
        {
            Emit(paused ? "background" : "resume", ThermalDetails());
        }

        private void OnApplicationFocus(bool focused)
        {
            Emit(focused ? "focus_gained" : "focus_lost", ThermalDetails());
        }

        private void EmitCheckpoint(int minute)
        {
            Emit(
                "comfort_checkpoint",
                "minute="
                    + minute.ToString(CultureInfo.InvariantCulture)
                    + ","
                    + ThermalDetails()
            );
        }

        private void EmitFrameWindow(double elapsedSeconds)
        {
            if (!ComfortFrameCalculator.TryCalculate(_frameTimesMilliseconds, out ComfortFrameStats stats))
            {
                Emit("frame_window_rejected", "reason=invalid_samples");
                return;
            }

            Emit(
                "frame_window",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "elapsed_seconds={0:F3},samples={1},avg_ms={2:F3},p50_ms={3:F3},p95_ms={4:F3},min_ms={5:F3},max_ms={6:F3},{7}",
                    elapsedSeconds,
                    stats.Count,
                    stats.AverageMilliseconds,
                    stats.P50Milliseconds,
                    stats.P95Milliseconds,
                    stats.MinimumMilliseconds,
                    stats.MaximumMilliseconds,
                    ThermalDetails()
                )
            );
        }

        private static string ThermalDetails()
        {
            return "thermal_state=" + S03ThermalReader.Read().ToString().ToLowerInvariant();
        }

        private void Emit(string eventName, string details)
        {
            if (_emittedRows >= MaximumLogRows)
            {
                return;
            }

            _emittedRows += 1;
            string line = string.Format(
                CultureInfo.InvariantCulture,
                "{{\"event\":\"{0}\",\"time\":{1:F3},\"details\":\"{2}\"}}",
                eventName,
                Time.realtimeSinceStartupAsDouble,
                details
            );
            Debug.Log(LogPrefix + line);
            try
            {
                File.AppendAllText(_outputPath, line + Environment.NewLine);
            }
            catch (Exception exception)
            {
                Debug.LogError(LogPrefix + "log_write_failed=" + exception.GetType().Name);
            }
        }
    }
}
