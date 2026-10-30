using System;
using UnityEngine;

namespace OmniLensXR.S03
{
    public interface IGazeDwellTarget
    {
        bool CanSelectByDwell { get; }

        int DwellTargetId { get; }

        void SelectByDwell();
    }

    /// <summary>
    /// Optional no-trigger fallback. Only components implementing IGazeDwellTarget on an
    /// allowlisted physics layer can receive a selection.
    /// </summary>
    public sealed class GazeDwellSelector : MonoBehaviour
    {
        private const int DefaultInteractionLayerMask = (1 << 7) | (1 << 8);
        private const int ProgressSegments = 32;

        [SerializeField]
        [Range(0.5f, 5.0f)]
        private float _dwellSeconds = 1.5f;

        [SerializeField]
        [Range(0.0f, 5.0f)]
        private float _cooldownSeconds = 0.75f;

        [SerializeField]
        [Range(0.5f, 20.0f)]
        private float _maximumDistanceMeters = 20.0f;

        [SerializeField]
        private LayerMask _interactionLayers = DefaultInteractionLayerMask;

        [SerializeField]
        private bool _showProgress = true;

        [SerializeField]
        private bool _reducedMotion = true;

        private bool _applicationFocused = true;
        private GazeDwellStateMachine _stateMachine;
        private LineRenderer _progressRing;
        private Material _progressMaterial;

        public GazeDwellPhase Phase { get; private set; } = GazeDwellPhase.Idle;

        public float Progress { get; private set; }

        public bool ReducedMotion => _reducedMotion;

        private void Awake()
        {
            _stateMachine = new GazeDwellStateMachine(
                _dwellSeconds,
                _cooldownSeconds
            );
            if (_showProgress)
            {
                CreateProgressRing();
            }
        }

        private void Update()
        {
            IGazeDwellTarget target = FindAllowlistedTarget(out float targetDistance);
            int? targetId = target != null && target.CanSelectByDwell
                ? target.DwellTargetId
                : null;
            GazeDwellStepResult result = _stateMachine.Advance(
                targetId,
                Time.unscaledDeltaTime,
                _applicationFocused && isActiveAndEnabled
            );
            Phase = result.Phase;
            Progress = result.Progress;
            UpdateProgressRing(targetId.HasValue, targetDistance);

            if (!result.SelectionTriggered || target == null)
            {
                return;
            }

            IGazeDwellTarget verified = FindAllowlistedTarget(out _);
            if (
                verified == null
                || !verified.CanSelectByDwell
                || verified.DwellTargetId != targetId.Value
            )
            {
                Debug.LogWarning("[S03_DWELL] selection_canceled reason=target_changed");
                return;
            }

            try
            {
                verified.SelectByDwell();
                Debug.Log("[S03_DWELL] selection_triggered");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[S03_DWELL] selection_failed=" + exception.GetType().Name
                );
            }
        }

        private void OnApplicationFocus(bool focused)
        {
            _applicationFocused = focused;
            if (!focused)
            {
                CancelInteraction();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                CancelInteraction();
            }
        }

        private void OnDisable()
        {
            CancelInteraction();
        }

        private void OnDestroy()
        {
            if (_progressMaterial != null)
            {
                Destroy(_progressMaterial);
            }
        }

        private void OnValidate()
        {
            _dwellSeconds = Mathf.Clamp(_dwellSeconds, 0.5f, 5.0f);
            _cooldownSeconds = Mathf.Clamp(_cooldownSeconds, 0.0f, 5.0f);
            _maximumDistanceMeters = Mathf.Clamp(_maximumDistanceMeters, 0.5f, 20.0f);
        }

        public static bool IsLayerAllowed(int layer, int layerMask)
        {
            return layer >= 0 && layer <= 31 && (layerMask & (1 << layer)) != 0;
        }

        private void CancelInteraction()
        {
            _stateMachine?.Reset();
            Phase = GazeDwellPhase.Idle;
            Progress = 0.0f;
            if (_progressRing != null)
            {
                _progressRing.enabled = false;
            }
        }

        private IGazeDwellTarget FindAllowlistedTarget(out float distance)
        {
            distance = _maximumDistanceMeters;
            if (
                !Physics.Raycast(
                    transform.position,
                    transform.forward,
                    out RaycastHit hit,
                    _maximumDistanceMeters
                )
                || !IsLayerAllowed(hit.collider.gameObject.layer, _interactionLayers.value)
            )
            {
                return null;
            }

            distance = hit.distance;
            MonoBehaviour[] candidates = hit.collider.GetComponentsInParent<MonoBehaviour>(true);
            foreach (MonoBehaviour candidate in candidates)
            {
                if (candidate is IGazeDwellTarget dwellTarget)
                {
                    return dwellTarget;
                }
            }

            return null;
        }

        private void CreateProgressRing()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogWarning("[S03_DWELL] progress_disabled reason=shader_missing");
                return;
            }

            GameObject ringObject = new GameObject("GazeDwellProgressRing");
            ringObject.transform.SetParent(transform, false);
            _progressRing = ringObject.AddComponent<LineRenderer>();
            _progressMaterial = new Material(shader);
            _progressRing.material = _progressMaterial;
            _progressRing.useWorldSpace = false;
            _progressRing.loop = false;
            _progressRing.widthMultiplier = 0.004f;
            _progressRing.startColor = new Color(0.20f, 0.85f, 1.0f, 0.95f);
            _progressRing.endColor = _progressRing.startColor;
            _progressRing.numCapVertices = _reducedMotion ? 0 : 4;
            _progressRing.enabled = false;
        }

        private void UpdateProgressRing(bool hasTarget, float targetDistance)
        {
            if (_progressRing == null)
            {
                return;
            }

            bool visible = hasTarget
                && (Phase == GazeDwellPhase.Focusing || Phase == GazeDwellPhase.Selected);
            _progressRing.enabled = visible;
            if (!visible)
            {
                return;
            }

            int segmentCount = Mathf.Max(2, Mathf.CeilToInt(ProgressSegments * Progress));
            _progressRing.positionCount = segmentCount + 1;
            float radius = Mathf.Clamp(targetDistance * 0.015f, 0.012f, 0.08f);
            float z = Mathf.Clamp(targetDistance - 0.01f, 0.45f, _maximumDistanceMeters);
            for (int index = 0; index <= segmentCount; index += 1)
            {
                float normalized = (float)index / ProgressSegments;
                float angle = normalized * Mathf.PI * 2.0f;
                _progressRing.SetPosition(
                    index,
                    new Vector3(Mathf.Sin(angle) * radius, Mathf.Cos(angle) * radius, z)
                );
            }
        }
    }
}
