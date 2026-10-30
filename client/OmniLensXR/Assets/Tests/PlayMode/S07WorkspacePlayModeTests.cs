using System.Collections;
using NUnit.Framework;
using OmniLensXR.S05;
using OmniLensXR.S07;
using UnityEngine;
using UnityEngine.TestTools;

namespace OmniLensXR.Tests.PlayMode
{
    public sealed class S07WorkspacePlayModeTests
    {
        [UnityTest]
        public IEnumerator RestoredLayoutCreatesThreeSynchronizedViews()
        {
            GameObject root = new GameObject("S07RestoredViews");
            try
            {
                WorkspaceLayoutData layout = S07WorkspaceDefaults.Create();
                Assert.That(
                    WorkspaceLayoutCodec.TryRestoreRegistry(
                        layout,
                        out PanelRegistry registry,
                        out WorkspaceLayoutError error
                    ),
                    Is.True,
                    error.ToString()
                );
                PanelFactory factory = new PanelFactory(root.transform);
                foreach (PanelState state in registry.CreateSnapshot().Panels)
                {
                    if (state.Visibility == PanelVisibility.Visible)
                    {
                        Assert.That(factory.TryAcquire(state, out _), Is.True);
                    }
                }

                yield return null;
                Assert.That(factory.ActiveCount, Is.EqualTo(3));
                Assert.That(factory.TryGetActive("document", out PanelView document), Is.True);
                Assert.That(document.IsFocused, Is.True);
                Assert.That(document.IsPinned, Is.True);
                Assert.That(document.transform.localPosition.x, Is.EqualTo(0.8f).Within(0.001f));
            }
            finally
            {
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator RuntimeFixtureReportsOfflineRestoreAndDeniedInputFallback()
        {
            GameObject fixtureObject = new GameObject("S07RuntimeFixtureTest");
            S07WorkspaceFixture fixture = fixtureObject.AddComponent<S07WorkspaceFixture>();
            try
            {
                yield return null;
                Assert.That(fixture.ActivePanelCount, Is.EqualTo(3));
                Assert.That(
                    fixture.DeniedAgentError,
                    Is.EqualTo(OfflineShellError.BackendUnavailable)
                );
                Assert.That(
                    fixture.DeniedInputFallback,
                    Is.EqualTo(SpatialInputFallback.GazeDwell)
                );
                Assert.That(fixture.ActivityFixtureCount, Is.EqualTo(30));
                Transform views = fixtureObject.transform.Find("S07PanelViews");
                Assert.That(views, Is.Not.Null);
                Assert.That(views.childCount, Is.EqualTo(3));
            }
            finally
            {
                Object.Destroy(fixtureObject);
            }
        }
    }
}
