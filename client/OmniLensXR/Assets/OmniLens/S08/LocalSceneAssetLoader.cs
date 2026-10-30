using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

namespace OmniLensXR.S08
{
    public enum SceneLoadOutcome
    {
        Success = 1,
        ManifestInvalid,
        UnsafePath,
        SourceMissing,
        SourceTooLarge,
        HashMismatch,
        Timeout,
        BundleInvalid,
        InternalError,
    }

    [Serializable]
    public sealed class SceneMeshBundleData
    {
        public int formatVersion;
        public string assetId;
        public float[] vertices;
        public int[] triangles;
        public float[] color;
    }

    public readonly struct SceneAssetMetrics
    {
        public SceneAssetMetrics(
            string assetId,
            int bundleBytes,
            int vertices,
            int triangles,
            int textureBytes,
            bool usedFallback
        )
        {
            AssetId = assetId;
            BundleBytes = bundleBytes;
            Vertices = vertices;
            Triangles = triangles;
            TextureBytes = textureBytes;
            UsedFallback = usedFallback;
        }

        public string AssetId { get; }
        public int BundleBytes { get; }
        public int Vertices { get; }
        public int Triangles { get; }
        public int TextureBytes { get; }
        public bool UsedFallback { get; }
    }

    public sealed class SceneAssetLifecycleTracker
    {
        public int Created { get; private set; }
        public int Disposed { get; private set; }
        public int Active { get; private set; }
        public int PeakActive { get; private set; }

        internal void OnCreated()
        {
            Created += 1;
            Active += 1;
            PeakActive = Math.Max(PeakActive, Active);
        }

        internal void OnDisposed()
        {
            Disposed += 1;
            Active = Math.Max(0, Active - 1);
        }
    }

    public interface ISceneAssetSource
    {
        bool TryRead(
            string fullPath,
            int maximumBytes,
            out byte[] bytes,
            out SceneLoadOutcome error
        );
    }

    public sealed class FileSceneAssetSource : ISceneAssetSource
    {
        public bool TryRead(
            string fullPath,
            int maximumBytes,
            out byte[] bytes,
            out SceneLoadOutcome error
        )
        {
            bytes = null;
            error = SceneLoadOutcome.Success;
            try
            {
                FileInfo info = new FileInfo(fullPath);
                if (!info.Exists)
                {
                    error = SceneLoadOutcome.SourceMissing;
                    return false;
                }

                if (info.Length <= 0 || info.Length > maximumBytes)
                {
                    error = SceneLoadOutcome.SourceTooLarge;
                    return false;
                }

                bytes = File.ReadAllBytes(fullPath);
                if (bytes.Length == 0 || bytes.Length > maximumBytes)
                {
                    bytes = null;
                    error = SceneLoadOutcome.SourceTooLarge;
                    return false;
                }

                return true;
            }
            catch (IOException)
            {
                error = SceneLoadOutcome.SourceMissing;
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                error = SceneLoadOutcome.SourceMissing;
                return false;
            }
        }
    }

    public interface ISceneLoadClock
    {
        double NowSeconds { get; }
    }

    public sealed class RealtimeSceneLoadClock : ISceneLoadClock
    {
        public double NowSeconds => Time.realtimeSinceStartupAsDouble;
    }

    public sealed class SceneAssetLease : IDisposable
    {
        private readonly SceneAssetLifecycleTracker _tracker;
        private readonly Mesh _ownedMesh;
        private readonly Material _ownedMaterial;
        private bool _disposed;

