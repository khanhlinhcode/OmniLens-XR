using System.Collections;
using NUnit.Framework;
using OmniLensXR.S05;
using OmniLensXR.S06;
using UnityEngine;
using UnityEngine.TestTools;

namespace OmniLensXR.Tests.PlayMode
{
    public sealed class S06WorkspacePlayModeTests
    {
        [UnityTest]
        public IEnumerator MoveResizeAndPinSynchronizeFocusedActiveView()
        {
            GameObject root = new GameObject("S06ManipulationRoot");
            PanelRegistry registry = new PanelRegistry();
            PanelFactory factory = new PanelFactory(root.transform);
            PanelWorkspaceController workspace = new PanelWorkspaceController(
                registry,
                factory
            );
            try
            {
                Register(registry, "document", PanelKind.Document, 0.0f);
                Open(workspace, "document");
                PanelManipulationController controller =
                    new PanelManipulationController(workspace);
                AssertAccepted(controller.Apply(PanelHandleKind.MoveRight, "document"));
                AssertAccepted(controller.Apply(PanelHandleKind.ResizeWider, "document"));
                AssertAccepted(controller.Apply(PanelHandleKind.TogglePin, "document"));
                yield return null;

                Assert.That(registry.TryGet("document", out PanelState state), Is.True);
                Assert.That(state.IsFocused, Is.True);
                Assert.That(state.IsPinned, Is.True);
                Assert.That(state.Bounds.PositionMeters.x, Is.EqualTo(0.1f).Within(0.001f));
                Assert.That(state.Bounds.SizeMeters.x, Is.EqualTo(0.6f).Within(0.001f));
                Assert.That(factory.TryGetActive("document", out PanelView view), Is.True);
                Assert.That(view.IsFocused, Is.True);
                Assert.That(view.IsPinned, Is.True);
                Assert.That(view.transform.localPosition.x, Is.EqualTo(0.1f).Within(0.001f));
                Assert.That(view.transform.localScale.x, Is.EqualTo(0.6f).Within(0.001f));
            }
            finally
            {
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator ManipulationClampsAtSizeAndFovEdgesWithoutLosingFocus()
        {
            GameObject root = new GameObject("S06BoundaryRoot");
            PanelRegistry registry = new PanelRegistry();
            PanelFactory factory = new PanelFactory(root.transform);
            PanelWorkspaceController workspace = new PanelWorkspaceController(
                registry,
                factory
            );
            try
            {
                Register(registry, "notes", PanelKind.Notes, 0.0f);
                Open(workspace, "notes");
                PanelManipulationController controller =
                    new PanelManipulationController(workspace);

                int narrowAccepted = ApplyUntilBound(
                    controller,
                    PanelHandleKind.ResizeNarrower,
                    "notes"
                );
                int shorterAccepted = ApplyUntilBound(
                    controller,
                    PanelHandleKind.ResizeShorter,
                    "notes"
                );
                int rightAccepted = ApplyUntilBound(
                    controller,
                    PanelHandleKind.MoveRight,
                    "notes"
                );
                Assert.That(narrowAccepted, Is.EqualTo(2));
                Assert.That(shorterAccepted, Is.EqualTo(3));
                Assert.That(rightAccepted, Is.GreaterThan(0));
                Assert.That(registry.TryGet("notes", out PanelState state), Is.True);
                Assert.That(
                    state.Bounds.SizeMeters.x,
                    Is.EqualTo(S04.SpatialUiTokens.MinimumPanelWidthMeters).Within(0.001f)
                );
                Assert.That(
                    state.Bounds.SizeMeters.y,
                    Is.EqualTo(S04.SpatialUiTokens.MinimumPanelHeightMeters).Within(0.001f)
                );
                Assert.That(PanelPlacementService.IsFullyInsideSafeZone(state.Bounds), Is.True);
                Assert.That(state.IsFocused, Is.True);

                PanelManipulationResult invalid = controller.Apply(
                    (PanelHandleKind)999,
                    "notes"
                );
                Assert.That(invalid.Accepted, Is.False);
                Assert.That(
                    invalid.Error,
                    Is.EqualTo(PanelManipulationError.InvalidHandle)
                );
                yield return null;
            }
            finally
            {
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator RuntimeFixtureReportsManipulatedStateAndIncompleteMetrics()
        {
            GameObject fixtureObject = new GameObject("S06RuntimeFixtureTest");
            S06WorkspaceFixture fixture =
                fixtureObject.AddComponent<S06WorkspaceFixture>();
            try
            {
                yield return null;
                Assert.That(fixture.Snapshot, Is.Not.Null);
                Assert.That(fixture.Snapshot.Version, Is.EqualTo(9));
                Assert.That(fixture.Snapshot.Panels.Count, Is.EqualTo(3));
                PanelState document = null;
                foreach (PanelState state in fixture.Snapshot.Panels)
                {
                    if (state.PanelId == "document")
                    {
                        document = state;
                    }
                }

                Assert.That(document, Is.Not.Null);
                Assert.That(document.IsFocused, Is.True);
                Assert.That(document.IsPinned, Is.True);
                Transform views = fixtureObject.transform.Find("S06PanelViews");
                Assert.That(views, Is.Not.Null);
                Assert.That(views.childCount, Is.EqualTo(3));
                Assert.That(fixture.Metrics, Is.Not.Null);
                Assert.That(
                    fixture.Metrics.State,
                    Is.EqualTo(FatigueProtocolState.Running)
                );
                Assert.That(fixture.Metrics.PhysicalViewerResult, Is.EqualTo("NOT_RUN"));
            }
            finally
            {
                Object.Destroy(fixtureObject);
            }
        }

        private static int ApplyUntilBound(
            PanelManipulationController controller,
            PanelHandleKind handle,
            string panelId
        )
        {
            int accepted = 0;
            for (int index = 0; index < 100; index += 1)
            {
                PanelManipulationResult result = controller.Apply(handle, panelId);
                if (!result.Accepted)
                {
                    Assert.That(
                        result.Error,
                        Is.EqualTo(PanelManipulationError.RegistryRejected)
                    );
                    Assert.That(
                        result.ApplyResult.Error,
                        Is.EqualTo(PanelApplyError.AlreadyInState)
                    );
                    return accepted;
                }

                accepted += 1;
            }

            Assert.Fail("Manipulation did not reach a bounded edge within 100 steps.");
            return accepted;
        }

        private static void Register(
            PanelRegistry registry,
            string panelId,
            PanelKind kind,
            float x
        )
        {
            Assert.That(
                PanelDefinition.TryCreate(
                    panelId,
                    kind,
                    new PanelBounds(
                        new Vector3(x, 0.0f, 2.5f),
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
            PanelApplyResult result = workspace.Apply(
                PanelCommand.For(
                    PanelCommandKind.Open,
                    workspace.Registry.Version,
                    panelId
                )
            );
            Assert.That(result.Accepted, Is.True, result.Error.ToString());
        }

        private static void AssertAccepted(PanelManipulationResult result)
        {
            Assert.That(
                result.Accepted,
                Is.True,
                result.Error + "/" + result.ApplyResult.Error
            );
        }
    }
}
