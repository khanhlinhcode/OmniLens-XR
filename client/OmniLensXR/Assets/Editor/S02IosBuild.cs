using System;
using System.IO;
using System.Linq;
using OmniLensXR.S02;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.iOS;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;

namespace OmniLensXR.Editor
{
    public static class S02IosBuild
    {
        public const string PackageName = "com.google.xr.cardboard";
        public const string PackageVersion = "1.36.0";
        public const string SampleDisplayName = "Hello Cardboard";
        public const string BundleIdentifier = "com.omnilensxr.local.s02.f4c9e27a";
        public const string DeploymentTarget = "15.0";
        public const string CameraUsageDescription =
            "Camera access is used only to scan a Cardboard viewer QR code and load its optical parameters.";
        public const string CardboardLoaderType = "Google.XR.Cardboard.XRLoader";
        public const string SigningProbeScene = "Assets/Scenes/SampleScene.unity";
        public const string GeneralSettingsPath =
            "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";

        private const int InteractiveLayerIndex = 7;

        [MenuItem("OmniLens/S02/Configure signing probe")]
        public static void ConfigureSigningProbe()
        {
            ConfigureCommonIosSettings();
            SetBuildScene(SigningProbeScene);
            Debug.Log("[S02_BUILD] Signing probe configuration is ready.");
        }

        [MenuItem("OmniLens/S02/Build signing probe")]
        public static void BuildSigningProbe()
        {
            ConfigureSigningProbe();
            BuildIosProject(SigningProbeScene, "Builds/iOS/S02Signing");
        }

        [MenuItem("OmniLens/S02/Import and configure Hello Cardboard")]
        public static void ImportAndConfigureHelloCardboard()
        {
            ConfigureCommonIosSettings();

            Sample sample = Sample.FindByPackage(PackageName, PackageVersion)
                .SingleOrDefault(candidate => candidate.displayName == SampleDisplayName);
            if (string.IsNullOrEmpty(sample.displayName))
            {
                throw new InvalidOperationException(
                    $"Sample '{SampleDisplayName}' was not found in {PackageName} {PackageVersion}."
                );
            }

            if (!sample.isImported)
            {
                bool imported = sample.Import(
                    Sample.ImportOptions.HideImportWindow
                        | Sample.ImportOptions.OverridePreviousImports
                );
                if (!imported)
                {
                    throw new InvalidOperationException("Unity failed to import Hello Cardboard.");
                }
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string scenePath = GetImportedHelloCardboardScenePath(sample.importPath);
            if (!File.Exists(Path.GetFullPath(scenePath)))
            {
                throw new FileNotFoundException("Imported HelloCardboard scene is missing.", scenePath);
            }

            ConfigureInteractiveLayer();
            AddRuntimeProbe(scenePath);
            ConfigureCardboardLoader();
            SetBuildScene(scenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[S02_BUILD] Hello Cardboard configured at {scenePath}.");
        }

        [MenuItem("OmniLens/S02/Build Hello Cardboard")]
        public static void BuildHelloCardboard()
        {
            ImportAndConfigureHelloCardboard();
            string scenePath = GetConfiguredHelloCardboardScenePath();
            BuildIosProject(scenePath, "Builds/iOS/HelloCardboard");
        }

        public static bool IsValidBundleIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 255)
            {
                return false;
            }

            string[] segments = value.Split('.');
            if (segments.Length < 3)
            {
                return false;
            }

            return segments.All(
                segment =>
                    segment.Length > 0
                    && char.IsLetter(segment[0])
                    && segment.All(character => char.IsLetterOrDigit(character) || character == '-')
            );
        }

        public static string GetConfiguredHelloCardboardScenePath()
        {
            Sample sample = Sample.FindByPackage(PackageName, PackageVersion)
                .SingleOrDefault(candidate => candidate.displayName == SampleDisplayName);
            if (string.IsNullOrEmpty(sample.importPath))
            {
                throw new InvalidOperationException("Hello Cardboard has not been imported.");
            }

            return GetImportedHelloCardboardScenePath(sample.importPath);
        }

