using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using OmniLensXR.S05;
using OmniLensXR.S08;
using UnityEngine;
using UnityEngine.Profiling;

namespace OmniLensXR.S09
{
    public sealed class S09SceneFixture : MonoBehaviour
    {
        public const int LifecycleCycles = 50;
        public const int SelectionCommands = 20;
        public const long MaximumObservedMemoryGrowthBytes = 32L * 1024L * 1024L;

        [SerializeField]
        private Material _sceneMaterial;

        [SerializeField]
        private Material _panelMaterial;

        private SceneAssetLease _displayLease;

        public bool Completed { get; private set; }
        public int AckCompleted { get; private set; }
        public int ExactSelections { get; private set; }
        public bool DuplicateReplayed { get; private set; }
        public int ExecutorCalls { get; private set; }
        public int ClickThroughCount { get; private set; }
        public int LifecycleCompleted { get; private set; }
        public int LifecycleActive { get; private set; }
        public int LifecyclePeak { get; private set; }
        public long MemoryBeforeBytes { get; private set; }
        public long MemoryAfterBytes { get; private set; }
        public long MemoryDeltaBytes { get; private set; }
        public bool MemoryWithinBound { get; private set; }
        public float ObservedFps { get; private set; }
        public SceneLoadOutcome MissingOutcome { get; private set; }
        public SceneLoadOutcome MalformedOutcome { get; private set; }
        public SceneLoadOutcome HashMismatchOutcome { get; private set; }
        public AnimationPlaybackState AnimationState { get; private set; }
        public long SceneVersion { get; private set; }

        public void ConfigureMaterials(Material sceneMaterial, Material panelMaterial)
        {
            _sceneMaterial = sceneMaterial
                ?? throw new ArgumentNullException(nameof(sceneMaterial));
            _panelMaterial = panelMaterial
                ?? throw new ArgumentNullException(nameof(panelMaterial));
        }

