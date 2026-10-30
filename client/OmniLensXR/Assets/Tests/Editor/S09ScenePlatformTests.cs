using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using OmniLensXR.S08;
using OmniLensXR.S09;
using UnityEngine;

namespace OmniLensXR.Tests.Editor
{
    public sealed class S09ScenePlatformTests
    {
        [Test]
        public void AnimationCatalogRequiresEveryManifestAnimationDuration()
        {
            SceneManifestData manifest = LoadManifest();
            Assert.That(
                SceneAnimationCatalog.TryCreate(
                    manifest,
                    new Dictionary<string, float> { { "idle", 2.0f } },
                    out SceneAnimationCatalog catalog,
                    out AnimationControlError error
                ),
                Is.True,
                error.ToString()
            );
            Assert.That(catalog.Count, Is.EqualTo(1));

            Assert.That(
                SceneAnimationCatalog.TryCreate(
                    manifest,
                    new Dictionary<string, float>(),
                    out _,
                    out error
                ),
                Is.False
            );
            Assert.That(error, Is.EqualTo(AnimationControlError.InvalidDefinition));

            Assert.That(
                SceneAnimationCatalog.TryCreate(
                    manifest,
                    new Dictionary<string, float>
                    {
                        { "idle", 2.0f },
                        { "undeclared", 1.0f },
                    },
                    out _,
                    out error
                ),
                Is.False
            );
            Assert.That(error, Is.EqualTo(AnimationControlError.InvalidDefinition));
        }

        [Test]
        public void AnimationControllerEnforcesNamesVersionsSeekAndSpeedBounds()
        {
            AnimationControllerV1 controller = AnimationController();
            AssertRejected(
                controller.Play(0, "missing"),
                AnimationControlError.UnknownAnimation
            );
            AssertAccepted(controller.Play(0, "idle"));
            AssertRejected(
                controller.Play(0, "idle"),
                AnimationControlError.StaleVersion
            );
            AssertRejected(
                controller.Seek(controller.Version, -0.1f),
                AnimationControlError.SeekOutOfBounds
            );
            AssertRejected(
                controller.Seek(controller.Version, 2.1f),
                AnimationControlError.SeekOutOfBounds
            );
            AssertRejected(
                controller.SetSpeed(controller.Version, 0.1f),
                AnimationControlError.SpeedOutOfBounds
            );
            AssertAccepted(controller.Seek(controller.Version, 1.0f));
            AssertAccepted(controller.SetSpeed(controller.Version, 1.5f));
            AssertAccepted(controller.Pause(controller.Version));
            Assert.That(controller.State, Is.EqualTo(AnimationPlaybackState.Paused));
            Assert.That(controller.PositionSeconds, Is.EqualTo(1.0f));
            Assert.That(controller.Speed, Is.EqualTo(1.5f));
            AssertAccepted(controller.Stop(controller.Version));
            Assert.That(controller.State, Is.EqualTo(AnimationPlaybackState.Idle));
        }

        [Test]
        public void AnimationTickStopsAtDurationWithoutVersionDrift()
        {
            AnimationControllerV1 controller = AnimationController();
            AssertAccepted(controller.Play(0, "idle"));
            long commandVersion = controller.Version;
            controller.Tick(10.0f);
            Assert.That(controller.PositionSeconds, Is.EqualTo(2.0f));
            Assert.That(controller.State, Is.EqualTo(AnimationPlaybackState.Paused));
            Assert.That(controller.Version, Is.EqualTo(commandVersion));
            controller.Tick(float.NaN);
            Assert.That(controller.PositionSeconds, Is.EqualTo(2.0f));
        }