        private static void ConfigureCommonIosSettings()
        {
            if (!IsValidBundleIdentifier(BundleIdentifier))
            {
                throw new InvalidOperationException("The S02 bundle identifier is invalid.");
            }

            PlayerSettings.companyName = "OmniLens XR";
            PlayerSettings.productName = "OmniLens XR S02";
            PlayerSettings.bundleVersion = "0.2.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleIdentifier);
            PlayerSettings.SetScriptingBackend(
                NamedBuildTarget.iOS,
                ScriptingImplementation.IL2CPP
            );
            PlayerSettings.iOS.buildNumber = "1";
            PlayerSettings.iOS.targetOSVersionString = DeploymentTarget;
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneOnly;
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            PlayerSettings.iOS.cameraUsageDescription = CameraUsageDescription;
            PlayerSettings.iOS.deferSystemGesturesMode =
                SystemGestureDeferMode.TopEdge
                | SystemGestureDeferMode.LeftEdge
                | SystemGestureDeferMode.RightEdge;
            PlayerSettings.iOS.hideHomeButton = true;
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.enableFrameTimingStats = true;
            EditorUserBuildSettings.development = true;
            EditorUserBuildSettings.allowDebugging = true;
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureCardboardLoader()
        {
            XRGeneralSettingsPerBuildTarget settings =
                AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(
                    GeneralSettingsPath
                );
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(settings, GeneralSettingsPath);
                EditorBuildSettings.AddConfigObject(
                    XRGeneralSettings.k_SettingsKey,
                    settings,
                    true
                );
            }

            if (!settings.HasSettingsForBuildTarget(BuildTargetGroup.iOS))
            {
                settings.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.iOS);
            }

            if (!settings.HasManagerSettingsForBuildTarget(BuildTargetGroup.iOS))
            {
                settings.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.iOS);
            }

            XRGeneralSettings general = settings.SettingsForBuildTarget(BuildTargetGroup.iOS);
            XRManagerSettings manager = settings.ManagerSettingsForBuildTarget(
                BuildTargetGroup.iOS
            );
            general.InitManagerOnStart = true;
            manager.automaticLoading = true;
            manager.automaticRunning = true;

            if (
                !XRPackageMetadataStore.IsLoaderAssigned(
                    CardboardLoaderType,
                    BuildTargetGroup.iOS
                )
                && !XRPackageMetadataStore.AssignLoader(
                    manager,
                    CardboardLoaderType,
                    BuildTargetGroup.iOS
                )
            )
            {
                throw new InvalidOperationException(
                    "Unable to assign the Cardboard XR loader for iOS."
                );
            }

            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(general);
            EditorUtility.SetDirty(manager);
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureInteractiveLayer()
        {
            UnityEngine.Object tagManager = AssetDatabase.LoadAllAssetsAtPath(
                "ProjectSettings/TagManager.asset"
            )[0];
            SerializedObject serialized = new SerializedObject(tagManager);
            SerializedProperty layers = serialized.FindProperty("layers");
            SerializedProperty interactive = layers.GetArrayElementAtIndex(
                InteractiveLayerIndex
            );
            interactive.stringValue = "Interactive";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        private static void AddRuntimeProbe(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            S02RuntimeProbe existing = UnityEngine.Object.FindFirstObjectByType<S02RuntimeProbe>();
            if (existing == null)
            {
                GameObject probeObject = new GameObject("S02RuntimeProbe");
                probeObject.AddComponent<S02RuntimeProbe>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static string GetImportedHelloCardboardScenePath(string importPath)
        {
            string absoluteScenePath = Path.Combine(
                importPath,
                "Scenes",
                "HelloCardboard.unity"
            );
            return FileUtil.GetProjectRelativePath(absoluteScenePath);
        }

        private static void SetBuildScene(string scenePath)
        {
            if (!File.Exists(Path.GetFullPath(scenePath)))
            {
                throw new FileNotFoundException("Build scene is missing.", scenePath);
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
        }

        private static void BuildIosProject(string scenePath, string locationPath)
        {
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                locationPathName = locationPath,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.Development | BuildOptions.AllowDebugging,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log(
                $"[S02_BUILD] result={summary.result} errors={summary.totalErrors} "
                    + $"warnings={summary.totalWarnings} bytes={summary.totalSize} "
                    + $"seconds={summary.totalTime.TotalSeconds:F1} output={locationPath}"
            );
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"S02 iOS project generation failed: {summary.result}."
                );
            }
        }
    }
}