        private IEnumerator Start()
        {
            string root = Path.Combine(Application.streamingAssetsPath, "S08");
            string manifestPath = Path.Combine(root, "scene-manifest-v1.json");
            if (!TryLoadManifest(manifestPath, out SceneManifestData manifest))
            {
                throw new InvalidOperationException("S09 requires the valid S08 manifest.");
            }

            SceneAssetLifecycleTracker displayTracker =
                new SceneAssetLifecycleTracker();
            LocalSceneAssetLoader displayLoader = Loader(displayTracker);
            _displayLease = displayLoader.Load(manifest, root);
            if (_displayLease.Outcome != SceneLoadOutcome.Success)
            {
                throw new InvalidOperationException(
                    $"S09 display load failed: {_displayLease.Outcome}."
                );
            }

            _displayLease.Root.transform.SetParent(transform, false);
            _displayLease.Root.transform.localPosition = new Vector3(0.65f, 0.0f, 3.0f);
            _displayLease.Root.transform.localScale = Vector3.one * 0.75f;
            Dictionary<string, ScenePartView> views = CreateViews(
                manifest,
                _displayLease.Root
            );
            SceneSelectionController selection = new SceneSelectionController(
                manifest,
                views
            );

            PanelWorkspaceController workspace = CreateSciencePanel();
            if (
                !workspace.Factory.TryGetActive("science-3d", out PanelView panel)
                || panel.Kind != PanelKind.Science3D
            )
            {
                throw new InvalidOperationException("S09 Science3D panel is not active.");
            }

            if (
                !SceneAnimationCatalog.TryCreate(
                    manifest,
                    new Dictionary<string, float>(StringComparer.Ordinal)
                    {
                        { "idle", 2.0f },
                    },
                    out SceneAnimationCatalog catalog,
                    out AnimationControlError catalogError
                )
            )
            {
                throw new InvalidOperationException(
                    $"S09 animation catalog failed: {catalogError}."
                );
            }

            AnimationControllerV1 animation = new AnimationControllerV1(catalog);
            ModelViewerPanelController viewer = new ModelViewerPanelController(
                selection,
                _displayLease.Root.transform
            );
            ModelViewerRenderExecutor executor = new ModelViewerRenderExecutor(
                animation,
                viewer
            );
            RenderEventQueue queue = new RenderEventQueue(executor);

            RenderCommand first = RenderCommand.ForTarget(
                "s09-animation-play",
                executor.SceneVersion,
                RenderCommandKind.AnimationPlay,
                "idle"
            );
            Complete(queue, first);
            int callsBeforeReplay = executor.ExecutionCount;
            RenderEnqueueResult replay = queue.Enqueue(first);
            DuplicateReplayed = replay.Accepted
                && replay.Duplicate
                && replay.Ack.Status == RenderAckStatus.Completed
                && executor.ExecutionCount == callsBeforeReplay;
            if (!DuplicateReplayed)
            {
                throw new InvalidOperationException("S09 duplicate ACK replay failed.");
            }

            Complete(
                queue,
                RenderCommand.ForNumber(
                    "s09-animation-speed",
                    executor.SceneVersion,
                    RenderCommandKind.AnimationSpeed,
                    1.25f
                )
            );
            Complete(
                queue,
                RenderCommand.ForNumber(
                    "s09-animation-seek",
                    executor.SceneVersion,
                    RenderCommandKind.AnimationSeek,
                    0.5f
                )
            );
            Complete(
                queue,
                RenderCommand.Simple(
                    "s09-animation-pause",
                    executor.SceneVersion,
                    RenderCommandKind.AnimationPause
                )
            );
            Complete(
                queue,
                RenderCommand.ForNumber(
                    "s09-panel-zoom",
                    executor.SceneVersion,
                    RenderCommandKind.Zoom,
                    1.4f
                )
            );
            Complete(
                queue,
                RenderCommand.ForNumber(
                    "s09-panel-rotate",
                    executor.SceneVersion,
                    RenderCommandKind.Rotate,
                    25.0f
                )
            );
            Complete(
                queue,
                RenderCommand.Simple(
                    "s09-panel-annotations",
                    executor.SceneVersion,
                    RenderCommandKind.ToggleAnnotations
                )
            );
            Complete(
                queue,
                RenderCommand.Simple(
                    "s09-panel-recenter",
                    executor.SceneVersion,
                    RenderCommandKind.Recenter
                )
            );

            string[] partIds = { "root", "body", "marker-left", "marker-right" };
            for (int index = 0; index < SelectionCommands; index += 1)
            {
                string expectedPart = partIds[index % partIds.Length];
                Complete(
                    queue,
                    RenderCommand.ForTarget(
                        "s09-select-" + index.ToString("D2", CultureInfo.InvariantCulture),
                        executor.SceneVersion,
                        RenderCommandKind.SelectPart,
                        expectedPart
                    )
                );
                if (selection.State.SelectedPartId != expectedPart)
                {
                    throw new InvalidOperationException(
                        $"S09 exact selection {index} targeted the wrong part."
                    );
                }

                ExactSelections += 1;
            }

            AnimationState = animation.State;
            SceneVersion = executor.SceneVersion;
            ExecutorCalls = executor.ExecutionCount;
            ClickThroughCount = viewer.ClickThroughCount;
            if (
                AnimationState != AnimationPlaybackState.Paused
                || ExactSelections != SelectionCommands
                || ClickThroughCount != 0
            )
            {
                throw new InvalidOperationException("S09 panel/animation acceptance failed.");
            }

            RunFailureTaxonomy(manifest, root);
            yield return RunLifecycleBenchmark(manifest, root);

            float elapsed = 0.0f;
            int frames = 0;
            while (frames < 60)
            {
                yield return null;
                float delta = Time.unscaledDeltaTime;
                if (delta > 0.0f && !float.IsNaN(delta) && !float.IsInfinity(delta))
                {
                    elapsed += delta;
                    frames += 1;
                }
            }

            ObservedFps = elapsed > 0.0f ? frames / elapsed : 0.0f;
            Completed = true;
            Debug.Log(
                "[S09_FIXTURE] animation="
                    + AnimationState
                    + " ack_completed="
                    + AckCompleted.ToString(CultureInfo.InvariantCulture)
                    + " duplicate_replayed="
                    + DuplicateReplayed.ToString().ToLowerInvariant()
                    + " executor_calls="
                    + ExecutorCalls.ToString(CultureInfo.InvariantCulture)
                    + " exact_selections="
                    + ExactSelections.ToString(CultureInfo.InvariantCulture)
                    + " click_through="
                    + ClickThroughCount.ToString(CultureInfo.InvariantCulture)
                    + " scene_version="
                    + SceneVersion.ToString(CultureInfo.InvariantCulture)
            );
            Debug.Log(
                "[S09_G03] missing="
                    + MissingOutcome
                    + " malformed="
                    + MalformedOutcome
                    + " hash_mismatch="
                    + HashMismatchOutcome
                    + " cycles="
                    + LifecycleCompleted.ToString(CultureInfo.InvariantCulture)
                    + " active="
                    + LifecycleActive.ToString(CultureInfo.InvariantCulture)
                    + " peak="
                    + LifecyclePeak.ToString(CultureInfo.InvariantCulture)
                    + " memory_before="
                    + MemoryBeforeBytes.ToString(CultureInfo.InvariantCulture)
                    + " memory_after="
                    + MemoryAfterBytes.ToString(CultureInfo.InvariantCulture)
                    + " memory_delta="
                    + MemoryDeltaBytes.ToString(CultureInfo.InvariantCulture)
                    + " memory_bound="
                    + MaximumObservedMemoryGrowthBytes.ToString(CultureInfo.InvariantCulture)
                    + " observed_fps="
                    + ObservedFps.ToString("F1", CultureInfo.InvariantCulture)
                    + " viewer=NOT_RUN gate=PARTIAL"
            );
        }

