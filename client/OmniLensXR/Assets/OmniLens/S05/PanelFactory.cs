using System;
using System.Collections.Generic;
using UnityEngine;

namespace OmniLensXR.S05
{
    public sealed class PanelView : MonoBehaviour
    {
        private TextMesh _label;

        public string PanelId { get; private set; }

        public PanelKind Kind { get; private set; }

        public bool IsFocused { get; private set; }

        public bool IsPinned { get; private set; }

        public void Initialize(TextMesh label)
        {
            _label = label;
        }

        public void Configure(PanelState state)
        {
            PanelId = state.PanelId;
            Kind = state.Kind;
            IsFocused = state.IsFocused;
            IsPinned = state.IsPinned;
            gameObject.name = "PanelView-" + state.PanelId;
            transform.localPosition = state.Bounds.PositionMeters;
            transform.localRotation = Quaternion.Euler(
                0.0f,
                S04.SpatialLayoutGeometry.FacingYawDegrees(
                    state.Bounds.PositionMeters
                ),
                0.0f
            );
            transform.localScale = new Vector3(
                state.Bounds.SizeMeters.x,
                state.Bounds.SizeMeters.y,
                1.0f
            );
            if (_label == null)
            {
                _label = GetComponentInChildren<TextMesh>(true);
            }

            if (_label != null)
            {
                _label.text = state.PanelId.ToUpperInvariant()
                    + (state.IsPinned ? "\nPINNED" : string.Empty);
                _label.color = state.IsFocused ? Color.cyan : Color.white;
                _label.characterSize = state.PanelId.Length >= 8
                    ? S04.SpatialUiTokens.DetailTextHeightMeters
                    : S04.SpatialUiTokens.BodyTextHeightMeters;
                _label.transform.localScale = new Vector3(
                    1.0f / state.Bounds.SizeMeters.x,
                    1.0f / state.Bounds.SizeMeters.y,
                    1.0f
                );
            }
        }
    }

    public sealed class PanelFactory
    {
        public const int MaximumPooledPerKind = 4;

        private readonly Transform _root;
        private readonly Dictionary<string, PanelView> _active =
            new Dictionary<string, PanelView>(StringComparer.Ordinal);
        private readonly Dictionary<PanelKind, Stack<PanelView>> _pools =
            new Dictionary<PanelKind, Stack<PanelView>>();
        private readonly Material _sharedMaterial;

        public PanelFactory(Transform root, Material sharedMaterial = null)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _sharedMaterial = sharedMaterial;
        }

        public int ActiveCount => _active.Count;

        public bool TryAcquire(PanelState state, out PanelView view)
        {
            view = null;
            if (
                state == null
                || state.Visibility != PanelVisibility.Visible
                || !IsAllowlisted(state.Kind)
                || _active.ContainsKey(state.PanelId)
            )
            {
                return false;
            }

            Stack<PanelView> pool = Pool(state.Kind);
            view = pool.Count > 0 ? pool.Pop() : CreateView(state.Kind);
            view.gameObject.SetActive(true);
            view.Configure(state);
            _active.Add(state.PanelId, view);
            return true;
        }

        public bool TryGetActive(string panelId, out PanelView view)
        {
            view = null;
            return panelId != null && _active.TryGetValue(panelId, out view);
        }

        public bool Release(string panelId)
        {
            if (panelId == null || !_active.TryGetValue(panelId, out PanelView view))
            {
                return false;
            }

            _active.Remove(panelId);
            view.gameObject.SetActive(false);
            Stack<PanelView> pool = Pool(view.Kind);
            if (pool.Count < MaximumPooledPerKind)
            {
                pool.Push(view);
            }
            else
            {
                DestroyObject(view.gameObject);
            }

            return true;
        }

        public int PooledCount(PanelKind kind)
        {
            return _pools.TryGetValue(kind, out Stack<PanelView> pool)
                ? pool.Count
                : 0;
        }

        private PanelView CreateView(PanelKind kind)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            gameObject.name = "PooledPanel-" + kind;
            gameObject.transform.SetParent(_root, false);
            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyObject(collider);
            }

            MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();
            if (_sharedMaterial != null)
            {
                renderer.sharedMaterial = _sharedMaterial;
            }

            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(gameObject.transform, false);
            labelObject.transform.localPosition = new Vector3(0.0f, 0.0f, 0.01f);
            labelObject.transform.localRotation = Quaternion.identity;
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 32;
            label.characterSize = S04.SpatialUiTokens.BodyTextHeightMeters;
            label.color = Color.white;

            PanelView view = gameObject.AddComponent<PanelView>();
            view.Initialize(label);
            return view;
        }

        private Stack<PanelView> Pool(PanelKind kind)
        {
            if (!_pools.TryGetValue(kind, out Stack<PanelView> pool))
            {
                pool = new Stack<PanelView>();
                _pools.Add(kind, pool);
            }

            return pool;
        }

        private static bool IsAllowlisted(PanelKind kind)
        {
            return kind == PanelKind.Chat
                || kind == PanelKind.Science3D
                || kind == PanelKind.Document
                || kind == PanelKind.Notes;
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }

    public sealed class PanelWorkspaceController
    {
        public PanelWorkspaceController(PanelRegistry registry, PanelFactory factory)
        {
            Registry = registry ?? throw new ArgumentNullException(nameof(registry));
            Factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public PanelRegistry Registry { get; }

        public PanelFactory Factory { get; }

        public PanelApplyResult Apply(PanelCommand command)
        {
            if (
                command.Kind != PanelCommandKind.Register
                && command.ExpectedVersion == Registry.Version
                && Registry.TryGet(command.PanelId, out PanelState current)
                && Factory.TryGetActive(command.PanelId, out _)
                    != (current.Visibility == PanelVisibility.Visible)
            )
            {
                return new PanelApplyResult(
                    false,
                    PanelApplyError.ViewStateConflict,
                    Registry.Version,
                    null
                );
            }

            PanelApplyResult result = Registry.Apply(command);
            if (!result.Accepted)
            {
                return result;
            }

            switch (command.Kind)
            {
                case PanelCommandKind.Open:
                case PanelCommandKind.Restore:
                    Registry.TryGet(command.PanelId, out PanelState opened);
                    if (!Factory.TryAcquire(opened, out _))
                    {
                        throw new InvalidOperationException(
                            "Accepted panel open could not acquire a view."
                        );
                    }

                    break;
                case PanelCommandKind.Close:
                case PanelCommandKind.Minimize:
                    if (
                        Factory.TryGetActive(command.PanelId, out _)
                        && !Factory.Release(command.PanelId)
                    )
                    {
                        throw new InvalidOperationException(
                            "Accepted panel close could not release its view."
                        );
                    }

                    break;
            }

            SynchronizeActiveViews();

            return result;
        }

        private void SynchronizeActiveViews()
        {
            foreach (PanelState state in Registry.CreateSnapshot().Panels)
            {
                if (
                    state.Visibility == PanelVisibility.Visible
                    && Factory.TryGetActive(state.PanelId, out PanelView view)
                )
                {
                    view.Configure(state);
                }
            }
        }
    }
}
