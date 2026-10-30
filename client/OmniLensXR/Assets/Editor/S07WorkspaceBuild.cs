using System;
using System.IO;
using OmniLensXR.S05;
using OmniLensXR.S07;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OmniLensXR.Editor
{
    public static class S07WorkspaceBuild
    {
        public const string ScenePath = "Assets/OmniLens/S07/S07WorkspaceFixture.unity";
        public const string MaterialPath = "Assets/OmniLens/S07/S07PanelBackground.mat";
        public const string BuildOutputPath = "Builds/iOS/S07Workspace";
        public const string PreviewOutputPath =
            "Builds/Previews/S07/OfflineWorkspace.png";

        [MenuItem("OmniLens/S07/Configure offline workspace fixture")]
        public static void ConfigureWorkspaceFixture()
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

            GameObject fixtureObject = new GameObject("S07WorkspaceFixture");
            S07WorkspaceFixture fixture = fixtureObject.AddComponent<S07WorkspaceFixture>();
            fixture.ConfigurePanelMaterial(material);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("Failed to save S07 fixture scene.");
            }

            PlayerSettings.productName = "OmniLens XR S07";
            PlayerSettings.bundleVersion = "0.7.0";
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[S07_BUILD] fixture configured; G02 viewer video NOT RUN");
        }

        [MenuItem("OmniLens/S07/Render offline workspace preview")]
        public static void RenderWorkspacePreview()
        {
            ConfigureWorkspaceFixture();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Camera camera = Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException("S07 fixture has no Main Camera.");
            }

            GameObject root = new GameObject("S07PreviewViews");
            Material material = GetOrCreateMaterial();
            WorkspaceLayoutData layout = S07WorkspaceDefaults.Create();
            if (
                !WorkspaceLayoutCodec.TryRestoreRegistry(
                    layout,
                    out PanelRegistry registry,
                    out WorkspaceLayoutError error
                )
            )
            {
                throw new InvalidOperationException($"S07 preview restore failed: {error}.");
            }

            PanelFactory factory = new PanelFactory(root.transform, material);
            foreach (PanelState state in registry.CreateSnapshot().Panels)
            {
                if (
                    state.Visibility == PanelVisibility.Visible
                    && !factory.TryAcquire(state, out _)
                )
                {
                    throw new InvalidOperationException("S07 preview view creation failed.");
                }
            }

            string directory = Path.GetDirectoryName(PreviewOutputPath);
            if (string.IsNullOrEmpty(directory))
            {
                throw new InvalidOperationException("Invalid S07 preview path.");
            }

            Directory.CreateDirectory(directory);
            RenderPreview(camera, PreviewOutputPath);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log(
                $"[S07_PREVIEW] rendered=1 panels={factory.ActiveCount} "
                    + $"output={PreviewOutputPath} viewer_readability=NOT_RUN"
            );
        }

        [MenuItem("OmniLens/S07/Build offline workspace fixture")]
        public static void BuildWorkspaceFixture()
        {
            ConfigureWorkspaceFixture();
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
                $"[S07_BUILD] result={summary.result} errors={summary.totalErrors} "
                    + $"warnings={summary.totalWarnings} bytes={summary.totalSize} "
                    + $"seconds={summary.totalTime.TotalSeconds:F1}"
            );
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"S07 iOS generation failed: {summary.result}."
                );
            }
        }

        private static Material GetOrCreateMaterial()
        {
            Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            if (shader == null)
            {
                throw new InvalidOperationException("No S07 fixture shader found.");
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

            material.color = new Color(0.045f, 0.095f, 0.135f, 1.0f);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static void RenderPreview(Camera camera, string outputPath)
        {
            const int width = 1600;
            const int height = 900;
            RenderTexture renderTexture = new RenderTexture(width, height, 24);
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                image.ReadPixels(new Rect(0.0f, 0.0f, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes(outputPath, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }
    }
}
