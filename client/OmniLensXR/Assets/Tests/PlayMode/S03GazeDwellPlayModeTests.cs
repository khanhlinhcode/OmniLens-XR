using System.Collections;
using System.Reflection;
using NUnit.Framework;
using OmniLensXR.S03;
using UnityEngine;
using UnityEngine.TestTools;

namespace OmniLensXR.Tests.PlayMode
{
    public sealed class DwellProbeTarget : MonoBehaviour, IGazeDwellTarget
    {
        public int SelectionCount { get; private set; }

        public bool CanSelectByDwell => isActiveAndEnabled;

        public int DwellTargetId => GetInstanceID();

        public void SelectByDwell()
        {
            SelectionCount += 1;
        }
    }

    public sealed class S03GazeDwellPlayModeTests
    {
        private const float MinimumDwellSeconds = 0.5f;
        private static readonly Vector3 IsolatedOrigin = new Vector3(
            1000.0f,
            1000.0f,
            1000.0f
        );

        [UnityTest]
        public IEnumerator AllowlistedTargetSelectsOnceAndRequiresGazeExit()
        {
            Fixture fixture = CreateFixture(7);
            try
            {
                yield return new WaitForSecondsRealtime(0.65f);
                Assert.That(fixture.Target.SelectionCount, Is.EqualTo(1));

                yield return new WaitForSecondsRealtime(0.65f);
                Assert.That(
                    fixture.Target.SelectionCount,
                    Is.EqualTo(1),
                    "Staring at one target must not repeat its action."
                );

                fixture.TargetObject.transform.position += Vector3.right * 5.0f;
                Physics.SyncTransforms();
                yield return null;
                fixture.TargetObject.transform.position = IsolatedOrigin + Vector3.forward * 2.0f;
                Physics.SyncTransforms();

                yield return new WaitForSecondsRealtime(0.65f);
                Assert.That(fixture.Target.SelectionCount, Is.EqualTo(2));
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator NonAllowlistedLayerNeverSelects()
        {
            Fixture fixture = CreateFixture(0);
            try
            {
                yield return new WaitForSecondsRealtime(0.65f);
                Assert.That(fixture.Target.SelectionCount, Is.Zero);
                Assert.That(fixture.Selector.Phase, Is.EqualTo(GazeDwellPhase.Idle));
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator DisableCancelsProgressBeforeReenable()
        {
            Fixture fixture = CreateFixture(7);
            try
            {
                yield return new WaitForSecondsRealtime(0.30f);
                Assert.That(fixture.Selector.Progress, Is.GreaterThan(0.0f));
                Assert.That(fixture.Target.SelectionCount, Is.Zero);

                fixture.Selector.enabled = false;
                yield return null;
                Assert.That(fixture.Selector.Phase, Is.EqualTo(GazeDwellPhase.Idle));
                Assert.That(fixture.Selector.Progress, Is.Zero);

                fixture.Selector.enabled = true;
                yield return new WaitForSecondsRealtime(0.30f);
                Assert.That(
                    fixture.Target.SelectionCount,
                    Is.Zero,
                    "Progress from before disable must not carry over."
                );

                yield return new WaitForSecondsRealtime(0.35f);
                Assert.That(fixture.Target.SelectionCount, Is.EqualTo(1));
            }
            finally
            {
                fixture.Dispose();
            }
        }

        private static Fixture CreateFixture(int targetLayer)
        {
            GameObject selectorObject = new GameObject("S03PlayModeSelector");
            selectorObject.SetActive(false);
            selectorObject.transform.position = IsolatedOrigin;
            GazeDwellSelector selector = selectorObject.AddComponent<GazeDwellSelector>();
            SetPrivateField(selector, "_dwellSeconds", MinimumDwellSeconds);
            SetPrivateField(selector, "_showProgress", false);

            GameObject targetObject = new GameObject("S03PlayModeTarget");
            targetObject.layer = targetLayer;
            targetObject.transform.position = IsolatedOrigin + Vector3.forward * 2.0f;
            targetObject.AddComponent<BoxCollider>();
            DwellProbeTarget target = targetObject.AddComponent<DwellProbeTarget>();

            selectorObject.SetActive(true);
            Physics.SyncTransforms();
            return new Fixture(selectorObject, selector, targetObject, target);
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

        private sealed class Fixture
        {
            public Fixture(
                GameObject selectorObject,
                GazeDwellSelector selector,
                GameObject targetObject,
                DwellProbeTarget target
            )
            {
                SelectorObject = selectorObject;
                Selector = selector;
                TargetObject = targetObject;
                Target = target;
            }

            public GameObject SelectorObject { get; }

            public GazeDwellSelector Selector { get; }

            public GameObject TargetObject { get; }

            public DwellProbeTarget Target { get; }

            public void Dispose()
            {
                Object.DestroyImmediate(TargetObject);
                Object.DestroyImmediate(SelectorObject);
            }
        }
    }
}

