using System;
using NUnit.Framework;
using OmniLensXR.Editor;
using OmniLensXR.S04;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OmniLensXR.Tests.Editor
{
    public sealed class S04SpatialFoundationTests
    {
        private sealed class CountingSink : ISpatialInputSink
        {
            public int Count { get; private set; }

            public bool TryHandle(SpatialInputEvent inputEvent)
            {
                Count += 1;
                return true;
            }
        }

        private sealed class ThrowingSink : ISpatialInputSink
        {
            public bool TryHandle(SpatialInputEvent inputEvent)
            {
                throw new InvalidOperationException("expected test failure");
            }
        }

        private sealed class SelfRemovingSink : ISpatialInputSink
        {
            private readonly SpatialInputRouter _router;

            public SelfRemovingSink(SpatialInputRouter router)
            {
                _router = router;
            }

            public int Count { get; private set; }

            public bool TryHandle(SpatialInputEvent inputEvent)
            {
                Count += 1;
                _router.UnregisterSink(this);
                return true;
            }
        }

        [TestCase(SpatialWireframeKind.HomeLauncher, SpatialLayoutMode.Mono)]
        [TestCase(SpatialWireframeKind.HomeLauncher, SpatialLayoutMode.Stereo)]
        [TestCase(SpatialWireframeKind.ThreePanelWorkspace, SpatialLayoutMode.Mono)]
        [TestCase(SpatialWireframeKind.ThreePanelWorkspace, SpatialLayoutMode.Stereo)]
        [TestCase(SpatialWireframeKind.SettingsAccessibility, SpatialLayoutMode.Mono)]
        [TestCase(SpatialWireframeKind.SettingsAccessibility, SpatialLayoutMode.Stereo)]
        public void FactoryLayoutsAreValid(
            SpatialWireframeKind kind,
            SpatialLayoutMode mode
        )
        {
            SpatialLayoutDefinition layout = SpatialWireframeFactory.Create(kind, mode);

            Assert.That(SpatialLayoutValidator.Validate(layout).IsValid, Is.True);
        }

        [TestCase(SpatialWireframeKind.HomeLauncher)]
        [TestCase(SpatialWireframeKind.ThreePanelWorkspace)]
        [TestCase(SpatialWireframeKind.SettingsAccessibility)]
        public void MonoAndStereoLayoutsPreservePanelIdentity(
            SpatialWireframeKind kind
        )
        {
            SpatialLayoutDefinition mono = SpatialWireframeFactory.Create(
                kind,
                SpatialLayoutMode.Mono
            );
            SpatialLayoutDefinition stereo = SpatialWireframeFactory.Create(
                kind,
                SpatialLayoutMode.Stereo
            );

            Assert.That(stereo.Panels.Count, Is.EqualTo(mono.Panels.Count));
            for (int index = 0; index < mono.Panels.Count; index += 1)
            {
                Assert.That(
                    stereo.Panels[index].PanelId,
                    Is.EqualTo(mono.Panels[index].PanelId)
                );
                Assert.That(
                    stereo.Panels[index].IsPrimary,
                    Is.EqualTo(mono.Panels[index].IsPrimary)
                );
                Assert.That(
                    mono.Panels[index].PositionMeters.magnitude,
                    Is.EqualTo(SpatialUiTokens.DefaultPanelDistanceMeters).Within(0.001f)
                );
                Assert.That(
                    stereo.Panels[index].PositionMeters.magnitude,
                    Is.EqualTo(2.5f).Within(0.001f)
                );
                Assert.That(
                    Mathf.Abs(
                        Mathf.DeltaAngle(
                            mono.Panels[index].YawDegrees,
                            SpatialLayoutGeometry.FacingYawDegrees(
                                mono.Panels[index].PositionMeters
                            )
                        )
                    ),
                    Is.LessThan(0.001f)
                );
            }
        }

        [Test]
        public void LayoutCopiesPanelsAndRejectsBadOrientationOrMultiplePrimary()
        {
            SpatialPanelSpec[] source =
            {
                ValidPanel("primary", Vector3.forward * 2.0f, true),
            };
            SpatialLayoutDefinition copied = Layout(source);
            source[0] = ValidPanel("mutated", Vector3.forward * 2.0f, true);

            SpatialLayoutDefinition badOrientation = Layout(
                new SpatialPanelSpec(
                    "primary",
                    Vector3.forward * 2.0f,
                    new Vector2(0.5f, 0.5f),
                    true,
                    180.0f
                )
            );
            SpatialLayoutDefinition multiplePrimary = Layout(
                ValidPanel("first", new Vector3(-0.7f, 0.0f, 2.0f), true),
                ValidPanel("second", new Vector3(0.7f, 0.0f, 2.0f), true)
            );

            Assert.That(copied.Panels[0].PanelId, Is.EqualTo("primary"));
            Assert.That(
                SpatialLayoutValidator.Validate(badOrientation).Error,
                Is.EqualTo(SpatialLayoutError.InvalidPanelOrientation)
            );
            Assert.That(
                SpatialLayoutValidator.Validate(multiplePrimary).Error,
                Is.EqualTo(SpatialLayoutError.MultiplePrimaryPanels)
            );
        }

        [Test]
        public void LayoutRejectsUnsupportedVersion()
        {
            SpatialLayoutDefinition layout = new SpatialLayoutDefinition(
                99,
                SpatialWireframeKind.HomeLauncher,
                SpatialLayoutMode.Mono,
                new[] { ValidPanel("primary", Vector3.forward * 2.0f, true) }
            );

            Assert.That(
                SpatialLayoutValidator.Validate(layout).Error,
                Is.EqualTo(SpatialLayoutError.UnsupportedVersion)
            );
        }

        [Test]
        public void LayoutRejectsDuplicatePanelId()
        {
            SpatialLayoutDefinition layout = Layout(
                ValidPanel("duplicate", new Vector3(-0.5f, 0.0f, 2.0f), true),
                ValidPanel("duplicate", new Vector3(0.5f, 0.0f, 2.0f), false)
            );

            Assert.That(
                SpatialLayoutValidator.Validate(layout).Error,
                Is.EqualTo(SpatialLayoutError.DuplicatePanelId)
            );
        }

        [TestCase(float.NaN, 0.0f, 2.0f)]
        [TestCase(0.0f, float.PositiveInfinity, 2.0f)]
        [TestCase(0.0f, 0.0f, float.NegativeInfinity)]
        public void LayoutRejectsNonFinitePosition(float x, float y, float z)
        {
            SpatialLayoutDefinition layout = Layout(
                ValidPanel("primary", new Vector3(x, y, z), true)
            );

            Assert.That(
                SpatialLayoutValidator.Validate(layout).Error,
                Is.EqualTo(SpatialLayoutError.InvalidPanelPosition)
            );
        }

        [Test]
        public void LayoutRejectsOutOfSafeZonePanel()
        {
            SpatialLayoutDefinition layout = Layout(
                ValidPanel("primary", new Vector3(4.0f, 0.0f, 1.0f), true)
            );

            Assert.That(
                SpatialLayoutValidator.Validate(layout).Error,
                Is.EqualTo(SpatialLayoutError.OutsideSafeZone)
            );
        }

        [Test]
        public void LayoutRejectsInvalidSizeOverlapAndMissingPrimary()
        {
            SpatialLayoutDefinition invalidSize = Layout(
                new SpatialPanelSpec(
                    "primary",
                    Vector3.forward * 2.0f,
                    new Vector2(float.NaN, 0.5f),
                    true
                )
            );
            SpatialLayoutDefinition overlap = Layout(
                ValidPanel("first", new Vector3(0.0f, 0.0f, 2.0f), true),
                ValidPanel("second", new Vector3(0.1f, 0.0f, 2.0f), false)
            );
            SpatialLayoutDefinition noPrimary = Layout(
                ValidPanel("secondary", Vector3.forward * 2.0f, false)
            );

            Assert.That(
                SpatialLayoutValidator.Validate(invalidSize).Error,
                Is.EqualTo(SpatialLayoutError.InvalidPanelSize)
            );
            Assert.That(
                SpatialLayoutValidator.Validate(overlap).Error,
                Is.EqualTo(SpatialLayoutError.PanelOverlap)
            );
            Assert.That(
                SpatialLayoutValidator.Validate(noPrimary).Error,
                Is.EqualTo(SpatialLayoutError.MissingPrimaryPanel)
            );
        }

        [Test]
        public void ThirtyTargetsSelectOnceAndRequireExit()
        {
            SpatialFocusState focus = new SpatialFocusState(0.5, 0.25);

            for (int index = 1; index <= 30; index += 1)
            {
                Assert.That(
                    SpatialTargetDescriptor.TryCreate(
                        index,
                        "target_" + index,
                        true,
                        out SpatialTargetDescriptor target
                    ),
                    Is.True
                );
                Assert.That(
                    focus.Advance(target, 0.49, true).SelectionRequested,
                    Is.False
                );
                SpatialFocusStep selected = focus.Advance(target, 0.01, true);
                Assert.That(selected.SelectedActionId, Is.EqualTo("target_" + index));
                Assert.That(
                    focus.Advance(target, 1.0, true).SelectionRequested,
                    Is.False
                );
                focus.Advance(null, 0.01, true);
            }
        }

        [Test]
        public void FocusRejectsInvalidTargetAndCancelsOnLifecycleLoss()
        {
            Assert.That(
                SpatialTargetDescriptor.TryCreate(0, "invalid", true, out _),
                Is.False
            );
            Assert.That(
                SpatialTargetDescriptor.TryCreate(1, "bad action", true, out _),
                Is.False
            );
            SpatialTargetDescriptor.TryCreate(1, "valid", true, out var target);
            SpatialFocusState focus = new SpatialFocusState(0.5, 0.25);
            focus.Advance(target, 0.49, true);

            SpatialFocusStep interrupted = focus.Advance(target, 0.01, false);

            Assert.That(interrupted.SelectionRequested, Is.False);
            Assert.That(interrupted.FocusCanceled, Is.True);
            Assert.That(focus.Advance(target, 0.01, true).SelectionRequested, Is.False);
        }

        [TestCase(0L, 0.0)]
        [TestCase(-1L, 0.0)]
        [TestCase(1L, -0.01)]
        [TestCase(1L, double.NaN)]
        [TestCase(1L, double.PositiveInfinity)]
        public void InputEventRejectsInvalidSequenceOrTime(
            long sequence,
            double timestamp
        )
        {
            Assert.That(
                SpatialInputEvent.TryCreate(
                    sequence,
                    timestamp,
                    SpatialInputEventKind.TriggerPressed,
                    SpatialInputSourceKind.Cardboard,
                    null,
                    out _
                ),
                Is.False
            );
        }

        [Test]
        public void HeadPoseRequiresFiniteNonZeroQuaternion()
        {
            SpatialHeadPose invalid = new SpatialHeadPose(
                Vector3.zero,
                new Quaternion(0.0f, 0.0f, 0.0f, 0.0f)
            );
            SpatialHeadPose valid = new SpatialHeadPose(Vector3.zero, Quaternion.identity);
            SpatialHeadPose underScaled = new SpatialHeadPose(
                Vector3.zero,
                new Quaternion(0.5f, 0.0f, 0.0f, 0.0f)
            );
            SpatialHeadPose overScaled = new SpatialHeadPose(
                Vector3.zero,
                new Quaternion(2.0f, 0.0f, 0.0f, 0.0f)
            );

            Assert.That(
                SpatialInputEvent.TryCreate(
                    1,
                    0.0,
                    SpatialInputEventKind.HeadPose,
                    SpatialInputSourceKind.Cardboard,
                    invalid,
                    out _
                ),
                Is.False
            );
            Assert.That(
                SpatialInputEvent.TryCreate(
                    5,
                    0.4,
                    SpatialInputEventKind.HeadPose,
                    SpatialInputSourceKind.Cardboard,
                    underScaled,
                    out _
                ),
                Is.False
            );
            Assert.That(
                SpatialInputEvent.TryCreate(
                    6,
                    0.5,
                    SpatialInputEventKind.HeadPose,
                    SpatialInputSourceKind.Cardboard,
                    overScaled,
                    out _
                ),
                Is.False
            );
            Assert.That(
                SpatialInputEvent.TryCreate(
                    3,
                    0.2,
                    (SpatialInputEventKind)999,
                    SpatialInputSourceKind.Cardboard,
                    null,
                    out _
                ),
                Is.False
            );
            Assert.That(
                SpatialInputEvent.TryCreate(
                    4,
                    0.3,
                    SpatialInputEventKind.Recenter,
                    (SpatialInputSourceKind)999,
                    null,
                    out _
                ),
                Is.False
            );
            Assert.That(
                SpatialInputEvent.TryCreate(
                    1,
                    0.0,
                    SpatialInputEventKind.HeadPose,
                    SpatialInputSourceKind.Cardboard,
                    valid,
                    out _
                ),
                Is.True
            );
            Assert.That(
                SpatialInputEvent.TryCreate(
                    2,
                    0.1,
                    SpatialInputEventKind.TriggerPressed,
                    SpatialInputSourceKind.Cardboard,
                    valid,
                    out _
                ),
                Is.False
            );
        }

        [Test]
        public void RouterRejectsReplayAndDisabledSourceThenRecovers()
        {
            SpatialInputRouter router = new SpatialInputRouter();
            CountingSink sink = new CountingSink();
            Assert.That(router.RegisterSink(sink), Is.True);
            SpatialInputEvent.TryCreate(
                1,
                0.0,
                SpatialInputEventKind.TriggerPressed,
                SpatialInputSourceKind.Controller,
                null,
                out SpatialInputEvent trigger
            );
            Assert.That(router.Route(trigger), Is.EqualTo(SpatialRouteResult.Accepted));
            Assert.That(
                router.Route(trigger),
                Is.EqualTo(SpatialRouteResult.StaleSequence)
            );

            SpatialInputEvent.TryCreate(
                2,
                0.1,
                SpatialInputEventKind.SourceLost,
                SpatialInputSourceKind.Controller,
                null,
                out SpatialInputEvent lost
            );
            Assert.That(router.Route(lost), Is.EqualTo(SpatialRouteResult.Accepted));
            SpatialInputEvent.TryCreate(
                3,
                0.2,
                SpatialInputEventKind.TriggerPressed,
                SpatialInputSourceKind.Controller,
                null,
                out SpatialInputEvent disabledTrigger
            );
            Assert.That(
                router.Route(disabledTrigger),
                Is.EqualTo(SpatialRouteResult.SourceDisabled)
            );
            SpatialInputEvent.TryCreate(
                4,
                0.3,
                SpatialInputEventKind.SourceRestored,
                SpatialInputSourceKind.Controller,
                null,
                out SpatialInputEvent restored
            );
            Assert.That(router.Route(restored), Is.EqualTo(SpatialRouteResult.Accepted));
            Assert.That(sink.Count, Is.EqualTo(3));
        }

        [Test]
        public void RouterBoundsAndDeduplicatesSinks()
        {
            SpatialInputRouter router = new SpatialInputRouter();
            CountingSink first = new CountingSink();
            Assert.That(router.RegisterSink(first), Is.True);
            Assert.That(router.RegisterSink(first), Is.False);
            for (int index = 1; index < SpatialInputRouter.MaximumSinks; index += 1)
            {
                Assert.That(router.RegisterSink(new CountingSink()), Is.True);
            }

            Assert.That(router.RegisterSink(new CountingSink()), Is.False);
            Assert.That(router.SinkCount, Is.EqualTo(SpatialInputRouter.MaximumSinks));
        }

        [Test]
        public void RouterIsolatesThrowingAndSelfRemovingSinks()
        {
            SpatialInputRouter router = new SpatialInputRouter();
            ThrowingSink throwing = new ThrowingSink();
            SelfRemovingSink removing = new SelfRemovingSink(router);
            CountingSink counting = new CountingSink();
            router.RegisterSink(throwing);
            router.RegisterSink(removing);
            router.RegisterSink(counting);
            SpatialInputEvent.TryCreate(
                1,
                0.0,
                SpatialInputEventKind.TriggerPressed,
                SpatialInputSourceKind.Simulator,
                null,
                out SpatialInputEvent first
            );

            Assert.That(
                router.Route(first),
                Is.EqualTo(SpatialRouteResult.AcceptedWithHandlerFailure)
            );
            Assert.That(removing.Count, Is.EqualTo(1));
            Assert.That(counting.Count, Is.EqualTo(1));
            Assert.That(router.SinkCount, Is.EqualTo(2));

            SpatialInputRouter failureOnly = new SpatialInputRouter();
            failureOnly.RegisterSink(new ThrowingSink());
            Assert.That(
                failureOnly.Route(first),
                Is.EqualTo(SpatialRouteResult.HandlerFailed)
            );
        }

        [Test]
        public void SimulatorAndCardboardSourcesShareTypedContract()
        {
            ISpatialInputSource simulator = new SpatialInputSimulator();
            ISpatialInputSource cardboardSource = new CardboardInputSource();
            Assert.That(
                simulator.TryEmit(
                    SpatialInputEventKind.Recenter,
                    1.0,
                    null,
                    out SpatialInputEvent simulated
                ),
                Is.True
            );
            Assert.That(
                cardboardSource.TryEmit(
                    SpatialInputEventKind.Recenter,
                    1.0,
                    null,
                    out SpatialInputEvent cardboard
                ),
                Is.True
            );

            Assert.That(simulated.Kind, Is.EqualTo(cardboard.Kind));
            Assert.That(simulated.Sequence, Is.EqualTo(cardboard.Sequence));
            Assert.That(simulated.TimestampSeconds, Is.EqualTo(cardboard.TimestampSeconds));
            Assert.That(simulated.Source, Is.EqualTo(simulator.Source));
            Assert.That(cardboard.Source, Is.EqualTo(cardboardSource.Source));
        }

        [Test]
        public void AccessibilityDefaultUsesStaticHighContrastReducedMotion()
        {
            SpatialAccessibilityProfile profile =
                SpatialAccessibilityProfile.ReducedMotionDefault();

            Assert.That(
                SpatialAccessibilityValidator.Validate(profile),
                Is.EqualTo(SpatialAccessibilityError.None)
            );
            Assert.That(
                SpatialAccessibilityValidator.ContrastRatio(Color.white, Color.black),
                Is.EqualTo(21.0f).Within(0.01f)
            );
        }

        [Test]
        public void AccessibilityRejectsLowContrastMissingCueAndReducedMotionScale()
        {
            SpatialAccessibilityProfile lowContrast = new SpatialAccessibilityProfile(
                true,
                true,
                SpatialTextTier.Body,
                Color.gray,
                new Color(0.55f, 0.55f, 0.55f),
                0.0f
            );
            SpatialAccessibilityProfile missingCue = new SpatialAccessibilityProfile(
                true,
                false,
                SpatialTextTier.Body,
                Color.white,
                Color.black,
                0.0f
            );
            SpatialAccessibilityProfile animated = new SpatialAccessibilityProfile(
                true,
                true,
                SpatialTextTier.Body,
                Color.white,
                Color.black,
                0.05f
            );
            SpatialAccessibilityProfile invalidColor = new SpatialAccessibilityProfile(
                true,
                true,
                SpatialTextTier.Body,
                new Color(float.NaN, 1.0f, 1.0f),
                Color.black,
                0.0f
            );

            Assert.That(
                SpatialAccessibilityValidator.Validate(lowContrast),
                Is.EqualTo(SpatialAccessibilityError.InsufficientFocusContrast)
            );
            Assert.That(
                SpatialAccessibilityValidator.Validate(missingCue),
                Is.EqualTo(SpatialAccessibilityError.MissingStaticFocusIndicator)
            );
            Assert.That(
                SpatialAccessibilityValidator.Validate(animated),
                Is.EqualTo(SpatialAccessibilityError.ReducedMotionUsesScaleAnimation)
            );
            Assert.That(
                SpatialAccessibilityValidator.Validate(invalidColor),
                Is.EqualTo(SpatialAccessibilityError.InvalidFocusColor)
            );
        }

        [Test]
        public void ConfiguredFixtureScenesContainExpectedPanels()
        {
            S04SpatialFixtureBuild.ConfigureSpatialFixtures();
            foreach (SpatialWireframeKind kind in Enum.GetValues(typeof(SpatialWireframeKind)))
            {
                SpatialLayoutDefinition layout = SpatialWireframeFactory.Create(
                    kind,
                    SpatialLayoutMode.Stereo
                );
                Scene scene = EditorSceneManager.OpenScene(
                    S04SpatialFixtureBuild.GetScenePath(kind),
                    OpenSceneMode.Single
                );
                GameObject root = GameObject.Find("Wireframe-" + kind);

                Assert.That(scene.IsValid(), Is.True);
                Assert.That(root, Is.Not.Null);
                Assert.That(root.transform.childCount, Is.EqualTo(layout.Panels.Count));
                for (int index = 0; index < layout.Panels.Count; index += 1)
                {
                    SpatialPanelSpec panel = layout.Panels[index];
                    Transform panelTransform = root.transform.Find(
                        "Panel-" + panel.PanelId
                    );
                    Assert.That(panelTransform, Is.Not.Null);
                    Assert.That(
                        Mathf.Abs(
                            Mathf.DeltaAngle(
                                panelTransform.eulerAngles.y,
                                panel.YawDegrees
                            )
                        ),
                        Is.LessThan(0.01f)
                    );

                    MeshRenderer renderer = panelTransform.GetComponent<MeshRenderer>();
                    Assert.That(
                        AssetDatabase.GetAssetPath(renderer.sharedMaterial),
                        Is.EqualTo(S04SpatialFixtureBuild.PanelMaterialPath)
                    );
                    TextMesh label = panelTransform.GetComponentInChildren<TextMesh>();
                    Assert.That(label, Is.Not.Null);
                    Assert.That(
                        label.characterSize,
                        Is.EqualTo(SpatialUiTokens.BodyTextHeightMeters).Within(0.0001f)
                    );
                    Bounds labelBounds = label.GetComponent<MeshRenderer>().bounds;
                    Assert.That(
                        labelBounds.size.x,
                        Is.LessThanOrEqualTo(panel.SizeMeters.x * 0.9f)
                    );
                    Assert.That(
                        labelBounds.size.y,
                        Is.LessThanOrEqualTo(panel.SizeMeters.y * 0.6f)
                    );
                    Assert.That(
                        SpatialAccessibilityValidator.ContrastRatio(
                            label.color,
                            renderer.sharedMaterial.color
                        ),
                        Is.GreaterThanOrEqualTo(
                            SpatialUiTokens.MinimumFocusContrastRatio
                        )
                    );
                    Transform indicator = panelTransform.Find("StaticFocusIndicator");
                    Assert.That(indicator != null, Is.EqualTo(panel.IsPrimary));
                }
            }
        }

        private static SpatialLayoutDefinition Layout(params SpatialPanelSpec[] panels)
        {
            return new SpatialLayoutDefinition(
                SpatialUiTokens.SchemaVersion,
                SpatialWireframeKind.HomeLauncher,
                SpatialLayoutMode.Mono,
                panels
            );
        }

        private static SpatialPanelSpec ValidPanel(
            string panelId,
            Vector3 position,
            bool primary
        )
        {
            return new SpatialPanelSpec(
                panelId,
                position,
                new Vector2(0.5f, 0.5f),
                primary
            );
        }
    }
}

