using System;
using OmniLensXR.S05;
using UnityEngine;

namespace OmniLensXR.S06
{
    public sealed class S06WorkspaceFixture : MonoBehaviour
    {
        [SerializeField]
        private Material _panelMaterial;
        private bool _ownsPanelMaterial;
        private PanelWorkspaceController _workspace;

        public PanelSnapshot Snapshot => _workspace?.Registry.CreateSnapshot();

        public FatigueProtocolSummary Metrics { get; private set; }

        public void ConfigurePanelMaterial(Material panelMaterial)
        {
            _panelMaterial = panelMaterial
                ?? throw new ArgumentNullException(nameof(panelMaterial));
            _ownsPanelMaterial = false;
        }

        private void Start()
        {
            if (_panelMaterial == null)
            {
                Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                if (shader == null)
                {
                    throw new InvalidOperationException(
                        "No S06 panel material or fallback shader is available."
                    );
                }

                _panelMaterial = new Material(shader)
                {
                    color = new Color(0.055f, 0.085f, 0.135f, 1.0f),
                };
                _ownsPanelMaterial = true;
            }

            GameObject root = new GameObject("S06PanelViews");
            root.transform.SetParent(transform, false);
            PanelRegistry registry = new PanelRegistry();
            _workspace = new PanelWorkspaceController(
                registry,
                new PanelFactory(root.transform, _panelMaterial)
            );
            Register("chat", PanelKind.Chat, new Vector3(-0.95f, 0.0f, 2.5f));
            Register("science-3d", PanelKind.Science3D, Vector3.forward * 2.5f);
            Register("document", PanelKind.Document, new Vector3(0.95f, 0.0f, 2.5f));
            Open("chat");
            Open("science-3d");
            Open("document");

            PanelManipulationController manipulation =
                new PanelManipulationController(_workspace);
            Require(manipulation.Apply(PanelHandleKind.MoveRight, "document"));
            Require(manipulation.Apply(PanelHandleKind.ResizeWider, "document"));
            Require(manipulation.Apply(PanelHandleKind.TogglePin, "document"));

            SpatialStringCatalog strings = new SpatialStringCatalog();
            if (
                !strings.TrySetLanguage(SpatialLanguage.Vietnamese)
                || !strings.TryGet(SpatialStringKey.Document, out LocalizedText document)
            )
            {
                throw new InvalidOperationException("S06 localization fixture failed.");
            }

            S06FatigueProtocol protocol = new S06FatigueProtocol();
            if (!protocol.Start(0.0))
            {
                throw new InvalidOperationException("S06 protocol failed to start.");
            }

            protocol.Record(FatigueInteractionKind.Selection, 0.1);
            protocol.Record(FatigueInteractionKind.HeadTurn, 0.2);
            protocol.Advance(5.0);
            Metrics = protocol.Summary();

            Debug.Log(
                $"[S06_FIXTURE] version={registry.Version} panels={registry.Count} "
                    + $"active={_workspace.Factory.ActiveCount} focused=document "
                    + $"pinned=document locale=vi-VN label={document.Value}"
            );
            Debug.Log(S06FatigueMetricLog.Format(Metrics));
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
            PanelPlacementResult placement = PanelPlacementService.Resolve(
                Array.Empty<PanelBounds>(),
                position,
                new Vector2(0.65f, 0.65f)
            );
            if (
                !placement.Accepted
                || !PanelDefinition.TryCreate(
                    panelId,
                    kind,
                    placement.Bounds,
                    out PanelDefinition definition
                )
            )
            {
                throw new InvalidOperationException(
                    $"Invalid S06 fixture panel: {panelId}."
                );
            }

            PanelApplyResult result = _workspace.Registry.Apply(
                PanelCommand.Register(_workspace.Registry.Version, definition)
            );
            if (!result.Accepted)
            {
                throw new InvalidOperationException(
                    $"S06 registration failed for {panelId}: {result.Error}."
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
                    $"S06 open failed for {panelId}: {result.Error}."
                );
            }
        }

        private static void Require(PanelManipulationResult result)
        {
            if (!result.Accepted)
            {
                throw new InvalidOperationException(
                    $"S06 manipulation failed: {result.Error}/"
                        + result.ApplyResult.Error
                );
            }
        }
    }
}
