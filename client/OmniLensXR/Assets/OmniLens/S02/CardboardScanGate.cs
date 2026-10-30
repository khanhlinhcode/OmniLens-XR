using UnityEngine;

namespace OmniLensXR.S02
{
    /// <summary>
    /// Prevents the native Cardboard QR scanner from opening while the physical device is
    /// still in portrait. The scanner is landscape-only in this project, and presenting it
    /// in portrait causes its visible controls and touch hit boxes to be rotated apart.
    /// </summary>
    public static class CardboardScanGate
    {
        public static bool IsLandscape(DeviceOrientation orientation)
        {
            return orientation == DeviceOrientation.LandscapeLeft
                || orientation == DeviceOrientation.LandscapeRight;
        }
    }
}