        private void OnDestroy()
        {
            _displayLease?.Dispose();
        }

        private LocalSceneAssetLoader Loader(SceneAssetLifecycleTracker tracker)
        {
            return new LocalSceneAssetLoader(
                new FileSceneAssetSource(),
                new RealtimeSceneLoadClock(),
                tracker,
                _sceneMaterial
            );
        }

        private PanelWorkspaceController CreateSciencePanel()
        {
            GameObject panelRoot = new GameObject("S09PanelRoot");
            panelRoot.transform.SetParent(transform, false);
            PanelWorkspaceController workspace = new PanelWorkspaceController(
                new PanelRegistry(),
                new PanelFactory(panelRoot.transform, _panelMaterial)
            );
            if (
                !PanelDefinition.TryCreate(
                    "science-3d",
                    PanelKind.Science3D,
                    new PanelBounds(
                        new Vector3(-0.65f, 0.0f, 2.6f),
                        new Vector2(0.75f, 0.5f)
                    ),
                    out PanelDefinition definition
                )
            )
            {
                throw new InvalidOperationException("S09 panel definition is invalid.");
            }

            Require(
                workspace.Apply(
                    PanelCommand.Register(workspace.Registry.Version, definition)
                )
            );
            Require(
                workspace.Apply(
                    PanelCommand.For(
                        PanelCommandKind.Open,
                        workspace.Registry.Version,
                        definition.PanelId
                    )
                )
            );
            return workspace;
        }

        private void Complete(RenderEventQueue queue, RenderCommand command)
        {
            RenderEnqueueResult enqueued = queue.Enqueue(command);
            if (!enqueued.Accepted || enqueued.Duplicate)
            {
                throw new InvalidOperationException(
                    $"S09 queue rejected {command.CommandId}: {enqueued.Error}."
                );
            }

            if (
                !queue.ProcessNext(out RenderCommandAck ack)
                || ack.Status != RenderAckStatus.Completed
            )
            {
                throw new InvalidOperationException(
                    $"S09 command failed {command.CommandId}: {ack?.ErrorCode}."
                );
            }

            AckCompleted += 1;
        }

        private void RunFailureTaxonomy(SceneManifestData source, string root)
        {
            SceneAssetLifecycleTracker tracker = new SceneAssetLifecycleTracker();
            SceneAssetLease missing = new LocalSceneAssetLoader(
                new MissingSource(),
                new RealtimeSceneLoadClock(),
                tracker,
                _sceneMaterial
            ).Load(source, root);
            MissingOutcome = missing.Outcome;
            missing.Dispose();

            byte[] malformedBytes = Encoding.UTF8.GetBytes("{\"formatVersion\":1}");
            SceneManifestData malformedManifest = Clone(source);
            malformedManifest.bundleSha256 = Sha256(malformedBytes);
            SceneAssetLease malformed = new LocalSceneAssetLoader(
                new BytesSource(malformedBytes),
                new RealtimeSceneLoadClock(),
                tracker,
                _sceneMaterial
            ).Load(malformedManifest, root);
            MalformedOutcome = malformed.Outcome;
            malformed.Dispose();

            SceneManifestData mismatchManifest = Clone(source);
            mismatchManifest.bundleSha256 = new string('0', 64);
            SceneAssetLease mismatch = Loader(tracker).Load(mismatchManifest, root);
            HashMismatchOutcome = mismatch.Outcome;
            mismatch.Dispose();

            if (
                MissingOutcome != SceneLoadOutcome.SourceMissing
                || MalformedOutcome != SceneLoadOutcome.BundleInvalid
                || HashMismatchOutcome != SceneLoadOutcome.HashMismatch
                || tracker.Active != 0
            )
            {
                throw new InvalidOperationException("S09 G03 failure taxonomy failed.");
            }
        }

