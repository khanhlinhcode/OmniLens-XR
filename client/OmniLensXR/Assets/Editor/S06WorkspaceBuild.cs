using System;
using System.IO;
using OmniLensXR.S05;
using OmniLensXR.S06;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OmniLensXR.Editor
{
    public static class S06WorkspaceBuild
    {
        public const string ScenePath =
            "Assets/OmniLens/S06/S06WorkspaceFixture.unity";
        public const string MaterialPath =
            "Assets/OmniLens/S06/S06PanelBackground.mat";
        public const string BuildOutputPath = "Builds/iOS/S06Workspace";
        public const string PreviewOutputPath =
            "Builds/Previews/S06/WorkspaceManipulation.png";

        private static readonly Color PanelColor = new Color(
            0.055f,
            0.085f,
            0.135f,
            1.0f
        );

        [MenuItem("OmniLens/S06/Configure workspace fixture")]
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

            GameObject fixtureObject = new GameObject("S06WorkspaceFixture");
            S06WorkspaceFixture fixture =
                fixtureObject.AddComponent<S06WorkspaceFixture>();
            fixture.ConfigurePanelMaterial(material);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("Failed to save S06 fixture scene.");
            }

            PlayerSettings.productName = "OmniLens XR S06";
            PlayerSettings.bundleVersion = "0.6.0";
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[S06_BUILD] workspace fixture configured; viewer fatigue NOT RUN"
            );
        }

        [MenuItem("OmniLens/S06/Render workspace preview")]
        public static void RenderWorkspacePreview()
        {
            ConfigureWorkspaceFixture();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Camera camera = Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException("S06 fixture has no Main Camera.");
            }

            Material material = GetOrCreateMaterial();
            GameObject root = new GameObject("S06PreviewViews");
            PanelRegistry registry = new PanelRegistry();
            PanelWorkspaceController workspace = new PanelWorkspaceController(
                registry,
                new PanelFactory(root.transform, material)
            );
            Register(registry, "chat", PanelKind.Chat, -0.95f);
            Register(registry, "science-3d", PanelKind.Science3D, 0.0f);
            Register(registry, "document", PanelKind.Document, 0.95f);
            Open(workspace, "chat");
            Open(workspace, "science-3d");
            Open(workspace, "document");
            PanelManipulationController manipulation =
                new PanelManipulationController(workspace);
            Require(manipulation.Apply(PanelHandleKind.MoveRight, "document"));
            Require(manipulation.Apply(PanelHandleKind.ResizeWider, "document"));
            Require(manipulation.Apply(PanelHandleKind.TogglePin, "document"));

            string directory = Path.GetDirectoryName(PreviewOutputPath);
            if (string.IsNullOrEmpty(directory))
            {
                throw new InvalidOperationException("Invalid S06 preview path.");
            }

            Directory.CreateDirectory(directory);
            RenderPreview(camera, PreviewOutputPath);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log(
                "[S06_PREVIEW] rendered=1 moved_resized_pinned=document "
                    + $"output={PreviewOutputPath} viewer_readability=NOT_RUN"
            );
        }

        [MenuItem("OmniLens/S06/Build workspace fixture")]
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
                $"[S06_BUILD] result={summary.result} errors={summary.totalErrors} "
                    + $"warnings={summary.totalWarnings} bytes={summary.totalSize} "
                    + $"seconds={summary.totalTime.TotalSeconds:F1}"
            );
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"S06 iOS generation failed: {summary.result}."
                );
            }
        }

        private static Material GetOrCreateMaterial()
        {
            Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            if (shader == null)
            {
                throw new InvalidOperationException("No S06 fixture shader found.");
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

            material.color = PanelColor;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static void Register(
            PanelRegistry registry,
            string panelId,
            PanelKind kind,
            float x
        )
        {
            PanelPlacementResult placement = PanelPlacementService.Resolve(
                Array.Empty<PanelBounds>(),
                new Vector3(x, 0.0f, 2.5f),
                new Vector2(0.65f, 0.65f)
            );
            if (
                !placement.Accepted
                || !PanelDefinition.TryCreate(
                    panelId,
                    kind,
                    placement.Bounds,
                    out PanelDefinition definition
                )
            )
            {
                throw new InvalidOperationException(
                    $"Invalid S06 preview panel: {panelId}."
                );
            }

            PanelApplyResult result = registry.Apply(
                PanelCommand.Register(registry.Version, definition)
            );
            if (!result.Accepted)
            {
                throw new InvalidOperationException(
                    $"S06 preview registration failed: {result.Error}."
                );
            }
        }

        private static void Open(PanelWorkspaceController workspace, string panelId)
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
                    $"S06 preview open failed: {result.Error}."
                );
            }
        }

        private static void Require(PanelManipulationResult result)
        {
            if (!result.Accepted)
            {
                throw new InvalidOperationException(
                    $"S06 preview manipulation failed: {result.Error}."
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
