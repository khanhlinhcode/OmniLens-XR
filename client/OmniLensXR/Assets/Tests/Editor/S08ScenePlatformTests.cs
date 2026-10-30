using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using OmniLensXR.S08;
using UnityEngine;

namespace OmniLensXR.Tests.Editor
{
    public sealed class S08ScenePlatformTests
    {
        [Test]
        public void CheckedInManifestHasValidHierarchyTargetsBudgetsAndHash()
        {
            SceneManifestData manifest = LoadManifest();
            Assert.That(manifest.schemaVersion, Is.EqualTo(1));
            Assert.That(manifest.parts.Length, Is.EqualTo(4));
            Assert.That(manifest.defaultPartId, Is.EqualTo("body"));
            Assert.That(manifest.license.licenseId, Is.EqualTo("LicenseRef-OmniLens-Generated"));
            byte[] bundle = File.ReadAllBytes(BundlePath());
            Assert.That(Sha256(bundle), Is.EqualTo(manifest.bundleSha256));
            Assert.That(bundle.Length, Is.LessThanOrEqualTo(manifest.maxBundleBytes));
        }

        [Test]
        public void ManifestRejectsMissingTargetsParentsCyclesAndDuplicates()
        {
            SceneManifestData manifest = LoadManifest();
            manifest.defaultPartId = "missing";
            AssertRejected(manifest, SceneManifestError.MissingTarget);

            manifest = LoadManifest();
            manifest.parts[2].parentPartId = "missing";
            AssertRejected(manifest, SceneManifestError.MissingParent);

            manifest = LoadManifest();
            manifest.parts[0].parentPartId = "marker-left";
            AssertRejected(manifest, SceneManifestError.BrokenHierarchy);

            manifest = LoadManifest();
            manifest.parts[1].partId = manifest.parts[0].partId;
            AssertRejected(manifest, SceneManifestError.DuplicatePart);

            manifest = LoadManifest();
            manifest.animations[0].targetPartId = "missing";
            AssertRejected(manifest, SceneManifestError.MissingTarget);
        }

        [Test]
        public void ManifestRejectsMissingProvenanceTraversalUnitsAndBudgets()
        {
            SceneManifestData manifest = LoadManifest();
            manifest.license.sourceUri = string.Empty;
            AssertRejected(manifest, SceneManifestError.InvalidProvenance);

            manifest = LoadManifest();
            manifest.bundleFileName = "../outside.mesh.json";
            AssertRejected(manifest, SceneManifestError.InvalidBundle);

            manifest = LoadManifest();
            manifest.unitScale = float.NaN;
            AssertRejected(manifest, SceneManifestError.InvalidUnit);

            manifest = LoadManifest();
            manifest.maxVertices = 0;
            AssertRejected(manifest, SceneManifestError.InvalidBudget);

            manifest = LoadManifest();
            manifest.lods[1].maxDistanceMeters = manifest.lods[0].maxDistanceMeters;
            AssertRejected(manifest, SceneManifestError.InvalidLod);
        }

        [Test]
        public void LoaderBuildsCheckedInMeshAndDisposesIdempotently()
        {
            SceneAssetLifecycleTracker tracker = new SceneAssetLifecycleTracker();
            LocalSceneAssetLoader loader = Loader(
                new FileSceneAssetSource(),
                new FakeClock(),
                tracker
            );
            SceneAssetLease lease = loader.Load(LoadManifest(), BundleRoot());
            Assert.That(lease.Outcome, Is.EqualTo(SceneLoadOutcome.Success));
            Assert.That(lease.Metrics.UsedFallback, Is.False);
            Assert.That(lease.Metrics.BundleBytes, Is.EqualTo(444));
            Assert.That(lease.Metrics.Vertices, Is.EqualTo(8));
            Assert.That(lease.Metrics.Triangles, Is.EqualTo(12));
            Assert.That(lease.Metrics.TextureBytes, Is.Zero);
            Assert.That(lease.Root.transform.Find("GeneratedCubeMesh"), Is.Not.Null);
            lease.Dispose();
            lease.Dispose();
            Assert.That(tracker.Created, Is.EqualTo(1));
            Assert.That(tracker.Disposed, Is.EqualTo(1));
            Assert.That(tracker.Active, Is.Zero);
            Assert.That(tracker.PeakActive, Is.EqualTo(1));
        }

