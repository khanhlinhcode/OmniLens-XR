using System;
using System.Collections.Generic;
using UnityEngine;

namespace OmniLensXR.S08
{
    public enum SceneManifestError
    {
        None = 0,
        EmptyPayload,
        InvalidJson,
        UnsupportedVersion,
        InvalidModel,
        InvalidBundle,
        InvalidProvenance,
        InvalidUnit,
        InvalidBudget,
        InvalidPart,
        DuplicatePart,
        MissingParent,
        BrokenHierarchy,
        MissingTarget,
        InvalidAnimation,
        InvalidLod,
    }

    [Serializable]
    public sealed class SceneLicenseData
    {
        public string licenseId;
        public string sourceUri;
        public string attribution;
        public bool redistributionAllowed;
    }

    [Serializable]
    public sealed class ScenePartData
    {
        public string partId;
        public string parentPartId;
        public string targetName;
    }

    [Serializable]
    public sealed class SceneAnimationData
    {
        public string animationName;
        public string targetPartId;
    }

    [Serializable]
    public sealed class SceneLodData
    {
        public int level;
        public float maxDistanceMeters;
        public int vertexBudget;
    }

    [Serializable]
    public sealed class SceneManifestData
    {
        public int schemaVersion;
        public string modelId;
        public string displayName;
        public string bundleFileName;
        public string bundleSha256;
        public string assetId;
        public string defaultPartId;
        public string unit;
        public float unitScale;
        public int maxBundleBytes;
        public int maxVertices;
        public int maxTriangles;
        public int maxTextureBytes;
        public SceneLicenseData license;
        public ScenePartData[] parts;
        public SceneAnimationData[] animations;
        public SceneLodData[] lods;
    }

    public static class SceneManifestCodec
    {
        public const int CurrentSchemaVersion = 1;
        public const int MaximumManifestCharacters = 64 * 1024;
        public const int MaximumParts = 256;
        public const int MaximumAnimations = 64;
        public const int MaximumLods = 8;

        public static bool TryDeserialize(
            string json,
            out SceneManifestData manifest,
            out SceneManifestError error
        )
        {
            manifest = null;
            error = SceneManifestError.None;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = SceneManifestError.EmptyPayload;
                return false;
            }

            if (json.Length > MaximumManifestCharacters)
            {
                error = SceneManifestError.InvalidJson;
                return false;
            }

            try
            {
                manifest = JsonUtility.FromJson<SceneManifestData>(json);
            }
            catch (ArgumentException)
            {
                error = SceneManifestError.InvalidJson;
                return false;
            }

            if (!TryValidate(manifest, out error))
            {
                manifest = null;
                return false;
            }

            return true;
        }

