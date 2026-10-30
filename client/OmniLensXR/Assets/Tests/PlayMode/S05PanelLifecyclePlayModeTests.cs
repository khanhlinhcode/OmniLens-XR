using System.Collections;
using NUnit.Framework;
using OmniLensXR.S05;
using UnityEngine;
using UnityEngine.TestTools;

namespace OmniLensXR.Tests.PlayMode
{
    public sealed class S05PanelLifecyclePlayModeTests
    {
        [UnityTest]
        public IEnumerator ThreePanelsOpenWithoutDuplicatesAndReusePool()
        {
            GameObject root = new GameObject("S05FactoryRoot");
            PanelRegistry registry = new PanelRegistry();
            PanelFactory factory = new PanelFactory(root.transform);
            PanelWorkspaceController workspace = new PanelWorkspaceController(
                registry,
                factory
            );
            try
            {
                Register(registry, "chat", PanelKind.Chat, -0.8f);
                Register(registry, "science", PanelKind.Science3D, 0.0f);
                Register(registry, "document", PanelKind.Document, 0.8f);
                Open(workspace, "chat");
                Open(workspace, "science");
                Open(workspace, "document");
                yield return null;

                Assert.That(factory.ActiveCount, Is.EqualTo(3));
                Assert.That(root.transform.childCount, Is.EqualTo(3));
                Assert.That(factory.TryGetActive("chat", out PanelView firstChat), Is.True);

                PanelApplyResult duplicate = workspace.Apply(
                    PanelCommand.For(
                        PanelCommandKind.Open,
                        registry.Version,
                        "chat"
                    )
                );
                Assert.That(duplicate.Accepted, Is.False);
                Assert.That(duplicate.Error, Is.EqualTo(PanelApplyError.AlreadyInState));
                Assert.That(factory.ActiveCount, Is.EqualTo(3));

                Apply(workspace, PanelCommandKind.Close, "chat");
                Assert.That(factory.ActiveCount, Is.EqualTo(2));
                Assert.That(factory.PooledCount(PanelKind.Chat), Is.EqualTo(1));
                Open(workspace, "chat");
                Assert.That(factory.TryGetActive("chat", out PanelView reopened), Is.True);
                Assert.That(reopened, Is.SameAs(firstChat));
                Assert.That(root.transform.childCount, Is.EqualTo(3));
            }
            finally
            {
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator MinimizeRestoreKeepsViewsAndSnapshotInSync()
        {
            GameObject root = new GameObject("S05SnapshotRoot");
            PanelRegistry registry = new PanelRegistry();
            PanelFactory factory = new PanelFactory(root.transform);
            PanelWorkspaceController workspace = new PanelWorkspaceController(
                registry,
                factory
            );
            try
            {
                Register(registry, "chat", PanelKind.Chat, -0.4f);
                Register(registry, "notes", PanelKind.Notes, 0.4f);
                Open(workspace, "chat");
                Open(workspace, "notes");
                Apply(workspace, PanelCommandKind.Minimize, "notes");
                Assert.That(factory.ActiveCount, Is.EqualTo(1));
                Assert.That(factory.TryGetActive("chat", out _), Is.True);
                Assert.That(factory.TryGetActive("notes", out _), Is.False);
                AssertFocus(registry, "chat");

                Apply(workspace, PanelCommandKind.Restore, "notes");
                yield return null;
                Assert.That(factory.ActiveCount, Is.EqualTo(2));
                AssertFocus(registry, "notes");
                PanelSnapshot snapshot = registry.CreateSnapshot();
                Assert.That(snapshot.Panels.Count, Is.EqualTo(2));
                Assert.That(snapshot.Version, Is.EqualTo(registry.Version));
            }
            finally
            {
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator RuntimeFixtureCreatesThreeActivePanelViews()
        {
            GameObject fixtureObject = new GameObject("S05RuntimeFixtureTest");
            S05PanelLifecycleFixture fixture =
                fixtureObject.AddComponent<S05PanelLifecycleFixture>();
            try
            {
                yield return null;
                Assert.That(fixture.Snapshot, Is.Not.Null);
                Assert.That(fixture.Snapshot.Panels.Count, Is.EqualTo(4));
                Transform views = fixtureObject.transform.Find("S05PanelViews");
                Assert.That(views, Is.Not.Null);
                Assert.That(views.childCount, Is.EqualTo(3));
            }
            finally
            {
                Object.Destroy(fixtureObject);
            }
        }

        [UnityTest]
        public IEnumerator PoolRetentionIsBoundedAcrossEightPanels()
        {
            GameObject root = new GameObject("S05BoundedPoolRoot");
            PanelRegistry registry = new PanelRegistry();
            PanelFactory factory = new PanelFactory(root.transform);
            PanelWorkspaceController workspace = new PanelWorkspaceController(
                registry,
                factory
            );
            try
            {
                for (int index = 0; index < PanelRegistry.MaximumPanels; index += 1)
                {
                    string panelId = "notes_" + index;
                    Register(
                        registry,
                        panelId,
                        PanelKind.Notes,
                        0.0f,
                        1.2f + index * 0.35f
                    );
                    Open(workspace, panelId);
                }

                Assert.That(factory.ActiveCount, Is.EqualTo(PanelRegistry.MaximumPanels));
                for (int index = 0; index < PanelRegistry.MaximumPanels; index += 1)
                {
                    Apply(workspace, PanelCommandKind.Close, "notes_" + index);
                }

                yield return null;
                Assert.That(factory.ActiveCount, Is.Zero);
                Assert.That(
                    factory.PooledCount(PanelKind.Notes),
                    Is.EqualTo(PanelFactory.MaximumPooledPerKind)
                );
                Assert.That(
                    root.transform.childCount,
                    Is.EqualTo(PanelFactory.MaximumPooledPerKind)
                );
            }
            finally
            {
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator MinimizedCloseAndViewConflictAreFailClosed()
        {
            GameObject root = new GameObject("S05ConflictRoot");
            PanelRegistry registry = new PanelRegistry();
            PanelFactory factory = new PanelFactory(root.transform);
            PanelWorkspaceController workspace = new PanelWorkspaceController(
                registry,
                factory
            );
            try
            {
                Register(registry, "chat", PanelKind.Chat, 0.0f);
                Open(workspace, "chat");
                Apply(workspace, PanelCommandKind.Minimize, "chat");
                Apply(workspace, PanelCommandKind.Close, "chat");
                Assert.That(factory.ActiveCount, Is.Zero);

                PanelApplyResult reopenedDirectly = registry.Apply(
                    PanelCommand.For(
                        PanelCommandKind.Open,
                        registry.Version,
                        "chat"
                    )
                );
                Assert.That(reopenedDirectly.Accepted, Is.True);
                long stableVersion = registry.Version;
                PanelApplyResult conflict = workspace.Apply(
                    PanelCommand.For(
                        PanelCommandKind.Close,
                        stableVersion,
                        "chat"
                    )
                );
                Assert.That(conflict.Accepted, Is.False);
                Assert.That(
                    conflict.Error,
                    Is.EqualTo(PanelApplyError.ViewStateConflict)
                );
                Assert.That(registry.Version, Is.EqualTo(stableVersion));
                yield return null;
            }
            finally
            {
                Object.Destroy(root);
            }
        }

        private static void Register(
            PanelRegistry registry,
            string panelId,
            PanelKind kind,
            float x,
            float z = 2.5f
        )
        {
            Assert.That(
                PanelDefinition.TryCreate(
                    panelId,
                    kind,
                    new PanelBounds(
                        new Vector3(x, 0.0f, z),
                        new Vector2(0.5f, 0.5f)
                    ),
                    out PanelDefinition definition
                ),
                Is.True
            );
            PanelApplyResult result = registry.Apply(
                PanelCommand.Register(registry.Version, definition)
            );
            Assert.That(result.Accepted, Is.True, result.Error.ToString());
        }

        private static void Open(PanelWorkspaceController workspace, string panelId)
        {
            Apply(workspace, PanelCommandKind.Open, panelId);
        }

        private static void Apply(
            PanelWorkspaceController workspace,
            PanelCommandKind kind,
            string panelId
        )
        {
            PanelApplyResult result = workspace.Apply(
                PanelCommand.For(kind, workspace.Registry.Version, panelId)
            );
            Assert.That(result.Accepted, Is.True, result.Error.ToString());
        }

        private static void AssertFocus(PanelRegistry registry, string panelId)
        {
            int focusCount = 0;
            foreach (PanelState state in registry.CreateSnapshot().Panels)
            {
                if (state.IsFocused)
                {
                    focusCount += 1;
                    Assert.That(state.PanelId, Is.EqualTo(panelId));
                }
            }

            Assert.That(focusCount, Is.EqualTo(1));
        }
    }
}
