using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;

namespace OmniLensXR.S08
{
    public sealed class S08SceneFixture : MonoBehaviour
    {
        public const long MaximumObservedMemoryGrowthBytes = 32L * 1024L * 1024L;
        [SerializeField]
        private Material _sceneMaterial;
        private SceneAssetLease _displayLease;

        public bool Completed { get; private set; }
        public int CompletedCycles { get; private set; }
        public int ActiveAfterCycles { get; private set; }
        public int PeakActive { get; private set; }
        public SceneLoadOutcome DisplayOutcome { get; private set; }
        public SceneAssetMetrics Metrics { get; private set; }
        public long SelectionVersion { get; private set; }
        public long MemoryBeforeBytes { get; private set; }
        public long MemoryAfterBytes { get; private set; }
        public long MemoryDeltaBytes { get; private set; }
        public bool MemoryWithinBound { get; private set; }

        public void ConfigureMaterial(Material material)
        {
            _sceneMaterial = material ?? throw new ArgumentNullException(nameof(material));
        }

        private IEnumerator Start()
        {
            string root = Path.Combine(Application.streamingAssetsPath, "S08");
            string manifestPath = Path.Combine(root, "scene-manifest-v1.json");
            if (!File.Exists(manifestPath))
            {
                throw new InvalidOperationException("S08 scene manifest is missing.");
            }

            if (
                !SceneManifestCodec.TryDeserialize(
                    File.ReadAllText(manifestPath),
                    out SceneManifestData manifest,
                    out SceneManifestError manifestError
                )
            )
            {
                throw new InvalidOperationException(
                    $"S08 manifest rejected: {manifestError}."
                );
            }

            SceneAssetLifecycleTracker tracker = new SceneAssetLifecycleTracker();
            LocalSceneAssetLoader loader = new LocalSceneAssetLoader(
                new FileSceneAssetSource(),
                new RealtimeSceneLoadClock(),
                tracker,
                _sceneMaterial
            );
            SceneAssetMetrics lastMetrics = default;
            MemoryBeforeBytes = Profiler.GetTotalAllocatedMemoryLong();
            for (int index = 0; index < 50; index += 1)
            {
                SceneAssetLease lease = loader.Load(manifest, root);
                if (lease.Outcome != SceneLoadOutcome.Success)
                {
                    lease.Dispose();
                    throw new InvalidOperationException(
                        $"S08 benchmark load {index} failed: {lease.Outcome}."
                    );
                }

                lastMetrics = lease.Metrics;
                lease.Dispose();
                CompletedCycles += 1;
                yield return null;
            }

            ActiveAfterCycles = tracker.Active;
            PeakActive = tracker.PeakActive;
            yield return Resources.UnloadUnusedAssets();
            GC.Collect();
            yield return null;
            MemoryAfterBytes = Profiler.GetTotalAllocatedMemoryLong();
            MemoryDeltaBytes = MemoryAfterBytes - MemoryBeforeBytes;
            MemoryWithinBound = MemoryDeltaBytes <= MaximumObservedMemoryGrowthBytes;
            if (
                tracker.Created != 50
                || tracker.Disposed != 50
                || ActiveAfterCycles != 0
                || PeakActive != 1
                || !MemoryWithinBound
            )
            {
                throw new InvalidOperationException(
                    $"S08 lifecycle imbalance: created={tracker.Created} "
                        + $"disposed={tracker.Disposed} active={tracker.Active} "
                        + $"peak={tracker.PeakActive} memory_delta={MemoryDeltaBytes}."
                );
            }

            _displayLease = loader.Load(manifest, root);
            DisplayOutcome = _displayLease.Outcome;
            Metrics = _displayLease.Metrics;
            if (DisplayOutcome != SceneLoadOutcome.Success)
            {
                throw new InvalidOperationException(
                    $"S08 display load failed: {DisplayOutcome}."
                );
            }

            _displayLease.Root.transform.SetParent(transform, false);
            _displayLease.Root.transform.localPosition = Vector3.forward * 2.5f;
            _displayLease.Root.transform.localScale = Vector3.one * 0.8f;
            Dictionary<string, ScenePartView> views = CreateViews(
                manifest,
                _displayLease.Root
            );
            SceneSelectionController selection = new SceneSelectionController(
                manifest,
                views
            );
            Require(selection.Apply(Command(SceneSelectionAction.Select, selection, "body")));
            Require(selection.Apply(Command(SceneSelectionAction.Highlight, selection, "marker-left")));
            Require(selection.Apply(Command(SceneSelectionAction.Isolate, selection, "body")));
            Require(selection.Apply(Command(SceneSelectionAction.Focus, selection, "body")));
            Require(selection.Apply(SceneSelectionCommand.Clear(selection.State.Version)));
            SelectionVersion = selection.State.Version;
            Completed = true;

            Debug.Log(
                "[S08_FIXTURE] schema=1 model="
                    + manifest.modelId
                    + " asset="
                    + Metrics.AssetId
                    + " outcome="
                    + DisplayOutcome
                    + " fallback="
                    + Metrics.UsedFallback.ToString().ToLowerInvariant()
                    + " commands="
                    + SelectionVersion.ToString(CultureInfo.InvariantCulture)
            );
            Debug.Log(
                "[S08_METRICS] cycles="
                    + CompletedCycles.ToString(CultureInfo.InvariantCulture)
                    + " active_after_cycles="
                    + ActiveAfterCycles.ToString(CultureInfo.InvariantCulture)
                    + " peak="
                    + PeakActive.ToString(CultureInfo.InvariantCulture)
                    + " bytes="
                    + lastMetrics.BundleBytes.ToString(CultureInfo.InvariantCulture)
                    + " vertices="
                    + lastMetrics.Vertices.ToString(CultureInfo.InvariantCulture)
                    + " triangles="
                    + lastMetrics.Triangles.ToString(CultureInfo.InvariantCulture)
                    + " texture_bytes="
                    + lastMetrics.TextureBytes.ToString(CultureInfo.InvariantCulture)
                    + " memory_before="
                    + MemoryBeforeBytes.ToString(CultureInfo.InvariantCulture)
                    + " memory_after="
                    + MemoryAfterBytes.ToString(CultureInfo.InvariantCulture)
                    + " memory_delta="
                    + MemoryDeltaBytes.ToString(CultureInfo.InvariantCulture)
                    + " memory_bound="
                    + MaximumObservedMemoryGrowthBytes.ToString(CultureInfo.InvariantCulture)
                    + " viewer=NOT_RUN"
            );
        }

