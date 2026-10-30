using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using OmniLensXR.S05;
using UnityEngine;

namespace OmniLensXR.S07
{
    public static class S07WorkspaceDefaults
    {
        public static WorkspaceLayoutData Create()
        {
            PanelRegistry registry = new PanelRegistry();
            Register(registry, "home", PanelKind.Chat, -0.8f);
            Register(registry, "science-3d", PanelKind.Science3D, 0.0f);
            Register(registry, "document", PanelKind.Document, 0.8f);
            Open(registry, "home");
            Open(registry, "science-3d");
            Open(registry, "document");
            Require(
                registry.Apply(
                    PanelCommand.For(
                        PanelCommandKind.SetPinned,
                        registry.Version,
                        "document",
                        true
                    )
                )
            );
            return WorkspaceLayoutCodec.Capture(
                registry.CreateSnapshot(),
                WorkspaceApp.Home,
                0
            );
        }

        private static void Register(
            PanelRegistry registry,
            string panelId,
            PanelKind kind,
            float x
        )
        {
            if (
                !PanelDefinition.TryCreate(
                    panelId,
                    kind,
                    new PanelBounds(
                        new Vector3(x, 0.0f, 2.5f),
                        new Vector2(0.5f, 0.5f)
                    ),
                    out PanelDefinition definition
                )
            )
            {
                throw new InvalidOperationException("Invalid S07 default panel.");
            }

            Require(registry.Apply(PanelCommand.Register(registry.Version, definition)));
        }

        private static void Open(PanelRegistry registry, string panelId)
        {
            Require(
                registry.Apply(
                    PanelCommand.For(
                        PanelCommandKind.Open,
                        registry.Version,
                        panelId
                    )
                )
            );
        }

        private static void Require(PanelApplyResult result)
        {
            if (!result.Accepted)
            {
                throw new InvalidOperationException(
                    $"S07 default layout failed: {result.Error}."
                );
            }
        }
    }

    public sealed class S07WorkspaceFixture : MonoBehaviour
    {
        [SerializeField]
        private Material _panelMaterial;
        private bool _ownsPanelMaterial;
        private int _frameCount;
        private float _frameElapsed;
        private bool _fpsLogged;

        public bool RestoredExistingState { get; private set; }
        public int ActivePanelCount { get; private set; }
        public OfflineShellError DeniedAgentError { get; private set; }
        public SpatialInputFallback DeniedInputFallback { get; private set; }
        public int ActivityFixtureCount { get; private set; }

        public void ConfigurePanelMaterial(Material panelMaterial)
        {
            _panelMaterial = panelMaterial
                ?? throw new ArgumentNullException(nameof(panelMaterial));
            _ownsPanelMaterial = false;
        }

        private void Start()
        {
            EnsureMaterial();
            string root = Path.Combine(
                Application.persistentDataPath,
                "omnilens-s07-fixture"
            );
            SandboxWorkspaceRepository repository =
                new SandboxWorkspaceRepository(root);
            WorkspaceLayoutData layout;
            if (repository.TryLoad(out layout, out WorkspaceRepositoryError loadError))
            {
                RestoredExistingState = true;
            }
            else
            {
                if (
                    loadError != WorkspaceRepositoryError.Missing
                    && !repository.TryReset(out _)
                )
                {
                    throw new InvalidOperationException(
                        $"S07 invalid repository could not reset: {loadError}."
                    );
                }

                layout = S07WorkspaceDefaults.Create();
                layout.savedUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                if (!repository.TrySave(layout, out WorkspaceRepositoryError saveError))
                {
                    throw new InvalidOperationException(
                        $"S07 fixture save failed: {saveError}."
                    );
                }

                if (!repository.TryLoad(out layout, out loadError))
                {
                    throw new InvalidOperationException(
                        $"S07 fixture round-trip failed: {loadError}."
                    );
                }
            }

            if (
                !WorkspaceLayoutCodec.TryRestoreRegistry(
                    layout,
                    out PanelRegistry registry,
                    out WorkspaceLayoutError restoreError
                )
            )
            {
                throw new InvalidOperationException(
                    $"S07 fixture restore failed: {restoreError}."
                );
            }

            GameObject rootObject = new GameObject("S07PanelViews");
            rootObject.transform.SetParent(transform, false);
            PanelFactory factory = new PanelFactory(rootObject.transform, _panelMaterial);
            foreach (PanelState state in registry.CreateSnapshot().Panels)
            {
                if (
                    state.Visibility == PanelVisibility.Visible
                    && !factory.TryAcquire(state, out _)
                )
                {
                    throw new InvalidOperationException(
                        $"S07 active view restore failed: {state.PanelId}."
                    );
                }
            }

            ActivePanelCount = factory.ActiveCount;
            OfflineWorkspaceShell shell = new OfflineWorkspaceShell(
                repository,
                S07WorkspaceDefaults.Create
            );
            shell.SetNetworkState(WorkspaceNetworkState.Offline);
            Require(shell.Apply(OfflineShellAction.OpenScience3D));
            Require(shell.Apply(OfflineShellAction.OpenSettings));
            OfflineShellResult deniedAgent = shell.Apply(OfflineShellAction.OpenAgent);
            DeniedAgentError = deniedAgent.Error;
            DeniedInputFallback = S07InputFallback.Resolve(false);
            IReadOnlyList<AgentActivityFixture> fixtures =
                AgentActivityFixtureFactory.CreateThirty();
            ActivityFixtureCount = fixtures.Count;

            Debug.Log(
                $"[S07_FIXTURE] schema={layout.schemaVersion} restored="
                    + $"{RestoredExistingState.ToString().ToLowerInvariant()} "
                    + $"panels={registry.Count} active={ActivePanelCount} "
                    + $"offline_local=true denied_agent={DeniedAgentError} "
                    + $"input_fallback={DeniedInputFallback} activities={ActivityFixtureCount}"
            );
            Debug.Log("[S07_GATE] viewer_video=NOT_RUN viewer_interaction=NOT_RUN");
        }

        private void Update()
        {
            if (_fpsLogged)
            {
                return;
            }

            _frameCount += 1;
            _frameElapsed += Time.unscaledDeltaTime;
            if (_frameCount < 60 || _frameElapsed <= 0.0f)
            {
                return;
            }

            double fps = _frameCount / (double)_frameElapsed;
            Debug.Log(
                "[S07_FPS] frames="
                    + _frameCount.ToString(CultureInfo.InvariantCulture)
                    + " fps="
                    + fps.ToString("F1", CultureInfo.InvariantCulture)
                    + " physical_viewer=NOT_RUN"
            );
            _fpsLogged = true;
        }

        private void EnsureMaterial()
        {
            if (_panelMaterial != null)
            {
                return;
            }

            Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            if (shader == null)
            {
                throw new InvalidOperationException("No S07 panel shader is available.");
            }

            _panelMaterial = new Material(shader)
            {
                color = new Color(0.045f, 0.095f, 0.135f, 1.0f),
            };
            _ownsPanelMaterial = true;
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

        private static void Require(OfflineShellResult result)
        {
            if (!result.Accepted)
            {
                throw new InvalidOperationException(
                    $"S07 offline shell failed: {result.Error}."
                );
            }
        }
    }
}