        public static bool TryValidate(
            SceneManifestData manifest,
            out SceneManifestError error
        )
        {
            error = SceneManifestError.None;
            if (manifest == null)
            {
                error = SceneManifestError.InvalidJson;
                return false;
            }

            if (manifest.schemaVersion != CurrentSchemaVersion)
            {
                error = SceneManifestError.UnsupportedVersion;
                return false;
            }

            if (
                !IsSafeId(manifest.modelId)
                || !IsSafeId(manifest.assetId)
                || string.IsNullOrWhiteSpace(manifest.displayName)
                || manifest.displayName.Length > 96
            )
            {
                error = SceneManifestError.InvalidModel;
                return false;
            }

            if (
                !IsSafeFileName(manifest.bundleFileName)
                || !IsSha256(manifest.bundleSha256)
            )
            {
                error = SceneManifestError.InvalidBundle;
                return false;
            }

            if (!HasValidProvenance(manifest.license))
            {
                error = SceneManifestError.InvalidProvenance;
                return false;
            }

            if (
                (manifest.unit != "meter"
                    && manifest.unit != "centimeter"
                    && manifest.unit != "millimeter")
                || !IsFinite(manifest.unitScale)
                || manifest.unitScale <= 0.0f
                || manifest.unitScale > 1000.0f
            )
            {
                error = SceneManifestError.InvalidUnit;
                return false;
            }

            if (
                manifest.maxBundleBytes <= 0
                || manifest.maxBundleBytes > 64 * 1024 * 1024
                || manifest.maxVertices <= 0
                || manifest.maxVertices > 1_000_000
                || manifest.maxTriangles <= 0
                || manifest.maxTriangles > 2_000_000
                || manifest.maxTextureBytes < 0
                || manifest.maxTextureBytes > 256 * 1024 * 1024
            )
            {
                error = SceneManifestError.InvalidBudget;
                return false;
            }

            if (
                manifest.parts == null
                || manifest.parts.Length == 0
                || manifest.parts.Length > MaximumParts
            )
            {
                error = SceneManifestError.InvalidPart;
                return false;
            }

            Dictionary<string, ScenePartData> parts =
                new Dictionary<string, ScenePartData>(StringComparer.Ordinal);
            int rootCount = 0;
            foreach (ScenePartData part in manifest.parts)
            {
                if (
                    part == null
                    || !IsSafeId(part.partId)
                    || !IsSafeId(part.targetName)
                )
                {
                    error = SceneManifestError.InvalidPart;
                    return false;
                }

                if (!parts.TryAdd(part.partId, part))
                {
                    error = SceneManifestError.DuplicatePart;
                    return false;
                }

                if (string.IsNullOrEmpty(part.parentPartId))
                {
                    rootCount += 1;
                }
                else if (!IsSafeId(part.parentPartId))
                {
                    error = SceneManifestError.InvalidPart;
                    return false;
                }
            }

            if (rootCount != 1)
            {
                error = SceneManifestError.BrokenHierarchy;
                return false;
            }

            foreach (ScenePartData part in manifest.parts)
            {
                if (
                    !string.IsNullOrEmpty(part.parentPartId)
                    && !parts.ContainsKey(part.parentPartId)
                )
                {
                    error = SceneManifestError.MissingParent;
                    return false;
                }

                if (HasCycle(part, parts))
                {
                    error = SceneManifestError.BrokenHierarchy;
                    return false;
                }
            }

            if (!parts.ContainsKey(manifest.defaultPartId))
            {
                error = SceneManifestError.MissingTarget;
                return false;
            }

            if (
                manifest.animations == null
                || manifest.animations.Length > MaximumAnimations
            )
            {
                error = SceneManifestError.InvalidAnimation;
                return false;
            }

            HashSet<string> animations = new HashSet<string>(StringComparer.Ordinal);
            foreach (SceneAnimationData animation in manifest.animations)
            {
                if (
                    animation == null
                    || !IsSafeId(animation.animationName)
                    || !animations.Add(animation.animationName)
                )
                {
                    error = SceneManifestError.InvalidAnimation;
                    return false;
                }

                if (!parts.ContainsKey(animation.targetPartId))
                {
                    error = SceneManifestError.MissingTarget;
                    return false;
                }
            }

            if (
                manifest.lods == null
                || manifest.lods.Length == 0
                || manifest.lods.Length > MaximumLods
            )
            {
                error = SceneManifestError.InvalidLod;
                return false;
            }

            float previousDistance = 0.0f;
            for (int index = 0; index < manifest.lods.Length; index += 1)
            {
                SceneLodData lod = manifest.lods[index];
                if (
                    lod == null
                    || lod.level != index
                    || !IsFinite(lod.maxDistanceMeters)
                    || lod.maxDistanceMeters <= previousDistance
                    || lod.vertexBudget <= 0
                    || lod.vertexBudget > manifest.maxVertices
                )
                {
                    error = SceneManifestError.InvalidLod;
                    return false;
                }

                previousDistance = lod.maxDistanceMeters;
            }

            return true;
        }

        public static bool IsSafeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 64)
            {
                return false;
            }

            foreach (char character in value)
            {
                if (
                    !char.IsLetterOrDigit(character)
                    && character != '-'
                    && character != '_'
                )
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsSafeFileName(string value)
        {
            return !string.IsNullOrWhiteSpace(value)
                && value.Length <= 96
                && value == System.IO.Path.GetFileName(value)
                && !System.IO.Path.IsPathRooted(value)
                && value.IndexOf("..", StringComparison.Ordinal) < 0;
        }

        private static bool IsSha256(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 64)
            {
                return false;
            }

            foreach (char character in value)
            {
                if (!Uri.IsHexDigit(character))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasValidProvenance(SceneLicenseData license)
        {
            return license != null
                && !string.IsNullOrWhiteSpace(license.licenseId)
                && license.licenseId.Length <= 96
                && !string.IsNullOrWhiteSpace(license.attribution)
                && license.attribution.Length <= 256
                && Uri.TryCreate(license.sourceUri, UriKind.Absolute, out Uri source)
                && (source.Scheme == "generated"
                    || source.Scheme == Uri.UriSchemeHttps
                    || source.Scheme == Uri.UriSchemeFile);
        }

        private static bool HasCycle(
            ScenePartData start,
            IReadOnlyDictionary<string, ScenePartData> parts
        )
        {
            HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
            ScenePartData current = start;
            while (current != null && !string.IsNullOrEmpty(current.parentPartId))
            {
                if (!visited.Add(current.partId))
                {
                    return true;
                }

                if (!parts.TryGetValue(current.parentPartId, out current))
                {
                    return false;
                }
            }

            return current != null && !visited.Add(current.partId);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
