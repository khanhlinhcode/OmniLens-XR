using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using PackageManagerPackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace OmniLensXR.Tests.Editor
{
    public sealed class S01FoundationTests
    {
        [Test]
        public void EditorVersionMatchesPinnedToolchain()
        {
            Assert.That(Application.unityVersion, Is.EqualTo("6000.3.27f1"));
        }

        [Test]
        public void CardboardPackageVersionMatchesPinnedToolchain()
        {
            PackageManagerPackageInfo package = PackageManagerPackageInfo.FindForAssetPath(
                "Packages/com.google.xr.cardboard/package.json"
            );

            Assert.That(package, Is.Not.Null);
            Assert.That(package.name, Is.EqualTo("com.google.xr.cardboard"));
            Assert.That(package.version, Is.EqualTo("1.36.0"));
        }

        [Test]
        public void IosDeploymentTargetMatchesPinnedToolchain()
        {
            Assert.That(PlayerSettings.iOS.targetOSVersionString, Is.EqualTo("15.0"));
        }
    }
}
