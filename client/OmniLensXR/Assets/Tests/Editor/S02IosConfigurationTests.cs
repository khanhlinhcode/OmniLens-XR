using System.IO;
using NUnit.Framework;
using OmniLensXR.Editor;
using OmniLensXR.S02;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.iOS;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;

namespace OmniLensXR.Tests.Editor
{
    public sealed class S02IosConfigurationTests
    {
        [Test]
        public void SigningProbeUsesNonIdentifyingValidBundleIdentifier()
        {
            string identifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS);

            Assert.That(identifier, Is.EqualTo(S02IosBuild.BundleIdentifier));
            Assert.That(S02IosBuild.IsValidBundleIdentifier(identifier), Is.True);
            Assert.That(identifier, Does.Not.Contain("@"));
            Assert.That(identifier.Split('.'), Has.Length.GreaterThanOrEqualTo(3));
        }

        [TestCase("")]
        [TestCase("two.segments")]
        [TestCase("com.omnilensxr.invalid_segment.value")]
        [TestCase("1com.omnilensxr.invalid")]
        [TestCase("com..omnilensxr.invalid")]
        public void InvalidBundleIdentifiersAreRejectedByFixture(string value)
        {
            Assert.That(S02IosBuild.IsValidBundleIdentifier(value), Is.False);
        }

        [Test]
        public void IosPlayerSettingsMatchCardboardQuickstartBoundary()
        {
            Assert.That(PlayerSettings.iOS.targetOSVersionString, Is.EqualTo("15.0"));
            Assert.That(
                PlayerSettings.iOS.cameraUsageDescription,
                Is.EqualTo(S02IosBuild.CameraUsageDescription)
            );
            Assert.That(
                PlayerSettings.defaultInterfaceOrientation,
                Is.EqualTo(UIOrientation.LandscapeLeft)
            );
            Assert.That(PlayerSettings.iOS.appleEnableAutomaticSigning, Is.True);
            Assert.That(
                PlayerSettings.iOS.deferSystemGesturesMode,
                Is.EqualTo(
                    SystemGestureDeferMode.TopEdge
                        | SystemGestureDeferMode.LeftEdge
                        | SystemGestureDeferMode.RightEdge
                )
            );
        }

        [Test]
        public void HelloCardboardSceneExistsAndIsTheOnlyEnabledBuildScene()
        {
            string scenePath = S02IosBuild.GetConfiguredHelloCardboardScenePath();
            Assert.That(scenePath, Does.Contain("/1.36.0/Hello Cardboard/"));
            Assert.That(File.Exists(scenePath), Is.True);

            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            Assert.That(scenes, Has.Length.EqualTo(1));
            Assert.That(scenes[0].path, Is.EqualTo(scenePath));
            Assert.That(scenes[0].enabled, Is.True);
        }

        [Test]
        public void HelloCardboardHasInteractiveLayerAndRuntimeProbe()
        {
            Assert.That(LayerMask.NameToLayer("Interactive"), Is.EqualTo(7));
            Scene scene = EditorSceneManager.OpenScene(
                S02IosBuild.GetConfiguredHelloCardboardScenePath(),
                OpenSceneMode.Single
            );
            Assert.That(scene.IsValid(), Is.True);
            Assert.That(Object.FindFirstObjectByType<S02RuntimeProbe>(), Is.Not.Null);
        }

        [Test]
        public void CardboardLoaderIsAssignedAndStartsAutomatically()
        {
            XRGeneralSettingsPerBuildTarget perBuildTarget =
                AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(
                    S02IosBuild.GeneralSettingsPath
                );
            Assert.That(perBuildTarget, Is.Not.Null);

            XRGeneralSettings general = perBuildTarget.SettingsForBuildTarget(
                BuildTargetGroup.iOS
            );
            XRManagerSettings manager = perBuildTarget.ManagerSettingsForBuildTarget(
                BuildTargetGroup.iOS
            );
            Assert.That(general, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(general.InitManagerOnStart, Is.True);
            Assert.That(manager.automaticLoading, Is.True);
            Assert.That(manager.automaticRunning, Is.True);
            Assert.That(
                manager.activeLoaders,
                Has.Some.Matches<XRLoader>(
                    loader => loader != null && loader.GetType().FullName == S02IosBuild.CardboardLoaderType
                )
            );
        }

        [TestCase(0.0, false, true)]
        [TestCase(0.25, false, true)]
        [TestCase(0.999, false, true)]
        [TestCase(1.0, false, false)]
        [TestCase(2.0, false, false)]
        [TestCase(0.25, true, false)]
        [TestCase(-0.1, false, false)]
        public void RuntimeProbeSeparatesCheckpointTapFromRecenterHold(
            double durationSeconds,
            bool canceled,
            bool expected
        )
        {
            Assert.That(
                S02RuntimeProbe.IsCheckpointTap(durationSeconds, canceled),
                Is.EqualTo(expected)
            );
        }

        [TestCase(DeviceOrientation.LandscapeLeft, true)]
        [TestCase(DeviceOrientation.LandscapeRight, true)]
        [TestCase(DeviceOrientation.Portrait, false)]
        [TestCase(DeviceOrientation.PortraitUpsideDown, false)]
        [TestCase(DeviceOrientation.FaceUp, false)]
        [TestCase(DeviceOrientation.FaceDown, false)]
        [TestCase(DeviceOrientation.Unknown, false)]
        public void CardboardQrScannerOnlyLaunchesInPhysicalLandscape(
            DeviceOrientation orientation,
            bool expected
        )
        {
            Assert.That(CardboardScanGate.IsLandscape(orientation), Is.EqualTo(expected));
        }
    }
}