        private void OnDestroy()
        {
            _displayLease?.Dispose();
        }

        private static SceneSelectionCommand Command(
            SceneSelectionAction action,
            SceneSelectionController controller,
            string partId
        )
        {
            return SceneSelectionCommand.For(
                action,
                controller.State.Version,
                partId
            );
        }

        private static Dictionary<string, ScenePartView> CreateViews(
            SceneManifestData manifest,
            GameObject root
        )
        {
            Dictionary<string, ScenePartView> views =
                new Dictionary<string, ScenePartView>(StringComparer.Ordinal);
            foreach (ScenePartData part in manifest.parts)
            {
                Transform target = FindRecursive(root.transform, part.targetName);
                if (target == null)
                {
                    throw new InvalidOperationException(
                        $"S08 loaded hierarchy is missing {part.targetName}."
                    );
                }

                ScenePartView view = target.gameObject.AddComponent<ScenePartView>();
                view.Initialize(part.partId);
                views.Add(part.partId, view);
            }

            return views;
        }

        private static Transform FindRecursive(Transform current, string targetName)
        {
            if (current.name == targetName)
            {
                return current;
            }

            for (int index = 0; index < current.childCount; index += 1)
            {
                Transform found = FindRecursive(current.GetChild(index), targetName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void Require(SceneSelectionResult result)
        {
            if (!result.Accepted)
            {
                throw new InvalidOperationException(
                    $"S08 selection failed: {result.Error}."
                );
            }
        }
    }
}