        [Test]
        public void RenderQueueReplaysDuplicateTerminalAckWithoutExecutingTwice()
        {
            CountingExecutor executor = new CountingExecutor();
            RenderEventQueue queue = new RenderEventQueue(executor);
            RenderCommand command = RenderCommand.Simple(
                "command-1",
                0,
                RenderCommandKind.ToggleAnnotations
            );
            RenderEnqueueResult first = queue.Enqueue(command);
            Assert.That(first.Accepted, Is.True);
            Assert.That(first.Duplicate, Is.False);
            Assert.That(queue.ProcessNext(out RenderCommandAck completed), Is.True);
            Assert.That(completed.Status, Is.EqualTo(RenderAckStatus.Completed));
            Assert.That(completed.SceneVersion, Is.EqualTo(1));
            Assert.That(executor.Calls, Is.EqualTo(1));

            RenderEnqueueResult duplicate = queue.Enqueue(command);
            Assert.That(duplicate.Accepted, Is.True);
            Assert.That(duplicate.Duplicate, Is.True);
            Assert.That(duplicate.Ack, Is.SameAs(completed));
            Assert.That(queue.PendingCount, Is.Zero);
            Assert.That(queue.ProcessNext(out _), Is.False);
            Assert.That(executor.Calls, Is.EqualTo(1));
        }

        [Test]
        public void RenderQueueRejectsInvalidAndBoundsPendingAndHistory()
        {
            CountingExecutor executor = new CountingExecutor();
            RenderEventQueue queue = new RenderEventQueue(executor);
            RenderEnqueueResult invalid = queue.Enqueue(
                RenderCommand.Simple("unsafe id", 0, RenderCommandKind.Recenter)
            );
            Assert.That(invalid.Accepted, Is.False);
            Assert.That(invalid.Error, Is.EqualTo(RenderQueueError.InvalidCommand));

            for (int index = 0; index < RenderEventQueue.MaximumPending; index += 1)
            {
                RenderEnqueueResult result = queue.Enqueue(
                    RenderCommand.Simple(
                        "pending-" + index.ToString("D2", CultureInfo.InvariantCulture),
                        0,
                        RenderCommandKind.Recenter
                    )
                );
                Assert.That(result.Accepted, Is.True);
            }

            RenderEnqueueResult full = queue.Enqueue(
                RenderCommand.Simple("pending-overflow", 0, RenderCommandKind.Recenter)
            );
            Assert.That(full.Accepted, Is.False);
            Assert.That(full.Error, Is.EqualTo(RenderQueueError.QueueFull));

            CountingExecutor historyExecutor = new CountingExecutor();
            RenderEventQueue history = new RenderEventQueue(historyExecutor);
            for (int index = 0; index < 140; index += 1)
            {
                string id = "history-" + index.ToString("D3", CultureInfo.InvariantCulture);
                Assert.That(
                    history.Enqueue(
                        RenderCommand.Simple(
                            id,
                            historyExecutor.SceneVersion,
                            RenderCommandKind.Recenter
                        )
                    ).Accepted,
                    Is.True
                );
                Assert.That(history.ProcessNext(out RenderCommandAck ack), Is.True);
                Assert.That(ack.Status, Is.EqualTo(RenderAckStatus.Completed));
            }

            Assert.That(history.HistoryCount, Is.EqualTo(RenderEventQueue.MaximumHistory));
            Assert.That(history.TryGetAck("history-000", out _), Is.False);
            Assert.That(history.TryGetAck("history-139", out _), Is.True);
        }

        [Test]
        public void RenderQueuePublishesFailedAckForStaleSceneVersion()
        {
            CountingExecutor executor = new CountingExecutor();
            RenderEventQueue queue = new RenderEventQueue(executor);
            Assert.That(
                queue.Enqueue(
                    RenderCommand.Simple(
                        "stale-command",
                        7,
                        RenderCommandKind.ToggleAnnotations
                    )
                ).Accepted,
                Is.True
            );
            Assert.That(queue.ProcessNext(out RenderCommandAck ack), Is.True);
            Assert.That(ack.Status, Is.EqualTo(RenderAckStatus.Failed));
            Assert.That(ack.SceneVersion, Is.Zero);
            Assert.That(ack.ErrorCode, Is.EqualTo("stale_scene_version"));
        }

