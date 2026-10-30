using System.Collections;
using System.Reflection;
using NUnit.Framework;
using OmniLensXR.S03;
using UnityEngine;
using UnityEngine.TestTools;

namespace OmniLensXR.Tests.PlayMode
{
    public sealed class S04DwellProbeTarget : MonoBehaviour, IGazeDwellTarget
    {
        public int SelectionCount { get; private set; }

        public bool CanSelectByDwell => isActiveAndEnabled;

        public int DwellTargetId => GetInstanceID();

        public void SelectByDwell()
        {
            SelectionCount += 1;
        }
    }

    public sealed class S04SpatialInteractionPlayModeTests
    {
        private const int InteractionLayer = 7;
        private const float MinimumDwellSeconds = 0.5f;
        private static readonly Vector3 IsolatedOrigin = new Vector3(
            2000.0f,
            2000.0f,
            2000.0f
        );

        [UnityTest]
        public IEnumerator ThirtyDistinctTargetsSelectExactlyOnceAfterExit()
        {
            SelectorFixture fixture = CreateSelector();
            try
            {
                for (int index = 1; index <= 30; index += 1)
                {
                    TargetFixture target = CreateTarget("S04Target-" + index);
                    yield return new WaitForSecondsRealtime(0.58f);
                    Assert.That(
                        target.Probe.SelectionCount,
                        Is.EqualTo(1),
                        $"Target {index} did not select exactly once."
                    );

                    yield return new WaitForSecondsRealtime(0.08f);
                    Assert.That(
                        target.Probe.SelectionCount,
                        Is.EqualTo(1),
                        $"Target {index} repeated without gaze exit."
                    );

                    target.GameObject.transform.position += Vector3.right * 5.0f;
                    Physics.SyncTransforms();
                    yield return null;
                    target.Dispose();
                }
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator GazeExitBeforeTimeoutCancelsWithoutSelection()
        {
            SelectorFixture fixture = CreateSelector();
            TargetFixture target = CreateTarget("S04CancellationTarget");
            try
            {
                yield return new WaitForSecondsRealtime(0.28f);
                Assert.That(fixture.Selector.Progress, Is.GreaterThan(0.0f));

                target.GameObject.transform.position += Vector3.right * 5.0f;
                Physics.SyncTransforms();
                yield return new WaitForSecondsRealtime(0.35f);

                Assert.That(target.Probe.SelectionCount, Is.Zero);
                Assert.That(fixture.Selector.Phase, Is.EqualTo(GazeDwellPhase.Idle));
                Assert.That(fixture.Selector.Progress, Is.Zero);
            }
            finally
            {
                target.Dispose();
                fixture.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator CompetingTargetCannotInheritPreviousDwellProgress()
        {
            SelectorFixture fixture = CreateSelector();
            TargetFixture first = CreateTarget("S04FirstTarget");
            TargetFixture second = CreateTarget(
                "S04SecondTarget",
                IsolatedOrigin + Vector3.right * 5.0f + Vector3.forward * 2.0f
            );
            try
            {
                yield return new WaitForSecondsRealtime(0.28f);
                first.GameObject.transform.position += Vector3.left * 5.0f;
                second.GameObject.transform.position =
                    IsolatedOrigin + Vector3.forward * 2.0f;
                Physics.SyncTransforms();

                yield return new WaitForSecondsRealtime(0.28f);
                Assert.That(first.Probe.SelectionCount, Is.Zero);
                Assert.That(
                    second.Probe.SelectionCount,
                    Is.Zero,
                    "A new target must not inherit dwell progress from the previous target."
                );

                yield return new WaitForSecondsRealtime(0.32f);
                Assert.That(second.Probe.SelectionCount, Is.EqualTo(1));
            }
            finally
            {
                second.Dispose();
                first.Dispose();
                fixture.Dispose();
            }
        }

        private static SelectorFixture CreateSelector()
        {
            GameObject selectorObject = new GameObject("S04PlayModeSelector");
            selectorObject.SetActive(false);
            selectorObject.transform.position = IsolatedOrigin;
            GazeDwellSelector selector = selectorObject.AddComponent<GazeDwellSelector>();
            SetPrivateField(selector, "_dwellSeconds", MinimumDwellSeconds);
            SetPrivateField(selector, "_cooldownSeconds", 0.0f);
            SetPrivateField(selector, "_showProgress", false);
            selectorObject.SetActive(true);
            Physics.SyncTransforms();
            return new SelectorFixture(selectorObject, selector);
        }

        private static TargetFixture CreateTarget(
            string name,
            Vector3? position = null
        )
        {
            GameObject targetObject = new GameObject(name);
            targetObject.layer = InteractionLayer;
            targetObject.transform.position =
                position ?? IsolatedOrigin + Vector3.forward * 2.0f;
            targetObject.AddComponent<BoxCollider>();
            S04DwellProbeTarget probe = targetObject.AddComponent<S04DwellProbeTarget>();
            Physics.SyncTransforms();
            return new TargetFixture(targetObject, probe);
        }

        private static void SetPrivateField<T>(
            GazeDwellSelector selector,
            string fieldName,
            T value
        )
        {
            FieldInfo field = typeof(GazeDwellSelector).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            Assert.That(field, Is.Not.Null, $"Missing test configuration field {fieldName}.");
            field.SetValue(selector, value);
        }

        private sealed class SelectorFixture
        {
            public SelectorFixture(GameObject gameObject, GazeDwellSelector selector)
            {
                GameObject = gameObject;
                Selector = selector;
            }

            public GameObject GameObject { get; }

            public GazeDwellSelector Selector { get; }

            public void Dispose()
            {
                Object.DestroyImmediate(GameObject);
            }
        }

        private sealed class TargetFixture
        {
            public TargetFixture(GameObject gameObject, S04DwellProbeTarget probe)
            {
                GameObject = gameObject;
                Probe = probe;
            }

            public GameObject GameObject { get; }

            public S04DwellProbeTarget Probe { get; }

            public void Dispose()
            {
                Object.DestroyImmediate(GameObject);
            }
        }
    }
}
