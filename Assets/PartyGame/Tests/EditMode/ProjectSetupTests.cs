using System.Linq;
using NUnit.Framework;
using PartyGame.Core.App;
using PartyGame.Core.Content;
using PartyGame.Core.Modes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PartyGame.Tests
{
    /// <summary>
    /// Guards the things that only break once, silently, and then break everything:
    /// the boot scene, the build list and the content library.
    /// </summary>
    public class ProjectSetupTests
    {
        private const string BootstrapScenePath = "Assets/PartyGame/Scenes/Bootstrap.unity";

        /// <summary>
        /// The published privacy policy says the app contains no advertising, analytics or
        /// purchasing SDK, and the store listing's data disclosures are filled in on that basis.
        /// Adding one is a perfectly reasonable thing to do - but it has to happen together with
        /// the policy and the store forms, so this fails loudly rather than letting the app and
        /// its published claims drift apart.
        /// </summary>
        [Test]
        public void NoAdvertisingOrAnalyticsSdkIsInstalled()
        {
            var manifestPath = System.IO.Path.Combine(
                System.IO.Directory.GetParent(Application.dataPath).FullName, "Packages", "manifest.json");
            Assert.IsTrue(System.IO.File.Exists(manifestPath), "No package manifest at " + manifestPath);

            var manifest = System.IO.File.ReadAllText(manifestPath);
            var trackers = new[]
            {
                "com.google.ads", "com.google.firebase", "com.google.android.gms",
                "com.unity.ads", "com.unity.services.levelplay", "com.unity.services.analytics",
                "com.unity.analytics", "com.unity.purchasing", "com.ironsource", "com.applovin",
                "com.facebook", "com.appsflyer", "com.adjust"
            };

            foreach (var package in trackers)
                Assert.IsFalse(manifest.Contains(package),
                    package + " is installed. Update PRIVACY.md, the published policy at " +
                    "konkz7.github.io/Imposter/privacy.html, the Play Data safety form and the " +
                    "Apple privacy labels in the same release.");
        }

        /// <summary>
        /// The same promise from the other direction: the shipping ad service is the one that
        /// does nothing. A real one would need the policy updated before it could go out.
        /// </summary>
        [Test]
        public void TheOnlyAdServiceIsTheOneThatShowsNothing()
        {
            var runtime = typeof(AppServices).Assembly;
            var adServices = runtime.GetTypes()
                .Where(t => typeof(Core.Monetisation.IAdService).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .Select(t => t.Name)
                .ToList();

            CollectionAssert.AreEquivalent(new[] { "NullAdService" }, adServices,
                "An ad service was added. The published privacy policy says there is none.");
        }

        [Test]
        public void TheBootstrapSceneIsFirstInTheBuild()
        {
            var enabled = EditorBuildSettings.scenes.Where(s => s.enabled).ToList();
            Assert.IsNotEmpty(enabled, "No scenes are enabled in the build settings.");
            Assert.AreEqual(BootstrapScenePath, enabled[0].path);
        }

        [Test]
        public void TheBootstrapSceneOpensAndStartsTheApp()
        {
            var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);
            try
            {
                Assert.IsTrue(scene.IsValid(), "The bootstrap scene did not open.");

                var roots = scene.GetRootGameObjects();
                Assert.IsTrue(roots.Any(go => go.GetComponent<GameBootstrap>() != null),
                    "The bootstrap scene has no GameBootstrap.");

                foreach (var root in roots)
                foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                    Assert.IsNotNull(behaviour, "The bootstrap scene has a missing script reference.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void TheContentLibraryLoadsFromResources()
        {
            var library = Resources.Load<ContentLibrary>(ContentLibrary.ResourcePath);
            Assert.IsNotNull(library, "No ContentLibrary at Resources/" + ContentLibrary.ResourcePath);
            Assert.IsTrue(library.GameModes.All(m => m != null), "The library has a broken game mode reference.");
            Assert.IsTrue(library.WordCategories.All(c => c != null), "The library has a broken word category reference.");
        }

        [Test]
        public void EveryImplementedModeHasAnAvailableDefinition()
        {
            var content = new ContentService(Resources.Load<ContentLibrary>(ContentLibrary.ResourcePath));
            foreach (var modeId in GameModeFactory.ImplementedModes)
            {
                var definition = content.GetMode(modeId);
                Assert.IsNotNull(definition, modeId + " has no definition asset.");
                Assert.IsTrue(definition.Available, modeId + " is marked unavailable.");
                Assert.GreaterOrEqual(definition.MaxPlayers, definition.MinPlayers,
                    modeId + " has an impossible player range.");
            }
        }

        [Test]
        public void TextMeshProResourcesAreImported()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            Assert.IsNotNull(font, "Run Party Game > Import TextMeshPro Resources.");
            Assert.IsNotNull(Shader.Find("TextMeshPro/Distance Field"),
                "The TextMeshPro shaders are missing, so text would not render.");
        }
    }
}