        [Test]
        public void PanelCommandsAreConsumedAndNeverDispatchThroughToScene()
        {
            using ViewerContext context = ViewerContext.Create();
            ModelViewerPanelController viewer = context.Viewer;
            ModelViewerResult outside = viewer.Apply(
                ModelViewerCommand.Number(
                    ModelViewerAction.Zoom,
                    viewer.Version,
                    ModelViewerHitRegion.Scene,
                    1.2f
                )
            );
            Assert.That(outside.Accepted, Is.False);
            Assert.That(outside.Consumed, Is.False);
            Assert.That(outside.Error, Is.EqualTo(ModelViewerError.NotPanelHit));
            Assert.That(viewer.SceneSelectionDispatches, Is.Zero);

            ModelViewerResult zoom = viewer.Apply(
                ModelViewerCommand.Number(
                    ModelViewerAction.Zoom,
                    viewer.Version,
                    ModelViewerHitRegion.Panel,
                    1.4f
                )
            );
            Assert.That(zoom.Accepted, Is.True);
            Assert.That(zoom.Consumed, Is.True);
            Assert.That(viewer.Zoom, Is.EqualTo(1.4f));
            Assert.That(viewer.SceneSelectionDispatches, Is.Zero);

            ModelViewerResult invalidRotate = viewer.Apply(
                ModelViewerCommand.Number(
                    ModelViewerAction.Rotate,
                    viewer.Version,
                    ModelViewerHitRegion.Panel,
                    90.0f
                )
            );
            Assert.That(invalidRotate.Accepted, Is.False);
            Assert.That(invalidRotate.Consumed, Is.True);
            Assert.That(viewer.SceneSelectionDispatches, Is.Zero);
            Assert.That(viewer.ClickThroughCount, Is.Zero);
        }

        [Test]
        public void PanelSelectsExactFixturePartTwentyOfTwenty()
        {
            using ViewerContext context = ViewerContext.Create();
            string[] partIds = { "root", "body", "marker-left", "marker-right" };
            for (int index = 0; index < 20; index += 1)
            {
                string expected = partIds[index % partIds.Length];
                ModelViewerResult result = context.Viewer.Apply(
                    ModelViewerCommand.Select(
                        context.Viewer.Version,
                        ModelViewerHitRegion.Panel,
                        expected
                    )
                );
                Assert.That(result.Accepted, Is.True, result.Error.ToString());
                Assert.That(result.Consumed, Is.True);
                Assert.That(result.SelectedPartId, Is.EqualTo(expected));
                Assert.That(context.Selection.State.SelectedPartId, Is.EqualTo(expected));
            }

            Assert.That(context.Viewer.SceneSelectionDispatches, Is.EqualTo(20));
            Assert.That(context.Viewer.ClickThroughCount, Is.Zero);
        }

        [Test]
        public void G03FailureTaxonomyReturnsTypedFallbacksAndCleansUp()
        {
            SceneAssetLifecycleTracker tracker = new SceneAssetLifecycleTracker();
            SceneManifestData manifest = LoadManifest();
            SceneAssetLease missing = new LocalSceneAssetLoader(
                new MissingSource(),
                new FixedClock(),
                tracker
            ).Load(manifest, BundleRoot());
            Assert.That(missing.Outcome, Is.EqualTo(SceneLoadOutcome.SourceMissing));
            Assert.That(missing.Metrics.UsedFallback, Is.True);
            missing.Dispose();

            byte[] malformedBytes = Encoding.UTF8.GetBytes("{\"formatVersion\":1}");
            manifest = LoadManifest();
            manifest.bundleSha256 = Sha256(malformedBytes);
            SceneAssetLease malformed = new LocalSceneAssetLoader(
                new BytesSource(malformedBytes),
                new FixedClock(),
                tracker
            ).Load(manifest, BundleRoot());
            Assert.That(malformed.Outcome, Is.EqualTo(SceneLoadOutcome.BundleInvalid));
            malformed.Dispose();

            manifest = LoadManifest();
            manifest.bundleSha256 = new string('0', 64);
            SceneAssetLease mismatch = new LocalSceneAssetLoader(
                new FileSceneAssetSource(),
                new FixedClock(),
                tracker
            ).Load(manifest, BundleRoot());
            Assert.That(mismatch.Outcome, Is.EqualTo(SceneLoadOutcome.HashMismatch));
            mismatch.Dispose();

            Assert.That(tracker.Created, Is.EqualTo(3));
            Assert.That(tracker.Disposed, Is.EqualTo(3));
            Assert.That(tracker.Active, Is.Zero);
            Assert.That(tracker.PeakActive, Is.EqualTo(1));
        }