        private IEnumerator RunLifecycleBenchmark(
            SceneManifestData manifest,
            string root
        )
        {
            SceneAssetLifecycleTracker tracker = new SceneAssetLifecycleTracker();
            LocalSceneAssetLoader loader = Loader(tracker);
            MemoryBeforeBytes = Profiler.GetTotalAllocatedMemoryLong();
            for (int index = 0; index < LifecycleCycles; index += 1)
            {
                SceneAssetLease lease = loader.Load(manifest, root);
                if (lease.Outcome != SceneLoadOutcome.Success)
                {
                    lease.Dispose();
                    throw new InvalidOperationException(
                        $"S09 lifecycle load {index} failed: {lease.Outcome}."
                    );
                }

                lease.Dispose();
                LifecycleCompleted += 1;
                yield return null;
            }

            LifecycleActive = tracker.Active;
            LifecyclePeak = tracker.PeakActive;
            yield return Resources.UnloadUnusedAssets();
            GC.Collect();
            yield return null;
            MemoryAfterBytes = Profiler.GetTotalAllocatedMemoryLong();
            MemoryDeltaBytes = MemoryAfterBytes - MemoryBeforeBytes;
            MemoryWithinBound = MemoryDeltaBytes <= MaximumObservedMemoryGrowthBytes;
            if (
                tracker.Created != LifecycleCycles
                || tracker.Disposed != LifecycleCycles
                || LifecycleActive != 0
                || LifecyclePeak != 1
                || !MemoryWithinBound
            )
            {
                throw new InvalidOperationException(
                    $"S09 lifecycle imbalance: created={tracker.Created} "
                        + $"disposed={tracker.Disposed} active={tracker.Active} "
                        + $"peak={tracker.PeakActive} memory_delta={MemoryDeltaBytes}."
                );
            }
        }

        private static bool TryLoadManifest(
            string path,
            out SceneManifestData manifest
        )
        {
            manifest = null;
            return File.Exists(path)
                && SceneManifestCodec.TryDeserialize(
                    File.ReadAllText(path),
                    out manifest,
                    out _
                );
        }

        private static SceneManifestData Clone(SceneManifestData manifest)
        {
            if (
                !SceneManifestCodec.TryDeserialize(
                    JsonUtility.ToJson(manifest),
                    out SceneManifestData clone,
                    out SceneManifestError error
                )
            )
            {
                throw new InvalidOperationException($"S09 manifest clone failed: {error}.");
            }

            return clone;
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
                        $"S09 loaded hierarchy is missing {part.targetName}."
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

        private static void Require(PanelApplyResult result)
        {
            if (!result.Accepted)
            {
                throw new InvalidOperationException(
                    $"S09 panel command failed: {result.Error}."
                );
            }
        }

        private static string Sha256(byte[] bytes)
        {
            using SHA256 algorithm = SHA256.Create();
            byte[] hash = algorithm.ComputeHash(bytes);
            StringBuilder builder = new StringBuilder(hash.Length * 2);
            foreach (byte value in hash)
            {
                builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        private sealed class MissingSource : ISceneAssetSource
        {
            public bool TryRead(
                string fullPath,
                int maximumBytes,
                out byte[] bytes,
                out SceneLoadOutcome error
            )
            {
                bytes = null;
                error = SceneLoadOutcome.SourceMissing;
                return false;
            }
        }

        private sealed class BytesSource : ISceneAssetSource
        {
            private readonly byte[] _bytes;

            public BytesSource(byte[] bytes)
            {
                _bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
            }

            public bool TryRead(
                string fullPath,
                int maximumBytes,
                out byte[] bytes,
                out SceneLoadOutcome error
            )
            {
                bytes = _bytes;
                error = _bytes.Length <= maximumBytes
                    ? SceneLoadOutcome.Success
                    : SceneLoadOutcome.SourceTooLarge;
                return error == SceneLoadOutcome.Success;
            }
        }
    }
}
