using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using OmniLensXR.S08;
using UnityEngine;
using UnityEngine.TestTools;

namespace OmniLensXR.Tests.PlayMode
{
    public sealed class S08ScenePlatformPlayModeTests
    {
        [UnityTest]
        public IEnumerator LoadedHierarchySynchronizesSelectionIsolationAndClear()
        {
            SceneManifestData manifest = LoadManifest();
            SceneAssetLifecycleTracker tracker = new SceneAssetLifecycleTracker();
            LocalSceneAssetLoader loader = Loader(tracker);
            SceneAssetLease lease = loader.Load(manifest, BundleRoot());
            try
            {
                Assert.That(lease.Outcome, Is.EqualTo(SceneLoadOutcome.Success));
                Dictionary<string, ScenePartView> views = Views(manifest, lease.Root);
                SceneSelectionController controller = new SceneSelectionController(
                    manifest,
                    views
                );
                AssertAccepted(controller.Apply(SceneSelectionCommand.For(
                    SceneSelectionAction.Isolate,
                    controller.State.Version,
                    "marker-left"
                )));
                yield return null;
                Assert.That(views["root"].gameObject.activeSelf, Is.True);
                Assert.That(views["body"].gameObject.activeSelf, Is.True);
                Assert.That(views["marker-left"].gameObject.activeSelf, Is.True);
                Assert.That(views["marker-right"].gameObject.activeSelf, Is.False);
                Assert.That(views["marker-left"].IsSelected, Is.True);

                AssertAccepted(controller.Apply(
                    SceneSelectionCommand.Clear(controller.State.Version)
                ));
                yield return null;
                Assert.That(views["marker-right"].gameObject.activeSelf, Is.True);
                Assert.That(controller.State.SelectedPartId, Is.Null);
            }
            finally
            {
                lease.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator FiftyLoadDisposeCyclesRemainBounded()
        {
            SceneManifestData manifest = LoadManifest();
            SceneAssetLifecycleTracker tracker = new SceneAssetLifecycleTracker();
            LocalSceneAssetLoader loader = Loader(tracker);
            for (int index = 0; index < 50; index += 1)
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
        public IEnumerator HashMismatchFallbackIsRenderableAndDisposable()
        {
            SceneManifestData manifest = LoadManifest();
            manifest.bundleSha256 = new string('f', 64);
            SceneAssetLifecycleTracker tracker = new SceneAssetLifecycleTracker();
            SceneAssetLease lease = Loader(tracker).Load(manifest, BundleRoot());
            Assert.That(lease.Outcome, Is.EqualTo(SceneLoadOutcome.HashMismatch));
            Assert.That(lease.Metrics.UsedFallback, Is.True);
            Assert.That(lease.Root.GetComponentInChildren<MeshRenderer>(), Is.Not.Null);
            yield return null;
            lease.Dispose();
            yield return null;
            Assert.That(tracker.Active, Is.Zero);
        }

        [UnityTest]
        public IEnumerator RuntimeFixtureCompletesFiftyCyclesAndPublishesMetrics()
        {
            GameObject fixtureObject = new GameObject("S08RuntimeFixtureTest");
            S08SceneFixture fixture = fixtureObject.AddComponent<S08SceneFixture>();
            try
            {
                int frames = 0;
                while (!fixture.Completed && frames < 200)
                {
                    frames += 1;
                    yield return null;
                }

                Assert.That(fixture.Completed, Is.True);
                Assert.That(fixture.CompletedCycles, Is.EqualTo(50));
                Assert.That(fixture.ActiveAfterCycles, Is.Zero);
                Assert.That(fixture.PeakActive, Is.EqualTo(1));
                Assert.That(fixture.DisplayOutcome, Is.EqualTo(SceneLoadOutcome.Success));
                Assert.That(fixture.Metrics.Vertices, Is.EqualTo(8));
                Assert.That(fixture.Metrics.Triangles, Is.EqualTo(12));
                Assert.That(fixture.Metrics.TextureBytes, Is.Zero);
                Assert.That(fixture.SelectionVersion, Is.EqualTo(5));
                Assert.That(fixture.MemoryWithinBound, Is.True);
                Assert.That(
                    fixture.MemoryDeltaBytes,
                    Is.LessThanOrEqualTo(S08SceneFixture.MaximumObservedMemoryGrowthBytes)
                );
            }
            finally
            {
                UnityEngine.Object.Destroy(fixtureObject);
            }
        }

        private static LocalSceneAssetLoader Loader(SceneAssetLifecycleTracker tracker)
        {
            return new LocalSceneAssetLoader(
                new FileSceneAssetSource(),
                new RealtimeSceneLoadClock(),
                tracker
            );
        }

        private static SceneManifestData LoadManifest()
        {
            Assert.That(
                SceneManifestCodec.TryDeserialize(
                    File.ReadAllText(Path.Combine(BundleRoot(), "scene-manifest-v1.json")),
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

        private static Dictionary<string, ScenePartView> Views(
            SceneManifestData manifest,
            GameObject root
        )
        {
            Dictionary<string, ScenePartView> result =
                new Dictionary<string, ScenePartView>(StringComparer.Ordinal);
            foreach (ScenePartData part in manifest.parts)
            {
                Transform target = Find(root.transform, part.targetName);
                Assert.That(target, Is.Not.Null, part.targetName);
                ScenePartView view = target.gameObject.AddComponent<ScenePartView>();
                view.Initialize(part.partId);
                result.Add(part.partId, view);
            }

            return result;
        }

        private static Transform Find(Transform current, string name)
        {
            if (current.name == name)
            {
                return current;
            }

            for (int index = 0; index < current.childCount; index += 1)
            {
                Transform result = Find(current.GetChild(index), name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static void AssertAccepted(SceneSelectionResult result)
        {
            Assert.That(result.Accepted, Is.True, result.Error.ToString());
        }
    }
}
