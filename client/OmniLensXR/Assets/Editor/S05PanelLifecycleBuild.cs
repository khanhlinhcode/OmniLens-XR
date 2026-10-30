using System;
using System.IO;
using OmniLensXR.S05;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OmniLensXR.Editor
{
    public static class S05PanelLifecycleBuild
    {
        public const string ScenePath =
            "Assets/OmniLens/S05/S05PanelLifecycleFixture.unity";
        public const string BuildOutputPath = "Builds/iOS/S05PanelLifecycle";
        public const string PreviewOutputPath =
            "Builds/Previews/S05/PanelLifecycle.png";
        public const string PanelMaterialPath =
            "Assets/OmniLens/S05/S05PanelBackground.mat";

        private static readonly Color PanelBackgroundColor = new Color(
            0.060f,
            0.090f,
            0.140f,
            1.0f
        );

        [MenuItem("OmniLens/S05/Configure panel lifecycle fixture")]
        public static void ConfigurePanelLifecycleFixture()
        {
            Material panelMaterial = GetOrCreatePanelMaterial();
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

            GameObject fixtureObject = new GameObject("S05PanelLifecycleFixture");
            S05PanelLifecycleFixture fixture =
                fixtureObject.AddComponent<S05PanelLifecycleFixture>();
            fixture.ConfigurePanelMaterial(panelMaterial);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("Failed to save S05 fixture scene.");
            }

            PlayerSettings.productName = "OmniLens XR S05";
            PlayerSettings.bundleVersion = "0.5.0";
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[S05_BUILD] panel lifecycle fixture configured");
        }

        [MenuItem("OmniLens/S05/Render panel lifecycle preview")]
        public static void RenderPanelLifecyclePreview()
        {
            ConfigurePanelLifecycleFixture();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Camera camera = Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException("S05 fixture has no Main Camera.");
            }

            Material material = GetOrCreatePanelMaterial();
            GameObject previewRoot = new GameObject("S05PreviewPanelViews");
            PanelRegistry registry = new PanelRegistry();
            PanelWorkspaceController workspace = new PanelWorkspaceController(
                registry,
                new PanelFactory(previewRoot.transform, material)
            );
            RegisterPreviewPanel(registry, "chat", PanelKind.Chat, -0.95f, 2.3f);
            RegisterPreviewPanel(
                registry,
                "science-3d",
                PanelKind.Science3D,
                0.0f,
                2.5f
            );
            RegisterPreviewPanel(
                registry,
                "document",
                PanelKind.Document,
                0.95f,
                2.3f
            );
            OpenPreviewPanel(workspace, "chat");
            OpenPreviewPanel(workspace, "science-3d");
            OpenPreviewPanel(workspace, "document");

            string directory = Path.GetDirectoryName(PreviewOutputPath);
            if (string.IsNullOrEmpty(directory))
            {
                throw new InvalidOperationException("Invalid S05 preview output path.");
            }

            Directory.CreateDirectory(directory);
            RenderPreview(camera, PreviewOutputPath);
            UnityEngine.Object.DestroyImmediate(previewRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log(
                "[S05_PREVIEW] rendered=1 visible=chat,document,science-3d "
                    + $"output={PreviewOutputPath} viewer_readability=NOT_RUN"
            );
        }

        [MenuItem("OmniLens/S05/Build panel lifecycle fixture")]
        public static void BuildPanelLifecycleFixture()
        {
            ConfigurePanelLifecycleFixture();
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
                $"[S05_BUILD] result={summary.result} errors={summary.totalErrors} "
                    + $"warnings={summary.totalWarnings} bytes={summary.totalSize} "
                    + $"seconds={summary.totalTime.TotalSeconds:F1}"
            );
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"S05 iOS generation failed: {summary.result}."
                );
            }
        }

        private static Material GetOrCreatePanelMaterial()
        {
            Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "No supported shader found for S05 panel material."
                );
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(
                PanelMaterialPath
            );
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, PanelMaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            material.color = PanelBackgroundColor;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static void RegisterPreviewPanel(
            PanelRegistry registry,
            string panelId,
            PanelKind kind,
            float x,
            float z
        )
        {
            if (
                !PanelDefinition.TryCreate(
                    panelId,
                    kind,
                    new PanelBounds(
                        new Vector3(x, 0.0f, z),
                        new Vector2(0.75f, 0.75f)
                    ),
                    out PanelDefinition definition
                )
            )
            {
                throw new InvalidOperationException(
                    $"Invalid S05 preview definition: {panelId}."
                );
            }

            PanelApplyResult result = registry.Apply(
                PanelCommand.Register(registry.Version, definition)
            );
            if (!result.Accepted)
            {
                throw new InvalidOperationException(
                    $"S05 preview registration failed: {result.Error}."
                );
            }
        }

        private static void OpenPreviewPanel(
            PanelWorkspaceController workspace,
            string panelId
        )
        {
            PanelApplyResult result = workspace.Apply(
                PanelCommand.For(
                    PanelCommandKind.Open,
                    workspace.Registry.Version,
                    panelId
                )
            );
            if (!result.Accepted)
            {
                throw new InvalidOperationException(
                    $"S05 preview open failed for {panelId}: {result.Error}."
                );
            }
        }

        private static void RenderPreview(Camera camera, string outputPath)
        {
            const int width = 1600;
            const int height = 900;
            RenderTexture renderTexture = new RenderTexture(width, height, 24);
            Texture2D image = new Texture2D(
                width,
                height,
                TextureFormat.RGB24,
                false
            );
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
