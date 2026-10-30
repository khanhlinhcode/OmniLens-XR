using System;
using System.IO;
using OmniLensXR.S08;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OmniLensXR.Editor
{
    public static class S08SceneBuild
    {
        public const string ScenePath = "Assets/OmniLens/S08/S08SceneFixture.unity";
        public const string MaterialPath = "Assets/OmniLens/S08/S08SceneMaterial.mat";
        public const string BuildOutputPath = "Builds/iOS/S08ScenePlatform";
        public const string PreviewOutputPath = "Builds/Previews/S08/GeneratedCube.png";

        [MenuItem("OmniLens/S08/Configure scene platform fixture")]
        public static void ConfigureFixture()
        {
            Material material = GetOrCreateMaterial();
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

            GameObject fixtureObject = new GameObject("S08SceneFixture");
            S08SceneFixture fixture = fixtureObject.AddComponent<S08SceneFixture>();
            fixture.ConfigureMaterial(material);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("Failed to save S08 fixture scene.");
            }

            PlayerSettings.productName = "OmniLens XR S08";
            PlayerSettings.bundleVersion = "0.8.0";
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[S08_BUILD] fixture configured; external assets downloaded=0");
        }

        [MenuItem("OmniLens/S08/Render generated scene preview")]
        public static void RenderPreview()
        {
            ConfigureFixture();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Camera camera = Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException("S08 fixture has no Main Camera.");
            }

            string root = Path.Combine(Application.dataPath, "StreamingAssets/S08");
            if (
                !SceneManifestCodec.TryDeserialize(
                    File.ReadAllText(Path.Combine(root, "scene-manifest-v1.json")),
                    out SceneManifestData manifest,
                    out SceneManifestError error
                )
            )
            {
                throw new InvalidOperationException($"S08 preview manifest failed: {error}.");
            }

            SceneAssetLifecycleTracker tracker = new SceneAssetLifecycleTracker();
            LocalSceneAssetLoader loader = new LocalSceneAssetLoader(
                new FileSceneAssetSource(),
                new RealtimeSceneLoadClock(),
                tracker,
                GetOrCreateMaterial()
            );
            SceneAssetLease lease = loader.Load(manifest, root);
            try
            {
                if (lease.Outcome != SceneLoadOutcome.Success)
                {
                    throw new InvalidOperationException(
                        $"S08 preview load failed: {lease.Outcome}."
                    );
                }

                lease.Root.transform.position = Vector3.forward * 2.5f;
                lease.Root.transform.rotation = Quaternion.Euler(18.0f, 28.0f, 0.0f);
                string directory = Path.GetDirectoryName(PreviewOutputPath);
                if (string.IsNullOrEmpty(directory))
                {
                    throw new InvalidOperationException("Invalid S08 preview path.");
                }

                Directory.CreateDirectory(directory);
                Render(camera, PreviewOutputPath);
                Debug.Log(
                    $"[S08_PREVIEW] outcome={lease.Outcome} bytes={lease.Metrics.BundleBytes} "
                        + $"vertices={lease.Metrics.Vertices} triangles={lease.Metrics.Triangles} "
                        + $"output={PreviewOutputPath} viewer=NOT_RUN"
                );
            }
            finally
            {
                lease.Dispose();
            }
        }

        [MenuItem("OmniLens/S08/Build scene platform fixture")]
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
                $"[S08_BUILD] result={summary.result} errors={summary.totalErrors} "
                    + $"warnings={summary.totalWarnings} bytes={summary.totalSize} "
                    + $"seconds={summary.totalTime.TotalSeconds:F1}"
            );
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"S08 iOS generation failed: {summary.result}."
                );
            }
        }

        private static Material GetOrCreateMaterial()
        {
            Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            if (shader == null)
            {
                throw new InvalidOperationException("No S08 fixture shader found.");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            material.color = new Color(0.08f, 0.55f, 0.95f, 1.0f);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static void Render(Camera camera, string outputPath)
        {
            const int width = 1600;
            const int height = 900;
            RenderTexture texture = new RenderTexture(width, height, 24);
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = texture;
                RenderTexture.active = texture;
                camera.Render();
                image.ReadPixels(new Rect(0.0f, 0.0f, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes(outputPath, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                texture.Release();
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
