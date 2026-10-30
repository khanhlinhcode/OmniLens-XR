using System;
using System.Collections.Generic;
using System.IO;
using OmniLensXR.S04;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OmniLensXR.Editor
{
    public static class S04SpatialFixtureBuild
    {
        public const string ProductName = "OmniLens XR S04";
        public const string BundleVersion = "0.4.0";
        public const string FixtureDirectory =
            "Assets/OmniLens/S04/Fixtures";
        public const string BuildOutputPath = "Builds/iOS/S04SpatialFixture";
        public const string PreviewOutputDirectory = "Builds/Previews/S04";
        public const string PanelMaterialPath =
            FixtureDirectory + "/S04PanelBackground.mat";
        public const string FocusMaterialPath =
            FixtureDirectory + "/S04StaticFocus.mat";

        private static readonly Color PanelBackgroundColor = new Color(
            0.060f,
            0.090f,
            0.140f,
            1.0f
        );
        private static readonly Color FocusColor = new Color(
            0.20f,
            0.85f,
            1.0f,
            1.0f
        );

        [MenuItem("OmniLens/S04/Configure spatial fixtures")]
        public static void ConfigureSpatialFixtures()
        {
            Directory.CreateDirectory(FixtureDirectory);
            Material panelMaterial = GetOrCreateMaterial(
                PanelMaterialPath,
                PanelBackgroundColor
            );
            Material focusMaterial = GetOrCreateMaterial(
                FocusMaterialPath,
                FocusColor
            );
            foreach (SpatialWireframeKind kind in WireframeKinds())
            {
                CreateFixtureScene(kind, panelMaterial, focusMaterial);
            }

            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = BundleVersion;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[S04_BUILD] Three spatial fixtures configured; viewer readability NOT RUN."
            );
        }

        [MenuItem("OmniLens/S04/Build spatial fixtures")]
        public static void BuildSpatialFixtures()
        {
            ConfigureSpatialFixtures();
            List<string> scenes = new List<string>();
            foreach (SpatialWireframeKind kind in WireframeKinds())
            {
                scenes.Add(GetScenePath(kind));
            }

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = BuildOutputPath,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.Development | BuildOptions.AllowDebugging,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log(
                $"[S04_BUILD] result={summary.result} errors={summary.totalErrors} "
                    + $"warnings={summary.totalWarnings} bytes={summary.totalSize} "
                    + $"seconds={summary.totalTime.TotalSeconds:F1} output={BuildOutputPath}"
            );
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"S04 iOS project generation failed: {summary.result}."
                );
            }
        }

        [MenuItem("OmniLens/S04/Render spatial fixture previews")]
        public static void RenderSpatialFixturePreviews()
        {
            ConfigureSpatialFixtures();
            Directory.CreateDirectory(PreviewOutputDirectory);
            foreach (SpatialWireframeKind kind in WireframeKinds())
            {
                EditorSceneManager.OpenScene(GetScenePath(kind), OpenSceneMode.Single);
                Camera camera = Camera.main;
                if (camera == null)
                {
                    throw new InvalidOperationException(
                        $"S04 {kind} fixture has no Main Camera."
                    );
                }

                RenderPreview(camera, GetPreviewPath(kind));
            }

            Debug.Log(
                $"[S04_PREVIEW] rendered=3 output={PreviewOutputDirectory} "
                    + "viewer_readability=NOT_RUN"
            );
        }

        public static string GetScenePath(SpatialWireframeKind kind)
        {
            return $"{FixtureDirectory}/S04{kind}.unity";
        }

        public static string GetPreviewPath(SpatialWireframeKind kind)
        {
            return $"{PreviewOutputDirectory}/{kind}.png";
        }

        private static IEnumerable<SpatialWireframeKind> WireframeKinds()
        {
            yield return SpatialWireframeKind.HomeLauncher;
            yield return SpatialWireframeKind.ThreePanelWorkspace;
            yield return SpatialWireframeKind.SettingsAccessibility;
        }

        private static void CreateFixtureScene(
            SpatialWireframeKind kind,
            Material panelMaterial,
            Material focusMaterial
        )
        {
            SpatialLayoutDefinition layout = SpatialWireframeFactory.Create(
                kind,
                SpatialLayoutMode.Stereo
            );
            SpatialLayoutValidationResult validation = SpatialLayoutValidator.Validate(
                layout
            );
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(
                    $"Invalid {kind} fixture layout: {validation.Error}."
                );
            }

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single
            );
            CreateCameraAndLight();
            GameObject root = new GameObject($"Wireframe-{kind}");
            foreach (SpatialPanelSpec panel in layout.Panels)
            {
                CreatePanel(root.transform, panel, panelMaterial, focusMaterial);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, GetScenePath(kind)))
            {
                throw new InvalidOperationException($"Failed to save {kind} fixture.");
            }
        }

        private static void CreateCameraAndLight()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera cameraComponent = cameraObject.AddComponent<Camera>();
            cameraComponent.clearFlags = CameraClearFlags.SolidColor;
            cameraComponent.backgroundColor = Color.black;
            cameraComponent.nearClipPlane = 0.1f;

            GameObject lightObject = new GameObject("Directional Light");
            Light lightComponent = lightObject.AddComponent<Light>();
            lightComponent.type = LightType.Directional;
            lightComponent.intensity = 1.0f;
            lightObject.transform.rotation = Quaternion.Euler(35.0f, -25.0f, 0.0f);
        }

        private static void CreatePanel(
            Transform root,
            SpatialPanelSpec panel,
            Material panelMaterial,
            Material focusMaterial
        )
        {
            GameObject panelObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            panelObject.name = "Panel-" + panel.PanelId;
            panelObject.transform.SetParent(root, false);
            panelObject.transform.position = panel.PositionMeters;
            panelObject.transform.rotation = Quaternion.Euler(
                0.0f,
                panel.YawDegrees,
                0.0f
            );
            panelObject.transform.localScale = new Vector3(
                panel.SizeMeters.x,
                panel.SizeMeters.y,
                1.0f
            );
            Collider collider = panelObject.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
            MeshRenderer panelRenderer = panelObject.GetComponent<MeshRenderer>();
            panelRenderer.sharedMaterial = panelMaterial;

            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(panelObject.transform, false);
            labelObject.transform.localPosition = new Vector3(0.0f, 0.0f, 0.01f);
            labelObject.transform.localRotation = Quaternion.identity;
            labelObject.transform.localScale = new Vector3(
                1.0f / panel.SizeMeters.x,
                1.0f / panel.SizeMeters.y,
                1.0f
            );
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = GetPanelLabel(panel.PanelId);
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 32;
            label.characterSize = SpatialUiTokens.BodyTextHeightMeters;
            label.color = panel.IsPrimary ? Color.cyan : Color.white;

            if (panel.IsPrimary)
            {
                CreateStaticFocusIndicator(panelObject.transform, focusMaterial);
            }
        }

        private static string GetPanelLabel(string panelId)
        {
            switch (panelId)
            {
                case "home-launcher":
                    return "HOME\nPRIMARY";
                case "workspace-left":
                    return "LEFT";
                case "workspace-primary":
                    return "WORKSPACE\nPRIMARY";
                case "workspace-right":
                    return "RIGHT";
                case "settings-primary":
                    return "SETTINGS\nPRIMARY";
                case "accessibility":
                    return "ACCESS";
                default:
                    throw new InvalidOperationException(
                        $"No fixture label for panel ID {panelId}."
                    );
            }
        }

        private static void CreateStaticFocusIndicator(
            Transform panel,
            Material focusMaterial
        )
        {
            GameObject indicatorObject = new GameObject("StaticFocusIndicator");
            indicatorObject.transform.SetParent(panel, false);
            indicatorObject.transform.localPosition = new Vector3(0.0f, 0.0f, 0.02f);
            LineRenderer indicator = indicatorObject.AddComponent<LineRenderer>();
            indicator.sharedMaterial = focusMaterial;
            indicator.useWorldSpace = false;
            indicator.loop = true;
            indicator.positionCount = 4;
            indicator.widthMultiplier = 0.012f;
            indicator.numCapVertices = 0;
            indicator.startColor = FocusColor;
            indicator.endColor = FocusColor;
            indicator.SetPosition(0, new Vector3(-0.51f, -0.51f, 0.0f));
            indicator.SetPosition(1, new Vector3(-0.51f, 0.51f, 0.0f));
            indicator.SetPosition(2, new Vector3(0.51f, 0.51f, 0.0f));
            indicator.SetPosition(3, new Vector3(0.51f, -0.51f, 0.0f));
        }

        private static Material GetOrCreateMaterial(string path, Color color)
        {
            Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "No supported shader found for S04 fixture materials."
                );
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

