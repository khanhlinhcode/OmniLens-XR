using System;
using UnityEngine;

namespace OmniLensXR.S05
{
    public sealed class S05PanelLifecycleFixture : MonoBehaviour
    {
        private PanelWorkspaceController _workspace;
        [SerializeField]
        private Material _panelMaterial;
        private bool _ownsPanelMaterial;

        public PanelSnapshot Snapshot => _workspace?.Registry.CreateSnapshot();

        public void ConfigurePanelMaterial(Material panelMaterial)
        {
            _panelMaterial = panelMaterial
                ?? throw new ArgumentNullException(nameof(panelMaterial));
            _ownsPanelMaterial = false;
        }

        private void Start()
        {
            GameObject viewRoot = new GameObject("S05PanelViews");
            viewRoot.transform.SetParent(transform, false);
            if (_panelMaterial == null)
            {
                Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                if (shader == null)
                {
                    throw new InvalidOperationException(
                        "No S05 panel material or fallback shader is available."
                    );
                }

                _panelMaterial = new Material(shader)
                {
                    color = new Color(0.060f, 0.090f, 0.140f, 1.0f),
                };
                _ownsPanelMaterial = true;
            }
            PanelRegistry registry = new PanelRegistry();
            _workspace = new PanelWorkspaceController(
                registry,
                new PanelFactory(viewRoot.transform, _panelMaterial)
            );

            Register("chat", PanelKind.Chat, new Vector3(-0.95f, 0.0f, 2.3f));
            Register("science-3d", PanelKind.Science3D, new Vector3(0.0f, 0.0f, 2.5f));
            Register("document", PanelKind.Document, new Vector3(0.95f, 0.0f, 2.3f));
            Register("notes", PanelKind.Notes, new Vector3(0.0f, -0.7f, 2.5f));

            Open("chat");
            Open("science-3d");
            Open("document");
            Debug.Log(
                $"[S05_FIXTURE] version={registry.Version} "
                    + $"registered={registry.Count} active={_workspace.Factory.ActiveCount}"
            );
            Debug.Log(
                "[S05_SNAPSHOT] version=7 focused=document "
                    + "visible=chat,document,science-3d"
            );
        }

        private void OnDestroy()
        {
            if (_panelMaterial == null || !_ownsPanelMaterial)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_panelMaterial);
            }
            else
            {
                DestroyImmediate(_panelMaterial);
            }
        }

        private void Register(string panelId, PanelKind kind, Vector3 position)
        {
            if (
                !PanelDefinition.TryCreate(
                    panelId,
                    kind,
                    new PanelBounds(position, new Vector2(0.75f, 0.75f)),
                    out PanelDefinition definition
                )
            )
            {
                throw new InvalidOperationException("Invalid S05 fixture panel definition.");
            }

            PanelApplyResult result = _workspace.Registry.Apply(
                PanelCommand.Register(_workspace.Registry.Version, definition)
            );
            if (!result.Accepted)
            {
                throw new InvalidOperationException(
                    $"S05 registration failed: {result.Error}."
                );
            }
        }

        private void Open(string panelId)
        {
            PanelApplyResult result = _workspace.Apply(
                PanelCommand.For(
                    PanelCommandKind.Open,
                    _workspace.Registry.Version,
                    panelId
                )
            );
            if (!result.Accepted)
            {
                throw new InvalidOperationException(
                    $"S05 open failed for {panelId}: {result.Error}."
                );
            }
        }
    }
}