        [Test]
        public void LoaderRejectsHashTimeoutMissingAndMalformedWithUsableFallback()
        {
            byte[] valid = File.ReadAllBytes(BundlePath());
            SceneManifestData manifest = LoadManifest();
            manifest.bundleSha256 = new string('0', 64);
            AssertFallback(
                Loader(new BytesSource(valid), new FakeClock(), new SceneAssetLifecycleTracker()),
                manifest,
                SceneLoadOutcome.HashMismatch
            );

            FakeClock timeoutClock = new FakeClock();
            AssertFallback(
                Loader(
                    new BytesSource(valid, timeoutClock, 3.0),
                    timeoutClock,
                    new SceneAssetLifecycleTracker()
                ),
                LoadManifest(),
                SceneLoadOutcome.Timeout,
                1.0
            );

            AssertFallback(
                Loader(
                    new MissingSource(),
                    new FakeClock(),
                    new SceneAssetLifecycleTracker()
                ),
                LoadManifest(),
                SceneLoadOutcome.SourceMissing
            );

            byte[] malformed = System.Text.Encoding.UTF8.GetBytes("{\"formatVersion\":1}");
            manifest = LoadManifest();
            manifest.bundleSha256 = Sha256(malformed);
            AssertFallback(
                Loader(
                    new BytesSource(malformed),
                    new FakeClock(),
                    new SceneAssetLifecycleTracker()
                ),
                manifest,
                SceneLoadOutcome.BundleInvalid
            );
        }

        [Test]
        public void LoaderRejectsInvalidProvenanceAndUnsafeRootWithFallback()
        {
            SceneManifestData manifest = LoadManifest();
            manifest.license.licenseId = string.Empty;
            AssertFallback(
                Loader(
                    new BytesSource(File.ReadAllBytes(BundlePath())),
                    new FakeClock(),
                    new SceneAssetLifecycleTracker()
                ),
                manifest,
                SceneLoadOutcome.ManifestInvalid
            );

            SceneAssetLifecycleTracker tracker = new SceneAssetLifecycleTracker();
            SceneAssetLease lease = Loader(
                new BytesSource(File.ReadAllBytes(BundlePath())),
                new FakeClock(),
                tracker
            ).Load(LoadManifest(), "relative/root");
            Assert.That(lease.Outcome, Is.EqualTo(SceneLoadOutcome.UnsafePath));
            Assert.That(lease.Metrics.UsedFallback, Is.True);
            lease.Dispose();
        }

        [Test]
        public void TwentyTypedSceneCommandsHitExactTargetsThenClear()
        {
            SceneSelectionState state = new SceneSelectionState(
                new[] { "root", "body", "marker-left", "marker-right" }
            );
            string[] parts = { "root", "body", "marker-left", "marker-right" };
            int accepted = 0;
            for (int round = 0; round < 4; round += 1)
            {
                AssertAccepted(state.Apply(SceneSelectionCommand.For(
                    SceneSelectionAction.Select,
                    state.Version,
                    parts[round]
                )));
                Assert.That(state.SelectedPartId, Is.EqualTo(parts[round]));
                accepted += 1;
                AssertAccepted(state.Apply(SceneSelectionCommand.For(
                    SceneSelectionAction.Highlight,
                    state.Version,
                    parts[(round + 1) % parts.Length]
                )));
                Assert.That(state.HighlightedPartId, Is.EqualTo(parts[(round + 1) % parts.Length]));
                accepted += 1;
                AssertAccepted(state.Apply(SceneSelectionCommand.For(
                    SceneSelectionAction.Isolate,
                    state.Version,
                    parts[(round + 2) % parts.Length]
                )));
                Assert.That(state.IsolatedPartId, Is.EqualTo(parts[(round + 2) % parts.Length]));
                accepted += 1;
                AssertAccepted(state.Apply(SceneSelectionCommand.For(
                    SceneSelectionAction.Focus,
                    state.Version,
                    parts[(round + 3) % parts.Length]
                )));
                Assert.That(state.FocusedPartId, Is.EqualTo(parts[(round + 3) % parts.Length]));
                accepted += 1;
                AssertAccepted(state.Apply(SceneSelectionCommand.Clear(state.Version)));
                Assert.That(state.SelectedPartId, Is.Null);
                accepted += 1;
            }

            Assert.That(accepted, Is.EqualTo(20));
            Assert.That(state.Version, Is.EqualTo(20));
        }

