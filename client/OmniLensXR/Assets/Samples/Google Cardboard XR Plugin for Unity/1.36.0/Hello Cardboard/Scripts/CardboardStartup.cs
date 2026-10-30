//-----------------------------------------------------------------------
// <copyright file="CardboardStartup.cs" company="Google LLC">
// Copyright 2020 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//-----------------------------------------------------------------------

using Google.XR.Cardboard;
using OmniLensXR.S02;
using UnityEngine;

/// <summary>
/// Initializes Cardboard XR Plugin.
/// </summary>
public class CardboardStartup : MonoBehaviour
{
    private const string RotatePrompt =
        "Rotate iPhone to landscape before opening the Cardboard QR scanner.";

    private bool _scanPending;
    private bool _showRotatePrompt;

    /// <summary>
    /// Start is called before the first frame update.
    /// </summary>
    public void Start()
    {
        // Configures the app to not shut down the screen and sets the brightness to maximum.
        // Brightness control is expected to work only in iOS, see:
        // https://docs.unity3d.com/ScriptReference/Screen-brightness.html.
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.brightness = 1.0f;

        // Checks if the device parameters are stored and scans them if not.
        if (!Api.HasDeviceParams())
        {
            RequestDeviceParamsScan();
        }
    }

    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    public void Update()
    {
        if (Api.IsGearButtonPressed)
        {
            RequestDeviceParamsScan();
        }

        TryLaunchPendingScan();

        if (Api.IsCloseButtonPressed)
        {
            Application.Quit();
        }

        if (Api.IsTriggerHeldPressed)
        {
            Api.Recenter();
        }

        if (Api.HasNewDeviceParams())
        {
            Api.ReloadDeviceParams();
        }

        Api.UpdateScreenParams();
    }

    private void OnGUI()
    {
        if (!_showRotatePrompt)
        {
            return;
        }

        float width = Mathf.Min(Screen.width - 48.0f, 760.0f);
        GUI.Box(
            new Rect((Screen.width - width) * 0.5f, 24.0f, width, 72.0f),
            RotatePrompt
        );
    }

    private void RequestDeviceParamsScan()
    {
        if (CardboardScanGate.IsLandscape(Input.deviceOrientation))
        {
            LaunchDeviceParamsScan();
            return;
        }

        _scanPending = true;
        _showRotatePrompt = true;
        Debug.LogWarning(
            "[S02_QR] scan_deferred reason=device_portrait orientation="
                + Input.deviceOrientation
        );
    }

    private void TryLaunchPendingScan()
    {
        if (!_scanPending || !CardboardScanGate.IsLandscape(Input.deviceOrientation))
        {
            return;
        }

        LaunchDeviceParamsScan();
    }

    private void LaunchDeviceParamsScan()
    {
        _scanPending = false;
        _showRotatePrompt = false;
        Debug.Log("[S02_QR] scan_launched orientation=" + Input.deviceOrientation);
        Api.ScanDeviceParams();
    }
}
