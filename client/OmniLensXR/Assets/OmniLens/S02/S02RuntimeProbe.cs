using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Google.XR.Cardboard;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Management;

using InputSystemTouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace OmniLensXR.S02
{
    public sealed class S02RuntimeProbe : MonoBehaviour
    {
        private const int FrameWindowSize = 300;
        private const int MaximumCheckpoints = 21;
        private const string LogPrefix = "[S02_PROBE] ";

        private readonly List<float> _frameTimesMilliseconds = new List<float>(
            FrameWindowSize
        );
        private string _outputPath;
        private int _checkpointCount;
        private bool _cameraAuthorized;
        private bool _hasDeviceParams;
        private bool _touchTracking;
        private double _touchStartedAt;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _outputPath = Path.Combine(
                Application.persistentDataPath,
                "omnilens-s02-runtime.jsonl"
            );

            try
            {
                File.WriteAllText(_outputPath, string.Empty);
            }
            catch (Exception exception)
            {
                Debug.LogError(LogPrefix + "log_initialization_failed=" + exception.GetType().Name);
            }

            _cameraAuthorized = CameraAuthorization();
            _hasDeviceParams = Api.HasDeviceParams();
            Emit("startup", RuntimeStateDetails());
        }

        private void Update()
        {
            float frameMilliseconds = Time.unscaledDeltaTime * 1000.0f;
            if (frameMilliseconds > 0.0f && !float.IsInfinity(frameMilliseconds))
            {
                _frameTimesMilliseconds.Add(frameMilliseconds);
            }

            if (_frameTimesMilliseconds.Count >= FrameWindowSize)
            {
                EmitFrameWindow();
                _frameTimesMilliseconds.Clear();
            }

            bool triggerPressed = Api.IsTriggerPressed;

            if (triggerPressed && _checkpointCount < MaximumCheckpoints)
            {
                _checkpointCount += 1;
                Quaternion pose = InputTracking.GetLocalRotation(XRNode.CenterEye);
                Vector3 euler = pose.eulerAngles;
                Emit(
                    "pose_checkpoint",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "index={0},x={1:F3},y={2:F3},z={3:F3},w={4:F3},pitch={5:F1},yaw={6:F1},roll={7:F1}",
                        _checkpointCount,
                        pose.x,
                        pose.y,
                        pose.z,
                        pose.w,
                        NormalizeAngle(euler.x),
                        NormalizeAngle(euler.y),
                        NormalizeAngle(euler.z)
                    )
                );
            }

            DetectReleasedHoldWithoutConsumingCardboardInput();

            bool cameraAuthorized = CameraAuthorization();
            if (cameraAuthorized != _cameraAuthorized)
            {
                _cameraAuthorized = cameraAuthorized;
                Emit(
                    "camera_authorization_changed",
                    "camera_authorized=" + cameraAuthorized.ToString().ToLowerInvariant()
                );
            }

            bool hasDeviceParams = Api.HasDeviceParams();
            if (hasDeviceParams != _hasDeviceParams)
            {
                _hasDeviceParams = hasDeviceParams;
                Emit(
                    "device_params_changed",
                    "has_device_params=" + hasDeviceParams.ToString().ToLowerInvariant()
                );
            }
        }

        private void OnApplicationPause(bool paused)
        {
            Emit(paused ? "background" : "resume", "source=application_pause");
        }

        private void OnApplicationFocus(bool focused)
        {
            Emit(
                focused ? "focus_gained" : "focus_lost",
                RuntimeStateDetails()
            );
        }

        private void OnApplicationQuit()
        {
            Emit("shutdown", "checkpoints=" + _checkpointCount.ToString(CultureInfo.InvariantCulture));
        }

        private void OnGUI()
        {
            GUI.Label(
                new Rect(24, 24, 900, 80),
                "S02 probe: tap/trigger once per direction (1-20), hold to recenter, then tap checkpoint 21."
            );
            GUI.Label(
                new Rect(24, 64, 500, 50),
                "Recorded checkpoints: "
                    + _checkpointCount.ToString(CultureInfo.InvariantCulture)
                    + "/21"
            );
        }

        private void EmitFrameWindow()
        {
            _frameTimesMilliseconds.Sort();
            double total = 0.0;
            foreach (float frameTime in _frameTimesMilliseconds)
            {
                total += frameTime;
            }

            int p95Index = Mathf.Clamp(
                Mathf.CeilToInt(_frameTimesMilliseconds.Count * 0.95f) - 1,
                0,
                _frameTimesMilliseconds.Count - 1
            );
            float average = (float)(total / _frameTimesMilliseconds.Count);
            Emit(
                "frame_window",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "samples={0},avg_ms={1:F3},p95_ms={2:F3},min_ms={3:F3},max_ms={4:F3}",
                    _frameTimesMilliseconds.Count,
                    average,
                    _frameTimesMilliseconds[p95Index],
                    _frameTimesMilliseconds[0],
                    _frameTimesMilliseconds[_frameTimesMilliseconds.Count - 1]
                )
            );
        }

        private void Emit(string eventName, string details)
        {
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

        private static bool CameraAuthorization()
        {
            return Application.HasUserAuthorization(UserAuthorization.WebCam);
        }

        private static string RuntimeStateDetails()
        {
            bool xrRunning =
                XRGeneralSettings.Instance != null
                && XRGeneralSettings.Instance.Manager != null
                && XRGeneralSettings.Instance.Manager.activeLoader != null;
            return string.Format(
                CultureInfo.InvariantCulture,
                "camera_authorized={0},has_device_params={1},xr_loader_active={2}",
                CameraAuthorization().ToString().ToLowerInvariant(),
                Api.HasDeviceParams().ToString().ToLowerInvariant(),
                xrRunning.ToString().ToLowerInvariant()
            );
        }

        private void DetectReleasedHoldWithoutConsumingCardboardInput()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                _touchTracking = false;
                return;
            }

            InputSystemTouchPhase phase = touchscreen.primaryTouch.phase.ReadValue();
            if (phase == InputSystemTouchPhase.Began)
            {
                _touchTracking = true;
                _touchStartedAt = Time.realtimeSinceStartupAsDouble;
                return;
            }

            if (
                _touchTracking
                && (phase == InputSystemTouchPhase.Ended || phase == InputSystemTouchPhase.Canceled)
            )
            {
                double duration = Time.realtimeSinceStartupAsDouble - _touchStartedAt;
                _touchTracking = false;
                if (duration >= 1.0)
                {
                    Emit(
                        "recenter_hold_released",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "duration_seconds={0:F3},next_checkpoint_is_post_recenter=true",
                            duration
                        )
                    );
                }
            }
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180.0f ? angle - 360.0f : angle;
        }
    }
}