        [Test]
        public void SceneCommandsRejectUnknownStaleInvalidAndNoOpWithoutDrift()
        {
            SceneSelectionState state = new SceneSelectionState(new[] { "root", "body" });
            AssertAccepted(state.Apply(SceneSelectionCommand.For(
                SceneSelectionAction.Select,
                0,
                "body"
            )));
            long stable = state.Version;
            AssertRejected(state.Apply(SceneSelectionCommand.For(
                SceneSelectionAction.Select,
                stable,
                "missing"
            )), SceneSelectionError.UnknownPart);
            AssertRejected(state.Apply(SceneSelectionCommand.For(
                SceneSelectionAction.Select,
                0,
                "root"
            )), SceneSelectionError.StaleVersion);
            AssertRejected(state.Apply(SceneSelectionCommand.For(
                (SceneSelectionAction)999,
                stable,
                "root"
            )), SceneSelectionError.InvalidCommand);
            AssertRejected(state.Apply(SceneSelectionCommand.For(
                SceneSelectionAction.Select,
                stable,
                "body"
            )), SceneSelectionError.AlreadyInState);
            Assert.That(state.Version, Is.EqualTo(stable));
            Assert.That(state.SelectedPartId, Is.EqualTo("body"));
        }

        private static LocalSceneAssetLoader Loader(
            ISceneAssetSource source,
            ISceneLoadClock clock,
            SceneAssetLifecycleTracker tracker
        )
        {
            return new LocalSceneAssetLoader(source, clock, tracker);
        }

        private static void AssertFallback(
            LocalSceneAssetLoader loader,
            SceneManifestData manifest,
            SceneLoadOutcome outcome,
            double timeout = LocalSceneAssetLoader.DefaultTimeoutSeconds
        )
        {
            SceneAssetLease lease = loader.Load(manifest, BundleRoot(), timeout);
            try
            {
                Assert.That(lease.Outcome, Is.EqualTo(outcome));
                Assert.That(lease.Metrics.UsedFallback, Is.True);
                Assert.That(lease.Metrics.Vertices, Is.EqualTo(4));
                Assert.That(lease.Root, Is.Not.Null);
            }
            finally
            {
                lease.Dispose();
            }
        }

        private static SceneManifestData LoadManifest()
        {
            Assert.That(
                SceneManifestCodec.TryDeserialize(
                    File.ReadAllText(ManifestPath()),
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

        private static string ManifestPath()
        {
            return Path.Combine(BundleRoot(), "scene-manifest-v1.json");
        }

        private static string BundlePath()
        {
            return Path.Combine(BundleRoot(), "generated-cube.mesh.json");
        }

        private static string Sha256(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(bytes))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }

        private static void AssertRejected(
            SceneManifestData manifest,
            SceneManifestError expected
        )
        {
            Assert.That(SceneManifestCodec.TryValidate(manifest, out SceneManifestError error), Is.False);
            Assert.That(error, Is.EqualTo(expected));
        }

        private static void AssertAccepted(SceneSelectionResult result)
        {
            Assert.That(result.Accepted, Is.True, result.Error.ToString());
        }

        private static void AssertRejected(
            SceneSelectionResult result,
            SceneSelectionError expected
        )
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Error, Is.EqualTo(expected));
        }

        private sealed class FakeClock : ISceneLoadClock
        {
            public double NowSeconds { get; set; }
        }

        private sealed class BytesSource : ISceneAssetSource
        {
            private readonly byte[] _bytes;
            private readonly FakeClock _clock;
            private readonly double _afterSeconds;

            public BytesSource(
                byte[] bytes,
                FakeClock clock = null,
                double afterSeconds = 0.0
            )
            {
                _bytes = bytes;
                _clock = clock;
                _afterSeconds = afterSeconds;
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
                if (_clock != null)
                {
                    _clock.NowSeconds = _afterSeconds;
                }

                return true;
            }
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
    }
}