        internal SceneAssetLease(
            GameObject root,
            SceneLoadOutcome outcome,
            SceneAssetMetrics metrics,
            SceneAssetLifecycleTracker tracker,
            Mesh ownedMesh,
            Material ownedMaterial
        )
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));
            Outcome = outcome;
            Metrics = metrics;
            _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
            _ownedMesh = ownedMesh;
            _ownedMaterial = ownedMaterial;
            _tracker.OnCreated();
        }

        public GameObject Root { get; }
        public SceneLoadOutcome Outcome { get; }
        public SceneAssetMetrics Metrics { get; }
        public bool IsDisposed => _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            DestroyObject(Root);
            DestroyObject(_ownedMesh);
            DestroyObject(_ownedMaterial);
            _tracker.OnDisposed();
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }

    public sealed class LocalSceneAssetLoader
    {
        public const double DefaultTimeoutSeconds = 2.0;
        private readonly ISceneAssetSource _source;
        private readonly ISceneLoadClock _clock;
        private readonly SceneAssetLifecycleTracker _tracker;
        private readonly Material _sharedMaterial;

        public LocalSceneAssetLoader(
            ISceneAssetSource source,
            ISceneLoadClock clock,
            SceneAssetLifecycleTracker tracker,
            Material sharedMaterial = null
        )
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
            _sharedMaterial = sharedMaterial;
        }

        public SceneAssetLease Load(
            SceneManifestData manifest,
            string sandboxRoot,
            double timeoutSeconds = DefaultTimeoutSeconds
        )
        {
            double started = _clock.NowSeconds;
            if (
                !SceneManifestCodec.TryValidate(manifest, out _)
                || !IsFinite(timeoutSeconds)
                || timeoutSeconds <= 0.0
                || timeoutSeconds > 30.0
            )
            {
                return CreateFallback(SceneLoadOutcome.ManifestInvalid);
            }

            if (!TryResolvePath(sandboxRoot, manifest.bundleFileName, out string fullPath))
            {
                return CreateFallback(SceneLoadOutcome.UnsafePath);
            }

            if (
                !_source.TryRead(
                    fullPath,
                    manifest.maxBundleBytes,
                    out byte[] bytes,
                    out SceneLoadOutcome sourceError
                )
            )
            {
                return CreateFallback(sourceError);
            }

            if (_clock.NowSeconds - started > timeoutSeconds)
            {
                return CreateFallback(SceneLoadOutcome.Timeout);
            }

            if (!FixedTimeHexEquals(Sha256(bytes), manifest.bundleSha256))
            {
                return CreateFallback(SceneLoadOutcome.HashMismatch);
            }

            SceneMeshBundleData bundle;
            try
            {
                bundle = JsonUtility.FromJson<SceneMeshBundleData>(
                    System.Text.Encoding.UTF8.GetString(bytes)
                );
            }
            catch (ArgumentException)
            {
                return CreateFallback(SceneLoadOutcome.BundleInvalid);
            }

            if (!TryValidateBundle(bundle, manifest, out Vector3[] vertices))
            {
                return CreateFallback(SceneLoadOutcome.BundleInvalid);
            }

            try
            {
                return CreateLoaded(manifest, bundle, bytes.Length, vertices);
            }
            catch (Exception exception) when (
                exception is ArgumentException
                || exception is InvalidOperationException
            )
            {
                return CreateFallback(SceneLoadOutcome.InternalError);
            }
        }

        private SceneAssetLease CreateLoaded(
            SceneManifestData manifest,
            SceneMeshBundleData bundle,
            int bundleBytes,
            Vector3[] vertices
        )
        {
            Dictionary<string, Transform> transforms = BuildHierarchy(manifest);
            GameObject root = RootObject(transforms);
            Transform meshTarget = transforms[manifest.defaultPartId];
            Mesh mesh = new Mesh { name = manifest.assetId + "-mesh" };
            mesh.vertices = vertices;
            mesh.triangles = bundle.triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            MeshFilter filter = meshTarget.gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = meshTarget.gameObject.AddComponent<MeshRenderer>();
            Material owned = AssignMaterial(renderer, bundle.color);
            return new SceneAssetLease(
                root,
                SceneLoadOutcome.Success,
                new SceneAssetMetrics(
                    manifest.assetId,
                    bundleBytes,
                    vertices.Length,
                    bundle.triangles.Length / 3,
                    0,
                    false
                ),
                _tracker,
                mesh,
                owned
            );
        }

        private SceneAssetLease CreateFallback(SceneLoadOutcome outcome)
        {
            GameObject root = new GameObject("S08FallbackRoot");
            GameObject visual = new GameObject("S08FallbackPlaceholder");
            visual.transform.SetParent(root.transform, false);
            Mesh mesh = new Mesh { name = "S08FallbackTetrahedron" };
            mesh.vertices = new[]
            {
                new Vector3(0.0f, 0.45f, 0.0f),
                new Vector3(-0.4f, -0.35f, -0.3f),
                new Vector3(0.4f, -0.35f, -0.3f),
                new Vector3(0.0f, -0.35f, 0.4f),
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3, 0, 3, 1, 1, 3, 2 };
            mesh.RecalculateNormals();
            MeshFilter filter = visual.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = visual.AddComponent<MeshRenderer>();
            Material owned = AssignMaterial(renderer, new[] { 1.0f, 0.45f, 0.1f, 1.0f });
            return new SceneAssetLease(
                root,
                outcome,
                new SceneAssetMetrics("fallback", 0, 4, 4, 0, true),
                _tracker,
                mesh,
                owned
            );
        }

        private Material AssignMaterial(MeshRenderer renderer, float[] color)
        {
            if (_sharedMaterial != null)
            {
                renderer.sharedMaterial = _sharedMaterial;
                return null;
            }

            Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            if (shader == null)
            {
                throw new InvalidOperationException("No S08 material shader is available.");
            }

            Material material = new Material(shader);
            if (color != null && color.Length == 4 && AllFinite(color))
            {
                material.color = new Color(color[0], color[1], color[2], color[3]);
            }

            renderer.sharedMaterial = material;
            return material;
        }

        private static Dictionary<string, Transform> BuildHierarchy(
            SceneManifestData manifest
        )
        {
            Dictionary<string, Transform> transforms =
                new Dictionary<string, Transform>(StringComparer.Ordinal);
            foreach (ScenePartData part in manifest.parts)
            {
                GameObject target = new GameObject(part.targetName);
                transforms.Add(part.partId, target.transform);
            }

            foreach (ScenePartData part in manifest.parts)
            {
                if (!string.IsNullOrEmpty(part.parentPartId))
                {
                    transforms[part.partId].SetParent(
                        transforms[part.parentPartId],
                        false
                    );
                }
            }

            return transforms;
        }

        private static GameObject RootObject(
            IReadOnlyDictionary<string, Transform> transforms
        )
        {
            foreach (Transform transform in transforms.Values)
            {
                if (transform.parent == null)
                {
                    return transform.gameObject;
                }
            }

            throw new InvalidOperationException("Validated scene hierarchy has no root.");
        }

        private static bool TryValidateBundle(
            SceneMeshBundleData bundle,
            SceneManifestData manifest,
            out Vector3[] vertices
        )
        {
            vertices = null;
            if (
                bundle == null
                || bundle.formatVersion != 1
                || bundle.assetId != manifest.assetId
                || bundle.vertices == null
                || bundle.triangles == null
                || bundle.vertices.Length == 0
                || bundle.vertices.Length % 3 != 0
                || bundle.triangles.Length == 0
                || bundle.triangles.Length % 3 != 0
            )
            {
                return false;
            }

            int vertexCount = bundle.vertices.Length / 3;
            int triangleCount = bundle.triangles.Length / 3;
            if (
                vertexCount > manifest.maxVertices
                || triangleCount > manifest.maxTriangles
                || !AllFinite(bundle.vertices)
            )
            {
                return false;
            }

            vertices = new Vector3[vertexCount];
            for (int index = 0; index < vertexCount; index += 1)
            {
                vertices[index] = new Vector3(
                    bundle.vertices[index * 3] * manifest.unitScale,
                    bundle.vertices[index * 3 + 1] * manifest.unitScale,
                    bundle.vertices[index * 3 + 2] * manifest.unitScale
                );
            }

            foreach (int triangleIndex in bundle.triangles)
            {
                if (triangleIndex < 0 || triangleIndex >= vertexCount)
                {
                    vertices = null;
                    return false;
                }
            }

            return true;
        }

        private static bool TryResolvePath(
            string root,
            string fileName,
            out string fullPath
        )
        {
            fullPath = null;
            if (
                string.IsNullOrWhiteSpace(root)
                || !Path.IsPathRooted(root)
                || fileName != Path.GetFileName(fileName)
                || Path.IsPathRooted(fileName)
            )
            {
                return false;
            }

            string fullRoot = Path.GetFullPath(root);
            string candidate = Path.GetFullPath(Path.Combine(fullRoot, fileName));
            string prefix = fullRoot.TrimEnd(Path.DirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            if (!candidate.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            fullPath = candidate;
            return true;
        }

        private static string Sha256(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(bytes);
                char[] text = new char[hash.Length * 2];
                const string digits = "0123456789abcdef";
                for (int index = 0; index < hash.Length; index += 1)
                {
                    text[index * 2] = digits[hash[index] >> 4];
                    text[index * 2 + 1] = digits[hash[index] & 0x0f];
                }

                return new string(text);
            }
        }

        private static bool FixedTimeHexEquals(string actual, string expected)
        {
            if (actual == null || expected == null || actual.Length != expected.Length)
            {
                return false;
            }

            int difference = 0;
            for (int index = 0; index < actual.Length; index += 1)
            {
                difference |= char.ToLowerInvariant(actual[index])
                    ^ char.ToLowerInvariant(expected[index]);
            }

            return difference == 0;
        }

        private static bool AllFinite(float[] values)
        {
            foreach (float value in values)
            {
                if (float.IsNaN(value) || float.IsInfinity(value))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
