using System;
using Google.XR.Cardboard;
using UnityEngine;

namespace OmniLensXR.S03
{
    /// <summary>
    /// Explicit world-space recenter action for viewers whose trigger is absent or broken.
    /// The selector provides cooldown and gaze-exit re-arm before this can run again.
    /// </summary>
    public sealed class GazeDwellRecenterTarget : MonoBehaviour, IGazeDwellTarget
    {
        public bool CanSelectByDwell => isActiveAndEnabled;

        public int DwellTargetId => GetInstanceID();

        public void SelectByDwell()
        {
            if (TryInvoke(Api.Recenter))
            {
                Debug.Log("[S03_DWELL] recenter_triggered");
                return;
            }

            Debug.LogError("[S03_DWELL] recenter_failed");
        }

        public static bool TryInvoke(Action recenterAction)
        {
            if (recenterAction == null)
            {
                return false;
            }

            try
            {
                recenterAction();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

