using System;
using OmniLensXR.S03;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OmniLensXR.Editor
{
    public static class S03IosBuild
    {
        public const string ProductName = "OmniLens XR S03";
        public const string BundleVersion = "0.3.0";
        public const string BuildOutputPath = "Builds/iOS/S03ViewerFixture";
        public const int RecenterInteractionLayer = 8;

        [MenuItem("OmniLens/S03/Configure viewer fixture")]
        public static void ConfigureViewerFixture()
        {
            S02IosBuild.ImportAndConfigureHelloCardboard();
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = BundleVersion;

            string scenePath = S02IosBuild.GetConfiguredHelloCardboardScenePath();
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            CardboardReticlePointer reticle = UnityEngine.Object.FindFirstObjectByType<CardboardReticlePointer>();
            if (reticle == null)
            {
                throw new InvalidOperationException(
                    "The HelloCardboard scene has no CardboardReticlePointer."
                );
            }

            if (reticle.GetComponent<GazeDwellSelector>() == null)
            {
                reticle.gameObject.AddComponent<GazeDwellSelector>();
            }

            if (UnityEngine.Object.FindFirstObjectByType<S03ComfortProbe>() == null)
            {
                GameObject probeObject = new GameObject("S03ComfortProbe");
                probeObject.AddComponent<S03ComfortProbe>();
            }

            EnsureGazeRecenterTarget();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[S03_BUILD] Viewer fixture configured without a hardware PASS claim.");
        }

        [MenuItem("OmniLens/S03/Build viewer fixture")]
        public static void BuildViewerFixture()
        {
            ConfigureViewerFixture();
            string scenePath = S02IosBuild.GetConfiguredHelloCardboardScenePath();
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                locationPathName = BuildOutputPath,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.Development | BuildOptions.AllowDebugging,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log(
                $"[S03_BUILD] result={summary.result} errors={summary.totalErrors} "
                    + $"warnings={summary.totalWarnings} bytes={summary.totalSize} "
                    + $"seconds={summary.totalTime.TotalSeconds:F1} output={BuildOutputPath}"
            );
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"S03 iOS project generation failed: {summary.result}."
                );
            }
        }

        private static void EnsureGazeRecenterTarget()
        {
            if (
                UnityEngine.Object.FindFirstObjectByType<GazeDwellRecenterTarget>()
                != null
            )
            {
                return;
            }

            Camera sceneCamera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (sceneCamera == null)
            {
                throw new InvalidOperationException(
                    "The HelloCardboard scene has no camera for the recenter target."
                );
            }

            GameObject targetObject = new GameObject("S03GazeRecenterTarget");
            targetObject.layer = RecenterInteractionLayer;
            targetObject.transform.position = sceneCamera.transform.TransformPoint(
                new Vector3(0.0f, -0.65f, 2.5f)
            );
            targetObject.transform.rotation =
                sceneCamera.transform.rotation * Quaternion.Euler(0.0f, 180.0f, 0.0f);

            TextMesh label = targetObject.AddComponent<TextMesh>();
            label.text = "RECENTER";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 64;
            label.characterSize = 0.08f;
            label.fontStyle = FontStyle.Bold;
            label.color = new Color(0.20f, 0.85f, 1.0f, 1.0f);

            BoxCollider collider = targetObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.2f, 0.35f, 0.05f);
            targetObject.AddComponent<GazeDwellRecenterTarget>();
        }
    }
}