        private static AnimationControllerV1 AnimationController()
        {
            Assert.That(
                SceneAnimationCatalog.TryCreate(
                    LoadManifest(),
                    new Dictionary<string, float> { { "idle", 2.0f } },
                    out SceneAnimationCatalog catalog,
                    out AnimationControlError error
                ),
                Is.True,
                error.ToString()
            );
            return new AnimationControllerV1(catalog);
        }

        private static SceneManifestData LoadManifest()
        {
            Assert.That(
                SceneManifestCodec.TryDeserialize(
                    File.ReadAllText(
                        Path.Combine(BundleRoot(), "scene-manifest-v1.json")
                    ),
                    out SceneManifestData manifest,
                    out SceneManifestError error
                ),
                Is.True,
                error.ToString()
            );
            return manifest;
        }

        private static string BundleRoot()
        {
            return Path.Combine(Application.dataPath, "StreamingAssets", "S08");
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

        private static void AssertAccepted(AnimationControlResult result)
        {
            Assert.That(result.Accepted, Is.True, result.Error.ToString());
        }

        private static void AssertRejected(
            AnimationControlResult result,
            AnimationControlError error
        )
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Error, Is.EqualTo(error));
        }

        private sealed class CountingExecutor : IRenderCommandExecutor
        {
            public long SceneVersion { get; private set; }
            public int Calls { get; private set; }

            public RenderExecutionResult Execute(RenderCommand command)
            {
                Calls += 1;
                if (command.ExpectedSceneVersion != SceneVersion)
                {
                    return RenderExecutionResult.Failed(
                        SceneVersion,
                        "stale_scene_version"
                    );
                }

                SceneVersion += 1;
                return RenderExecutionResult.Completed(SceneVersion);
            }
        }

        private sealed class ViewerContext : IDisposable
        {
            private ViewerContext(
                SceneAssetLease lease,
                SceneSelectionController selection,
                ModelViewerPanelController viewer
            )
            {
                Lease = lease;
                Selection = selection;
                Viewer = viewer;
            }

            private SceneAssetLease Lease { get; }
            public SceneSelectionController Selection { get; }
            public ModelViewerPanelController Viewer { get; }

            public static ViewerContext Create()
            {
                SceneManifestData manifest = LoadManifest();
                SceneAssetLease lease = new LocalSceneAssetLoader(
                    new FileSceneAssetSource(),
                    new FixedClock(),
                    new SceneAssetLifecycleTracker()
                ).Load(manifest, BundleRoot());
                Assert.That(lease.Outcome, Is.EqualTo(SceneLoadOutcome.Success));
                Dictionary<string, ScenePartView> views =
                    new Dictionary<string, ScenePartView>(StringComparer.Ordinal);
                foreach (ScenePartData part in manifest.parts)
                {
                    Transform target = Find(lease.Root.transform, part.targetName);
                    Assert.That(target, Is.Not.Null, part.targetName);
                    ScenePartView view = target.gameObject.AddComponent<ScenePartView>();
                    view.Initialize(part.partId);
                    views.Add(part.partId, view);
                }

                SceneSelectionController selection = new SceneSelectionController(
                    manifest,
                    views
                );
                return new ViewerContext(
                    lease,
                    selection,
                    new ModelViewerPanelController(selection, lease.Root.transform)
                );
            }

            public void Dispose()
            {
                Lease.Dispose();
            }
        }

        private static Transform Find(Transform current, string targetName)
        {
            if (current.name == targetName)
            {
                return current;
            }
            for (int index = 0; index < current.childCount; index += 1)
            {
                Transform found = Find(current.GetChild(index), targetName);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private sealed class FixedClock : ISceneLoadClock
        {
            public double NowSeconds => 0.0;
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
                _bytes = bytes;
            }

            public bool TryRead(
                string fullPath,
                int maximumBytes,
                out byte[] bytes,
                out SceneLoadOutcome error
            )
            {
                bytes = _bytes;
                error = SceneLoadOutcome.Success;
                return true;
            }
        }
    }
}
