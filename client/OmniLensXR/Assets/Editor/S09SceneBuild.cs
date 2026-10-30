using System;
using OmniLensXR.S09;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OmniLensXR.Editor
{
    public static class S09SceneBuild
    {
        public const string ScenePath = "Assets/OmniLens/S09/S09SceneFixture.unity";
        public const string SceneMaterialPath =
            "Assets/OmniLens/S09/S09SceneMaterial.mat";
        public const string PanelMaterialPath =
            "Assets/OmniLens/S09/S09PanelMaterial.mat";
        public const string BuildOutputPath = "Builds/iOS/S09SceneInteraction";

        [MenuItem("OmniLens/S09/Configure scene interaction fixture")]
        public static void ConfigureFixture()
        {
            Material sceneMaterial = GetOrCreateMaterial(
                SceneMaterialPath,
                new Color(0.08f, 0.55f, 0.95f, 1.0f)
            );
            Material panelMaterial = GetOrCreateMaterial(
                PanelMaterialPath,
                new Color(0.06f, 0.10f, 0.18f, 1.0f)
            );
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single
            );
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.nearClipPlane = 0.1f;

            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.0f;
            lightObject.transform.rotation = Quaternion.Euler(35.0f, -30.0f, 0.0f);

            GameObject fixtureObject = new GameObject("S09SceneFixture");
            S09SceneFixture fixture = fixtureObject.AddComponent<S09SceneFixture>();
            fixture.ConfigureMaterials(sceneMaterial, panelMaterial);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("Failed to save S09 fixture scene.");
            }

            PlayerSettings.productName = "OmniLens XR S09";
            PlayerSettings.bundleVersion = "0.9.0";
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[S09_BUILD] fixture configured; external assets downloaded=0");
        }

        [MenuItem("OmniLens/S09/Build scene interaction fixture")]
        public static void BuildFixture()
        {
            ConfigureFixture();
            BuildReport report = BuildPipeline.BuildPlayer(
                new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = BuildOutputPath,
                    target = BuildTarget.iOS,
                    targetGroup = BuildTargetGroup.iOS,
                    options = BuildOptions.Development | BuildOptions.AllowDebugging,
                }
            );
            BuildSummary summary = report.summary;
            Debug.Log(
                $"[S09_BUILD] result={summary.result} errors={summary.totalErrors} "
                    + $"warnings={summary.totalWarnings} bytes={summary.totalSize} "
                    + $"seconds={summary.totalTime.TotalSeconds:F1}"
            );
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"S09 iOS generation failed: {summary.result}."
                );
            }
        }

        private static Material GetOrCreateMaterial(string path, Color color)
        {
            Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            if (shader == null)
            {
                throw new InvalidOperationException("No S09 fixture shader found.");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }
    }
}
