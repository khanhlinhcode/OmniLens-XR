using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using OmniLensXR.S08;
using OmniLensXR.S09;
using UnityEngine;
using UnityEngine.TestTools;

namespace OmniLensXR.Tests.PlayMode
{
    public sealed class S09ScenePlatformPlayModeTests
    {
        [UnityTest]
        public IEnumerator ViewerZoomRotateAnnotationsAndRecenterSynchronizeTransform()
        {
            using ViewerContext context = ViewerContext.Create();
            Vector3 originalScale = context.Lease.Root.transform.localScale;
            Quaternion originalRotation = context.Lease.Root.transform.localRotation;
            AssertAccepted(
                context.Viewer.Apply(
                    ModelViewerCommand.Number(
                        ModelViewerAction.Zoom,
                        context.Viewer.Version,
                        ModelViewerHitRegion.Panel,
                        1.5f
                    )
                )
            );
            Assert.That(
                context.Lease.Root.transform.localScale,
                Is.EqualTo(originalScale * 1.5f)
            );
            AssertAccepted(
                context.Viewer.Apply(
                    ModelViewerCommand.Number(
                        ModelViewerAction.Rotate,
                        context.Viewer.Version,
                        ModelViewerHitRegion.Panel,
                        30.0f
                    )
                )
            );
            Assert.That(
                context.Lease.Root.transform.localRotation,
                Is.Not.EqualTo(originalRotation)
            );
            AssertAccepted(
                context.Viewer.Apply(
                    ModelViewerCommand.Simple(
                        ModelViewerAction.ToggleAnnotations,
                        context.Viewer.Version,
                        ModelViewerHitRegion.Panel
                    )
                )
            );
            Assert.That(context.Viewer.AnnotationsVisible, Is.True);
            AssertAccepted(
                context.Viewer.Apply(
                    ModelViewerCommand.Simple(
                        ModelViewerAction.Recenter,
                        context.Viewer.Version,
                        ModelViewerHitRegion.Panel
                    )
                )
            );
            yield return null;
            Assert.That(context.Viewer.Zoom, Is.EqualTo(1.0f));
            Assert.That(context.Viewer.RotationDegrees, Is.Zero);
            Assert.That(context.Lease.Root.transform.localScale, Is.EqualTo(originalScale));
            Assert.That(
                context.Lease.Root.transform.localRotation,
                Is.EqualTo(originalRotation)
            );
            Assert.That(context.Viewer.ClickThroughCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator QueueDuplicateDoesNotRepeatModelMutation()
        {
            using ViewerContext context = ViewerContext.Create();
            AnimationControllerV1 animation = AnimationController();
            ModelViewerRenderExecutor executor = new ModelViewerRenderExecutor(
                animation,
                context.Viewer
            );
            RenderEventQueue queue = new RenderEventQueue(executor);
            RenderCommand command = RenderCommand.ForTarget(
                "select-body-once",
                0,
                RenderCommandKind.SelectPart,
                "body"
            );
            Assert.That(queue.Enqueue(command).Accepted, Is.True);
            Assert.That(queue.ProcessNext(out RenderCommandAck ack), Is.True);
            Assert.That(ack.Status, Is.EqualTo(RenderAckStatus.Completed));
            int calls = executor.ExecutionCount;
            long selectionVersion = context.Selection.State.Version;
            RenderEnqueueResult duplicate = queue.Enqueue(command);
            Assert.That(duplicate.Accepted, Is.True);
            Assert.That(duplicate.Duplicate, Is.True);
            yield return null;
            Assert.That(executor.ExecutionCount, Is.EqualTo(calls));
            Assert.That(context.Selection.State.Version, Is.EqualTo(selectionVersion));
            Assert.That(context.Selection.State.SelectedPartId, Is.EqualTo("body"));
        }

        [UnityTest]
        public IEnumerator FiftySceneOpenCloseCyclesLeaveNoActiveLease()
        {
            SceneManifestData manifest = LoadManifest();
            SceneAssetLifecycleTracker tracker = new SceneAssetLifecycleTracker();
            LocalSceneAssetLoader loader = new LocalSceneAssetLoader(
                new FileSceneAssetSource(),
                new RealtimeSceneLoadClock(),
                tracker
            );
            for (int index = 0; index < S09SceneFixture.LifecycleCycles; index += 1)
            {
                SceneAssetLease lease = loader.Load(manifest, BundleRoot());
                Assert.That(lease.Outcome, Is.EqualTo(SceneLoadOutcome.Success));
                lease.Dispose();
                yield return null;
            }

            Assert.That(tracker.Created, Is.EqualTo(50));
            Assert.That(tracker.Disposed, Is.EqualTo(50));
            Assert.That(tracker.Active, Is.Zero);
            Assert.That(tracker.PeakActive, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator RuntimeFixturePublishesS09AcceptanceState()
        {
            GameObject fixtureObject = new GameObject("S09RuntimeFixtureTest");
            S09SceneFixture fixture = fixtureObject.AddComponent<S09SceneFixture>();
            try
            {
                int frames = 0;
                while (!fixture.Completed && frames < 500)
                {
                    frames += 1;
                    yield return null;
                }

                Assert.That(fixture.Completed, Is.True);
                Assert.That(fixture.DuplicateReplayed, Is.True);
                Assert.That(fixture.AckCompleted, Is.EqualTo(28));
                Assert.That(fixture.ExecutorCalls, Is.EqualTo(28));
                Assert.That(fixture.ExactSelections, Is.EqualTo(20));
                Assert.That(fixture.ClickThroughCount, Is.Zero);
                Assert.That(
                    fixture.AnimationState,
                    Is.EqualTo(AnimationPlaybackState.Paused)
                );
                Assert.That(fixture.MissingOutcome, Is.EqualTo(SceneLoadOutcome.SourceMissing));
                Assert.That(fixture.MalformedOutcome, Is.EqualTo(SceneLoadOutcome.BundleInvalid));
                Assert.That(
                    fixture.HashMismatchOutcome,
                    Is.EqualTo(SceneLoadOutcome.HashMismatch)
                );
                Assert.That(fixture.LifecycleCompleted, Is.EqualTo(50));
                Assert.That(fixture.LifecycleActive, Is.Zero);
                Assert.That(fixture.LifecyclePeak, Is.EqualTo(1));
                Assert.That(fixture.MemoryWithinBound, Is.True);
                Assert.That(fixture.ObservedFps, Is.GreaterThan(0.0f));
            }
            finally
            {
                UnityEngine.Object.Destroy(fixtureObject);
            }
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

        private static void AssertAccepted(ModelViewerResult result)
        {
            Assert.That(result.Accepted, Is.True, result.Error.ToString());
            Assert.That(result.Consumed, Is.True);
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

            public SceneAssetLease Lease { get; }
            public SceneSelectionController Selection { get; }
            public ModelViewerPanelController Viewer { get; }

            public static ViewerContext Create()
            {
                SceneManifestData manifest = LoadManifest();
                SceneAssetLease lease = new LocalSceneAssetLoader(
                    new FileSceneAssetSource(),
                    new RealtimeSceneLoadClock(),
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
    }
}
