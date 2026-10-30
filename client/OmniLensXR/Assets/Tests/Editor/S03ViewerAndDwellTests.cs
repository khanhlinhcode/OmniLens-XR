using System;
using NUnit.Framework;
using OmniLensXR.Editor;
using OmniLensXR.S03;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OmniLensXR.Tests.Editor
{
    public sealed class S03ViewerAndDwellTests
    {
        [TestCase(0.0)]
        [TestCase(0.49)]
        [TestCase(5.01)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void DwellStateRejectsInvalidDuration(double dwellSeconds)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new GazeDwellStateMachine(dwellSeconds, 0.5)
            );
        }

        [TestCase(-0.01)]
        [TestCase(5.01)]
        [TestCase(double.NaN)]
        public void DwellStateRejectsInvalidCooldown(double cooldownSeconds)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new GazeDwellStateMachine(1.5, cooldownSeconds)
            );
        }

        [Test]
        public void UninterruptedDwellTriggersExactlyOnceUntilGazeExit()
        {
            GazeDwellStateMachine state = new GazeDwellStateMachine(1.5, 0.5);

            GazeDwellStepResult halfway = state.Advance(10, 0.75, true);
            GazeDwellStepResult selected = state.Advance(10, 0.75, true);
            GazeDwellStepResult cooling = state.Advance(10, 0.25, true);
            GazeDwellStepResult locked = state.Advance(10, 0.25, true);

            Assert.That(halfway.Phase, Is.EqualTo(GazeDwellPhase.Focusing));
            Assert.That(halfway.Progress, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(selected.SelectionTriggered, Is.True);
            Assert.That(cooling.SelectionTriggered, Is.False);
            Assert.That(cooling.Phase, Is.EqualTo(GazeDwellPhase.Cooldown));
            Assert.That(locked.SelectionTriggered, Is.False);
            Assert.That(locked.Phase, Is.EqualTo(GazeDwellPhase.AwaitingExit));
        }

        [Test]
        public void GazeExitRearmsTheSameTarget()
        {
            GazeDwellStateMachine state = new GazeDwellStateMachine(1.0, 0.0);
            Assert.That(state.Advance(7, 1.0, true).SelectionTriggered, Is.True);
            Assert.That(state.Advance(7, 1.0, true).SelectionTriggered, Is.False);
            Assert.That(state.Advance(null, 0.1, true).Phase, Is.EqualTo(GazeDwellPhase.Idle));
            Assert.That(state.Advance(7, 1.0, true).SelectionTriggered, Is.True);
        }

        [Test]
        public void TargetChangeCancelsPreviousProgress()
        {
            GazeDwellStateMachine state = new GazeDwellStateMachine(1.5, 0.5);
            state.Advance(1, 1.0, true);

            GazeDwellStepResult changed = state.Advance(2, 0.5, true);

            Assert.That(changed.FocusCanceled, Is.True);
            Assert.That(changed.SelectionTriggered, Is.False);
            Assert.That(changed.Progress, Is.EqualTo(1.0f / 3.0f).Within(0.001f));
        }

        [Test]
        public void LifecycleInterruptionCancelsWithoutSelection()
        {
            GazeDwellStateMachine state = new GazeDwellStateMachine(1.0, 0.5);
            state.Advance(1, 0.9, true);

            GazeDwellStepResult interrupted = state.Advance(1, 0.2, false);

            Assert.That(interrupted.Phase, Is.EqualTo(GazeDwellPhase.Idle));
            Assert.That(interrupted.FocusCanceled, Is.True);
            Assert.That(interrupted.SelectionTriggered, Is.False);
        }

        [Test]
        public void InvalidDeltaFailsClosed()
        {
            GazeDwellStateMachine state = new GazeDwellStateMachine(1.0, 0.5);
            state.Advance(1, 0.9, true);

            GazeDwellStepResult invalid = state.Advance(1, double.NaN, true);

            Assert.That(invalid.Phase, Is.EqualTo(GazeDwellPhase.Idle));
            Assert.That(invalid.FocusCanceled, Is.True);
            Assert.That(invalid.SelectionTriggered, Is.False);
        }

        [Test]
        public void SelectorLayerAllowlistRejectsOtherLayers()
        {
            int mask = (1 << 7) | (1 << 8);
            Assert.That(GazeDwellSelector.IsLayerAllowed(7, mask), Is.True);
            Assert.That(GazeDwellSelector.IsLayerAllowed(8, mask), Is.True);
            Assert.That(GazeDwellSelector.IsLayerAllowed(0, mask), Is.False);
            Assert.That(GazeDwellSelector.IsLayerAllowed(-1, mask), Is.False);
            Assert.That(GazeDwellSelector.IsLayerAllowed(32, mask), Is.False);
        }

        [Test]
        public void ComfortFrameStatsUseBoundedPercentiles()
        {
            bool calculated = ComfortFrameCalculator.TryCalculate(
                new[] { 40.0f, 10.0f, 30.0f, 20.0f },
                out ComfortFrameStats stats
            );

            Assert.That(calculated, Is.True);
            Assert.That(stats.Count, Is.EqualTo(4));
            Assert.That(stats.AverageMilliseconds, Is.EqualTo(25.0f));
            Assert.That(stats.P50Milliseconds, Is.EqualTo(20.0f));
            Assert.That(stats.P95Milliseconds, Is.EqualTo(40.0f));
            Assert.That(stats.MinimumMilliseconds, Is.EqualTo(10.0f));
            Assert.That(stats.MaximumMilliseconds, Is.EqualTo(40.0f));
        }

        [TestCase(0.0f)]
        [TestCase(-1.0f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void ComfortFrameStatsRejectInvalidSamples(float invalidSample)
        {
            Assert.That(
                ComfortFrameCalculator.TryCalculate(
                    new[] { 16.0f, invalidSample },
                    out _
                ),
                Is.False
            );
        }

        [Test]
        public void ComfortScheduleEmitsEveryCrossedCheckpointOnce()
        {
            Assert.That(
                ComfortCheckpointSchedule.CrossedMinutes(299.0, 600.0),
                Is.EqualTo(new[] { 5, 10 })
            );
            Assert.That(
                ComfortCheckpointSchedule.CrossedMinutes(600.0, 1200.0),
                Is.EqualTo(new[] { 20 })
            );
            Assert.That(
                ComfortCheckpointSchedule.CrossedMinutes(1200.0, 1250.0),
                Is.Empty
            );
        }

        [Test]
        public void ThermalStateMappingRejectsUnknownNativeValues()
        {
            Assert.That(
                S03ThermalReader.FromNativeValue(0),
                Is.EqualTo(S03ThermalState.Nominal)
            );
            Assert.That(
                S03ThermalReader.FromNativeValue(3),
                Is.EqualTo(S03ThermalState.Critical)
            );
            Assert.That(
                S03ThermalReader.FromNativeValue(-2),
                Is.EqualTo(S03ThermalState.Unavailable)
            );
            Assert.That(
                S03ThermalReader.FromNativeValue(4),
                Is.EqualTo(S03ThermalState.Unavailable)
            );
        }

        [Test]
        public void RecenterTargetCallbackFailsClosed()
        {
            int invocations = 0;

            Assert.That(
                GazeDwellRecenterTarget.TryInvoke(() => invocations += 1),
                Is.True
            );
            Assert.That(GazeDwellRecenterTarget.TryInvoke(null), Is.False);
            Assert.That(
                GazeDwellRecenterTarget.TryInvoke(
                    () => throw new InvalidOperationException("test failure")
                ),
                Is.False
            );
            Assert.That(invocations, Is.EqualTo(1));
        }

        [Test]
        public void ConfiguredSceneContainsDwellAndComfortComponents()
        {
            S03IosBuild.ConfigureViewerFixture();
            Scene scene = EditorSceneManager.OpenScene(
                S02IosBuild.GetConfiguredHelloCardboardScenePath(),
                OpenSceneMode.Single
            );

            Assert.That(scene.IsValid(), Is.True);
            Assert.That(
                UnityEngine.Object.FindFirstObjectByType<GazeDwellSelector>(),
                Is.Not.Null
            );
            Assert.That(
                UnityEngine.Object.FindFirstObjectByType<S03ComfortProbe>(),
                Is.Not.Null
            );
            GazeDwellRecenterTarget recenterTarget =
                UnityEngine.Object.FindFirstObjectByType<GazeDwellRecenterTarget>();
            Assert.That(recenterTarget, Is.Not.Null);
            Assert.That(
                GazeDwellSelector.IsLayerAllowed(
                    recenterTarget.gameObject.layer,
                    (1 << 7) | (1 << 8)
                ),
                Is.True
            );
            MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
            Assert.That(
                Array.Exists(
                    behaviours,
                    behaviour =>
                        behaviour is IGazeDwellTarget
                        && GazeDwellSelector.IsLayerAllowed(
                            behaviour.gameObject.layer,
                            (1 << 7) | (1 << 8)
                        )
                ),
                Is.True,
                "The fixture must contain an allowlisted dwell target."
            );
            Assert.That(PlayerSettings.productName, Is.EqualTo(S03IosBuild.ProductName));
        }
    }
}
